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
        public static void RunFirstRunPipelineNow()
        {
            CCSEditorLog.Info("CCS Hub: RunFirstRunPipelineNow — refreshing installed package list…");
            CCSPackageStatusService.RefreshInstalledPackages(ExecuteFirstRunPipelineAfterListReady);
        }

        #endregion

        #region Private Methods

        private static void OnEditorDelayCall()
        {
            CCSEditorLog.Info("CCS Hub: Bootstrap — editor delayCall (InitializeOnLoad first-run pipeline).");
            RunFirstRunPipelineNow();
        }

        private static void ExecuteFirstRunPipelineAfterListReady()
        {
            CCSSetupOrchestrator.EnsureInitialized();
            CCSEditorLog.Info("CCS Hub: Bootstrap — package list ready.");
            CCSSetupState.TryRecoverStaleFirstRunAutoOpenSessionStateIfNoHubWindow();

            CCSSetupState.ShouldAutoOpenMainHubAfterRequiredPhase(out string gateReason);
            CCSEditorLog.Info(
                $"CCS Hub: First-run auto-open gate (for next required pass): {(gateReason == null ? "ALLOW" : "BLOCK (" + gateReason + ")")}. "
                + $"installQueueBusy={CCSPackageInstallService.IsBusy()}.");

            CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
        }

        #endregion
    }
}
