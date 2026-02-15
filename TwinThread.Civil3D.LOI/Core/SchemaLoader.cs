using System;
using System.IO;
using System.Text.Json;
using TwinThread.Civil3D.LOI.Models;

namespace TwinThread.Civil3D.LOI.Core
{
    /// <summary>
    /// Loads and parses TwinThread schema JSON files
    /// </summary>
    public class SchemaLoader
    {
        /// <summary>
        /// Load schema from JSON file path
        /// </summary>
        public Schema LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Schema file not found: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            Schema schema = JsonSerializer.Deserialize<Schema>(jsonContent, options);

            if (schema == null)
            {
                throw new InvalidOperationException("Failed to deserialize schema JSON");
            }

            return schema;
        }
    }
}
