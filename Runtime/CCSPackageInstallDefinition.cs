// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageInstallDefinition
// GameObject: N/A
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Serializable manifest row for CCSDependencyManifest.json; converts to CCSPackageDefinition for the install queue and UI.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Runtime/
// ============================================================================

using System;

namespace CCS.Hub
{
    /// <summary>
    /// One data-driven package row loaded from <see cref="CCSDependencyManifest"/> (JSON).
    /// Enum fields use underlying integer values matching <see cref="CCSPackageSourceType"/> and <see cref="CCSPackageCategory"/>.
    /// </summary>
    [Serializable]
    public sealed class CCSPackageInstallDefinition
    {
        /// <summary>Stable registry id (e.g. ccs-branding).</summary>
        public string id;

        /// <summary>Editor-facing name.</summary>
        public string displayName;

        /// <summary>Unity Package Manager package name (e.g. com.unity.inputsystem).</summary>
        public string packageId;

        /// <summary><see cref="CCSPackageSourceType"/> as int.</summary>
        public int sourceType;

        /// <summary>Client.Add target: registry id, or id@version, or Git URL.</summary>
        public string installIdentifier;

        /// <summary><see cref="CCSPackageCategory"/> as int.</summary>
        public int category;

        public bool isRequired;

        public bool defaultSelected;

        public bool autoInstallSupported;

        public bool showInFirstRunWizard;

        public bool showInPackageHub;

        public string description;

        public string installNotes;

        public bool showInOptionalToolsHub;

        /// <summary>
        /// Builds the immutable registry struct used by <see cref="CCSPackageInstallService"/> and Hub UI.
        /// </summary>
        public CCSPackageDefinition ToPackageDefinition()
        {
            return new CCSPackageDefinition(
                id ?? string.Empty,
                displayName ?? id ?? "Package",
                packageId ?? string.Empty,
                (CCSPackageSourceType)sourceType,
                installIdentifier ?? packageId ?? string.Empty,
                (CCSPackageCategory)category,
                isRequired,
                defaultSelected,
                autoInstallSupported,
                showInFirstRunWizard,
                showInPackageHub,
                description ?? string.Empty,
                installNotes ?? string.Empty,
                showInOptionalToolsHub);
        }
    }
}
