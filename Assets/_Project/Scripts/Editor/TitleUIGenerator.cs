using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleUIGenerator
{
    private const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";
    private const string LegacyCanvasName = "TitleScreen_Canvas";
    private const string RuntimeCanvasName = "TitleRuntimeCanvas";

    [MenuItem("_Project/UI/Refresh Title UI")]
    [MenuItem("Tools/GymGame/UI/Materialize Title UI")]
    public static void Generate()
    {
        MaterializeTitleRuntimeCanvas(false);
    }

    public static void Build()
    {
        MaterializeTitleRuntimeCanvas(false);
    }

    private static void MaterializeTitleRuntimeCanvas(bool preserveCurrentScene)
    {
        Scene originalActiveScene = EditorSceneManager.GetActiveScene();
        Scene scene = originalActiveScene;
        bool openedAdditively = false;
        if (scene.path != TitleScenePath)
        {
            scene = EditorSceneManager.OpenScene(
                TitleScenePath,
                preserveCurrentScene ? OpenSceneMode.Additive : OpenSceneMode.Single);
            openedAdditively = preserveCurrentScene;
        }

        GameObject legacyCanvas = FindRootObject(scene, LegacyCanvasName);
        if (legacyCanvas != null)
        {
            legacyCanvas.SetActive(false);
            EditorUtility.SetDirty(legacyCanvas);
        }

        EnsureSceneEventSystem(scene);

        GameObject runtimeCanvas = FindRootObject(scene, RuntimeCanvasName);
        if (runtimeCanvas == null)
        {
            runtimeCanvas = new GameObject(RuntimeCanvasName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(runtimeCanvas, scene);
        }

        runtimeCanvas.SetActive(true);
        ConfigureCanvas(runtimeCanvas);

        TitleMenuUIController controller = GetOrAdd<TitleMenuUIController>(runtimeCanvas);
        controller.Configure(GameUiTheme.CreateDefault(), "TestSandbox");
        controller.BuildUi(runtimeCanvas.transform);

        EditorUtility.SetDirty(runtimeCanvas);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (openedAdditively)
        {
            EditorSceneManager.CloseScene(scene, true);
            if (originalActiveScene.IsValid())
            {
                EditorSceneManager.SetActiveScene(originalActiveScene);
            }
        }

        Debug.Log($"[TitleUIGenerator] Materialized {RuntimeCanvasName} in {TitleScenePath} and disabled {LegacyCanvasName}.");
    }

    private static void ConfigureCanvas(GameObject canvasObject)
    {
        RectTransform rect = GetOrAdd<RectTransform>(canvasObject);
        GameUiFactory.Stretch(rect);

        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        GetOrAdd<GraphicRaycaster>(canvasObject);
    }

    private static T GetOrAdd<T>(GameObject gameObject)
        where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        if (!scene.IsValid())
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }
        }

        return null;
    }

    private static void EnsureSceneEventSystem(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<EventSystem>(true) != null)
            {
                return;
            }
        }

        GameObject eventSystemObject = new GameObject("TitleScene_EventSystem", typeof(EventSystem));
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        EditorUtility.SetDirty(eventSystemObject);
    }
}
