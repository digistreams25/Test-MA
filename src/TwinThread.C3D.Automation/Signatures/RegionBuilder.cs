using System;
using System.Collections.Generic;
using TwinThread.C3D.Automation.Geometry;

namespace TwinThread.C3D.Automation.Signatures
{
    /// <summary>
    /// Walks a list of <see cref="SectionSample"/> results, computes signatures,
    /// and groups consecutive stations with the same signature into
    /// <see cref="StationRegion"/> objects.
    /// </summary>
    public static class RegionBuilder
    {
        /// <summary>
        /// Builds regions from ordered section samples.
        /// A new region starts whenever the signature changes.
        /// </summary>
        /// <param name="samples">Ordered by station (ascending).</param>
        /// <returns>Ordered list of contiguous regions.</returns>
        public static List<StationRegion> Build(IReadOnlyList<SectionSample> samples)
        {
            if (samples == null || samples.Count == 0)
                return new List<StationRegion>();

            var regions = new List<StationRegion>();
            StationRegion current = null;

            foreach (var sample in samples)
            {
                string sig = SignatureBuilder.Build(sample);

                if (current == null || current.Signature != sig)
                {
                    // Close previous region at (station - epsilon)
                    if (current != null)
                        current.EndStation = sample.Station - 0.001;

                    current = new StationRegion
                    {
                        Signature = sig,
                        AssemblyName = SignatureBuilder.AssemblyName(sig),
                        StartStation = sample.Station,
                        EndStation = sample.Station,
                        SampleCount = 1
                    };
                    regions.Add(current);
                }
                else
                {
                    current.EndStation = sample.Station;
                    current.SampleCount++;
                }
            }

            return regions;
        }

        /// <summary>
        /// Returns the set of unique signatures found across all regions.
        /// </summary>
        public static HashSet<string> UniqueSignatures(IReadOnlyList<StationRegion> regions)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in regions)
                set.Add(r.Signature);
            return set;
        }
    }
}
