// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSCharacterControllerAssetsImportService
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: Downloads public GitHub archive and imports CCS Character Controller into Assets/CCS/CharacterController only (not Package Manager). Copies files only so empty folders are not created.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CCS.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Brings Character Controller sources into <see cref="CCSSetupConstants.CharacterControllerAssetsRoot"/> via GitHub zip (never UPM).
    /// </summary>
    public static class CCSCharacterControllerAssetsImportService
    {
        private static readonly HashSet<string> FailedDefinitionIds = new HashSet<string>();

        private static string activeImportDefinitionId;

        public static event Action StateChanged;

        public static bool IsImportInProgress => !string.IsNullOrEmpty(activeImportDefinitionId);

        public static bool IsFailed(string definitionId)
        {
            return FailedDefinitionIds.Contains(definitionId);
        }

        public static bool IsImporting(string definitionId)
        {
            return activeImportDefinitionId == definitionId;
        }

        /// <summary>
        /// True when Character Controller sources are present under Assets/CCS/CharacterController.
        /// Hub imports omit <c>package.json</c> so Unity does not list it as a UPM package; we detect via folders (or legacy package.json).
        /// </summary>
        public static bool IsCharacterControllerImportedIntoAssets()
        {
            string root = Path.Combine(Application.dataPath, "CCS", "CharacterController");
            if (!Directory.Exists(root))
            {
                return false;
            }

            if (File.Exists(Path.Combine(root, "package.json")))
            {
                return true;
            }

            return Directory.Exists(Path.Combine(root, "Runtime"))
                || Directory.Exists(Path.Combine(root, "Scripts"))
                || Directory.Exists(Path.Combine(root, "Editor"));
        }

        public static void StartImport(CCSPackageDefinition definition)
        {
            if (definition.Id != CCSSetupConstants.CharacterControllerDefinitionId
                || definition.SourceType != CCSPackageSourceType.AssetsGitImport)
            {
                CCSEditorLog.Warning("CCS Hub: Character Controller assets import was requested for an unexpected definition.");
                return;
            }

            if (IsImportInProgress)
            {
                CCSEditorLog.Warning("CCS Hub: A Character Controller import is already running.");
                return;
            }

            if (IsCharacterControllerImportedIntoAssets())
            {
                CCSEditorLog.Info(
                    "CCS Hub: Character Controller is already present under Assets/CCS/CharacterController; skipping import.");
                return;
            }

            if (!TryBuildGitHubArchiveZipUrl(definition.InstallIdentifier, CCSSetupConstants.CharacterControllerGitBranch, out string zipUrl))
            {
                CCSEditorLog.Error(
                    "CCS Hub: Character Controller import needs a public https://github.com/... Git URL. Private repos require a manual workflow.");
                FailedDefinitionIds.Add(definition.Id);
                RaiseStateChanged();
                return;
            }

            FailedDefinitionIds.Remove(definition.Id);
            activeImportDefinitionId = definition.Id;
            RaiseStateChanged();

            string zipPath = Path.Combine(Application.temporaryCachePath, $"ccs-cc-import-{Guid.NewGuid():N}.zip");
            var request = new UnityWebRequest(zipUrl);
            request.downloadHandler = new DownloadHandlerFile(zipPath);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => OnZipDownloadCompleted(request, zipPath, definition.Id);
        }

        private static void OnZipDownloadCompleted(UnityWebRequest request, string zipPath, string definitionId)
        {
            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string err = request.error ?? request.result.ToString();
                    CCSEditorLog.Error($"CCS Hub: Character Controller download failed: {err}");
                    FailedDefinitionIds.Add(definitionId);
                    return;
                }

                EditorUtility.DisplayProgressBar("CCS Hub", "Extracting Character Controller…", 0.45f);

                string extractRoot = Path.Combine(Application.temporaryCachePath, $"ccs-cc-extract-{Guid.NewGuid():N}");
                Directory.CreateDirectory(extractRoot);

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, extractRoot);
                }
                catch (Exception exception)
                {
                    CCSEditorLog.Error($"CCS Hub: Failed to extract Character Controller archive: {exception.Message}");
                    FailedDefinitionIds.Add(definitionId);
                    return;
                }

                string[] topLevel = Directory.GetDirectories(extractRoot);
                if (topLevel.Length != 1)
                {
                    CCSEditorLog.Error("CCS Hub: Unexpected GitHub archive layout (expected one root folder).");
                    FailedDefinitionIds.Add(definitionId);
                    return;
                }

                string innerSource = topLevel[0];
                CCSProjectFolderUtility.CreateDefaultCcsFolderStructure();

                string destRoot = Path.Combine(Application.dataPath, "CCS", "CharacterController");
                if (Directory.Exists(destRoot))
                {
                    FileUtil.DeleteFileOrDirectory(destRoot);
                }

                Directory.CreateDirectory(destRoot);
                CopyFilesOnlySkipEmptyDirectories(innerSource, destRoot, skipUpmPackageManifest: true);

                AssetDatabase.Refresh();

                TryStripPackageManifestFromCharacterControllerAssets();

                TryMaterializeSamplesBasicSetupIfNeeded();

                CCSEditorLog.Info($"CCS Hub: Character Controller imported into {CCSSetupConstants.CharacterControllerAssetsRoot}.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (File.Exists(zipPath))
                {
                    try
                    {
                        File.Delete(zipPath);
                    }
                    catch (IOException)
                    {
                        // Best-effort temp cleanup.
                    }
                }

                request.Dispose();
                activeImportDefinitionId = null;
                RaiseStateChanged();
            }
        }

        /// <summary>
        /// If Samples~/BasicSetup exists under the imported tree, mirror into BasicSetup so Unity imports assets (Samples~ stays hidden).
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

            CopyFilesOnlySkipEmptyDirectories(samplesBasic, destBasic, skipUpmPackageManifest: false);
            AssetDatabase.Refresh();
            CCSEditorLog.Info("CCS Hub: Materialized Samples~/BasicSetup into Assets/CCS/CharacterController/BasicSetup.");
        }

        /// <summary>
        /// Removes a legacy UPM <c>package.json</c> from the Character Controller Assets folder so Unity does not register it under Packages.
        /// Safe once Runtime/Scripts content exists.
        /// </summary>
        public static void TryStripPackageManifestFromCharacterControllerAssets()
        {
            string root = Path.Combine(Application.dataPath, "CCS", "CharacterController");
            string physicalManifest = Path.Combine(root, "package.json");
            if (!File.Exists(physicalManifest))
            {
                return;
            }

            if (!Directory.Exists(Path.Combine(root, "Runtime"))
                && !Directory.Exists(Path.Combine(root, "Scripts")))
            {
                return;
            }

            string assetPath = $"{CCSSetupConstants.CharacterControllerAssetsRoot}/package.json";
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                CCSEditorLog.Info("CCS Hub: Removed package.json under Assets/CCS/CharacterController so Character Controller stays Assets-only (not a UPM package).");
            }
        }

        internal static void CopyFilesOnlySkipEmptyDirectories(string sourceRoot, string destinationRoot, bool skipUpmPackageManifest)
        {
            sourceRoot = Path.GetFullPath(sourceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            destinationRoot = Path.GetFullPath(destinationRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (!Directory.Exists(sourceRoot))
            {
                return;
            }

            foreach (string filePath in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = filePath.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (skipUpmPackageManifest && ShouldSkipUpmPackageManifestFile(relative))
                {
                    continue;
                }

                string destPath = Path.Combine(destinationRoot, relative);
                string destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(filePath, destPath, true);
            }
        }

        /// <summary>Skips repository-root UPM manifest so imported content is plain Assets, not an installable package.</summary>
        private static bool ShouldSkipUpmPackageManifestFile(string relativePath)
        {
            string norm = relativePath.Replace('\\', '/');
            return norm == "package.json" || norm == "package.json.meta";
        }

        private static bool TryBuildGitHubArchiveZipUrl(string gitHttpsUrl, string branch, out string zipUrl)
        {
            zipUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(gitHttpsUrl))
            {
                return false;
            }

            string trimmed = gitHttpsUrl.Trim();
            if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 4);
            }

            trimmed = trimmed.TrimEnd('/');

            if (!trimmed.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            zipUrl = $"{trimmed}/archive/refs/heads/{branch}.zip";
            return true;
        }

        private static void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
