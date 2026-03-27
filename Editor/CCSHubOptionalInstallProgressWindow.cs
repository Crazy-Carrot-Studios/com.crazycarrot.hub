// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubOptionalInstallProgressWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 26, 2025
// Summary: Modal-style progress window after the user chooses Install selected in CCS Hub; centered title and subtitle until Package Manager and Character Controller bootstrap finish.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

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

        private GUIStyle pleaseWaitTitleStyle;
        private GUIStyle installingSubtitleStyle;

        /// <summary>
        /// Opens the progress window after optional packages are enqueued from CCS Hub.
        /// </summary>
        public static void ShowAfterOptionalInstallEnqueue()
        {
            CCSHubOptionalInstallProgressWindow window = GetWindow<CCSHubOptionalInstallProgressWindow>(true, "CCS Hub — Installing", true);
            window.minSize = new Vector2(420f, 160f);
            window.maxSize = new Vector2(560f, 300f);
            window.sawInstallActivity = false;
            window.closeScheduled = false;
            instance = window;
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("CCS Hub — Installing");
            pleaseWaitTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            installingSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
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
                EditorGUILayout.Space(12f);
                GUILayout.Label("Please Wait", pleaseWaitTitleStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.Space(6f);
                GUILayout.Label("Installing selected Content", installingSubtitleStyle, GUILayout.ExpandWidth(true));

                EditorGUILayout.Space(14f);
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
