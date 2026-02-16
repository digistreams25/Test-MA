# Property Set Investigation Guide

## Problem
Property Set Definitions require AutoCAD Architecture DLLs that may not be available in standard Civil 3D installations.

## Required DLLs
The following DLLs are needed for Property Set functionality:
- `Autodesk.Aec.PropertyData.dll`
- `Autodesk.Aec.PropertyData.DatabaseServices.dll`

## Investigation Steps

### Step 1: Check for DLLs in Your Installation

Open PowerShell and run:

```powershell
# Search for Property Data DLLs
Get-ChildItem "C:\Program Files\Autodesk" -Recurse -Filter "*PropertyData*.dll" -ErrorAction SilentlyContinue | Select-Object FullName
```

### Step 2: Check for Alternative Property APIs

```powershell
# Search for all Aec DLLs
Get-ChildItem "C:\Program Files\Autodesk\AutoCAD 2024" -Filter "*.dll" | Where-Object { $_.Name -like "*Aec*" -or $_.Name -like "*Property*" } | Select-Object Name, FullName
```

### Step 3: Check Your Civil 3D Installation Type

Run this in Civil 3D command line:
```
(getvar "PRODUCT")
```

Expected results:
- "AutoCAD Civil 3D 2024" - Standard Civil 3D
- "AutoCAD Architecture 2024" - Has Property Sets
- "AutoCAD MEP 2024" - Has Property Sets

## Alternative Solutions

### Option 1: Install AutoCAD Architecture Components
If Property Sets are required, you may need to install AutoCAD Architecture alongside Civil 3D.

### Option 2: Use Object Data Tables (Map 3D)
If you have Map 3D installed with Civil 3D, we can use Object Data Tables instead.

Check for Map 3D DLLs:
```powershell
Get-ChildItem "C:\Program Files\Autodesk\AutoCAD 2024" -Filter "*Map*.dll" | Select-Object Name
```

Look for:
- `AcMapMgd.dll`
- `AcMapMgd.Interop.dll`

### Option 3: Custom Properties via COM Interop
Use AutoCAD's COM API to add custom properties:
- More complex but works everywhere
- Requires COM Interop

### Option 4: Data Extraction + XData
Keep using XData for storage and create:
- A custom palette to display data
- Enhanced CSV export
- Data extraction templates

## Recommended Next Steps

1. Run the PowerShell commands above
2. Report back which DLLs you found
3. Based on availability, we'll implement:
   - Property Sets (if DLLs found)
   - Object Data Tables (if Map 3D available)
   - Custom palette + XData (fallback, works everywhere)

## Testing Property Data API Availability

If you find the DLLs, we can test if they work by:
1. Adding them to the project references
2. Creating a simple test command
3. Checking if the API is accessible in your Civil 3D environment
