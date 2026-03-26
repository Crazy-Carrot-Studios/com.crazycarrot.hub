// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Runs after import to refresh Package Manager state and auto-open the CCS Setup Wizard once per project session when appropriate.
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

        // Bootstrap uses static entry only; no serialized state.

        #endregion

        #region Unity Callbacks

        static CCSSetupBootstrap()
        {
            EditorApplication.delayCall += OnEditorDelayCall;
        }

        #endregion

        #region Public Methods

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

                CCSSetupState.MarkAutoOpenedThisSession();
                CCSSetupWindow.ShowFirstRunAuto();
            });
        }

        #endregion
    }
}
