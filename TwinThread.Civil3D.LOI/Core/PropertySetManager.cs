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
            string description)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DictionaryPropertySetDefinitions propSetDefs = new DictionaryPropertySetDefinitions(db);

                PropertySetDefinition psd = null;
                bool isNew = false;

                // Check if definition exists
                if (propSetDefs.Has(name, tr))
                {
                    ObjectId existingId = propSetDefs.GetAt(name);
                    psd = tr.GetObject(existingId, OpenMode.ForWrite) as PropertySetDefinition;

                    // Handle merge behavior
                    if (mergeBehavior == "Replace")
                    {
                        // Delete and recreate
                        psd.Erase();
                        psd = null;
                        isNew = true;
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

                    ObjectId psdId = propSetDefs.AddNewRecord(name, psd);
                    tr.AddNewlyCreatedDBObject(psd, true);
                }

                // Set applicability
                SetApplicability(psd, applicability, db, tr);

                // Add or update properties
                foreach (var param in parameters)
                {
                    AddOrUpdateProperty(psd, param, isNew || mergeBehavior == "Replace");
                }

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
                            break;
                        }
                    }
                }

                // Create new property if needed
                if (!exists)
                {
                    propDef = new PropertyDefinition();
                    propDef.SetToStandard(psd.Database);
                    propDef.Name = param.Name;
                    propDef.Description = param.Name;

                    // Set data type
                    propDef.DataType = MapStorageTypeToAecDataType(param.StorageType);
                    propDef.DefaultData = GetDefaultValue(param.DefaultValue, propDef.DataType);

                    psd.Definitions.Add(propDef);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Property '{param.Name}' add/update error: {ex.Message}");
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
                return null;

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
                return null;
            }
        }

        /// <summary>
        /// Attach property set to an entity
        /// </summary>
        public bool AttachPropertySet(ObjectId entityId, ObjectId propertySetDefId, Database db)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                    if (entity == null)
                        return false;

                    PropertySetDefinition psd = tr.GetObject(propertySetDefId, OpenMode.ForRead) as PropertySetDefinition;
                    if (psd == null)
                        return false;

                    // Check if already attached
                    ObjectIdCollection propSetIds = PropertyDataServices.GetPropertySets(entity);
                    foreach (ObjectId psId in propSetIds)
                    {
                        PropertySet ps = tr.GetObject(psId, OpenMode.ForRead) as PropertySet;
                        if (ps != null && ps.PropertySetDefinition == propertySetDefId)
                        {
                            // Already attached
                            tr.Commit();
                            return true;
                        }
                    }

                    // Attach new property set using PropertyDataServices
                    PropertyDataServices.AddPropertySet(entity, propertySetDefId);

                    tr.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Attach property set error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Set property value on an entity
        /// </summary>
        public bool SetPropertyValue(ObjectId entityId, ObjectId propertySetDefId, string propertyName, object value, Database db)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                    if (entity == null)
                        return false;

                    // Find the property set instance
                    ObjectIdCollection propSetIds = PropertyDataServices.GetPropertySets(entity);
                    foreach (ObjectId psId in propSetIds)
                    {
                        PropertySet ps = tr.GetObject(psId, OpenMode.ForWrite) as PropertySet;
                        if (ps != null && ps.PropertySetDefinition == propertySetDefId)
                        {
                            // Find property by name
                            PropertySetDefinition psd = tr.GetObject(ps.PropertySetDefinition, OpenMode.ForRead) as PropertySetDefinition;
                            for (int i = 0; i < psd.Definitions.Count; i++)
                            {
                                if (psd.Definitions[i].Name == propertyName)
                                {
                                    ps.SetAt(i, value);
                                    tr.Commit();
                                    return true;
                                }
                            }
                        }
                    }

                    tr.Abort();
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set property value error: {ex.Message}");
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
