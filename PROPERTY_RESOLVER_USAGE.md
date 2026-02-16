# PropertyResolver Usage Guide

## Overview

The `PropertyResolver` class provides a dynamic property mapping layer that translates schema path strings (like `"Pipe.Length3D"`) into actual Civil 3D API property calls.

## Key Features

✅ **Comprehensive Object Type Support:**
- Pipe Network Pipes
- Pipe Network Structures
- Pressure Network Pipes
- Pressure Network Fittings
- Pressure Network Appurtenances
- Corridors
- Alignments
- 3D Solids
- Generic AutoCAD Entities

✅ **Intelligent Error Handling:**
- Graceful fallback for unsupported properties
- Single debug log per unsupported path (no spam)
- Null-safe property access

✅ **Flexible Value Retrieval:**
- Raw object values
- Formatted string values
- Point3d special formatting

---

## Basic Usage

### 1. Simple Property Resolution

```csharp
using TwinThread.Civil3D.LOI.Core;
using Autodesk.Civil.DatabaseServices;

var resolver = new PropertyResolver();

// Resolve pipe length
Pipe pipe = /* get pipe object */;
object length = resolver.ResolveProperty(pipe, "Pipe.Length3D", transaction);

// Or using shorthand (without object type prefix)
object diameter = resolver.ResolveProperty(pipe, "InnerDiameter", transaction);
```

### 2. Formatted String Output

```csharp
// Get property as formatted string
string lengthStr = resolver.ResolvePropertyAsString(pipe, "Length3D", transaction, "F2");
// Output: "125.43"

// Get point coordinates
string startPoint = resolver.ResolvePropertyAsString(pipe, "StartPoint", transaction);
// Output: "100.000,200.000,50.500"
```

### 3. Check if Property is Supported

```csharp
bool supported = resolver.IsPropertySupported(pipe, "Length3D");
if (supported)
{
    object value = resolver.ResolveProperty(pipe, "Length3D", transaction);
}
```

---

## Supported Properties by Object Type

### Pipe Network - Pipe

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `Pipe.Length3D` | `pipe.Length3DCenterToCenter` | double |
| `Pipe.Length2D` | `pipe.Length2DCenterToCenter` | double |
| `Pipe.InnerDiameter` | `pipe.InnerDiameterOrWidth` | double |
| `Pipe.OuterDiameter` | `pipe.OuterDiameterOrWidth` | double |
| `Pipe.Slope` | `pipe.Slope` | double |
| `Pipe.StartPoint` | `pipe.StartPoint` | Point3d |
| `Pipe.EndPoint` | `pipe.EndPoint` | Point3d |
| `Pipe.StartOffset` | `pipe.StartOffset` | double |
| `Pipe.EndOffset` | `pipe.EndOffset` | double |
| `Pipe.StartInvert` | Calculated: `StartPoint.Z - StartOffset` | double |
| `Pipe.EndInvert` | Calculated: `EndPoint.Z - EndOffset` | double |
| `Pipe.PartFamilyName` | `pipe.PartFamilyName` | string |
| `Pipe.PartSizeName` | `pipe.PartSizeName` | string |
| `Pipe.PartDescription` | `pipe.PartDescription` | string |
| `Pipe.NetworkName` | `pipe.NetworkName` | string |
| `Pipe.FlowDirection` | `pipe.FlowDirection.ToString()` | string |
| `Pipe.StyleName` | `pipe.StyleName` | string |
| `Pipe.Name` | `pipe.Name` | string |
| `Pipe.WallThickness` | Calculated: `(OuterDiam - InnerDiam) / 2` | double |

### Pipe Network - Structure

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `Structure.RimElevation` | `structure.RimElevation` | double |
| `Structure.SumpElevation` | `structure.SumpElevation` | double |
| `Structure.SumpDepth` | `structure.SumpDepth` | double |
| `Structure.Position` | `structure.Position` | Point3d |
| `Structure.Rotation` | `structure.Rotation` | double |
| `Structure.InnerDiameter` | `structure.InnerDiameterOrWidth` | double |
| `Structure.InnerLength` | `structure.InnerLength` | double |
| `Structure.PartFamilyName` | `structure.PartFamilyName` | string |
| `Structure.PartSizeName` | `structure.PartSizeName` | string |
| `Structure.NetworkName` | `structure.NetworkName` | string |
| `Structure.StyleName` | `structure.StyleName` | string |
| `Structure.Depth` | Calculated: `RimElevation - SumpElevation` | double |

### Pressure Network - Pipe

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `PressurePipe.Length3D` | `pressurePipe.Length3DCenterToCenter` | double |
| `PressurePipe.InnerDiameter` | `pressurePipe.InnerDiameter` | double |
| `PressurePipe.OuterDiameter` | `pressurePipe.OuterDiameter` | double |
| `PressurePipe.WallThickness` | `pressurePipe.WallThickness` | double |
| `PressurePipe.StartPoint` | `pressurePipe.StartPoint` | Point3d |
| `PressurePipe.EndPoint` | `pressurePipe.EndPoint` | Point3d |
| `PressurePipe.PartFamilyName` | `pressurePipe.PartFamilyName` | string |

### Pressure Network - Fitting

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `PressureFitting.Position` | `fitting.Position` | Point3d |
| `PressureFitting.Rotation` | `fitting.Rotation` | double |
| `PressureFitting.PartFamilyName` | `fitting.PartFamilyName` | string |

### Pressure Network - Appurtenance

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `PressureAppurtenance.Position` | `appurtenance.Position` | Point3d |
| `PressureAppurtenance.PartFamilyName` | `appurtenance.PartFamilyName` | string |

### Corridor

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `Corridor.Name` | `corridor.Name` | string |
| `Corridor.StyleName` | `corridor.StyleName` | string |
| `Corridor.BaselineCount` | `corridor.Baselines.Count` | int |
| `Corridor.AlignmentName` | `corridor.Baselines[0].Name` | string |

### Alignment

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `Alignment.Name` | `alignment.Name` | string |
| `Alignment.Length` | `alignment.Length` | double |
| `Alignment.StartingStation` | `alignment.StartingStation` | double |
| `Alignment.EndingStation` | `alignment.EndingStation` | double |

### Solid3d

| Property Path | API Mapping | Return Type |
|---------------|-------------|-------------|
| `Solid3d.Volume` | `solid.MassProperties.Volume` | double |
| `Solid3d.Material` | `solid.Material` | string |

### Generic Entity Properties

All entities support these common properties:

| Property Path | API Mapping |
|---------------|-------------|
| `Layer` | `entity.Layer` |
| `Handle` | `entity.Handle.ToString()` |
| `Color` | `entity.Color.ToString()` |
| `Linetype` | `entity.Linetype` |

---

## Integration Example

### Using PropertyResolver with Schema Parameters

```csharp
public void ApplySchemaProperties(Entity entity, List<SchemaElementParameter> parameters)
{
    var resolver = new PropertyResolver();

    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        foreach (var param in parameters)
        {
            // Resolve property from entity using schema path
            object value = resolver.ResolveProperty(entity, param.PropertyPath, tr);

            if (value != null)
            {
                // Store in XData, PropertySet, or wherever needed
                string valueStr = resolver.ResolvePropertyAsString(entity, param.PropertyPath, tr);
                Console.WriteLine($"{param.Name}: {valueStr}");
            }
            else
            {
                // Property not supported or not available
                Console.WriteLine($"Warning: Could not resolve '{param.PropertyPath}'");
            }
        }

        tr.Commit();
    }
}
```

### Dynamic Property Reading

```csharp
public Dictionary<string, object> ExtractProperties(Entity entity, string[] propertyPaths)
{
    var resolver = new PropertyResolver();
    var results = new Dictionary<string, object>();

    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        foreach (string path in propertyPaths)
        {
            object value = resolver.ResolveProperty(entity, path, tr);
            if (value != null)
            {
                results[path] = value;
            }
        }

        tr.Commit();
    }

    return results;
}

// Usage
var properties = ExtractProperties(pipe, new[] {
    "Pipe.Length3D",
    "Pipe.InnerDiameter",
    "Pipe.PartFamilyName",
    "Pipe.StartPoint"
});
```

---

## Unsupported Properties

When a property path is not supported:

1. **Method returns `null`**
2. **Debug log message (once per path):**
   ```
   Unsupported path: Pipe.CustomProperty
   ```

This prevents log spam while still alerting developers to missing mappings.

---

## Error Handling

The resolver uses try-catch blocks internally to handle:
- Properties that don't exist on specific object types
- Properties that throw exceptions when accessed
- Null reference scenarios

**Example:**
```csharp
try
{
    object value = resolver.ResolveProperty(entity, "Pipe.Length3D", tr);
    if (value != null)
    {
        double length = (double)value;
        // Use length
    }
}
catch (Exception ex)
{
    // Resolver internal errors are logged, not thrown
    // This catch is for your own value processing errors
}
```

---

## Adding New Property Mappings

To add support for a new property:

1. **Identify the object type** (Pipe, Structure, etc.)
2. **Add case to the appropriate resolver method:**

```csharp
private object ResolvePipeProperty(Pipe pipe, string propertyName)
{
    switch (propertyName)
    {
        // Existing cases...

        case "MyNewProperty":
            return pipe.MyNewApiProperty;

        default:
            LogUnsupportedPath($"Pipe.{propertyName}");
            return null;
    }
}
```

3. **Test the new mapping:**
```csharp
object value = resolver.ResolveProperty(pipe, "MyNewProperty", tr);
Assert.IsNotNull(value);
```

---

## Best Practices

1. **Always provide Transaction parameter** when reading related objects (though currently optional for most properties)

2. **Check for null before casting:**
   ```csharp
   object value = resolver.ResolveProperty(entity, path, tr);
   if (value is double doubleValue)
   {
       // Use doubleValue safely
   }
   ```

3. **Use formatted strings for display:**
   ```csharp
   string display = resolver.ResolvePropertyAsString(pipe, "Length3D", tr, "F2");
   ```

4. **Log unsupported paths during development** to identify missing mappings

5. **Consider caching the PropertyResolver instance** instead of creating new ones repeatedly

---

## Performance Notes

- Property resolution is lightweight (simple switch statements)
- No reflection is used
- Unsupported path logging uses HashSet for O(1) duplicate checking
- Transaction parameter allows batch operations without repeated transaction creation

---

## Future Enhancements

Potential additions:
- Surface properties (TIN surfaces, grading surfaces)
- Profile properties
- Assembly/subassembly properties
- Feature line properties
- Custom user-defined property support via configuration file
