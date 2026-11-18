using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace LF
{
    /// <summary>-lf-probe: logs what the XR runtime (Meta XR Simulator or a real Quest) actually provides, saves screenshots, exits. Honest check that the app runs on an OpenXR runtime.</summary>
    public class LFProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return new WaitForSeconds(8f);
            var app = LFApp.I; var disp = new System.Collections.Generic.List<XRDisplaySubsystem>(); SubsystemManager.GetSubsystems(disp);
            Debug.Log("[PROBE] device=" + XRSettings.loadedDeviceName + " active=" + XRSettings.isDeviceActive + " displaySubsystems=" + disp.Count + " running=" + (disp.Count > 0 && disp[0].running));
            Debug.Log("[PROBE] OVRManager present=" + (FindFirstObjectByType<OVRManager>() != null) + " hmdPresent=" + OVRManager.isHmdPresent + " hands=" + FindObjectsByType<OVRHand>(FindObjectsSortMode.None).Length);
            int tracked = 0; foreach (var h in FindObjectsByType<OVRHand>(FindObjectsSortMode.None)) if (h.IsTracked) tracked++;
            Debug.Log("[PROBE] trackedHands=" + tracked + " state=" + (app != null ? app.State.ToString() : "null") + " theme=" + (app != null ? app.Theme : -1) + " fps=" + (1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f)).ToString("F0"));
            System.IO.Directory.CreateDirectory(@"E:\Winter\metavr\LastFrequency\logs\sim"); ScreenCapture.CaptureScreenshot(@"E:\Winter\metavr\LastFrequency\logs\sim\probe.png");
            yield return new WaitForSeconds(2f); Debug.Log("[PROBE] done");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
    }
}
