using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TwinThread.C3D.Automation.Geometry;

namespace TwinThread.C3D.Automation.Signatures
{
    /// <summary>
    /// Builds a canonical string signature from detected left/right components.
    /// Signatures are deterministic and comparable: identical component sequences
    /// produce identical strings, enabling assembly reuse.
    ///
    /// Format: "L:Type(W)|Type(W)|...;R:Type(W)|Type(W)|..."
    /// where W is the width rounded to 2 decimal places (omitted for zero-width
    /// components like Curb).
    /// </summary>
    public static class SignatureBuilder
    {
        /// <summary>
        /// Builds a canonical signature string for a section sample.
        /// Components are ordered by offset from CL (nearest first).
        /// </summary>
        public static string Build(SectionSample sample)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));

            string left = BuildSide("L", sample.LeftComponents);
            string right = BuildSide("R", sample.RightComponents);

            return $"{left};{right}";
        }

        /// <summary>
        /// Builds a canonical signature string from explicit component lists.
        /// </summary>
        public static string Build(
            List<DetectedComponent> leftComponents,
            List<DetectedComponent> rightComponents)
        {
            string left = BuildSide("L", leftComponents ?? new List<DetectedComponent>());
            string right = BuildSide("R", rightComponents ?? new List<DetectedComponent>());
            return $"{left};{right}";
        }

        /// <summary>
        /// Computes a short hash (8 hex chars) of the signature for use in
        /// assembly naming.
        /// </summary>
        public static string Hash(string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return "00000000";

            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(signature));
                // Take first 4 bytes → 8 hex chars
                return BitConverter.ToString(bytes, 0, 4).Replace("-", "").ToUpperInvariant();
            }
        }

        /// <summary>
        /// Creates a short human-readable label from the signature.
        /// E.g., "L2R3" meaning 2 left components, 3 right components.
        /// </summary>
        public static string ShortLabel(string signature)
        {
            if (string.IsNullOrEmpty(signature))
                return "EMPTY";

            var parts = signature.Split(';');
            int leftCount = CountComponents(parts.Length > 0 ? parts[0] : "");
            int rightCount = CountComponents(parts.Length > 1 ? parts[1] : "");

            return $"L{leftCount}R{rightCount}";
        }

        /// <summary>
        /// Generates the canonical assembly name for a given signature.
        /// Pattern: "TT_ASSM_{hash}_{shortLabel}"
        /// </summary>
        public static string AssemblyName(string signature)
        {
            return $"TT_ASSM_{Hash(signature)}_{ShortLabel(signature)}";
        }

        private static string BuildSide(string prefix, List<DetectedComponent> components)
        {
            if (components == null || components.Count == 0)
                return $"{prefix}:NONE";

            var parts = components.Select(c =>
            {
                if (c.Width.HasValue && c.Width.Value > 0.001)
                    return $"{c.ComponentType}({c.Width.Value:F2})";
                return c.ComponentType;
            });

            return $"{prefix}:{string.Join("|", parts)}";
        }

        private static int CountComponents(string sideStr)
        {
            if (string.IsNullOrEmpty(sideStr))
                return 0;

            int colonIdx = sideStr.IndexOf(':');
            if (colonIdx < 0)
                return 0;

            string after = sideStr.Substring(colonIdx + 1);
            if (after == "NONE")
                return 0;

            return after.Split('|').Length;
        }
    }
}
