// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubRequiredDependencyBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: After CCS Hub loads, ensures required UPM dependencies (Branding, Input System, Cinemachine) without user prompts.
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
        /// Fired after required dependency auto-install has finished (queue drained or all already present). Dispatched on a delay call.
        /// </summary>
        public static event Action RequiredAutoInstallCompleted;

        /// <summary>
        /// Called from <see cref="CCSSetupBootstrap"/> after Package Manager list refresh. Idempotent per project.
        /// </summary>
        public static void TryScheduleAutoInstall()
        {
            CCSSetupOrchestrator.EnsureInitialized();

            if (!CCSPackageStatusService.IsListReady())
            {
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

            if (missing.Count == 0)
            {
                string summary = BuildAlreadyPresentSummary();
                CCSSetupState.SetRequiredAutoDependenciesSatisfied(summary);
                CCSEditorLog.Info("CCS Hub: all required dependencies already present; skipping auto-install queue.");
                ScheduleRequiredAutoInstallCompletedNotification();
                return;
            }

            if (CCSSetupState.AreRequiredAutoDependenciesSatisfied())
            {
                CCSSetupState.ClearRequiredAutoDependenciesSatisfied();
                CCSEditorLog.Info(
                    "CCS Hub: required dependency list is not fully installed (e.g. CCS Branding); clearing stale satisfied flag and queueing installs.");
            }

            CCSEditorLog.Info($"CCS Hub: queueing {missing.Count} required package install(s).");
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
            CCSEditorLog.Info("CCS Hub: required dependency auto-install pass finished.");
            ScheduleRequiredAutoInstallCompletedNotification();
        }

        private static void ScheduleRequiredAutoInstallCompletedNotification()
        {
            EditorApplication.delayCall += () => RequiredAutoInstallCompleted?.Invoke();
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
