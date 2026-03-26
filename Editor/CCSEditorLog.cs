// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSEditorLog
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Centralized log prefixing for CCS Hub editor diagnostics.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEngine;

namespace CCS.Hub.Editor
{
    public static class CCSEditorLog
    {
        private const string Prefix = "[CCS Hub]";

        public static void Info(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }
    }
}
