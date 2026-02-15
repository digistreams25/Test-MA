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
                            ed.WriteMessage("\nError [{0}]: {1}", ctx.Handle?.ToString() ?? "unknown", ctx.ErrorMessage);
                            continue;
                        }

                        try
                        {
                            Entity entity = tr.GetObject(ctx.ObjectId, OpenMode.ForWrite) as Entity;
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

                            // Apply schema element (fill-missing logic)
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
                            ed.WriteMessage("\nError processing object {0}: {1}", ctx.Handle?.ToString() ?? "unknown", ex.Message);
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
                PromptResult pr = ed.GetKeyword(pko);

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
                            Entity entity = tr.GetObject(ctx.ObjectId, OpenMode.ForWrite) as Entity;
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
    }
}
