using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace LF.Editor
{
    /// <summary>Desktop editor uses the OpenXR loader; the Meta XR Simulator (v207.1, already the active OpenXR runtime on this PC) then behaves like a Quest runtime
    /// (head pose, hand tracking, controllers). Android/Quest keeps the Oculus loader. Run: Unity -batchmode -executeMethod LF.Editor.LFSimSetup.ApplyAndExit</summary>
    public static class LFSimSetup
    {
        [MenuItem("Last Frequency/Setup OpenXR for Meta XR Simulator")]
        public static void Apply()
        {
            var group = BuildTargetGroup.Standalone; XRGeneralSettingsPerBuildTarget perTarget;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget) || perTarget == null) { Debug.LogError("[LF] no XR settings asset; run LFBuilder.BuildScene first"); return; }
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var general = perTarget.SettingsForBuildTarget(group); var manager = perTarget.ManagerSettingsForBuildTarget(group); general.AssignedSettings = manager; general.InitManagerOnStart = true;
            bool ok = XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group); Debug.Log("[LF] OpenXR loader (Standalone) assigned: " + ok);
            var oxr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (oxr != null)
            {
                var meta = oxr.GetFeature<Meta.XR.MetaXRFeature>(); if (meta != null) { meta.enabled = true; Debug.Log("[LF] MetaXRFeature enabled"); }
                var hands = oxr.GetFeature<HandTracking>(); if (hands != null) { hands.enabled = true; Debug.Log("[LF] HandTracking feature enabled"); }
                EditorUtility.SetDirty(oxr);
            }
            EditorUtility.SetDirty(perTarget); EditorUtility.SetDirty(manager); AssetDatabase.SaveAssets();
        }
        public static void ApplyAndExit() { Apply(); EditorApplication.Exit(0); }
    }
}
