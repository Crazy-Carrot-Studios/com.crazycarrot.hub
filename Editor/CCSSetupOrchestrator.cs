// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupOrchestrator
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Coordinates first-run required installs (manifest-driven) then opens the CCS Hub optional UI after Package Manager work completes and the editor is stable.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Listens for completion of the automatic required-dependency batch and transitions into the optional CCS Hub window on first run.
    /// </summary>
    [InitializeOnLoad]
    public static class CCSSetupOrchestrator
    {
        #region Unity Callbacks

        static CCSSetupOrchestrator()
        {
            CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted += OnRequiredAutoInstallCompleted;
        }

        #endregion

        #region Private Methods

        private static void OnRequiredAutoInstallCompleted()
        {
            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                return;
            }

            EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
        }

        private static void WaitForStableEditorThenOpenHub()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
                return;
            }

            if (CCSPackageInstallService.IsBusy())
            {
                EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
                return;
            }

            OpenFirstRunHubAfterRequiredPhase();
        }

        private static void OpenFirstRunHubAfterRequiredPhase()
        {
            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                return;
            }

            CCSSetupState.MarkAutoOpenedThisSession();
            CCSSetupProgressWindow.CloseForFirstRunTransition();
            CCSSetupWindow.ShowFirstRunAuto();
        }

        #endregion
    }
}
