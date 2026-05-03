using UnityEditor;
using UnityEngine;

public static class UiPrefabAutoRegenerator
{
    private const string MainHudPrefabPath = "Assets/_Project/Prefabs/UI/MainHUD_Canvas.prefab";

    [MenuItem("_Project/UI/Manual Refresh Loaded MainHUD Instances")]
    public static void RefreshLoadedSceneUiInstances()
    {
        MainHUDController[] controllers = Object.FindObjectsByType<MainHUDController>(FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            MainHUDController controller = controllers[i];
            if (controller == null)
            {
                continue;
            }

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(controller.gameObject);
            if (prefabRoot == null)
            {
                continue;
            }

            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
            if (prefabPath != MainHudPrefabPath)
            {
                continue;
            }

            PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.UserAction);
        }
    }
}
