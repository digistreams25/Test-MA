# Changelog

All notable changes to the TwinThread Civil 3D LOI Plugin will be documented in this file.

## [1.0.0] - 2026-02-15

### Initial Release

#### Features
- **TT_APPLY_SCHEMA Command**: Apply LOI schema to Civil 3D objects
- **TT_CLEAR_SCHEMA Command**: Remove all TwinThread XData from drawing
- **Object Type Support**: Corridors, Pipes, Structures, 3D Solids
- **Matching Rules**: 6 rule types with 21 specific match kinds
  - Layer matching (equals, contains, regex)
  - Style matching (equals, regex)
  - Name matching (equals, contains, regex)
  - Pipe network matching (network, part family, part size)
  - Corridor matching (alignment, assembly, region)
- **LOI Fields**: Enforce 6 standard fields
  - PDS_Code
  - Company_Name
  - Design_Stage
  - Design_Status
  - Material
  - Suitability_Code
- **Validation**: PASS/WARN status with missing field tracking
- **Fill-Missing Logic**: Preserve existing non-empty values
- **XData Storage**: All data under "TWINTHREAD" RegApp
- **Error Reporting**: Comprehensive error handling and reporting

#### Architecture
- **Models**: JSON schema deserialization with System.Text.Json
- **Core Logic**: SchemaLoader, MatchingEngine, LOIValidator, XDataStore
- **Discovery**: Robust object discovery with defensive null handling
- **Commands**: Interactive commands with user feedback

#### Compatibility
- Civil 3D 2023, 2024, 2025, 2026
- .NET Framework 4.8
- Windows 64-bit

#### Documentation
- Complete README with usage examples
- Deployment guide for testing and production
- Example schema JSON file
- PackageContents.xml for .bundle deployment

---

## [Planned] - Future Versions

### [1.1.0] - TBD
- [ ] Add TT_VALIDATE command (check compliance without modifying)
- [ ] Add TT_REPORT command (generate compliance report CSV/JSON)
- [ ] Add TT_SHOW_INFO command (display XData for selected object)
- [ ] Progress bar for large drawings
- [ ] Logging to file option

### [1.2.0] - TBD
- [ ] Batch processing mode (multiple drawings)
- [ ] Schema validation before application
- [ ] Custom field support beyond 6 LOI fields
- [ ] Export unmatched objects to JSON for schema builder

### [2.0.0] - TBD
- [ ] UI panel for schema management
- [ ] Real-time validation on object creation/modification
- [ ] Integration with TwinThread API for automatic schema sync
- [ ] Multi-language support
