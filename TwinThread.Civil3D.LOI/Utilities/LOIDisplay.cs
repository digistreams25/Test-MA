using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace TwinThread.Civil3D.LOI.Utilities
{
    /// <summary>
    /// Displays LOI data from objects in user-friendly formats
    /// </summary>
    public class LOIDisplay
    {
        private readonly XDataStore _xdataStore;

        public LOIDisplay()
        {
            _xdataStore = new XDataStore();
        }

        /// <summary>
        /// Shows LOI data for the given entity in a message box
        /// </summary>
        public void ShowInMessageBox(Entity entity, string objectType)
        {
            Dictionary<string, string> loiData = _xdataStore.ReadXDataToDictionary(entity);

            if (loiData == null || loiData.Count == 0)
            {
                MessageBox.Show(
                    "No LOI data found on this object.\n\nUse TT_APPLY_SCHEMA to apply LOI data.",
                    "TwinThread LOI - No Data",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Object Type: {objectType}");
            sb.AppendLine($"Handle: {entity.Handle}");
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine("     TwinThread LOI Properties");
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine();

            // Display in organized sections
            if (loiData.ContainsKey(Constants.LoiStatus))
            {
                sb.AppendLine($"LOI Status: {loiData[Constants.LoiStatus]}");
                sb.AppendLine();
            }

            sb.AppendLine("Schema Information:");
            sb.AppendLine("───────────────────────────────────");
            AddIfExists(sb, loiData, Constants.SchemaElementName, "  Schema Element");
            AddIfExists(sb, loiData, Constants.MilestoneName, "  Milestone");
            sb.AppendLine();

            sb.AppendLine("Design Information:");
            sb.AppendLine("───────────────────────────────────");
            AddIfExists(sb, loiData, "PDS_Code", "  PDS Code");
            AddIfExists(sb, loiData, "Design_Stage", "  Design Stage");
            AddIfExists(sb, loiData, "Design_Status", "  Design Status");
            AddIfExists(sb, loiData, "Suitability_Code", "  Suitability Code");
            sb.AppendLine();

            sb.AppendLine("Project Information:");
            sb.AppendLine("───────────────────────────────────");
            AddIfExists(sb, loiData, "Company_Name", "  Company Name");
            AddIfExists(sb, loiData, "Material", "  Material");

            // Show any additional properties
            List<string> knownKeys = new List<string>
            {
                Constants.LoiStatus,
                Constants.SchemaElementName,
                Constants.MilestoneName,
                "PDS_Code",
                "Design_Stage",
                "Design_Status",
                "Suitability_Code",
                "Company_Name",
                "Material"
            };

            List<string> additionalKeys = new List<string>();
            foreach (string key in loiData.Keys)
            {
                if (!knownKeys.Contains(key))
                {
                    additionalKeys.Add(key);
                }
            }

            if (additionalKeys.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Additional Properties:");
                sb.AppendLine("───────────────────────────────────");
                foreach (string key in additionalKeys)
                {
                    sb.AppendLine($"  {key}: {loiData[key]}");
                }
            }

            MessageBox.Show(
                sb.ToString(),
                "TwinThread LOI Properties",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        /// <summary>
        /// Formats LOI data as a formatted string for console output
        /// </summary>
        public string FormatForConsole(Entity entity, string objectType)
        {
            Dictionary<string, string> loiData = _xdataStore.ReadXDataToDictionary(entity);

            if (loiData == null || loiData.Count == 0)
            {
                return "No LOI data found.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"\nObject: {objectType} (Handle: {entity.Handle})");
            sb.AppendLine("─────────────────────────────────────────────────");

            foreach (KeyValuePair<string, string> kvp in loiData)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets LOI status indicator for quick display
        /// </summary>
        public string GetStatusIndicator(Entity entity)
        {
            Dictionary<string, string> loiData = _xdataStore.ReadXDataToDictionary(entity);

            if (loiData == null || loiData.Count == 0)
            {
                return "[No LOI Data]";
            }

            if (loiData.ContainsKey(Constants.LoiStatus))
            {
                string status = loiData[Constants.LoiStatus];
                return status == "PASS" ? "[LOI: ✓ PASS]" :
                       status == "FAIL" ? "[LOI: ✗ FAIL]" :
                       $"[LOI: {status}]";
            }

            return "[LOI: Unknown]";
        }

        /// <summary>
        /// Checks if entity has LOI data
        /// </summary>
        public bool HasLOIData(Entity entity)
        {
            Dictionary<string, string> loiData = _xdataStore.ReadXDataToDictionary(entity);
            return loiData != null && loiData.Count > 0;
        }

        /// <summary>
        /// Helper to add property if it exists
        /// </summary>
        private void AddIfExists(StringBuilder sb, Dictionary<string, string> data, string key, string displayName)
        {
            if (data.ContainsKey(key) && !string.IsNullOrWhiteSpace(data[key]))
            {
                sb.AppendLine($"{displayName}: {data[key]}");
            }
        }
    }
}
