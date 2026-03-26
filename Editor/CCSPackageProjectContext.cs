// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageProjectContext
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Detects whether Universal RP is already present so the hub does not blindly recommend the URP package.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CCS.Hub.Editor
{
    public static class CCSPackageProjectContext
    {
        #region Public Methods

        public static bool IsUrpEffectivelyPresent()
        {
            if (CCSPackageStatusService.IsListReady()
                && CCSPackageStatusService.IsPackageInstalled("com.unity.render-pipelines.universal"))
            {
                return true;
            }

            return IsUniversalRenderPipelineAssetActive();
        }

        public static string GetUrpContextHint()
        {
            if (IsUrpEffectivelyPresent())
            {
                return "URP detected (package installed and/or an active Universal render pipeline asset). You can leave this unchecked unless you still need to add the UPM package.";
            }

            return "URP is not detected yet. Enable this row if you want the Universal RP package added for a URP-based project.";
        }

        #endregion

        #region Private Methods

        private static bool IsUniversalRenderPipelineAssetActive()
        {
            RenderPipelineAsset asset = GraphicsSettings.defaultRenderPipeline;
            if (asset == null)
            {
                return false;
            }

            Type type = asset.GetType();
            string fullName = type.FullName ?? string.Empty;
            if (fullName.IndexOf("Universal", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
