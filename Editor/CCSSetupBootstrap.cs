// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: On import, shows required-install progress UI, runs auto-installs, then opens the main CCS Hub when complete.
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

                void OpenFirstRunHub()
                {
                    if (!CCSSetupState.ShouldAutoOpenSetupWizard())
                    {
                        return;
                    }

                    CCSSetupState.MarkAutoOpenedThisSession();
                    CCSHubRequiredInstallProgressWindow.CloseForFirstRun();
                    CCSSetupWindow.ShowFirstRunAuto();
                }

                void OnRequiredAutoFinished()
                {
                    CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted -= OnRequiredAutoFinished;
                    OpenFirstRunHub();
                }

                CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted += OnRequiredAutoFinished;

                CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall();

                if (!CCSSetupState.ShouldAutoOpenSetupWizard())
                {
                    CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted -= OnRequiredAutoFinished;
                    CCSHubRequiredInstallProgressWindow.CloseForFirstRun();
                    return;
                }

                if (CCSSetupState.AreRequiredAutoDependenciesSatisfied())
                {
                    CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted -= OnRequiredAutoFinished;
                    OpenFirstRunHub();
                }
            });
        }

        #endregion
    }
}
