// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageDefinition
// GameObject: N/A
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Data model for one row in the CCS Hub package registry (Unity registry, Git, or manual).
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Runtime/
// ============================================================================

namespace CCS.Hub
{
    public readonly struct CCSPackageDefinition
    {
        public CCSPackageDefinition(
            string id,
            string displayName,
            string packageId,
            CCSPackageSourceType sourceType,
            string installIdentifier,
            CCSPackageCategory category,
            bool isRequired,
            bool defaultSelected,
            bool autoInstallSupported,
            bool showInFirstRunWizard,
            bool showInPackageHub,
            string description,
            string installNotes)
        {
            Id = id;
            DisplayName = displayName;
            PackageId = packageId;
            SourceType = sourceType;
            InstallIdentifier = installIdentifier;
            Category = category;
            IsRequired = isRequired;
            DefaultSelected = defaultSelected;
            AutoInstallSupported = autoInstallSupported;
            ShowInFirstRunWizard = showInFirstRunWizard;
            ShowInPackageHub = showInPackageHub;
            Description = description;
            InstallNotes = installNotes;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string PackageId { get; }

        public CCSPackageSourceType SourceType { get; }

        public string InstallIdentifier { get; }

        public CCSPackageCategory Category { get; }

        public bool IsRequired { get; }

        public bool DefaultSelected { get; }

        public bool AutoInstallSupported { get; }

        public bool ShowInFirstRunWizard { get; }

        public bool ShowInPackageHub { get; }

        public string Description { get; }

        public string InstallNotes { get; }
    }
}
