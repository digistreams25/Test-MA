using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using TwinThread.Civil3D.LOI.Models;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Handles property value assignment based on different modes
    /// </summary>
    public class PropertySetValueAssigner
    {
        private PropertySetManager _propertySetManager;
        private Editor _editor;
        private PropertyResolver _propertyResolver;

        public PropertySetValueAssigner(PropertySetManager manager, Editor editor)
        {
            _propertySetManager = manager;
            _editor = editor;
            _propertyResolver = new PropertyResolver();
        }

        /// <summary>
        /// Assign values to properties for a list of entities
        /// </summary>
        public AssignmentResult AssignValues(
            List<EntityContext> contexts,
            List<SchemaElementParameter> parameters,
            ObjectId propertySetDefId,
            Database db)
        {
            AssignmentResult result = new AssignmentResult();

            foreach (var param in parameters)
            {
                string mode = param.ValueMode ?? "Manual";

                switch (mode.ToLowerInvariant())
                {
                    case "global":
                        AssignGlobalValue(contexts, param, propertySetDefId, db, result);
                        break;

                    case "manual":
                        AssignManualValue(contexts, param, propertySetDefId, db, result);
                        break;

                    case "perobject":
                        AssignPerObjectValue(contexts, param, propertySetDefId, db, result);
                        break;

                    case "rule":
                        AssignRuleBasedValue(contexts, param, propertySetDefId, db, result);
                        break;

                    default:
                        _editor.WriteMessage($"\nUnknown value mode '{mode}' for parameter '{param.Name}'. Skipping...");
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Assign same value to all objects
        /// </summary>
        private void AssignGlobalValue(
            List<EntityContext> contexts,
            SchemaElementParameter param,
            ObjectId propertySetDefId,
            Database db,
            AssignmentResult result)
        {
            // Use default value or prompt user
            string value = param.DefaultValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                PromptStringOptions pso = new PromptStringOptions($"\nEnter value for '{param.Name}' (all objects): ");
                pso.AllowSpaces = true;
                PromptResult pr = _editor.GetString(pso);
                if (pr.Status != PromptStatus.OK)
                {
                    result.Skipped += contexts.Count;
                    return;
                }
                value = pr.StringResult;
            }

            // Apply to all objects
            foreach (var ctx in contexts)
            {
                try
                {
                    if (_propertySetManager.SetPropertyValue(ctx.ObjectId, propertySetDefId, param.Name, value, db))
                    {
                        result.Updated++;
                    }
                    else
                    {
                        result.Failed++;
                        result.Errors.Add($"Handle {ctx.Handle}: Failed to set '{param.Name}'");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"Handle {ctx.Handle}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Prompt for value once, apply to all
        /// </summary>
        private void AssignManualValue(
            List<EntityContext> contexts,
            SchemaElementParameter param,
            ObjectId propertySetDefId,
            Database db,
            AssignmentResult result)
        {
            PromptStringOptions pso = new PromptStringOptions($"\nEnter value for '{param.Name}': ");
            pso.AllowSpaces = true;
            if (!string.IsNullOrWhiteSpace(param.DefaultValue))
            {
                pso.DefaultValue = param.DefaultValue;
                pso.UseDefaultValue = true;
            }

            PromptResult pr = _editor.GetString(pso);
            if (pr.Status != PromptStatus.OK)
            {
                result.Skipped += contexts.Count;
                return;
            }

            string value = pr.StringResult;

            foreach (var ctx in contexts)
            {
                try
                {
                    if (_propertySetManager.SetPropertyValue(ctx.ObjectId, propertySetDefId, param.Name, value, db))
                    {
                        result.Updated++;
                    }
                    else
                    {
                        result.Failed++;
                        result.Errors.Add($"Handle {ctx.Handle}: Failed to set '{param.Name}'");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"Handle {ctx.Handle}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Prompt for value per object
        /// </summary>
        private void AssignPerObjectValue(
            List<EntityContext> contexts,
            SchemaElementParameter param,
            ObjectId propertySetDefId,
            Database db,
            AssignmentResult result)
        {
            _editor.WriteMessage($"\n--- Entering per-object value mode for '{param.Name}' ---");

            foreach (var ctx in contexts)
            {
                PromptStringOptions pso = new PromptStringOptions($"\nValue for Handle {ctx.Handle} ({ctx.Name}): ");
                pso.AllowSpaces = true;
                if (!string.IsNullOrWhiteSpace(param.DefaultValue))
                {
                    pso.DefaultValue = param.DefaultValue;
                    pso.UseDefaultValue = true;
                }

                PromptResult pr = _editor.GetString(pso);
                if (pr.Status != PromptStatus.OK)
                {
                    result.Skipped++;
                    continue;
                }

                string value = pr.StringResult;

                try
                {
                    if (_propertySetManager.SetPropertyValue(ctx.ObjectId, propertySetDefId, param.Name, value, db))
                    {
                        result.Updated++;
                    }
                    else
                    {
                        result.Failed++;
                        result.Errors.Add($"Handle {ctx.Handle}: Failed to set '{param.Name}'");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"Handle {ctx.Handle}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Apply value based on mapping rule
        /// </summary>
        private void AssignRuleBasedValue(
            List<EntityContext> contexts,
            SchemaElementParameter param,
            ObjectId propertySetDefId,
            Database db,
            AssignmentResult result)
        {
            string rule = param.MappingRule ?? "name";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var ctx in contexts)
                {
                    try
                    {
                        string value = ExtractValueFromRule(ctx, rule, tr);

                        if (_propertySetManager.SetPropertyValue(ctx.ObjectId, propertySetDefId, param.Name, value, db))
                        {
                            result.Updated++;
                        }
                        else
                        {
                            result.Failed++;
                            result.Errors.Add($"Handle {ctx.Handle}: Failed to set '{param.Name}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Errors.Add($"Handle {ctx.Handle}: {ex.Message}");
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Extract value from entity context based on mapping rule
        /// Supports both simple context properties and dynamic PropertyResolver paths
        /// </summary>
        private string ExtractValueFromRule(EntityContext ctx, string rule, Transaction tr)
        {
            // First check for simple context properties (backward compatibility)
            switch (rule.ToLowerInvariant())
            {
                case "handle":
                    return ctx.Handle.ToString();
                case "layer":
                    return ctx.Layer ?? "";
                case "name":
                    return ctx.Name ?? "";
                case "objecttype":
                    return ctx.ObjectType ?? "";
                case "style":
                case "stylename":
                    return ctx.StyleName ?? "";
                case "network":
                case "networkname":
                    return ctx.NetworkName ?? "";
                case "partfamily":
                    return ctx.PartFamily ?? "";
                case "partsize":
                    return ctx.PartSize ?? "";
                case "alignment":
                case "alignmentname":
                    return ctx.AlignmentName ?? "";
                case "assembly":
                case "assemblyname":
                    return ctx.AssemblyName ?? "";
                case "region":
                case "regionname":
                    return ctx.RegionName ?? "";
                case "shapecode":
                case "shapecodename":
                    return ctx.ShapeCodeName ?? "";
            }

            // If not a simple property, try PropertyResolver for dynamic property paths
            // Format: "Pipe.Length3D", "Structure.RimElevation", etc.
            try
            {
                using (Entity entity = tr.GetObject(ctx.ObjectId, OpenMode.ForRead) as Entity)
                {
                    if (entity != null)
                    {
                        object resolvedValue = _propertyResolver.ResolveProperty(entity, rule, tr);
                        if (resolvedValue != null)
                        {
                            return FormatPropertyValue(resolvedValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PropertyResolver failed for rule '{rule}': {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Format property value for storage as string
        /// </summary>
        private string FormatPropertyValue(object value)
        {
            if (value == null)
                return "";

            // Format specific types
            if (value is double d)
                return d.ToString("F3");
            else if (value is int i)
                return i.ToString();
            else if (value is Autodesk.AutoCAD.Geometry.Point3d pt)
                return $"{pt.X:F3}, {pt.Y:F3}, {pt.Z:F3}";
            else if (value is Autodesk.AutoCAD.Geometry.Point2d pt2)
                return $"{pt2.X:F3}, {pt2.Y:F3}";
            else
                return value.ToString();
        }
    }

    /// <summary>
    /// Result of property value assignment operation
    /// </summary>
    public class AssignmentResult
    {
        public int Updated { get; set; }
        public int Created { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"Updated: {Updated}, Created: {Created}, Skipped: {Skipped}, Failed: {Failed}";
        }
    }
}
