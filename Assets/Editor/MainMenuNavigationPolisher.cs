#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuNavigationPolisher
{
    private const string PrefabPath = "Assets/Resources/MainMenuCanvas.prefab";

    [MenuItem("Tools/Endless Zombie/UI/Apply Four-Tab Main Menu")]
    public static void ApplyFromMenu() => Apply(true);

    private static void Apply(bool verbose)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null) return;
        MainMenuCanvasView view = root.GetComponent<MainMenuCanvasView>();
        if (view != null)
        {
            view.CaptureReferences();
            MainMenuManager.StyleAuthoredNavigation(view, true);
            ApplyIcons(root.transform);
            view.CaptureReferences();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (verbose) Debug.Log("[Main Menu] Applied four evenly spaced tabs and removed Inventory.");
        }
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyIcons(Transform root)
    {
        SetIcon(root, "Navigation/Battle Tab/Battle Icon", "icon_battle.png");
        SetIcon(root, "Navigation/PET Tab/PET Icon", "icon_pet.png");
        SetIcon(root, "Navigation/WEAPON Tab/WEAPON Icon", "icon_damage.png");
        SetIcon(root, "Navigation/SHOP Tab/SHOP Icon", "icon_shop.png");
        SetIcon(root, "MAX HP/MAX HP Icon", "icon_health.png");
        SetIcon(root, "INCOME/INCOME Icon", "icon_gold.png");
    }

    private static void SetIcon(Transform root, string path, string fileName)
    {
        Transform target = root.Find(path);
        if (target == null || !target.TryGetComponent(out Image image)) return;
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/ApocalypseGenerated/{fileName}");
        image.preserveAspect = true;
        image.enabled = image.sprite != null;
        image.raycastTarget = false;
        EditorUtility.SetDirty(image);
    }
}
#endif
