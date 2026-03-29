# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.32] - 2026-03-28

### Fixed

- **First-run regression:** Automatic pipeline no longer bails out only because `SetupCompleted` / `SetupSkipped` EditorPrefs are set. `ShouldSkipAutomaticFirstRunPipeline()` now requires a **successful Package Manager list**, **no missing auto-required manifest packages**, and only then honors skip/completed. If anything required is missing, the pipeline runs.
- **Stale wizard state:** After a successful package list refresh, `ExecuteFirstRunPipelineAfterListReady()` calls **`TryRecoverStaleWizardStateIfRequiredPackagesMissing`**, which clears completed/skipped + required-deps-satisfied prefs and session auto-open flags when the manifest says required packages are still missing, then runs **`TryScheduleAutoInstall()`** (no early return on completed/skipped).
- **Restored required queue UI:** `RequestRequiredProgressUiForRestoredAutoRequiredQueue()` shows the required progress window whenever the list is unusable or any auto-required package is missing — not blocked by stale completed/skipped alone.

### Changed

- **Diagnostics:** When the automatic pipeline is skipped legitimately, an **Info** log records reason plus `setupCompleted`, `setupSkipped`, pending queue, pending Hub auto-open, `autoOpenedThisSession`, list readiness, and `missingRequired`.

## [0.2.30] - 2026-03-28

### Changed

- **Logging:** Reduced **Info**-level console noise for normal operation (auto-open, package list success, required phase, EditorPrefs changes, install queue / Client.Add chatter, Character Controller & DOTween & folder bootstrap success). **Warning** / **Error** retained for real issues. **`CCSSetupState.LogFirstRunStateSnapshot`** still emits a full **Info** dump when invoked explicitly.
- **Hub window copy:** Skip path status line simplified; optional “nothing left to install” path no longer logs completion lines (outcome is already clear in the UI).

## [0.2.29] - 2026-03-28

### Fixed

- **First-run reopen after Skip / optional completion:** `ShouldSkipAutomaticFirstRunPipeline()` now treats **`IsSetupSkipped()`** as a hard skip (previously only `IsSetupCompleted()` gated the automatic pipeline, so **Skip for now** still re-ran required progress after `AssemblyReloadEvents` / editor stable bootstrap). `ExecuteFirstRunPipelineAfterListReady` also no-ops if setup is completed or skipped.

### Changed

- **Progress UI:** Required and optional **Package Manager** rows use **manifest/batch order** so only one row is non-terminal at a time (others show **Pending** until the previous step finishes); DOTween row stays **Pending** until optional batch rows are complete.
- **CCSSetupWindow:** Clearer subtitle, spacing, `helpBox` optional rows, separator + footer actions (Install Selected / Skip for now).
- **Removed** `Tools → CCS Hub (Internal) → Reset first-run state…` (**`CCSSetupDevReset`** menu) from the shipping package.

## [0.2.28] - 2026-03-28

### Changed

- **Required → Hub transition:** Completion banner (**Finished installing required items** / **→ Opening CCS Hub…**) stays visible for **~1 second** (`EditorApplication.timeSinceStartup`), then **`RequiredAutoInstallCompleted`** runs (no close-before-continuation). **CCSSetupOrchestrator** opens the Hub **before** closing the progress window so the handoff feels sequential, not flickery. Optional completion path unchanged.

## [0.2.27] - 2026-03-28

### Fixed

- **Automatic first-run bootstrap:** The pipeline no longer relies on a single `EditorApplication.delayCall` immediately after `[InitializeOnLoad]`. It now waits until **`EditorApplication.isCompiling`** is false (with a bounded timeout), and **re-schedules after `AssemblyReloadEvents.afterAssemblyReload`** so Git URL installs survive compilation/domain reload noise. Internal reset still uses **`RunFirstRunPipelineNow(true)`** unchanged (forced path, no stability wait).

## [0.2.26] - 2026-03-28

### Fixed

- **Editor assembly reference:** `CCS.Hub.Editor.asmdef` now references **`CCS.Hub.Runtime` by assembly name** instead of a **GUID**. GUID-based references break when the runtime `.meta` GUID differs (mirrored repos, partial copies, reimport). A broken reference prevents the Editor assembly from compiling, which disables `[InitializeOnLoad]` bootstrap, **CCS** menu items, internal **Tools** menu, progress UI, and editor logs.

## [0.2.25] - 2026-03-27

### Fixed

- **Package Manager list failure:** `Client.List` failure no longer sets `listReady` or pretends the installed set is valid. Added `IsLastPackageListRefreshFailed()` / `IsListRefreshInProgress()`; required evaluation aborts with **Error** instead of treating every package as missing. Install queue dequeue path stops spinning when the list stays broken.

### Changed

- **First-run bootstrap:** Cheap early-out on editor load when setup is **completed** and there is no pending install queue, pending Hub auto-open, or busy install service (`RunFirstRunPipelineNow(forceRun: false)`). Internal reset / forced rerun uses **`RunFirstRunPipelineNow(true)`** so the pipeline always runs after clearing state.
- **Required bootstrap:** Re-entry guard (`requiredBootstrapCycleActive`) prevents overlapping `TryScheduleAutoInstall` runs until `RequiredAutoInstallCompleted` clears the cycle; reset via **`ResetRequiredBootstrapCycleGuard()`** from `ResetAllFirstRunStateForThisProject`.
- **Restore vs UI:** `CCSPackageInstallService` no longer calls `ShowRequiredPhase` directly after domain reload; it calls **`CCSHubRequiredDependencyBootstrap.RequestRequiredProgressUiForRestoredAutoRequiredQueue()`** so required progress UI is owned in one place.
- **`CCSSetupProgressWindow`:** Required-phase rows use **`EnumerateRequiredDefinitionsForProgress`**, **`GetStatusForRequiredRow`**, and **`DrawDefinitionRowForRequiredPhase`** (optional batch still uses `DrawDefinitionRow`).
- **Removed** temporary **`CCSSetupDiagnosticTrace`** (always-on QA logging).

## [0.2.24] - 2026-03-27

### Added

- **Temporary Phase 1 QA diagnostics:** `CCSSetupDiagnosticTrace` (`CCS Hub DIAG:` prefix via `Debug.Log`) traces Bootstrap → PM list refresh → `TryScheduleAutoInstall` → `ShowRequiredPhase` → completion. One `Debug.LogWarning` banner reminds you to show regular Logs in the Console. Set `CCSSetupDiagnosticTrace.Enabled = false` (or remove calls) after debugging.

## [0.2.23] - 2026-03-27

### Changed

- **Phase 1 required flow:** `CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall` now opens the required progress window once at the start of evaluation (when first-run setup is not completed/skipped), removes duplicate `ShowRequiredPhase` calls on the enqueue and zero-missing branches, and uses a single completion callback before `RequiredAutoInstallCompleted`. Production logging: one **Info** when the required phase starts and one **Info** when it completes (only when that UI path ran); removed the prior subscriber-invocation debug line.
- **Bootstrap / orchestrator:** Fewer **Info** lines on the first-run pipeline and Hub auto-open path (`CCSSetupBootstrap`, `CCSSetupOrchestrator`); keep skip/cancel **Info** and real **Warning**/**Error** messages.

## [0.2.22] - 2026-03-27

### Added

- **Internal testing:** `Tools → CCS Hub (Internal) → Reset first-run state for testing…` — clears Hub EditorPrefs/SessionState/install pipeline markers and reruns bootstrap (not under the public CCS menu).

### Changed

- **CCSSetupWindow:** Minimal Hub UI — optional toggles, one-line manifest descriptions, DOTween row, **Install Selected** / **Skip for now** only; no install progress bar, required-summary foldouts, or long help boxes.
- **CCSSetupProgressWindow:** Required completion copy **Finished installing required items** / **→ Opening CCS Hub…**; optional completion **✔ Finished** then **~1s** hold before close; installing rows show **batch %** when Package Manager reports progress; optional phase calls **`Show()`** explicitly.
- **CCSSetupOrchestrator:** Reduced verbose **Info** dumps on the success path.

## [0.2.21] - 2026-03-27

### Fixed

- **Required progress before Hub (0-missing case):** When all required packages are already installed, **`ShowRequiredPhase()`** runs first, then **`RequiredAutoInstallCompleted`** is scheduled on the **next** `delayCall` so the window is never in the same frame as Hub auto-open.
- **Queue path:** **`ShowRequiredPhase()`** always runs immediately after **`EnqueueAutoRequiredDefinitions`** (not only when `IsBusy()`).

### Changed

- **Progress UI:** Row status column uses **[✔] / [●] / [ ] / [✖]** with labels; required completion shows **✔ Required installs complete** and **→ Opening CCS Hub…**; optional completion shows **✔ Optional installs complete**; optional completion uses a **two-step delayCall** before close (same idea as required).
- **README:** Locked to minimal dev-facing copy only.

## [0.2.20] - 2026-03-27

### Fixed

- **Required completion banner timing:** `NotifyRequiredPassCompleteThenRun` now uses **two** `EditorApplication.delayCall` steps (and `Show`/`Focus`/`Repaint` on the window) so **“Required installs complete” / “Opening CCS Hub…”** can render before the window closes and `RequiredAutoInstallCompleted` runs.

### Changed

- **README:** Minimal dev-facing text only (no repo URL line or extra detail).

## [0.2.19] - 2026-03-27

### Added

- **Unified setup progress window:** `CCSSetupProgressWindow` is the single EditorWindow for **required** and **optional** installs (`SetupMode.RequiredSetup` / `SetupMode.OptionalSetup`). Required mode lists every manifest required package with **Installed / Pending / Installing / Failed**, determinate Package Manager batch bar when available (otherwise reload/indeterminate), highlights the active row, then shows **Required installs complete** / **Opening CCS Hub…** before closing and firing `RequiredAutoInstallCompleted`. Optional mode lists only the current batch (session-stored definition ids + DOTween row when selected), same statuses, then **Optional installs complete** before closing.

### Removed

- **`CCSRequiredInstallProgressWindow`:** behavior merged into `CCSSetupProgressWindow`.

### Documentation

- **README:** Short dev-facing version only (install URL, behavior, manual open, optional installs, requirements, notes).

## [0.2.18] - 2026-03-27

### Added

- **`CCSRequiredInstallProgressWindow`:** Non-modal EditorWindow shown while the automatic required UPM queue runs; updates from `CCSPackageInstallService` state; closes on the same `delayCall` **before** `RequiredAutoInstallCompleted` is invoked (and again defensively before opening the main Hub). Re-shown after domain reload when the auto-required queue is restored from session state.

### Changed

- **Production logging:** Removed first-run `Debug.LogWarning` spam (`CCS HUB FLOW`); retained `CCSEditorLog` / errors / gate snapshots for real blocks. Removed stack traces from `MarkAutoOpenedThisSession` and `SetPendingHubAutoOpenAfterRequiredPhase`. One **Info** line per editor session when first-run Hub auto-open runs: `CCS Hub auto-open triggered (first-run).` Stale session recovery logs as **Info** (not Warning).

### Documentation

- **README:** Short onboarding version (install URL, flow, manual open, optional installs, requirements, notes).

## [0.2.17] - 2026-03-27

### Fixed

- **Stale `pendingHubAutoOpenAfterRequiredPhase`:** Bootstrap now calls **`TryRecoverStaleFirstRunAutoOpenSessionStateIfNoHubWindow()`**, which clears both **`autoOpenedThisSession`** and **`pendingHubAutoOpenAfterRequiredPhase`** when they are set but **no** `CCSSetupWindow` exists and setup is not completed/skipped — fixing blocks where `TryBeginFirstRunHubAutoOpen` reported “pending already scheduled” after a reload/interrupted delayCall.

### Added

- **`SetPendingHubAutoOpenAfterRequiredPhase(true)`:** temporary **`Environment.StackTrace`** log (same idea as `MarkAutoOpenedThisSession`).
- **`ClearPendingHubAutoOpenAfterRequiredPhase()`:** warning log with **`hadPending`** so clears are visible in the Console.

## [0.2.16] - 2026-03-27

### Fixed

- **Stale / early `autoOpenedThisSession`:** `MarkAutoOpenedThisSession()` is no longer called from `CCSSetupOrchestrator.OpenMainHubAfterRequiredPhase` before the window is shown. It runs **only from `ShowOrFocusFirstRunAuto()` after `window.Show()`** succeeds, so a failed or skipped presentation cannot leave the session flag set without a real Hub show.
- **Bootstrap recovery:** On first-run pipeline start (`ExecuteFirstRunPipelineAfterListReady`), if `autoOpenedThisSession` is true, **no** `CCSSetupWindow` exists, and setup is not completed/skipped, the flag is **cleared** as stale (e.g. leftover SessionState after a closed editor or aborted open).

### Added

- **`MarkAutoOpenedThisSession`:** `Debug.LogWarning` includes **`Environment.StackTrace`** (temporary diagnostic) to identify callers.
- **Bootstrap:** `BOOTSTRAP STARTUP STATE` line logs `autoOpenedThisSession`, `pendingHubAutoOpen`, `setupCompleted`, `setupSkipped` before gate/required-deps work.
- **`ShowOrFocusFirstRunAuto`:** Logs existing instance vs new, before/after `Show()`, and inside the delayed Focus/Repaint callback.

## [0.2.15] - 2026-03-27

### Fixed

- **First-run Hub auto-open:** Opens **as soon as CCS Branding’s** `Client.Add` **succeeds** (`PackageInstallSucceeded`), while Input System / Cinemachine can keep installing. **Fallback:** the same scheduling path runs when **`RequiredAutoInstallCompleted`** fires (all required already present, or queue fully drained). **Idempotent:** if Branding already scheduled the open, the later required-pass completion **does not** open a second Hub (`autoOpenedThisSession` / pending flag / single window reuse unchanged).

### Added

- **Diagnostics:** `CCSSetupConstants.HubFlowDiagnosticPrefix` (`CCS HUB FLOW >>>`) — `Debug.LogWarning` lines across bootstrap, required-deps bootstrap, install success / queue empty, orchestrator (gate, schedule, open), `MarkAutoOpenedThisSession` / pending flag, and `ShowOrFocusFirstRunAuto` / duplicate close — so the pipeline is visible in the Console without filtering Info.

## [0.2.14] - 2026-03-27

### Fixed

- **Duplicate Hub windows:** Manual **Open CCS Hub** and orchestrated **first-run auto-open** now share **`AcquireHubWindowForReuse()`** — focus/bring forward an existing `CCSSetupWindow`, or create one; stray duplicate instances are closed. **`ShowOrFocusFromMenu()`** / **`ShowOrFocusFirstRunAuto()`** replace always calling **`GetWindow`** for a second dockable instance.

## [0.2.13] - 2026-03-27

### Fixed

- **Manual Open CCS Hub** no longer calls **`MarkAutoOpenedThisSession()`**. That session flag is only set by the **orchestrated first-run auto-open** path (`CCSSetupOrchestrator`), so opening the Hub from the menu no longer blocks automatic first-run open later in the same session.

### Removed

- **Debug / test menu items** under **CCS → CCS Hub** (**Reset first-run setup state**, **Force run first-run pipeline now**, **Dump setup state to Console**). The public menu now exposes only **Open CCS Hub**; **`ResetAllFirstRunStateForThisProject`**, **`RunFirstRunPipelineNow`**, and **`LogFirstRunStateSnapshot`** remain available from code for internal tooling if needed.

## [0.2.12] - 2026-03-27

### Added

- **`CCSSetupMenuCommands`:** **CCS → CCS Hub → Open CCS Hub**, **Reset first-run setup state (this project)**, **Force run first-run pipeline now**, **Dump setup state to Console**.
- **`CCSPackageInstallService.ResetPipelineStateForFirstRunStateReset`:** clears queued installs and session queue markers (does not cancel an in-flight `Client.Add`; warns if one is active).
- **`CCSSetupState.ResetAllFirstRunStateForThisProject`:** blank slate for this project — EditorPrefs (`SetupCompleted`, `SetupSkipped`, required-deps satisfied/summary, optional DOTween toggle), Hub SessionState (auto-open, pending auto-open, queue ids, auto-required pass, optional-install tracking, DOTween copy pending), optional-install context, then install pipeline reset. Logs a full snapshot after reset when invoked from the menu path before **`RunFirstRunPipelineNow`**.
- **`CCSSetupState.BuildFirstRunStateDump` / `LogFirstRunStateSnapshot`:** one scan-friendly block listing all relevant EditorPrefs and SessionState plus package-list readiness, install-queue busy, and auto-open gate result.

### Changed

- **Deterministic first-run auto-open:** **`ShouldAutoOpenMainHubAfterRequiredPhase`** — blocks when **`setupCompleted`**, **`setupSkipped`**, or **`autoOpenedThisSession`** (EditorPrefs + SessionState as documented). **`SessionStatePendingHubAutoOpenAfterRequiredPhase`** is set when scheduling the post-required Hub open and cleared when the window is shown.
- **`CCSSetupOrchestrator`:** structured logs before/after gate, **`RequiredAutoInstallCompleted`** invocation log, stable-editor wait log, Hub show confirmation dump.
- **`CCSHubRequiredDependencyBootstrap`:** logs missing required count, queue vs already-present, and delayCall before invoking subscribers.
- **`CCSSetupBootstrap`:** logs delayCall start, **`RunFirstRunPipelineNow`**, package list ready, and gate line.
- **Restore `SetupCompleted` / `SetupSkipped`** persistence for production: optional flow and **Skip for now** set flags again; later editor sessions do not auto-open the Hub once setup is finished or skipped (use **Reset** to test again).

### Fixed

- **First-run reset + immediate rerun** uses the **same** code path as editor load (**`CCSSetupBootstrap.RunFirstRunPipelineNow`**), not a separate test-only pipeline.

## [0.2.11] - 2026-03-27

### Removed

- **`CCSSetupMenuItems`** and the **Reset first-run** menu entry (temporary simplification).

### Changed

- **First-run auto-open** used **only** `SessionState` (one main Hub auto-open per editor session). **`SetupCompleted` / `SetupSkipped`** EditorPrefs were removed from the auto-open gate.
- **CCS → CCS Hub** called **`MarkAutoOpenedThisSession()`** when opening from the menu so a parallel auto-open did not spawn a second window.

## [0.2.10] - 2026-03-27

### Fixed

- **Hub never opened / no Console logs:** `RequiredAutoInstallCompleted` could fire before `CCSSetupOrchestrator` subscribed (static ctor order). **CCSSetupOrchestrator.EnsureInitialized()** now runs from `CCSSetupBootstrap` (first) and from `CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall` before any install scheduling, so the handler is always registered.
- **Diagnostics:** Every completion invokes **CCSEditorLog.Info** (`RequiredAutoInstallCompleted received.`). Blocked auto-open uses **Debug.LogWarning** so it appears even when only Warnings are visible.

## [0.2.9] - 2026-03-27

### Changed

- **First-run UX:** The separate **CCS Hub — Setup** required-phase window is no longer shown on bootstrap. Required packages still queue automatically; the **main CCS Hub** window opens by itself after **all** required Package Manager installs for that pass complete. Optional installs still use the setup progress window when started from the Hub.

## [0.2.8] - 2026-03-27

### Fixed

- **Reset first-run:** After clearing flags, **`CCSSetupBootstrap.RunFirstRunPipelineNow()`** runs immediately (no Unity restart). **`SetSetupCompleted(false)`** / **`SetSetupSkipped(false)`** are applied after `DeleteKey` so the completed state cannot stick.
- **Console:** “Auto setup UI skipped” when `setupCompleted` is expected is now **Info** (not Warning) to reduce noise.

## [0.2.7] - 2026-03-27

### Added

- **Menu:** `CCS / CCS Hub / Reset first-run setup state (this project)` clears Hub EditorPrefs + session markers so you can test first-run again after **restarting Unity** (when `setupCompleted` was blocking auto UI).

### Changed

- **Bootstrap warning** when auto UI is skipped: explains that `setupCompleted=True` is normal after finishing setup once and points to the new reset menu command.

## [0.2.6] - 2026-03-27

### Fixed

- **`CCSSetupBootstrap`:** Use `UnityEditor.SessionState` (not `UnityEngine.SessionState`) in the first-run skip warning (CS0234).

## [0.2.5] - 2026-03-27

### Fixed

- **First-run Hub not opening / no logs:** `WaitForStableEditorThenOpenHub` no longer waits on `CCSPackageInstallService.IsBusy()`. That flag stays true while **any** required package remains queued, so the Hub never opened until the full batch finished (and callbacks could feel “silent”). The Hub now opens after **compilation** settles while Package Manager can still be working.

### Changed

- **Diagnostics:** `CCSSetupBootstrap` logs a **Warning** when first-run UI is skipped (with `setupCompleted` / `setupSkipped` / `autoOpenedThisSession`). `CCSSetupOrchestrator` logs **Info** when scheduling or opening the Hub.

## [0.2.4] - 2026-03-27

### Fixed

- **`CCSSetupOrchestrator`:** `CCSPackageDefinition` is a struct — do not compare to `null` (CS0019); guard with `string.IsNullOrEmpty(definition.Id)` instead.

## [0.2.3] - 2026-03-27

### Fixed

- **First-run Hub not opening:** `CCSSetupOrchestrator` no longer waits on `EditorApplication.isUpdating` (it often stays true during normal editor frames and could reschedule `delayCall` forever). Only `isCompiling` and the Package Manager busy check gate the open.
- **Hub after Branding:** Subscribes to `CCSPackageInstallService.PackageInstallSucceeded` for **ccs-branding** so the optional CCS Hub window can open as soon as Branding’s `Client.Add` succeeds (Cinemachine / Input System can continue in the background).

### Changed

- **Diagnostics:** When auto-open is skipped, the Console logs `setupCompleted`, `setupSkipped`, and `autoOpenedThisSession` (unless the hub already opened this session, to avoid noise). `IsSetupCompleted` / `ShouldAutoOpenSetupWizard` docstrings clarify defaults.

## [0.2.2] - 2026-03-27

### Fixed

- **`CCSSetupProgressWindow`:** `BuildOptionalStageText` is now an instance method so it can read `optionalSawActivity` (fixes CS0120).

## [0.2.1] - 2026-03-27

### Fixed

- **`CCSPackageInstallDefinition.cs.meta`:** Corrected Unity asset GUID to **32** hexadecimal characters (was 33), which caused Unity to ignore the script so `CCSPackageInstallDefinition` was missing at compile time.

## [0.2.0] - 2026-03-27

### Added

- **Manifest-driven registry:** `Runtime/Resources/CCSDependencyManifest.json` defines **required**, **optional**, and **catalog** packages; `CCSPackageInstallDefinition` rows deserialize via `CCSDependencyManifest` and map to `CCSPackageDefinition`. A **legacy fallback** in `CCSPackageRegistry` applies if the manifest is missing or invalid.
- **CCSSetupOrchestrator:** After required UPM installs finish and the editor is stable (not compiling/updating, queue idle), first-run opens the **CCS Hub** optional UI automatically.
- **CCSSetupProgressWindow:** Unified first-run progress UI (required and optional phases) with item name, **Required/Optional** tier, status (**Pending / Installing / Installed / Failed / Skipped**), stage text, overall batch counts, post-reload banner, and **Retry** for failed required packages.
- **Install service:** `IsSkipped` for dequeue skips (already installed), `RetryFailedDefinition` for per-package retry with logging.

### Changed

- **First-run flow:** `CCSSetupBootstrap` opens **CCSSetupProgressWindow** immediately when auto-setup applies, then queues manifest-driven required installs **one at a time** (existing `CCSPackageInstallService` behavior). The main Hub window opens **after** the full required pass completes and Unity is stable—not mid-queue after Branding only.
- **CCSSetupWindow:** Optional installs use **CCSSetupProgressWindow** (optional phase); **Setup complete** is shown when `EditorPrefs` reports completion. Status labels include **Skipped** when applicable.

### Removed

- **CCSHubRequiredInstallProgressWindow** and **CCSHubOptionalInstallProgressWindow** (replaced by **CCSSetupProgressWindow**).

## [0.1.27] - 2026-03-27

### Changed

- **First-run Hub open:** The main CCS Hub window now opens **as soon as CCS Branding** (`com.crazycarrot.branding`) **finishes installing** via the required auto-install queue (instead of on the first editor tick before Branding completes). If Branding is **already** present, the Hub still opens on the next tick for first-run projects. Input System and Cinemachine may continue installing afterward.

## [0.1.26] - 2026-03-27

### Changed

- **First-run flow:** After adding the Hub via Git URL, **required** packages (CCS Branding, Input System, Cinemachine) still **queue and install automatically** on load. The **main CCS Hub** window now opens **on the next editor tick** (no blocking “required packages only” modal first), so optional Character Controller / DOTween choices are visible **while** required installs run. Progress for required packages appears in the Hub (active package name + bar). **Mark setup complete** was removed; completion still happens automatically when your optional install pass finishes (or via **Skip for now**).

## [0.1.25] - 2026-03-27

### Fixed

- **Hub window:** After optional installs finish (or when setup completes with no Package Manager batch), any remaining **CCS Hub** editor windows are closed via `CCSSetupWindow.CloseAllInstances()` so the Hub does not stay open in the background.
- **Character Controller bootstrap:** After copy, reimport order is **Armature.fbx** → **`Armature.prefab`** (non-nested prefab) → optional **`CCS_Player_TestingRobot.prefab`** when present.

## [0.1.24] - 2026-03-27

### Fixed

- **Optional install progress:** `CCSHubOptionalInstallProgressWindow` no longer builds `GUIStyle` from `EditorStyles` in `OnEnable` (fixes `NullReferenceException` on `EditorStyles.boldLabel` in some editor load orders). User-facing step counts (**Optional setup 0 / 2**) now reflect Character Controller + DOTween selections; status text shows the active phase (Package Manager vs import). Character Controller bootstrap **reimports** `Armature.fbx` before `CCS_Player_TestingRobot.prefab` so nested prefab references resolve after copy.

## [0.1.23] - 2026-03-27

### Fixed

- **`CCSDotweenBundleInstaller`:** `IsDemigiantDotweenPresentInProject` used a typo (`demiant` instead of `demigiant`), which broke compilation.

## [0.1.22] - 2026-03-27

### Documentation

- **README:** Version banner and tag examples aligned with `package.json` (was still showing 0.1.19 on GitHub).

## [0.1.21] - 2026-03-27

### Changed

- **Release:** Version bump for UPM testing alongside **CCS Character Controller 0.1.7** (no functional change from 0.1.20).

## [0.1.20] - 2026-03-27

### Fixed

- **Required installs on Hub load:** `CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall()` runs after Package Manager list refresh **even when the first-run wizard does not open**, so Branding / Input System / Cinemachine still queue whenever `com.crazycarrot.hub` is present.
- **Hub UI:** The main Hub again shows **Character Controller** and **DOTween** sections (`DrawDotweenOptionalSection` wired). **Install** with no Package Manager work (already imported CC, DOTween-only copy, or nothing left to do) now **marks setup complete and closes** the Hub; closing the Hub alone no longer marks setup complete.
- **Character Controller import:** Package bootstrap **no longer copies** a top-level **`Tests`** folder from the UPM package into `Assets/CCS/CharacterController`.

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
