// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: On import, shows first-run progress UI and queues manifest-driven required UPM installs. CCSSetupOrchestrator opens the CCS Hub optional UI after required installs finish and the editor is stable.
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
            EditorApplication.delayCall += OnEditorDelayCall;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Re-runs the same first-run path as editor load: refresh Package Manager list, optionally show progress, queue required installs.
        /// Call after <see cref="CCSSetupState.ResetAllSetupFlagsForDevelopment"/> so a Unity restart is not required.
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
            if (CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                CCSEditorLog.Info("CCS Hub: First-run bootstrap — showing setup progress and queueing required packages.");
                CCSSetupProgressWindow.ShowRequiredPhase();
            }
            else
            {
                CCSEditorLog.Info(
                    $"CCS Hub: Auto setup UI skipped (setupCompleted={CCSSetupState.IsSetupCompleted()}, setupSkipped={CCSSetupState.IsSetupSkipped()}, "
                    + $"autoOpenedThisSession={SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false)}). "
                    + "Expected after you finished setup once. Use CCS → CCS Hub to open manually, or "
                    + $"{CCSSetupConstants.MenuPathResetFirstRunSetup} to clear flags and run the pipeline again without restarting.");
            }

            CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
        }

        #endregion
    }
}
