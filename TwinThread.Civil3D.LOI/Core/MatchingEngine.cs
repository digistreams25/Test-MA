using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwinThread.Civil3D.LOI.Models;
using TwinThread.Civil3D.LOI.Utilities;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Evaluates matching rules to select appropriate SchemaElement
    /// </summary>
    public class MatchingEngine
    {
        private Dictionary<string, Regex> _regexCache = new Dictionary<string, Regex>();

        /// <summary>
        /// Select the first matching SchemaElement for the given entity context
        /// </summary>
        public SchemaElement SelectElement(EntityContext ctx, IEnumerable<SchemaElement> elements, Dictionary<string, string> existingXData)
        {
            // A) If tt.schemaElementId exists in XData, find by ID
            if (existingXData.TryGetValue(Constants.SchemaElementId, out string existingId) && !string.IsNullOrWhiteSpace(existingId))
            {
                var elementById = elements.FirstOrDefault(e => e.Id == existingId);
                if (elementById != null)
                    return elementById;
            }

            // B) Evaluate schemaElements in order, return first match
            foreach (var element in elements)
            {
                if (IsMatch(ctx, element))
                    return element;
            }

            return null; // No match found
        }

        /// <summary>
        /// Check if entity context matches the schemaElement rules
        /// </summary>
        private bool IsMatch(EntityContext ctx, SchemaElement element)
        {
            var c3d = element.C3d;
            if (c3d == null)
                return false; // No matching config

            // Check if object type is in targets (empty/null means all types)
            if (c3d.Targets != null && c3d.Targets.Count > 0)
            {
                if (!c3d.Targets.Contains(ctx.ObjectType))
                    return false;
            }

            // Evaluate matchAll (all must be true)
            if (c3d.MatchAll != null && c3d.MatchAll.Count > 0)
            {
                if (!c3d.MatchAll.All(rule => EvaluateRule(ctx, rule)))
                    return false;
            }

            // Evaluate matchAny (at least one must be true, or empty = true)
            if (c3d.MatchAny != null && c3d.MatchAny.Count > 0)
            {
                if (!c3d.MatchAny.Any(rule => EvaluateRule(ctx, rule)))
                    return false;
            }

            return true; // All conditions passed
        }

        /// <summary>
        /// Evaluate a single match rule
        /// </summary>
        private bool EvaluateRule(EntityContext ctx, MatchRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Kind))
                return false;

            try
            {
                switch (rule.Kind.ToLowerInvariant())
                {
                    // Layer matching
                    case "layerequals":
                        return string.Equals(ctx.Layer, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "layercontains":
                        return ctx.Layer != null && ctx.Layer.IndexOf(rule.Value, StringComparison.OrdinalIgnoreCase) >= 0;

                    case "layerregex":
                        return MatchesRegex(ctx.Layer, rule.Value);

                    // Style matching
                    case "styleequals":
                        return string.Equals(ctx.StyleName, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "styleregex":
                        return MatchesRegex(ctx.StyleName, rule.Value);

                    // Name matching
                    case "nameequals":
                        return string.Equals(ctx.Name, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "namecontains":
                        return ctx.Name != null && ctx.Name.IndexOf(rule.Value, StringComparison.OrdinalIgnoreCase) >= 0;

                    case "nameregex":
                        return MatchesRegex(ctx.Name, rule.Value);

                    // Pipe network matching
                    case "networkequals":
                        return string.Equals(ctx.NetworkName, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "partfamilyequals":
                        return string.Equals(ctx.PartFamily, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "partsizeequals":
                        return string.Equals(ctx.PartSize, rule.Value, StringComparison.OrdinalIgnoreCase);

                    // Corridor matching
                    case "alignmentequals":
                        return string.Equals(ctx.AlignmentName, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "alignmentregex":
                        return MatchesRegex(ctx.AlignmentName, rule.Value);

                    case "assemblyequals":
                        return string.Equals(ctx.AssemblyName, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "assemblyregex":
                        return MatchesRegex(ctx.AssemblyName, rule.Value);

                    case "regionequals":
                        return string.Equals(ctx.RegionName, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "regionregex":
                        return MatchesRegex(ctx.RegionName, rule.Value);

                    // Corridor solid shape code matching
                    case "shapecodeequals":
                        return string.Equals(ctx.ShapeCodeName, rule.Value, StringComparison.OrdinalIgnoreCase);

                    case "shapecodecontains":
                        return ctx.ShapeCodeName != null && ctx.ShapeCodeName.IndexOf(rule.Value, StringComparison.OrdinalIgnoreCase) >= 0;

                    case "shapecoderegex":
                        return MatchesRegex(ctx.ShapeCodeName, rule.Value);

                    default:
                        return false; // Unknown rule type
                }
            }
            catch
            {
                // Invalid regex or other error - skip this rule
                return false;
            }
        }

        /// <summary>
        /// Check if text matches regex pattern (cached)
        /// </summary>
        private bool MatchesRegex(string text, string pattern)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(pattern))
                return false;

            try
            {
                if (!_regexCache.TryGetValue(pattern, out Regex regex))
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    _regexCache[pattern] = regex;
                }

                return regex.IsMatch(text);
            }
            catch
            {
                return false; // Invalid regex
            }
        }
    }
}
