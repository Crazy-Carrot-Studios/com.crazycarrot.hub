// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2025
// Summary: On import, shows required-install progress UI, runs auto-installs, then opens the main CCS Hub when complete (after a short defer so CCS Branding can show its UI first).
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
                if (!CCSSetupState.ShouldAutoOpenSetupWizard())
                {
                    return;
                }

                CCSHubRequiredInstallProgressWindow.ShowForFirstRun();

                CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted -= OnRequiredAutoInstallCompletedForFirstRun;
                CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted += OnRequiredAutoInstallCompletedForFirstRun;

                CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();

                if (!CCSSetupState.ShouldAutoOpenSetupWizard())
                {
                    CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted -= OnRequiredAutoInstallCompletedForFirstRun;
                    CCSHubRequiredInstallProgressWindow.CloseForFirstRun();
                }
            });
        }

        /// <summary>
        /// Fired when the required-dependency pass finishes (queue drained or all already present).
        /// Uses a static handler so domain reload does not accumulate duplicate subscriptions.
        /// </summary>
        private static void OnRequiredAutoInstallCompletedForFirstRun()
        {
            CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted -= OnRequiredAutoInstallCompletedForFirstRun;

            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                return;
            }

            ScheduleOpenHubAfterBrandingCanShowFirst();
        }

        /// <summary>
        /// Defer opening CCS Hub until after the next editor update cycles so com.crazycarrot.branding (and other required packages)
        /// can run InitializeOnLoad and show their windows before the Hub appears.
        /// </summary>
        private static void ScheduleOpenHubAfterBrandingCanShowFirst()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += OpenFirstRunHubNow;
            };
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
