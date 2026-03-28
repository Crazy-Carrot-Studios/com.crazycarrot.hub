// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupMenuItems
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: CCS menu entries for Hub setup (reset first-run state for testing or recovery).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;

namespace CCS.Hub.Editor
{
    public static class CCSSetupMenuItems
    {
        [MenuItem(CCSSetupConstants.MenuPathResetFirstRunSetup, false, 20)]
        private static void ResetFirstRunSetupState()
        {
            CCSSetupState.ResetAllSetupFlagsForDevelopment();
            EditorUtility.DisplayDialog(
                "CCS Hub",
                "First-run flags were cleared for this project.\n\n"
                + "Click OK to queue required installs again; the main CCS Hub will open when that pass finishes. "
                + "You do not need to restart Unity.",
                "OK");
            CCSSetupBootstrap.RunFirstRunPipelineNow();
        }
    }
}
