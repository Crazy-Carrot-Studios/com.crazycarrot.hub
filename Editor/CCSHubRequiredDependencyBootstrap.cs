// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubRequiredDependencyBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: Evaluates manifest required packages; queues missing installs or completes immediately; raises RequiredAutoInstallCompleted on delayCall (same path whether installs ran or all were present).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCS.Hub;
using UnityEditor;

namespace CCS.Hub.Editor
{
    public static class CCSHubRequiredDependencyBootstrap
    {
        #region Variables

        /// <summary>
        /// When true, <see cref="TryScheduleAutoInstall"/> showed the required progress window and we pair one completion <see cref="CCSEditorLog.Info"/> with it.
        /// </summary>
        private static bool logRequiredPhaseLifecycle;

        /// <summary>
        /// Prevents overlapping required evaluations (delayCall re-entry, duplicate bootstrap, etc.). Cleared when the required pass completes or on explicit reset.
        /// </summary>
        private static bool requiredBootstrapCycleActive;

        private static int tryScheduleDeferralCount;

        private const int MaxTryScheduleDeferrals = 64;

        #endregion

        #region Events

        /// <summary>
        /// Fired after required dependency auto-install has finished (queue drained or all already present). Dispatched on a delay call.
        /// </summary>
        public static event Action RequiredAutoInstallCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        /// Called from <see cref="CCSSetupState.ResetAllFirstRunStateForThisProject"/> so the next <see cref="TryScheduleAutoInstall"/> can run.
        /// </summary>
        public static void ResetRequiredBootstrapCycleGuard()
        {
            requiredBootstrapCycleActive = false;
            tryScheduleDeferralCount = 0;
        }

        /// <summary>
        /// Single entry for showing required progress UI after <see cref="CCSPackageInstallService"/> restores an auto-required queue from session (domain reload). Does not start evaluation.
        /// </summary>
        public static void RequestRequiredProgressUiForRestoredAutoRequiredQueue()
        {
            if (CCSSetupState.IsSetupCompleted() || CCSSetupState.IsSetupSkipped())
            {
                return;
            }

            CCSSetupProgressWindow.ShowRequiredPhase();
        }

        /// <summary>
        /// Called from <see cref="CCSSetupBootstrap"/> after Package Manager list refresh.
        /// </summary>
        public static void TryScheduleAutoInstall()
        {
            CCSSetupOrchestrator.EnsureInitialized();

            if (!CCSPackageStatusService.IsListReady())
            {
                if (CCSPackageStatusService.IsLastPackageListRefreshFailed())
                {
                    CCSEditorLog.Error(
                        "CCS Hub: Required package evaluation aborted — Package Manager package list failed to load. "
                        + "Fix Package Manager connectivity and use the internal reset menu or reopen the project.");
                    tryScheduleDeferralCount = 0;
                    return;
                }

                if (CCSPackageStatusService.IsListRefreshInProgress())
                {
                    if (tryScheduleDeferralCount >= MaxTryScheduleDeferrals)
                    {
                        CCSEditorLog.Error("CCS Hub: Required bootstrap stopped — package list stayed unavailable (deferral limit).");
                        tryScheduleDeferralCount = 0;
                        return;
                    }

                    tryScheduleDeferralCount++;
                    EditorApplication.delayCall += TryScheduleAutoInstall;
                    return;
                }

                // No list data yet — request a refresh then retry (covers callers that did not preflight Refresh).
                tryScheduleDeferralCount = 0;
                CCSPackageStatusService.RefreshInstalledPackages(TryScheduleAutoInstall);
                return;
            }

            tryScheduleDeferralCount = 0;

            if (requiredBootstrapCycleActive)
            {
                return;
            }

            requiredBootstrapCycleActive = true;

            List<CCSPackageDefinition> missing = new List<CCSPackageDefinition>();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (!CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
                {
                    missing.Add(definition);
                }
            }

            // Phase 1: one entry — show required progress as soon as we evaluate (before enqueue / zero-missing completion).
            if (!CCSSetupState.IsSetupCompleted() && !CCSSetupState.IsSetupSkipped())
            {
                logRequiredPhaseLifecycle = true;
                CCSSetupProgressWindow.ShowRequiredPhase();
                CCSEditorLog.Info("CCS Hub: Required install phase started.");
            }
            else
            {
                logRequiredPhaseLifecycle = false;
            }

            if (missing.Count == 0)
            {
                string summary = BuildAlreadyPresentSummary();
                CCSSetupState.SetRequiredAutoDependenciesSatisfied(summary);
                EditorApplication.delayCall += ScheduleRequiredAutoInstallCompletedNotification;
                return;
            }

            if (CCSSetupState.AreRequiredAutoDependenciesSatisfied())
            {
                CCSSetupState.ClearRequiredAutoDependenciesSatisfied();
            }

            CCSPackageInstallService.EnqueueAutoRequiredDefinitions(missing);
        }

        /// <summary>
        /// Invoked when the auto-required Client.Add queue has fully drained (including skip-only passes).
        /// </summary>
        public static void NotifyAutoRequiredBatchFinished(IReadOnlyList<string> lastSuccessDisplayNames)
        {
            StringBuilder builder = new StringBuilder();
            if (lastSuccessDisplayNames != null && lastSuccessDisplayNames.Count > 0)
            {
                for (int index = 0; index < lastSuccessDisplayNames.Count; index++)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(lastSuccessDisplayNames[index]);
                }
            }

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (!CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
                {
                    continue;
                }

                if (lastSuccessDisplayNames != null
                    && lastSuccessDisplayNames.Contains(definition.DisplayName))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(definition.DisplayName);
                builder.Append(" (already present)");
            }

            string summary = builder.Length > 0
                ? builder.ToString()
                : "Required CCS packages (see Package Manager if this stays empty).";

            CCSSetupState.SetRequiredAutoDependenciesSatisfied(summary);
            ScheduleRequiredAutoInstallCompletedNotification();
        }

        #endregion

        #region Private Methods

        private static void ScheduleRequiredAutoInstallCompletedNotification()
        {
            EditorApplication.delayCall += () =>
            {
                CCSSetupProgressWindow.NotifyRequiredPassCompleteThenRun(NotifyRequiredAutoInstallCompletedSubscribers);
            };
        }

        private static void NotifyRequiredAutoInstallCompletedSubscribers()
        {
            if (logRequiredPhaseLifecycle)
            {
                CCSEditorLog.Info("CCS Hub: Required install phase complete.");
                logRequiredPhaseLifecycle = false;
            }

            RequiredAutoInstallCompleted?.Invoke();
            requiredBootstrapCycleActive = false;
        }

        private static string BuildAlreadyPresentSummary()
        {
            StringBuilder builder = new StringBuilder();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(definition.DisplayName);
            }

            return builder.Length > 0
                ? $"{builder} (already present)"
                : "Required dependencies already satisfied.";
        }

        #endregion
    }
}
