// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: First-run CCS Setup Wizard UI: required and optional packages, sequential installs, and Assets/CCS folder setup.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.Collections.Generic;
using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    public class CCSSetupWindow : EditorWindow
    {
        #region Variables

        [Header("Layout")]
        [Tooltip("Scroll position for the setup wizard content.")]
        [SerializeField]
        private Vector2 scrollPosition;

        [Header("Sections")]
        [Tooltip("Foldout for required packages.")]
        [SerializeField]
        private bool foldoutRequired = true;

        [Tooltip("Foldout for recommended packages.")]
        [SerializeField]
        private bool foldoutRecommended = true;

        [Tooltip("Foldout for optional CCS packages.")]
        [SerializeField]
        private bool foldoutOptional = true;

        [Tooltip("Foldout for manual or special entries.")]
        [SerializeField]
        private bool foldoutManual = true;

        [Tooltip("Foldout for last setup summary (success, failed, manual).")]
        [SerializeField]
        private bool foldoutCompletion = true;

        private readonly Dictionary<string, bool> optionalSelectionByDefinitionId = new Dictionary<string, bool>();
        private string statusLine = "Ready.";
        private bool subscribedToInstallEvents;

        protected virtual bool IsPackageHubMode => false;

        #endregion

        #region Unity Callbacks

        [MenuItem(CCSSetupConstants.MenuPathSetupWizard, priority = 10)]
        public static void OpenSetupWizardFromMenu()
        {
            CCSSetupWindow window = GetWindow<CCSSetupWindow>(true, "CCS Setup Wizard", true);
            window.minSize = new Vector2(560f, 620f);
            window.Show();
        }

        public static void ShowFirstRunAuto()
        {
            CCSSetupWindow window = GetWindow<CCSSetupWindow>(true, "CCS Setup Wizard", true);
            window.minSize = new Vector2(560f, 620f);
            window.Show();
        }

        protected virtual void OnEnable()
        {
            titleContent = new GUIContent(IsPackageHubMode ? "CCS Package Hub" : "CCS Setup Wizard");
            InitializeSelectionFromRegistry();
            CCSPackageStatusService.RefreshInstalledPackages(() => Repaint());
            SubscribeInstallEvents();
        }

        private void OnDisable()
        {
            UnsubscribeInstallEvents();
        }

        private void OnGUI()
        {
            CCSHubBrandingUi.TryBeginBody();
            try
            {
                DrawHeader();
                EditorGUILayout.Space(6f);
                DrawPostReloadBanner();
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawCategory(CCSPackageCategory.Required, ref foldoutRequired);
                DrawCategory(CCSPackageCategory.Recommended, ref foldoutRecommended);
                DrawCategory(CCSPackageCategory.OptionalCCS, ref foldoutOptional);
                DrawCategory(CCSPackageCategory.ManualSpecial, ref foldoutManual);
                EditorGUILayout.Space(8f);
                DrawCompletionSummary();
                EditorGUILayout.Space(8f);
                DrawRecommendedFolderCallout();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(6f);
                DrawToolbar();
                EditorGUILayout.Space(4f);
                DrawStatusBar();
            }
            finally
            {
                CCSHubBrandingUi.TryEndBody();
            }
        }

        #endregion

        #region Public Methods

        public void RefreshPackageStatusFromService()
        {
            CCSPackageStatusService.RefreshInstalledPackages(() =>
            {
                statusLine = "Package status refreshed.";
                Repaint();
            });
        }

        #endregion

        #region Private Methods

        private void SubscribeInstallEvents()
        {
            if (subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = true;
            CCSPackageInstallService.StateChanged += OnInstallPipelineStateChanged;
        }

        private void UnsubscribeInstallEvents()
        {
            if (!subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = false;
            CCSPackageInstallService.StateChanged -= OnInstallPipelineStateChanged;
        }

        private void OnInstallPipelineStateChanged()
        {
            Repaint();
        }

        private void InitializeSelectionFromRegistry()
        {
            optionalSelectionByDefinitionId.Clear();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.All)
            {
                if (!definition.IsRequired)
                {
                    optionalSelectionByDefinitionId[definition.Id] = definition.DefaultSelected;
                }
            }
        }

        private bool ShouldShowDefinition(CCSPackageDefinition definition)
        {
            return IsPackageHubMode ? definition.ShowInPackageHub : definition.ShowInFirstRunWizard;
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            string title = IsPackageHubMode ? "CCS Package Hub" : "CCS Setup Wizard";
            CCSHubBrandingUi.TryDrawTitleBanner(title);
            CCSHubBrandingUi.TryDrawSectionLabel("Crazy Carrot Studios");
            EditorGUILayout.HelpBox(
                "Install required Unity and CCS packages, optionally add more CCS tools, and scaffold Assets/CCS. Package content stays under Packages; this wizard never copies UPM files into Assets/CCS.",
                MessageType.Info);
        }

        private static void DrawPostReloadBanner()
        {
            if (!CCSPackageInstallService.ShouldShowPostReloadInstallBanner())
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Installing after reload: a pending Package Manager queue was restored. Client.Add runs one at a time until the queue is empty.",
                MessageType.Warning);
        }

        private void DrawCompletionSummary()
        {
            foldoutCompletion = EditorGUILayout.Foldout(foldoutCompletion, "Last setup summary", true);
            if (!foldoutCompletion)
            {
                return;
            }

            IReadOnlyList<string> successes = CCSPackageInstallService.LastSuccessfulInstallDisplayNames;
            if (successes.Count > 0)
            {
                CCSHubBrandingUi.TryDrawSectionLabel("Installed this batch");
                for (int index = 0; index < successes.Count; index++)
                {
                    EditorGUILayout.LabelField($"• {successes[index]}", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No automated installs recorded for the latest batch yet.", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4f);
            List<string> failed = CCSPackageInstallService.GetFailedInstallDisplayNames();
            CCSHubBrandingUi.TryDrawSectionLabel("Failed installs");
            if (failed.Count == 0)
            {
                EditorGUILayout.LabelField("None recorded.", EditorStyles.miniLabel);
            }
            else
            {
                for (int index = 0; index < failed.Count; index++)
                {
                    EditorGUILayout.LabelField($"• {failed[index]}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(4f);
            CCSHubBrandingUi.TryDrawSectionLabel("Manual / special remaining");
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateCategory(CCSPackageCategory.ManualSpecial))
            {
                if (definition.SourceType == CCSPackageSourceType.Manual && !definition.AutoInstallSupported)
                {
                    EditorGUILayout.LabelField($"• {definition.DisplayName}", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCategory(CCSPackageCategory category, ref bool foldout)
        {
            string heading = GetCategoryHeading(category);
            foldout = EditorGUILayout.Foldout(foldout, heading, true);
            if (!foldout)
            {
                return;
            }

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateCategory(category))
            {
                if (!ShouldShowDefinition(definition))
                {
                    continue;
                }

                DrawPackageRow(definition);
            }
        }

        private static string GetCategoryHeading(CCSPackageCategory category)
        {
            switch (category)
            {
                case CCSPackageCategory.Required:
                    return "Required";
                case CCSPackageCategory.Recommended:
                    return "Recommended";
                case CCSPackageCategory.OptionalCCS:
                    return "Optional CCS Packages";
                case CCSPackageCategory.ManualSpecial:
                    return "Manual / Special Install";
                default:
                    return category.ToString();
            }
        }

        private void DrawPackageRow(CCSPackageDefinition definition)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(definition.Description, EditorStyles.wordWrappedMiniLabel);
            if (definition.Id == CCSSetupConstants.UnityUrpDefinitionId)
            {
                EditorGUILayout.HelpBox(CCSPackageProjectContext.GetUrpContextHint(), MessageType.None);
            }

            EditorGUILayout.LabelField($"Source: {FormatSource(definition.SourceType)}  |  Id: {definition.PackageId}", EditorStyles.miniLabel);
            CCSPackageInstallStatus rowStatus = GetRowStatus(definition);
            EditorGUILayout.LabelField($"Status: {FormatStatus(rowStatus)}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (definition.IsRequired)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ToggleLeft("Selected (required)", true);
                EditorGUI.EndDisabledGroup();
            }
            else if (definition.SourceType != CCSPackageSourceType.Manual || definition.AutoInstallSupported)
            {
                bool selected = optionalSelectionByDefinitionId.TryGetValue(definition.Id, out bool value) && value;
                bool newValue = EditorGUILayout.ToggleLeft("Include in batch install", selected);
                optionalSelectionByDefinitionId[definition.Id] = newValue;
            }
            else
            {
                EditorGUILayout.LabelField("Automatic UPM install not supported for this entry.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(definition.InstallNotes))
            {
                EditorGUILayout.HelpBox(definition.InstallNotes, MessageType.None);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static string FormatSource(CCSPackageSourceType sourceType)
        {
            switch (sourceType)
            {
                case CCSPackageSourceType.UnityRegistry:
                    return "Unity Registry";
                case CCSPackageSourceType.GitUrl:
                    return "Git URL";
                case CCSPackageSourceType.Manual:
                    return "Manual";
                default:
                    return sourceType.ToString();
            }
        }

        private static string FormatStatus(CCSPackageInstallStatus status)
        {
            switch (status)
            {
                case CCSPackageInstallStatus.Unknown:
                    return "Unknown";
                case CCSPackageInstallStatus.NotInstalled:
                    return "Not Installed";
                case CCSPackageInstallStatus.Installed:
                    return "Installed";
                case CCSPackageInstallStatus.Pending:
                    return "Pending";
                case CCSPackageInstallStatus.Installing:
                    return "Installing";
                case CCSPackageInstallStatus.Failed:
                    return "Failed";
                case CCSPackageInstallStatus.Manual:
                    return "Manual";
                default:
                    return status.ToString();
            }
        }

        private CCSPackageInstallStatus GetRowStatus(CCSPackageDefinition definition)
        {
            if (definition.SourceType == CCSPackageSourceType.Manual && !definition.AutoInstallSupported)
            {
                return CCSPackageInstallStatus.Manual;
            }

            if (definition.Id == CCSSetupConstants.UnityUrpDefinitionId && CCSPackageProjectContext.IsUrpEffectivelyPresent())
            {
                return CCSPackageInstallStatus.Installed;
            }

            if (CCSPackageInstallService.IsFailed(definition.Id))
            {
                return CCSPackageInstallStatus.Failed;
            }

            if (CCSPackageInstallService.IsInstalling(definition.Id))
            {
                return CCSPackageInstallStatus.Installing;
            }

            if (CCSPackageInstallService.IsPending(definition.Id))
            {
                return CCSPackageInstallStatus.Pending;
            }

            if (!CCSPackageStatusService.IsListReady())
            {
                return CCSPackageInstallStatus.Unknown;
            }

            if (CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
            {
                return CCSPackageInstallStatus.Installed;
            }

            return CCSPackageInstallStatus.NotInstalled;
        }

        private void DrawRecommendedFolderCallout()
        {
            EditorGUILayout.HelpBox(
                "Recommended: create a standard Assets/CCS content tree for your project (Art, Prefabs, Scenes, Scripts, etc.). Safe to run multiple times.",
                MessageType.None);
        }

        private void DrawToolbar()
        {
            bool busy = CCSPackageInstallService.IsBusy();
            using (new EditorGUI.DisabledScope(busy))
            {
                if (GUILayout.Button("Run Full CCS Setup", GUILayout.Height(32f)))
                {
                    RunFullCcsSetup();
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Install Selected", GUILayout.Height(28f)))
                {
                    RunInstallSelected(includeOptional: true);
                }

                if (GUILayout.Button("Install Required Only", GUILayout.Height(28f)))
                {
                    RunInstallSelected(includeOptional: false);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create CCS Project Folders", GUILayout.Height(26f)))
                {
                    int created = CCSProjectFolderUtility.CreateDefaultCcsFolderStructure();
                    statusLine = $"Created or verified {created} folder node(s) under Assets/CCS.";
                }

                if (GUILayout.Button("Refresh Package Status", GUILayout.Height(26f)))
                {
                    RefreshPackageStatusFromService();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Skip For Now", GUILayout.Height(26f)))
                {
                    CCSSetupState.SetSetupSkipped(true);
                    statusLine = "Setup skipped. You can reopen Tools / CCS / Setup Wizard at any time.";
                    Close();
                }

                if (GUILayout.Button("Mark Setup Complete", GUILayout.Height(26f)))
                {
                    CCSSetupState.SetSetupCompleted(true);
                    statusLine = "Setup marked complete for this project.";
                }

                if (GUILayout.Button("Clear Install Failure Flags", GUILayout.Height(26f)))
                {
                    CCSPackageInstallService.ClearFailedFlags();
                    statusLine = "Failure flags cleared.";
                }

                EditorGUILayout.EndHorizontal();
            }

            if (busy)
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(active)
                        ? "Package Manager operations are running. Only one Client.Add runs at a time."
                        : $"Installing: {active}",
                    MessageType.Warning);
            }
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.HelpBox(statusLine, MessageType.None);
        }

        private void RunInstallSelected(bool includeOptional)
        {
            List<CCSPackageDefinition> batch = new List<CCSPackageDefinition>();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.All)
            {
                if (!ShouldShowDefinition(definition))
                {
                    continue;
                }

                if (!definition.AutoInstallSupported || definition.SourceType == CCSPackageSourceType.Manual)
                {
                    continue;
                }

                if (definition.IsRequired)
                {
                    batch.Add(definition);
                    continue;
                }

                if (!includeOptional)
                {
                    continue;
                }

                if (optionalSelectionByDefinitionId.TryGetValue(definition.Id, out bool selected) && selected)
                {
                    batch.Add(definition);
                }
            }

            CCSPackageInstallService.EnqueueDefinitions(batch);
            statusLine = $"Queued {batch.Count} package install operation(s).";
        }

        private void RunFullCcsSetup()
        {
            RunInstallSelected(includeOptional: true);
            int created = CCSProjectFolderUtility.CreateDefaultCcsFolderStructure();
            statusLine =
                $"Full CCS setup: queued required and selected optional installs; created or verified {created} folder node(s) under Assets/CCS.";
        }

        #endregion
    }

    public static class CCSSetupDeveloperMenu
    {
        #region Public Methods

        [MenuItem(CCSSetupConstants.MenuPathResetSetupState, priority = 120)]
        public static void ResetSetupStateForDevelopment()
        {
            CCSSetupState.ResetAllSetupFlagsForDevelopment();
        }

        #endregion
    }
}
