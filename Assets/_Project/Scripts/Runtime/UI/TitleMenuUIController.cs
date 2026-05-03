using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class TitleMenuUIController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "TestSandbox";

    [Header("Buttons")]
    public Button continueButton;
    public Button newGameButton;
    public Button slot1Button;
    public Button slot2Button;

    [Header("Status Text")]
    public Text slot1StatusText;
    public Text slot2StatusText;

    [Header("Disabled Overlays")]
    public GameObject continueDisabledOverlay;
    public GameObject slot1DisabledOverlay;
    public GameObject slot2DisabledOverlay;

    private const string SlotReadyLabel = "저장 있음";
    private const string SlotEmptyLabel = "빈 슬롯";
    private const string RuntimeRootName = "RuntimeTitleUIRoot";
    private const int TextReadabilityFontSizeOffset = 2;

    private GameUiTheme theme;
    private MonoBehaviour legacyTitleMenu;
    private float nextRefreshAt;
    private bool isBuilt;

    public void Configure(GameUiTheme uiTheme, string nextSceneName)
    {
        theme = uiTheme ?? GameUiTheme.CreateDefault();
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            gameSceneName = nextSceneName;
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            PrepareEditModePreview();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorApplication.delayCall -= PrepareEditModePreviewDelayed;
        EditorApplication.delayCall += PrepareEditModePreviewDelayed;
    }

    private void PrepareEditModePreviewDelayed()
    {
        if (this == null || Application.isPlaying)
        {
            return;
        }

        PrepareEditModePreview();
    }
#endif

    [ContextMenu("Repair Title Edit Preview Sprites Only")]
    public void RepairTitleEditPreviewSpritesOnly()
    {
        PrepareEditModePreview();
    }

    private void PrepareEditModePreview()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || transform == null)
        {
            return;
        }

        theme ??= GameUiTheme.CreateDefault();

        Transform existingRoot = transform.Find(RuntimeRootName);
        if (existingRoot == null)
        {
            return;
        }

        existingRoot.gameObject.SetActive(true);
        TryBindExistingUi(transform);
        EnsureExistingGeneratedSprites(existingRoot);
        NormalizeExistingTextStyle(existingRoot);
        HideLegacyTitleChildren(transform, existingRoot);
        existingRoot.SetAsLastSibling();

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(existingRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    public void BuildUi(Transform canvasRoot)
    {
        if (isBuilt && Application.isPlaying)
        {
            return;
        }

        theme ??= GameUiTheme.CreateDefault();
        GameUiFactory.EnsureEventSystem(canvasRoot, "TitleUIRuntime_EventSystem");

        Transform existingRoot = canvasRoot != null ? canvasRoot.Find(RuntimeRootName) : null;
        if (existingRoot != null && TryBindExistingUi(canvasRoot))
        {
            // 이미 씬에 직렬화된 타이틀 UI가 있으면 RectTransform/배치값을 절대 초기화하지 않는다.
            // 누락된 Sprite 참조만 보수하고 버튼 바인딩만 다시 연결한다.
            EnsureExistingGeneratedSprites(existingRoot);
            NormalizeExistingTextStyle(existingRoot);
            HideLegacyTitleChildren(canvasRoot, existingRoot);
            existingRoot.SetAsLastSibling();
            BindButtons();
            RefreshState();
            return;
        }

        GameObject root = existingRoot != null
            ? existingRoot.gameObject
            : GameUiFactory.CreateNode(canvasRoot, RuntimeRootName);
        root.SetActive(true);
        HideLegacyTitleChildren(canvasRoot, root.transform);
        root.transform.SetAsLastSibling();
        GameUiFactory.ClearChildren(root.transform);
        GameUiFactory.Stretch(root.GetComponent<RectTransform>());

        BuildBackground(root.transform);
        BuildLogo(root.transform);
        BuildMenu(root.transform);

        BindButtons();
        isBuilt = true;
        RefreshState();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            PrepareEditModePreview();
            return;
        }

        if (!isBuilt && !TryBindExistingUi(transform))
        {
            BuildUi(transform);
        }

        BindButtons();
        DisableLegacyOnGui();
        RefreshState();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (Time.unscaledTime < nextRefreshAt)
        {
            return;
        }

        nextRefreshAt = Time.unscaledTime + 0.5f;
        RefreshState();
    }

    private void BuildBackground(Transform root)
    {
        GameObject backgroundRoot = GameUiFactory.CreateNode(root, "BackgroundRoot");
        GameUiFactory.Stretch(backgroundRoot.GetComponent<RectTransform>());

        GameObject image = CreateGeneratedImage(
            backgroundRoot.transform,
            "TitleBackgroundImage",
            "GeneratedRuntimeUI/title/title_background_v2",
            0f,
            960f,
            1080f,
            1920f,
            false,
            false);
        GameUiFactory.Stretch(image.GetComponent<RectTransform>());
    }

    private void BuildLogo(Transform root)
    {
        GameObject logoRoot = GameUiFactory.CreateNode(root, "LogoRoot");
        SetRect(logoRoot.GetComponent<RectTransform>(), 0f, 1508f, 1010f, 474f);
        CreateGeneratedImage(logoRoot.transform, "TitleLogoImage", "GeneratedRuntimeUI/title/title_logo_v2", 0f, 0f, 1010f, 474f, true, true);
    }

    private void BuildMenu(Transform root)
    {
        GameObject menuRoot = GameUiFactory.CreateNode(root, "MenuPanelRoot");
        SetRect(menuRoot.GetComponent<RectTransform>(), 0f, 392f, 910f, 628f);

        GameObject frame = CreateGeneratedImage(menuRoot.transform, "MenuPanel", "GeneratedRuntimeUI/ui_v2/title_menu_panel_base", 0f, 0f, 910f, 628f, false, true);
        CreateGeneratedImage(frame.transform, "MenuRibbon", "GeneratedRuntimeUI/ui_v2/header_bar_blue", 0f, 364f, 852f, 68f, false, true);

        continueButton = CreateSpriteButton(frame.transform, "ContinueButton", "GeneratedRuntimeUI/ui_v2/button_green_base", "이어하기", -204f, 164f, 360f, 94f, Color.white, out continueDisabledOverlay, 38);
        newGameButton = CreateSpriteButton(frame.transform, "NewGameButton", "GeneratedRuntimeUI/ui_v2/button_beige_base", "새 게임", 204f, 164f, 360f, 94f, theme.Ink, out _, 38);

        CreateText(frame.transform, "LoadLabel", "불러오기", 31, theme.Ink, TextAnchor.MiddleLeft, -344f, 50f, 220f, 44f, true);
        CreateText(frame.transform, "GuideLabel", "저장 슬롯을 선택해 이어서 운영하세요", 18, theme.MutedInk, TextAnchor.MiddleRight, 202f, 50f, 460f, 36f, true);

        slot1Button = CreateSlotRow(frame.transform, "SlotRow1Button", 0f, -56f, 1, out slot1StatusText, out slot1DisabledOverlay);
        slot2Button = CreateSlotRow(frame.transform, "SlotRow2Button", 0f, -160f, 2, out slot2StatusText, out slot2DisabledOverlay);
        CreateText(frame.transform, "FooterTip", "새 게임은 현재 저장 시스템을 유지한 채 새 운영으로 시작합니다.", 17, theme.MutedInk, TextAnchor.MiddleCenter, 0f, -270f, 740f, 34f, true);
    }

    private static void EnsureExistingGeneratedSprites(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            switch (image.gameObject.name)
            {
                case "TitleBackgroundImage":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/title/title_background_v2", false);
                    break;
                case "TitleLogoImage":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/title/title_logo_v2", true);
                    break;
                case "MenuPanel":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/title_menu_panel_base", false);
                    break;
                case "MenuRibbon":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/header_bar_blue", false);
                    break;
                case "ContinueButton":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/button_green_base", false);
                    break;
                case "NewGameButton":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/button_beige_base", false);
                    break;
                case "SlotRow1Button":
                case "SlotRow2Button":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/slot_row_base", false);
                    break;
                case "SlotIconBack":
                    GeneratedRuntimeSprites.Assign(image, "GeneratedRuntimeUI/ui_v2/button_small_beige_base", false);
                    break;
            }
        }
    }

    private Button CreateSlotRow(Transform parent, string name, float x, float y, int slot, out Text statusText, out GameObject disabledOverlay)
    {
        GameObject row = CreateGeneratedImage(parent, name, "GeneratedRuntimeUI/ui_v2/slot_row_base", x, y, 768f, 88f, false, true);
        Image image = row.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = row.AddComponent<Button>();
        button.targetGraphic = image;

        CreateGeneratedImage(row.transform, "SlotIconBack", "GeneratedRuntimeUI/ui_v2/button_small_beige_base", -330f, 0f, 58f, 58f, false, true);
        CreateText(row.transform, "SlotIcon", "★", 27, theme.Ink, TextAnchor.MiddleCenter, -330f, 0f, 52f, 52f, true);
        statusText = CreateText(row.transform, "SlotStatus", $"슬롯 {slot} : 확인 중", 29, theme.Ink, TextAnchor.MiddleLeft, 4f, 0f, 570f, 60f, true);
        CreateText(row.transform, "Arrow", ">", 39, theme.MutedInk, TextAnchor.MiddleCenter, 346f, 0f, 44f, 58f, true);

        disabledOverlay = CreateSolid(row.transform, "DisabledOverlay", new Color(0.10f, 0.07f, 0.03f, 0.30f), 0f, 0f, 768f, 88f, true);
        disabledOverlay.SetActive(false);
        return button;
    }

    private Button CreateSpriteButton(Transform parent, string name, string spritePath, string label, float x, float y, float width, float height, Color textColor, out GameObject disabledOverlay, int fontSize)
    {
        GameObject node = CreateGeneratedImage(parent, name, spritePath, x, y, width, height, false, true);
        Image image = node.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = node.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
        colors.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        button.colors = colors;

        Text labelText = CreateText(node.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, 0f, 0f, width - 22f, height - 16f, true);
        labelText.fontSize = fontSize;
        labelText.fontStyle = FontStyle.Bold;
        disabledOverlay = CreateSolid(button.transform, "DisabledOverlay", new Color(0.10f, 0.07f, 0.03f, 0.34f), 0f, 0f, width, height, true);
        disabledOverlay.SetActive(false);
        return button;
    }

    private static void NormalizeExistingTextStyle(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            if (IsPrimaryTitleButtonLabel(text))
            {
                text.fontStyle = FontStyle.Bold;
                if (text.fontSize < 38)
                {
                    text.fontSize = 38;
                }
                ApplyStrongTextTreatment(text, text.color);
                continue;
            }

            text.fontStyle = FontStyle.Normal;
            if (TryGetTitleTextFontSize(text, out int normalizedFontSize))
            {
                text.fontSize = normalizedFontSize;
            }

            ApplyStrongTextTreatment(text, text.color);
        }
    }

    private static bool IsPrimaryTitleButtonLabel(Text text)
    {
        if (text == null || text.gameObject.name != "Label" || text.transform.parent == null)
        {
            return false;
        }

        string parentName = text.transform.parent.name;
        return parentName == "ContinueButton" || parentName == "NewGameButton";
    }

    private static bool TryGetTitleTextFontSize(Text text, out int fontSize)
    {
        fontSize = 0;
        if (text == null)
        {
            return false;
        }

        switch (text.gameObject.name)
        {
            case "LoadLabel":
                fontSize = 31 + TextReadabilityFontSizeOffset;
                return true;
            case "GuideLabel":
                fontSize = 18 + TextReadabilityFontSizeOffset;
                return true;
            case "FooterTip":
                fontSize = 17 + TextReadabilityFontSizeOffset;
                return true;
            case "SlotIcon":
                fontSize = 27 + TextReadabilityFontSizeOffset;
                return true;
            case "SlotStatus":
                fontSize = 29 + TextReadabilityFontSizeOffset;
                return true;
            case "Arrow":
                fontSize = 39 + TextReadabilityFontSizeOffset;
                return true;
        }

        return false;
    }

    private GameObject CreateGeneratedImage(Transform parent, string name, string spritePath, float x, float y, float width, float height, bool preserveAspect, bool localParent)
    {
        GameObject node = GameUiFactory.CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image image = node.GetComponent<Image>();
        image.raycastTarget = false;
        if (!GeneratedRuntimeSprites.Assign(image, spritePath, preserveAspect))
        {
            image.color = theme.PanelFill;
        }

        SetRect(node.GetComponent<RectTransform>(), x, y, width, height, localParent);
        return node;
    }

    private GameObject CreateSolid(Transform parent, string name, Color color, float x, float y, float width, float height, bool localParent)
    {
        GameObject node = GameUiFactory.CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image image = node.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        SetRect(node.GetComponent<RectTransform>(), x, y, width, height, localParent);
        return node;
    }

    private Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment, float x, float y, float width, float height, bool localParent)
    {
        Text text = GameUiFactory.CreateText(parent, name, theme, fontSize + TextReadabilityFontSizeOffset, color, alignment, FontStyle.Normal);
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.92f;
        ApplyStrongTextTreatment(text, color);
        SetRect(text.rectTransform, x, y, width, height, localParent);
        return text;
    }

    private static void ApplyStrongTextTreatment(Text text, Color color)
    {
        if (text == null)
        {
            return;
        }

        float luma = (color.r * 0.299f) + (color.g * 0.587f) + (color.b * 0.114f);
        Outline outline = GameUiFactory.GetOrAdd<Outline>(text.gameObject);
        Shadow shadow = GameUiFactory.GetOrAdd<Shadow>(text.gameObject);

        outline.effectColor = luma > 0.6f
            ? new Color(0.08f, 0.05f, 0.02f, 0.45f)
            : new Color(1.00f, 0.92f, 0.70f, 0.14f);
        outline.effectDistance = new Vector2(1f, -1f);
        shadow.effectColor = new Color(0.08f, 0.05f, 0.02f, luma > 0.6f ? 0.38f : 0.16f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height, bool localParent = false)
    {
        rect.anchorMin = localParent ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void DisableLegacyOnGui()
    {
        if (legacyTitleMenu != null)
        {
            legacyTitleMenu.enabled = false;
            return;
        }

        legacyTitleMenu = FindFirstObjectByType<TitleMenuManager>();
        if (legacyTitleMenu != null)
        {
            legacyTitleMenu.enabled = false;
        }
    }

    private bool TryBindExistingUi(Transform canvasRoot)
    {
        theme ??= GameUiTheme.CreateDefault();

        Transform root = canvasRoot != null ? canvasRoot.Find(RuntimeRootName) : null;
        if (root == null)
        {
            return false;
        }

        continueButton = FindDeepComponent<Button>(root, "ContinueButton");
        newGameButton = FindDeepComponent<Button>(root, "NewGameButton");
        slot1Button = FindDeepComponent<Button>(root, "SlotRow1Button");
        slot2Button = FindDeepComponent<Button>(root, "SlotRow2Button");
        slot1StatusText = FindDeepComponent<Text>(root, "SlotStatus", "SlotRow1Button");
        slot2StatusText = FindDeepComponent<Text>(root, "SlotStatus", "SlotRow2Button");
        continueDisabledOverlay = FindDeepChild(root, "DisabledOverlay", "ContinueButton")?.gameObject;
        slot1DisabledOverlay = FindDeepChild(root, "DisabledOverlay", "SlotRow1Button")?.gameObject;
        slot2DisabledOverlay = FindDeepChild(root, "DisabledOverlay", "SlotRow2Button")?.gameObject;

        isBuilt = continueButton != null &&
                  newGameButton != null &&
                  slot1Button != null &&
                  slot2Button != null &&
                  slot1StatusText != null &&
                  slot2StatusText != null;
        if (isBuilt)
        {
            EnsureExistingGeneratedSprites(root);
            NormalizeExistingTextStyle(root);
        }

        return isBuilt;
    }

    private static T FindDeepComponent<T>(Transform root, string objectName, string parentName = null) where T : Component
    {
        Transform found = FindDeepChild(root, objectName, parentName);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform root, string objectName, string parentName = null)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName && (string.IsNullOrEmpty(parentName) || HasParentNamed(child, parentName)))
            {
                return child;
            }

            Transform nested = FindDeepChild(child, objectName, parentName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static bool HasParentNamed(Transform child, string parentName)
    {
        Transform current = child.parent;
        while (current != null)
        {
            if (current.name == parentName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void ClearChildrenExceptEventSystem(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
            {
                continue;
            }

            GameUiFactory.DestroyObject(child.gameObject);
        }
    }

    private static void HideLegacyTitleChildren(Transform canvasRoot, Transform runtimeRoot)
    {
        if (canvasRoot == null)
        {
            return;
        }

        for (int i = 0; i < canvasRoot.childCount; i++)
        {
            Transform child = canvasRoot.GetChild(i);
            if (child == runtimeRoot || child.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private void BindButtons()
    {
        BindButton(continueButton, ContinueGame);
        BindButton(newGameButton, StartNewGame);
        BindButton(slot1Button, () => LoadManualSlot(1));
        BindButton(slot2Button, () => LoadManualSlot(2));
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static bool HasAutoSave()
    {
        return SaveManager.HasAutoSaveFile();
    }

    private static bool HasManualSlot(int slot)
    {
        return SaveManager.HasManualSaveFile(slot);
    }

    private void ContinueGame()
    {
        if (HasAutoSave())
        {
            GameEntryRequest.Set(GameEntryRequest.EntryMode.ContinueFromAutoSave);
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (HasManualSlot(1))
        {
            GameEntryRequest.Set(GameEntryRequest.EntryMode.LoadManualSlot1);
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (HasManualSlot(2))
        {
            GameEntryRequest.Set(GameEntryRequest.EntryMode.LoadManualSlot2);
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void StartNewGame()
    {
        GameEntryRequest.Set(GameEntryRequest.EntryMode.NewGame);
        SceneManager.LoadScene(gameSceneName);
    }

    private void LoadManualSlot(int slot)
    {
        if (!HasManualSlot(slot))
        {
            return;
        }

        GameEntryRequest.EntryMode mode = slot == 1
            ? GameEntryRequest.EntryMode.LoadManualSlot1
            : slot == 2
                ? GameEntryRequest.EntryMode.LoadManualSlot2
                : GameEntryRequest.EntryMode.None;
        if (mode == GameEntryRequest.EntryMode.None)
        {
            Debug.LogWarning($"[TitleMenuUIController] 지원하지 않는 수동 슬롯 요청: {slot}");
            return;
        }

        GameEntryRequest.Set(mode);
        SceneManager.LoadScene(gameSceneName);
    }

    private void RefreshState()
    {
        DisableLegacyOnGui();

        bool hasAutoSave = HasAutoSave();
        bool hasSlot1 = HasManualSlot(1);
        bool hasSlot2 = HasManualSlot(2);
        bool hasAnySave = hasAutoSave || hasSlot1 || hasSlot2;

        SetText(slot1StatusText, FormatSlotStatus(1, hasSlot1));
        SetText(slot2StatusText, FormatSlotStatus(2, hasSlot2));

        SetInteractable(continueButton, hasAnySave);
        SetInteractable(slot1Button, hasSlot1);
        SetInteractable(slot2Button, hasSlot2);

        SetOverlay(continueDisabledOverlay, !hasAnySave);
        SetOverlay(slot1DisabledOverlay, !hasSlot1);
        SetOverlay(slot2DisabledOverlay, !hasSlot2);
    }

    private static string FormatSlotStatus(int slot, bool hasSave)
    {
        return $"슬롯 {slot} : {(hasSave ? SlotReadyLabel : SlotEmptyLabel)}";
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static void SetOverlay(GameObject overlay, bool visible)
    {
        if (overlay != null)
        {
            overlay.SetActive(visible);
        }
    }
}
