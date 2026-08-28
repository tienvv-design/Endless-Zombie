using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LoadingScreenInstaller
{
    private const string ArtworkPath = "Assets/Art/LoadingScreen/EndlessZombie_LoadingScreen.png";
    private const string LoadingScenePath = "Assets/Scenes/LoadingScreen.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    [MenuItem("Tools/Endless Zombie/Build Loading Screen")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before rebuilding the loading screen.");

        ConfigureArtworkImporter();
        Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(ArtworkPath);
        if (artwork == null)
            throw new MissingReferenceException($"Loading artwork was not found at {ArtworkPath}.");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            BuildScene(scene, artwork);
            if (!EditorSceneManager.SaveScene(scene, LoadingScenePath))
                throw new InvalidOperationException($"Unity could not save {LoadingScenePath}.");
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        ConfigureBuildSettings();
        MainMenuPlayModeStart.UseMainMenu();
        AssetDatabase.SaveAssets();
        Debug.Log($"Loading screen built from {ArtworkPath}; startup order is LoadingScreen -> GameScene.");
    }

    [MenuItem("Tools/Endless Zombie/Validate Loading Screen")]
    public static void Validate()
    {
        Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(ArtworkPath);
        if (artwork == null)
            throw new MissingReferenceException("Loading artwork is not imported as a Sprite.");

        int loadingIndex = Array.FindIndex(EditorBuildSettings.scenes,
            entry => entry.enabled && entry.path == LoadingScenePath);
        int gameIndex = Array.FindIndex(EditorBuildSettings.scenes,
            entry => entry.enabled && entry.path == GameScenePath);
        if (loadingIndex != 0 || gameIndex < 1)
            throw new InvalidOperationException("LoadingScreen must be the first enabled scene and GameScene must follow it.");

        Scene scene = EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Additive);
        try
        {
            LoadingScreenController controller = UnityEngine.Object.FindFirstObjectByType<LoadingScreenController>();
            if (controller == null || scene.GetRootGameObjects().All(root => root.GetComponentInChildren<LoadingScreenController>(true) == null))
                throw new MissingComponentException("LoadingScreenController is missing from LoadingScreen.unity.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("Loading screen validation passed: artwork, controller and startup scene order are ready.");
    }

    private static void ConfigureArtworkImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(ArtworkPath) as TextureImporter;
        if (importer == null)
            throw new MissingReferenceException($"Texture importer was not found for {ArtworkPath}.");

        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || importer.alphaIsTransparency;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        if (changed)
            importer.SaveAndReimport();
    }

    private static void BuildScene(Scene scene, Sprite artwork)
    {
        GameObject canvasObject = new("Loading Screen", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(LoadingScreenController));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        AddImage(root, "Black Background", null, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image artworkImage = AddImage(root, "Loading Artwork", artwork, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AspectRatioFitter fitter = artworkImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = artwork.rect.width / artwork.rect.height;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AddText(root, "Title", "ENDLESS ZOMBIE", font, 76, FontStyle.Bold, new Color(0.96f, 0.23f, 0.1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(920f, 130f));

        RectTransform track = AddImage(root, "Progress Track", null, new Color(0.015f, 0.02f, 0.025f, 0.92f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 142f), new Vector2(720f, 22f)).rectTransform;
        Outline trackOutline = track.gameObject.AddComponent<Outline>();
        trackOutline.effectColor = new Color(0.96f, 0.23f, 0.1f, 0.8f);
        trackOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform fill = AddImage(track, "Progress Fill", null, new Color(1f, 0.42f, 0.08f, 1f),
            Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero).rectTransform;
        fill.pivot = new Vector2(0f, 0.5f);

        Text label = AddText(root, "Progress Label", "LOADING  0%", font, 31, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(560f, 54f));

        SerializedObject controller = new(canvasObject.GetComponent<LoadingScreenController>());
        controller.FindProperty("_nextSceneName").stringValue = "GameScene";
        controller.FindProperty("_minimumDisplayTime").floatValue = 1.75f;
        controller.FindProperty("_progressFill").objectReferenceValue = fill;
        controller.FindProperty("_progressLabel").objectReferenceValue = label;
        controller.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image AddImage(RectTransform parent, string name, Sprite sprite, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text AddText(RectTransform parent, string name, string value, Font font, int size,
        FontStyle style, Color color, Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text text = item.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(14, size / 2);
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(entry => entry.path != LoadingScenePath && entry.path != GameScenePath)
            .ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(LoadingScenePath, true));
        scenes.Insert(1, new EditorBuildSettingsScene(GameScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
