using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

// Resolve Entity ambiguity - use AutoCAD Entity as base type
using AcadEntity = Autodesk.AutoCAD.DatabaseServices.Entity;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Resolves schema property paths to actual Civil 3D API calls
    /// Maps strings like "Pipe.Length3D" to actual object properties
    /// </summary>
    public class PropertyResolver
    {
        private static readonly HashSet<string> _loggedUnsupportedPaths = new HashSet<string>();

        /// <summary>
        /// Resolve a property path to its actual value from a Civil 3D object
        /// </summary>
        /// <param name="entity">The entity object (Pipe, Structure, etc.)</param>
        /// <param name="propertyPath">Property path like "Pipe.Length3D" or "Structure.RimElevation"</param>
        /// <param name="transaction">Active transaction for reading related objects</param>
        /// <returns>Property value as object, or null if unsupported/unavailable</returns>
        public object ResolveProperty(AcadEntity entity, string propertyPath, Transaction transaction)
        {
            if (entity == null || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            try
            {
                // Parse property path (format: "ObjectType.PropertyName" or just "PropertyName")
                string[] parts = propertyPath.Split('.');
                string propertyName = parts.Length > 1 ? parts[1] : parts[0];

                // Determine object type and resolve
                if (entity is Pipe pipe)
                {
                    return ResolvePipeProperty(pipe, propertyName);
                }
                else if (entity is Structure structure)
                {
                    return ResolveStructureProperty(structure, propertyName);
                }
                else if (entity is PressurePipe pressurePipe)
                {
                    return ResolvePressurePipeProperty(pressurePipe, propertyName);
                }
                else if (entity is PressureFitting pressureFitting)
                {
                    return ResolvePressureFittingProperty(pressureFitting, propertyName);
                }
                else if (entity is PressureAppurtenance pressureAppurtenance)
                {
                    return ResolvePressureAppurtenanceProperty(pressureAppurtenance, propertyName);
                }
                else if (entity is Corridor corridor)
                {
                    return ResolveCorridorProperty(corridor, propertyName);
                }
                else if (entity is Alignment alignment)
                {
                    return ResolveAlignmentProperty(alignment, propertyName);
                }
                else if (entity is Solid3d solid)
                {
                    return ResolveSolidProperty(solid, propertyName);
                }
                else
                {
                    // Generic AutoCAD entity properties
                    return ResolveGenericProperty(entity, propertyName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PropertyResolver error for '{propertyPath}': {ex.Message}");
                return null;
            }
        }

        #region Pipe Network - Pipe

        private object ResolvePipeProperty(Pipe pipe, string propertyName)
        {
            switch (propertyName)
            {
                // Geometry properties
                case "Length3D":
                case "Length3DCenterToCenter":
                    return pipe.Length3DCenterToCenter;

                case "Length2D":
                case "Length2DCenterToCenter":
                    return pipe.Length2DCenterToCenter;

                case "InnerDiameter":
                case "InnerDiameterOrWidth":
                    return pipe.InnerDiameterOrWidth;

                case "OuterDiameter":
                case "OuterDiameterOrWidth":
                    return pipe.OuterDiameterOrWidth;

                case "Slope":
                    return pipe.Slope;

                case "StartPoint":
                    return pipe.StartPoint;

                case "EndPoint":
                    return pipe.EndPoint;

                case "StartOffset":
                    return pipe.StartOffset;

                case "EndOffset":
                    return pipe.EndOffset;

                case "StartInvert":
                    return pipe.StartPoint.Z - pipe.StartOffset;

                case "EndInvert":
                    return pipe.EndPoint.Z - pipe.EndOffset;

                // Part properties
                case "PartFamilyName":
                case "PartFamily":
                    return pipe.PartFamilyName;

                case "PartSizeName":
                case "PartSize":
                    return pipe.PartSizeName;

                case "PartDescription":
                    return pipe.PartDescription;

                // Network properties
                case "NetworkName":
                    return pipe.NetworkName;

                case "FlowDirection":
                    return pipe.FlowDirection.ToString();

                case "FlowDirectionMethod":
                    return pipe.FlowDirectionMethod.ToString();

                // Style and display
                case "StyleName":
                    return pipe.StyleName;

                case "Name":
                    return pipe.Name;

                case "Description":
                    return pipe.Description;

                case "Layer":
                    return pipe.Layer;

                case "Handle":
                    return pipe.Handle.ToString();

                // Material
                case "WallThickness":
                    return (pipe.OuterDiameterOrWidth - pipe.InnerDiameterOrWidth) / 2.0;

                default:
                    LogUnsupportedPath($"Pipe.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Pipe Network - Structure

        private object ResolveStructureProperty(Structure structure, string propertyName)
        {
            switch (propertyName)
            {
                // Elevation properties
                case "RimElevation":
                    return structure.RimElevation;

                case "SumpElevation":
                    return structure.SumpElevation;

                case "SumpDepth":
                    return structure.SumpDepth;

                // Geometry
                case "Position":
                    return structure.Position;

                case "Rotation":
                    return structure.Rotation;

                case "InnerDiameter":
                case "InnerDiameterOrWidth":
                    return structure.InnerDiameterOrWidth;

                case "InnerLength":
                    return structure.InnerLength;

                // Part properties
                case "PartFamilyName":
                case "PartFamily":
                    return structure.PartFamilyName;

                case "PartSizeName":
                case "PartSize":
                    return structure.PartSizeName;

                case "PartDescription":
                    return structure.PartDescription;

                // Network properties
                case "NetworkName":
                    return structure.NetworkName;

                // Style and display
                case "StyleName":
                    return structure.StyleName;

                case "Name":
                    return structure.Name;

                case "Description":
                    return structure.Description;

                case "Layer":
                    return structure.Layer;

                case "Handle":
                    return structure.Handle.ToString();

                // Calculated
                case "Depth":
                    return structure.RimElevation - structure.SumpElevation;

                default:
                    LogUnsupportedPath($"Structure.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Pressure Network - Pipe

        private object ResolvePressurePipeProperty(PressurePipe pressurePipe, string propertyName)
        {
            switch (propertyName)
            {
                // Geometry
                case "Length3D":
                case "Length3DCenterToCenter":
                    return pressurePipe.Length3DCenterToCenter;

                case "Length2D":
                case "Length2DCenterToCenter":
                    return pressurePipe.Length2DCenterToCenter;

                case "InnerDiameter":
                    return pressurePipe.InnerDiameter;

                case "OuterDiameter":
                    return pressurePipe.OuterDiameter;

                case "Slope":
                    return pressurePipe.Slope;

                case "StartPoint":
                    return pressurePipe.StartPoint;

                case "EndPoint":
                    return pressurePipe.EndPoint;

                // Part properties
                case "PartFamilyName":
                case "PartFamily":
                    return pressurePipe.PartFamilyName;

                case "PartSizeName":
                case "PartSize":
                    return pressurePipe.PartSizeName;

                case "PartDescription":
                    return pressurePipe.Description;

                // Network properties
                case "NetworkName":
                    return pressurePipe.NetworkName;

                // Style and display
                case "StyleName":
                    return pressurePipe.StyleName;

                case "Name":
                    return pressurePipe.Name;

                case "Description":
                    return pressurePipe.Description;

                case "Layer":
                    return pressurePipe.Layer;

                case "Handle":
                    return pressurePipe.Handle.ToString();

                // Material
                case "WallThickness":
                    return pressurePipe.WallThickness;

                default:
                    LogUnsupportedPath($"PressurePipe.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Pressure Network - Fitting

        private object ResolvePressureFittingProperty(PressureFitting fitting, string propertyName)
        {
            switch (propertyName)
            {
                // Geometry
                case "Position":
                    return fitting.Position;

                case "Rotation":
                    return fitting.Rotation;

                // Part properties
                case "PartFamilyName":
                case "PartFamily":
                    return fitting.PartFamilyName;

                case "PartSizeName":
                case "PartSize":
                    return fitting.PartSizeName;

                case "PartDescription":
                    return fitting.Description;

                // Network properties
                case "NetworkName":
                    return fitting.NetworkName;

                // Style and display
                case "StyleName":
                    return fitting.StyleName;

                case "Name":
                    return fitting.Name;

                case "Description":
                    return fitting.Description;

                case "Layer":
                    return fitting.Layer;

                case "Handle":
                    return fitting.Handle.ToString();

                default:
                    LogUnsupportedPath($"PressureFitting.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Pressure Network - Appurtenance

        private object ResolvePressureAppurtenanceProperty(PressureAppurtenance appurtenance, string propertyName)
        {
            switch (propertyName)
            {
                // Geometry
                case "Position":
                    return appurtenance.Position;

                case "Rotation":
                    return appurtenance.Rotation;

                // Part properties
                case "PartFamilyName":
                case "PartFamily":
                    return appurtenance.PartFamilyName;

                case "PartSizeName":
                case "PartSize":
                    return appurtenance.PartSizeName;

                case "PartDescription":
                    return appurtenance.Description;

                // Network properties
                case "NetworkName":
                    return appurtenance.NetworkName;

                // Style and display
                case "StyleName":
                    return appurtenance.StyleName;

                case "Name":
                    return appurtenance.Name;

                case "Description":
                    return appurtenance.Description;

                case "Layer":
                    return appurtenance.Layer;

                case "Handle":
                    return appurtenance.Handle.ToString();

                default:
                    LogUnsupportedPath($"PressureAppurtenance.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Corridor

        private object ResolveCorridorProperty(Corridor corridor, string propertyName)
        {
            switch (propertyName)
            {
                case "Name":
                    return corridor.Name;

                case "Description":
                    return corridor.Description;

                case "StyleName":
                    return corridor.StyleName;

                case "Layer":
                    return corridor.Layer;

                case "Handle":
                    return corridor.Handle.ToString();

                case "BaselineCount":
                    return corridor.Baselines?.Count ?? 0;

                case "AlignmentName":
                    try
                    {
                        if (corridor.Baselines != null && corridor.Baselines.Count > 0)
                            return corridor.Baselines[0]?.Name ?? "";
                    }
                    catch { }
                    return "";

                default:
                    LogUnsupportedPath($"Corridor.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Alignment

        private object ResolveAlignmentProperty(Alignment alignment, string propertyName)
        {
            switch (propertyName)
            {
                case "Name":
                    return alignment.Name;

                case "Description":
                    return alignment.Description;

                case "StyleName":
                    return alignment.StyleName;

                case "Layer":
                    return alignment.Layer;

                case "Handle":
                    return alignment.Handle.ToString();

                case "Length":
                    return alignment.Length;

                case "StartingStation":
                    return alignment.StartingStation;

                case "EndingStation":
                    return alignment.EndingStation;

                default:
                    LogUnsupportedPath($"Alignment.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Solid3d

        private object ResolveSolidProperty(Solid3d solid, string propertyName)
        {
            switch (propertyName)
            {
                case "Layer":
                    return solid.Layer;

                case "Handle":
                    return solid.Handle.ToString();

                case "Color":
                    return solid.Color.ToString();

                case "Material":
                    return solid.Material;

                case "Volume":
                    return solid.MassProperties?.Volume ?? 0.0;

                default:
                    LogUnsupportedPath($"Solid3d.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Generic Entity

        private object ResolveGenericProperty(AcadEntity entity, string propertyName)
        {
            switch (propertyName)
            {
                case "Layer":
                    return entity.Layer;

                case "Handle":
                    return entity.Handle.ToString();

                case "Color":
                    return entity.Color.ToString();

                case "Linetype":
                    return entity.Linetype;

                case "LineWeight":
                    return entity.LineWeight.ToString();

                default:
                    LogUnsupportedPath($"Entity.{propertyName}");
                    return null;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Log unsupported property path (once per path to avoid spam)
        /// </summary>
        private void LogUnsupportedPath(string path)
        {
            if (_loggedUnsupportedPaths.Add(path))
            {
                System.Diagnostics.Debug.WriteLine($"Unsupported path: {path}");
            }
        }

        /// <summary>
        /// Check if a property path is supported for a given entity type
        /// </summary>
        public bool IsPropertySupported(AcadEntity entity, string propertyPath)
        {
            object result = ResolveProperty(entity, propertyPath, null);
            return result != null;
        }

        /// <summary>
        /// Get formatted property value as string
        /// </summary>
        public string ResolvePropertyAsString(AcadEntity entity, string propertyPath, Transaction transaction, string format = null)
        {
            object value = ResolveProperty(entity, propertyPath, transaction);
            if (value == null)
                return "";

            // Handle special formatting
            if (!string.IsNullOrWhiteSpace(format))
            {
                if (value is double doubleValue)
                    return doubleValue.ToString(format);
                if (value is int intValue)
                    return intValue.ToString(format);
            }

            // Handle Point3d specially
            if (value is Point3d point)
                return $"{point.X:F3},{point.Y:F3},{point.Z:F3}";

            return value.ToString();
        }

        #endregion
    }
}
