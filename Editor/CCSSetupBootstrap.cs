// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: On import, queues manifest-driven required UPM installs. CCSSetupOrchestrator opens the main CCS Hub window after required installs complete (no separate required progress window).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

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
        /// Re-runs the same first-run path as editor load: refresh Package Manager list and queue required installs (main Hub opens when the pass completes if it has not been opened this session).
        /// </summary>
        public static void RunFirstRunPipelineNow()
        {
            CCSPackageStatusService.RefreshInstalledPackages(ExecuteFirstRunPipelineAfterListReady);
        }

        #endregion

        #region Private Methods

        private static void OnEditorDelayCall()
        {
            RunFirstRunPipelineNow();
        }

        private static void ExecuteFirstRunPipelineAfterListReady()
        {
            CCSSetupOrchestrator.EnsureInitialized();
            if (CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                CCSEditorLog.Info(
                    "CCS Hub: First-run bootstrap — queueing required packages. Main CCS Hub will open automatically when that pass finishes.");
            }
            else
            {
                CCSEditorLog.Info(
                    "CCS Hub: Auto setup UI skipped (Hub already opened this session or marked). "
                    + $"autoOpenedThisSession={SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false)}. "
                    + "Use CCS → CCS Hub to open the window manually.");
            }

            CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
        }

        #endregion
    }
}
