// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubOptionalInstallContext
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 27, 2026
// Summary: Session-backed counts for optional-install UI so progress matches user selections (e.g. Character Controller + DOTween = 2 steps), separate from Package Manager queue depth.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    /// <summary>
    /// Tracks what the user checked in <see cref="CCSSetupWindow"/> so the optional progress window can show
    /// "0 / 2" and phase text instead of only PM batch size (which omits DOTween and conflates required packages).
    /// </summary>
    internal static class CCSHubOptionalInstallContext
    {
        public static void BeginOptionalUserTracking(bool characterControllerChecked, bool dotweenChecked)
        {
            SessionState.SetBool(CCSSetupConstants.SessionStateOptionalUserCcSelected, characterControllerChecked);
            SessionState.SetBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected, dotweenChecked);
            int total = (characterControllerChecked ? 1 : 0) + (dotweenChecked ? 1 : 0);
            SessionState.SetInt(CCSSetupConstants.SessionStateOptionalUserStepTotal, total);
        }

        public static void ClearOptionalUserTracking()
        {
            SessionState.EraseInt(CCSSetupConstants.SessionStateOptionalUserStepTotal);
            SessionState.EraseBool(CCSSetupConstants.SessionStateOptionalUserCcSelected);
            SessionState.EraseBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected);
        }

        public static bool TryGetUserFacingStepCounts(out int completed, out int total)
        {
            total = SessionState.GetInt(CCSSetupConstants.SessionStateOptionalUserStepTotal, 0);
            completed = 0;
            if (total <= 0)
            {
                return false;
            }

            bool ccWanted = SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserCcSelected, false);
            bool dotweenWanted = SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected, false);

            if (ccWanted && dotweenWanted)
            {
                if (CCSPackageInstallService.IsBusy())
                {
                    completed = 0;
                }
                else if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                    || !CCSCharacterControllerAssetsBootstrap.IsCharacterControllerProjectImportComplete())
                {
                    completed = 1;
                }
                else
                {
                    completed = 2;
                }

                return true;
            }

            if (ccWanted)
            {
                completed = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                    ? 0
                    : 1;
                return true;
            }

            if (dotweenWanted)
            {
                completed = CCSPackageInstallService.IsBusy() || CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy
                    ? 0
                    : 1;
                return true;
            }

            return false;
        }

        /// <summary>Short label for under the progress bar (current phase).</summary>
        public static string GetCurrentPhaseLabel()
        {
            bool ccWanted = SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserCcSelected, false);
            bool dotweenWanted = SessionState.GetBool(CCSSetupConstants.SessionStateOptionalUserDotweenSelected, false);

            if (CCSPackageInstallService.IsBusy())
            {
                string active = CCSPackageInstallService.GetActiveInstallDisplayName();
                return string.IsNullOrEmpty(active)
                    ? "Package Manager…"
                    : $"Installing: {active}";
            }

            if (CCSCharacterControllerAssetsBootstrap.IsBootstrapBusy)
            {
                if (ccWanted && dotweenWanted && SessionState.GetBool(CCSSetupConstants.SessionStateDotweenCopyPending, false))
                {
                    return "Importing Character Controller into Assets (includes DOTween bundle step)…";
                }

                return "Importing Character Controller into Assets/CCS/CharacterController…";
            }

            return string.Empty;
        }
    }
}
