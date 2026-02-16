using System.Collections.Generic;

namespace TwinThread.Civil3D.LOI.Utilities
{
    /// <summary>
    /// Constants used throughout the plugin
    /// </summary>
    public static class Constants
    {
        // XData RegApp name
        public const string RegAppName = "TWINTHREAD";

        // XData field names (tt.* prefix)
        public const string ProjectId = "tt.projectId";
        public const string ProjectName = "tt.projectName";
        public const string MilestoneId = "tt.milestoneId";
        public const string MilestoneName = "tt.milestoneName";
        public const string SchemaElementId = "tt.schemaElementId";
        public const string SchemaElementName = "tt.schemaElementName";
        public const string StorageMode = "tt.storageMode";
        public const string PropertySetName = "tt.propertySetName";
        public const string UpdatedAtUtc = "tt.updatedAtUtc";
        public const string LoiStatus = "tt.loi.status";
        public const string LoiMissingFields = "tt.loi.missingFields";

        // LOI field names (exact as per schema)
        public static readonly HashSet<string> LoiFieldNames = new HashSet<string>
        {
            "PDS_Code",
            "Company_Name",
            "Design_Stage",
            "Design_Status",
            "Material",
            "Suitability_Code"
        };

        // Status values
        public const string StatusPass = "PASS";
        public const string StatusWarn = "WARN";

        // Special normalization
        public static readonly Dictionary<string, string> FieldNameNormalization = new Dictionary<string, string>
        {
            { "Suitability Code", "Suitability_Code" }
        };

        // Object type identifiers
        public const string ObjectTypeCorridor = "corridor";
        public const string ObjectTypePipe = "pipe";
        public const string ObjectTypeStructure = "structure";
        public const string ObjectTypeSolid = "solid";
    }
}
