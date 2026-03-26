// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSCharacterControllerProjectBootstrap
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: After Character Controller exists under Assets/CCS/CharacterController, materializes Samples~/BasicSetup into BasicSetup when needed (Unity ignores Samples~ in Assets).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.IO;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Character Controller is imported into Assets only (not UPM). This type runs a light pass on editor load to materialize sample content.
    /// </summary>
    [InitializeOnLoad]
    public static class CCSCharacterControllerProjectBootstrap
    {
        static CCSCharacterControllerProjectBootstrap()
        {
            EditorApplication.delayCall += OnEditorLoaded;
        }

        private static void OnEditorLoaded()
        {
            if (!CCSCharacterControllerAssetsImportService.IsCharacterControllerImportedIntoAssets())
            {
                return;
            }

            CCSCharacterControllerAssetsImportService.TryStripPackageManifestFromCharacterControllerAssets();
            TryMaterializeSamplesBasicSetupIfNeeded();
        }

        /// <summary>
        /// Delegates to <see cref="CCSCharacterControllerAssetsImportService.TryMaterializeSamplesBasicSetupIfNeeded"/> for shared logic.
        /// </summary>
        public static void TryMaterializeSamplesBasicSetupIfNeeded()
        {
            CCSCharacterControllerAssetsImportService.TryMaterializeSamplesBasicSetupIfNeeded();
        }
    }
}
