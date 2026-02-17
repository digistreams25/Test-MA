using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using TwinThread.C3D.Automation.Config;

namespace TwinThread.C3D.Automation.Geometry
{
    /// <summary>
    /// Finds intersections between a perpendicular section line and target
    /// polylines/featurelines on specified layers.
    /// </summary>
    public sealed class TargetFinder
    {
        private readonly Database _db;
        private readonly Transaction _tr;

        public TargetFinder(Database db, Transaction tr)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _tr = tr ?? throw new ArgumentNullException(nameof(tr));
        }

        /// <summary>
        /// Scans all entities on <paramref name="layerName"/> and finds the
        /// nearest intersection with the section line defined by
        /// <paramref name="sectionStart"/> → <paramref name="sectionEnd"/>.
        /// Returns the signed offset from CL (<paramref name="centerPoint"/>).
        /// Negative = left, positive = right.
        /// Returns null if no intersection found.
        /// </summary>
        public double? FindNearestOffset(
            string layerName,
            Point2d centerPoint,
            Point2d sectionStart,   // left end
            Point2d sectionEnd,     // right end
            double tangentAngle,
            double minOffset)
        {
            var sectionLine = new LineSegment2d(sectionStart, sectionEnd);
            double? bestOffset = null;
            double bestDist = double.MaxValue;

            var bt = (BlockTable)_tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)_tr.GetObject(
                bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId oid in ms)
            {
                var ent = _tr.GetObject(oid, OpenMode.ForRead) as Entity;
                if (ent == null || !IsLayerMatch(ent.Layer, layerName))
                    continue;

                var points = ExtractIntersections(ent, sectionLine);
                foreach (var pt in points)
                {
                    double offset = ComputeSignedOffset(centerPoint, pt, tangentAngle);
                    double absDist = Math.Abs(offset);
                    if (absDist < minOffset)
                        continue;
                    if (absDist < bestDist)
                    {
                        bestDist = absDist;
                        bestOffset = offset;
                    }
                }
            }

            return bestOffset;
        }

        /// <summary>
        /// Finds all target offsets on a given layer that intersect the section line.
        /// Returns offsets sorted by absolute value (nearest to CL first).
        /// </summary>
        public List<double> FindAllOffsets(
            string layerName,
            Point2d centerPoint,
            Point2d sectionStart,
            Point2d sectionEnd,
            double tangentAngle,
            double minOffset)
        {
            var sectionLine = new LineSegment2d(sectionStart, sectionEnd);
            var offsets = new List<double>();

            var bt = (BlockTable)_tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)_tr.GetObject(
                bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId oid in ms)
            {
                var ent = _tr.GetObject(oid, OpenMode.ForRead) as Entity;
                if (ent == null || !IsLayerMatch(ent.Layer, layerName))
                    continue;

                var points = ExtractIntersections(ent, sectionLine);
                foreach (var pt in points)
                {
                    double offset = ComputeSignedOffset(centerPoint, pt, tangentAngle);
                    if (Math.Abs(offset) >= minOffset)
                        offsets.Add(offset);
                }
            }

            offsets.Sort((a, b) => Math.Abs(a).CompareTo(Math.Abs(b)));
            return offsets;
        }

        /// <summary>
        /// Extracts 2D intersection points between an entity and a section line.
        /// Supports Polyline, Polyline3d, Polyline2d, Line, and FeatureLine.
        /// </summary>
        private List<Point2d> ExtractIntersections(Entity ent, LineSegment2d sectionLine)
        {
            var results = new List<Point2d>();

            // Get the curve from the entity
            Curve curve = ent as Curve;
            if (curve == null)
                return results;

            // Sample the curve and find segment intersections
            try
            {
                // Use the entity's geometric extents to decide sampling
                var segments = GetSegments2d(curve);
                foreach (var seg in segments)
                {
                    var intersection = Intersect2d(seg, sectionLine);
                    if (intersection.HasValue)
                        results.Add(intersection.Value);
                }
            }
            catch
            {
                // Entity geometry not accessible; skip
            }

            return results;
        }

        /// <summary>
        /// Decomposes a Curve into 2D line segments by walking its vertices.
        /// </summary>
        private List<LineSegment2d> GetSegments2d(Curve curve)
        {
            var segments = new List<LineSegment2d>();

            try
            {
                double start = curve.StartParam;
                double end = curve.EndParam;

                // For polylines, walk vertex by vertex
                if (curve is Polyline pline)
                {
                    for (int i = 0; i < pline.NumberOfVertices - 1; i++)
                    {
                        var p1 = pline.GetPoint2dAt(i);
                        var p2 = pline.GetPoint2dAt(i + 1);
                        segments.Add(new LineSegment2d(p1, p2));
                    }
                    if (pline.Closed && pline.NumberOfVertices > 2)
                    {
                        var pFirst = pline.GetPoint2dAt(0);
                        var pLast = pline.GetPoint2dAt(pline.NumberOfVertices - 1);
                        segments.Add(new LineSegment2d(pLast, pFirst));
                    }
                    return segments;
                }

                // Generic: sample at integer parameter values
                int steps = Math.Max((int)(end - start), 1);
                // Cap to avoid huge loops
                steps = Math.Min(steps, 10000);
                for (int i = 0; i < steps; i++)
                {
                    double t1 = start + i;
                    double t2 = start + i + 1;
                    if (t2 > end) t2 = end;
                    var pt1 = curve.GetPointAtParameter(t1);
                    var pt2 = curve.GetPointAtParameter(t2);
                    segments.Add(new LineSegment2d(
                        new Point2d(pt1.X, pt1.Y),
                        new Point2d(pt2.X, pt2.Y)));
                }
            }
            catch
            {
                // Geometry extraction failed; return empty
            }

            return segments;
        }

        /// <summary>
        /// 2D line-segment / line-segment intersection.
        /// Returns intersection point or null.
        /// </summary>
        private Point2d? Intersect2d(LineSegment2d a, LineSegment2d b)
        {
            double x1 = a.StartPoint.X, y1 = a.StartPoint.Y;
            double x2 = a.EndPoint.X, y2 = a.EndPoint.Y;
            double x3 = b.StartPoint.X, y3 = b.StartPoint.Y;
            double x4 = b.EndPoint.X, y4 = b.EndPoint.Y;

            double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denom) < 1e-12)
                return null;

            double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
            double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

            if (t >= -1e-9 && t <= 1.0 + 1e-9 && u >= -1e-9 && u <= 1.0 + 1e-9)
            {
                double ix = x1 + t * (x2 - x1);
                double iy = y1 + t * (y2 - y1);
                return new Point2d(ix, iy);
            }

            return null;
        }

        /// <summary>
        /// Computes signed offset: positive = right of CL, negative = left.
        /// Uses the cross product of tangent direction and the vector from
        /// CL to the intersection point.
        /// </summary>
        private double ComputeSignedOffset(Point2d center, Point2d hit, double tangentAngle)
        {
            double dx = hit.X - center.X;
            double dy = hit.Y - center.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Cross product of tangent direction and offset vector
            double tx = Math.Cos(tangentAngle);
            double ty = Math.Sin(tangentAngle);
            double cross = tx * dy - ty * dx;

            // Civil 3D convention: right is positive offset
            // cross > 0 → point is to the left → negative offset
            return cross > 0 ? -dist : dist;
        }

        /// <summary>
        /// Layer name match with simple wildcard support ('*' at start/end).
        /// </summary>
        private bool IsLayerMatch(string entityLayer, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            if (pattern == "*")
                return true;

            if (pattern.StartsWith("*") && pattern.EndsWith("*"))
            {
                string inner = pattern.Substring(1, pattern.Length - 2);
                return entityLayer.IndexOf(inner, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (pattern.EndsWith("*"))
            {
                string prefix = pattern.Substring(0, pattern.Length - 1);
                return entityLayer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            if (pattern.StartsWith("*"))
            {
                string suffix = pattern.Substring(1);
                return entityLayer.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(entityLayer, pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
