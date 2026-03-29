// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupDiagnosticTrace
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Summary: TEMPORARY Phase 1 QA tracing — set Enabled=false before release. Proves Bootstrap → Required bootstrap → TryScheduleAutoInstall → ShowRequiredPhase.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Temporary diagnostic lines prefixed with "CCS Hub DIAG:". Disable before shipping a non-debug build.
    /// </summary>
    internal static class CCSSetupDiagnosticTrace
    {
        /// <summary>Set to <c>false</c> to silence all DIAG lines (or delete this file's usages after root cause is found).</summary>
        internal const bool Enabled = true;

        internal static void Log(string message)
        {
            if (!Enabled)
            {
                return;
            }

            Debug.Log($"CCS Hub DIAG: {message}");
        }

        /// <summary>Call once so a line appears even when the Console is filtered to Warnings+ only.</summary>
        internal static void LogTraceBannerOnce()
        {
            if (!Enabled)
            {
                return;
            }

            Debug.LogWarning(
                "CCS Hub DIAG: Temporary QA trace is enabled — also expect regular Log lines (prefix CCS Hub DIAG). "
                + "If you see only this line, enable 'Info' / regular logs in the Console. Disable via CCSSetupDiagnosticTrace.Enabled after debugging.");
        }

        /// <summary>One-line snapshot of gates relevant to required progress + Hub auto-open.</summary>
        internal static void LogSetupGateSnapshot()
        {
            if (!Enabled)
            {
                return;
            }

            bool completed = CCSSetupState.IsSetupCompleted();
            bool skipped = CCSSetupState.IsSetupSkipped();
            bool autoOpened = SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false);
            bool pendingHub = SessionState.GetBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase, false);
            bool listReady = CCSPackageStatusService.IsListReady();
            CCSSetupState.ShouldAutoOpenMainHubAfterRequiredPhase(out string blockReason);
            string gate = string.IsNullOrEmpty(blockReason) ? "ALLOW" : $"BLOCK({blockReason})";
            Log(
                $"Setup state completed={completed} skipped={skipped} autoOpenedThisSession={autoOpened} "
                + $"pendingHubAutoOpen={pendingHub} listReady={listReady} autoOpenGate={gate}");
        }
    }
}
