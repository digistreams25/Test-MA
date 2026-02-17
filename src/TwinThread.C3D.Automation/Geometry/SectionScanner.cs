using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using TwinThread.C3D.Automation.Config;

namespace TwinThread.C3D.Automation.Geometry
{
    /// <summary>
    /// Orchestrates section sampling: walks the alignment at the configured
    /// interval, queries <see cref="TargetFinder"/> for each rule, and
    /// builds <see cref="SectionSample"/> results with left/right components.
    /// </summary>
    public sealed class SectionScanner
    {
        private readonly SectionSampler _sampler;
        private readonly TargetFinder _finder;
        private readonly AutomationConfig _config;
        private readonly List<string> _warnings = new List<string>();
        private readonly List<MissingTarget> _missingTargets = new List<MissingTarget>();

        public IReadOnlyList<string> Warnings => _warnings;
        public IReadOnlyList<MissingTarget> MissingTargets => _missingTargets;

        public SectionScanner(
            Alignment alignment,
            Database db,
            Transaction tr,
            AutomationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sampler = new SectionSampler(alignment, config.SampleInterval);
            _finder = new TargetFinder(db, tr);
        }

        /// <summary>
        /// Scans every station and returns an ordered list of section samples.
        /// </summary>
        public List<SectionSample> Scan()
        {
            var results = new List<SectionSample>();

            foreach (double station in _sampler.EnumerateStations())
            {
                var sample = ScanStation(station);
                results.Add(sample);
            }

            return results;
        }

        private SectionSample ScanStation(double station)
        {
            var (pt, tangent) = _sampler.GetStationInfo(station);
            var (leftEnd, rightEnd) = _sampler.GetSectionEndpoints(
                station, _config.SearchMaxOffsetLeft, _config.SearchMaxOffsetRight);

            var sample = new SectionSample
            {
                Station = station,
                CenterX = pt.X,
                CenterY = pt.Y,
                TangentAngle = tangent
            };

            // Collect all offsets per rule per side
            var leftHits = new List<DetectedComponent>();
            var rightHits = new List<DetectedComponent>();

            foreach (var rule in _config.Rules)
            {
                bool checkLeft = rule.Side == RuleSide.Both || rule.Side == RuleSide.Left;
                bool checkRight = rule.Side == RuleSide.Both || rule.Side == RuleSide.Right;

                // Find all offsets for this rule's layer on the section line
                var allOffsets = _finder.FindAllOffsets(
                    rule.Layer, pt, leftEnd, rightEnd, tangent, rule.MinOffset);

                bool foundLeft = false;
                bool foundRight = false;

                foreach (double offset in allOffsets)
                {
                    if (offset < 0 && checkLeft && !foundLeft)
                    {
                        leftHits.Add(new DetectedComponent
                        {
                            RuleName = rule.Name,
                            ComponentType = rule.Component.Type,
                            Offset = offset,
                            Width = rule.Component.DefaultWidth,
                            Slope = rule.Component.DefaultSlope
                        });
                        foundLeft = true;
                    }
                    else if (offset >= 0 && checkRight && !foundRight)
                    {
                        rightHits.Add(new DetectedComponent
                        {
                            RuleName = rule.Name,
                            ComponentType = rule.Component.Type,
                            Offset = offset,
                            Width = rule.Component.DefaultWidth,
                            Slope = rule.Component.DefaultSlope
                        });
                        foundRight = true;
                    }
                }

                // Log missing targets
                if (checkLeft && !foundLeft)
                {
                    _missingTargets.Add(new MissingTarget
                    {
                        RuleName = rule.Name,
                        Station = station,
                        Side = "Left"
                    });
                }
                if (checkRight && !foundRight)
                {
                    _missingTargets.Add(new MissingTarget
                    {
                        RuleName = rule.Name,
                        Station = station,
                        Side = "Right"
                    });
                }
            }

            // Sort by absolute offset (nearest to CL first)
            leftHits.Sort((a, b) => Math.Abs(a.Offset).CompareTo(Math.Abs(b.Offset)));
            rightHits.Sort((a, b) => Math.Abs(a.Offset).CompareTo(Math.Abs(b.Offset)));

            // Compute widths from offsets where possible
            ComputeWidthsFromOffsets(leftHits);
            ComputeWidthsFromOffsets(rightHits);

            sample.LeftComponents = leftHits;
            sample.RightComponents = rightHits;

            return sample;
        }

        /// <summary>
        /// For consecutive components, compute width as the difference
        /// between their offsets.  Falls back to defaultWidth if only
        /// one component exists.
        /// </summary>
        private void ComputeWidthsFromOffsets(List<DetectedComponent> components)
        {
            for (int i = 0; i < components.Count; i++)
            {
                double innerEdge = i == 0
                    ? 0.0
                    : Math.Abs(components[i - 1].Offset);
                double outerEdge = Math.Abs(components[i].Offset);
                double computed = outerEdge - innerEdge;

                if (computed > 0.01)
                    components[i].Width = Math.Round(computed, 2);
                // else keep the defaultWidth from the rule
            }
        }
    }

    /// <summary>
    /// Represents a rule target that was not found at a particular station/side.
    /// </summary>
    public sealed class MissingTarget
    {
        public string RuleName { get; set; }
        public double Station { get; set; }
        public string Side { get; set; }
    }
}
