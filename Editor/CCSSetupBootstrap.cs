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

        #region Private Methods

        private static void OnEditorDelayCall()
        {
            CCSPackageStatusService.RefreshInstalledPackages(() =>
            {
                if (CCSSetupState.ShouldAutoOpenSetupWizard())
                {
                    CCSSetupProgressWindow.ShowRequiredPhase();
                }

                CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();
            });
        }

        #endregion
    }
}
