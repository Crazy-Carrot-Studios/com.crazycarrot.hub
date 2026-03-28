// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupProgressWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Last Modified: March 27, 2026
// Summary: Persistent first-run progress UI for sequential UPM installs (required and optional phases): item name, tier label, status, stage text, and overall counts with retry on failure.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.Text;
using CCS.Hub;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Manifest-driven setup progress: required dependencies first, then optional installs after the user confirms selections in CCS Hub.
    /// </summary>
    public sealed class CCSSetupProgressWindow : EditorWindow
    {
        #region Variables

        private static CCSSetupProgressWindow instance;

        [SerializeField]
        private Vector2 scrollPosition;

        private SetupPhase viewPhase = SetupPhase.RequiredDependencies;
        private bool subscribedToInstallEvents;
        private bool subscribedToEditorUpdate;
        private bool optionalSawActivity;
        private bool optionalCloseScheduled;

        #endregion

        #region Enums

        private enum SetupPhase
        {
            RequiredDependencies,
            OptionalInstalls,
        }

        #endregion

        #region Public Methods

        /// <summary>Shows the progress window for the automatic required-dependency pass (first run).</summary>
        public static void ShowRequiredPhase()
        {
            CCSSetupProgressWindow window = GetWindow<CCSSetupProgressWindow>(true, "CCS Hub — Setup", true);
            window.viewPhase = SetupPhase.RequiredDependencies;
            window.minSize = new Vector2(520f, 400f);
            window.optionalSawActivity = false;
            window.optionalCloseScheduled = false;
            instance = window;
            window.Show();
            EditorApplication.delayCall += () =>
            {
                window.Focus();
                window.Repaint();
            };
        }

        /// <summary>Shows the same window for optional Package Manager / bootstrap work after the user clicks Install in CCS Hub.</summary>
        public static void ShowOptionalPhase()
        {
            CCSSetupProgressWindow window = GetWindow<CCSSetupProgressWindow>(true, "CCS Hub — Setup", true);
            window.viewPhase = SetupPhase.OptionalInstalls;
            window.minSize = new Vector2(520f, 360f);
            window.optionalSawActivity = false;
            window.optionalCloseScheduled = false;
            instance = window;
            window.Show();
            window.Focus();
        }

        /// <summary>Closes the progress window before opening the main CCS Hub optional UI (first-run transition).</summary>
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
            titleContent = new GUIContent("CCS Hub — Setup");
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
            CCSHubBrandingUi.TryBeginBody();
            try
            {
                DrawTitle();
                EditorGUILayout.Space(6f);
                DrawStageLine();
                EditorGUILayout.Space(6f);

                if (CCSHubInstallProgressBar.ShouldShow())
                {
                    CCSHubInstallProgressBar.Draw();
                }
                else if (ShouldShowPulseBar())
                {
                    Rect rect = EditorGUILayout.GetControlRect(false, 22f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 2.5f);
                    EditorGUI.ProgressBar(rect, Mathf.Clamp01(pulse), "Working…");
                }

                EditorGUILayout.Space(8f);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                if (viewPhase == SetupPhase.RequiredDependencies)
                {
                    DrawRequiredRows();
                }
                else
                {
                    DrawOptionalSummaryRows();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(8f);
                DrawOverallCount();
                DrawRetrySection();
            }
            finally
            {
                CCSHubBrandingUi.TryEndBody();
            }
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
            if (viewPhase != SetupPhase.OptionalInstalls)
            {
                return;
            }

            bool active = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy;
            if (active)
            {
                optionalSawActivity = true;
                optionalCloseScheduled = false;
                Repaint();
            }
            else if (optionalSawActivity && !optionalCloseScheduled)
            {
                optionalCloseScheduled = true;
                EditorApplication.delayCall += CloseOptionalWhenIdle;
            }
        }

        private void CloseOptionalWhenIdle()
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

            CCSHubOptionalInstallContext.ClearOptionalUserTracking();
            Close();
            EditorApplication.delayCall += CCSSetupWindow.CloseAllInstances;
        }

        private void DrawTitle()
        {
            if (CCSPackageInstallService.ShouldShowPostReloadInstallBanner())
            {
                EditorGUILayout.HelpBox(
                    "Resuming after reload: Package Manager installs continue one at a time until the queue is empty.",
                    MessageType.Warning);
            }

            string headline = viewPhase == SetupPhase.RequiredDependencies
                ? "Installing required dependencies"
                : "Installing selected optional packages";
            EditorGUILayout.LabelField(headline, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                viewPhase == SetupPhase.RequiredDependencies
                    ? "Packages are added one at a time via the Unity Package Manager. When this pass finishes and the editor is stable, the CCS Hub window opens for optional CCS tools."
                    : "Optional selections install sequentially. This window closes when Package Manager and asset bootstrap steps finish.",
                MessageType.Info);
        }

        private void DrawStageLine()
        {
            string stage = viewPhase == SetupPhase.RequiredDependencies
                ? BuildRequiredStageText()
                : BuildOptionalStageText();
            EditorGUILayout.HelpBox(stage, MessageType.None);
        }

        private static string BuildRequiredStageText()
        {
            if (CCSPackageInstallService.IsBusy())
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                return string.IsNullOrEmpty(active)
                    ? "Package Manager: installing next dependency…"
                    : $"Package Manager: installing {active}";
            }

            if (CCSSetupState.AreRequiredAutoDependenciesSatisfied())
            {
                return "Required dependencies satisfied. Preparing CCS Hub…";
            }

            return "Waiting for required dependency installs…";
        }

        private string BuildOptionalStageText()
        {
            string phase = CCSHubOptionalInstallContext.GetCurrentPhaseLabel();
            if (!string.IsNullOrEmpty(phase))
            {
                return phase;
            }

            if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                return "Importing Character Controller into Assets/CCS/CharacterController…";
            }

            if (CCSPackageInstallService.IsBusy())
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                return string.IsNullOrEmpty(active)
                    ? "Package Manager: working…"
                    : $"Package Manager: installing {active}";
            }

            if (!optionalSawActivity)
            {
                return "Starting optional installs…";
            }

            return "Finishing…";
        }

        private static bool ShouldShowPulseBar()
        {
            return CCSPackageInstallService.IsBusy()
                || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                || CCSPackageInstallService.GetInstallBatchProgressNormalized() < 0f;
        }

        private void DrawRequiredRows()
        {
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateAutoRequiredDefinitions())
            {
                DrawManifestRow(definition, tierLabel: "Required");
            }
        }

        private void DrawOptionalSummaryRows()
        {
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateOptionalToolsForHub())
            {
                DrawManifestRow(definition, tierLabel: "Optional");
            }

            EditorGUILayout.Space(4f);
            string dotweenStatus = CCSDotweenBundleInstaller.IsDemigiantDotweenPresentInProject()
                ? "Installed"
                : "Pending / copy when Character Controller + DOTween are selected";
            DrawPackageRow(
                "DOTween (Demigiant bundle copy)",
                "Optional",
                dotweenStatus,
                CCSPackageInstallStatus.Unknown);
        }

        private void DrawManifestRow(CCSPackageDefinition definition, string tierLabel)
        {
            CCSPackageInstallStatus status = ResolveStatus(definition);
            string statusLabel = FormatStatusLabel(status);
            DrawPackageRow(
                $"{definition.DisplayName} ({definition.PackageId})",
                tierLabel,
                statusLabel,
                status);
        }

        private static void DrawPackageRow(
            string title,
            string tierLabel,
            string statusLabel,
            CCSPackageInstallStatus status)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Tier: {tierLabel}", EditorStyles.miniLabel);
            MessageType lineType = status == CCSPackageInstallStatus.Failed ? MessageType.Error : MessageType.None;
            EditorGUILayout.HelpBox($"Status: {statusLabel}", lineType);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static CCSPackageInstallStatus ResolveStatus(CCSPackageDefinition definition)
        {
            if (CCSPackageInstallService.IsFailed(definition.Id))
            {
                return CCSPackageInstallStatus.Failed;
            }

            if (CCSPackageInstallService.IsSkipped(definition.Id))
            {
                return CCSPackageInstallStatus.Skipped;
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
                return CCSPackageInstallStatus.Unknown;
            }

            return CCSPackageInstallStatus.NotInstalled;
        }

        private static string FormatStatusLabel(CCSPackageInstallStatus status)
        {
            switch (status)
            {
                case CCSPackageInstallStatus.Pending:
                    return "Pending";
                case CCSPackageInstallStatus.Installing:
                    return "Installing";
                case CCSPackageInstallStatus.Installed:
                    return "Installed";
                case CCSPackageInstallStatus.Failed:
                    return "Failed";
                case CCSPackageInstallStatus.Skipped:
                    return "Skipped";
                case CCSPackageInstallStatus.Unknown:
                    return "Pending";
                default:
                    return status.ToString();
            }
        }

        private void DrawOverallCount()
        {
            if (viewPhase == SetupPhase.RequiredDependencies)
            {
                if (CCSPackageInstallService.TryGetInstallBatchProgressCounts(out int processed, out int total) && total > 0)
                {
                    EditorGUILayout.LabelField($"Overall: {processed} / {total} steps completed (Package Manager batch).", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Overall: batch progress resumes after domain reload if needed.", EditorStyles.miniLabel);
                }

                return;
            }

            if (CCSHubOptionalInstallContext.TryGetUserFacingStepCounts(out int done, out int userTotal) && userTotal > 0)
            {
                EditorGUILayout.LabelField($"Optional setup: {done} / {userTotal} steps.", EditorStyles.miniLabel);
            }
        }

        private void DrawRetrySection()
        {
            if (viewPhase != SetupPhase.RequiredDependencies)
            {
                return;
            }

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
