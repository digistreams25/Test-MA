# Pipe PropertyResolver Testing Guide

## 🎯 Overview

This guide shows how to test the **PropertyResolver** functionality using the `pipe-property-resolver-test.json` schema with actual Civil 3D pipe networks.

---

## 📋 Test Schema Summary

**File:** `pipe-property-resolver-test.json`

**Contains 4 test schema elements:**

1. **Pipe Geometry Properties Test** - Tests all geometric properties (Length3D, diameters, slopes, inverts, points)
2. **Pipe Part Information Test** - Tests part catalogs (PartFamily, PartSize, Network, Style)
3. **Structure Properties Test** - Tests structure properties (RimElevation, SumpElevation, Depth)
4. **Complete Pipe Test** - Combines LOI required fields + PropertyResolver paths

---

## 🛠️ Setup Instructions

### 1. Prepare Civil 3D Drawing

**Create or open a drawing with:**
- ✅ At least one **Pipe Network** (Storm, Sanitary, or Water)
- ✅ Multiple **pipes** with different sizes
- ✅ Multiple **structures** (manholes, catch basins)
- ✅ Pipes with varying slopes and elevations

**Recommended Test Setup:**
```
Network Name: "Storm" or "Sanitary"
- 3-5 pipes with different diameters (300mm, 450mm, 600mm)
- 4-6 structures (manholes)
- Varying slopes (0.5% to 2%)
- Different elevations for testing inverts
```

### 2. Load the Plugin

```
NETLOAD
→ Browse to: TwinThread.Civil3D.LOI.dll
```

### 3. Load the Test Schema

**Option A: Using Command (if schema loading command exists)**
```
TWINTHREAD_LOADSCHEMA
→ Select: pipe-property-resolver-test.json
```

**Option B: Place in expected location**
```
Copy pipe-property-resolver-test.json to:
C:\TwinThread\pipe-property-resolver-test.json

(Or wherever your plugin looks for schemas)
```

---

## 🧪 Test Scenarios

### Test 1: Pipe Geometry Properties

**Schema Element:** `pipe-geometry-test`

**Properties Tested:**
- ✅ `Pipe.Length3D` → 3D center-to-center length
- ✅ `Pipe.Length2D` → 2D horizontal length
- ✅ `Pipe.InnerDiameter` → Inner diameter/width
- ✅ `Pipe.OuterDiameter` → Outer diameter/width
- ✅ `Pipe.WallThickness` → Calculated: (Outer - Inner) / 2
- ✅ `Pipe.Slope` → Pipe slope
- ✅ `Pipe.StartOffset` → Start vertical offset
- ✅ `Pipe.EndOffset` → End vertical offset
- ✅ `Pipe.StartInvert` → Calculated: StartPoint.Z - StartOffset
- ✅ `Pipe.EndInvert` → Calculated: EndPoint.Z - EndOffset
- ✅ `Pipe.StartPoint` → 3D start point (formatted as X,Y,Z)
- ✅ `Pipe.EndPoint` → 3D end point (formatted as X,Y,Z)

**Expected Result:**
All numeric values should be populated from the actual pipe geometry in Civil 3D.

**Verification:**
1. Select a pipe in Civil 3D
2. Check Properties palette
3. Compare values with resolved properties in XData/PropertySet

---

### Test 2: Pipe Part Information

**Schema Element:** `pipe-part-info-test`

**Properties Tested:**
- ✅ `Pipe.PartFamilyName` → e.g., "Concrete Pipe"
- ✅ `Pipe.PartSizeName` → e.g., "450 mm"
- ✅ `Pipe.PartDescription` → Part catalog description
- ✅ `Pipe.NetworkName` → Network name (e.g., "Storm")
- ✅ `Pipe.FlowDirection` → "BySlope", "Start2End", etc.
- ✅ `Pipe.StyleName` → Pipe style name
- ✅ `Pipe.Name` → Pipe name/label
- ✅ `Pipe.Layer` → Layer name
- ✅ `Pipe.Handle` → AutoCAD handle

**Expected Result:**
All text values should match the part catalog and object properties.

**Verification:**
```
1. Select pipe → Properties
2. Check "Part Family" field
3. Compare with RESOLVED_PartFamily in stored data
```

---

### Test 3: Structure Properties

**Schema Element:** `structure-test`

**Properties Tested:**
- ✅ `Structure.RimElevation` → Top elevation
- ✅ `Structure.SumpElevation` → Bottom/sump elevation
- ✅ `Structure.SumpDepth` → Sump depth
- ✅ `Structure.Depth` → Calculated: Rim - Sump
- ✅ `Structure.InnerDiameter` → Inner diameter
- ✅ `Structure.Position` → 3D position point
- ✅ `Structure.PartFamilyName` → e.g., "Rectangular Structure"
- ✅ `Structure.PartSizeName` → e.g., "1200x1200"
- ✅ `Structure.NetworkName` → Network name
- ✅ `Structure.Name` → Structure name

**Expected Result:**
All elevations and dimensions match Civil 3D structure properties.

**Verification:**
```
1. Select structure → Properties
2. Check "Rim Elevation" and "Sump Elevation"
3. Verify Depth = Rim - Sump
```

---

### Test 4: Complete Pipe Test (LOI + PropertyResolver)

**Schema Element:** `pipe-all-properties`

**Matches:** Pipes with "TEST" or "DEMO" in the name

**Tests:**
- ✅ Standard LOI fields (PDS_Code, Company_Name, Design_Stage, etc.)
- ✅ PropertyResolver fields (Length3D, InnerDiameter, Slope, etc.)
- ✅ Combination of manual and auto-resolved properties

**Expected Result:**
- Manual LOI fields get static values from schema
- PropertyResolver fields get dynamic values from pipe API

---

## 🚀 Running the Tests

### Method 1: AutoCAD Command Line

```
1. NETLOAD → Load TwinThread.Civil3D.LOI.dll
2. TWINTHREAD_APPLY → Run LOI application
3. Select schema: pipe-property-resolver-test.json
4. Select target objects (pipes and structures)
5. Review results
```

### Method 2: Programmatic Testing

```csharp
// In your command or test harness:
var resolver = new PropertyResolver();

using (Transaction tr = db.TransactionManager.StartTransaction())
{
    Pipe pipe = tr.GetObject(pipeId, OpenMode.ForRead) as Pipe;

    // Test individual property resolution
    object length = resolver.ResolveProperty(pipe, "Pipe.Length3D", tr);
    object diameter = resolver.ResolveProperty(pipe, "Pipe.InnerDiameter", tr);
    object partFamily = resolver.ResolveProperty(pipe, "Pipe.PartFamilyName", tr);

    // Test formatted output
    string lengthStr = resolver.ResolvePropertyAsString(pipe, "Length3D", tr, "F2");

    Console.WriteLine($"Length: {lengthStr} m");
    Console.WriteLine($"Diameter: {diameter}");
    Console.WriteLine($"Part Family: {partFamily}");

    tr.Commit();
}
```

---

## ✅ Validation Checklist

### Pre-Test Setup
- [ ] Civil 3D drawing has at least 1 pipe network
- [ ] Multiple pipes with different sizes exist
- [ ] Pipes have varying elevations and slopes
- [ ] Plugin DLL is loaded successfully
- [ ] Test schema JSON is in correct location

### During Testing
- [ ] Schema loads without errors
- [ ] PropertyResolver doesn't throw exceptions
- [ ] All property paths resolve successfully
- [ ] Numeric values are reasonable (not 0 or NaN)
- [ ] Text values are populated (not empty strings)
- [ ] Point3d values are formatted correctly (X,Y,Z)

### Post-Test Verification
- [ ] Check XData on pipes (TWINTHREAD_INSPECT command)
- [ ] Verify PropertySet values (if PropertySet mode enabled)
- [ ] Compare resolved values with Civil 3D Properties palette
- [ ] Check debug log for "Unsupported path" warnings
- [ ] Validate calculated properties (WallThickness, Depth, Inverts)

---

## 🐛 Troubleshooting

### Issue: "Unsupported path: Pipe.XXX"

**Cause:** Property path not implemented in PropertyResolver

**Solution:**
1. Check `PropertyResolver.cs` → `ResolvePipeProperty()` method
2. Add the missing property case
3. Rebuild and reload plugin

---

### Issue: All values return null

**Cause:** Entity type mismatch or incorrect property path format

**Solution:**
1. Verify object is actually a `Pipe` or `Structure` entity
2. Check property path format: `"ObjectType.PropertyName"` or `"PropertyName"`
3. Enable debug logging to see resolution attempts

---

### Issue: Point3d shows as "Autodesk.AutoCAD.Geometry.Point3d"

**Cause:** Using raw `ToString()` instead of formatted output

**Solution:**
Use `ResolvePropertyAsString()` which formats Point3d as "X,Y,Z":
```csharp
string point = resolver.ResolvePropertyAsString(pipe, "StartPoint", tr);
// Output: "1234.567,5678.901,890.123"
```

---

### Issue: Calculated properties (WallThickness, Depth, Inverts) are wrong

**Cause:** Underlying API properties not accessible or different API version

**Solution:**
1. Verify API property names for your Civil 3D version
2. Check if `InnerDiameterOrWidth` vs `InnerDiameter` naming
3. Update PropertyResolver mappings if needed

---

## 📊 Expected Output Example

```
Pipe: (1) - Handle: 2A4F
  Length3D: 45.67 m
  InnerDiameter: 0.45 m (450mm)
  Slope: 0.015 (1.5%)
  PartFamilyName: Concrete Pipe
  PartSizeName: 450 mm
  NetworkName: Storm
  StartInvert: 98.234 m
  EndInvert: 97.548 m
  WallThickness: 0.050 m

Structure: MH-1 - Handle: 2A50
  RimElevation: 100.500 m
  SumpElevation: 96.200 m
  Depth: 4.300 m
  PartFamilyName: Rectangular Structure
  PartSizeName: 1200x1200
  NetworkName: Storm
```

---

## 🎓 Learning Objectives

After completing these tests, you should understand:

1. ✅ How PropertyResolver maps schema paths to API calls
2. ✅ Difference between manual LOI fields vs auto-resolved properties
3. ✅ How to add new property mappings to PropertyResolver
4. ✅ How to handle different data types (Real, Text, Point3d)
5. ✅ How calculated properties work (WallThickness, Depth, Inverts)
6. ✅ How to verify property resolution in Civil 3D

---

## 📝 Next Steps

Once pipe testing is complete:

1. **Test Structures** - Use `structure-test` schema element
2. **Test Pressure Networks** - Add PressurePipe test schema
3. **Test Corridors** - Create corridor property test schema
4. **Integrate with LOI Validator** - Use resolved properties in validation rules
5. **Performance Testing** - Test with large networks (100+ pipes)

---

## 📚 Reference

- **PropertyResolver API:** See `PROPERTY_RESOLVER_USAGE.md`
- **Schema Format:** See `ExampleSchema.json`
- **Civil 3D API:** Autodesk.Civil.DatabaseServices namespace

---

**Ready to test!** Load the schema, select some pipes, and watch the PropertyResolver in action! 🚀
