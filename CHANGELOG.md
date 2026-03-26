# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.2] - 2025-03-26

### Changed

- Character Controller **GitHub import** skips repository **package.json** (and meta) so Unity does not treat **Assets/CCS/CharacterController** as an installable UPM package. Legacy **package.json** in that folder is removed on load when sources are present.
- Registry **PackageId** is **`ccs.charactercontroller.assets-only`** (not `com.crazycarrot.charactercontroller`) to avoid confusion with a Package Manager dependency.

## [0.1.1] - 2025-03-25

### Changed

- **CCS Character Controller** installs from a public **GitHub archive** into **`Assets/CCS/CharacterController` only** (not Package Manager). File copy skips empty folders; optional **Samples~/BasicSetup** is materialized to **BasicSetup** when present.
- Default hub scaffold folders are **`Assets/CCS`** and **`Assets/CCS/CharacterController`** (no extra empty CCS sibling template folders).

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
- **URP** project detection (Package Manager + active pipeline) so the hub does not queue a redundant URP add when URP is already in use
- Post-reload **installing after reload** banner when a pending queue is restored from session state
- **Last setup summary** foldout (installed batch, failed installs, manual rows)
- **Run Full CCS Setup** (queue installs + **Assets/CCS** folders in one action)
- Optional **CCS Branding** styling via reflection when `com.crazycarrot.branding` is present (`CCSHubBrandingUi`)
- Session state cleanup uses `SetBool` / `SetString` instead of `EraseBool` / `EraseString` for broader Unity compatibility
