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
        #region Variables

        private static bool diagnosticBannerLogged;

        #endregion

        #region Unity Callbacks

        static CCSSetupBootstrap()
        {
            if (!diagnosticBannerLogged)
            {
                diagnosticBannerLogged = true;
                CCSSetupDiagnosticTrace.LogTraceBannerOnce();
            }

            CCSSetupDiagnosticTrace.Log("Bootstrap static ctor (InitializeOnLoad — assembly loaded)");
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
            CCSSetupDiagnosticTrace.Log("Bootstrap RunFirstRunPipelineNow (requesting Package Manager list refresh)");
            CCSPackageStatusService.RefreshInstalledPackages(ExecuteFirstRunPipelineAfterListReady);
        }

        #endregion

        #region Private Methods

        private static void OnEditorDelayCall()
        {
            CCSSetupDiagnosticTrace.Log("Bootstrap OnEditorDelayCall (first-run pipeline)");
            RunFirstRunPipelineNow();
        }

        private static void ExecuteFirstRunPipelineAfterListReady()
        {
            CCSSetupDiagnosticTrace.Log("Bootstrap ExecuteFirstRunPipelineAfterListReady (PM list callback)");
            CCSSetupOrchestrator.EnsureInitialized();
            CCSSetupDiagnosticTrace.LogSetupGateSnapshot();
            CCSSetupState.TryRecoverStaleFirstRunAutoOpenSessionStateIfNoHubWindow();
            CCSSetupDiagnosticTrace.Log("Bootstrap invoking TryScheduleAutoInstall");
            CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
        }

        #endregion
    }
}
