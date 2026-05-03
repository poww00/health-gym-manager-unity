using UnityEditor;
using UnityEngine;

public static class MainHUDGenerator
{
    private const string MainHudPrefabPath = "Assets/_Project/Prefabs/UI/MainHUD_Canvas.prefab";

    [MenuItem("_Project/UI/Refresh Main HUD")]
    public static void Generate()
    {
        GenerateAll();
    }

    [MenuItem("_Project/UI/Refresh All UI")]
    public static void GenerateAll()
    {
        PrepareUiSpriteImports();

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MainHudPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[MainHUDGenerator] 프리팹을 열지 못했다: {MainHudPrefabPath}");
            return;
        }

        try
        {
            MainHUDController controller = prefabRoot.GetComponent<MainHUDController>();
            EquipmentCatalogUIController catalogController = prefabRoot.GetComponent<EquipmentCatalogUIController>();
            GameMenuUIController menuController = prefabRoot.GetComponent<GameMenuUIController>();
            StaffUIController staffController = prefabRoot.GetComponent<StaffUIController>();

            if (controller == null)
            {
                Debug.LogError("[MainHUDGenerator] MainHUDController를 찾지 못했다.");
                return;
            }

            GameUiTheme theme = GameUiTheme.CreateDefault();

            if (catalogController != null)
            {
                catalogController.Configure(theme, controller);
            }

            controller.Configure(theme, catalogController, menuController, staffController);
            controller.EditorRebuildLowerPanelPreview();

            RemoveMissingScriptsRecursively(prefabRoot);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MainHudPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MainHUDGenerator] MainHUD_Canvas lower panel preview regenerated.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static void PrepareUiSpriteImports()
    {
        AssetDatabase.Refresh();
    }

    private static void RemoveMissingScriptsRecursively(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        RemoveMissingScriptsOnObject(root);

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
            {
                continue;
            }

            RemoveMissingScriptsOnObject(child.gameObject);
        }
    }

    private static void RemoveMissingScriptsOnObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
    }
}