// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: On import, queues required UPM installs (Branding, Input System, Cinemachine). When first-run applies, opens CCS Hub on the next editor tick so optional installs are visible while required packages install in the background (no blocking required-only modal).
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
                CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();

                if (!CCSSetupState.ShouldAutoOpenSetupWizard())
                {
                    return;
                }

                EditorApplication.delayCall += OpenFirstRunHubNow;
            });
        }

        private static void OpenFirstRunHubNow()
        {
            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                return;
            }

            CCSSetupState.MarkAutoOpenedThisSession();
            CCSHubRequiredInstallProgressWindow.CloseForFirstRun();
            CCSSetupWindow.ShowFirstRunAuto();
        }

        #endregion
    }
}
