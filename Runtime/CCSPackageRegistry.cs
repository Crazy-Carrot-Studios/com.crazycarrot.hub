// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageRegistry
// GameObject: N/A
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Central data-driven registry of CCS Hub packages, Unity dependencies, and manual entries.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Runtime/
// ============================================================================

using System.Collections.Generic;

namespace CCS.Hub
{
    public static class CCSPackageRegistry
    {
        public static IReadOnlyList<CCSPackageDefinition> All { get; } = new[]
        {
            new CCSPackageDefinition(
                "ccs-branding",
                "CCS Branding",
                "com.crazycarrot.branding",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.branding.git",
                CCSPackageCategory.Required,
                isRequired: true,
                defaultSelected: true,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Shared Crazy Carrot Studios editor branding and UI helpers for CCS tools.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "unity-inputsystem",
                "Input System",
                "com.unity.inputsystem",
                CCSPackageSourceType.UnityRegistry,
                "com.unity.inputsystem",
                CCSPackageCategory.Required,
                isRequired: true,
                defaultSelected: true,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Unity New Input System. Target version in CCS stacks: 1.18.0 (project may resolve newer compatible).",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "unity-cinemachine",
                "Cinemachine",
                "com.unity.cinemachine",
                CCSPackageSourceType.UnityRegistry,
                "com.unity.cinemachine",
                CCSPackageCategory.Required,
                isRequired: true,
                defaultSelected: true,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Unity Cinemachine camera rigs. Target version in CCS stacks: 3.1.6.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "unity-urp",
                "Universal RP",
                "com.unity.render-pipelines.universal",
                CCSPackageSourceType.UnityRegistry,
                "com.unity.render-pipelines.universal",
                CCSPackageCategory.Recommended,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Universal Render Pipeline. Target version in CCS stacks: 17.3.0 line (Unity 6).",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "ccs-charactercreator",
                "CCS Character Creator",
                "com.crazycarrot.charactercreator",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.charactercreator.git",
                CCSPackageCategory.OptionalCCS,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Optional CCS character creation tooling when the repository is available.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "ccs-charactercontroller",
                "CCS Character Controller",
                "com.crazycarrot.charactercontroller",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.charactercontroller.git",
                CCSPackageCategory.OptionalCCS,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "CCS locomotion and character controller package.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "ccs-inventory",
                "CCS Inventory",
                "com.crazycarrot.inventory",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.inventory.git",
                CCSPackageCategory.OptionalCCS,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Optional CCS inventory systems when the repository is available.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "ccs-attributes",
                "CCS Attributes",
                "com.crazycarrot.attributes",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.attributes.git",
                CCSPackageCategory.OptionalCCS,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Optional CCS attributes framework when the repository is available.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "ccs-vitals",
                "CCS Vitals",
                "com.crazycarrot.vitals",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.vitals.git",
                CCSPackageCategory.OptionalCCS,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Optional CCS vitals and health-style systems when the repository is available.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "ccs-umatools",
                "CCS UMA Tools",
                "com.crazycarrot.umatools",
                CCSPackageSourceType.GitUrl,
                "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.umatools.git",
                CCSPackageCategory.OptionalCCS,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: true,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "Optional CCS UMA-related helpers when the repository is available.",
                installNotes: string.Empty),
            new CCSPackageDefinition(
                "dotween-manual",
                "DOTween",
                "dotween-manual",
                CCSPackageSourceType.Manual,
                "manual",
                CCSPackageCategory.ManualSpecial,
                isRequired: false,
                defaultSelected: false,
                autoInstallSupported: false,
                showInFirstRunWizard: true,
                showInPackageHub: true,
                description: "DOTween is not installed automatically. Official Demigiant distributions expect writable Assets/Plugin-style setup and are not reliable as a plain Git UPM install. Import DOTween from the Asset Store or your approved internal package, then continue.",
                installNotes: "Manual or custom CCS-supported workflow only. Do not assume Git URL UPM install."),
        };

        public static bool TryFindById(string definitionId, out CCSPackageDefinition definition)
        {
            for (int index = 0; index < All.Count; index++)
            {
                if (All[index].Id == definitionId)
                {
                    definition = All[index];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static IEnumerable<CCSPackageDefinition> EnumerateCategory(CCSPackageCategory category)
        {
            for (int index = 0; index < All.Count; index++)
            {
                CCSPackageDefinition definition = All[index];
                if (definition.Category == category)
                {
                    yield return definition;
                }
            }
        }
    }
}
