using System;
using System.Collections.Generic;
using System.Linq;
using TwinThread.Civil3D.LOI.Models;
using TwinThread.Civil3D.LOI.Utilities;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Validates LOI requirements and generates compliance status
    /// </summary>
    public class LOIValidator
    {
        /// <summary>
        /// Apply schema element to XData dictionary (fill-missing logic)
        /// </summary>
        public void ApplySchemaElement(
            Dictionary<string, string> xdata,
            SchemaElement element,
            Schema schema)
        {
            // Update metadata fields (always write these)
            xdata[Constants.ProjectId] = schema.ProjectId ?? "";
            xdata[Constants.ProjectName] = schema.ProjectName ?? "";
            xdata[Constants.MilestoneId] = schema.Milestone?.Id ?? "";
            xdata[Constants.MilestoneName] = schema.Milestone?.Name ?? "";
            xdata[Constants.SchemaElementId] = element.Id ?? "";
            xdata[Constants.SchemaElementName] = element.Name ?? "";
            xdata[Constants.UpdatedAtUtc] = DateTime.UtcNow.ToString("o");

            // Apply LOI parameters (fill-missing only)
            if (element.SchemaElementParameters != null)
            {
                foreach (var param in element.SchemaElementParameters)
                {
                    string paramName = NormalizeParameterName(param.Name);

                    // Only enforce the 6 LOI fields
                    if (!Constants.LoiFieldNames.Contains(paramName))
                        continue;

                    // Fill-missing: only write if field is empty or doesn't exist
                    if (!xdata.ContainsKey(paramName) || string.IsNullOrWhiteSpace(xdata[paramName]))
                    {
                        string defaultValue = param.ScopedDefinition?.ParameterDefinitionSource?.Value ?? "";
                        xdata[paramName] = defaultValue;
                    }
                }
            }

            // Validate and set LOI status
            ValidateAndSetStatus(xdata, element);
        }

        /// <summary>
        /// Validate LOI fields and set status (PASS/WARN)
        /// </summary>
        private void ValidateAndSetStatus(Dictionary<string, string> xdata, SchemaElement element)
        {
            List<string> missingFields = new List<string>();

            if (element.SchemaElementParameters != null)
            {
                foreach (var param in element.SchemaElementParameters)
                {
                    string paramName = NormalizeParameterName(param.Name);

                    // Only check the 6 LOI fields
                    if (!Constants.LoiFieldNames.Contains(paramName))
                        continue;

                    // Check if required and missing
                    if (param.IsRequired)
                    {
                        if (!xdata.TryGetValue(paramName, out string value) || string.IsNullOrWhiteSpace(value))
                        {
                            missingFields.Add(paramName);
                        }
                    }
                }
            }

            // Set status
            if (missingFields.Count == 0)
            {
                xdata[Constants.LoiStatus] = Constants.StatusPass;
                xdata[Constants.LoiMissingFields] = "";
            }
            else
            {
                xdata[Constants.LoiStatus] = Constants.StatusWarn;
                xdata[Constants.LoiMissingFields] = string.Join(";", missingFields);
            }
        }

        /// <summary>
        /// Normalize parameter name (handle special cases)
        /// </summary>
        private string NormalizeParameterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            // Check for special normalization
            if (Constants.FieldNameNormalization.TryGetValue(name, out string normalized))
                return normalized;

            return name; // Use exact name from schema
        }
    }
}
