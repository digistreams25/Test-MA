using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TwinThread.C3D.Automation.Config
{
    /// <summary>
    /// Specifies which layer to search for a corridor component target,
    /// how to detect it, and the component it maps to.
    /// Rules are evaluated in order; first match at a given station wins
    /// for offset calculations.
    /// </summary>
    public sealed class LayerRule
    {
        /// <summary>
        /// Human-readable name of this rule (e.g., "EOC", "ParkingLimit").
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Which side(s) of the alignment this rule applies to.
        /// </summary>
        [JsonProperty("side")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RuleSide Side { get; set; } = RuleSide.Both;

        /// <summary>
        /// AutoCAD layer name to search for target polylines/featurelines.
        /// Supports wildcards (e.g., "C3D_EOC*").
        /// </summary>
        [JsonProperty("layer", Required = Required.Always)]
        public string Layer { get; set; }

        /// <summary>
        /// The corridor component this target maps to.
        /// </summary>
        [JsonProperty("component", Required = Required.Always)]
        public ComponentSpec Component { get; set; }

        /// <summary>
        /// Optional: minimum offset from CL to consider a hit (meters).
        /// Default: 0.0 (at CL itself is valid).
        /// </summary>
        [JsonProperty("minOffset")]
        public double MinOffset { get; set; } = 0.0;
    }

    public enum RuleSide
    {
        Both,
        Left,
        Right
    }
}
