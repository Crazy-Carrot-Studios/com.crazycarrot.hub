// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSSetupWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 27, 2026
// Summary: Minimal optional-package picker: toggles, short descriptions, Install Selected / Skip. No install progress UI (handled by CCSSetupProgressWindow).
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

        private readonly Dictionary<string, bool> optionalSelectionByDefinitionId = new Dictionary<string, bool>();
        private bool includeDotweenOptional;
        private string statusLine = "Ready.";
        private bool subscribedToInstallEvents;

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
            CCSSetupWindow window = AcquireHubWindowForReuse(out _);
            ApplyHubWindowLayoutAndFocus(window);
        }

        /// <summary>Orchestrated first-run path: reuse/focus existing Hub or create one; clears pending; calls <see cref="CCSSetupState.MarkAutoOpenedThisSession"/> only after <c>Show()</c>.</summary>
        public static void ShowOrFocusFirstRunAuto()
        {
            CCSSetupState.ClearPendingHubAutoOpenAfterRequiredPhase();
            openedFromFirstRunAuto = true;

            CCSSetupWindow window = AcquireHubWindowForReuse(out _);

            ApplyHubWindowLayoutAndFocus(window);

            CCSSetupState.MarkAutoOpenedThisSession();
            if (!SessionState.GetBool(CCSSetupConstants.SessionStateLoggedFirstRunAutoOpenInfoThisSession, false))
            {
                SessionState.SetBool(CCSSetupConstants.SessionStateLoggedFirstRunAutoOpenInfoThisSession, true);
                CCSEditorLog.Info("CCS Hub auto-open triggered (first-run).");
            }
        }

        private static CCSSetupWindow AcquireHubWindowForReuse(out bool createdNewInstance)
        {
            createdNewInstance = false;
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
                    candidate.Close();
                }
            }

            if (keep != null)
            {
                return keep;
            }

            createdNewInstance = true;
            return GetWindow<CCSSetupWindow>(true, "CCS Hub", true);
        }

        private static void ApplyHubWindowLayoutAndFocus(CCSSetupWindow window)
        {
            window.minSize = new Vector2(420f, 280f);
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
        }

        private void OnDisable()
        {
            UnsubscribeInstallEvents();
        }

        private void OnDestroy()
        {
            if (openedFromFirstRunAuto)
            {
                openedFromFirstRunAuto = false;
            }
        }

        private void OnGUI()
        {
            CCSHubBrandingUi.TryBeginBody();
            try
            {
                EditorGUILayout.Space(6f);
                CCSHubBrandingUi.TryDrawTitleBanner("CCS Hub");
                EditorGUILayout.LabelField("Optional packages", EditorStyles.boldLabel);
                EditorGUILayout.Space(8f);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawOptionalPackageRows();
                DrawDotweenOptionalRow();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space(10f);
                DrawMinimalToolbar();
                if (!string.IsNullOrEmpty(statusLine) && statusLine != "Ready.")
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.HelpBox(statusLine, MessageType.None);
                }
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

        private void DrawOptionalPackageRows()
        {
            foreach (CCSPackageDefinition definition in CCSPackageRegistry.EnumerateOptionalToolsForHub())
            {
                if (!definition.AutoInstallSupported || definition.SourceType == CCSPackageSourceType.Manual)
                {
                    continue;
                }

                EditorGUILayout.BeginVertical(GUI.skin.box);
                bool selected = optionalSelectionByDefinitionId.TryGetValue(definition.Id, out bool value) && value;
                bool newValue = EditorGUILayout.ToggleLeft(definition.DisplayName, selected);
                optionalSelectionByDefinitionId[definition.Id] = newValue;
                if (!string.IsNullOrWhiteSpace(definition.Description))
                {
                    EditorGUILayout.LabelField(definition.Description, EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6f);
            }
        }

        private void DrawDotweenOptionalRow()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.BeginChangeCheck();
            includeDotweenOptional = EditorGUILayout.ToggleLeft("DOTween (Demigiant)", includeDotweenOptional);
            if (EditorGUI.EndChangeCheck())
            {
                CCSSetupState.SetIncludeDotweenOptional(includeDotweenOptional);
            }

            EditorGUILayout.LabelField(
                "Optional bundle copy into Assets/Plugins and Resources (license terms apply).",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawMinimalToolbar()
        {
            bool busy = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy;
            using (new EditorGUI.DisabledScope(busy))
            {
                if (GUILayout.Button("Install Selected", GUILayout.Height(32f)))
                {
                    RunInstallSelectedOptional();
                }

                if (GUILayout.Button("Skip for now", GUILayout.Height(24f)))
                {
                    CCSSetupState.SetSetupSkipped(true);
                    statusLine = "Skipped. Reopen from CCS → CCS Hub anytime.";
                    Close();
                }
            }
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
                List<string> optionalBatchIds = packageManagerBatch.Select(d => d.Id).ToList();
                CCSHubOptionalInstallContext.BeginOptionalUserTracking(ccChecked, includeDotweenOptional, optionalBatchIds);
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
