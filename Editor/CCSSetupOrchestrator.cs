// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupOrchestrator
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Single subscriber to RequiredAutoInstallCompleted: stable-editor delayCall → main Hub auto-open with pending flag + gate. EnsureInitialized() before any event raise.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Opens <see cref="CCSSetupWindow"/> when the required-dependency pass completes and the first-run auto-open gate allows it.
    /// </summary>
    public static class CCSSetupOrchestrator
    {
        private static bool eventHandlersRegistered;

        /// <summary>
        /// Registers handlers for <see cref="CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted"/>.
        /// Idempotent; call from bootstrap and from <see cref="CCSHubRequiredDependencyBootstrap.TryScheduleAutoInstall"/> before firing the event.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (eventHandlersRegistered)
            {
                return;
            }

            eventHandlersRegistered = true;
            CCSHubRequiredDependencyBootstrap.RequiredAutoInstallCompleted += OnRequiredAutoInstallCompleted;
        }

        private static void OnRequiredAutoInstallCompleted()
        {
            CCSEditorLog.Info("CCS Hub: RequiredAutoInstallCompleted — event received.");
            CCSEditorLog.Info(CCSSetupState.BuildFirstRunStateDump("orchestrator: before auto-open gate").TrimEnd());

            if (!CCSSetupState.ShouldAutoOpenMainHubAfterRequiredPhase(out string blockReason))
            {
                CCSEditorLog.Info(
                    $"CCS Hub: Main Hub auto-open — BLOCKED (reason={blockReason}). "
                    + "installQueueBusy="
                    + CCSPackageInstallService.IsBusy()
                    + ".");
                return;
            }

            CCSSetupState.SetPendingHubAutoOpenAfterRequiredPhase(true);
            CCSEditorLog.Info(
                "CCS Hub: Main Hub auto-open — ALLOWED; pending flag set; scheduling open after editor is stable (not compiling).");
            EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
        }

        private static void WaitForStableEditorThenOpenHub()
        {
            if (EditorApplication.isCompiling)
            {
                CCSEditorLog.Info("CCS Hub: Waiting for compilation to finish before opening main Hub.");
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
            CCSSetupState.MarkAutoOpenedThisSession();
            CCSEditorLog.Info("CCS Hub: Opening main CCS Hub window (first-run auto, required phase done).");
            CCSSetupProgressWindow.CloseForFirstRunTransition();
            CCSSetupWindow.ShowOrFocusFirstRunAuto();
            CCSEditorLog.Info(CCSSetupState.BuildFirstRunStateDump("orchestrator: after ShowOrFocusFirstRunAuto").TrimEnd());
        }
    }
}
