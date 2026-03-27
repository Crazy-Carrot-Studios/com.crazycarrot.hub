// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSDependencyManifest
// GameObject: N/A
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Loads Runtime/Resources/CCSDependencyManifest.json into CCSPackageInstallDefinition rows and builds CCSPackageDefinition list.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Runtime/
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCS.Hub
{
    /// <summary>
    /// Manifest-driven registry loader. JSON lives next to this assembly under Resources (see file name without extension for Resources.Load).
    /// </summary>
    public static class CCSDependencyManifest
    {
        private const string ResourcesAssetName = "CCSDependencyManifest";

        [Serializable]
        private sealed class ManifestRoot
        {
            public string manifestVersion;

            public CCSPackageInstallDefinition[] required;

            public CCSPackageInstallDefinition[] optional;

            public CCSPackageInstallDefinition[] catalog;
        }

        /// <summary>
        /// Attempts to load and merge required → optional → catalog into a flat registry list.
        /// </summary>
        public static bool TryLoad(out List<CCSPackageDefinition> definitions)
        {
            definitions = null;
            TextAsset textAsset = Resources.Load<TextAsset>(ResourcesAssetName);
            if (textAsset == null)
            {
                return false;
            }

            try
            {
                ManifestRoot root = JsonUtility.FromJson<ManifestRoot>(textAsset.text);
                if (root == null)
                {
                    return false;
                }

                definitions = new List<CCSPackageDefinition>();
                AppendRows(definitions, root.required);
                AppendRows(definitions, root.optional);
                AppendRows(definitions, root.catalog);
                return definitions.Count > 0;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CCS Hub] Failed to parse dependency manifest: {exception.Message}");
                definitions = null;
                return false;
            }
        }

        private static void AppendRows(List<CCSPackageDefinition> target, CCSPackageInstallDefinition[] rows)
        {
            if (rows == null || target == null)
            {
                return;
            }

            for (int index = 0; index < rows.Length; index++)
            {
                CCSPackageInstallDefinition row = rows[index];
                if (row == null || string.IsNullOrWhiteSpace(row.id))
                {
                    continue;
                }

                target.Add(row.ToPackageDefinition());
            }
        }
    }
}
