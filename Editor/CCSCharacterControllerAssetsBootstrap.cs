// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSCharacterControllerAssetsBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: After Package Manager installs the Character Controller Git UPM package under Packages/, copies its contents into Assets/CCS/CharacterController and removes the package entry so the project owns editable sources (import-package workflow). Materializes Samples~/BasicSetup into BasicSetup when present.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using CCS.Hub;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Ensures the Git UPM package is the delivery mechanism, then bootstraps editable content under Assets/CCS/CharacterController.
    /// Technically: Package Manager keeps sources under Packages/; this step imports/copies them into Assets/CCS (not a second UPM install into Assets).
    /// </summary>
    [InitializeOnLoad]
    public static class CCSCharacterControllerAssetsBootstrap
    {
        private static readonly HashSet<string> BootstrapFailedDefinitionIds = new HashSet<string>();

        private static bool bootstrapBusy;

        private static RemoveRequest activeRemoveRequest;

        private static int resolvePackageInfoAttemptCount;

        static CCSCharacterControllerAssetsBootstrap()
        {
            CCSPackageInstallService.PackageInstallSucceeded += OnPackageInstallSucceeded;
            EditorApplication.delayCall += OnEditorLoadedDeferred;
        }

        public static event Action StateChanged;

        /// <summary>True while copying into Assets or removing the transient UPM package entry.</summary>
        public static bool IsBootstrapBusy => bootstrapBusy;

        public static bool IsFailed(string definitionId)
        {
            return BootstrapFailedDefinitionIds.Contains(definitionId);
        }

        /// <summary>Lets the user retry after a failed bootstrap without leaving the editor.</summary>
        public static void ClearBootstrapFailureState()
        {
            BootstrapFailedDefinitionIds.Remove(CCSSetupConstants.CharacterControllerDefinitionId);
        }

        /// <summary>
        /// True when Character Controller project content is present under Assets/CCS/CharacterController (editable tree).
        /// </summary>
        public static bool IsCharacterControllerProjectImportComplete()
        {
            string root = Path.Combine(Application.dataPath, "CCS", "CharacterController");
            if (!Directory.Exists(root))
            {
                return false;
            }

            if (Directory.Exists(Path.Combine(root, "Runtime"))
                || Directory.Exists(Path.Combine(root, "Scripts"))
                || Directory.Exists(Path.Combine(root, "Editor")))
            {
                return true;
            }

            string basic = Path.Combine(root, "BasicSetup");
            if (Directory.Exists(basic) && Directory.GetFiles(basic, "*", SearchOption.AllDirectories).Length > 0)
            {
                return true;
            }

            string content = Path.Combine(root, "Content");
            if (Directory.Exists(content) && Directory.GetFiles(content, "*", SearchOption.AllDirectories).Length > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// If Samples~/BasicSetup exists under the Assets tree, mirror into BasicSetup so Unity imports assets (Samples~ stays hidden).
        /// </summary>
        public static void TryMaterializeSamplesBasicSetupIfNeeded()
        {
            string root = Path.Combine(Application.dataPath, "CCS", "CharacterController");
            string samplesBasic = Path.Combine(root, "Samples~", "BasicSetup");
            string destBasic = Path.Combine(root, "BasicSetup");
            if (!Directory.Exists(samplesBasic))
            {
                return;
            }

            if (Directory.Exists(destBasic))
            {
                string[] existing = Directory.GetFiles(destBasic, "*", SearchOption.AllDirectories);
                if (existing != null && existing.Length > 0)
                {
                    return;
                }

                FileUtil.DeleteFileOrDirectory(destBasic);
            }

            CCSAssetFolderCopyUtility.CopyFilesOnlySkipEmptyDirectories(samplesBasic, destBasic, skipUpmPackageManifest: false);
            AssetDatabase.Refresh();
            CCSEditorLog.Info("CCS Hub: Materialized Samples~/BasicSetup into Assets/CCS/CharacterController/BasicSetup.");
        }

        private static void OnPackageInstallSucceeded(CCSPackageDefinition definition)
        {
            if (definition.Id != CCSSetupConstants.CharacterControllerDefinitionId)
            {
                return;
            }

            if (IsCharacterControllerProjectImportComplete())
            {
                return;
            }

            bootstrapBusy = true;
            BootstrapFailedDefinitionIds.Remove(definition.Id);
            RaiseStateChanged();
            EditorApplication.delayCall += () => RunBootstrapFromInstalledPackage(definition.PackageId);
        }

        private static void OnEditorLoadedDeferred()
        {
            TryMaterializeSamplesBasicSetupIfNeeded();

            if (IsCharacterControllerProjectImportComplete() || bootstrapBusy || activeRemoveRequest != null)
            {
                return;
            }

            if (!CCSPackageStatusService.IsListReady())
            {
                CCSPackageStatusService.RefreshInstalledPackages(() => EditorApplication.delayCall += OnEditorLoadedDeferred);
                return;
            }

            if (!CCSPackageStatusService.IsPackageInstalled(CCSSetupConstants.CharacterControllerPackageId))
            {
                return;
            }

            bootstrapBusy = true;
            RaiseStateChanged();
            EditorApplication.delayCall += () => RunBootstrapFromInstalledPackage(CCSSetupConstants.CharacterControllerPackageId);
        }

        private static void RunBootstrapFromInstalledPackage(string packageId)
        {
            UnityEditor.PackageManager.PackageInfo info = TryResolvePackageInfo(packageId);
            if (info == null)
            {
                if (resolvePackageInfoAttemptCount < 12)
                {
                    resolvePackageInfoAttemptCount++;
                    EditorApplication.delayCall += () => RunBootstrapFromInstalledPackage(packageId);
                    return;
                }

                resolvePackageInfoAttemptCount = 0;
                CCSEditorLog.Error(
                    "CCS Hub: Could not resolve Character Controller package on disk after install. Open Package Manager and retry, or check the Git URL.");
                BootstrapFailedDefinitionIds.Add(CCSSetupConstants.CharacterControllerDefinitionId);
                bootstrapBusy = false;
                RaiseStateChanged();
                return;
            }

            resolvePackageInfoAttemptCount = 0;

            try
            {
                EditorUtility.DisplayProgressBar("CCS Hub", "Importing Character Controller into Assets/CCS…", 0.2f);

                string sourceRoot = info.resolvedPath;
                if (string.IsNullOrEmpty(sourceRoot) || !Directory.Exists(sourceRoot))
                {
                    CCSEditorLog.Error("CCS Hub: Character Controller package resolvedPath is invalid.");
                    BootstrapFailedDefinitionIds.Add(CCSSetupConstants.CharacterControllerDefinitionId);
                    return;
                }

                CCSProjectFolderUtility.CreateDefaultCcsFolderStructure();

                string destRoot = Path.Combine(Application.dataPath, "CCS", "CharacterController");
                if (Directory.Exists(destRoot))
                {
                    FileUtil.DeleteFileOrDirectory(destRoot);
                }

                Directory.CreateDirectory(destRoot);
                CCSAssetFolderCopyUtility.CopyFilesOnlySkipEmptyDirectories(sourceRoot, destRoot, skipUpmPackageManifest: true);
                AssetDatabase.Refresh();
                TryMaterializeSamplesBasicSetupIfNeeded();
                EditorUtility.ClearProgressBar();

                CCSEditorLog.Info(
                    $"CCS Hub: Character Controller sources copied to {CCSSetupConstants.CharacterControllerAssetsRoot}. Removing UPM package entry to avoid duplicate scripts.");

                BeginRemovePackage(packageId);
            }
            catch (Exception exception)
            {
                EditorUtility.ClearProgressBar();
                CCSEditorLog.Error($"CCS Hub: Character Controller bootstrap failed: {exception.Message}");
                BootstrapFailedDefinitionIds.Add(CCSSetupConstants.CharacterControllerDefinitionId);
            }
            finally
            {
                if (activeRemoveRequest == null)
                {
                    bootstrapBusy = false;
                    RaiseStateChanged();
                }
            }
        }

        private static UnityEditor.PackageManager.PackageInfo TryResolvePackageInfo(string packageId)
        {
            string path = $"Packages/{packageId}/package.json";
            return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
        }

        private static void BeginRemovePackage(string packageId)
        {
            activeRemoveRequest = Client.Remove(packageId);
            EditorApplication.update -= PollRemoveRequest;
            EditorApplication.update += PollRemoveRequest;
        }

        private static void PollRemoveRequest()
        {
            if (activeRemoveRequest == null || !activeRemoveRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= PollRemoveRequest;

            if (activeRemoveRequest.Status == StatusCode.Success)
            {
                CCSEditorLog.Info(
                    "CCS Hub: Removed the Character Controller package entry from Package Manager after copying into Assets (expected; your editable copy lives under Assets/CCS/CharacterController).");
            }
            else
            {
                string message = activeRemoveRequest.Error != null ? activeRemoveRequest.Error.message : "Unknown error.";
                CCSEditorLog.Error(
                    $"CCS Hub: Could not remove the Character Controller package after copy. Remove it manually in Package Manager to avoid duplicate scripts: {message}");
                BootstrapFailedDefinitionIds.Add(CCSSetupConstants.CharacterControllerDefinitionId);
            }

            activeRemoveRequest = null;
            bootstrapBusy = false;
            CCSPackageStatusService.RefreshInstalledPackages(RaiseStateChanged);
        }

        private static void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
