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
using UnityEngine;

namespace CCS.Hub.Editor
{
    public static class CCSHubRequiredDependencyBootstrap
    {
        /// <summary>
        /// Fired after required dependency auto-install has finished (queue drained or all already present). Dispatched on a delay call.
        /// </summary>
        public static event Action RequiredAutoInstallCompleted;

        /// <summary>
        /// Called from <see cref="CCSSetupBootstrap"/> after Package Manager list refresh.
        /// </summary>
        public static void TryScheduleAutoInstall()
        {
            Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}Checking required dependencies (TryScheduleAutoInstall).");
            CCSSetupOrchestrator.EnsureInitialized();

            if (!CCSPackageStatusService.IsListReady())
            {
                CCSEditorLog.Info("CCS Hub: Required-deps — package list not ready yet; retrying on delayCall.");
                EditorApplication.delayCall += TryScheduleAutoInstall;
                return;
            }

            List<CCSPackageDefinition> missing = new List<CCSPackageDefinition>();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (!CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
                {
                    missing.Add(definition);
                }
            }

            CCSEditorLog.Info($"CCS Hub: Required-deps — missing required package count = {missing.Count}.");
            Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}Missing required count: {missing.Count}");

            if (missing.Count == 0)
            {
                string summary = BuildAlreadyPresentSummary();
                CCSSetupState.SetRequiredAutoDependenciesSatisfied(summary);
                Debug.LogWarning(
                    $"{CCSSetupConstants.HubFlowDiagnosticPrefix}All required dependencies already satisfied → scheduling RequiredAutoInstallCompleted.");
                CCSEditorLog.Info("CCS Hub: Required-deps — all present; no Client.Add queue. Scheduling RequiredAutoInstallCompleted.");
                ScheduleRequiredAutoInstallCompletedNotification();
                return;
            }

            if (CCSSetupState.AreRequiredAutoDependenciesSatisfied())
            {
                CCSSetupState.ClearRequiredAutoDependenciesSatisfied();
                CCSEditorLog.Info(
                    "CCS Hub: Required-deps — satisfied flag was stale; cleared. Queueing missing packages.");
            }

            Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}Queueing required installs ({missing.Count}).");
            CCSEditorLog.Info($"CCS Hub: Required-deps — queueing {missing.Count} required package install(s). installQueueBusy will be true until the pass finishes.");
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
            CCSEditorLog.Info("CCS Hub: Required-deps — Client.Add queue drained; pass finished. Scheduling RequiredAutoInstallCompleted.");
            ScheduleRequiredAutoInstallCompletedNotification();
        }

        private static void ScheduleRequiredAutoInstallCompletedNotification()
        {
            EditorApplication.delayCall += () =>
            {
                Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}RequiredAutoInstallCompleted INVOKED (delayCall).");
                CCSEditorLog.Info("CCS Hub: Required-deps — invoking RequiredAutoInstallCompleted subscribers (delayCall).");
                RequiredAutoInstallCompleted?.Invoke();
            };
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
