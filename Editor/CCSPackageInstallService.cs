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
        private static readonly HashSet<string> SkippedDefinitionIds = new HashSet<string>();
        private static readonly List<string> LastBatchSuccessDisplayNames = new List<string>();
        private static AddRequest activeAddRequest;
        private static CCSPackageDefinition activeDefinition;
        private static bool updateRegistered;
        private static bool waitingForPackageListBeforeDequeue;
        private static bool postReloadInstallQueueHint;
        private static bool autoRequiredPassActive;

        /// <summary>Steps in the current enqueue batch (Client.Add or dequeue-skip each count as one).</summary>
        private static int batchProgressTotal;

        private static int batchProgressProcessed;
        private static bool batchProgressIndeterminate;

        public static event Action StateChanged;

        /// <summary>Fired when a Client.Add completes successfully for a queued definition.</summary>
        public static event Action<CCSPackageDefinition> PackageInstallSucceeded;

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
            EnqueueDefinitions(definitions, isAutoRequiredBatch: false);
        }

        /// <summary>Queues required hub dependencies without user confirmation; completion is recorded in EditorPrefs by the bootstrap.</summary>
        public static void EnqueueAutoRequiredDefinitions(IEnumerable<CCSPackageDefinition> definitions)
        {
            EnqueueDefinitions(definitions, isAutoRequiredBatch: true);
        }

        /// <summary>
        /// Queues missing hub required packages (Branding, Input System, Cinemachine, …) first, then the optional batch.
        /// Unity cannot list Git URLs as nested <c>package.json</c> dependencies, so required CCS Git packages must be installed in order via the Hub.
        /// </summary>
        public static void EnqueueOptionalWithRequiredPrerequisites(IEnumerable<CCSPackageDefinition> optionalDefinitions)
        {
            var combined = new List<CCSPackageDefinition>();
            bool anyRequiredEnqueued = false;

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (ShouldSkipEnqueueing(definition))
                {
                    continue;
                }

                combined.Add(definition);
                anyRequiredEnqueued = true;
            }

            if (optionalDefinitions != null)
            {
                foreach (CCSPackageDefinition definition in optionalDefinitions)
                {
                    if (ShouldSkipEnqueueing(definition))
                    {
                        continue;
                    }

                    combined.Add(definition);
                }
            }

            EnqueueDefinitions(combined, isAutoRequiredBatch: anyRequiredEnqueued);
        }

        private static void EnqueueDefinitions(IEnumerable<CCSPackageDefinition> definitions, bool isAutoRequiredBatch)
        {
            if (definitions == null)
            {
                return;
            }

            LastBatchSuccessDisplayNames.Clear();
            autoRequiredPassActive = isAutoRequiredBatch;
            if (isAutoRequiredBatch)
            {
                SessionState.SetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, true);
            }
            else
            {
                SessionState.SetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, false);
            }

            batchProgressIndeterminate = false;
            int enqueuedCount = 0;
            foreach (CCSPackageDefinition definition in definitions)
            {
                if (ShouldSkipEnqueueing(definition))
                {
                    continue;
                }

                InstallQueue.Enqueue(definition);
                enqueuedCount++;
            }

            batchProgressTotal = enqueuedCount;
            batchProgressProcessed = 0;

            PersistQueueToSession();
            RegisterEditorUpdate();
            RaiseStateChanged();
        }

        private static bool ShouldSkipEnqueueing(CCSPackageDefinition definition)
        {
            if (!definition.AutoInstallSupported
                || definition.SourceType == CCSPackageSourceType.Manual
                || definition.SourceType == CCSPackageSourceType.AssetsGitImport)
            {
                return true;
            }

            if (definition.Id == CCSSetupConstants.UnityUrpDefinitionId && CCSPackageProjectContext.IsUrpEffectivelyPresent())
            {
                CCSEditorLog.Info("Queue skipped Universal RP because URP is already detected for this project.");
                return true;
            }

            if (CCSPackageStatusService.IsListReady() && CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
            {
                CCSEditorLog.Info($"Queue skipped '{definition.DisplayName}' because Package Manager reports it as installed.");
                return true;
            }

            return false;
        }

        public static void ClearFailedFlags()
        {
            FailedDefinitionIds.Clear();
            RaiseStateChanged();
        }

        /// <summary>True when the queue skipped this definition because the package was already installed (no Client.Add).</summary>
        public static bool IsSkipped(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId) && SkippedDefinitionIds.Contains(definitionId);
        }

        /// <summary>Re-queues a failed definition after the user chooses Retry (one Client.Add at a time).</summary>
        public static void RetryFailedDefinition(CCSPackageDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.Id) || !FailedDefinitionIds.Contains(definition.Id))
            {
                return;
            }

            FailedDefinitionIds.Remove(definition.Id);
            InstallQueue.Enqueue(definition);
            batchProgressTotal++;
            PersistQueueToSession();
            RegisterEditorUpdate();
            RaiseStateChanged();
            CCSEditorLog.Info($"Retry queued for '{definition.DisplayName}' ({definition.Id}).");
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

        /// <summary>
        /// Overall install progress for the current batch (0–1). Returns -1 when progress cannot be quantified (e.g. queue restored after domain reload).
        /// </summary>
        public static float GetInstallBatchProgressNormalized()
        {
            if (batchProgressIndeterminate)
            {
                return -1f;
            }

            if (batchProgressTotal <= 0)
            {
                return IsBusy() ? 0f : 1f;
            }

            float p = (float)batchProgressProcessed / batchProgressTotal;
            if (activeAddRequest != null && !activeAddRequest.IsCompleted)
            {
                p += Mathf.Clamp01(0.35f / batchProgressTotal);
            }

            return Mathf.Clamp01(p);
        }

        public static bool TryGetInstallBatchProgressCounts(out int processed, out int total)
        {
            processed = batchProgressProcessed;
            total = batchProgressTotal;
            return batchProgressTotal > 0 && !batchProgressIndeterminate;
        }

        /// <summary>
        /// Clears queued installs and session-backed queue ids for a first-run state reset. Does not cancel an in-flight <c>Client.Add</c>;
        /// if one is active, it is allowed to finish; the required-pass completion path will be driven by the next pipeline run.
        /// </summary>
        public static void ResetPipelineStateForFirstRunStateReset()
        {
            InstallQueue.Clear();
            FailedDefinitionIds.Clear();
            SkippedDefinitionIds.Clear();
            LastBatchSuccessDisplayNames.Clear();
            autoRequiredPassActive = false;
            waitingForPackageListBeforeDequeue = false;
            postReloadInstallQueueHint = false;
            batchProgressTotal = 0;
            batchProgressProcessed = 0;
            batchProgressIndeterminate = false;
            SessionState.SetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, false);
            SessionState.EraseString(CCSSetupConstants.SessionStatePendingInstallQueueIds);

            if (activeAddRequest != null)
            {
                CCSEditorLog.Warning(
                    "First-run reset: a Package Manager Client.Add is still in progress; queue and session ids were cleared. "
                    + "Wait for it to finish or use Force run first-run pipeline after it completes.");
            }
            else
            {
                UnregisterEditorUpdateIfIdle();
            }

            RaiseStateChanged();
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
                if (SessionState.GetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, false))
                {
                    autoRequiredPassActive = true;
                }

                batchProgressIndeterminate = true;
                batchProgressTotal = 0;
                batchProgressProcessed = 0;
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

            if (autoRequiredPassActive)
            {
                CCSHubRequiredDependencyBootstrap.NotifyAutoRequiredBatchFinished(LastBatchSuccessDisplayNames);
                autoRequiredPassActive = false;
                SessionState.SetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, false);
            }

            ResetBatchProgressTracking();
        }

        private static void ResetBatchProgressTracking()
        {
            batchProgressTotal = 0;
            batchProgressProcessed = 0;
            batchProgressIndeterminate = false;
        }

        private static void IncrementBatchProgressProcessed()
        {
            batchProgressProcessed++;
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
                SkippedDefinitionIds.Add(next.Id);
                IncrementBatchProgressProcessed();
                RaiseStateChanged();
                return;
            }

            if (CCSPackageStatusService.IsPackageInstalled(next.PackageId))
            {
                CCSEditorLog.Info($"Install step skipped; already present: {next.DisplayName}");
                SkippedDefinitionIds.Add(next.Id);
                IncrementBatchProgressProcessed();
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
                PackageInstallSucceeded?.Invoke(finished);
            }
            else
            {
                string message = activeAddRequest.Error != null ? activeAddRequest.Error.message : "Unknown error.";
                CCSEditorLog.Error($"Client.Add failed for '{finished.DisplayName}': {message}");
                FailedDefinitionIds.Add(finished.Id);
            }

            IncrementBatchProgressProcessed();
        }

        private static void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        #endregion
    }
}
