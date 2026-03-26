# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-03-25

### Added

- First-run **CCS Setup Wizard** (`CCSSetupWindow`) with auto-open via `CCSSetupBootstrap` and `CCSSetupState`
- **Tools → CCS → Package Hub** (`CCSPackageHubWindow`) and **Tools → CCS → Setup Wizard**
- **Tools → CCS → Developer → Reset Setup State** for testing
- Data-driven `CCSPackageRegistry`, `CCSPackageDefinition`, categories, source types, and install status enum
- `CCSPackageInstallService` (sequential `Client.Add` queue, session-persisted pending ids)
- `CCSPackageStatusService` (Package Manager list refresh)
- `CCSProjectFolderUtility` (idempotent **Assets/CCS** tree)
- `CCSEditorLog`, `CCSSetupConstants`
- **Manual / Special** entry for DOTween (no automated Git UPM install)
- Recommended **URP** registry entry (optional)
- `package.json` uses empty `dependencies` for hybrid installer workflow (branding installed by wizard, not only as a static dep)
