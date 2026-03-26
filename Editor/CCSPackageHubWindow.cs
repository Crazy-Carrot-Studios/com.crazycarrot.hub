// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageHubWindow
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Manual Tools menu entry for the CCS package hub; same behaviors as the setup wizard with hub-oriented defaults.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using UnityEditor;

namespace CCS.Hub.Editor
{
    public sealed class CCSPackageHubWindow : CCSSetupWindow
    {
        #region Variables

        protected override bool IsPackageHubMode => true;

        #endregion

        #region Unity Callbacks

        [MenuItem(CCSSetupConstants.MenuPathPackageHub, priority = 11)]
        public static void OpenPackageHubFromMenu()
        {
            CCSPackageHubWindow window = GetWindow<CCSPackageHubWindow>(true, "CCS Package Hub", true);
            window.minSize = new Vector2(560f, 620f);
            window.Show();
        }

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion
    }
}
