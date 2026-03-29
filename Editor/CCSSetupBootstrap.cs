// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: On import, same first-run path as after reset: refresh PM list → evaluate required deps → queue installs → orchestrator opens Hub when pass completes.
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
        #region Unity Callbacks

        static CCSSetupBootstrap()
        {
            CCSSetupOrchestrator.EnsureInitialized();
            EditorApplication.delayCall += OnEditorDelayCall;
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
        /// Skips PM refresh and required evaluation when the project is already past first-run and nothing Hub-related is pending.
        /// </summary>
        private static bool ShouldSkipAutomaticFirstRunPipeline()
        {
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

        private static void OnEditorDelayCall()
        {
            RunFirstRunPipelineNow(forceRun: false);
        }

        private static void ExecuteFirstRunPipelineAfterListReady()
        {
            CCSSetupOrchestrator.EnsureInitialized();
            CCSSetupState.TryRecoverStaleFirstRunAutoOpenSessionStateIfNoHubWindow();
            CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
        }

        #endregion
    }
}
