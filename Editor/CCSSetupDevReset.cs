// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupDevReset
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Summary: Internal/testing only — resets first-run state and reruns bootstrap. Not exposed under the public CCS menu.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Developer-only entry to clear Hub first-run state for the current project so the required → Hub → optional flow can be retested without manual EditorPrefs edits.
    /// </summary>
    internal static class CCSSetupDevReset
    {
        private const string MenuPath = "Tools/CCS Hub (Internal)/Reset first-run state for testing…";

        [MenuItem(MenuPath, false, 100)]
        private static void ResetFirstRunStateForTesting()
        {
            if (!EditorUtility.DisplayDialog(
                "CCS Hub (internal)",
                "Reset all first-run EditorPrefs and SessionState for this project, then rerun the bootstrap pipeline?\n\nFor testing only.",
                "Reset",
                "Cancel"))
            {
                return;
            }

            CCSSetupState.ResetAllFirstRunStateForThisProject();
            CCSEditorLog.Info("CCS Hub: Internal reset — first-run state cleared; rerunning pipeline.");
            EditorApplication.delayCall += () => CCSSetupBootstrap.RunFirstRunPipelineNow(true);
        }
    }
}
