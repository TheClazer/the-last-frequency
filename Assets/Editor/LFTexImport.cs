using UnityEditor;
using UnityEngine;

namespace LF.Editor
{
    /// <summary>Jungle textures: *_n.png = normal maps; everything capped at 2048 and ASTC on Android.</summary>
    public class LFTexImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("Resources/Jungle/")) return;
            var ti = (TextureImporter)assetImporter;
            if (assetPath.EndsWith("_n.png")) ti.textureType = TextureImporterType.NormalMap;
            ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.anisoLevel = 2;
            var android = ti.GetPlatformTextureSettings("Android"); android.overridden = true; android.maxTextureSize = assetPath.Contains("GiantGuardian") || assetPath.Contains("TempleArch") ? 2048 : 1024; android.format = TextureImporterFormat.ASTC_6x6; ti.SetPlatformTextureSettings(android);
        }
    }
}
