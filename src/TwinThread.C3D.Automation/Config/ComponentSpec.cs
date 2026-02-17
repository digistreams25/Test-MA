using Newtonsoft.Json;

namespace TwinThread.C3D.Automation.Config
{
    /// <summary>
    /// Describes a corridor component type and its default geometry.
    /// </summary>
    public sealed class ComponentSpec
    {
        /// <summary>
        /// Component type name: Lane, Parking, Curb, Sidewalk, Verge, Shoulder, etc.
        /// </summary>
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; }

        /// <summary>
        /// Default width in meters.  Used when actual width cannot be computed
        /// from adjacent target offsets.  Null means the component has no width
        /// (e.g., a curb is a point element).
        /// </summary>
        [JsonProperty("defaultWidth")]
        public double? DefaultWidth { get; set; }

        /// <summary>
        /// Default cross-slope in percent (positive = away from CL).
        /// Used when no slope can be inferred.  Default: 2.0%.
        /// </summary>
        [JsonProperty("defaultSlope")]
        public double DefaultSlope { get; set; } = 2.0;
    }
}
