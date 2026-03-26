// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: Minimal CCS Hub window: required dependencies auto-installed by bootstrap; optional tools (Character Controller) and one install action.
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

        [SerializeField]
        private Vector2 scrollPosition;

        [SerializeField]
        private bool foldoutAutoInstalled = true;

        private readonly Dictionary<string, bool> optionalSelectionByDefinitionId = new Dictionary<string, bool>();
        private string statusLine = "Ready.";
        private bool subscribedToInstallEvents;
        private bool subscribedToEditorUpdate;

        #endregion

        #region Unity Callbacks

        [MenuItem(CCSSetupConstants.MenuPathSetupWizard, priority = 10)]
        public static void OpenSetupWizardFromMenu()
        {
            CCSSetupWindow window = GetWindow<CCSSetupWindow>(true, "CCS Hub", true);
            window.minSize = new Vector2(460f, 420f);
            window.Show();
            EditorApplication.delayCall += () =>
            {
                window.Focus();
                window.Repaint();
            };
        }

        public static void ShowFirstRunAuto()
        {
            CCSSetupWindow window = GetWindow<CCSSetupWindow>(true, "CCS Hub", true);
            window.minSize = new Vector2(460f, 420f);
            window.Show();
            EditorApplication.delayCall += () =>
            {
                window.Focus();
                window.Repaint();
            };
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("CCS Hub");
            InitializeOptionalSelection();
            CCSPackageStatusService.RefreshInstalledPackages(() => Repaint());
            SubscribeInstallEvents();
            SubscribeEditorUpdateRepaint();
        }

        private void OnDisable()
        {
            UnsubscribeInstallEvents();
            UnsubscribeEditorUpdateRepaint();
        }

        private void SubscribeEditorUpdateRepaint()
        {
            if (subscribedToEditorUpdate)
            {
                return;
            }

            subscribedToEditorUpdate = true;
            EditorApplication.update += OnEditorUpdateRepaint;
        }

        private void UnsubscribeEditorUpdateRepaint()
        {
            if (!subscribedToEditorUpdate)
            {
                return;
            }

            subscribedToEditorUpdate = false;
            EditorApplication.update -= OnEditorUpdateRepaint;
        }

        private void OnEditorUpdateRepaint()
        {
            if (CCSPackageInstallService.IsBusy()
                || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f
                || CCSCharacterControllerAssetsImportService.IsImportInProgress)
            {
                Repaint();
            }
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
                DrawRequiredAutoSummary();
                EditorGUILayout.Space(8f);
                DrawOptionalToolsSection();
                EditorGUILayout.Space(8f);
                DrawAssetsExplanation();
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
            CCSCharacterControllerAssetsImportService.StateChanged += OnInstallPipelineStateChanged;
        }

        private void UnsubscribeInstallEvents()
        {
            if (!subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = false;
            CCSPackageInstallService.StateChanged -= OnInstallPipelineStateChanged;
            CCSCharacterControllerAssetsImportService.StateChanged -= OnInstallPipelineStateChanged;
        }

        private void OnInstallPipelineStateChanged()
        {
            Repaint();
        }

        private void InitializeOptionalSelection()
        {
            optionalSelectionByDefinitionId.Clear();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateOptionalToolsForHub())
            {
                optionalSelectionByDefinitionId[definition.Id] = definition.DefaultSelected;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            CCSHubBrandingUi.TryDrawTitleBanner("CCS Hub");
            CCSHubBrandingUi.TryDrawSectionLabel("Crazy Carrot Studios");
            EditorGUILayout.HelpBox(
                "Required CCS dependencies (Branding, Input System, Cinemachine) are installed automatically in the background. You only choose optional CCS tools here.",
                MessageType.Info);
        }

        private static void DrawPostReloadBanner()
        {
            if (!CCSPackageInstallService.ShouldShowPostReloadInstallBanner())
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "Installing after reload: Package Manager installs resume in order until the queue is empty.",
                MessageType.Warning);
        }

        private void DrawRequiredAutoSummary()
        {
            string summary = CCSSetupState.GetRequiredAutoDependenciesSummary();
            bool satisfied = CCSSetupState.AreRequiredAutoDependenciesSatisfied();
            bool busy = CCSPackageInstallService.IsBusy();

            if (!satisfied && busy)
            {
                EditorGUILayout.HelpBox("Installing required CCS dependencies automatically…", MessageType.None);
                return;
            }

            if (!satisfied && !busy)
            {
                EditorGUILayout.HelpBox(
                    "Required dependencies will install automatically. If this message persists, use Package Manager or check the Console.",
                    MessageType.Warning);
            }

            foldoutAutoInstalled = EditorGUILayout.Foldout(foldoutAutoInstalled, "Installed automatically", true);
            if (!foldoutAutoInstalled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                EditorGUILayout.LabelField(
                    satisfied ? "Summary not available yet." : "Waiting for required dependency installs…",
                    EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField(summary, EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawOptionalToolsSection()
        {
            CCSHubBrandingUi.TryDrawSectionLabel("Available CCS tools");
            EditorGUILayout.LabelField(
                "Optional Git CCS packages install via Package Manager under Packages/. Character Controller is imported into Assets/CCS/CharacterController only (not as a package).",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateOptionalToolsForHub())
            {
                DrawOptionalToolRow(definition);
            }
        }

        private void DrawOptionalToolRow(CCSPackageDefinition definition)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(definition.Description, EditorStyles.wordWrappedMiniLabel);

            bool selected = optionalSelectionByDefinitionId.TryGetValue(definition.Id, out bool value) && value;
            bool newValue = EditorGUILayout.ToggleLeft("Include when installing", selected);
            optionalSelectionByDefinitionId[definition.Id] = newValue;

            CCSPackageInstallStatus rowStatus = GetOptionalRowStatus(definition);
            EditorGUILayout.LabelField($"Status: {FormatStatus(rowStatus)}", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(definition.InstallNotes))
            {
                EditorGUILayout.HelpBox(definition.InstallNotes, MessageType.None);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawAssetsExplanation()
        {
            EditorGUILayout.HelpBox(
                "Character Controller is downloaded from the public GitHub repository into Assets/CCS/CharacterController (editable project sources). Optional CCS tools that remain UPM-only resolve under Packages/.",
                MessageType.None);
        }

        private void DrawToolbar()
        {
            bool busy = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsImportService.IsImportInProgress;
            using (new EditorGUI.DisabledScope(busy))
            {
                if (GUILayout.Button("Install selected", GUILayout.Height(32f)))
                {
                    RunInstallSelectedOptional();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Skip for now", GUILayout.Height(24f)))
                {
                    CCSSetupState.SetSetupSkipped(true);
                    statusLine = "Skipped. Reopen from CCS / CCS Hub anytime.";
                    Close();
                }

                if (GUILayout.Button("Mark setup complete", GUILayout.Height(24f)))
                {
                    CCSSetupState.SetSetupCompleted(true);
                    statusLine = "Marked complete for this project.";
                }

                EditorGUILayout.EndHorizontal();
            }

            if (CCSHubInstallProgressBar.ShouldShow())
            {
                EditorGUILayout.Space(4f);
                CCSHubInstallProgressBar.Draw();
            }

            if (CCSCharacterControllerAssetsImportService.IsImportInProgress)
            {
                EditorGUILayout.HelpBox(
                    "Importing Character Controller into Assets/CCS/CharacterController…",
                    MessageType.Warning);
            }
            else if (CCSPackageInstallService.IsBusy())
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(active)
                        ? "Package Manager is busy."
                        : $"Installing: {active}",
                    MessageType.Warning);
            }
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.HelpBox(statusLine, MessageType.None);
        }

        private static string FormatStatus(CCSPackageInstallStatus status)
        {
            switch (status)
            {
                case CCSPackageInstallStatus.Unknown:
                    return "Unknown";
                case CCSPackageInstallStatus.NotInstalled:
                    return "Not installed";
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

        private CCSPackageInstallStatus GetOptionalRowStatus(CCSPackageDefinition definition)
        {
            if (definition.SourceType == CCSPackageSourceType.AssetsGitImport)
            {
                if (CCSCharacterControllerAssetsImportService.IsFailed(definition.Id))
                {
                    return CCSPackageInstallStatus.Failed;
                }

                if (CCSCharacterControllerAssetsImportService.IsImporting(definition.Id))
                {
                    return CCSPackageInstallStatus.Installing;
                }

                if (CCSCharacterControllerAssetsImportService.IsCharacterControllerImportedIntoAssets())
                {
                    return CCSPackageInstallStatus.Installed;
                }

                return CCSPackageInstallStatus.NotInstalled;
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

        private void RunInstallSelectedOptional()
        {
            List<CCSPackageDefinition> packageManagerBatch = new List<CCSPackageDefinition>();
            int assetsImportStarted = 0;

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateOptionalToolsForHub())
            {
                if (!definition.AutoInstallSupported || definition.SourceType == CCSPackageSourceType.Manual)
                {
                    continue;
                }

                if (!optionalSelectionByDefinitionId.TryGetValue(definition.Id, out bool selected) || !selected)
                {
                    continue;
                }

                if (definition.SourceType == CCSPackageSourceType.AssetsGitImport)
                {
                    CCSCharacterControllerAssetsImportService.StartImport(definition);
                    assetsImportStarted++;
                    continue;
                }

                packageManagerBatch.Add(definition);
            }

            if (packageManagerBatch.Count == 0 && assetsImportStarted == 0)
            {
                statusLine = "Select at least one optional tool, or mark setup complete.";
                return;
            }

            if (packageManagerBatch.Count > 0)
            {
                CCSPackageInstallService.EnqueueDefinitions(packageManagerBatch);
            }

            if (assetsImportStarted > 0 && packageManagerBatch.Count > 0)
            {
                statusLine =
                    $"Queued {packageManagerBatch.Count} Package Manager install(s) and started Character Controller import into Assets.";
            }
            else if (assetsImportStarted > 0)
            {
                statusLine = "Importing Character Controller into Assets/CCS/CharacterController…";
            }
            else
            {
                statusLine = $"Queued {packageManagerBatch.Count} optional package install(s).";
            }
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
