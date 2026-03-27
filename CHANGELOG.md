# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.19] - 2026-03-27

### Changed

- **Standalone repo:** Republished **`https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub`** with `package.json`, **README**, **CHANGELOG**, and Git tag **`v0.1.19`** aligned so GitHub and UPM installs show the current version.

## [0.1.18] - 2026-03-27

### Changed

- **Release:** Version bump for standalone `com.crazycarrot.hub` repository publish (sync with character-controller dev package).

## [0.1.17] - 2026-03-27

### Fixed

- **DOTween bundle path:** The Demigiant bundle folder is now **`DemigiantDOTweenBundle~`** (trailing tilde). Unity **does not import** paths ending in `~`, so the bundle can live in the project **alongside** `Assets/Plugins/Demigiant` after the Hub copies files—no duplicate `DOTweenModule*` / `DOTweenCYInstruction` types.

## [0.1.16] - 2026-03-27

### Fixed

- **First-run Hub:** Closing the Hub after bootstrap (including after **Install selected**, which closes the window while optional installs run) now **persists “setup completed”** so the Hub **does not auto-open on every project load**. The optional install progress window also marks setup complete when it auto-closes after work finishes.

## [0.1.15] - 2026-03-27

### Added

- **Optional DOTween (Demigiant):** CCS Hub can merge **`DemigiantDOTweenBundle~`** from `com.crazycarrot.charactercontroller` (or `Assets/CCS/CharacterController/DemigiantDOTweenBundle~` in the project) into **`Assets/Plugins`** and **`Assets/Resources`**. When Character Controller is part of the install batch, the copy runs during bootstrap **before** `Assets/CCS/CharacterController` is replaced; otherwise the copy runs immediately when you click **Install selected**. License compliance remains the user's responsibility.

## [0.1.14] - 2025-03-27

### Changed

- **CCS Hub (optional tools):** Rows show **title** (display name), **Include when installing**, and **status** only—no long descriptions, install notes, or extra “Character Controller” explainer block.
- **Optional install progress window:** Closes automatically when installs finish; idle detection no longer treats post-reload indeterminate progress as “still active,” which could block auto-close.

## [0.1.13] - 2025-03-27

### Changed

- **First-run:** CCS Hub opens only via **`RequiredAutoInstallCompleted`** (removed duplicate code path). A **static** subscription handler prevents duplicate handlers when the domain reloads during installs.
- **First-run:** Opening the main Hub is **deferred** by two **`EditorApplication.delayCall`** steps so **CCS Branding** (and other required packages) can run **`InitializeOnLoad`** and show UI **before** the Hub window.

### Removed

- **CCS → Developer → Reset Setup State** menu (development-only).

## [0.1.12] - 2025-03-27

### Fixed

- **Optional Install selected** now prepends any **missing required** hub dependencies (CCS Branding, Input System, Cinemachine) **before** optional packages such as Character Controller. Unity cannot use Git URLs as nested `package.json` dependencies, so Branding is not a transitive dep of the controller package; the Hub installs it in order via `Client.Add`.

## [0.1.11] - 2025-03-27

### Fixed

- **Character Controller bootstrap (embedded tree):** The copy whitelist no longer includes top-level **`Runtime`** or **`Editor`** under `Assets/CCS/CharacterController`. Only **`Scripts`**, **`Content`**, **`Animations`**, and **`Samples~`** are copied from the embedded package layout, so **`Scripts/Runtime`** and **`Scripts/Editor`** are not duplicated by a second `Runtime/` / `Editor/` tree with the same asmdef names.

## [0.1.10] - 2025-03-27

### Fixed

- **Required auto-install:** No longer skips the Package Manager check when EditorPrefs said dependencies were already satisfied. A stale flag could skip **CCS Branding** (`com.crazycarrot.branding`) even though it was not installed. The hub now always compares `EnumerateAutoRequiredDefinitions()` to the live package list; if anything is missing, it clears the satisfied flag and queues **Branding**, **Input System**, and **Cinemachine** as needed.

## [0.1.9] - 2025-03-27

### Fixed

- **Character Controller bootstrap:** When the package uses embedded **`Assets/CCS/CharacterController/Scripts/Runtime`** (and **Scripts/Editor**), the Hub no longer copies **package-root** `Runtime` / `Editor` into **`Assets/CCS/CharacterController/`**, which previously produced duplicate `CCS.CharacterController.*` assemblies.

## [0.1.8] - 2025-03-27

### Added

- **Optional tools install UX:** choosing **Install selected** in CCS Hub closes the setup window and opens a **CCS Hub — Installing** utility window with progress feedback and copy such as **Installing {package display names}** until Package Manager work and Character Controller bootstrap (when applicable) finish.

## [0.1.7] - 2025-03-26

### Changed

- Character Controller bootstrap merges **`Plugins`** and **`Resources`** from the package into **`Assets/Plugins`** and **`Assets/Resources`** (siblings of **`Assets/CCS`**), not under **`Assets/CCS/CharacterController`**. CCS content stays only under **`Assets/CCS/CharacterController`**.

## [0.1.6] - 2025-03-26

### Fixed

- Character Controller bootstrap **no longer copies the entire Git package** into `Assets/CCS/CharacterController`. Repos that embed a dev project (Starter Assets, nested `Assets/`, `Packages/`, `ProjectSettings/`, etc.) were causing GUID conflicts, duplicate assemblies, and template folders under CCS. The Hub now copies **whitelisted** folders only—prefer **`Assets/CCS/CharacterController/{Scripts,Content,…}`** when present, otherwise **package-root** `Runtime`/`Editor`/`Content`/…—and **supplements** missing folders from the package root when the embedded tree omits them.

## [0.1.5] - 2025-03-26

### Changed

- **CCS Character Controller** optional install now uses **Package Manager** (`com.crazycarrot.charactercontroller` Git URL) so sources land under **`Packages/`** first, then **CCS Hub** copies/bootstraps them into **`Assets/CCS/CharacterController`** and removes the UPM dependency to avoid duplicate script compilation. Replaces the previous GitHub **zip-only** import.
- Added **`CCSCharacterControllerAssetsBootstrap`** and **`CCSAssetFolderCopyUtility`**; removed **`CCSCharacterControllerAssetsImportService`** (zip download) and folded editor-load sample materialization into the bootstrap type.
- **README** shows the package **version** at the top for quick verification against `package.json`.

## [0.1.3] - 2025-03-26

### Fixed

- Removed root `Documentation~.meta` and `Samples~.meta` so Unity does not warn about missing tilde folders when the package is installed from Git (folder metas come from child `README` assets).
- Character Controller GitHub zip import tries **`main`**, then **`master`**, when the first archive returns **404** (repos using either default branch).

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
