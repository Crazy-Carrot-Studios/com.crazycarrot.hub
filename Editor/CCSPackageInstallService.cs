// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageInstallService
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Sequential Package Manager Client.Add queue with session-persisted pending ids across domain reloads.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using CCS.Hub;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace CCS.Hub.Editor
{
    public static class CCSPackageInstallService
    {
        #region Variables

        private static readonly Queue<CCSPackageDefinition> InstallQueue = new Queue<CCSPackageDefinition>();
        private static readonly HashSet<string> FailedDefinitionIds = new HashSet<string>();
        private static readonly List<string> LastBatchSuccessDisplayNames = new List<string>();
        private static AddRequest activeAddRequest;
        private static CCSPackageDefinition activeDefinition;
        private static bool updateRegistered;
        private static bool waitingForPackageListBeforeDequeue;
        private static bool postReloadInstallQueueHint;

        public static event Action StateChanged;

        #endregion

        #region Unity Callbacks

        [InitializeOnLoadMethod]
        private static void RestorePendingQueueAfterDomainReload()
        {
            EditorApplication.delayCall += TryRestoreQueueFromSession;
        }

        #endregion

        #region Public Methods

        public static IReadOnlyList<string> LastSuccessfulInstallDisplayNames => LastBatchSuccessDisplayNames;

        public static bool ShouldShowPostReloadInstallBanner()
        {
            return postReloadInstallQueueHint;
        }

        public static List<string> GetFailedInstallDisplayNames()
        {
            List<string> result = new List<string>();
            foreach (string definitionId in FailedDefinitionIds)
            {
                if (CCSPackageRegistry.TryFindById(definitionId, out CCSPackageDefinition definition))
                {
                    result.Add(definition.DisplayName);
                }
                else
                {
                    result.Add(definitionId);
                }
            }

            return result;
        }

        public static void EnqueueDefinitions(IEnumerable<CCSPackageDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            LastBatchSuccessDisplayNames.Clear();

            foreach (CCSPackageDefinition definition in definitions)
            {
                if (!definition.AutoInstallSupported || definition.SourceType == CCSPackageSourceType.Manual)
                {
                    continue;
                }

                if (definition.Id == CCSSetupConstants.UnityUrpDefinitionId && CCSPackageProjectContext.IsUrpEffectivelyPresent())
                {
                    CCSEditorLog.Info("Queue skipped Universal RP because URP is already detected for this project.");
                    continue;
                }

                if (CCSPackageStatusService.IsListReady() && CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
                {
                    CCSEditorLog.Info($"Queue skipped '{definition.DisplayName}' because Package Manager reports it as installed.");
                    continue;
                }

                InstallQueue.Enqueue(definition);
            }

            PersistQueueToSession();
            RegisterEditorUpdate();
            RaiseStateChanged();
        }

        public static void ClearFailedFlags()
        {
            FailedDefinitionIds.Clear();
            RaiseStateChanged();
        }

        public static bool IsBusy()
        {
            return activeAddRequest != null || InstallQueue.Count > 0;
        }

        public static bool IsPending(string definitionId)
        {
            if (!string.IsNullOrEmpty(activeDefinition.Id) && activeDefinition.Id == definitionId && activeAddRequest != null)
            {
                return true;
            }

            foreach (CCSPackageDefinition queued in InstallQueue)
            {
                if (queued.Id == definitionId)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInstalling(string definitionId)
        {
            return activeAddRequest != null
                && !string.IsNullOrEmpty(activeDefinition.Id)
                && activeDefinition.Id == definitionId;
        }

        public static bool IsFailed(string definitionId)
        {
            return FailedDefinitionIds.Contains(definitionId);
        }

        public static string GetActiveInstallDisplayName()
        {
            if (activeAddRequest == null || string.IsNullOrEmpty(activeDefinition.Id))
            {
                return string.Empty;
            }

            return activeDefinition.DisplayName;
        }

        #endregion

        #region Private Methods

        private static void TryRestoreQueueFromSession()
        {
            string raw = SessionState.GetString(CCSSetupConstants.SessionStatePendingInstallQueueIds, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            string[] tokens = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < tokens.Length; index++)
            {
                string id = tokens[index].Trim();
                if (CCSPackageRegistry.TryFindById(id, out CCSPackageDefinition definition))
                {
                    InstallQueue.Enqueue(definition);
                }
            }

            if (InstallQueue.Count > 0)
            {
                postReloadInstallQueueHint = true;
                CCSEditorLog.Info("Restored pending CCS Hub install queue from session state after domain reload.");
                RegisterEditorUpdate();
                RaiseStateChanged();
            }
        }

        private static void PersistQueueToSession()
        {
            if (InstallQueue.Count == 0)
            {
                SessionState.EraseString(CCSSetupConstants.SessionStatePendingInstallQueueIds);
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (CCSPackageDefinition definition in InstallQueue)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(definition.Id);
            }

            SessionState.SetString(CCSSetupConstants.SessionStatePendingInstallQueueIds, builder.ToString());
        }

        private static void RegisterEditorUpdate()
        {
            if (updateRegistered)
            {
                return;
            }

            updateRegistered = true;
            EditorApplication.update += ProcessInstallQueue;
        }

        private static void UnregisterEditorUpdateIfIdle()
        {
            if (activeAddRequest != null || InstallQueue.Count > 0)
            {
                return;
            }

            if (updateRegistered)
            {
                EditorApplication.update -= ProcessInstallQueue;
                updateRegistered = false;
            }

            postReloadInstallQueueHint = false;
        }

        private static void ProcessInstallQueue()
        {
            if (activeAddRequest != null)
            {
                if (!activeAddRequest.IsCompleted)
                {
                    return;
                }

                CCSPackageDefinition finished = activeDefinition;
                CompleteActiveRequest(finished);
                activeAddRequest = null;
                activeDefinition = default;
                PersistQueueToSession();
                CCSPackageStatusService.RefreshInstalledPackages(RaiseStateChanged);
                return;
            }

            if (InstallQueue.Count == 0)
            {
                UnregisterEditorUpdateIfIdle();
                waitingForPackageListBeforeDequeue = false;
                return;
            }

            if (!CCSPackageStatusService.IsListReady())
            {
                if (!waitingForPackageListBeforeDequeue)
                {
                    waitingForPackageListBeforeDequeue = true;
                    CCSPackageStatusService.RefreshInstalledPackages(() =>
                    {
                        waitingForPackageListBeforeDequeue = false;
                        RaiseStateChanged();
                    });
                }

                return;
            }

            CCSPackageDefinition next = InstallQueue.Dequeue();
            PersistQueueToSession();

            if (next.Id == CCSSetupConstants.UnityUrpDefinitionId && CCSPackageProjectContext.IsUrpEffectivelyPresent())
            {
                CCSEditorLog.Info("Install step skipped; Universal RP already detected for this project.");
                RaiseStateChanged();
                return;
            }

            if (CCSPackageStatusService.IsPackageInstalled(next.PackageId))
            {
                CCSEditorLog.Info($"Install step skipped; already present: {next.DisplayName}");
                RaiseStateChanged();
                return;
            }

            activeDefinition = next;
            FailedDefinitionIds.Remove(next.Id);
            CCSEditorLog.Info($"Package Manager Client.Add starting for definition '{next.Id}' using '{next.InstallIdentifier}'.");
            activeAddRequest = Client.Add(next.InstallIdentifier);
            RaiseStateChanged();
        }

        private static void CompleteActiveRequest(CCSPackageDefinition finished)
        {
            if (activeAddRequest == null)
            {
                return;
            }

            if (activeAddRequest.Status == StatusCode.Success)
            {
                CCSEditorLog.Info($"Client.Add succeeded for '{finished.DisplayName}'.");
                LastBatchSuccessDisplayNames.Add(finished.DisplayName);
            }
            else
            {
                string message = activeAddRequest.Error != null ? activeAddRequest.Error.message : "Unknown error.";
                CCSEditorLog.Error($"Client.Add failed for '{finished.DisplayName}': {message}");
                FailedDefinitionIds.Add(finished.Id);
            }
        }

        private static void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        #endregion
    }
}
