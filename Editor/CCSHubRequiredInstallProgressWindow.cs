// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubRequiredInstallProgressWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Summary: First-import utility window listing required CCS Hub dependencies while Package Manager installs them; closed when the hub main window opens.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    public sealed class CCSHubRequiredInstallProgressWindow : EditorWindow
    {
        private static CCSHubRequiredInstallProgressWindow instance;
        private bool subscribedToInstallEvents;
        private bool subscribedToEditorUpdate;

        public static void ShowForFirstRun()
        {
            CCSHubRequiredInstallProgressWindow window = GetWindow<CCSHubRequiredInstallProgressWindow>(true, "CCS Hub — Required packages", true);
            window.minSize = new Vector2(440f, 300f);
            window.maxSize = new Vector2(520f, 420f);
            instance = window;
        }

        public static void CloseForFirstRun()
        {
            if (instance == null)
            {
                return;
            }

            instance.Close();
            instance = null;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("CCS Hub — Required packages");
            SubscribeInstallEvents();
            SubscribeEditorUpdateRepaint();
        }

        private void OnDisable()
        {
            UnsubscribeInstallEvents();
            UnsubscribeEditorUpdateRepaint();
            if (instance == this)
            {
                instance = null;
            }
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

        private void SubscribeInstallEvents()
        {
            if (subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = true;
            CCSPackageInstallService.StateChanged += OnInstallStateChanged;
        }

        private void UnsubscribeInstallEvents()
        {
            if (!subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = false;
            CCSPackageInstallService.StateChanged -= OnInstallStateChanged;
        }

        private void OnInstallStateChanged()
        {
            Repaint();
        }

        private void OnGUI()
        {
            CCSHubBrandingUi.TryBeginBody();
            try
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Installing required dependencies", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "CCS Hub is adding the packages below via the Unity Package Manager. This window closes automatically when installation finishes, then the main CCS Hub window opens for optional tools.",
                    MessageType.Info);

                EditorGUILayout.Space(6f);
                CCSHubInstallProgressBar.Draw();

                EditorGUILayout.Space(8f);
                foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
                {
                    DrawDependencyRow(definition);
                }

                EditorGUILayout.Space(10f);
                DrawGlobalStatus();
            }
            finally
            {
                CCSHubBrandingUi.TryEndBody();
            }
        }

        private static void DrawDependencyRow(CCSPackageDefinition definition)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(definition.DisplayName, GUILayout.Width(180f));
            EditorGUILayout.LabelField(FormatRowStatus(definition), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static string FormatRowStatus(CCSPackageDefinition definition)
        {
            if (CCSPackageInstallService.IsFailed(definition.Id))
            {
                return "Failed — see Console";
            }

            if (CCSPackageInstallService.IsInstalling(definition.Id))
            {
                return "Installing…";
            }

            if (CCSPackageInstallService.IsPending(definition.Id))
            {
                return "Queued…";
            }

            if (CCSPackageStatusService.IsListReady() && CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
            {
                return "Installed";
            }

            if (!CCSPackageStatusService.IsListReady())
            {
                return "Waiting for package list…";
            }

            return "Pending…";
        }

        private void DrawGlobalStatus()
        {
            if (CCSPackageInstallService.IsBusy())
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(active)
                        ? "Package Manager is installing a dependency. Only one operation runs at a time."
                        : $"Active install: {active}",
                    MessageType.None);
            }
            else if (CCSSetupState.AreRequiredAutoDependenciesSatisfied())
            {
                EditorGUILayout.HelpBox("Required packages are ready. Opening CCS Hub…", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Preparing required packages…", MessageType.None);
            }
        }
    }
}
