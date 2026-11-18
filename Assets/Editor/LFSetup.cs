using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace LF.Editor
{
    /// <summary>One-shot Quest 3 project configuration: Unity -batchmode -executeMethod LF.Editor.LFSetup.Apply</summary>
    public static class LFSetup
    {
        public const int VersionCode = 3;   // bump by one for every Dashboard upload
        [MenuItem("Last Frequency/Apply Quest Setup")]
        public static void Apply()
        {
            PlayerSettings.companyName = "LastFrequency";
            PlayerSettings.productName = "The Last Frequency";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lastfrequency.app");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.forceInternetPermission = true;   // QR share upload
            // every upload needs a higher code. (Until 2026-11-17 this line reset the code to 1 on every build, so the Nov 16 APK was versionCode 1, not 2.)
            PlayerSettings.bundleVersion = "1.0.3"; PlayerSettings.Android.bundleVersionCode = VersionCode;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/icon_512.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, UnityEditor.IconKind.Any);
            var cfg = OVRProjectConfig.CachedProjectConfig;
            cfg.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;   // hands-only: with ControllersAndHands an awake controller takes over and the hands are posed from it (seen on a Quest 3S, 2026-11-18)
            cfg.handTrackingFrequency = OVRProjectConfig.HandTrackingFrequency.HIGH;
            cfg.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;   // optional, never required
            cfg.sceneSupport = OVRProjectConfig.FeatureSupport.None;
            cfg.anchorSupport = OVRProjectConfig.AnchorSupport.Disabled;   // no spatial anchors in this app: drops com.oculus.permission.USE_ANCHOR_API
            cfg.targetDeviceTypes = new System.Collections.Generic.List<OVRProjectConfig.DeviceType>
            { OVRProjectConfig.DeviceType.Quest3, OVRProjectConfig.DeviceType.Quest3S };
            OVRProjectConfig.CommitProjectConfig(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log("[LF] Quest project setup applied.");
        }

        public static void ApplyAndExit() { Apply(); EditorApplication.Exit(0); }
    }
}
