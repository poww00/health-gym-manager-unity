using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameUIGenerator
{
    private const string TestSandboxScenePath = "Assets/_Project/Scenes/TestSandbox.unity";
    private const string RuntimeCanvasName = "RuntimeGameUI_Canvas";
    private const string LegacyHudCanvasName = "MainHUD_Canvas";
    private const string LegacyStaffCanvasName = "StaffUI_Canvas";
    private const string LegacySimpleCanvasName = "SimpleGameUI_Canvas";

    [MenuItem("_Project/UI/Refresh TestSandbox Game UI")]
    [MenuItem("Tools/GymGame/UI/Materialize Game UI")]
    public static void Generate()
    {
        MaterializeTestSandboxRuntimeCanvas(false);
    }

    public static void Build()
    {
        MaterializeTestSandboxRuntimeCanvas(false);
    }

    private static void MaterializeTestSandboxRuntimeCanvas(bool preserveCurrentScene)
    {
        Scene originalActiveScene = EditorSceneManager.GetActiveScene();
        Scene scene = originalActiveScene;
        bool openedAdditively = false;

        if (scene.path != TestSandboxScenePath)
        {
            scene = EditorSceneManager.OpenScene(
                TestSandboxScenePath,
                preserveCurrentScene ? OpenSceneMode.Additive : OpenSceneMode.Single);
            openedAdditively = preserveCurrentScene;
        }

        HideLegacyCanvas(scene, LegacySimpleCanvasName);
        HideLegacyCanvas(scene, LegacyHudCanvasName);
        HideLegacyCanvas(scene, LegacyStaffCanvasName);
        EnsureSceneEventSystem(scene);

        GameObject runtimeCanvas = FindRootObject(scene, RuntimeCanvasName);
        if (runtimeCanvas == null)
        {
            runtimeCanvas = new GameObject(RuntimeCanvasName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(runtimeCanvas, scene);
        }

        runtimeCanvas.SetActive(true);
        ConfigureCanvas(runtimeCanvas);

        GameRuntimeUIController controller = GameUiFactory.GetOrAdd<GameRuntimeUIController>(runtimeCanvas);
        controller.MaterializeForEditMode();

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

        Debug.Log($"[GameUIGenerator] Materialized {RuntimeCanvasName} in {TestSandboxScenePath}.");
    }

    private static void ConfigureCanvas(GameObject canvasObject)
    {
        RectTransform rect = GameUiFactory.GetOrAdd<RectTransform>(canvasObject);
        GameUiFactory.Stretch(rect);

        Canvas canvas = GameUiFactory.GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GameUiFactory.GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        GameUiFactory.GetOrAdd<GraphicRaycaster>(canvasObject);
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

    private static void HideLegacyCanvas(Scene scene, string objectName)
    {
        GameObject root = FindRootObject(scene, objectName);
        if (root == null)
        {
            return;
        }

        CanvasGroup group = GameUiFactory.GetOrAdd<CanvasGroup>(root);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        EditorUtility.SetDirty(root);
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

        GameObject eventSystemObject = new GameObject("RuntimeGameUI_EventSystem", typeof(EventSystem));
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        EditorUtility.SetDirty(eventSystemObject);
    }
}
