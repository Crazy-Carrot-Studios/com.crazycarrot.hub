// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageStatusService
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
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
        private static bool listReady;
        private static Action pendingOnComplete;

        #endregion

        #region Public Methods

        public static bool IsListReady()
        {
            return listReady;
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
                CCSSetupDiagnosticTrace.Log("PackageStatus RefreshInstalledPackages — refresh already in progress, chaining callback");
                pendingOnComplete += onComplete;
                return;
            }

            CCSSetupDiagnosticTrace.Log("PackageStatus RefreshInstalledPackages — Client.List started");
            listRefreshInProgress = true;
            listReady = false;
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
                    CCSEditorLog.Info($"Package list refresh succeeded with {InstalledPackageNames.Count} entries.");
                }
                else
                {
                    string message = listRequest.Error != null ? listRequest.Error.message : "Unknown error.";
                    CCSEditorLog.Warning($"Package list refresh failed: {message}");
                    listReady = true;
                }

                listRequest = null;
                listRefreshInProgress = false;
                CCSSetupDiagnosticTrace.Log(
                    $"PackageStatus list refresh finished — listReady={listReady} entryCount={InstalledPackageNames.Count}");
                onComplete?.Invoke();
                Action chained = pendingOnComplete;
                pendingOnComplete = null;
                chained?.Invoke();
            }
        }

        #endregion
    }
}
