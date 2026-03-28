// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupMenuCommands
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: CCS menu entries under CCS/CCS Hub: open Hub, full first-run reset + same pipeline as editor load, force pipeline, dump state.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;

namespace CCS.Hub.Editor
{
    public static class CCSSetupMenuCommands
    {
        [MenuItem(CCSSetupConstants.MenuPathOpenHub, false, 10)]
        private static void OpenCcsHub()
        {
            CCSSetupWindow.OpenHubFromMenu();
        }

        [MenuItem(CCSSetupConstants.MenuPathResetFirstRunSetup, false, 20)]
        private static void ResetFirstRunSetupState()
        {
            CCSSetupState.ResetAllFirstRunStateForThisProject();
            CCSSetupState.LogFirstRunStateSnapshot("menu: after full reset (before RunFirstRunPipelineNow)");
            CCSSetupOrchestrator.EnsureInitialized();
            CCSSetupBootstrap.RunFirstRunPipelineNow();
            EditorUtility.DisplayDialog(
                "CCS Hub",
                "First-run state was reset for this project.\n\n"
                + "The same first-run pipeline as a fresh editor load is running now (package list refresh → required deps). "
                + "Check the Console for [CCS Hub] logs.",
                "OK");
        }

        [MenuItem(CCSSetupConstants.MenuPathForceFirstRunPipeline, false, 21)]
        private static void ForceRunFirstRunPipeline()
        {
            CCSEditorLog.Info("CCS Hub: Menu — Force run first-run pipeline (no reset).");
            CCSSetupOrchestrator.EnsureInitialized();
            CCSSetupBootstrap.RunFirstRunPipelineNow();
        }

        [MenuItem(CCSSetupConstants.MenuPathDumpSetupState, false, 22)]
        private static void DumpSetupStateToConsole()
        {
            CCSSetupState.LogFirstRunStateSnapshot("menu: Dump setup state");
        }
    }
}
