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

        /// <summary>
        /// Project EditorPrefs flag: user finished optional setup. Defaults to <c>false</c> when the key is missing.
        /// </summary>
        public static bool IsSetupCompleted()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey("SetupCompleted"), false);
        }

        public static bool IsSetupSkipped()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey("SetupSkipped"), false);
        }

        public static bool AreRequiredAutoDependenciesSatisfied()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey), false);
        }

        public static string GetRequiredAutoDependenciesSummary()
        {
            return EditorPrefs.GetString(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey), string.Empty);
        }

        public static bool GetIncludeDotweenOptional()
        {
            return EditorPrefs.GetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsIncludeDotweenOptionalKey), false);
        }

        public static void SetIncludeDotweenOptional(bool value)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsIncludeDotweenOptionalKey), value);
        }

        public static void SetRequiredAutoDependenciesSatisfied(string summary)
        {
            EditorPrefs.SetBool(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey), true);
            EditorPrefs.SetString(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey), summary ?? string.Empty);
        }

        /// <summary>
        /// Clears the "required auto-deps satisfied" flag so the next bootstrap pass re-evaluates Package Manager
        /// (e.g. CCS Branding added to the required set after a prior successful pass).
        /// </summary>
        public static void ClearRequiredAutoDependenciesSatisfied()
        {
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey));
        }

        /// <summary>
        /// True when the Hub may auto-open on load: setup not completed/skipped, and we have not already auto-opened this editor session.
        /// </summary>
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
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSatisfiedKey));
            EditorPrefs.DeleteKey(ProjectEditorPrefsKey(CCSSetupConstants.EditorPrefsRequiredAutoDepsSummaryKey));
            SessionState.SetBool(CCSSetupConstants.SessionStateAutoOpenedThisSession, false);
            SessionState.SetString(CCSSetupConstants.SessionStatePendingInstallQueueIds, string.Empty);
            SessionState.SetBool(CCSSetupConstants.SessionStateAutoRequiredPassActive, false);
            CCSEditorLog.Warning("CCS Hub setup flags and session install queue markers were cleared for development.");
        }

        #endregion
    }
}
