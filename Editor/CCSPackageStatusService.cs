// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageStatusService
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: Loads Package Manager package names and resolves whether registry package IDs are installed.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace CCS.Hub.Editor
{
    public static class CCSPackageStatusService
    {
        #region Variables

        private static ListRequest listRequest;
        private static readonly HashSet<string> InstalledPackageNames = new HashSet<string>();
        private static bool listRefreshInProgress;
        /// <summary>True only after a <em>successful</em> Client.List — never after a failed refresh.</summary>
        private static bool listReady;

        /// <summary>Set when the last completed refresh failed; cleared when a new refresh starts or succeeds.</summary>
        private static bool lastListRefreshFailed;
        private static Action pendingOnComplete;

        #endregion

        #region Public Methods

        /// <summary>True only when the last package list refresh completed successfully with usable data.</summary>
        public static bool IsListReady()
        {
            return listReady;
        }

        /// <summary>True while a Client.List request is in flight.</summary>
        public static bool IsListRefreshInProgress()
        {
            return listRefreshInProgress;
        }

        /// <summary>
        /// True when the most recent list refresh finished with an error. Required-flow callers must not treat package membership as known.
        /// </summary>
        public static bool IsLastPackageListRefreshFailed()
        {
            return lastListRefreshFailed;
        }

        public static bool IsPackageInstalled(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return false;
            }

            return InstalledPackageNames.Contains(packageName);
        }

        public static void RefreshInstalledPackages(Action onComplete)
        {
            if (listRefreshInProgress)
            {
                pendingOnComplete += onComplete;
                return;
            }

            listRefreshInProgress = true;
            listReady = false;
            lastListRefreshFailed = false;
            listRequest = Client.List(true);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            void OnEditorUpdate()
            {
                if (listRequest == null || !listRequest.IsCompleted)
                {
                    return;
                }

                EditorApplication.update -= OnEditorUpdate;
                InstalledPackageNames.Clear();
                if (listRequest.Status == StatusCode.Success && listRequest.Result != null)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo package in listRequest.Result)
                    {
                        InstalledPackageNames.Add(package.name);
                    }

                    listReady = true;
                    lastListRefreshFailed = false;
                    CCSEditorLog.Info($"Package list refresh succeeded with {InstalledPackageNames.Count} entries.");
                }
                else
                {
                    string message = listRequest.Error != null ? listRequest.Error.message : "Unknown error.";
                    CCSEditorLog.Warning($"Package list refresh failed: {message}");
                    listReady = false;
                    lastListRefreshFailed = true;
                }

                listRequest = null;
                listRefreshInProgress = false;
                onComplete?.Invoke();
                Action chained = pendingOnComplete;
                pendingOnComplete = null;
                chained?.Invoke();
            }
        }

        #endregion
    }
}
