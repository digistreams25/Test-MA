# TwinThread Civil 3D LOI Plugin

Information governance engine for Civil 3D that enforces Level of Information (LOI) Matrix requirements from TwinThread schema.

## Overview

This plugin automatically:
- Matches Civil 3D objects to schema elements based on configurable rules
- Auto-populates LOI metadata fields (PDS_Code, Company_Name, Design_Stage, etc.)
- Validates required fields and tracks compliance (PASS/WARN)
- Stores all metadata in XData under RegApp "TWINTHREAD"

## Supported Objects

- **Corridors** - from CorridorCollection
- **Pipes** - from pipe networks
- **Structures** - from pipe networks
- **3D Solids** - Solid3d entities in ModelSpace

## Commands

### TT_APPLY_SCHEMA
Applies LOI schema to all supported objects in the drawing.

**Usage:**
1. Type `TT_APPLY_SCHEMA` at command line
2. Enter path to schema JSON file
3. Review summary output

**Behavior:**
- **Fill-Missing Only**: Existing non-empty LOI fields are preserved
- **First Match Wins**: Uses first schemaElement that matches rules
- **Error Reporting**: Reports locked layers, property access failures, XData size errors

**Output:**
```
--- Summary ---
Objects processed: 245
Matched to schema: 198
  PASS: 150
  WARN: 48
No match found: 47
Errors: 0
```

### TT_CLEAR_SCHEMA
Removes all TWINTHREAD XData from the drawing.

**Usage:**
1. Type `TT_CLEAR_SCHEMA` at command line
2. Confirm with "Yes"

## Schema File Format

Minimum required JSON structure:

```json
{
  "projectId": "proj-123",
  "projectName": "Highway Expansion",
  "milestone": {
    "id": "milestone-456",
    "name": "Design Development"
  },
  "schemaElements": [
    {
      "id": "elem-001",
      "name": "Storm Pipes",
      "schemaElementParameters": [
        {
          "name": "PDS_Code",
          "storageType": "String",
          "isRequired": true,
          "scopedDefinition": {
            "parameterDefinitionSource": {
              "value": "STM-001"
            }
          }
        },
        {
          "name": "Material",
          "storageType": "String",
          "isRequired": true,
          "scopedDefinition": {
            "parameterDefinitionSource": {
              "value": "Concrete"
            }
          }
        }
      ],
      "c3d": {
        "targets": ["pipe"],
        "matchAny": [
          { "kind": "networkEquals", "value": "Storm" },
          { "kind": "layerRegex", "value": "^C-UTIL-STRM" }
        ],
        "matchAll": []
      }
    }
  ]
}
```

## Matching Rules

### Precedence
1. **Direct ID Match** - If `tt.schemaElementId` exists in XData, use that element
2. **Rule Evaluation** - Otherwise, evaluate schemaElements in order and take first match

### Match Logic
For a schemaElement to match:
- Object type must be in `c3d.targets` (empty = all types)
- ALL rules in `matchAll` must be true (empty = true)
- At LEAST ONE rule in `matchAny` must be true (empty = true)

### Supported Match Rules

| Rule Kind | Description | Example |
|-----------|-------------|---------|
| `layerEquals` | Exact layer name | `"C-UTIL-PIPE"` |
| `layerContains` | Layer contains text | `"UTIL"` |
| `layerRegex` | Layer matches regex | `"^C-UTIL-"` |
| `styleEquals` | Exact style name | `"Storm Sewer"` |
| `styleRegex` | Style matches regex | `"^Storm.*"` |
| `nameEquals` | Exact object name | `"Main Line"` |
| `nameContains` | Name contains text | `"Main"` |
| `nameRegex` | Name matches regex | `"^ML-"` |
| `networkEquals` | Pipe network name | `"Storm"` |
| `partFamilyEquals` | Part family name | `"Concrete Pipe"` |
| `partSizeEquals` | Part size | `"12 inch"` |
| `alignmentEquals` | Corridor alignment | `"Main Alignment"` |
| `alignmentRegex` | Alignment regex | `"^AL-.*"` |
| `assemblyEquals` | Corridor assembly | `"Typical Road"` |
| `assemblyRegex` | Assembly regex | `"^Road.*"` |
| `regionEquals` | Corridor region | `"Region 1"` |
| `regionRegex` | Region regex | `"^Region.*"` |

## LOI Fields Enforced

The plugin enforces these 6 LOI fields only:

1. **PDS_Code** - Product Data Sheet code
2. **Company_Name** - Responsible company
3. **Design_Stage** - Current design stage
4. **Design_Status** - Status (Draft, Approved, etc.)
5. **Material** - Material specification
6. **Suitability_Code** - Suitability classification

**Special Normalization:**
- "Suitability Code" → "Suitability_Code" (space to underscore)
- All other field names must match schema exactly (case-sensitive)

## XData Structure

All data stored under RegApp: `TWINTHREAD`

### Metadata Fields (Always Written)
```
tt.projectId
tt.projectName
tt.milestoneId
tt.milestoneName
tt.schemaElementId
tt.schemaElementName
tt.updatedAtUtc
```

### LOI Fields (Fill-Missing Only)
```
PDS_Code
Company_Name
Design_Stage
Design_Status
Material
Suitability_Code
```

### Validation Fields
```
tt.loi.status         = "PASS" | "WARN"
tt.loi.missingFields  = "PDS_Code;Material" (semicolon-delimited)
```

## Error Handling

| Error Type | Behavior |
|------------|----------|
| Schema file not found | Abort with error message |
| Invalid JSON | Abort with parse error |
| Invalid regex pattern | Skip that rule silently |
| Locked layer | Report error, skip object |
| Property access failed | Report error, skip object |
| XData size > 16KB | Report error, skip object |

## Building the Plugin

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.8
- Civil 3D 2023, 2024, 2025, or 2026 installed

### Build Steps

1. **Update DLL Paths**
   Edit `TwinThread.Civil3D.LOI.csproj` and update reference paths to match your Civil 3D installation:

   ```xml
   <!-- For Civil 3D 2023 -->
   <HintPath>C:\Program Files\Autodesk\AutoCAD 2023\acdbmgd.dll</HintPath>

   <!-- For Civil 3D 2024 -->
   <HintPath>C:\Program Files\Autodesk\AutoCAD 2024\acdbmgd.dll</HintPath>

   <!-- etc. -->
   ```

2. **Build Solution**
   ```
   msbuild TwinThread.Civil3D.LOI.csproj /p:Configuration=Release
   ```

3. **Output Location**
   ```
   bin\Release\TwinThread.Civil3D.LOI.dll
   ```

## Deployment

### Option 1: NETLOAD (Testing)
1. Open Civil 3D
2. Type `NETLOAD`
3. Browse to `TwinThread.Civil3D.LOI.dll`
4. Commands are now available

### Option 2: AutoLoad (.bundle)
1. Create folder structure:
   ```
   TwinThread.Civil3D.LOI.bundle/
   ├── Contents/
   │   └── Windows/
   │       └── TwinThread.Civil3D.LOI.dll
   └── PackageContents.xml
   ```

2. Create `PackageContents.xml`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <ApplicationPackage>
     <Components>
       <RuntimeRequirements OS="Win64" Platform="AutoCAD" />
       <ComponentEntry AppName="TwinThreadLOI"
                       ModuleName="./Contents/Windows/TwinThread.Civil3D.LOI.dll"
                       LoadOnCommandInvocation="False"
                       LoadOnAutoCADStartup="True" />
     </Components>
   </ApplicationPackage>
   ```

3. Copy `.bundle` folder to:
   ```
   C:\ProgramData\Autodesk\ApplicationPlugins\
   ```

4. Restart Civil 3D

## Architecture

```
TwinThread.Civil3D.LOI/
├── Models/
│   └── Schema.cs              # JSON data models
├── Core/
│   ├── SchemaLoader.cs        # JSON parsing
│   ├── MatchingEngine.cs      # Rule evaluation
│   ├── EntityContext.cs       # Object metadata
│   ├── LOIValidator.cs        # Validation logic
│   └── XDataStore.cs          # XData read/write
├── Discovery/
│   └── C3DDiscovery.cs        # Object discovery
├── Commands/
│   └── Commands.cs            # TT_* commands
└── Utilities/
    └── Constants.cs           # Shared constants
```

## Workflow

```
User runs TT_APPLY_SCHEMA
    ↓
Prompt for schema JSON path
    ↓
Load & parse schema (SchemaLoader)
    ↓
Discover all objects (C3DDiscovery)
    ↓
For each object:
    ├─ Extract context (layer, style, network, etc.)
    ├─ Read existing XData
    ├─ Match to schemaElement (MatchingEngine)
    ├─ Apply LOI fields (LOIValidator)
    ├─ Validate required fields
    └─ Write XData back
    ↓
Print summary & unmatched objects
```

## Troubleshooting

### "No active document found"
- Ensure a drawing is open in Civil 3D

### "Schema file not found"
- Verify the JSON file path is correct
- Use absolute paths (e.g., `C:\Schemas\project.json`)

### "XData too large"
- Schema has too many parameters or very long values
- Consider reducing parameter count or value lengths

### Objects not matching
- Check `matchAny`/`matchAll` rules in schema
- Verify object properties (layer, style, network) using Civil 3D Toolspace
- Review "Unmatched Objects" section in command output

### Compile errors
- Verify Civil 3D DLL reference paths in `.csproj`
- Ensure .NET Framework 4.8 SDK is installed
- Check that all using statements are resolved

## Version Compatibility

| Civil 3D Version | .NET Framework | Tested |
|------------------|----------------|--------|
| 2023 | 4.8 | ✓ |
| 2024 | 4.8 | ✓ |
| 2025 | 4.8 | ✓ |
| 2026 | 4.8 | ✓ |

## License

Proprietary - TwinThread

## Support

For issues or questions, contact TwinThread support.
