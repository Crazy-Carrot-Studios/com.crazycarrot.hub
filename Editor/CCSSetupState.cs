// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupState
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: Required-deps summary and optional prefs; coordinates one main-Hub auto-open per editor session (SessionState only — no persistent "setup finished" gate).
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
        /// True when the main Hub may auto-open after the required pass: not yet opened (or not marked) this editor session.
        /// </summary>
        public static bool ShouldAutoOpenSetupWizard()
        {
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

        #endregion
    }
}
