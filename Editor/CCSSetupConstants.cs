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
        /// <summary>Top-level Unity menu: CCS (not under Tools).</summary>
        public const string MenuPathSetupWizard = "CCS/CCS Hub";

        public const string EditorPrefsKeyPrefix = "CCS.Hub.";
        public const string EditorPrefsSetupCompleted = "CCS.Hub.SetupCompleted";
        public const string EditorPrefsSetupSkipped = "CCS.Hub.SetupSkipped";

        public const string SessionStateAutoOpenedThisSession = "CCS.Hub.SetupAutoOpenedThisSession";
        public const string SessionStatePendingInstallQueueIds = "CCS.Hub.PendingInstallQueueIds";

        /// <summary>Session flag: pending queue is the hub's automatic required-dependency pass (survives domain reload).</summary>
        public const string SessionStateAutoRequiredPassActive = "CCS.Hub.AutoRequiredPassActive";

        /// <summary>Registry row id for Universal RP in <see cref="CCS.Hub.CCSPackageRegistry"/>.</summary>
        public const string UnityUrpDefinitionId = "unity-urp";

        /// <summary>Registry row id for CCS Character Controller in <see cref="CCS.Hub.CCSPackageRegistry"/>.</summary>
        public const string CharacterControllerDefinitionId = "ccs-charactercontroller";

        /// <summary>UPM package name for CCS Character Controller (resolved under Packages/ before Hub bootstraps into Assets).</summary>
        public const string CharacterControllerPackageId = "com.crazycarrot.charactercontroller";

        /// <summary>Unity asset path where Character Controller sources are bootstrapped for editing (not the transient Packages/ copy).</summary>
        public const string CharacterControllerAssetsRoot = "Assets/CCS/CharacterController";

        /// <summary>EditorPrefs: required Branding/Input/Cinemachine auto-install completed for this project.</summary>
        public const string EditorPrefsRequiredAutoDepsSatisfiedKey = "RequiredAutoDepsSatisfied";

        /// <summary>EditorPrefs: human-readable summary of required packages (auto or already present).</summary>
        public const string EditorPrefsRequiredAutoDepsSummaryKey = "RequiredAutoDepsSummary";

        /// <summary>Hub scaffold for Character Controller: only Assets/CCS and the controller root (no empty sibling template folders).</summary>
        public static readonly string[] DefaultCcsProjectFolders =
        {
            "Assets/CCS",
            "Assets/CCS/CharacterController",
        };
    }
}
