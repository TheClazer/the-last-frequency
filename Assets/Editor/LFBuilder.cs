using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LF.Editor
{
    /// <summary>Scene builder, headless autopilot and APK build.
    ///   Unity -batchmode -projectPath ...\LastFrequency -executeMethod LF.Editor.LFBuilder.BuildScene
    ///   Unity -batchmode ... -executeMethod LF.Editor.LFBuilder.Autopilot -lf-auto -lf-video      (frame-exact simulation video)
    ///   Unity -batchmode ... -executeMethod LF.Editor.LFBuilder.BuildNamed -apkOut E:/path/app.apk</summary>
    public static class LFBuilder
    {
        public const string ScenePath = "Assets/Scenes/LastFrequency.unity";

        [MenuItem("Last Frequency/Build Scene")]
        public static void BuildScene()
        {
            LFSetup.Apply();
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab); rig.name = "OVRCameraRig"; rig.transform.position = Vector3.zero;
            var mgr = rig.GetComponent<OVRManager>();
            mgr.isInsightPassthroughEnabled = false; mgr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel; mgr.usePositionTracking = true;
            var pt = rig.GetComponent<OVRPassthroughLayer>(); if (pt != null) Object.DestroyImmediate(pt);
            var eye = rig.transform.Find("TrackingSpace/CenterEyeAnchor"); var cam = eye.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.nearClipPlane = 0.05f; cam.farClipPlane = 300f; eye.gameObject.tag = "MainCamera";
            var uc = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); uc.renderPostProcessing = false; uc.antialiasing = AntialiasingMode.None;
            AddHand(rig, "TrackingSpace/LeftHandAnchor", HandL); AddHand(rig, "TrackingSpace/RightHandAnchor", HandR);
            var app = new GameObject("LFApp"); app.AddComponent<LFApp>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); Debug.Log("[LF] scene saved: " + ScenePath);
        }
        public static void BuildSceneAndExit() { BuildScene(); EditorApplication.Exit(0); }

        // OculusRuntimeSettings uses the OpenXR hand skeleton (handSkeletonVersion 1, the Meta SDK default); the hand prefabs must be the OpenXR-rigged ones to match.
        const string HandL = "OpenXRCustomHandPrefab_L", HandR = "OpenXRCustomHandPrefab_R";
        /// <summary>Replace legacy OVRCustomHandPrefab_L/R instances in the saved scene with the OpenXR prefabs (idempotent). Called before every autopilot and build.</summary>
        public static void EnsureOpenXRHands()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath); bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var hand in root.GetComponentsInChildren<OVRHand>(true))
                {
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(hand.gameObject); string nm = src != null ? src.name : hand.gameObject.name;
                    if (!nm.StartsWith("OVRCustomHandPrefab")) continue;
                    var anchor = hand.transform.parent; bool left = nm.EndsWith("_L");
                    if (PrefabUtility.GetNearestPrefabInstanceRoot(hand.gameObject) != hand.gameObject) { Debug.LogWarning("[LF] unexpected hand hierarchy, not replaced: " + hand.name); continue; }
                    Object.DestroyImmediate(hand.gameObject); AddHandUnder(anchor, left ? HandL : HandR); changed = true;
                }
            if (changed) { EditorSceneManager.SaveScene(scene); Debug.Log("[LF] scene hands switched to OpenXR hand prefabs"); }
            int n = 0; foreach (var root in scene.GetRootGameObjects()) foreach (var hand in root.GetComponentsInChildren<OVRHand>(true)) { var src = PrefabUtility.GetCorrespondingObjectFromSource(hand.gameObject); Debug.Log("[LF] scene hand " + (n++) + ": " + (src != null ? src.name : hand.gameObject.name)); }
        }
        static void AddHandUnder(Transform anchor, string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/" + prefabName + ".prefab");
            if (anchor == null || prefab == null) { Debug.LogWarning("[LF] hand prefab missing: " + prefabName); return; }
            PrefabUtility.InstantiatePrefab(prefab, anchor);
        }
        static void AddHand(GameObject rig, string anchorPath, string prefabName)
        {
            var anchor = rig.transform.Find(anchorPath); var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/" + prefabName + ".prefab");
            if (anchor == null || prefab == null) { Debug.LogWarning("[LF] hand prefab missing: " + prefabName); return; }
            PrefabUtility.InstantiatePrefab(prefab, anchor);
        }

        public static void QrTest()
        {
            var t = LF.LFShare.QrTexture("http://192.168.100.23:8080/"); System.IO.File.WriteAllBytes(@"E:\Winter\metavr\LastFrequency\logs\qr_test.png", t.EncodeToPNG()); Debug.Log("[LF] qr test written"); EditorApplication.Exit(0);
        }

        public static void Autopilot()
        {
            EnsureOpenXRHands(); EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Last Frequency/Build Quest APK")]
        public static void BuildApk() { BuildTo("E:/Winter/metavr/LastFrequency/Builds/LastFrequency.apk"); }
        public static void BuildNamed()
        {
            string o = "E:/Winter/metavr/LastFrequency/Builds/LastFrequency.apk"; var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-apkOut") o = a[i + 1];
            BuildTo(o);
        }
        static void BuildTo(string path)
        {
            LFSetup.Apply(); EnsureOpenXRHands(); Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None });
            Debug.Log("[LF] APK build: " + report.summary.result + ", " + report.summary.totalSize / (1024 * 1024) + " MB, errors " + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1); else if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
