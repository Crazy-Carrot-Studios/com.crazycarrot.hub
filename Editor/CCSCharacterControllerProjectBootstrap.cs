// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSCharacterControllerProjectBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: After the Character Controller UPM package is installed, creates Assets/CCS and copies package sample content into Assets when present.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.IO;
using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// UPM keeps packages under Packages/; this bootstrap copies starter content from the package's Samples~ folder into Assets/CCS.
    /// </summary>
    [InitializeOnLoad]
    public static class CCSCharacterControllerProjectBootstrap
    {
        static CCSCharacterControllerProjectBootstrap()
        {
            CCSPackageInstallService.PackageInstallSucceeded += OnPackageInstallSucceeded;
        }

        private static void OnPackageInstallSucceeded(CCSPackageDefinition definition)
        {
            if (definition.Id != CCSSetupConstants.CharacterControllerDefinitionId)
            {
                return;
            }

            EditorApplication.delayCall += RunBootstrap;
        }

        private static void RunBootstrap()
        {
            if (!CCSPackageStatusService.IsPackageInstalled("com.crazycarrot.charactercontroller"))
            {
                return;
            }

            CCSProjectFolderUtility.CreateDefaultCcsFolderStructure();
            TryCopyCharacterControllerSampleFromPackage();
        }

        private static void TryCopyCharacterControllerSampleFromPackage()
        {
            UnityEditor.PackageManager.PackageInfo info =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.crazycarrot.charactercontroller");
            if (info == null)
            {
                CCSEditorLog.Warning(
                    "CCS Hub: Character Controller is not resolved under Packages yet; Assets/CCS folders were created. Retry after Package Manager finishes.");
                return;
            }

            string assetDestRoot = "Assets/CCS/CharacterController/BasicSetup";
            if (AssetDatabase.IsValidFolder(assetDestRoot))
            {
                string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { assetDestRoot });
                if (guids != null && guids.Length > 0)
                {
                    CCSEditorLog.Info($"CCS Hub: {assetDestRoot} already has content; skipping sample copy.");
                    return;
                }
            }

            string sourcePhysical = Path.Combine(info.resolvedPath, "Samples~", "BasicSetup");
            if (!Directory.Exists(sourcePhysical))
            {
                CCSEditorLog.Info(
                    "CCS Hub: No Samples~/BasicSetup folder in the Character Controller package; Assets/CCS structure is ready for your content.");
                return;
            }

            string destPhysical = Path.Combine(Application.dataPath, "CCS", "CharacterController", "BasicSetup");
            string parentPhysical = Path.GetDirectoryName(destPhysical);
            if (!string.IsNullOrEmpty(parentPhysical))
            {
                Directory.CreateDirectory(parentPhysical);
            }

            FileUtil.CopyFileOrDirectory(sourcePhysical, destPhysical);
            AssetDatabase.Refresh();
            CCSEditorLog.Info("CCS Hub: copied Character Controller sample to Assets/CCS/CharacterController/BasicSetup.");
        }
    }
}
