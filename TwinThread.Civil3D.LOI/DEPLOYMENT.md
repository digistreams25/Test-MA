# Deployment Guide - TwinThread Civil 3D LOI Plugin

## Quick Start (For Testing)

### 1. Build the Plugin
```bash
# Open in Visual Studio or use MSBuild
msbuild TwinThread.Civil3D.LOI.csproj /p:Configuration=Release
```

### 2. Load into Civil 3D
1. Open Civil 3D
2. Type `NETLOAD` at command line
3. Browse to: `bin\Release\TwinThread.Civil3D.LOI.dll`
4. Click **Load**

### 3. Test Commands
```
TT_APPLY_SCHEMA
TT_CLEAR_SCHEMA
```

---

## Production Deployment (.bundle Method)

### Step 1: Create .bundle Structure

Create this folder structure:

```
TwinThread.Civil3D.LOI.bundle/
├── Contents/
│   ├── Windows/
│   │   └── TwinThread.Civil3D.LOI.dll
│   └── README.md (optional)
└── PackageContents.xml
```

### Step 2: Copy Files

1. **Copy the DLL:**
   ```
   bin\Release\TwinThread.Civil3D.LOI.dll
   → TwinThread.Civil3D.LOI.bundle/Contents/Windows/
   ```

2. **Copy PackageContents.xml:**
   ```
   PackageContents.xml
   → TwinThread.Civil3D.LOI.bundle/
   ```

### Step 3: Deploy to ApplicationPlugins

Copy the entire `.bundle` folder to one of these locations:

**System-wide (All Users):**
```
C:\ProgramData\Autodesk\ApplicationPlugins\
```

**User-specific:**
```
C:\Users\<username>\AppData\Roaming\Autodesk\ApplicationPlugins\
```

### Step 4: Restart Civil 3D

The plugin will load automatically on startup.

---

## Verification

### Check if Plugin Loaded

1. Open Civil 3D
2. Type `TT_APPLY_SCHEMA` at command line
3. If the command is recognized, plugin loaded successfully

### View Load Status

Check the Civil 3D application menu:
- **Manage** tab
- **Applications** panel
- **Plugin Manager**

---

## Multi-Version Support

### For Civil 3D 2023, 2024, 2025, 2026

The plugin is compatible with all versions. Update `.csproj` reference paths for your target version:

**Civil 3D 2023:**
```xml
<HintPath>C:\Program Files\Autodesk\AutoCAD 2023\acdbmgd.dll</HintPath>
<HintPath>C:\Program Files\Autodesk\AutoCAD 2023\C3D\AeccDbMgd.dll</HintPath>
```

**Civil 3D 2024:**
```xml
<HintPath>C:\Program Files\Autodesk\AutoCAD 2024\acdbmgd.dll</HintPath>
<HintPath>C:\Program Files\Autodesk\AutoCAD 2024\C3D\AeccDbMgd.dll</HintPath>
```

Build separate DLLs for each version if needed.

---

## Network Deployment

### Option A: Shared Network Path

1. Build `.bundle` on network share:
   ```
   \\server\shared\Plugins\TwinThread.Civil3D.LOI.bundle\
   ```

2. Create startup script (`.bat` or Group Policy):
   ```batch
   @echo off
   mklink /D "C:\ProgramData\Autodesk\ApplicationPlugins\TwinThread.Civil3D.LOI.bundle" "\\server\shared\Plugins\TwinThread.Civil3D.LOI.bundle"
   ```

3. Users get updates automatically when DLL is replaced on server

### Option B: Deployment Package

1. Create installer using WiX or Inno Setup
2. Include:
   - DLL file
   - PackageContents.xml
   - README.md
   - ExampleSchema.json

3. Installer copies `.bundle` to ApplicationPlugins folder

---

## Uninstall

### Remove Plugin

1. Close all Civil 3D instances
2. Delete the `.bundle` folder from:
   ```
   C:\ProgramData\Autodesk\ApplicationPlugins\TwinThread.Civil3D.LOI.bundle\
   ```
3. Restart Civil 3D

### Remove XData from Drawings

Before uninstalling, run `TT_CLEAR_SCHEMA` on drawings with TwinThread data if you want to remove the metadata.

---

## Troubleshooting Deployment

### Plugin Not Loading

**Check 1: DLL Location**
- Verify DLL is in: `.bundle/Contents/Windows/`
- Check file permissions (not blocked)

**Check 2: .NET Framework**
- Ensure .NET Framework 4.8 is installed
- Check Windows Features

**Check 3: Dependencies**
- Verify Civil 3D DLLs are referenced correctly
- Check Civil 3D version matches build target

**Check 4: PackageContents.xml**
- Validate XML syntax
- Check `SeriesMin` and `SeriesMax` for version compatibility

### Commands Not Recognized

**Issue:** Type `TT_APPLY_SCHEMA` → "Unknown command"

**Solutions:**
1. Check if plugin loaded:
   - Type `NETLOAD`
   - Manually load DLL
   - Check for error messages

2. Verify command registration:
   - Check `[CommandMethod]` attributes in code
   - Rebuild plugin in Debug mode for detailed errors

### Permission Errors

**Issue:** "Access denied" when copying to ApplicationPlugins

**Solutions:**
1. Run as Administrator
2. Copy to user-specific folder instead:
   ```
   C:\Users\<username>\AppData\Roaming\Autodesk\ApplicationPlugins\
   ```

---

## Security Considerations

### Code Signing (Recommended for Production)

1. Obtain code signing certificate
2. Sign the DLL:
   ```bash
   signtool sign /f certificate.pfx /p password /t http://timestamp.server.com TwinThread.Civil3D.LOI.dll
   ```

### Network Security

- Use HTTPS for schema file hosting
- Validate schema JSON schema version
- Log all schema applications for audit trail

---

## Updates and Maintenance

### Updating Plugin

1. Build new version
2. Update version in `PackageContents.xml`:
   ```xml
   AppVersion="1.1.0"
   ```
3. Replace DLL in `.bundle/Contents/Windows/`
4. Users get update on next Civil 3D restart

### Schema Updates

Schema files are separate from plugin:
- No need to rebuild plugin for schema changes
- BIM managers update schemas via TwinThread web app
- Users point to new schema JSON files

---

## Support

For deployment issues:
- Check Civil 3D error logs: `%LOCALAPPDATA%\Autodesk\Civil 3D\`
- Review Windows Event Viewer for .NET errors
- Contact TwinThread support with:
  - Civil 3D version
  - Windows version
  - Error messages
  - Plugin version
