// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupConstants
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Shared menu paths, EditorPrefs keys, SessionState keys, and default Assets/CCS paths.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

namespace CCS.Hub.Editor
{
    public static class CCSSetupConstants
    {
        public const string MenuPathSetupWizard = "Tools/CCS/Setup Wizard";
        public const string MenuPathPackageHub = "Tools/CCS/Package Hub";
        public const string MenuPathResetSetupState = "Tools/CCS/Developer/Reset Setup State";

        public const string EditorPrefsKeyPrefix = "CCS.Hub.";
        public const string EditorPrefsSetupCompleted = "CCS.Hub.SetupCompleted";
        public const string EditorPrefsSetupSkipped = "CCS.Hub.SetupSkipped";

        public const string SessionStateAutoOpenedThisSession = "CCS.Hub.SetupAutoOpenedThisSession";
        public const string SessionStatePendingInstallQueueIds = "CCS.Hub.PendingInstallQueueIds";

        /// <summary>Registry row id for Universal RP in <see cref="CCS.Hub.CCSPackageRegistry"/>.</summary>
        public const string UnityUrpDefinitionId = "unity-urp";

        public static readonly string[] DefaultCcsProjectFolders =
        {
            "Assets/CCS",
            "Assets/CCS/Art",
            "Assets/CCS/Materials",
            "Assets/CCS/Prefabs",
            "Assets/CCS/Scenes",
            "Assets/CCS/Scripts",
            "Assets/CCS/ScriptableObjects",
            "Assets/CCS/Settings",
            "Assets/CCS/UI",
            "Assets/CCS/Animations",
        };
    }
}
