using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

// Moves the Quest (Android) build from the deprecated Oculus XR provider to OpenXR, which is what
// Meta XR SDK v207 and Unity 6 recommend, and turns on the Meta features this game needs.
namespace RoomIsLying.Editor
{
    public static class AndroidOpenXRSetup
    {
        [MenuItem("Room Is Lying/Migrate Android to OpenXR")]
        public static void Apply()
        {
            var group = BuildTargetGroup.Android;
            XRGeneralSettingsPerBuildTarget perTarget;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget) || perTarget == null)
            {
                Debug.LogError("[RoomIsLying] XR settings not found; run SceneBuilder.BuildAll first.");
                return;
            }
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var general = perTarget.SettingsForBuildTarget(group);
            var manager = perTarget.ManagerSettingsForBuildTarget(group);
            general.AssignedSettings = manager;
            general.InitManagerOnStart = true;

            bool removed = XRPackageMetadataStore.RemoveLoader(manager, "Unity.XR.Oculus.OculusLoader", group);
            bool added = XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
            Debug.Log($"[RoomIsLying] Android XR: Oculus loader removed={removed}, OpenXR loader assigned={added}");

            EnableFeatures(group);
            EditorUtility.SetDirty(perTarget);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Enable the OpenXR features the game relies on for the given platform.</summary>
        public static void EnableFeatures(BuildTargetGroup group)
        {
            var oxr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (oxr == null) { Debug.LogError("[RoomIsLying] No OpenXR settings for " + group); return; }

            // Single-pass instanced rendering is the Quest default; 16-bit depth submission is cheaper.
            oxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            oxr.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;

            Enable<Meta.XR.MetaXRFeature>(oxr, "Meta XR Feature (passthrough, anchors, scene)");
            Enable<HandTracking>(oxr, "Hand Tracking Subsystem");
            Enable<MetaHandTrackingAim>(oxr, "Meta Hand Tracking Aim");
            Enable<HandInteractionProfile>(oxr, "Hand Interaction Profile");
            Enable<OculusTouchControllerProfile>(oxr, "Oculus Touch Controller Profile (required by OVRInput)");
            Enable<Meta.XR.MetaXRSubsampledLayout>(oxr, "Subsampled Layout (GPU performance)");

            EditorUtility.SetDirty(oxr);
            FeatureHelpers.RefreshFeatures(group);
        }

        static void Enable<T>(OpenXRSettings oxr, string label) where T : OpenXRFeature
        {
            var f = oxr.GetFeature<T>();
            if (f == null) { Debug.LogWarning($"[RoomIsLying] OpenXR feature not found: {label}"); return; }
            f.enabled = true;
            EditorUtility.SetDirty(f);
            Debug.Log($"[RoomIsLying] OpenXR feature enabled ({oxr.name}): {label}");
        }
    }
}
