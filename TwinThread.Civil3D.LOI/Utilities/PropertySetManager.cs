using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Aec.PropertyData;
using Autodesk.Aec.PropertyData.DatabaseServices;

namespace TwinThread.Civil3D.LOI.Utilities
{
    /// <summary>
    /// Manages AutoCAD Property Set Definitions for displaying LOI data in Properties palette
    /// </summary>
    public class PropertySetManager
    {
        private const string PropertySetName = "TwinThread LOI";
        private const string PropertySetDescription = "TwinThread Level of Information (LOI) data";

        /// <summary>
        /// Ensures the TwinThread LOI Property Set Definition exists in the drawing
        /// </summary>
        public void EnsurePropertySetDefinition(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    DictionaryPropertySetDefinitions dictPropSetDef =
                        new DictionaryPropertySetDefinitions(db);

                    // Check if property set already exists
                    if (dictPropSetDef.Has(PropertySetName, tr))
                    {
                        tr.Commit();
                        return; // Already exists
                    }

                    // Create new Property Set Definition
                    PropertySetDefinition psd = new PropertySetDefinition();
                    psd.SetToStandard(db);
                    psd.Description = PropertySetDescription;

                    // Define which object types this applies to (all AEC and Civil objects)
                    PropertySetDefinitionAppliesToFilter filter =
                        new PropertySetDefinitionAppliesToFilter();
                    filter.AddClass(RXClass.GetClass(typeof(Entity)));
                    psd.SetAppliesToFilter(filter, false);

                    // Add property definitions for LOI fields
                    AddPropertyDefinition(psd, "LOI_Status", "LOI Status", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Schema_Element", "Schema Element", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Milestone", "Milestone", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "PDS_Code", "PDS Code", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Company_Name", "Company Name", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Design_Stage", "Design Stage", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Design_Status", "Design Status", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Material", "Material", PropertyDataType.Text);
                    AddPropertyDefinition(psd, "Suitability_Code", "Suitability Code", PropertyDataType.Text);

                    // Add to dictionary
                    dictPropSetDef.AddNewRecord(PropertySetName, psd, tr);
                    tr.AddNewlyCreatedDBObject(psd, true);

                    tr.Commit();
                }
                catch (System.Exception)
                {
                    tr.Abort();
                    throw;
                }
            }
        }

        /// <summary>
        /// Writes LOI data to the Property Set for the given entity
        /// </summary>
        public void WriteToPropertySet(Entity entity, Dictionary<string, string> loiData, Transaction tr)
        {
            try
            {
                Database db = entity.Database;
                DictionaryPropertySetDefinitions dictPropSetDef =
                    new DictionaryPropertySetDefinitions(db);

                if (!dictPropSetDef.Has(PropertySetName, tr))
                {
                    throw new System.Exception("Property Set Definition not found. Run EnsurePropertySetDefinition first.");
                }

                ObjectId psdId = dictPropSetDef.GetAt(PropertySetName, tr);
                PropertySetDefinition psd = tr.GetObject(psdId, OpenMode.ForRead) as PropertySetDefinition;

                if (psd == null)
                    return;

                // Get or create property set on the entity
                PropertySet ps = null;
                ObjectIdCollection psIds = PropertyDataServices.GetPropertySets(entity);

                foreach (ObjectId psId in psIds)
                {
                    PropertySet existingPs = tr.GetObject(psId, OpenMode.ForWrite) as PropertySet;
                    if (existingPs != null && existingPs.PropertySetDefinition == psdId)
                    {
                        ps = existingPs;
                        break;
                    }
                }

                // If property set doesn't exist on entity, create it
                if (ps == null)
                {
                    ps = PropertySet.Create(db, entity.ObjectId, psdId);
                    tr.AddNewlyCreatedDBObject(ps, true);
                }
                else
                {
                    ps.UpgradeOpen();
                }

                // Write values to property set
                WriteProperty(ps, psd, "LOI_Status", loiData.ContainsKey(Constants.LoiStatus) ? loiData[Constants.LoiStatus] : "");
                WriteProperty(ps, psd, "Schema_Element", loiData.ContainsKey(Constants.SchemaElementName) ? loiData[Constants.SchemaElementName] : "");
                WriteProperty(ps, psd, "Milestone", loiData.ContainsKey(Constants.MilestoneName) ? loiData[Constants.MilestoneName] : "");
                WriteProperty(ps, psd, "PDS_Code", loiData.ContainsKey("PDS_Code") ? loiData["PDS_Code"] : "");
                WriteProperty(ps, psd, "Company_Name", loiData.ContainsKey("Company_Name") ? loiData["Company_Name"] : "");
                WriteProperty(ps, psd, "Design_Stage", loiData.ContainsKey("Design_Stage") ? loiData["Design_Stage"] : "");
                WriteProperty(ps, psd, "Design_Status", loiData.ContainsKey("Design_Status") ? loiData["Design_Status"] : "");
                WriteProperty(ps, psd, "Material", loiData.ContainsKey("Material") ? loiData["Material"] : "");
                WriteProperty(ps, psd, "Suitability_Code", loiData.ContainsKey("Suitability_Code") ? loiData["Suitability_Code"] : "");
            }
            catch (System.Exception)
            {
                // Silently fail - property sets might not be supported for all object types
                // XData will still be written as backup
            }
        }

        /// <summary>
        /// Removes the TwinThread LOI property set from an entity
        /// </summary>
        public void RemovePropertySet(Entity entity, Transaction tr)
        {
            try
            {
                Database db = entity.Database;
                DictionaryPropertySetDefinitions dictPropSetDef =
                    new DictionaryPropertySetDefinitions(db);

                if (!dictPropSetDef.Has(PropertySetName, tr))
                    return;

                ObjectId psdId = dictPropSetDef.GetAt(PropertySetName, tr);
                ObjectIdCollection psIds = PropertyDataServices.GetPropertySets(entity);

                foreach (ObjectId psId in psIds)
                {
                    PropertySet ps = tr.GetObject(psId, OpenMode.ForWrite) as PropertySet;
                    if (ps != null && ps.PropertySetDefinition == psdId)
                    {
                        ps.Erase();
                        break;
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Helper to add a property definition to a Property Set Definition
        /// </summary>
        private void AddPropertyDefinition(PropertySetDefinition psd, string name, string description, PropertyDataType dataType)
        {
            PropertyDefinition propDef = new PropertyDefinition();
            propDef.SetToStandard(psd.Database);
            propDef.Name = name;
            propDef.Description = description;
            propDef.DataType = dataType;
            propDef.DefaultData = "";

            PropertyDefinitionCollection propDefCol = psd.Definitions;
            propDefCol.Add(propDef);
        }

        /// <summary>
        /// Helper to write a property value to a Property Set
        /// </summary>
        private void WriteProperty(PropertySet ps, PropertySetDefinition psd, string propertyName, string value)
        {
            try
            {
                int propIndex = psd.Definitions.IndexOf(propertyName);
                if (propIndex >= 0)
                {
                    ps.SetAt(propIndex, value ?? "");
                }
            }
            catch
            {
                // Skip if property doesn't exist
            }
        }
    }
}
