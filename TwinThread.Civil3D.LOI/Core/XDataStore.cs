using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using TwinThread.Civil3D.LOI.Utilities;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Manages XData storage under TWINTHREAD RegApp
    /// </summary>
    public class XDataStore
    {
        /// <summary>
        /// Ensure TWINTHREAD RegApp exists in database
        /// </summary>
        public void EnsureRegApp(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                RegAppTable rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);

                if (!rat.Has(Constants.RegAppName))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord();
                    ratr.Name = Constants.RegAppName;
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Read XData into dictionary (key-value pairs)
        /// </summary>
        public Dictionary<string, string> ReadXDataToDictionary(Entity entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            ResultBuffer xdata = entity.GetXDataForApplication(Constants.RegAppName);
            if (xdata == null)
                return result;

            TypedValue[] values = xdata.AsArray();

            // Parse as key-value pairs (skip first RegApp code)
            for (int i = 1; i < values.Length - 1; i += 2)
            {
                if (values[i].TypeCode == (short)DxfCode.ExtendedDataAsciiString &&
                    values[i + 1].TypeCode == (short)DxfCode.ExtendedDataAsciiString)
                {
                    string key = values[i].Value?.ToString() ?? "";
                    string value = values[i + 1].Value?.ToString() ?? "";
                    result[key] = value;
                }
            }

            xdata.Dispose();
            return result;
        }

        /// <summary>
        /// Write dictionary to XData (overwrites existing)
        /// </summary>
        public void WriteXDataFromDictionary(Entity entity, Dictionary<string, string> data)
        {
            List<TypedValue> values = new List<TypedValue>();
            values.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Constants.RegAppName));

            foreach (var kvp in data)
            {
                values.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, kvp.Key));
                values.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, kvp.Value ?? ""));
            }

            ResultBuffer rb = new ResultBuffer(values.ToArray());
            entity.XData = rb;
            rb.Dispose();
        }

        /// <summary>
        /// Remove all TWINTHREAD XData from entity
        /// </summary>
        public void RemoveXData(Entity entity)
        {
            ResultBuffer xdata = entity.GetXDataForApplication(Constants.RegAppName);
            if (xdata != null)
            {
                xdata.Dispose();
                entity.XData = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, Constants.RegAppName));
            }
        }

        /// <summary>
        /// Check if XData size is within limits (approximately 16KB)
        /// </summary>
        public bool IsXDataSizeValid(Dictionary<string, string> data)
        {
            int estimatedSize = 0;
            foreach (var kvp in data)
            {
                estimatedSize += (kvp.Key?.Length ?? 0) + (kvp.Value?.Length ?? 0) + 4; // +4 for type codes
            }

            return estimatedSize < 15000; // Conservative limit below 16KB
        }
    }
}
