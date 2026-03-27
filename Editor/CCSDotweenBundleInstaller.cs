// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSDotweenBundleInstaller
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Summary: Copies bundled Demigiant DOTween (Plugins + Resources) from the Character Controller package into the project Assets root.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.IO;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Optional Asset Store–style DOTween files shipped under
    /// <c>Assets/CCS/CharacterController/DemigiantDOTweenBundle~</c> in <c>com.crazycarrot.charactercontroller</c> (not copied into Assets/CCS by bootstrap).
    /// The folder name ends with <c>~</c> so Unity does not import it as assets (avoids duplicate types when <c>Assets/Plugins/Demigiant</c> already exists).
    /// </summary>
    public static class CCSDotweenBundleInstaller
    {
        /// <summary>Folder under package or project <c>Assets/CCS/CharacterController</c> containing <c>Plugins</c> and <c>Resources</c> trees.</summary>
        public const string BundleFolderName = "DemigiantDOTweenBundle~";

        /// <summary>
        /// True when Demigiant DOTween appears to already be merged under <c>Assets/Plugins</c> (avoids redundant copy).
        /// </summary>
        public static bool IsDemigiantDotweenPresentInProject()
        {
            string demigiant = Path.Combine(Application.dataPath, "Plugins", "Demigiant");
            return Directory.Exists(demigiant);
        }

        /// <summary>
        /// Merges <c>Plugins</c> and <c>Resources</c> from the bundle into <c>Assets/Plugins</c> and <c>Assets/Resources</c>.
        /// </summary>
        public static bool TryCopyDemigiantIntoProject(out string errorMessage)
        {
            if (!TryResolveBundleRoot(out string bundleRoot, out errorMessage))
            {
                return false;
            }

            return TryCopyFromBundleRoot(bundleRoot, out errorMessage);
        }

        /// <summary>
        /// Copies from an explicit bundle root (e.g. inside the resolved Character Controller UPM package before bootstrap removes it).
        /// </summary>
        public static bool TryCopyFromBundleRoot(string bundleRoot, out string errorMessage)
        {
            if (string.IsNullOrEmpty(bundleRoot) || !Directory.Exists(bundleRoot))
            {
                errorMessage = "Invalid or missing DOTween bundle folder.";
                return false;
            }

            string pluginsSrc = Path.Combine(bundleRoot, "Plugins");
            string resourcesSrc = Path.Combine(bundleRoot, "Resources");
            if (!Directory.Exists(pluginsSrc))
            {
                errorMessage = $"DOTween bundle has no Plugins folder at {pluginsSrc}.";
                return false;
            }

            string dataPath = UnityEngine.Application.dataPath;
            CCSAssetFolderCopyUtility.CopyFilesOnlySkipEmptyDirectories(
                pluginsSrc,
                Path.Combine(dataPath, "Plugins"),
                skipUpmPackageManifest: true);

            if (Directory.Exists(resourcesSrc))
            {
                CCSAssetFolderCopyUtility.CopyFilesOnlySkipEmptyDirectories(
                    resourcesSrc,
                    Path.Combine(dataPath, "Resources"),
                    skipUpmPackageManifest: true);
            }

            AssetDatabase.Refresh();
            CCSEditorLog.Info("CCS Hub: Copied Demigiant DOTween bundle into Assets/Plugins and Assets/Resources.");
            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Resolves the bundle root: <c>com.crazycarrot.charactercontroller</c> package on disk, else the embedded project path under Assets.
        /// </summary>
        public static bool TryResolveBundleRoot(out string bundleRoot, out string errorMessage)
        {
            string fromPackage = TryGetBundleFromPackage();
            if (!string.IsNullOrEmpty(fromPackage))
            {
                bundleRoot = fromPackage;
                errorMessage = null;
                return true;
            }

            string projectAssets = Path.Combine(UnityEngine.Application.dataPath, "CCS", "CharacterController", BundleFolderName);
            if (Directory.Exists(projectAssets))
            {
                bundleRoot = projectAssets;
                errorMessage = null;
                return true;
            }

            bundleRoot = null;
            errorMessage =
                "DOTween bundle not found. Add the CCS Character Controller package (com.crazycarrot.charactercontroller) or ensure Assets/CCS/CharacterController/DemigiantDOTweenBundle~ exists (tilde keeps the bundle off the import pipeline).";
            return false;
        }

        private static string TryGetBundleFromPackage()
        {
            string packageJsonPath = $"Packages/{CCSSetupConstants.CharacterControllerPackageId}/package.json";
            UnityEditor.PackageManager.PackageInfo info =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageJsonPath);
            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
            {
                return null;
            }

            string candidate = Path.Combine(
                info.resolvedPath,
                "Assets",
                "CCS",
                "CharacterController",
                BundleFolderName);
            return Directory.Exists(candidate) ? candidate : null;
        }
    }
}
