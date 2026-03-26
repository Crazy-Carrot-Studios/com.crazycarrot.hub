// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSProjectFolderUtility
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Idempotently creates the standard Assets/CCS content folder structure for CCS projects.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;

namespace CCS.Hub.Editor
{
    public static class CCSProjectFolderUtility
    {
        #region Public Methods

        public static int CreateDefaultCcsFolderStructure()
        {
            int createdCount = 0;
            string[] folders = CCSSetupConstants.DefaultCcsProjectFolders;
            for (int index = 0; index < folders.Length; index++)
            {
                if (EnsureAssetFolder(folders[index]))
                {
                    createdCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CCSEditorLog.Info($"Assets/CCS folder pass complete. Created {createdCount} new folder node(s).");
            return createdCount;
        }

        #endregion

        #region Private Methods

        private static bool EnsureAssetFolder(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return false;
            }

            int lastSlash = assetPath.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                return false;
            }

            string parent = assetPath.Substring(0, lastSlash);
            string leaf = assetPath.Substring(lastSlash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolder(parent);
            }

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.CreateFolder(parent, leaf);
                return true;
            }

            return false;
        }

        #endregion
    }
}
