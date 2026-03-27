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
        /// <summary>
        /// When the Git repo embeds a dev project under Assets/CCS/CharacterController, only these folders are copied
        /// (excludes nested Assets/Starter Assets, Packages/, ProjectSettings/, Docs/, etc.).
        /// </summary>
        /// <summary>
        /// Top-level folders under <c>Assets/CCS/CharacterController</c> in the package.
        /// Do not list parallel <c>Runtime</c> or <c>Editor</c> here: assemblies live only under
        /// <c>Scripts/Runtime</c> and <c>Scripts/Editor</c>. Copying both <c>Scripts</c> and root
        /// <c>Runtime</c>/<c>Editor</c> duplicates the same <c>CCS.CharacterController.Runtime</c> / <c>CCS.CharacterController.Editor</c> assembly definitions.
        /// </summary>
        private static readonly string[] EmbeddedCcsCharacterControllerFolders =
        {
            "Scripts",
            "Content",
            "Animations",
            "Samples~",
        };

        /// <summary>Standard UPM layout at package root when no embedded Assets/CCS tree exists.</summary>
        private static readonly string[] RootUpmPackageFolders =
        {
            "Runtime",
            "Editor",
            "Content",
            "Animations",
            "Samples~",
        };

        /// <summary>
        /// Folders merged from package root when embedded layout was used but omits a standard UPM folder (e.g. Runtime at root only).
        /// </summary>
        private static readonly string[] SupplementFromRootIfMissingInDest =
        {
            "Runtime",
            "Editor",
            "Content",
            "Animations",
            "Samples~",
        };

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

                if (SessionState.GetBool(CCSSetupConstants.SessionStateDotweenCopyPending, false))
                {
                    SessionState.SetBool(CCSSetupConstants.SessionStateDotweenCopyPending, false);
                    string bundlePath = Path.Combine(
                        sourceRoot,
                        "Assets",
                        "CCS",
                        "CharacterController",
                        CCSDotweenBundleInstaller.BundleFolderName);
                    if (Directory.Exists(bundlePath))
                    {
                        if (!CCSDotweenBundleInstaller.TryCopyFromBundleRoot(bundlePath, out string dotweenErr))
                        {
                            CCSEditorLog.Warning($"CCS Hub: DOTween copy failed: {dotweenErr}");
                        }
                    }
                    else
                    {
                        CCSEditorLog.Warning(
                            "CCS Hub: DOTween was requested but DemigiantDOTweenBundle was not found in the Character Controller package.");
                    }
                }

                CCSProjectFolderUtility.CreateDefaultCcsFolderStructure();

                string destRoot = Path.Combine(Application.dataPath, "CCS", "CharacterController");
                if (Directory.Exists(destRoot))
                {
                    FileUtil.DeleteFileOrDirectory(destRoot);
                }

                Directory.CreateDirectory(destRoot);
                CopyCharacterControllerPackageIntoAssets(sourceRoot, destRoot);
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

        /// <summary>
        /// Does not copy the entire package tree: dev repos often contain Assets/Starter Assets, ProjectSettings, nested Packages/, etc.
        /// Those must stay out of Assets/CCS/CharacterController to avoid GUID conflicts and duplicate assemblies.
        /// </summary>
        private static void CopyCharacterControllerPackageIntoAssets(string packageRoot, string destRoot)
        {
            string embeddedCcs = Path.Combine(packageRoot, "Assets", "CCS", "CharacterController");
            if (Directory.Exists(embeddedCcs))
            {
                int copied = CCSAssetFolderCopyUtility.CopyExistingTopLevelFolders(
                    embeddedCcs,
                    destRoot,
                    EmbeddedCcsCharacterControllerFolders,
                    skipUpmPackageManifest: true);
                if (copied > 0)
                {
                    CCSEditorLog.Info(
                        "CCS Hub: Copied Character Controller from package path Assets/CCS/CharacterController (Starter Assets, template Scenes/Settings, and other project folders are excluded).");
                    SupplementPackageRootFoldersIfMissing(packageRoot, destRoot);
                    CopyPluginsAndResourcesToAssetsRoot(packageRoot, embeddedCcs);
                    return;
                }

                CCSEditorLog.Warning(
                    "CCS Hub: Package contains Assets/CCS/CharacterController but no expected subfolders (Scripts, Content, …). Falling back to package-root UPM layout.");
            }

            int rootCopied = CCSAssetFolderCopyUtility.CopyExistingTopLevelFolders(
                packageRoot,
                destRoot,
                RootUpmPackageFolders,
                skipUpmPackageManifest: true);
            if (rootCopied == 0)
            {
                CCSEditorLog.Error(
                    "CCS Hub: Character Controller package has no bootstrappable folders. Expected either Assets/CCS/CharacterController/{Scripts|Content|…} or package-root {Runtime|Editor|Content|…}.");
            }
            else
            {
                CCSEditorLog.Info("CCS Hub: Copied Character Controller from package root (Runtime/Editor/Content/…).");
            }

            CopyPluginsAndResourcesToAssetsRoot(packageRoot, embeddedCcs);
        }

        /// <summary>
        /// Merges <c>Plugins</c> and <c>Resources</c> to <c>Assets/Plugins</c> and <c>Assets/Resources</c> (siblings of <c>Assets/CCS</c>),
        /// not under <c>Assets/CCS/CharacterController</c>, matching Unity’s expected layout.
        /// </summary>
        private static void CopyPluginsAndResourcesToAssetsRoot(string packageRoot, string embeddedCcsPath)
        {
            string assetsDataPath = Application.dataPath;

            void MergeIfExists(string sourceDir, string destName)
            {
                if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
                {
                    return;
                }

                string destPath = Path.Combine(assetsDataPath, destName);
                CCSAssetFolderCopyUtility.CopyFilesOnlySkipEmptyDirectories(sourceDir, destPath, skipUpmPackageManifest: true);
                CCSEditorLog.Info($"CCS Hub: Merged '{destName}' into Assets/{destName}/ (project root, not under CCS).");
            }

            if (!string.IsNullOrEmpty(embeddedCcsPath) && Directory.Exists(embeddedCcsPath))
            {
                MergeIfExists(Path.Combine(embeddedCcsPath, "Plugins"), "Plugins");
                MergeIfExists(Path.Combine(embeddedCcsPath, "Resources"), "Resources");
            }

            MergeIfExists(Path.Combine(packageRoot, "Plugins"), "Plugins");
            MergeIfExists(Path.Combine(packageRoot, "Resources"), "Resources");
        }

        /// <summary>
        /// If the embedded tree used Scripts/ but Runtime/ lives only at package root, merge missing standard folders.
        /// Skips supplementing package-root <c>Runtime</c> / <c>Editor</c> when the embedded layout already uses
        /// <c>Scripts/Runtime</c> / <c>Scripts/Editor</c> (otherwise both trees compile the same assembly names).
        /// </summary>
        private static void SupplementPackageRootFoldersIfMissing(string packageRoot, string destRoot)
        {
            bool embeddedUsesScriptsRuntime = Directory.Exists(Path.Combine(destRoot, "Scripts", "Runtime"));
            bool embeddedUsesScriptsEditor = Directory.Exists(Path.Combine(destRoot, "Scripts", "Editor"));

            for (int index = 0; index < SupplementFromRootIfMissingInDest.Length; index++)
            {
                string name = SupplementFromRootIfMissingInDest[index];
                if (Directory.Exists(Path.Combine(destRoot, name)))
                {
                    continue;
                }

                if (string.Equals(name, "Runtime", StringComparison.Ordinal) && embeddedUsesScriptsRuntime)
                {
                    continue;
                }

                if (string.Equals(name, "Editor", StringComparison.Ordinal) && embeddedUsesScriptsEditor)
                {
                    continue;
                }

                string src = Path.Combine(packageRoot, name);
                if (!Directory.Exists(src))
                {
                    continue;
                }

                string dst = Path.Combine(destRoot, name);
                CCSAssetFolderCopyUtility.CopyFilesOnlySkipEmptyDirectories(src, dst, skipUpmPackageManifest: true);
                CCSEditorLog.Info($"CCS Hub: Supplemented '{name}' from package root (not present under embedded Assets/CCS/CharacterController).");
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
