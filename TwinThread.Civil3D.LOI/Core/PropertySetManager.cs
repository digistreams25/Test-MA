using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Aec.PropertyData;
using Autodesk.Aec.PropertyData.DatabaseServices;
using TwinThread.Civil3D.LOI.Models;
using TwinThread.Civil3D.LOI.Utilities;
using AecDataType = Autodesk.Aec.PropertyData.DataType;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Manages Civil 3D Property Set Definition creation and attachment
    /// </summary>
    public class PropertySetManager
    {
        /// <summary>
        /// Create or update Property Set Definition
        /// </summary>
        public ObjectId EnsurePropertySetDefinition(
            Database db,
            string name,
            List<SchemaElementParameter> parameters,
            string applicability,
            string mergeBehavior,
            string description,
            Autodesk.AutoCAD.EditorInput.Editor ed = null)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DictionaryPropertySetDefinitions propSetDefs = new DictionaryPropertySetDefinitions(db);

                PropertySetDefinition psd = null;
                bool isNew = false;
                bool shouldClearProperties = false;

                // Check if definition exists
                if (propSetDefs.Has(name, tr))
                {
                    ObjectId existingId = propSetDefs.GetAt(name);
                    psd = tr.GetObject(existingId, OpenMode.ForWrite) as PropertySetDefinition;

                    // Handle merge behavior
                    if (mergeBehavior == "Replace")
                    {
                        // Clear all existing properties instead of erasing the definition
                        // This avoids eDuplicateKey errors
                        shouldClearProperties = true;
                    }
                }
                else
                {
                    isNew = true;
                }

                // Create new definition if needed
                if (isNew)
                {
                    psd = new PropertySetDefinition();
                    psd.SetToStandard(db);
                    psd.Description = description ?? $"Property Set for {name}";

                    propSetDefs.AddNewRecord(name, psd);
                    tr.AddNewlyCreatedDBObject(psd, true);
                }

                // Clear existing properties if Replace mode
                if (shouldClearProperties && psd != null)
                {
                    // Remove all properties from the definition
                    while (psd.Definitions.Count > 0)
                    {
                        psd.Definitions.RemoveAt(0);
                    }
                    System.Diagnostics.Debug.WriteLine($"Cleared all properties from existing PropertySet definition '{name}'");
                }

                // Set applicability
                SetApplicability(psd, applicability, db, tr);

                // Add or update properties
                System.Diagnostics.Debug.WriteLine($"PropertySet '{name}': isNew={isNew}, mergeBehavior={mergeBehavior}, shouldClearProperties={shouldClearProperties}");
                System.Diagnostics.Debug.WriteLine($"Adding {parameters.Count} properties to PropertySet definition");

                foreach (var param in parameters)
                {
                    try
                    {
                        AddOrUpdateProperty(psd, param, isNew || shouldClearProperties);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"EXCEPTION adding property '{param.Name}': {ex.Message}";
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                        if (ed != null)
                        {
                            ed.WriteMessage("\n  ERROR: " + errorMsg);
                        }
                        // Continue with other properties
                    }
                }

                System.Diagnostics.Debug.WriteLine($"PropertySet definition now has {psd.Definitions.Count} properties");

                tr.Commit();
                return psd.ObjectId;
            }
        }

        /// <summary>
        /// Set applicability for property set definition
        /// </summary>
        private void SetApplicability(PropertySetDefinition psd, string applicability, Database db, Transaction tr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicability) || applicability == "All")
                {
                    // Apply to all entities
                    var appliesTo = new StringCollection();
                    appliesTo.Add("AcDbEntity");
                    psd.SetAppliesToFilter(appliesTo, false);
                }
                else if (applicability.StartsWith("DXF:", StringComparison.OrdinalIgnoreCase))
                {
                    // DXF-based filter (e.g., "DXF:LINE,ARC,CIRCLE")
                    string[] dxfNames = applicability.Substring(4).Split(',');
                    var appliesTo = new StringCollection();
                    foreach (string name in dxfNames)
                    {
                        appliesTo.Add(name.Trim());
                    }
                    psd.SetAppliesToFilter(appliesTo, false);
                }
                else
                {
                    // Civil 3D object types or custom filter
                    var appliesTo = new StringCollection();
                    appliesTo.Add(applicability);
                    psd.SetAppliesToFilter(appliesTo, false);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail - default to all entities
                System.Diagnostics.Debug.WriteLine($"Applicability filter error: {ex.Message}");
                var appliesTo = new StringCollection();
                appliesTo.Add("AcDbEntity");
                psd.SetAppliesToFilter(appliesTo, false);
            }
        }

        /// <summary>
        /// Add or update a property in the definition
        /// </summary>
        private void AddOrUpdateProperty(PropertySetDefinition psd, SchemaElementParameter param, bool forceAdd)
        {
            try
            {
                PropertyDefinition propDef = null;
                bool exists = false;

                // Check if property already exists
                if (!forceAdd)
                {
                    for (int i = 0; i < psd.Definitions.Count; i++)
                    {
                        var existingDef = psd.Definitions[i];
                        if (existingDef.Name == param.Name)
                        {
                            propDef = existingDef;
                            exists = true;
                            System.Diagnostics.Debug.WriteLine($"Property '{param.Name}' already exists in definition");
                            break;
                        }
                    }
                }

                // Create new property if needed
                if (!exists)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding property '{param.Name}' to definition (type: {param.StorageType})");

                    propDef = new PropertyDefinition();
                    propDef.SetToStandard(psd.Database);
                    propDef.Name = param.Name;

                    // Set description to indicate computed vs manual properties
                    if (param.ValueMode == "Rule" && !string.IsNullOrWhiteSpace(param.MappingRule))
                    {
                        propDef.Description = $"{param.Name} (Auto-computed from object)";
                    }
                    else
                    {
                        propDef.Description = param.Name;
                    }

                    // Set data type
                    propDef.DataType = MapStorageTypeToAecDataType(param.StorageType);
                    propDef.DefaultData = GetDefaultValue(param.DefaultValue, propDef.DataType);

                    psd.Definitions.Add(propDef);

                    System.Diagnostics.Debug.WriteLine($"Property '{param.Name}' added successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Property '{param.Name}' add/update error: {ex.Message}");
                throw; // Re-throw so caller can see the error
            }
        }

        /// <summary>
        /// Map schema storage type to Property Data type
        /// </summary>
        private AecDataType MapStorageTypeToAecDataType(string storageType)
        {
            if (string.IsNullOrWhiteSpace(storageType))
                return AecDataType.Text;

            switch (storageType.ToLowerInvariant())
            {
                case "text":
                case "string":
                    return AecDataType.Text;
                case "integer":
                case "int":
                    return AecDataType.Integer;
                case "real":
                case "double":
                case "decimal":
                    return AecDataType.Real;
                case "boolean":
                case "bool":
                    return AecDataType.TrueFalse;
                default:
                    return AecDataType.Text;
            }
        }

        /// <summary>
        /// Get default value based on data type
        /// </summary>
        private object GetDefaultValue(string defaultValue, AecDataType dataType)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                // Return type-appropriate defaults instead of null
                // AutoCAD API throws ArgumentException if defaultData is null
                switch (dataType)
                {
                    case AecDataType.Integer:
                        return 0;
                    case AecDataType.Real:
                        return 0.0;
                    case AecDataType.TrueFalse:
                        return false;
                    case AecDataType.Text:
                    default:
                        return "";  // Empty string, not null
                }
            }

            try
            {
                switch (dataType)
                {
                    case AecDataType.Integer:
                        return int.TryParse(defaultValue, out int intVal) ? intVal : 0;
                    case AecDataType.Real:
                        return double.TryParse(defaultValue, out double dblVal) ? dblVal : 0.0;
                    case AecDataType.TrueFalse:
                        return bool.TryParse(defaultValue, out bool boolVal) && boolVal;
                    case AecDataType.Text:
                    default:
                        return defaultValue;
                }
            }
            catch
            {
                // Return type-appropriate defaults on error
                switch (dataType)
                {
                    case AecDataType.Integer:
                        return 0;
                    case AecDataType.Real:
                        return 0.0;
                    case AecDataType.TrueFalse:
                        return false;
                    case AecDataType.Text:
                    default:
                        return "";
                }
            }
        }

        /// <summary>
        /// Attach property set to an entity
        /// </summary>
        public bool AttachPropertySet(ObjectId entityId, ObjectId propertySetDefId, Database db, Autodesk.AutoCAD.EditorInput.Editor ed = null)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                    if (entity == null)
                    {
                        string msg = "AttachPropertySet FAILED: Entity is null";
                        System.Diagnostics.Debug.WriteLine(msg);
                        ed?.WriteMessage("\n  " + msg);
                        return false;
                    }

                    PropertySetDefinition psd = tr.GetObject(propertySetDefId, OpenMode.ForRead) as PropertySetDefinition;
                    if (psd == null)
                    {
                        string msg = "AttachPropertySet FAILED: PropertySetDefinition is null";
                        System.Diagnostics.Debug.WriteLine(msg);
                        ed?.WriteMessage("\n  " + msg);
                        return false;
                    }

                    // Check if already attached
                    ObjectIdCollection propSetIds = PropertyDataServices.GetPropertySets(entity);
                    ed?.WriteMessage("\n  AttachPropertySet: Entity currently has {0} property sets", propSetIds.Count);

                    foreach (ObjectId psId in propSetIds)
                    {
                        PropertySet ps = tr.GetObject(psId, OpenMode.ForRead) as PropertySet;
                        if (ps != null && ps.PropertySetDefinition == propertySetDefId)
                        {
                            // Check if the instance is in sync with the definition
                            PropertySetDefinition defCheck = tr.GetObject(propertySetDefId, OpenMode.ForRead) as PropertySetDefinition;
                            int instanceCount = ps.Count;
                            int definitionCount = defCheck.Definitions.Count;

                            if (instanceCount == definitionCount)
                            {
                                // Already attached and in sync
                                ed?.WriteMessage("\n  AttachPropertySet: Already attached and in sync ({0} properties), skipping", instanceCount);
                                tr.Commit();
                                return true;
                            }
                            else
                            {
                                // Instance is out of sync with definition - remove so we can reattach fresh
                                ed?.WriteMessage("\n  AttachPropertySet: Instance out of sync (instance={0}, definition={1}), removing stale instance", instanceCount, definitionCount);
                                ps.UpgradeOpen();
                                PropertyDataServices.RemovePropertySet(entity, propertySetDefId);
                                break;
                            }
                        }
                    }

                    // Attach new property set using PropertyDataServices
                    ed?.WriteMessage("\n  AttachPropertySet: Calling PropertyDataServices.AddPropertySet");
                    PropertyDataServices.AddPropertySet(entity, propertySetDefId);

                    // Verify attachment
                    propSetIds = PropertyDataServices.GetPropertySets(entity);
                    ed?.WriteMessage("\n  AttachPropertySet: After attach, entity has {0} property sets", propSetIds.Count);

                    tr.Commit();
                    ed?.WriteMessage("\n  AttachPropertySet: SUCCESS");
                    return true;
                }
            }
            catch (Exception ex)
            {
                string msg = $"AttachPropertySet EXCEPTION: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(msg + "\n" + ex.StackTrace);
                ed?.WriteMessage("\n  " + msg);
                return false;
            }
        }

        /// <summary>
        /// Set property value on an entity
        /// </summary>
        public bool SetPropertyValue(ObjectId entityId, ObjectId propertySetDefId, string propertyName, object value, Database db, Autodesk.AutoCAD.EditorInput.Editor ed = null)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        string msg = $"SetPropertyValue FAILED: Entity is null for {propertyName}";
                        System.Diagnostics.Debug.WriteLine(msg);
                        ed?.WriteMessage("\n  " + msg);
                        return false;
                    }

                    // Find the property set instance
                    ObjectIdCollection propSetIds = PropertyDataServices.GetPropertySets(entity);

                    ed?.WriteMessage("\n  SetPropertyValue({0}): Entity has {1} property sets attached", propertyName, propSetIds.Count);

                    foreach (ObjectId psId in propSetIds)
                    {
                        PropertySet ps = tr.GetObject(psId, OpenMode.ForWrite) as PropertySet;
                        if (ps != null && ps.PropertySetDefinition == propertySetDefId)
                        {
                            // Find property by name
                            PropertySetDefinition psd = tr.GetObject(ps.PropertySetDefinition, OpenMode.ForRead) as PropertySetDefinition;

                            ed?.WriteMessage("\n  SetPropertyValue({0}): Found matching PropertySet with {1} properties", propertyName, psd.Definitions.Count);

                            for (int i = 0; i < psd.Definitions.Count; i++)
                            {
                                if (psd.Definitions[i].Name == propertyName)
                                {
                                    ed?.WriteMessage("\n  SetPropertyValue: Setting {0} = {1}", propertyName, value);
                                    ps.SetAt(i, value);
                                    tr.Commit();
                                    return true;
                                }
                            }

                            string msg = $"SetPropertyValue FAILED: Property '{propertyName}' not found in definition";
                            System.Diagnostics.Debug.WriteLine(msg);
                            ed?.WriteMessage("\n  " + msg);
                            tr.Abort();
                            return false;
                        }
                    }

                    string msg2 = $"SetPropertyValue FAILED: PropertySet not attached to entity for {propertyName}";
                    System.Diagnostics.Debug.WriteLine(msg2);
                    ed?.WriteMessage("\n  " + msg2);
                    tr.Abort();
                    return false;
                }
            }
            catch (Exception ex)
            {
                string msg = $"SetPropertyValue EXCEPTION for {propertyName}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(msg + "\n" + ex.StackTrace);
                ed?.WriteMessage("\n  " + msg);
                return false;
            }
        }

        /// <summary>
        /// Get property value from an entity
        /// </summary>
        public object GetPropertyValue(ObjectId entityId, ObjectId propertySetDefId, string propertyName, Database db)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                    if (entity == null)
                        return null;

                    ObjectIdCollection propSetIds = PropertyDataServices.GetPropertySets(entity);
                    foreach (ObjectId psId in propSetIds)
                    {
                        PropertySet ps = tr.GetObject(psId, OpenMode.ForRead) as PropertySet;
                        if (ps != null && ps.PropertySetDefinition == propertySetDefId)
                        {
                            PropertySetDefinition psd = tr.GetObject(ps.PropertySetDefinition, OpenMode.ForRead) as PropertySetDefinition;
                            for (int i = 0; i < psd.Definitions.Count; i++)
                            {
                                if (psd.Definitions[i].Name == propertyName)
                                {
                                    object val = ps.GetAt(i);
                                    tr.Commit();
                                    return val;
                                }
                            }
                        }
                    }

                    tr.Commit();
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
