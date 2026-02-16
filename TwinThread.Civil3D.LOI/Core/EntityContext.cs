using Autodesk.AutoCAD.DatabaseServices;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Contains metadata extracted from a Civil 3D object for matching
    /// </summary>
    public class EntityContext
    {
        // Object identification
        public ObjectId ObjectId { get; set; }
        public Handle Handle { get; set; }
        public string ObjectType { get; set; } // "corridor", "pipe", "structure", "solid"

        // Common properties
        public string Layer { get; set; }
        public string Name { get; set; }
        public string StyleName { get; set; }

        // Pipe/Structure specific
        public string NetworkName { get; set; }
        public string PartFamily { get; set; }
        public string PartSize { get; set; }

        // Corridor specific
        public string AlignmentName { get; set; }
        public string AssemblyName { get; set; }
        public string RegionName { get; set; }
        public string ShapeCodeName { get; set; } // For corridor solids

        // Status
        public bool IsLocked { get; set; }
        public string ErrorMessage { get; set; }
    }
}
