using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AndroidSpriteCompressionTool
{
    private const string MenuPath = "Tools/Optimization/Apply Android ETC2 To All Sprites";
    private const string SessionKey = "EndlessZombie.AndroidSpriteCompressionTool.Applied.v1";

    static AndroidSpriteCompressionTool()
    {
        EditorApplication.delayCall += ApplyOnceAfterCompile;
    }

    private static void ApplyOnceAfterCompile()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        ApplyToAllSprites();
    }

    [MenuItem(MenuPath)]
    public static void ApplyToAllSprites()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int changedCount = 0;
        int spriteCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in textureGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer ||
                    importer.textureType != TextureImporterType.Sprite)
                    continue;

                spriteCount++;
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Android");

                if (settings.overridden &&
                    settings.format == TextureImporterFormat.ETC2_RGBA8Crunched &&
                    settings.crunchedCompression)
                    continue;

                settings.name = "Android";
                settings.overridden = true;
                settings.format = TextureImporterFormat.ETC2_RGBA8Crunched;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.crunchedCompression = true;
                importer.SetPlatformTextureSettings(settings);

                if (AssetDatabase.WriteImportSettingsIfDirty(assetPath))
                    changedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[Android Sprite Compression] Updated {changedCount}/{spriteCount} sprites to RGBA Crunched ETC2.");
    }
}
