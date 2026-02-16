using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwinThread.Civil3D.LOI.Models
{
    /// <summary>
    /// Root schema model from TwinThread
    /// </summary>
    public class Schema
    {
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; }

        [JsonPropertyName("milestone")]
        public Milestone Milestone { get; set; }

        [JsonPropertyName("schemaElements")]
        public List<SchemaElement> SchemaElements { get; set; }
    }

    public class Milestone
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class SchemaElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("schemaElementParameters")]
        public List<SchemaElementParameter> SchemaElementParameters { get; set; }

        [JsonPropertyName("c3d")]
        public C3dMatchingConfig C3d { get; set; }
    }

    public class SchemaElementParameter
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("storageType")]
        public string StorageType { get; set; }

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }

        [JsonPropertyName("scopedDefinition")]
        public ScopedDefinition ScopedDefinition { get; set; }

        /// <summary>
        /// Default value assignment mode: "Global", "Manual", "PerObject", "Rule"
        /// </summary>
        [JsonPropertyName("valueMode")]
        public string ValueMode { get; set; }

        /// <summary>
        /// Default value (for Global mode or as fallback)
        /// </summary>
        [JsonPropertyName("defaultValue")]
        public string DefaultValue { get; set; }

        /// <summary>
        /// Mapping rule for automatic value assignment (e.g., "layer", "name", "handle")
        /// </summary>
        [JsonPropertyName("mappingRule")]
        public string MappingRule { get; set; }
    }

    public class ScopedDefinition
    {
        [JsonPropertyName("parameterDefinitionSource")]
        public ParameterDefinitionSource ParameterDefinitionSource { get; set; }
    }

    public class ParameterDefinitionSource
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class C3dMatchingConfig
    {
        [JsonPropertyName("targets")]
        public List<string> Targets { get; set; }

        [JsonPropertyName("matchAny")]
        public List<MatchRule> MatchAny { get; set; }

        [JsonPropertyName("matchAll")]
        public List<MatchRule> MatchAll { get; set; }
    }

    public class MatchRule
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
