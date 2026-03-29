// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupProgressWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Single EditorWindow for required (first-run) and optional setup: manifest rows with statuses, batch progress bar, completion banners before close.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Shared setup progress UI: <see cref="SetupMode.RequiredSetup"/> (all manifest required rows) and
    /// <see cref="SetupMode.OptionalSetup"/> (user-selected optional batch only).
    /// </summary>
    public sealed class CCSSetupProgressWindow : EditorWindow
    {
        #region Variables

        private static CCSSetupProgressWindow instance;

        private readonly Color panelBackground = new Color(0.12f, 0.12f, 0.13f);
        private readonly Color rowHighlight = new Color(0.22f, 0.18f, 0.14f);
        private readonly Color accentOrange = new Color(1f, 0.48f, 0.12f);
        private static readonly Color AccentOrangeStatic = new Color(1f, 0.48f, 0.12f);

        [SerializeField]
        private Vector2 scrollPosition;

        private SetupMode viewMode = SetupMode.RequiredSetup;
        private bool subscribedToInstallEvents;
        private bool subscribedToEditorUpdate;
        private bool optionalSawActivity;
        private bool optionalCloseScheduled;
        private bool requiredCompletionUi;
        private bool optionalCompletionUi;
        private Action pendingAfterClose;
        private EditorApplication.CallbackFunction optionalFinishWaitTick;
        private double optionalFinishDeadline;

        #endregion

        #region Enums

        public enum SetupMode
        {
            RequiredSetup,
            OptionalSetup,
        }

        private enum SetupPhase
        {
            RequiredDependencies,
            OptionalInstalls,
        }

        #endregion

        #region Public Methods

        /// <summary>Opens when the automatic required install queue starts (or resumes after domain reload).</summary>
        public static void ShowRequiredPhase()
        {
            if (CCSSetupState.IsSetupCompleted() || CCSSetupState.IsSetupSkipped())
            {
                return;
            }

            CCSSetupProgressWindow window = GetWindow<CCSSetupProgressWindow>(true, "CCS Hub — Setup", true);
            window.viewMode = SetupMode.RequiredSetup;
            window.requiredCompletionUi = false;
            window.optionalCompletionUi = false;
            window.pendingAfterClose = null;
            window.optionalSawActivity = false;
            window.optionalCloseScheduled = false;
            instance = window;
            window.minSize = new Vector2(480f, 380f);
            window.maxSize = new Vector2(720f, 900f);
            window.Show();
            EditorApplication.delayCall += () =>
            {
                if (window != null)
                {
                    window.Focus();
                    window.Repaint();
                }
            };
        }

        /// <summary>
        /// After the required pass finishes: show completion text, then close, then run <paramref name="continuation"/>
        /// (typically <c>RequiredAutoInstallCompleted</c>). Safe if the window was never shown.
        /// </summary>
        public static void NotifyRequiredPassCompleteThenRun(Action continuation)
        {
            if (instance == null || instance.MapModeToPhase() != SetupPhase.RequiredDependencies)
            {
                continuation?.Invoke();
                return;
            }

            instance.requiredCompletionUi = true;
            instance.pendingAfterClose = continuation;
            instance.Show();
            instance.Focus();
            instance.Repaint();
            CCSSetupProgressWindow win = instance;
            // Two delayCall steps so "Required installs complete / Opening CCS Hub…" paints at least one frame before close + orchestrator.
            EditorApplication.delayCall += () =>
            {
                if (win == null)
                {
                    continuation?.Invoke();
                    return;
                }

                win.Repaint();
                EditorApplication.delayCall += () =>
                {
                    if (win != null)
                    {
                        win.RunRequiredCloseThenContinuation();
                    }
                    else
                    {
                        continuation?.Invoke();
                    }
                };
            };
        }

        /// <summary>Same window in optional mode after the user clicks Install selected in CCS Hub.</summary>
        public static void ShowOptionalPhase()
        {
            CCSSetupProgressWindow window = GetWindow<CCSSetupProgressWindow>(true, "CCS Hub — Setup", true);
            window.viewMode = SetupMode.OptionalSetup;
            window.requiredCompletionUi = false;
            window.optionalCompletionUi = false;
            window.pendingAfterClose = null;
            window.optionalSawActivity = false;
            window.optionalCloseScheduled = false;
            instance = window;
            window.minSize = new Vector2(480f, 360f);
            window.maxSize = new Vector2(720f, 900f);
            window.Show();
            window.Focus();
        }

        /// <summary>Closes the window if still open (e.g. safety before first-run Hub open).</summary>
        public static void CloseForFirstRunTransition()
        {
            if (instance == null)
            {
                return;
            }

            instance.Close();
            instance = null;
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            titleContent = new GUIContent("Setting up Crazy Carrot Studio…");
            SubscribeInstallEvents();
            SubscribeEditorUpdate();
        }

        private void OnDisable()
        {
            UnsubscribeInstallEvents();
            UnsubscribeEditorUpdate();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnGUI()
        {
            Rect full = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(full, panelBackground);

            GUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(14f);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawHeader();
                    EditorGUILayout.Space(8f);

                    if (CCSPackageInstallService.ShouldShowPostReloadInstallBanner())
                    {
                        EditorGUILayout.HelpBox(
                            "Resuming after reload: installs continue one at a time until the queue is empty.",
                            MessageType.Warning);
                        EditorGUILayout.Space(6f);
                    }

                    if (requiredCompletionUi && MapModeToPhase() == SetupPhase.RequiredDependencies)
                    {
                        DrawRequiredCompleteBanner();
                    }
                    else if (optionalCompletionUi && MapModeToPhase() == SetupPhase.OptionalInstalls)
                    {
                        DrawOptionalCompleteBanner();
                    }
                    else
                    {
                        DrawSubtitle();
                        EditorGUILayout.Space(6f);
                        DrawPhaseProgressBar();
                        EditorGUILayout.Space(8f);
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                        if (MapModeToPhase() == SetupPhase.RequiredDependencies)
                        {
                            DrawRequiredRows();
                            DrawRetrySection();
                        }
                        else
                        {
                            DrawOptionalRows();
                        }

                        EditorGUILayout.EndScrollView();
                        DrawOverallCountLine();
                    }
                }

                GUILayout.Space(14f);
            }
        }

        #endregion

        #region Private Methods

        private SetupPhase MapModeToPhase()
        {
            return viewMode == SetupMode.RequiredSetup
                ? SetupPhase.RequiredDependencies
                : SetupPhase.OptionalInstalls;
        }

        private void DrawHeader()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = accentOrange },
            };
            GUILayout.Label("Setting up Crazy Carrot Studio…", titleStyle);
        }

        private void DrawSubtitle()
        {
            string section = MapModeToPhase() == SetupPhase.RequiredDependencies ? "Required installs" : "Optional installs";
            EditorGUILayout.LabelField(section, EditorStyles.boldLabel);
        }

        private void DrawRequiredCompleteBanner()
        {
            GUIStyle accent = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = accentOrange } };
            GUILayout.Label("✔ Finished installing required items", accent);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("→ Opening CCS Hub…", EditorStyles.wordWrappedLabel);
        }

        private void DrawOptionalCompleteBanner()
        {
            GUIStyle accent = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = accentOrange } };
            GUILayout.Label("✔ Finished", accent);
        }

        private void DrawPhaseProgressBar()
        {
            if (MapModeToPhase() == SetupPhase.RequiredDependencies)
            {
                DrawRequiredOnlyProgressBar();
            }
            else
            {
                if (CCSHubInstallProgressBar.ShouldShow())
                {
                    CCSHubInstallProgressBar.Draw();
                }
                else if (ShouldShowOptionalPulseBar())
                {
                    Rect rect = EditorGUILayout.GetControlRect(false, 22f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                    EditorGUI.ProgressBar(rect, Mathf.Clamp01(pulse), "Working…");
                }
            }
        }

        private static void DrawRequiredOnlyProgressBar()
        {
            float normalizedPm = CCSPackageInstallService.GetInstallBatchProgressNormalized();
            string labelPm;
            if (normalizedPm < 0f)
            {
                normalizedPm = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                labelPm = "Resuming after reload — Package Manager…";
            }
            else if (CCSPackageInstallService.TryGetInstallBatchProgressCounts(out int processed, out int total) && total > 0)
            {
                labelPm = $"Package installs {processed} / {total}";
            }
            else if (CCSPackageInstallService.IsBusy())
            {
                labelPm = "Working…";
            }
            else
            {
                labelPm = "Done";
                normalizedPm = 1f;
            }

            Rect rectPm = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.ProgressBar(rectPm, Mathf.Clamp01(normalizedPm), labelPm);
        }

        private static bool ShouldShowOptionalPulseBar()
        {
            return CCSPackageInstallService.IsBusy()
                || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f;
        }

        private void DrawOverallCountLine()
        {
            if (MapModeToPhase() == SetupPhase.RequiredDependencies)
            {
                if (CCSPackageInstallService.TryGetInstallBatchProgressCounts(out int processed, out int total) && total > 0)
                {
                    EditorGUILayout.LabelField(
                        $"Overall: {processed} / {total} Package Manager steps.",
                        EditorStyles.miniLabel);
                }

                return;
            }

            if (CCSHubOptionalInstallContext.TryGetUserFacingStepCounts(out int done, out int userTotal) && userTotal > 0)
            {
                EditorGUILayout.LabelField($"Optional setup: {done} / {userTotal} steps.", EditorStyles.miniLabel);
            }
        }

        private void SubscribeInstallEvents()
        {
            if (subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = true;
            CCSPackageInstallService.StateChanged += OnPipelineStateChanged;
            CCSCharacterControllerAssetsBootstrap.StateChanged += OnPipelineStateChanged;
        }

        private void UnsubscribeInstallEvents()
        {
            if (!subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = false;
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
            if (MapModeToPhase() == SetupPhase.RequiredDependencies)
            {
                if (CCSPackageInstallService.IsBusy() || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f)
                {
                    Repaint();
                }

                return;
            }

            bool active = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy;
            if (active)
            {
                optionalSawActivity = true;
                optionalCloseScheduled = false;
                Repaint();
            }
            else if (optionalSawActivity && !optionalCloseScheduled && !optionalCompletionUi)
            {
                optionalCloseScheduled = true;
                EditorApplication.delayCall += BeginOptionalCompletionSequence;
            }
        }

        private void RunRequiredCloseThenContinuation()
        {
            if (this == null)
            {
                return;
            }

            Action cont = pendingAfterClose;
            pendingAfterClose = null;
            Close();
            cont?.Invoke();
        }

        private void BeginOptionalCompletionSequence()
        {
            if (this == null)
            {
                return;
            }

            if (CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                optionalCloseScheduled = false;
                return;
            }

            optionalCompletionUi = true;
            Show();
            Focus();
            Repaint();
            optionalFinishDeadline = EditorApplication.timeSinceStartup + 1.0;
            optionalFinishWaitTick = OptionalFinishWaitTick;
            EditorApplication.update += optionalFinishWaitTick;
        }

        private void OptionalFinishWaitTick()
        {
            if (this == null)
            {
                UnregisterOptionalFinishWait();
                return;
            }

            Repaint();
            if (EditorApplication.timeSinceStartup >= optionalFinishDeadline)
            {
                UnregisterOptionalFinishWait();
                FinishOptionalCompletionAndClose();
            }
        }

        private void UnregisterOptionalFinishWait()
        {
            if (optionalFinishWaitTick != null)
            {
                EditorApplication.update -= optionalFinishWaitTick;
                optionalFinishWaitTick = null;
            }
        }

        private void FinishOptionalCompletionAndClose()
        {
            if (this == null)
            {
                return;
            }

            CCSSetupState.SetSetupCompleted(true);
            CCSHubOptionalInstallContext.ClearOptionalUserTracking();
            Close();
            EditorApplication.delayCall += CCSSetupWindow.CloseAllInstances;
        }

        private void DrawRequiredRows()
        {
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                DrawDefinitionRow(definition);
            }
        }

        private void DrawOptionalRows()
        {
            foreach (CCSPackageDefinition definition in CCSHubOptionalInstallContext.EnumerateCurrentOptionalBatchDefinitions())
            {
                DrawDefinitionRow(definition);
            }

            bool dotweenWanted = SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected, false);
            if (dotweenWanted)
            {
                bool dotweenInstalled = CCSDotweenBundleInstaller.IsDemigiantDotweenPresentInProject();
                CCSPackageInstallStatus st = dotweenInstalled
                    ? CCSPackageInstallStatus.Installed
                    : (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                        && SessionState.GetBool(CCSSetupConstants.SessionStateDotweenCopyPending, false)
                        ? CCSPackageInstallStatus.Installing
                        : CCSPackageInstallStatus.Pending);
                DrawCompactRow(
                    "DOTween (Demigiant bundle)",
                    FormatStatusLabelWithGlyphAndPercentForDotween(st),
                    st,
                    highlight: st == CCSPackageInstallStatus.Installing);
            }
        }

        private void DrawDefinitionRow(CCSPackageDefinition definition)
        {
            CCSPackageInstallStatus status = ResolveStatus(definition);
            string label = FormatStatusLabelWithGlyphAndPercent(definition, status);
            bool highlight = status == CCSPackageInstallStatus.Installing;
            DrawCompactRow(definition.DisplayName, label, status, highlight);
        }

        private void DrawCompactRow(string title, string statusLabel, CCSPackageInstallStatus status, bool highlight)
        {
            Color bg = highlight ? rowHighlight : new Color(0.18f, 0.18f, 0.19f);
            Rect rowRect = EditorGUILayout.GetControlRect(false, 28f);
            EditorGUI.DrawRect(rowRect, bg);

            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white * 0.92f },
            };
            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = StatusColor(status) },
                fontStyle = FontStyle.Bold,
            };

            Rect inner = rowRect;
            inner.xMin += 8f;
            inner.xMax -= 8f;
            Rect left = inner;
            left.width = inner.width * 0.58f;
            Rect right = inner;
            right.xMin = left.xMax + 6f;

            GUI.Label(left, title, nameStyle);
            GUI.Label(right, statusLabel, statusStyle);
            EditorGUILayout.Space(3f);
        }

        /// <summary>Clear status column: [✔] Installed, [●] Installing, [ ] Pending, [✖] Failed; appends batch % when PM is installing this row.</summary>
        private static string FormatStatusLabelWithGlyph(CCSPackageInstallStatus status)
        {
            string word = FormatStatusLabel(status);
            string glyph;
            switch (status)
            {
                case CCSPackageInstallStatus.Installed:
                case CCSPackageInstallStatus.Skipped:
                    glyph = "[✔]";
                    break;
                case CCSPackageInstallStatus.Installing:
                    glyph = "[●]";
                    break;
                case CCSPackageInstallStatus.Failed:
                    glyph = "[✖]";
                    break;
                default:
                    glyph = "[ ]";
                    break;
            }

            return $"{glyph} {word}";
        }

        private static string FormatStatusLabelWithGlyphAndPercent(CCSPackageDefinition definition, CCSPackageInstallStatus status)
        {
            string line = FormatStatusLabelWithGlyph(status);
            if (status != CCSPackageInstallStatus.Installing)
            {
                return line;
            }

            if (CCSPackageInstallService.IsInstalling(definition.Id))
            {
                float n = CCSPackageInstallService.GetInstallBatchProgressNormalized();
                if (n >= 0f && n <= 1f)
                {
                    return $"{line} ({Mathf.RoundToInt(n * 100f)}%)";
                }
            }

            return line;
        }

        private static string FormatStatusLabelWithGlyphAndPercentForDotween(CCSPackageInstallStatus status)
        {
            string line = FormatStatusLabelWithGlyph(status);
            if (status != CCSPackageInstallStatus.Installing)
            {
                return line;
            }

            float n = CCSPackageInstallService.GetInstallBatchProgressNormalized();
            if (n >= 0f && n <= 1f)
            {
                return $"{line} ({Mathf.RoundToInt(n * 100f)}%)";
            }

            return line;
        }

        private static Color StatusColor(CCSPackageInstallStatus status)
        {
            switch (status)
            {
                case CCSPackageInstallStatus.Failed:
                    return new Color(1f, 0.45f, 0.45f);
                case CCSPackageInstallStatus.Installing:
                    return AccentOrangeStatic;
                case CCSPackageInstallStatus.Installed:
                    return new Color(0.55f, 0.9f, 0.55f);
                default:
                    return new Color(0.75f, 0.75f, 0.78f);
            }
        }

        private static CCSPackageInstallStatus ResolveStatus(CCSPackageDefinition definition)
        {
            if (CCSPackageInstallService.IsFailed(definition.Id))
            {
                return CCSPackageInstallStatus.Failed;
            }

            if (CCSPackageInstallService.IsSkipped(definition.Id))
            {
                return CCSPackageInstallStatus.Installed;
            }

            if (CCSPackageInstallService.IsInstalling(definition.Id))
            {
                return CCSPackageInstallStatus.Installing;
            }

            if (CCSPackageInstallService.IsPending(definition.Id))
            {
                return CCSPackageInstallStatus.Pending;
            }

            if (definition.Id == CCSSetupConstants.CharacterControllerDefinitionId)
            {
                if (CCSCharacterControllerAssetsBootstrap.IsFailed(definition.Id))
                {
                    return CCSPackageInstallStatus.Failed;
                }

                if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
                {
                    return CCSPackageInstallStatus.Installing;
                }

                if (CCSCharacterControllerAssetsBootstrap.IsCharacterControllerProjectImportComplete())
                {
                    return CCSPackageInstallStatus.Installed;
                }
            }

            if (CCSPackageStatusService.IsListReady() && CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
            {
                return CCSPackageInstallStatus.Installed;
            }

            if (!CCSPackageStatusService.IsListReady())
            {
                return CCSPackageInstallStatus.Pending;
            }

            return CCSPackageInstallStatus.Pending;
        }

        private static string FormatStatusLabel(CCSPackageInstallStatus status)
        {
            switch (status)
            {
                case CCSPackageInstallStatus.Pending:
                case CCSPackageInstallStatus.Unknown:
                case CCSPackageInstallStatus.NotInstalled:
                    return "Pending";
                case CCSPackageInstallStatus.Installing:
                    return "Installing";
                case CCSPackageInstallStatus.Installed:
                case CCSPackageInstallStatus.Skipped:
                    return "Installed";
                case CCSPackageInstallStatus.Failed:
                    return "Failed";
                default:
                    return status.ToString();
            }
        }

        private void DrawRetrySection()
        {
            StringBuilder failedNames = new StringBuilder();
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (!CCSPackageInstallService.IsFailed(definition.Id))
                {
                    continue;
                }

                if (failedNames.Length > 0)
                {
                    failedNames.Append(", ");
                }

                failedNames.Append(definition.DisplayName);
            }

            if (failedNames.Length == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                $"Failed: {failedNames}. Check the Console for Package Manager errors. You can retry one package at a time.",
                MessageType.Warning);

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                if (!CCSPackageInstallService.IsFailed(definition.Id))
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(definition.DisplayName, GUILayout.Width(220f));
                    if (GUILayout.Button("Retry", GUILayout.Width(80f)))
                    {
                        CCSPackageInstallService.RetryFailedDefinition(definition);
                    }
                }
            }
        }

        #endregion
    }
}
