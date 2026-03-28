// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupState
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: Single place for Hub first-run EditorPrefs + SessionState: auto-open gate after required pass, full reset, and Console diagnostics dumps.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    public static class CCSSetupState
    {
        #region Variables

        private static string ProjectEditorPrefsKey(string suffix)
        {
            return $"{CCSSetupConstants.EditorPrefsKeyPrefix}{suffix}_{Application.dataPath.GetHashCode():X8}";
        }

        #endregion

        #region Public Methods — EditorPrefs

        public static bool IsSetupCompleted()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsSetupCompletedKey), false);
        }

        public static bool IsSetupSkipped()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsSetupSkippedKey), false);
        }

        public static void SetSetupCompleted(bool value)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsSetupCompletedKey), value);
            CCSEditorLog.Info($"First-run: EditorPrefs SetupCompleted = {value} (this project).");
        }

        public static void SetSetupSkipped(bool value)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsSetupSkippedKey), value);
            CCSEditorLog.Info($"First-run: EditorPrefs SetupSkipped = {value} (this project).");
        }

        public static bool AreRequiredAutoDependenciesSatisfied()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey), false);
        }

        public static string GetRequiredAutoDependenciesSummary()
        {
            return EditorPrefs.GetString(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey), string.Empty);
        }

        public static bool GetIncludeDotweenOptional()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsIncludeDotweenOptionalKey), false);
        }

        public static void SetIncludeDotweenOptional(bool value)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsIncludeDotweenOptionalKey), value);
        }

        public static void SetRequiredAutoDependenciesSatisfied(string summary)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey), true);
            EditorPrefs.SetString(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey), summary ?? string.Empty);
        }

        public static void ClearRequiredAutoDependenciesSatisfied()
        {
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey));
        }

        #endregion

        #region Public Methods — Auto-open gate

        /// <summary>
        /// After the required dependency pass completes, the main Hub may auto-open once per editor session if the user has not finished or skipped setup.
        /// </summary>
        public static bool ShouldAutoOpenMainHubAfterRequiredPhase(out string blockReason)
        {
            if (IsSetupCompleted())
            {
                blockReason = "setupCompleted";
                return false;
            }

            if (IsSetupSkipped())
            {
                blockReason = "setupSkipped";
                return false;
            }

            if (SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
            {
                blockReason = "autoOpenedThisSession";
                return false;
            }

            blockReason = null;
            return true;
        }

        /// <summary>
        /// Sets SessionState after the Hub window has been shown (first-run auto path — see <see cref="CCSSetupWindow.ShowOrFocusFirstRunAuto"/>).
        /// Do not call from manual <c>Open CCS Hub</c>.
        /// </summary>
        public static void MarkAutoOpenedThisSession()
        {
            SessionState.SetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, true);
            Debug.LogWarning(
                $"{CCSSetupConstants.HubFlowDiagnosticPrefix}MarkAutoOpenedThisSession SET (after Hub Show)\n{Environment.StackTrace}");
        }

        /// <summary>
        /// First-run bootstrap only: clears stale <see cref="SessionStateAutoOpenedThisSession"/> and/or
        /// <see cref="SessionStatePendingHubAutoOpenAfterRequiredPhase"/> when no Hub window exists and first-run setup
        /// is not finished (e.g. domain reload or lost delayCall left SessionState inconsistent).
        /// </summary>
        public static void TryRecoverStaleFirstRunAutoOpenSessionStateIfNoHubWindow()
        {
            if (IsSetupCompleted() || IsSetupSkipped())
            {
                return;
            }

            if (CCSSetupWindow.GetExistingInstance() != null)
            {
                return;
            }

            if (SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
            {
                SessionState.EraseBool(CCSSetupConstants.SessionStateAutoOpenedThisSession);
                Debug.LogWarning(
                    $"{CCSSetupConstants.HubFlowDiagnosticPrefix}Recovered stale autoOpenedThisSession "
                    + "(no CCSSetupWindow, setup not completed/skipped).");
            }

            if (SessionState.GetBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase, false))
            {
                SessionState.EraseBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase);
                Debug.LogWarning(
                    $"{CCSSetupConstants.HubFlowDiagnosticPrefix}Recovered stale pendingHubAutoOpenAfterRequiredPhase "
                    + "(no Hub window exists).");
            }
        }

        public static void SetPendingHubAutoOpenAfterRequiredPhase(bool value)
        {
            SessionState.SetBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase, value);
            if (value)
            {
                Debug.LogWarning(
                    $"{CCSSetupConstants.HubFlowDiagnosticPrefix}PendingHubAutoOpenAfterRequiredPhase set = true\n{Environment.StackTrace}");
            }
            else
            {
                Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}PendingHubAutoOpenAfterRequiredPhase = false");
            }
        }

        public static bool IsPendingHubAutoOpenAfterRequiredPhase()
        {
            return SessionState.GetBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase, false);
        }

        public static void ClearPendingHubAutoOpenAfterRequiredPhase()
        {
            bool had = SessionState.GetBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase, false);
            SessionState.EraseBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase);
            Debug.LogWarning(
                $"{CCSSetupConstants.HubFlowDiagnosticPrefix}ClearPendingHubAutoOpenAfterRequiredPhase — cleared (hadPending={had}).");
        }

        #endregion

        #region Public Methods — Reset & diagnostics

        /// <summary>
        /// Blank slate for this project: Hub EditorPrefs, Hub SessionState, optional-install session, install queue session, then optional-install context.
        /// Does not restart Unity. Call <see cref="CCSSetupBootstrap.RunFirstRunPipelineNow"/> immediately after to rerun the same path as a fresh load.
        /// Order: pipeline reset first, then prefs/session clears, so nothing rewrites stale values mid-flow.
        /// </summary>
        public static void ResetAllFirstRunStateForThisProject()
        {
            CCSPackageInstallService.ResetPipelineStateForFirstRunStateReset();
            CCSHubOptionalInstallContext.ClearOptionalUserTracking();

            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsSetupCompletedKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsSetupSkippedKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsIncludeDotweenOptionalKey));

            SessionState.EraseBool(CCSSetupConstants.SessionStateAutoOpenedThisSession);
            SessionState.EraseBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase);
            SessionState.EraseString(CCSSetupConstants.SessionStatePendingInstallQueueIds);
            SessionState.EraseBool(CCSSetupConstants.SessionStateAutoRequiredPassActive);
            SessionState.EraseBool(CCSSetupConstants.SessionStateDotweenCopyPending);
            SessionState.EraseBool(CCSSetupConstants.SessionStateOptionalUserCcSelected);
            SessionState.EraseBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected);
            SessionState.EraseInt(CCSSetupConstants.SessionStateOptionalUserStepTotal);

            CCSEditorLog.Info("First-run: full reset applied for this project (EditorPrefs + SessionState + install pipeline markers).");
        }

        /// <summary>Logs every Hub first-run related value for the current project (scan-friendly, consistent prefix).</summary>
        public static void LogFirstRunStateSnapshot(string context)
        {
            CCSEditorLog.Info(BuildFirstRunStateDump(context));
        }

        public static string BuildFirstRunStateDump(string context)
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine($"First-run state dump — {context}");
            builder.AppendLine($"  projectEditorHash: {Application.dataPath.GetHashCode():X8}");
            builder.AppendLine($"  EditorPrefs SetupCompleted: {IsSetupCompleted()}");
            builder.AppendLine($"  EditorPrefs SetupSkipped: {IsSetupSkipped()}");
            builder.AppendLine($"  EditorPrefs RequiredAutoDepsSatisfied: {AreRequiredAutoDependenciesSatisfied()}");
            builder.AppendLine($"  EditorPrefs RequiredAutoDepsSummary: \"{GetRequiredAutoDependenciesSummary()}\"");
            builder.AppendLine($"  EditorPrefs IncludeDotweenOptional: {GetIncludeDotweenOptional()}");
            builder.AppendLine($"  Session autoOpenedThisSession: {SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false)}");
            builder.AppendLine($"  Session pendingHubAutoOpenAfterRequired: {SessionState.GetBool(CCSSetupConstants.SessionStatePendingHubAutoOpenAfterRequiredPhase, false)}");
            builder.AppendLine($"  Session pendingInstallQueueIds: \"{SessionState.GetString(CCSSetupConstants.SessionStatePendingInstallQueueIds, string.Empty)}\"");
            builder.AppendLine($"  Session autoRequiredPassActive: {SessionState.GetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, false)}");
            builder.AppendLine($"  Session dotweenCopyPending: {SessionState.GetBool(CCSSetupConstants.SessionStateDotweenCopyPending, false)}");
            builder.AppendLine($"  Session optionalUserCcSelected: {SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserCcSelected, false)}");
            builder.AppendLine($"  Session optionalUserDotweenSelected: {SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected, false)}");
            builder.AppendLine($"  Session optionalUserStepTotal: {SessionState.GetInt(CCSSetupConstants.SessionStateOptionalUserStepTotal, 0)}");
            builder.AppendLine($"  Package list ready: {CCSPackageStatusService.IsListReady()}");
            builder.AppendLine($"  Install queue busy: {CCSPackageInstallService.IsBusy()}");
            ShouldAutoOpenMainHubAfterRequiredPhase(out string blockReason);
            builder.AppendLine(
                $"  Auto-open gate: {(string.IsNullOrEmpty(blockReason) ? "ALLOW" : "BLOCK (" + blockReason + ")")}");
            return builder.ToString();
        }

        #endregion
    }
}
