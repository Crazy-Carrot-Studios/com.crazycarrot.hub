// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSHubBrandingUi
// GameObject: N/A (Editor Utility)
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Optional CCS Branding (com.crazycarrot.branding) UI via reflection—no compile-time dependency on branding.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Editor/
// ============================================================================

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CCS.Hub.Editor
{
    public static class CCSHubBrandingUi
    {
        #region Variables

        private static bool? brandingResolved;
        private static MethodInfo drawSectionLabelMethod;
        private static MethodInfo beginBodyMethod;
        private static MethodInfo endBodyMethod;
        private static MethodInfo drawTitleBannerMethod;

        #endregion

        #region Public Methods

        public static bool IsBrandingAvailable
        {
            get
            {
                if (brandingResolved.HasValue)
                {
                    return brandingResolved.Value;
                }

                brandingResolved = TryResolveBranding();
                return brandingResolved.Value;
            }
        }

        public static void TryBeginBody()
        {
            if (IsBrandingAvailable && beginBodyMethod != null)
            {
                try
                {
                    beginBodyMethod.Invoke(null, null);
                    return;
                }
                catch (Exception exception)
                {
                    CCSEditorLog.Warning($"CCS Hub branding BeginBody failed: {exception.Message}");
                }
            }
        }

        public static void TryEndBody()
        {
            if (IsBrandingAvailable && endBodyMethod != null)
            {
                try
                {
                    endBodyMethod.Invoke(null, null);
                    return;
                }
                catch (Exception exception)
                {
                    CCSEditorLog.Warning($"CCS Hub branding EndBody failed: {exception.Message}");
                }
            }
        }

        public static void TryDrawTitleBanner(string titleText)
        {
            if (IsBrandingAvailable && drawTitleBannerMethod != null)
            {
                try
                {
                    drawTitleBannerMethod.Invoke(null, new object[] { titleText });
                    return;
                }
                catch (Exception exception)
                {
                    CCSEditorLog.Warning($"CCS Hub branding DrawTitleBanner failed: {exception.Message}");
                }
            }

            EditorGUILayout.LabelField(titleText, EditorStyles.boldLabel);
        }

        public static void TryDrawSectionLabel(string label)
        {
            if (IsBrandingAvailable && drawSectionLabelMethod != null)
            {
                try
                {
                    drawSectionLabelMethod.Invoke(null, new object[] { label });
                    return;
                }
                catch (Exception exception)
                {
                    CCSEditorLog.Warning($"CCS Hub branding DrawSectionLabel failed: {exception.Message}");
                }
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        #endregion

        #region Private Methods

        private static bool TryResolveBranding()
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int index = 0; index < assemblies.Length; index++)
                {
                    Assembly assembly = assemblies[index];
                    if (assembly.GetName().Name != "CCS.Branding.Editor")
                    {
                        continue;
                    }

                    Type stylesType = assembly.GetType("CCS.Editor.CustomInspectors.Branding.CCSEditorStyles");
                    if (stylesType == null)
                    {
                        continue;
                    }

                    drawSectionLabelMethod = stylesType.GetMethod(
                        "DrawSectionLabel",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string) },
                        null);

                    beginBodyMethod = stylesType.GetMethod(
                        "BeginBody",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        Type.EmptyTypes,
                        null);

                    endBodyMethod = stylesType.GetMethod(
                        "EndBody",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        Type.EmptyTypes,
                        null);

                    drawTitleBannerMethod = stylesType.GetMethod(
                        "DrawTitleBanner",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string) },
                        null);

                    return drawSectionLabelMethod != null
                        && beginBodyMethod != null
                        && endBodyMethod != null
                        && drawTitleBannerMethod != null;
                }
            }
            catch (Exception exception)
            {
                CCSEditorLog.Warning($"CCS Hub branding resolution failed: {exception.Message}");
            }

            return false;
        }

        #endregion
    }
}
