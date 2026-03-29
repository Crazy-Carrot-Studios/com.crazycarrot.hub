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
        /// <summary>
        /// When true, <see cref="TryScheduleAutoInstall"/> showed the required progress window and we pair one completion <see cref="CCSEditorLog.Info"/> with it.
        /// </summary>
        private static bool logRequiredPhaseLifecycle;

        /// <summary>
        /// Fired after required dependency auto-install has finished (queue drained or all already present). Dispatched on a delay call.
        /// </summary>
        public static event Action RequiredAutoInstallCompleted;

        /// <summary>
        /// Called from <see cref="CCSSetupBootstrap"/> after Package Manager list refresh.
        /// </summary>
        public static void TryScheduleAutoInstall()
        {
            CCSSetupOrchestrator.EnsureInitialized();

            if (!CCSPackageStatusService.IsListReady())
            {
                EditorApplication.delayCall += TryScheduleAutoInstall;
                return;
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

            List<CCSPackageDefinition> missing = new List<CCSPackageDefinition>();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (!CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
                {
                    missing.Add(definition);
                }
            }

            if (missing.Count == 0)
            {
                string summary = BuildAlreadyPresentSummary();
                CCSSetupState.SetRequiredAutoDependenciesSatisfied(summary);
                // Defer completion scheduling so the window paints at least one frame before Hub auto-open (never same frame as Show).
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
    }
}
