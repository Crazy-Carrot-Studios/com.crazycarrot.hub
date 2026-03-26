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
            float normalized = CCSPackageInstallService.GetInstallBatchProgressNormalized();
            string label;
            if (normalized < 0f)
            {
                normalized = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                label = "Resuming after reload — Package Manager…";
            }
            else if (CCSPackageInstallService.TryGetInstallBatchProgressCounts(out int processed, out int total))
            {
                label = $"Package installs {processed} / {total}";
            }
            else if (CCSPackageInstallService.IsBusy())
            {
                label = "Working…";
            }
            else
            {
                label = "Done";
                normalized = 1f;
            }

            Rect rect = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.ProgressBar(rect, Mathf.Clamp01(normalized), label);
        }

        public static bool ShouldShow()
        {
            if (CCSPackageInstallService.IsBusy()
                || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f)
            {
                return true;
            }

            return CCSPackageInstallService.TryGetInstallBatchProgressCounts(out _, out int total) && total > 0;
        }
    }
}
