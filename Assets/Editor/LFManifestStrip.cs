using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace LF.Editor
{
    /// <summary>Removes the legacy android.permission.BLUETOOTH that Unity adds next to RECORD_AUDIO when the Microphone class is used.
    /// The app never touches Bluetooth, and on Android 12+ (all Quest 3 / 3S firmware, target SDK 36) that permission grants nothing anyway.
    /// RECORD_AUDIO (singing), MODIFY_AUDIO_SETTINGS (Unity's microphone path), INTERNET and ACCESS_NETWORK_STATE (share to phone on the local network)
    /// and HAND_TRACKING stay. Runs after the Meta XR manifest step; acts only for this product.</summary>
    public class LFManifestStrip : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 10000; } }
        const string AndroidNs = "http://schemas.android.com/apk/res/android", ToolsNs = "http://schemas.android.com/tools";
        static readonly string[] Drop = { "android.permission.BLUETOOTH" };

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (PlayerSettings.productName != "The Last Frequency") return;
            int removed = Strip(Path.Combine(path, "src", "main", "AndroidManifest.xml"), false);
            int marked = Strip(Path.GetFullPath(Path.Combine(path, "..", "launcher", "src", "main", "AndroidManifest.xml")), true);
            Debug.Log("[LF] manifest strip: removed " + removed + " permission entries, added " + marked + " remove-markers (" + string.Join(", ", Drop) + ")");
        }

        /// <summary>Library manifest: delete the entries. Launcher manifest (highest merge priority): add tools:node="remove" so no AAR can bring them back.</summary>
        static int Strip(string file, bool addMarker)
        {
            if (!File.Exists(file)) return 0;
            var doc = new XmlDocument(); doc.Load(file); var root = doc.DocumentElement; if (root == null) return 0; int n = 0;
            foreach (var perm in Drop)
            {
                bool present = false; var list = new System.Collections.Generic.List<XmlNode>(); foreach (XmlNode node in root.SelectNodes("uses-permission")) list.Add(node);
                foreach (var node in list)
                {
                    var el = node as XmlElement; if (el == null || el.GetAttribute("name", AndroidNs) != perm) continue;
                    if (addMarker && el.GetAttribute("node", ToolsNs) == "remove") { present = true; continue; }
                    root.RemoveChild(el); if (!addMarker) n++;
                }
                if (addMarker && !present)
                {
                    if (root.GetAttribute("xmlns:tools") == "") root.SetAttribute("xmlns:tools", ToolsNs);
                    var m = doc.CreateElement("uses-permission");
                    var an = doc.CreateAttribute("android", "name", AndroidNs); an.Value = perm; m.Attributes.Append(an);
                    var tn = doc.CreateAttribute("tools", "node", ToolsNs); tn.Value = "remove"; m.Attributes.Append(tn);
                    root.PrependChild(m); n++;
                }
            }
            doc.Save(file); return n;
        }
    }
}
