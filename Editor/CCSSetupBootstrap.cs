// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 28, 2026
// Summary: On import, same first-run path as after reset: refresh PM list → evaluate required deps → queue installs → orchestrator opens Hub when pass completes.
// Automatic startup waits for editor stability and retries after assembly reload so Git URL installs are not missed by a single early delayCall.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using CCS.Hub;
using UnityEditor;

namespace CCS.Hub.Editor
{
    [InitializeOnLoad]
    public static class CCSSetupBootstrap
    {
        #region Variables

        private static bool automaticStableWaitRegistered;

        private static int stableWaitFramesRemaining;

        /// <summary>Upper bound on how long we wait for compilation/import to finish before running the automatic pipeline once.</summary>
        private const int MaxStableWaitFrames = 3600;

        #endregion

        #region Unity Callbacks

        static CCSSetupBootstrap()
        {
            CCSSetupOrchestrator.EnsureInitialized();
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.delayCall += OnInitialEditorDelayCall;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Same entry point as editor load: refresh Package Manager list, then <see cref="CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall"/>.
        /// Use after <see cref="CCSSetupState.ResetAllFirstRunStateForThisProject"/> to rerun without restarting Unity.
        /// </summary>
        /// <param name="forceRun">When <c>true</c>, skips the idle early-out (internal reset / forced pipeline).</param>
        public static void RunFirstRunPipelineNow(bool forceRun = false)
        {
            if (!forceRun && ShouldSkipAutomaticFirstRunPipeline())
            {
                return;
            }

            CCSPackageStatusService.RefreshInstalledPackages(ExecuteFirstRunPipelineAfterListReady);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// After domain reload (new scripts, package resolve), defer automatic first-run — single delayCall is often too early during Git URL import.
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
            EditorApplication.delayCall += ScheduleAutomaticFirstRunWhenEditorStable;
        }

        /// <summary>First editor tick after Hub Editor assembly loads.</summary>
        private static void OnInitialEditorDelayCall()
        {
            ScheduleAutomaticFirstRunWhenEditorStable();
        }

        /// <summary>
        /// Waits until <see cref="EditorApplication.isCompiling"/> is false, then runs the automatic pipeline once.
        /// </summary>
        private static void ScheduleAutomaticFirstRunWhenEditorStable()
        {
            if (ShouldSkipAutomaticFirstRunPipeline())
            {
                return;
            }

            if (automaticStableWaitRegistered)
            {
                return;
            }

            automaticStableWaitRegistered = true;
            stableWaitFramesRemaining = MaxStableWaitFrames;
            EditorApplication.update += WaitUntilEditorStableForAutomaticPipeline;
        }

        private static void WaitUntilEditorStableForAutomaticPipeline()
        {
            stableWaitFramesRemaining--;

            if (ShouldSkipAutomaticFirstRunPipeline())
            {
                UnregisterAutomaticStableWait();
                return;
            }

            if (EditorApplication.isCompiling)
            {
                if (stableWaitFramesRemaining <= 0)
                {
                    CCSEditorLog.Warning(
                        "CCS Hub: Automatic first-run — stability wait timed out during compilation; running pipeline once.");
                    UnregisterAutomaticStableWait();
                    RunFirstRunPipelineNow(forceRun: false);
                }

                return;
            }

            UnregisterAutomaticStableWait();
            RunFirstRunPipelineNow(forceRun: false);
        }

        private static void UnregisterAutomaticStableWait()
        {
            EditorApplication.update -= WaitUntilEditorStableForAutomaticPipeline;
            automaticStableWaitRegistered = false;
            stableWaitFramesRemaining = MaxStableWaitFrames;
        }

        /// <summary>
        /// Skips PM refresh and required evaluation when the project is already past first-run and nothing Hub-related is pending.
        /// </summary>
        private static bool ShouldSkipAutomaticFirstRunPipeline()
        {
            // Skip / finished Hub — never re-run automatic required flow (fixes reopen after "Skip for now" or optional completion).
            if (CCSSetupState.IsSetupSkipped())
            {
                return true;
            }

            if (!CCSSetupState.IsSetupCompleted())
            {
                return false;
            }

            if (CCSPackageInstallService.IsBusy())
            {
                return false;
            }

            if (CCSSetupState.IsPendingHubAutoOpenAfterRequiredPhase())
            {
                return false;
            }

            string pendingQueue = SessionState.GetString(CCSSetupConstants.SessionStatePendingInstallQueueIds, string.Empty);
            if (!string.IsNullOrWhiteSpace(pendingQueue))
            {
                return false;
            }

            return true;
        }

        private static void ExecuteFirstRunPipelineAfterListReady()
        {
            if (CCSSetupState.IsSetupSkipped() || CCSSetupState.IsSetupCompleted())
            {
                return;
            }

            CCSSetupOrchestrator.EnsureInitialized();
            CCSSetupState.TryRecoverStaleFirstRunAutoOpenSessionStateIfNoHubWindow();
            CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
        }

        #endregion
    }
}
