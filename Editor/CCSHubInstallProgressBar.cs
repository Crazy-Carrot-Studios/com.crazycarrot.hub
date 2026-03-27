// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubInstallProgressBar
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: Shared EditorGUI progress bar for CCSPackageInstallService batch state (determinate counts or indeterminate pulse after reload).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    internal static class CCSHubInstallProgressBar
    {
        public static void Draw()
        {
            bool working = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy;

            if (CCSHubOptionalInstallContext.TryGetUserFacingStepCounts(out int userDone, out int userTotal))
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                float mid = working ? Mathf.Clamp01(0.15f + 0.25f * pulse) : 0f;
                float normalized = Mathf.Clamp01((userDone + mid) / Mathf.Max(1, userTotal));
                string label = $"Optional setup {userDone} / {userTotal}";
                Rect rect = EditorGUILayout.GetControlRect(false, 22f);
                EditorGUI.ProgressBar(rect, normalized, label);
                return;
            }

            if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                Rect rectAssets = EditorGUILayout.GetControlRect(false, 22f);
                EditorGUI.ProgressBar(rectAssets, Mathf.Clamp01(pulse), "Importing Character Controller into Assets…");
                return;
            }

            float normalizedPm = CCSPackageInstallService.GetInstallBatchProgressNormalized();
            string labelPm;
            if (normalizedPm < 0f)
            {
                normalizedPm = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                labelPm = "Resuming after reload — Package Manager…";
            }
            else if (CCSPackageInstallService.TryGetInstallBatchProgressCounts(out int processed, out int total))
            {
                labelPm = $"Package installs {processed} / {total}";
            }
            else if (CCSPackageInstallService.IsBusy())
            {
                labelPm = "Working…";
            }
            else
            {
                labelPm = "Done";
                normalizedPm = 1f;
            }

            Rect rectPm = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.ProgressBar(rectPm, Mathf.Clamp01(normalizedPm), labelPm);
        }

        public static bool ShouldShow()
        {
            if (CCSHubOptionalInstallContext.TryGetUserFacingStepCounts(out _, out int userTotal) && userTotal > 0)
            {
                return true;
            }

            if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                return true;
            }

            if (CCSPackageInstallService.IsBusy()
                || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f)
            {
                return true;
            }

            return CCSPackageInstallService.TryGetInstallBatchProgressCounts(out _, out int total) && total > 0;
        }
    }
}
