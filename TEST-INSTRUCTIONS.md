# Corridor Solids Property Set Testing

## Test Schema Created

Location: `test-schema-corridor-solids.json`

This schema defines 6 corridor solid types with Property Set storage:

### 1. **Pave1** - Paving Layer 1
- Property Set: `TT_Pave1_Properties`
- Matches: Layers containing "Pave1"
- Properties: Material, Thickness_mm, Layer, Object_Handle

### 2. **Pave2** - Paving Layer 2
- Property Set: `TT_Pave2_Properties`
- Matches: Layers containing "Pave2"
- Properties: Material, Thickness_mm, Layer

### 3. **Base** - Base Course
- Property Set: `TT_Base_Properties`
- Matches: Layers containing "- Base"
- Properties: Material, Thickness_mm, Compaction_Percent, Layer

### 4. **SubBase** - SubBase Course
- Property Set: `TT_SubBase_Properties`
- Matches: Layers containing "SubBase"
- Properties: Material, Thickness_mm, Compaction_Percent, Layer

### 5. **Curb** - Curb
- Property Set: `TT_Curb_Properties`
- Matches: Layers containing "Curb"
- Properties: Material, Type, Height_mm, Width_mm, Layer

### 6. **Sidewalk** - Sidewalk
- Property Set: `TT_Sidewalk_Properties`
- Matches: Layers containing "Sidewalk"
- Properties: Material, Thickness_mm, Width_mm, Finish, Layer

---

## How to Test

### Step 1: Pull Latest Code
```powershell
cd C:\Users\P002715B\Test-MA-GIT
git pull origin claude/civil3d-loi-plugin-JMzp1
```

### Step 2: Rebuild Plugin
1. Open `TwinThread.Civil3D.LOI.sln` in Visual Studio
2. Build Solution (Ctrl+Shift+B)
3. Check for any errors

### Step 3: Load in Civil 3D
1. Open Civil 3D
2. Type `NETLOAD` command
3. Browse to: `TwinThread.Civil3D.LOI\bin\Debug\TwinThread.Civil3D.LOI.dll`
4. Load the plugin

### Step 4: Run the Command
```
Command: TT_APPLY_SCHEMA
Enter schema JSON file path: C:\Users\P002715B\Test-MA-GIT\test-schema-corridor-solids.json
```

### Step 5: Review Results
The command will:
1. Discover all corridor solids on matching layers
2. Create Property Set Definitions for each type
3. Attach property sets to the solids
4. Prompt you to enter values for each property

**Interactive Prompts:**
- Properties with `valueMode: "Manual"` will prompt for input
- Properties with `valueMode: "Rule"` will auto-populate from object data
- You can press Enter to accept default values

### Step 6: Verify Property Sets
1. Select any corridor solid in Civil 3D
2. Open Properties palette (Ctrl+1)
3. Scroll down to see the Property Sets section
4. You should see properties like:
   - `TT_Pave1_Properties` (for Pave1 solids)
   - `TT_Curb_Properties` (for Curb solids)
   - etc.

---

## Expected Output

```
--- TwinThread LOI Schema Application ---
Loading schema from: C:\Users\P002715B\Test-MA-GIT\test-schema-corridor-solids.json
Project: Corridor Solids Test Project
Milestone: Design Phase 1
Schema Elements: 6

Discovering Civil 3D objects...
Total objects found: X

--- Processing Property Set: TT_Pave1_Properties ---
Objects: X
Property sets attached: X

--- Assigning Property Values ---
Enter value for 'Material' (all objects): [Asphalt Type 1]
Enter value for 'Thickness_mm' (all objects): [50]

Results: Updated: X, Created: 0, Skipped: 0, Failed: 0

--- Summary ---
Objects processed: X
Matched to schema: X
  PASS: X
  WARN: 0
No match found: 0
Errors: 0

Schema application complete!
```

---

## Troubleshooting

### Issue: "No objects found"
- Make sure you have corridor solids in the drawing
- Check layer names match the patterns (e.g., contains "Pave1", "Curb", etc.)

### Issue: "Property Set not visible"
- Make sure you selected a corridor solid (3D Solid object)
- Check Properties palette - look for "Property Sets" section
- Try running `REGEN` command

### Issue: Compilation errors
- Make sure all NuGet packages are restored
- Check that you have Civil 3D SDK references installed
- Try Clean Solution then Rebuild

---

## Alternative: Shape Code Matching

Once shape code extraction is working properly, you can modify the schema to use shape code matching instead of layer matching:

```json
"c3d": {
  "targets": ["solid"],
  "matchAll": [
    { "kind": "shapeCodeEquals", "value": "Pave1" }
  ]
}
```

This will match by the actual shape code name from the corridor solid, not the layer name.

---

## Next Steps

After successful testing:
1. Verify property sets are visible in Civil 3D
2. Test editing property values manually in Properties palette
3. Export data to verify values are saved correctly
4. Try different value modes (Global, PerObject, Rule)
