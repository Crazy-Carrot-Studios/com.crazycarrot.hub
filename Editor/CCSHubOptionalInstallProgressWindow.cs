// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubOptionalInstallProgressWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 26, 2025
// Summary: Modal-style progress window after the user chooses Install selected in CCS Hub; shows until Package Manager and Character Controller bootstrap finish.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.Collections.Generic;
using System.Text;
using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Shown when optional CCS tools are queued from <see cref="CCSSetupWindow"/>; keeps the user informed while installs run.
    /// </summary>
    public sealed class CCSHubOptionalInstallProgressWindow : EditorWindow
    {
        private static CCSHubOptionalInstallProgressWindow instance;

        private bool subscribedToEvents;
        private bool subscribedToEditorUpdate;
        private bool sawInstallActivity;
        private bool closeScheduled;
        private string queuedItemsDisplayLine;

        /// <summary>
        /// Opens the progress window after optional packages are enqueued; <paramref name="queuedDefinitions"/> drives the “installing …” copy.
        /// </summary>
        public static void ShowAfterOptionalInstallEnqueue(IReadOnlyList<CCSPackageDefinition> queuedDefinitions)
        {
            CCSHubOptionalInstallProgressWindow window = GetWindow<CCSHubOptionalInstallProgressWindow>(true, "CCS Hub — Installing", true);
            window.minSize = new Vector2(420f, 180f);
            window.maxSize = new Vector2(560f, 320f);
            window.sawInstallActivity = false;
            window.closeScheduled = false;
            window.queuedItemsDisplayLine = FormatQueuedDisplayNames(queuedDefinitions);
            instance = window;
            window.Focus();
        }

        private static string FormatQueuedDisplayNames(IReadOnlyList<CCSPackageDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                return "the selected content";
            }

            if (definitions.Count == 1)
            {
                return definitions[0].DisplayName;
            }

            if (definitions.Count == 2)
            {
                return $"{definitions[0].DisplayName} and {definitions[1].DisplayName}";
            }

            var builder = new StringBuilder();
            for (int index = 0; index < definitions.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(index == definitions.Count - 1 ? ", and " : ", ");
                }

                builder.Append(definitions[index].DisplayName);
            }

            return builder.ToString();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("CCS Hub — Installing");
            SubscribeEvents();
            SubscribeEditorUpdate();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            UnsubscribeEditorUpdate();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void SubscribeEvents()
        {
            if (subscribedToEvents)
            {
                return;
            }

            subscribedToEvents = true;
            CCSPackageInstallService.StateChanged += OnPipelineStateChanged;
            CCSCharacterControllerAssetsBootstrap.StateChanged += OnPipelineStateChanged;
        }

        private void UnsubscribeEvents()
        {
            if (!subscribedToEvents)
            {
                return;
            }

            subscribedToEvents = false;
            CCSPackageInstallService.StateChanged -= OnPipelineStateChanged;
            CCSCharacterControllerAssetsBootstrap.StateChanged -= OnPipelineStateChanged;
        }

        private void SubscribeEditorUpdate()
        {
            if (subscribedToEditorUpdate)
            {
                return;
            }

            subscribedToEditorUpdate = true;
            EditorApplication.update += OnEditorUpdate;
        }

        private void UnsubscribeEditorUpdate()
        {
            if (!subscribedToEditorUpdate)
            {
                return;
            }

            subscribedToEditorUpdate = false;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPipelineStateChanged()
        {
            Repaint();
        }

        private void OnEditorUpdate()
        {
            bool active = CCSPackageInstallService.IsBusy()
                || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f;

            if (active)
            {
                sawInstallActivity = true;
                closeScheduled = false;
                Repaint();
            }
            else if (sawInstallActivity && !closeScheduled)
            {
                closeScheduled = true;
                EditorApplication.delayCall += CloseWhenIdle;
            }
        }

        private void CloseWhenIdle()
        {
            if (this == null)
            {
                return;
            }

            if (CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                closeScheduled = false;
                return;
            }

            Close();
        }

        private void OnGUI()
        {
            CCSHubBrandingUi.TryBeginBody();
            try
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Please wait", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Installing the selected content. Package Manager may take a while; Character Controller also imports into Assets/CCS when applicable.",
                    EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space(10f);
                if (CCSHubInstallProgressBar.ShouldShow())
                {
                    CCSHubInstallProgressBar.Draw();
                }
                else if (CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
                {
                    Rect rect = EditorGUILayout.GetControlRect(false, 22f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                    EditorGUI.ProgressBar(rect, Mathf.Clamp01(pulse), "Working…");
                }

                EditorGUILayout.Space(8f);
                DrawStatusLine();
            }
            finally
            {
                CCSHubBrandingUi.TryEndBody();
            }
        }

        private void DrawStatusLine()
        {
            if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                EditorGUILayout.HelpBox(
                    "Importing Character Controller into Assets/CCS/CharacterController…",
                    MessageType.None);
                return;
            }

            if (CCSPackageInstallService.IsBusy())
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(active)
                        ? "Package Manager is working…"
                        : $"Installing: {active}",
                    MessageType.None);
            }
            else if (!sawInstallActivity)
            {
                EditorGUILayout.HelpBox("Starting…", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Finishing…", MessageType.None);
            }
        }
    }
}
