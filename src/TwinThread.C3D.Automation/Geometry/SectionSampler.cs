using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;
using TwinThread.C3D.Automation.Config;

namespace TwinThread.C3D.Automation.Geometry
{
    /// <summary>
    /// Walks along an alignment at a fixed interval and produces
    /// <see cref="SectionSample"/> objects for each station.
    /// </summary>
    public sealed class SectionSampler
    {
        private readonly Alignment _alignment;
        private readonly double _interval;

        public SectionSampler(Alignment alignment, double interval)
        {
            _alignment = alignment ?? throw new ArgumentNullException(nameof(alignment));
            _interval = interval > 0 ? interval : 10.0;
        }

        /// <summary>
        /// Returns station values from the alignment start to end at the
        /// configured interval.  Always includes the start and end stations.
        /// </summary>
        public IEnumerable<double> EnumerateStations()
        {
            double start = _alignment.StartingStation;
            double end = _alignment.EndingStation;

            yield return start;

            double current = start + _interval;
            while (current < end - 0.001)
            {
                yield return Math.Round(current, 4);
                current += _interval;
            }

            if (Math.Abs(current - end) > 0.001)
                yield return end;
        }

        /// <summary>
        /// Computes the plan-view point and tangent direction at a given station.
        /// </summary>
        public (Point2d Point, double TangentAngle) GetStationInfo(double station)
        {
            double easting = 0, northing = 0;
            _alignment.PointLocation(station, 0, ref easting, ref northing);

            // Compute tangent by finite difference (±0.01 m)
            double e1 = 0, n1 = 0, e2 = 0, n2 = 0;
            double dt = 0.01;
            double s1 = Math.Max(_alignment.StartingStation, station - dt);
            double s2 = Math.Min(_alignment.EndingStation, station + dt);
            _alignment.PointLocation(s1, 0, ref e1, ref n1);
            _alignment.PointLocation(s2, 0, ref e2, ref n2);

            double angle = Math.Atan2(n2 - n1, e2 - e1);
            return (new Point2d(easting, northing), angle);
        }

        /// <summary>
        /// Builds a perpendicular section line at the given station.
        /// Returns (startPoint, endPoint) where start is maxLeft offset
        /// and end is maxRight offset.
        /// Left = tangent + π/2, Right = tangent − π/2 (Civil 3D convention).
        /// </summary>
        public (Point2d Left, Point2d Right) GetSectionEndpoints(
            double station, double maxLeft, double maxRight)
        {
            var (pt, tangent) = GetStationInfo(station);

            // Left is perpendicular left (CCW from tangent)
            double leftAngle = tangent + Math.PI / 2.0;
            double rightAngle = tangent - Math.PI / 2.0;

            var leftPt = new Point2d(
                pt.X + maxLeft * Math.Cos(leftAngle),
                pt.Y + maxLeft * Math.Sin(leftAngle));

            var rightPt = new Point2d(
                pt.X + maxRight * Math.Cos(rightAngle),
                pt.Y + maxRight * Math.Sin(rightAngle));

            return (leftPt, rightPt);
        }
    }
}
