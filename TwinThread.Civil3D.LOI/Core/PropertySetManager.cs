using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Aec.PropertyData;
using Autodesk.Aec.PropertyData.DatabaseServices;
using TwinThread.Civil3D.LOI.Models;
using TwinThread.Civil3D.LOI.Utilities;

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
                    psd.SetAppliesToFilter(new[] { "AcDbEntity" }, false);
                }
                else if (applicability.StartsWith("DXF:", StringComparison.OrdinalIgnoreCase))
                {
                    // DXF-based filter (e.g., "DXF:LINE,ARC,CIRCLE")
                    string[] dxfNames = applicability.Substring(4).Split(',');
                    psd.SetAppliesToFilter(dxfNames.Select(d => d.Trim()).ToArray(), false);
                }
                else
                {
                    // Civil 3D object types or custom filter
                    psd.SetAppliesToFilter(new[] { applicability }, false);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail - default to all entities
                System.Diagnostics.Debug.WriteLine($"Applicability filter error: {ex.Message}");
                psd.SetAppliesToFilter(new[] { "AcDbEntity" }, false);
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
                    for (int i = 0; i < psd.PropertyDefinitions.Count; i++)
                    {
                        var existingDef = psd.PropertyDefinitions[i];
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
                    propDef.DataType = MapStorageTypeToDataType(param.StorageType);
                    propDef.DefaultData = GetDefaultValue(param.DefaultValue, propDef.DataType);

                    psd.PropertyDefinitions.Add(propDef);
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
        private DataType MapStorageTypeToDataType(string storageType)
        {
            if (string.IsNullOrWhiteSpace(storageType))
                return DataType.Text;

            switch (storageType.ToLowerInvariant())
            {
                case "text":
                case "string":
                    return DataType.Text;
                case "integer":
                case "int":
                    return DataType.Integer;
                case "real":
                case "double":
                case "decimal":
                    return DataType.Real;
                case "boolean":
                case "bool":
                    return DataType.TrueFalse;
                default:
                    return DataType.Text;
            }
        }

        /// <summary>
        /// Get default value based on data type
        /// </summary>
        private object GetDefaultValue(string defaultValue, DataType dataType)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
                return null;

            try
            {
                switch (dataType)
                {
                    case DataType.Integer:
                        return int.TryParse(defaultValue, out int intVal) ? intVal : 0;
                    case DataType.Real:
                        return double.TryParse(defaultValue, out double dblVal) ? dblVal : 0.0;
                    case DataType.TrueFalse:
                        return bool.TryParse(defaultValue, out bool boolVal) && boolVal;
                    case DataType.Text:
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

                    // Attach new property set
                    PropertySet newPs = PropertySet.Attach(psd, entity);
                    tr.AddNewlyCreatedDBObject(newPs, true);

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
                            for (int i = 0; i < psd.PropertyDefinitions.Count; i++)
                            {
                                if (psd.PropertyDefinitions[i].Name == propertyName)
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
                            for (int i = 0; i < psd.PropertyDefinitions.Count; i++)
                            {
                                if (psd.PropertyDefinitions[i].Name == propertyName)
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
