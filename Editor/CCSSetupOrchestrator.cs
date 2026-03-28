// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupOrchestrator
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Opens the main CCS Hub window after required Package Manager installs complete. Subscribe via EnsureInitialized() before any code can raise RequiredAutoInstallCompleted (avoid missing the event due to static ctor order).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Opens <see cref="CCSSetupWindow"/> when the required-dependency auto-install pass finishes and first-run auto-open is allowed.
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
            // Always log so "silent failure" is visible in the Console (Info filter on).
            CCSEditorLog.Info("CCS Hub: RequiredAutoInstallCompleted received.");

            if (!CCSSetupState.ShouldAutoOpenSetupWizard())
            {
                if (!SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
                {
                    LogAutoOpenBlocked("RequiredAutoInstallCompleted");
                }

                return;
            }

            CCSEditorLog.Info("CCS Hub: Scheduling main CCS Hub window (first-run).");
            EditorApplication.delayCall += WaitForStableEditorThenOpenHub;
        }

        private static void WaitForStableEditorThenOpenHub()
        {
            if (EditorApplication.isCompiling)
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
            CCSEditorLog.Info("CCS Hub: Opening CCS Hub window (first-run auto, after required installs).");
            CCSSetupProgressWindow.CloseForFirstRunTransition();
            CCSSetupWindow.ShowFirstRunAuto();
        }

        private static void LogAutoOpenBlocked(string context)
        {
            bool sessionOpened = SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false);
            string message =
                $"CCS Hub: {context} — main Hub auto-open skipped "
                + $"(setupCompleted={CCSSetupState.IsSetupCompleted()}, setupSkipped={CCSSetupState.IsSetupSkipped()}, autoOpenedThisSession={sessionOpened}). "
                + "Use CCS → CCS Hub, or CCS → CCS Hub → Reset first-run setup state (this project).";
            Debug.LogWarning(message);
        }
    }
}
