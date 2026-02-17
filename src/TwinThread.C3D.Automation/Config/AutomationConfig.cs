using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwinThread.C3D.Automation.Config
{
    /// <summary>
    /// Top-level configuration for the Auto Corridor Assembly Builder.
    /// Serialised from / to JSON.
    /// </summary>
    public sealed class AutomationConfig
    {
        [JsonProperty("alignmentName", Required = Required.Always)]
        public string AlignmentName { get; set; }

        /// <summary>
        /// Profile to use for the corridor baseline.
        /// Null/empty = use the first profile found on the alignment.
        /// </summary>
        [JsonProperty("baselineProfileName")]
        public string BaselineProfileName { get; set; }

        /// <summary>
        /// Name of the corridor to create or update.
        /// Default pattern: "TT_CORR_{AlignmentName}".
        /// </summary>
        [JsonProperty("corridorName")]
        public string CorridorName { get; set; }

        /// <summary>
        /// Station sampling interval in meters.
        /// </summary>
        [JsonProperty("sampleInterval")]
        public double SampleInterval { get; set; } = 10.0;

        /// <summary>
        /// Maximum search distance to the left of the alignment (meters).
        /// </summary>
        [JsonProperty("searchMaxOffsetLeft")]
        public double SearchMaxOffsetLeft { get; set; } = 30.0;

        /// <summary>
        /// Maximum search distance to the right of the alignment (meters).
        /// </summary>
        [JsonProperty("searchMaxOffsetRight")]
        public double SearchMaxOffsetRight { get; set; } = 30.0;

        /// <summary>
        /// Ordered list of layer detection rules.
        /// </summary>
        [JsonProperty("rules", Required = Required.Always)]
        public List<LayerRule> Rules { get; set; } = new List<LayerRule>();

        /// <summary>
        /// When true, no assemblies or corridor regions are created/modified.
        /// The tool only computes signatures, planned regions, and returns a
        /// report of what *would* change.
        /// </summary>
        [JsonProperty("dryRun")]
        public bool DryRun { get; set; } = false;

        /// <summary>
        /// Applies defaults where values are null/empty.
        /// Call after deserialisation.
        /// </summary>
        public void ApplyDefaults()
        {
            if (string.IsNullOrWhiteSpace(CorridorName))
                CorridorName = $"TT_CORR_{AlignmentName}";
        }
    }
}
