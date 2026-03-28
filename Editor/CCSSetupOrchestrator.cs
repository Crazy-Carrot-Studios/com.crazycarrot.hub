// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupOrchestrator
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Opens the CCS Hub optional UI on first run after CCS Branding installs (or when all required are already present / queue finishes). Does not use EditorApplication.isUpdating — it can stay true during normal editor frames and block delayCall indefinitely.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Listens for CCS Branding Package Manager success and for completion of the automatic required-dependency batch, then opens the CCS Hub window when appropriate.
    /// </summary>
    [InitializeOnLoad]
    public static class CCSSetupOrchestrator
    {
        #region Unity Callbacks

        static CCSSetupOrchestrator()
        {
            CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted += OnRequiredAutoInstallCompleted;
            CCSPackageInstallService.PackageInstallSucceeded += OnBrandingPackageInstallSucceeded;
        }

        #endregion

        #region Private Methods

        private static void OnRequiredAutoInstallCompleted()
        {
            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                if (!SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
                {
                    LogAutoOpenBlocked("RequiredAutoInstallCompleted");
                }

                return;
            }

            EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
        }

        /// <summary>
        /// Opens the Hub as soon as Branding’s Client.Add succeeds so optional UI is available while Cinemachine / Input System continue (manifest order after Branding).
        /// </summary>
        private static void OnBrandingPackageInstallSucceeded(CCSPackageDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.Id) || definition.Id != CCSSetupConstants.BrandingDefinitionId)
            {
                return;
            }

            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                if (!SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
                {
                    LogAutoOpenBlocked("After CCS Branding Client.Add");
                }

                return;
            }

            EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
        }

        /// <summary>
        /// Waits until assemblies are not compiling and the PM queue is idle for this callback chain.
        /// Do not gate on <see cref="EditorApplication.isUpdating"/> — it is often true during normal editor updates and will reschedule forever.
        /// </summary>
        private static void WaitForStableEditorThenOpenHub()
        {
            if (EditorApplication.isCompiling)
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
                if (!SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
                {
                    LogAutoOpenBlocked("OpenFirstRunHubAfterRequiredPhase");
                }

                return;
            }

            CCSSetupState.MarkAutoOpenedThisSession();
            CCSSetupProgressWindow.CloseForFirstRunTransition();
            CCSSetupWindow.ShowFirstRunAuto();
        }

        private static void LogAutoOpenBlocked(string context)
        {
            bool sessionOpened = SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false);
            CCSEditorLog.Info(
                $"CCS Hub: {context} — auto-open skipped "
                + $"(setupCompleted={CCSSetupState.IsSetupCompleted()}, setupSkipped={CCSSetupState.IsSetupSkipped()}, autoOpenedThisSession={sessionOpened}). "
                + "SetupCompleted and SetupSkipped default to false; clear them in EditorPrefs or use CCS Hub dev reset if stuck.");
        }

        #endregion
    }
}
