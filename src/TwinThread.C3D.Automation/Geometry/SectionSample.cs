using System.Collections.Generic;

namespace TwinThread.C3D.Automation.Geometry
{
    /// <summary>
    /// Result of sampling one station along the alignment.
    /// Contains the detected components on each side.
    /// </summary>
    public sealed class SectionSample
    {
        public double Station { get; set; }

        /// <summary>
        /// X,Y of the alignment at this station (plan view).
        /// </summary>
        public double CenterX { get; set; }
        public double CenterY { get; set; }

        /// <summary>
        /// Direction of the alignment tangent at this station (radians, CCW from east).
        /// </summary>
        public double TangentAngle { get; set; }

        /// <summary>
        /// Detected components on the left side, ordered by offset from CL (nearest first).
        /// </summary>
        public List<DetectedComponent> LeftComponents { get; set; } = new List<DetectedComponent>();

        /// <summary>
        /// Detected components on the right side, ordered by offset from CL (nearest first).
        /// </summary>
        public List<DetectedComponent> RightComponents { get; set; } = new List<DetectedComponent>();
    }

    /// <summary>
    /// A single component detected at a section station.
    /// </summary>
    public sealed class DetectedComponent
    {
        /// <summary>
        /// The rule that matched this target.
        /// </summary>
        public string RuleName { get; set; }

        /// <summary>
        /// Component type name from the rule.
        /// </summary>
        public string ComponentType { get; set; }

        /// <summary>
        /// Signed offset from CL (negative = left, positive = right).
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// Computed width of this component zone.  May be null if width
        /// cannot be determined and the component has no defaultWidth.
        /// </summary>
        public double? Width { get; set; }

        /// <summary>
        /// Cross-slope in percent.
        /// </summary>
        public double Slope { get; set; }
    }
}
