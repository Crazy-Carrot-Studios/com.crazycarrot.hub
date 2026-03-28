// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: Single CCS Hub EditorWindow: manual and first-run auto-open reuse one instance (focus/bring forward). Optional installs and progress close behavior unchanged.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System.Collections.Generic;
using System.Linq;
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
        private bool includeDotweenOptional;
        private string statusLine = "Ready.";
        private bool subscribedToInstallEvents;
        private bool subscribedToEditorUpdate;

        /// <summary>
        /// True when the window was last shown via <see cref="ShowOrFocusFirstRunAuto"/> (orchestrated path after required deps).
        /// </summary>
        private static bool openedFromFirstRunAuto;

        #endregion

        #region Unity Callbacks

        /// <summary>Menu entry: opens or focuses the single Hub window. Does not set the first-run auto-open session flag.</summary>
        [MenuItem(CCSSetupConstants.MenuPathOpenHub, false, 0)]
        public static void OpenHubFromMenu()
        {
            ShowOrFocusFromMenu();
        }

        /// <summary>
        /// Closes every CCS Hub <see cref="CCSSetupWindow"/> instance (e.g. after optional installs finish while a stray window stayed open).
        /// </summary>
        public static void CloseAllInstances()
        {
            CCSSetupWindow[] windows = Resources.FindObjectsOfTypeAll<CCSSetupWindow>();
            for (int index = 0; index < windows.Length; index++)
            {
                if (windows[index] != null)
                {
                    windows[index].Close();
                }
            }
        }

        #endregion

        #region Window lifecycle (single instance)

        /// <summary>Returns a live Hub window if one exists, otherwise null (does not create).</summary>
        public static CCSSetupWindow GetExistingInstance()
        {
            CCSSetupWindow[] found = Resources.FindObjectsOfTypeAll<CCSSetupWindow>();
            for (int index = 0; index < found.Length; index++)
            {
                if (found[index] != null)
                {
                    return found[index];
                }
            }

            return null;
        }

        /// <summary>Manual menu path: reuse/focus existing Hub or create one. Does not set <see cref="CCSSetupState.MarkAutoOpenedThisSession"/>.</summary>
        public static void ShowOrFocusFromMenu()
        {
            openedFromFirstRunAuto = false;
            CCSSetupWindow window = AcquireHubWindowForReuse();
            ApplyHubWindowLayoutAndFocus(window);
        }

        /// <summary>Orchestrated first-run path: reuse/focus existing Hub or create one; clears pending auto-open flag here for safety.</summary>
        public static void ShowOrFocusFirstRunAuto()
        {
            Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}ShowOrFocusFirstRunAuto CALLED");
            CCSSetupState.ClearPendingHubAutoOpenAfterRequiredPhase();
            openedFromFirstRunAuto = true;
            CCSSetupWindow window = AcquireHubWindowForReuse();
            ApplyHubWindowLayoutAndFocus(window);
        }

        private static CCSSetupWindow AcquireHubWindowForReuse()
        {
            Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}Acquiring Hub window instance (reuse or create)");
            CCSSetupWindow[] found = Resources.FindObjectsOfTypeAll<CCSSetupWindow>();
            CCSSetupWindow keep = null;
            for (int index = 0; index < found.Length; index++)
            {
                CCSSetupWindow candidate = found[index];
                if (candidate == null)
                {
                    continue;
                }

                if (keep == null)
                {
                    keep = candidate;
                }
                else
                {
                    Debug.LogWarning($"{CCSSetupConstants.HubFlowDiagnosticPrefix}Closing duplicate Hub window instance.");
                    candidate.Close();
                }
            }

            if (keep != null)
            {
                return keep;
            }

            return GetWindow<CCSSetupWindow>(true, "CCS Hub", true);
        }

        private static void ApplyHubWindowLayoutAndFocus(CCSSetupWindow window)
        {
            window.minSize = new Vector2(460f, 420f);
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

        #endregion

        #region Unity EditorWindow

        private void OnEnable()
        {
            titleContent = new GUIContent("CCS Hub");
            includeDotweenOptional = CCSSetupState.GetIncludeDotweenOptional();
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

        private void OnDestroy()
        {
            if (openedFromFirstRunAuto)
            {
                openedFromFirstRunAuto = false;
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
                || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
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
                if (CCSHubInstallProgressBar.ShouldShow())
                {
                    EditorGUILayout.Space(6f);
                    CCSHubInstallProgressBar.Draw();
                }

                EditorGUILayout.Space(8f);
                DrawOptionalToolsSection();
                DrawDotweenOptionalSection();
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
            CCSCharacterControllerAssetsBootstrap.StateChanged += OnInstallPipelineStateChanged;
        }

        private void UnsubscribeInstallEvents()
        {
            if (!subscribedToInstallEvents)
            {
                return;
            }

            subscribedToInstallEvents = false;
            CCSPackageInstallService.StateChanged -= OnInstallPipelineStateChanged;
            CCSCharacterControllerAssetsBootstrap.StateChanged -= OnInstallPipelineStateChanged;
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
                "Required CCS dependencies come from the Hub manifest and install automatically on load. Choose optional Character Controller and/or DOTween below, then click Install. Progress uses the CCS Hub setup window; when your selections finish, windows close automatically. Reopen anytime from CCS → CCS Hub.",
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
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(active)
                        ? "Installing required CCS dependencies (Branding, Input System, Cinemachine)…"
                        : $"Installing required: {active}",
                    MessageType.None);
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
            CCSHubBrandingUi.TryDrawSectionLabel("Character Controller");
            EditorGUILayout.Space(4f);

            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateOptionalToolsForHub())
            {
                DrawOptionalToolRow(definition);
            }
        }

        private void DrawDotweenOptionalSection()
        {
            EditorGUILayout.Space(8f);
            CCSHubBrandingUi.TryDrawSectionLabel("DOTween (Demigiant)");
            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            includeDotweenOptional = EditorGUILayout.ToggleLeft(
                "Include Demigiant DOTween (copies into Assets/Plugins and Assets/Resources)",
                includeDotweenOptional);
            if (EditorGUI.EndChangeCheck())
            {
                CCSSetupState.SetIncludeDotweenOptional(includeDotweenOptional);
            }

            EditorGUILayout.HelpBox(
                "Shipped inside the Character Controller package as DemigiantDOTweenBundle~ (not imported by Unity; copied to Assets/Plugins when selected). You are responsible for complying with Demigiant / DOTween license terms.",
                MessageType.None);
        }

        private void DrawOptionalToolRow(CCSPackageDefinition definition)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);

            bool selected = optionalSelectionByDefinitionId.TryGetValue(definition.Id, out bool value) && value;
            bool newValue = EditorGUILayout.ToggleLeft("Include when installing", selected);
            optionalSelectionByDefinitionId[definition.Id] = newValue;

            CCSPackageInstallStatus rowStatus = GetOptionalRowStatus(definition);
            EditorGUILayout.LabelField($"Status: {FormatStatus(rowStatus)}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawToolbar()
        {
            bool busy = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy;
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
                    statusLine = "Skipped. Reopen from CCS → CCS Hub anytime.";
                    Close();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                EditorGUILayout.HelpBox(
                    "Importing Character Controller into Assets/CCS/CharacterController (copying from Packages/ or removing duplicate package entry)…",
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
                case CCSPackageInstallStatus.Skipped:
                    return "Skipped";
                default:
                    return status.ToString();
            }
        }

        private CCSPackageInstallStatus GetOptionalRowStatus(CCSPackageDefinition definition)
        {
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

                if (CCSPackageInstallService.IsFailed(definition.Id))
                {
                    return CCSPackageInstallStatus.Failed;
                }

                if (CCSPackageInstallService.IsSkipped(definition.Id))
                {
                    return CCSPackageInstallStatus.Skipped;
                }

                if (CCSPackageInstallService.IsInstalling(definition.Id) || CCSPackageInstallService.IsPending(definition.Id))
                {
                    return CCSPackageInstallService.IsPending(definition.Id)
                        ? CCSPackageInstallStatus.Pending
                        : CCSPackageInstallStatus.Installing;
                }

                if (!CCSPackageStatusService.IsListReady())
                {
                    return CCSPackageInstallStatus.Unknown;
                }

                if (CCSPackageStatusService.IsPackageInstalled(definition.PackageId))
                {
                    return CCSPackageInstallStatus.Installing;
                }

                return CCSPackageInstallStatus.NotInstalled;
            }

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
            SessionState.SetBool(CCSSetupConstants.SessionStateDotweenCopyPending, false);

            List<CCSPackageDefinition> packageManagerBatch = new List<CCSPackageDefinition>();
            int skippedAlreadyImported = 0;

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

                if (definition.Id == CCSSetupConstants.CharacterControllerDefinitionId
                    && CCSCharacterControllerAssetsBootstrap.IsCharacterControllerProjectImportComplete())
                {
                    skippedAlreadyImported++;
                    continue;
                }

                if (definition.Id == CCSSetupConstants.CharacterControllerDefinitionId)
                {
                    CCSCharacterControllerAssetsBootstrap.ClearBootstrapFailureState();
                }

                packageManagerBatch.Add(definition);
            }

            bool wantsDotween = includeDotweenOptional;
            bool batchContainsCc = packageManagerBatch.Any(d => d.Id == CCSSetupConstants.CharacterControllerDefinitionId);

            if (wantsDotween && batchContainsCc)
            {
                SessionState.SetBool(CCSSetupConstants.SessionStateDotweenCopyPending, true);
            }
            else if (wantsDotween && !batchContainsCc)
            {
                if (!CCSDotweenBundleInstaller.IsDemigiantDotweenPresentInProject())
                {
                    if (!CCSDotweenBundleInstaller.TryCopyDemigiantIntoProject(out string dotweenErr))
                    {
                        statusLine = dotweenErr;
                        return;
                    }
                }
            }

            if (packageManagerBatch.Count > 0)
            {
                bool ccChecked = optionalSelectionByDefinitionId.TryGetValue(
                    CCSSetupConstants.CharacterControllerDefinitionId,
                    out bool ccSel)
                    && ccSel;
                CCSHubOptionalInstallContext.BeginOptionalUserTracking(ccChecked, includeDotweenOptional);
                CCSPackageInstallService.EnqueueOptionalWithRequiredPrerequisites(packageManagerBatch);
                CCSSetupProgressWindow.ShowOptionalPhase();
                Close();
                return;
            }

            string doneMessage;
            if (skippedAlreadyImported > 0 && wantsDotween)
            {
                doneMessage =
                    "Character Controller was already in the project; DOTween (Demigiant) is ready. Setup complete.";
            }
            else if (skippedAlreadyImported > 0)
            {
                doneMessage =
                    "Character Controller is already imported under Assets/CCS/CharacterController. Setup complete.";
            }
            else if (wantsDotween)
            {
                doneMessage = "DOTween (Demigiant) is ready. Setup complete.";
            }
            else
            {
                doneMessage = "Setup complete.";
            }

            CCSSetupState.SetSetupCompleted(true);
            CCSEditorLog.Info($"CCS Hub: {doneMessage}");
            Close();
            EditorApplication.delayCall += CloseAllInstances;
        }

        #endregion
    }
}
