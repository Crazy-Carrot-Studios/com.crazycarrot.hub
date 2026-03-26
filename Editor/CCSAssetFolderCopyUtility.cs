// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSAssetFolderCopyUtility
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: File-only folder copy (no empty directories). Used when bootstrapping package content into Assets.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.IO;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Copies files from a source tree into a destination tree. Empty directories are not created.
    /// </summary>
    public static class CCSAssetFolderCopyUtility
    {
        /// <param name="skipUpmPackageManifest">When true, skips repository-root package.json and package.json.meta.</param>
        public static void CopyFilesOnlySkipEmptyDirectories(string sourceRoot, string destinationRoot, bool skipUpmPackageManifest)
        {
            sourceRoot = Path.GetFullPath(sourceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            destinationRoot = Path.GetFullPath(destinationRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (!Directory.Exists(sourceRoot))
            {
                return;
            }

            foreach (string filePath in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = filePath.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (skipUpmPackageManifest && ShouldSkipUpmPackageManifestFile(relative))
                {
                    continue;
                }

                string destPath = Path.Combine(destinationRoot, relative);
                string destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(filePath, destPath, true);
            }
        }

        /// <summary>Skips repository-root UPM manifest so copied content under Assets is not treated as an installable package.</summary>
        private static bool ShouldSkipUpmPackageManifestFile(string relativePath)
        {
            string norm = relativePath.Replace('\\', '/');
            return norm == "package.json" || norm == "package.json.meta";
        }
    }
}
