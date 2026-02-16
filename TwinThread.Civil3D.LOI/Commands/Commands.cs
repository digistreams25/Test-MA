using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using TwinThread.Civil3D.LOI.Core;
using TwinThread.Civil3D.LOI.Discovery;
using TwinThread.Civil3D.LOI.Models;
using TwinThread.Civil3D.LOI.Utilities;

[assembly: CommandClass(typeof(TwinThread.Civil3D.LOI.Commands.Commands))]

namespace TwinThread.Civil3D.LOI.Commands
{
    /// <summary>
    /// Civil 3D commands for TwinThread LOI enforcement
    /// </summary>
    public class Commands
    {
        [CommandMethod("TT_APPLY_SCHEMA")]
        public void ApplySchema()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                System.Windows.Forms.MessageBox.Show("No active document found.");
                return;
            }

            Editor ed = acDoc.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            try
            {
                // Prompt for schema file
                PromptStringOptions pso = new PromptStringOptions("\nEnter schema JSON file path: ");
                pso.AllowSpaces = true;
                PromptResult pr = ed.GetString(pso);

                if (pr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                string schemaPath = pr.StringResult;

                // Load schema
                ed.WriteMessage("\n--- TwinThread LOI Schema Application ---");
                ed.WriteMessage("\nLoading schema from: {0}", schemaPath);

                SchemaLoader loader = new SchemaLoader();
                Schema schema = loader.LoadFromFile(schemaPath);

                ed.WriteMessage("\nProject: {0}", schema.ProjectName);
                ed.WriteMessage("\nMilestone: {0}", schema.Milestone?.Name);
                ed.WriteMessage("\nSchema Elements: {0}", schema.SchemaElements?.Count ?? 0);

                // Ensure RegApp exists
                XDataStore xdataStore = new XDataStore();
                xdataStore.EnsureRegApp(acDoc.Database);

                // Initialize PropertySet manager for native property display
                PropertySetManager propSetMgr = new PropertySetManager();
                ObjectId propSetDefId = ObjectId.Null;

                // Collect ALL unique parameters from ALL schema elements
                ed.WriteMessage("\n\nCollecting parameters from all schema elements...");
                var allParameters = new List<SchemaElementParameter>();
                var paramNames = new HashSet<string>();

                foreach (var element in schema.SchemaElements)
                {
                    if (element.SchemaElementParameters != null)
                    {
                        foreach (var param in element.SchemaElementParameters)
                        {
                            if (paramNames.Add(param.Name))  // Add only if unique
                            {
                                allParameters.Add(param);
                            }
                        }
                    }
                }

                ed.WriteMessage("\nTotal unique parameters: {0}", allParameters.Count);

                // Create PropertySet definition with ALL parameters upfront
                if (allParameters.Count > 0)
                {
                    propSetDefId = propSetMgr.EnsurePropertySetDefinition(
                        acDoc.Database,
                        "TwinThread_LOI",
                        allParameters,
                        "All",
                        "Update",
                        "TwinThread Level of Information Properties");

                    ed.WriteMessage("\nPropertySet definition created with {0} properties", allParameters.Count);
                }

                // Discover all objects
                ed.WriteMessage("\n\nDiscovering Civil 3D objects...");
                C3DDiscovery discovery = new C3DDiscovery();
                List<EntityContext> contexts = discovery.DiscoverAll(civilDoc, acDoc);

                ed.WriteMessage("\nTotal objects found: {0}", contexts.Count);

                // Initialize components
                MatchingEngine matcher = new MatchingEngine();
                LOIValidator validator = new LOIValidator();

                // Processing counters
                int processed = 0;
                int matched = 0;
                int passCount = 0;
                int warnCount = 0;
                int noMatch = 0;
                int errors = 0;
                List<string> unmatchedTypes = new List<string>();

                // Process each object
                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    foreach (EntityContext ctx in contexts)
                    {
                        // Skip if there was an error extracting context
                        if (!string.IsNullOrEmpty(ctx.ErrorMessage))
                        {
                            errors++;
                            ed.WriteMessage("\nError [{0}]: {1}", ctx.Handle.ToString(), ctx.ErrorMessage);
                            continue;
                        }

                        try
                        {
                            DBObject dbObj = tr.GetObject(ctx.ObjectId, OpenMode.ForWrite);
                            Autodesk.AutoCAD.DatabaseServices.Entity entity = dbObj as Autodesk.AutoCAD.DatabaseServices.Entity;
                            if (entity == null)
                            {
                                errors++;
                                continue;
                            }

                            // Skip locked objects
                            if (ctx.IsLocked)
                            {
                                errors++;
                                ed.WriteMessage("\nError: Cannot access locked object on layer '{0}'", ctx.Layer);
                                continue;
                            }

                            // Read existing XData
                            Dictionary<string, string> xdata = xdataStore.ReadXDataToDictionary(entity);

                            // Match to schema element
                            SchemaElement matchedElement = matcher.SelectElement(ctx, schema.SchemaElements, xdata);

                            if (matchedElement == null)
                            {
                                noMatch++;
                                unmatchedTypes.Add($"{ctx.ObjectType}:{ctx.Layer}:{ctx.Name}");
                                continue;
                            }

                            // Apply schema element to XData
                            validator.ApplySchemaElement(xdata, matchedElement, schema);

                            // Check XData size
                            if (!xdataStore.IsXDataSizeValid(xdata))
                            {
                                errors++;
                                ed.WriteMessage("\nError: XData too large for object {0}", ctx.Handle);
                                continue;
                            }

                            // Write XData
                            xdataStore.WriteXDataFromDictionary(entity, xdata);

                            // Write to PropertySet for native Civil 3D property display
                            try
                            {
                                // Attach PropertySet to entity (definition already created)
                                if (propSetDefId != ObjectId.Null)
                                {
                                    propSetMgr.AttachPropertySet(ctx.ObjectId, propSetDefId, acDoc.Database);

                                    // Write each LOI value to PropertySet
                                    foreach (var kvp in xdata)
                                    {
                                        propSetMgr.SetPropertyValue(ctx.ObjectId, propSetDefId, kvp.Key, kvp.Value, acDoc.Database);
                                    }
                                }
                            }
                            catch (System.Exception psEx)
                            {
                                // Log PropertySet error but don't fail - XData is already written
                                ed.WriteMessage("\nWarning: PropertySet error for {0}: {1}", ctx.Handle, psEx.Message);
                            }

                            // Update counters
                            processed++;
                            matched++;

                            if (xdata[Constants.LoiStatus] == Constants.StatusPass)
                                passCount++;
                            else
                                warnCount++;
                        }
                        catch (System.Exception ex)
                        {
                            errors++;
                            ed.WriteMessage("\nError processing object {0}: {1}", ctx.Handle.ToString(), ex.Message);
                        }
                    }

                    tr.Commit();
                }

                // Print summary
                ed.WriteMessage("\n\n--- Summary ---");
                ed.WriteMessage("\nObjects processed: {0}", processed);
                ed.WriteMessage("\nMatched to schema: {0}", matched);
                ed.WriteMessage("\n  PASS: {0}", passCount);
                ed.WriteMessage("\n  WARN: {0}", warnCount);
                ed.WriteMessage("\nNo match found: {0}", noMatch);
                ed.WriteMessage("\nErrors: {0}", errors);

                // Report unmatched objects
                if (noMatch > 0)
                {
                    ed.WriteMessage("\n\n--- Unmatched Objects (add to schema) ---");
                    var grouped = unmatchedTypes.GroupBy(x => x.Split(':')[0]);
                    foreach (var group in grouped)
                    {
                        ed.WriteMessage("\n{0}: {1} objects", group.Key, group.Count());
                        foreach (var item in group.Take(5))
                        {
                            var parts = item.Split(':');
                            ed.WriteMessage("\n  - Layer: {0}, Name: {1}", parts.Length > 1 ? parts[1] : "", parts.Length > 2 ? parts[2] : "");
                        }
                        if (group.Count() > 5)
                            ed.WriteMessage("\n  ... and {0} more", group.Count() - 5);
                    }
                }

                ed.WriteMessage("\n\nSchema application complete!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n\nERROR: {0}", ex.Message);
                ed.WriteMessage("\n{0}", ex.StackTrace);
            }
        }

        [CommandMethod("TT_CLEAR_SCHEMA")]
        public void ClearSchema()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                System.Windows.Forms.MessageBox.Show("No active document found.");
                return;
            }

            Editor ed = acDoc.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            try
            {
                // Confirm with user
                PromptKeywordOptions pko = new PromptKeywordOptions("\nRemove all TWINTHREAD XData from drawing? [Yes/No]");
                pko.Keywords.Add("Yes");
                pko.Keywords.Add("No");
                pko.Keywords.Default = "No";
                PromptResult pr = ed.GetKeywords(pko);

                if (pr.Status != PromptStatus.OK || pr.StringResult != "Yes")
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                ed.WriteMessage("\n--- Clearing TwinThread Schema ---");

                // Discover all objects
                C3DDiscovery discovery = new C3DDiscovery();
                List<EntityContext> contexts = discovery.DiscoverAll(civilDoc, acDoc);

                XDataStore xdataStore = new XDataStore();
                int cleared = 0;

                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    foreach (EntityContext ctx in contexts)
                    {
                        if (!string.IsNullOrEmpty(ctx.ErrorMessage))
                            continue;

                        try
                        {
                            DBObject dbObj = tr.GetObject(ctx.ObjectId, OpenMode.ForWrite);
                            Autodesk.AutoCAD.DatabaseServices.Entity entity = dbObj as Autodesk.AutoCAD.DatabaseServices.Entity;
                            if (entity != null)
                            {
                                xdataStore.RemoveXData(entity);
                                cleared++;
                            }
                        }
                        catch
                        {
                            // Skip errors
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\nCleared XData from {0} objects.", cleared);
                ed.WriteMessage("\n\nComplete!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n\nERROR: {0}", ex.Message);
            }
        }
        [CommandMethod("TT_INSPECT")]
        public void InspectObject()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                System.Windows.Forms.MessageBox.Show("No active document found.");
                return;
            }

            Editor ed = acDoc.Editor;

            try
            {
                // Prompt user to select object
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect object to inspect LOI data: ");
                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    DBObject dbObj = tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    Autodesk.AutoCAD.DatabaseServices.Entity entity = dbObj as Autodesk.AutoCAD.DatabaseServices.Entity;

                    if (entity == null)
                    {
                        ed.WriteMessage("\nSelected object is not a valid entity.");
                        return;
                    }

                    ed.WriteMessage("\n\n=== TwinThread LOI Inspector ===");
                    ed.WriteMessage("\nObject Type: {0}", entity.GetType().Name);
                    ed.WriteMessage("\nHandle: {0}", entity.Handle);
                    ed.WriteMessage("\nLayer: {0}", entity.Layer);

                    // Read XData
                    XDataStore xdataStore = new XDataStore();
                    Dictionary<string, string> xdata = xdataStore.ReadXDataToDictionary(entity);

                    if (xdata.Count == 0)
                    {
                        ed.WriteMessage("\n\n*** NO LOI DATA FOUND ***");
                        ed.WriteMessage("\nThis object has no TWINTHREAD XData.");
                        ed.WriteMessage("\nRun TT_APPLY_SCHEMA to add LOI data.");
                    }
                    else
                    {
                        ed.WriteMessage("\n\n--- LOI Status ---");
                        string status = xdata.ContainsKey(Constants.LoiStatus) ? xdata[Constants.LoiStatus] : "Unknown";
                        string statusIcon = status == Constants.StatusPass ? "[PASS]" : "[WARN]";
                        ed.WriteMessage("\nStatus: {0} {1}", statusIcon, status);

                        if (xdata.ContainsKey(Constants.SchemaElementName))
                            ed.WriteMessage("\nSchema Element: {0}", xdata[Constants.SchemaElementName]);

                        if (xdata.ContainsKey(Constants.MilestoneName))
                            ed.WriteMessage("\nMilestone: {0}", xdata[Constants.MilestoneName]);

                        if (xdata.ContainsKey(Constants.StorageMode))
                            ed.WriteMessage("\nStorage Mode: {0}", xdata[Constants.StorageMode]);

                        if (xdata.ContainsKey(Constants.PropertySetName))
                            ed.WriteMessage("\nProperty Set: {0}", xdata[Constants.PropertySetName]);

                        ed.WriteMessage("\n\n--- Parameters ---");
                        foreach (var kvp in xdata)
                        {
                            // Skip internal fields
                            if (kvp.Key == Constants.LoiStatus ||
                                kvp.Key == Constants.SchemaElementName ||
                                kvp.Key == Constants.SchemaElementId ||
                                kvp.Key == Constants.MilestoneName ||
                                kvp.Key == Constants.MilestoneId ||
                                kvp.Key == Constants.ProjectId ||
                                kvp.Key == Constants.ProjectName ||
                                kvp.Key == Constants.StorageMode ||
                                kvp.Key == Constants.PropertySetName ||
                                kvp.Key == Constants.UpdatedAtUtc ||
                                kvp.Key == Constants.LoiMissingFields)
                                continue;

                            ed.WriteMessage("\n  {0}: {1}", kvp.Key, kvp.Value);
                        }

                        ed.WriteMessage("\n\nTotal parameters: {0}", xdata.Count);
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\n\n=================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n\nERROR: {0}", ex.Message);
            }
        }

        [CommandMethod("TT_SHOW_LOI")]
        public void ShowLOI()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                System.Windows.Forms.MessageBox.Show("No active document found.");
                return;
            }

            Editor ed = acDoc.Editor;

            try
            {
                // Prompt user to select object
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect object to view LOI properties: ");
                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    DBObject dbObj = tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    Autodesk.AutoCAD.DatabaseServices.Entity entity = dbObj as Autodesk.AutoCAD.DatabaseServices.Entity;

                    if (entity == null)
                    {
                        ed.WriteMessage("\nSelected object is not a valid entity.");
                        return;
                    }

                    // Display LOI data in a dialog box
                    LOIDisplay display = new LOIDisplay();
                    string objectType = entity.GetType().Name;
                    display.ShowInMessageBox(entity, objectType);

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nERROR: {0}", ex.Message);
            }
        }

        [CommandMethod("TT_REPORT")]
        public void ExportReport()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                System.Windows.Forms.MessageBox.Show("No active document found.");
                return;
            }

            Editor ed = acDoc.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            try
            {
                // Prompt for output file path
                PromptStringOptions pso = new PromptStringOptions("\nEnter output file path (CSV): ");
                pso.AllowSpaces = true;
                pso.DefaultValue = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(acDoc.Name),
                    System.IO.Path.GetFileNameWithoutExtension(acDoc.Name) + "_LOI_Report.csv"
                );
                PromptResult pr = ed.GetString(pso);

                if (pr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nCommand cancelled.");
                    return;
                }

                string outputPath = pr.StringResult;

                ed.WriteMessage("\n--- Generating LOI Report ---");

                // Discover all objects
                C3DDiscovery discovery = new C3DDiscovery();
                List<EntityContext> contexts = discovery.DiscoverAll(civilDoc, acDoc);

                XDataStore xdataStore = new XDataStore();
                List<string> reportLines = new List<string>();

                // CSV Header
                reportLines.Add("Handle,ObjectType,Name,Layer,LOI_Status,Schema_Element,Milestone,PDS_Code,Company_Name,Design_Stage,Design_Status,Material,Suitability_Code");

                int exported = 0;

                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    foreach (EntityContext ctx in contexts)
                    {
                        if (!string.IsNullOrEmpty(ctx.ErrorMessage))
                            continue;

                        try
                        {
                            DBObject dbObj = tr.GetObject(ctx.ObjectId, OpenMode.ForRead);
                            Autodesk.AutoCAD.DatabaseServices.Entity entity = dbObj as Autodesk.AutoCAD.DatabaseServices.Entity;

                            if (entity == null)
                                continue;

                            Dictionary<string, string> xdata = xdataStore.ReadXDataToDictionary(entity);

                            if (xdata.Count == 0)
                                continue; // Skip objects without LOI data

                            // Build CSV line
                            string line = string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\"",
                                ctx.Handle.ToString(),
                                ctx.ObjectType,
                                EscapeCsv(ctx.Name),
                                EscapeCsv(ctx.Layer),
                                xdata.ContainsKey(Constants.LoiStatus) ? xdata[Constants.LoiStatus] : "",
                                xdata.ContainsKey(Constants.SchemaElementName) ? EscapeCsv(xdata[Constants.SchemaElementName]) : "",
                                xdata.ContainsKey(Constants.MilestoneName) ? EscapeCsv(xdata[Constants.MilestoneName]) : "",
                                xdata.ContainsKey("PDS_Code") ? xdata["PDS_Code"] : "",
                                xdata.ContainsKey("Company_Name") ? EscapeCsv(xdata["Company_Name"]) : "",
                                xdata.ContainsKey("Design_Stage") ? xdata["Design_Stage"] : "",
                                xdata.ContainsKey("Design_Status") ? EscapeCsv(xdata["Design_Status"]) : "",
                                xdata.ContainsKey("Material") ? EscapeCsv(xdata["Material"]) : "",
                                xdata.ContainsKey("Suitability_Code") ? xdata["Suitability_Code"] : ""
                            );

                            reportLines.Add(line);
                            exported++;
                        }
                        catch
                        {
                            // Skip errors
                        }
                    }

                    tr.Commit();
                }

                // Write to file
                System.IO.File.WriteAllLines(outputPath, reportLines);

                ed.WriteMessage("\n\nReport exported successfully!");
                ed.WriteMessage("\nObjects exported: {0}", exported);
                ed.WriteMessage("\nFile: {0}", outputPath);
                ed.WriteMessage("\n\nOpen in Excel or any CSV viewer.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n\nERROR: {0}", ex.Message);
            }
        }

        [CommandMethod("TT_HIGHLIGHT")]
        public void HighlightByStatus()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            if (acDoc == null)
            {
                System.Windows.Forms.MessageBox.Show("No active document found.");
                return;
            }

            Editor ed = acDoc.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            try
            {
                ed.WriteMessage("\n--- Highlighting Objects by LOI Status ---");
                ed.WriteMessage("\n  Green (Color 3) = PASS");
                ed.WriteMessage("\n  Yellow (Color 2) = WARN");
                ed.WriteMessage("\n  White (Color 7) = No LOI Data");

                // Discover all objects
                C3DDiscovery discovery = new C3DDiscovery();
                List<EntityContext> contexts = discovery.DiscoverAll(civilDoc, acDoc);

                XDataStore xdataStore = new XDataStore();
                int passCount = 0;
                int warnCount = 0;
                int noDataCount = 0;

                using (Transaction tr = acDoc.Database.TransactionManager.StartTransaction())
                {
                    foreach (EntityContext ctx in contexts)
                    {
                        if (!string.IsNullOrEmpty(ctx.ErrorMessage))
                            continue;

                        try
                        {
                            DBObject dbObj = tr.GetObject(ctx.ObjectId, OpenMode.ForWrite);
                            Autodesk.AutoCAD.DatabaseServices.Entity entity = dbObj as Autodesk.AutoCAD.DatabaseServices.Entity;

                            if (entity == null)
                                continue;

                            Dictionary<string, string> xdata = xdataStore.ReadXDataToDictionary(entity);

                            if (xdata.Count == 0)
                            {
                                entity.ColorIndex = 7; // White - No data
                                noDataCount++;
                            }
                            else if (xdata.ContainsKey(Constants.LoiStatus))
                            {
                                if (xdata[Constants.LoiStatus] == Constants.StatusPass)
                                {
                                    entity.ColorIndex = 3; // Green - PASS
                                    passCount++;
                                }
                                else
                                {
                                    entity.ColorIndex = 2; // Yellow - WARN
                                    warnCount++;
                                }
                            }
                        }
                        catch
                        {
                            // Skip errors
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\n\n--- Highlighting Complete ---");
                ed.WriteMessage("\nGreen (PASS): {0}", passCount);
                ed.WriteMessage("\nYellow (WARN): {0}", warnCount);
                ed.WriteMessage("\nWhite (No Data): {0}", noDataCount);
                ed.WriteMessage("\n\nRun REGEN to refresh display if needed.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n\nERROR: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Escape special characters for CSV format
        /// </summary>
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Replace quotes with double quotes
            return value.Replace("\"", "\"\"");
        }
    }
}
