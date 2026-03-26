// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupState
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Persists first-run setup completion and skip flags; coordinates one auto-open per editor session.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    public static class CCSSetupState
    {
        #region Variables

        private static string ProjectEditorPrefsKey(string suffix)
        {
            return $"{CCSSetupConstants.EditorPrefsKeyPrefix}{suffix}_{Application.dataPath.GetHashCode():X8}";
        }

        #endregion

        #region Public Methods

        public static bool IsSetupCompleted()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey("SetupCompleted"), false);
        }

        public static bool IsSetupSkipped()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey("SetupSkipped"), false);
        }

        public static bool ShouldAutoOpenSetupWizard()
        {
            if (IsSetupCompleted() || IsSetupSkipped())
            {
                return false;
            }

            if (SessionState.GetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false))
            {
                return false;
            }

            return true;
        }

        public static void MarkAutoOpenedThisSession()
        {
            SessionState.SetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, true);
        }

        public static void SetSetupCompleted(bool value)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey("SetupCompleted"), value);
            CCSEditorLog.Info($"Setup completed flag set to {value} for this project.");
        }

        public static void SetSetupSkipped(bool value)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey("SetupSkipped"), value);
            CCSEditorLog.Info($"Setup skipped flag set to {value} for this project.");
        }

        public static void ResetAllSetupFlagsForDevelopment()
        {
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey("SetupCompleted"));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey("SetupSkipped"));
            SessionState.EraseBool(CCSSetupConstants.SessionStateAutoOpenedThisSession);
            SessionState.EraseString(CCSSetupConstants.SessionStatePendingInstallQueueIds);
            CCSEditorLog.Warning("CCS Hub setup flags and session install queue markers were cleared for development.");
        }

        #endregion
    }
}
