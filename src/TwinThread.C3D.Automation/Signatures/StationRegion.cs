namespace TwinThread.C3D.Automation.Signatures
{
    /// <summary>
    /// Represents a contiguous station range sharing the same section signature.
    /// </summary>
    public sealed class StationRegion
    {
        /// <summary>
        /// Canonical signature for this region.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Assembly name derived from the signature.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Start station of this region.
        /// </summary>
        public double StartStation { get; set; }

        /// <summary>
        /// End station of this region.
        /// </summary>
        public double EndStation { get; set; }

        /// <summary>
        /// Number of sample stations contained in this region.
        /// </summary>
        public int SampleCount { get; set; }
    }
}
