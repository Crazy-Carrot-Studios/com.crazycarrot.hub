// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupOrchestrator
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 28, 2026
// Summary: First-run Hub auto-open: (1) when CCS Branding Client.Add succeeds, (2) fallback when required pass completes. Shared gate + single schedule. EnsureInitialized before any install/event.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using CCS.Hub;
using UnityEditor;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Opens <see cref="CCSSetupWindow"/> on first-run when the auto-open gate allows it — earliest after Branding install succeeds; fallback when the full required pass completes.
    /// </summary>
    public static class CCSSetupOrchestrator
    {
        private static bool eventHandlersRegistered;

        /// <summary>
        /// Subscribes to <see cref="CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted"/> and
        /// <see cref="CCSPackageInstallService.PackageInstallSucceeded"/> (Branding only). Idempotent.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (eventHandlersRegistered)
            {
                return;
            }

            eventHandlersRegistered = true;
            CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted += OnRequiredAutoInstallCompleted;
            CCSPackageInstallService.PackageInstallSucceeded += OnPackageInstallSucceeded;
        }

        private static void OnPackageInstallSucceeded(CCSPackageDefinition definition)
        {
            if (definition.Id != CCSSetupConstants.BrandingDefinitionId
                && definition.PackageId != CCSSetupConstants.BrandingPackageId)
            {
                return;
            }

            TryBeginFirstRunHubAutoOpen("branding_install_succeeded");
        }

        private static void OnRequiredAutoInstallCompleted()
        {
            TryBeginFirstRunHubAutoOpen("required_pass_complete");
        }

        /// <summary>
        /// Single entry for scheduling the first-run Hub open: idempotent across Branding success and required-pass completion.
        /// </summary>
        private static void TryBeginFirstRunHubAutoOpen(string trigger)
        {
            if (SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
            {
                return;
            }

            if (CCSSetupState.IsPendingHubAutoOpenAfterRequiredPhase())
            {
                return;
            }

            if (!CCSSetupState.ShouldAutoOpenMainHubAfterRequiredPhase(out string blockReason))
            {
                CCSEditorLog.Info(
                    $"CCS Hub: Main Hub auto-open skipped (reason={blockReason}, trigger={trigger}).");
                return;
            }

            CCSSetupState.SetPendingHubAutoOpenAfterRequiredPhase(true);
            EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
        }

        private static void WaitForStableEditorThenOpenHub()
        {
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
                return;
            }

            OpenMainHubAfterRequiredPhase();
        }

        private static void OpenMainHubAfterRequiredPhase()
        {
            if (!CCSSetupState.IsPendingHubAutoOpenAfterRequiredPhase())
            {
                CCSEditorLog.Warning(
                    "CCS Hub: OpenMainHubAfterRequiredPhase — pending Hub auto-open flag was cleared; aborting (no duplicate open).");
                return;
            }

            if (!CCSSetupState.ShouldAutoOpenMainHubAfterRequiredPhase(out string blockReason))
            {
                CCSSetupState.ClearPendingHubAutoOpenAfterRequiredPhase();
                CCSEditorLog.Info($"CCS Hub: Main Hub auto-open — cancelled before show (reason={blockReason}).");
                return;
            }

            CCSSetupState.ClearPendingHubAutoOpenAfterRequiredPhase();
            // Show Hub while the completion banner may still be visible; then close progress so the transition reads as "next stage" not "window disappeared first."
            CCSSetupWindow.ShowOrFocusFirstRunAuto();
            CCSSetupProgressWindow.CloseForFirstRunTransition();
        }
    }
}
