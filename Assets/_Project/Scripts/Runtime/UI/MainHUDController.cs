using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainHUDController : MonoBehaviour
{
    [Header("Top Info Texts")]
    public Text cashText;
    public Text starCoinText;
    public Text dateText;
    public Text timeText;

    [Header("Top Buttons")]
    public Button staffButton;
    public Button menuButton;
    public Button btnPlay;
    public Button btnFast;
    public Button btnVeryFast;

    [Header("Bottom Tab Buttons")]
    public Button operateTabBtn;
    public Button placementTabBtn;
    public Button economyTabBtn;
    public Button reviewTabBtn;

    [Header("Bottom Panel Content")]
    public Text bottomPanelTitle;
    public Text bottomPanelContent;
    public ScrollRect bottomScrollRect;

    [Header("Bottom Panel States")]
    public GameObject catalogRoot;
    public GameObject textRoot;

    [Header("Prefab Skin")]
    [SerializeField] private Sprite inactiveTabSprite;
    [SerializeField] private Sprite activeTabSprite;
    [SerializeField] private Sprite speedButtonSprite;
    [SerializeField] private Sprite topHudInactiveButtonSprite;
    [SerializeField] private Sprite topHudActiveButtonSprite;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite bottomTabInactiveSprite;
    [SerializeField] private Sprite bottomTabActiveSprite;
    [SerializeField] private Sprite lowerContentBaseSprite;
    [SerializeField] private Sprite lowerHeaderOperateTabSprite;
    [SerializeField] private Sprite lowerHeaderStatusTabSprite;
    [SerializeField] private Sprite lowerSummaryRowSprite;
    [SerializeField] private Sprite lowerFeatureRowSprite;
    [SerializeField] private Sprite lowerMemoPanelSprite;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite iconCash;
    [SerializeField] private Sprite iconClock;
    [SerializeField] private Sprite iconStarCoin;
    [SerializeField] private Sprite iconUser;
    [SerializeField] private Sprite iconMenu;
    [SerializeField] private Sprite iconPlay;
    [SerializeField] private Sprite iconFastForward;
    [SerializeField] private Sprite iconClipboard;
    [SerializeField] private Sprite iconHammer;
    [SerializeField] private Sprite iconGraph;
    [SerializeField] private Sprite iconStar;

    public Sprite LowerHeaderOperateTabSprite => lowerHeaderOperateTabSprite;
    public Sprite LowerHeaderStatusTabSprite => lowerHeaderStatusTabSprite;
    public Sprite LowerSummaryRowSprite => lowerSummaryRowSprite;
    public Sprite LowerFeatureRowSprite => lowerFeatureRowSprite;
    public Sprite LowerMemoPanelSprite => lowerMemoPanelSprite;

    public ScrollRect GetCatalogScrollRect()
    {
        return catalogScrollRect != null ? catalogScrollRect : bottomScrollRect;
    }

    public float GetCatalogViewportWidth()
    {
        if (catalogScrollRect != null && catalogScrollRect.viewport != null)
        {
            return catalogScrollRect.viewport.rect.width;
        }

        if (bottomScrollRect != null && bottomScrollRect.viewport != null)
        {
            return bottomScrollRect.viewport.rect.width;
        }

        return 0f;
    }

    private GameUiTheme theme;
    private EquipmentCatalogUIController equipmentCatalogUiController;
    private GameMenuUIController menuUiController;
    private StaffUIController staffUiController;

    private WalletManager walletManager;
    private TimeManager timeManager;
    private GymEconomyManager economyManager;
    private GymEventManager eventManager;
    private InGameMenuManager menuManager;
    private StaffManager staffManager;
    private PlacementManager placementManager;
    private RelocationManager relocationManager;
    private MonthlySettlementManager settlementManager;

    private ScrollRect textScrollRect;
    private ScrollRect catalogScrollRect;
    private Image legacyContentPanelImage;
    private Image legacyContentPanelShadowImage;
    private int activeTabIndex;
    private string activeSignature = string.Empty;
    private bool isBuilt;

    private GameObject selectionPanel;
    private Text selectionEyebrowText;
    private Text selectionTitleText;
    private Text selectionStatusText;
    private Text selectionDetailText;
    private readonly Button[] selectionActionButtons = new Button[4];
    private readonly Text[] selectionActionLabels = new Text[4];

    private GameObject relocationPanel;
    private Text relocationTitleText;
    private Text relocationStatusText;
    private Button relocationSkipButton;
    private Text relocationSkipLabel;
    private Text floorTitleText;
    private Text floorStatusText;
    private Text leftSummaryTitleText;
    private Text leftSummaryBodyText;
    private Text leftActionTitleText;
    private Text leftActionBodyText;
    private Text rightMemoTitleText;
    private Text rightMemoBodyText;

    public void Configure(GameUiTheme uiTheme, EquipmentCatalogUIController catalogController, GameMenuUIController menuController, StaffUIController staffController)
    {
        theme = uiTheme ?? GameUiTheme.CreateDefault();
        equipmentCatalogUiController = catalogController;
        menuUiController = menuController;
        staffUiController = staffController;
    }

    public void BuildUi(Transform canvasRoot)
    {
        if (isBuilt)
        {
            return;
        }

        theme ??= GameUiTheme.CreateDefault();
        EnsureHudIconSprites();

        GameObject hudRoot = GameUiFactory.CreateNode(canvasRoot, "HUDRoot");
        GameUiFactory.Stretch(hudRoot.GetComponent<RectTransform>());

        BuildAmbientFrame(hudRoot.transform);
        BuildPlayfieldFrame(hudRoot.transform);
        BuildTopHud(hudRoot.transform);
        BuildSideBoards(hudRoot.transform);
        BuildBottomHud(hudRoot.transform);
        BuildSelectionHud(hudRoot.transform);
        BuildRelocationHud(hudRoot.transform);
        CacheManagers();
        ConfigureSiblingControllers();
        BindButtons();
        RefreshHudIconDecorations();
        UpdateSpeedVisuals();
        SwitchTab(0);
        RefreshContextHud();
        isBuilt = true;
    }

    private void Awake()
    {
        theme ??= GameUiTheme.CreateDefault();
        CacheSiblingControllers();
        EnsurePrefabBindings();
        EnsureHudIconSprites();
        EnsureThemeUsesSceneFont();
        ConfigureSiblingControllers();
        ImproveExistingTextReadability();
        BindButtons();
        RefreshHudIconDecorations();
        UpdateTabVisuals(activeTabIndex);
        UpdateSpeedVisuals();
    }

    private void Start()
    {
        CacheSiblingControllers();
        EnsurePrefabBindings();
        EnsureHudIconSprites();
        EnsureThemeUsesSceneFont();
        ConfigureSiblingControllers();
        ImproveExistingTextReadability();
        CacheManagers();
        BindButtons();
        RefreshHudIconDecorations();
        RefreshTopHud();
        RefreshStructuredScreen(true);
        RefreshContextHud();
    }

    private void Update()
    {
        CacheManagers();
        RefreshTopHud();
        RefreshContextHud();

        if (activeTabIndex == 1)
        {
            equipmentCatalogUiController?.Tick(true);
            return;
        }

        equipmentCatalogUiController?.Tick(false);
        if (InGameMenuManager.IsMenuOpen)
        {
            return;
        }

        RefreshStructuredScreen(false);
    }

    private void CacheManagers()
    {
        walletManager ??= FindFirstObjectByType<WalletManager>();
        timeManager ??= FindFirstObjectByType<TimeManager>();
        economyManager ??= FindFirstObjectByType<GymEconomyManager>();
        eventManager ??= FindFirstObjectByType<GymEventManager>();
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();
        staffManager ??= FindFirstObjectByType<StaffManager>();
        placementManager ??= FindFirstObjectByType<PlacementManager>();
        relocationManager ??= FindFirstObjectByType<RelocationManager>();
        settlementManager ??= FindFirstObjectByType<MonthlySettlementManager>();
    }

    private void CacheSiblingControllers()
    {
        equipmentCatalogUiController ??= GetComponentInChildren<EquipmentCatalogUIController>(true);
        menuUiController ??= GetComponentInChildren<GameMenuUIController>(true);
        staffUiController ??= GetComponentInChildren<StaffUIController>(true);

        equipmentCatalogUiController ??= FindFirstObjectByType<EquipmentCatalogUIController>(FindObjectsInactive.Include);
        menuUiController ??= FindFirstObjectByType<GameMenuUIController>(FindObjectsInactive.Include);
        staffUiController ??= FindFirstObjectByType<StaffUIController>(FindObjectsInactive.Include);
    }

    private void ConfigureSiblingControllers()
    {
        equipmentCatalogUiController?.Configure(theme, this);
        menuUiController?.Configure(theme);
        staffUiController?.Configure(theme);
    }

    private void EnsurePrefabBindings()
    {
        if (bottomScrollRect == null)
        {
            Transform scrollTransform = FindDeepChild(transform, "ContentScrollView");
            if (scrollTransform != null)
            {
                bottomScrollRect = scrollTransform.GetComponent<ScrollRect>();
            }
        }

        textScrollRect ??= bottomScrollRect;

        if (textRoot == null)
        {
            Transform textRootTransform = FindDeepChild(transform, "TextRoot");
            if (textRootTransform != null)
            {
                textRoot = textRootTransform.gameObject;
            }
        }

        if (catalogRoot == null)
        {
            Transform catalogRootTransform = FindDeepChild(transform, "CatalogRoot");
            if (catalogRootTransform != null)
            {
                catalogRoot = catalogRootTransform.gameObject;
            }
        }

        if (catalogScrollRect == null && bottomScrollRect != null && bottomScrollRect.viewport != null && textRoot != null && catalogRoot != null)
        {
            bool usesSharedViewport =
                textRoot.transform.IsChildOf(bottomScrollRect.viewport) &&
                catalogRoot.transform.IsChildOf(bottomScrollRect.viewport);

            if (usesSharedViewport)
            {
                textScrollRect = bottomScrollRect;
                catalogScrollRect = bottomScrollRect;
            }
        }

        EnsureLegacyBottomPanelVisuals();
        EnsureScrollContentLayout(textRoot);
        EnsureScrollContentLayout(catalogRoot);
        RebindBottomScrollContent(activeTabIndex == 1 ? ResolveCatalogScrollRoot() : textRoot);
    }

    private void EnsureThemeUsesSceneFont()
    {
        Font preferredFont = ResolveSceneFont();
        if (preferredFont == null)
        {
            return;
        }

        if (theme == null || theme.Font != preferredFont)
        {
            theme = new GameUiTheme(preferredFont);
        }
    }

    private Font ResolveSceneFont()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        Font fallback = null;
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null || text.font == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = text.font;
            }

            if (!string.IsNullOrEmpty(text.font.name) && text.font.name.ToLowerInvariant().Contains("neodgm"))
            {
                return text.font;
            }
        }

        return fallback;
    }

    private void ImproveExistingTextReadability()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            if (text.fontSize < 14)
            {
                text.fontSize = 14;
            }

            GameUiFactory.EnhanceTextReadability(text, theme);
        }
    }

    public void EditorRebuildLowerPanelPreview()
    {
        theme ??= GameUiTheme.CreateDefault();
        CacheSiblingControllers();
        EnsurePrefabBindings();
        EnsureThemeUsesSceneFont();

        if (catalogRoot != null)
        {
            equipmentCatalogUiController?.Configure(theme, this);
            equipmentCatalogUiController?.EditorBuildPreviewCatalog();
            catalogRoot.SetActive(false);
        }

        if (textRoot != null)
        {
            GameUiFactory.ClearChildren(textRoot.transform);
            EnsureScrollContentLayout(textRoot);
            BuildOperationsPreviewScreen();
            textRoot.SetActive(true);
        }

        activeTabIndex = 0;
        activeSignature = string.Empty;
        UpdateLegacyBottomPanelBackdrop();
        UpdateTabVisuals(0);
        UpdateSpeedVisuals();
    }

    private void EnsureLegacyBottomPanelVisuals()
    {
        Transform contentPanelTransform = FindDeepChild(transform, "ContentPanel");
        if (contentPanelTransform != null)
        {
            legacyContentPanelImage = contentPanelTransform.GetComponent<Image>();
            if (legacyContentPanelImage != null)
            {
                legacyContentPanelImage.sprite = lowerContentBaseSprite != null ? lowerContentBaseSprite : panelSprite;
                legacyContentPanelImage.type = Image.Type.Simple;
                legacyContentPanelImage.color = Color.white;
                legacyContentPanelImage.raycastTarget = false;
            }

            Transform surfaceTransform = contentPanelTransform.Find("Surface");
            if (surfaceTransform != null)
            {
                surfaceTransform.gameObject.SetActive(false);
            }
        }

        Transform shadowTransform = FindDeepChild(transform, "ContentPanelShadow");
        if (shadowTransform != null)
        {
            shadowTransform.gameObject.SetActive(true);
            legacyContentPanelShadowImage = shadowTransform.GetComponent<Image>();
            if (legacyContentPanelShadowImage != null)
            {
                legacyContentPanelShadowImage.sprite = lowerContentBaseSprite != null ? lowerContentBaseSprite : panelSprite;
                legacyContentPanelShadowImage.type = Image.Type.Simple;
                legacyContentPanelShadowImage.color = new Color(0.16f, 0.20f, 0.29f, 0.18f);
                legacyContentPanelShadowImage.raycastTarget = false;
            }
        }

        UpdateLegacyBottomPanelBackdrop();
    }

    private void UpdateLegacyBottomPanelBackdrop()
    {
        bool useScrollingComposedBase = activeTabIndex == 0;

        if (legacyContentPanelImage != null)
        {
            legacyContentPanelImage.color = useScrollingComposedBase
                ? new Color(1f, 1f, 1f, 0f)
                : Color.white;
        }

        if (legacyContentPanelShadowImage != null)
        {
            legacyContentPanelShadowImage.color = useScrollingComposedBase
                ? new Color(0.16f, 0.20f, 0.29f, 0.08f)
                : new Color(0.16f, 0.20f, 0.29f, 0.18f);
        }
    }

    private void BuildOperationsPreviewScreen()
    {
        EnsureScrollContentLayout(textRoot);

        BuildLegacyOperationsSurface(
            textRoot.transform,
            "\uC6B4\uC601 \uD604\uD669",
            "\ub9cc\uc871\ub3c4: \uc88b\uc74c",
            new[]
            {
                "\ud68c\uc6d0\uc218",
                "\uc218\uc775",
                "\ub9cc\uc871\ub3c4",
                "\ud3c9\ud310"
            },
            new[]
            {
                "56\uba85",
                "4,200 G",
                "85%",
                "Lv.2"
            },
            "\uc77c\uc77c \ubaa9\ud45c",
            "40\uba85 \ubc29\ubb38",
            "(\ud604\uc7ac 22\uba85)",
            "\uc2dc\uc124 \uc810\uac80",
            "\uae30\uad6c \uccad\uc18c \uc911",
            "(\uc9c4\ud589\uc911)",
            new[]
            {
                "\uc0c8\ub85c\uc6b4 PT \uc218\uc5c5 \uc900\ube44 \uc911",
                "\ud68c\uc6d0 \ud53c\ub4dc\ubc31: \ub108\ubb34 \uc88b\uc544\uc694!",
                "\uc6b4\ub3d9 \uae30\uad6c \uc218\ub9ac \uc644\ub8cc"
            });
    }

    private void EnsureScrollContentLayout(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return;
        }

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(rootObject);
        layout.padding = new RectOffset(14, 18, 12, 12);
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = GameUiFactory.GetOrAdd<ContentSizeFitter>(rootObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private Text FindTextByName(string objectName)
    {
        Transform match = FindDeepChild(transform, objectName);
        return match != null ? match.GetComponent<Text>() : null;
    }

    private static Transform FindDeepChild(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void BuildAmbientFrame(Transform parent)
    {
        CreateAmbientBand(parent, "WallBandA", new Vector2(1400f, 230f), new Vector2(0f, -120f), 0f, new Color(0.12f, 0.18f, 0.28f, 0.72f));
        CreateAmbientBand(parent, "WallBandB", new Vector2(1400f, 190f), new Vector2(0f, -320f), 0f, new Color(0.17f, 0.24f, 0.37f, 0.74f));
        CreateAmbientBand(parent, "WallBandC", new Vector2(1400f, 170f), new Vector2(0f, -500f), 0f, new Color(0.20f, 0.28f, 0.42f, 0.76f));
        CreateAmbientBand(parent, "FloorBandA", new Vector2(1400f, 320f), new Vector2(0f, -1180f), 0f, new Color(0.16f, 0.20f, 0.28f, 0.82f));
        CreateAmbientBand(parent, "FloorBandB", new Vector2(1400f, 260f), new Vector2(0f, -1450f), 0f, new Color(0.13f, 0.17f, 0.24f, 0.88f));

        GameObject rail = GameUiFactory.CreateNode(parent, "MidRail", typeof(CanvasRenderer), typeof(Image));
        Image railImage = rail.GetComponent<Image>();
        railImage.color = new Color(0.73f, 0.56f, 0.32f, 0.86f);
        railImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(rail.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -510f), new Vector2(0f, 20f));

        GameObject topShade = GameUiFactory.CreateNode(parent, "TopShade", typeof(CanvasRenderer), typeof(Image));
        Image topShadeImage = topShade.GetComponent<Image>();
        topShadeImage.color = new Color(0.03f, 0.05f, 0.08f, 0.24f);
        topShadeImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(topShade.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 240f));

        GameObject bottomShade = GameUiFactory.CreateNode(parent, "BottomShade", typeof(CanvasRenderer), typeof(Image));
        Image bottomShadeImage = bottomShade.GetComponent<Image>();
        bottomShadeImage.color = new Color(0.03f, 0.05f, 0.08f, 0.20f);
        bottomShadeImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(bottomShade.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 520f));
    }

    private void BuildPlayfieldFrame(Transform parent)
    {
        GameObject frame = GameUiFactory.CreateNode(parent, "PlayfieldFrame", typeof(CanvasRenderer), typeof(Image));
        Image frameFill = frame.GetComponent<Image>();
        frameFill.color = new Color(0.05f, 0.08f, 0.12f, 0.08f);
        frameFill.raycastTarget = false;
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(820f, 960f));

        CreateFrameEdge(frame.transform, "Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 18f), theme.Outline);
        CreateFrameEdge(frame.transform, "Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 18f), theme.Outline);
        CreateFrameEdge(frame.transform, "Left", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(18f, 0f), theme.Outline);
        CreateFrameEdge(frame.transform, "Right", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(18f, 0f), theme.Outline);
        CreateFrameEdge(frame.transform, "InnerTop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-54f, 8f), new Color(0.73f, 0.56f, 0.32f, 0.65f));
        CreateFrameEdge(frame.transform, "InnerBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-54f, 8f), new Color(0.73f, 0.56f, 0.32f, 0.35f));

        RectTransform signContent;
        GameObject sign = GameUiFactory.CreatePanel(parent, "FloorSign", theme, theme.PanelFill, out signContent, 14f);
        GameUiFactory.SetAnchoredRect(sign.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(430f, 96f));

        floorTitleText = GameUiFactory.CreateText(signContent, "Title", theme, 34, theme.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(floorTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 36f));
        floorTitleText.text = "\uc6b4\uc601 \ud50c\ub85c\uc5b4";

        floorStatusText = GameUiFactory.CreateText(signContent, "Body", theme, 18, theme.MutedInk, TextAnchor.LowerCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(floorStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(0f, -42f));
        floorStatusText.text = "\ud50c\ub808\uc774 \uc900\ube44 \uc911";
    }

    private void BuildSideBoards(Transform parent)
    {
        RectTransform leftSummaryContent;
        GameObject leftSummary = GameUiFactory.CreatePanel(parent, "LeftSummaryBoard", theme, theme.PanelFill, out leftSummaryContent, 18f);
        GameUiFactory.SetAnchoredRect(leftSummary.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -236f), new Vector2(284f, 304f));

        leftSummaryTitleText = CreateBoardTitle(leftSummaryContent, "\uC624\uB298\uC758 \uC6B4\uC601");
        leftSummaryBodyText = CreateBoardBody(leftSummaryContent);
        SetTextIfChanged(leftSummaryTitleText, "\uC624\uB298\uC758 \uC6B4\uC601");

        RectTransform leftActionContent;
        GameObject leftAction = GameUiFactory.CreatePanel(parent, "LeftActionBoard", theme, theme.PanelFillAlt, out leftActionContent, 18f);
        GameUiFactory.SetAnchoredRect(leftAction.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -560f), new Vector2(284f, 254f));

        leftActionTitleText = CreateBoardTitle(leftActionContent, "\uC6B4\uC601 \uCCB4\uD06C");
        leftActionBodyText = CreateBoardBody(leftActionContent);
        SetTextIfChanged(leftActionTitleText, "\uC6B4\uC601 \uCCB4\uD06C");

        RectTransform rightMemoContent;
        GameObject rightMemo = GameUiFactory.CreatePanel(parent, "RightMemoBoard", theme, theme.PanelFill, out rightMemoContent, 18f);
        GameUiFactory.SetAnchoredRect(rightMemo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -560f), new Vector2(316f, 254f));

        rightMemoTitleText = CreateBoardTitle(rightMemoContent, "\uD604\uC7A5 \uBA54\uBAA8");
        rightMemoBodyText = CreateBoardBody(rightMemoContent);
        SetTextIfChanged(rightMemoTitleText, "\uD604\uC7A5 \uBA54\uBAA8");
    }

    private Text CreateBoardTitle(RectTransform parent, string title)
    {
        Text titleText = GameUiFactory.CreateText(parent, "Title", theme, 24, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(0f, 28f));
        titleText.text = title;
        return titleText;
    }

    private Text CreateBoardBody(RectTransform parent)
    {
        Text bodyText = GameUiFactory.CreateText(parent, "Body", theme, 18, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -38f), new Vector2(0f, -42f));
        bodyText.text = "--";
        return bodyText;
    }

    private void CreateFrameEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject edge = GameUiFactory.CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image edgeImage = edge.GetComponent<Image>();
        edgeImage.color = color;
        edgeImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(edge.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
    }

    private void BuildTopHud(Transform parent)
    {
        RectTransform statusContent;
        GameObject statusPanel = GameUiFactory.CreatePanel(parent, "StatusCluster", theme, theme.PanelFill, out statusContent, 18f);
        RectTransform statusRect = statusPanel.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(statusRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -20f), new Vector2(610f, 188f));

        GameObject statusChip = GameUiFactory.CreateStateChip(statusContent, "StatusChip", theme, "\uD5EC\uC2A4\uC7A5 \uD604\uD669", GameUiTone.Surface, out Text statusChipLabel);
        statusChipLabel.fontSize = 18;
        statusChipLabel.text = "\uD5EC\uC2A4\uC7A5 \uD604\uD669";
        GameUiFactory.SetAnchoredRect(statusChip.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(182f, 44f));

        dateText = CreateTopMetric(statusContent, "DateMetric", "\uB0A0\uC9DC", new Vector2(0f, 0.52f), new Vector2(0.28f, 1f), theme.AccentAlt, 24, iconClock);
        cashText = CreateTopMetric(statusContent, "CashMetric", "\uC790\uAE08", new Vector2(0.30f, 0.52f), new Vector2(1f, 1f), theme.Accent, 32, iconCash);
        starCoinText = CreateTopMetric(statusContent, "StarMetric", "\uBCC4\uCF54\uC778", new Vector2(0f, 0f), new Vector2(0.28f, 0.46f), theme.Warning, 22, iconStarCoin);
        timeText = CreateTopMetric(statusContent, "SpeedMetric", "\uC18D\uB3C4", new Vector2(0.30f, 0f), new Vector2(1f, 0.46f), theme.AccentAlt, 28, ResolveSpeedMetricIcon());






        RectTransform controlContent;
        GameObject controlPanel = GameUiFactory.CreatePanel(parent, "ControlRail", theme, theme.PanelFillAlt, out controlContent, 16f);
        RectTransform controlRect = controlPanel.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(controlRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -20f), new Vector2(390f, 188f));

        Text controlHeader = GameUiFactory.CreateText(controlContent, "ControlHeader", theme, 18, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(controlHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(0f, 24f));
        controlHeader.text = "\uBE60\uB978 \uBA54\uB274";


        staffButton = CreateTopButton(controlContent, "StaffButton", "\uC9C1\uC6D0", GameUiTone.AccentAlt, new Vector2(0f, 0.52f), new Vector2(0.48f, 0.88f), iconUser);
        menuButton = CreateTopButton(controlContent, "MenuButton", "\uBA54\uB274", GameUiTone.Warning, new Vector2(0.52f, 0.52f), new Vector2(1f, 0.88f), iconMenu);
        btnPlay = CreateTopButton(controlContent, "PlayButton", "1\uBC30\uC18D", GameUiTone.Surface, new Vector2(0f, 0f), new Vector2(0.31f, 0.32f), iconPlay);
        btnFast = CreateTopButton(controlContent, "FastButton", "2\uBC30\uC18D", GameUiTone.Surface, new Vector2(0.345f, 0f), new Vector2(0.655f, 0.32f), iconFastForward);
        btnVeryFast = CreateTopButton(controlContent, "VeryFastButton", "4\uBC30\uC18D", GameUiTone.Surface, new Vector2(0.69f, 0f), new Vector2(1f, 0.32f), iconFastForward);
    }










    private Text CreateTopMetric(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color accent, int valueFontSize, Sprite iconSprite)
    {
        RectTransform contentRoot;
        GameObject panel = GameUiFactory.CreateCard(parent, name, theme, out contentRoot, 0f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(panelRect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
        SetFillColor(panel, theme.PanelFill);

        GameObject accentBar = GameUiFactory.CreateNode(panel.transform, "AccentBar", typeof(CanvasRenderer), typeof(Image));
        Image accentImage = accentBar.GetComponent<Image>();
        accentImage.color = accent;
        accentImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(accentBar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(12f, 0f));

        Text labelText = GameUiFactory.CreateText(contentRoot, "Label", theme, 16, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 20f));
        labelText.text = label;

        Text valueText = GameUiFactory.CreateText(contentRoot, "Value", theme, valueFontSize, theme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(0f, -24f));
        valueText.text = "--";
        ApplyMetricIcon(valueText, iconSprite, "MetricIcon");
        return valueText;
    }

    private Button CreateTopButton(Transform parent, string name, string label, GameUiTone tone, Vector2 anchorMin, Vector2 anchorMax, Sprite iconSprite)
    {
        Button button = GameUiFactory.CreateButton(parent, name, theme, label, tone, out Text labelText, 8f);
        RectTransform rect = button.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
        labelText.fontSize = 22;
        ApplyButtonIcon(button, iconSprite, 22f, 10f, 8f);
        return button;
    }

    private void BuildBottomHud(Transform parent)
    {
        RectTransform stageContent;
        GameObject stagePanel = GameUiFactory.CreatePanel(parent, "ContentStage", theme, theme.PanelFill, out stageContent, 22f);
        RectTransform stageRect = stagePanel.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(stageRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 164f), new Vector2(1000f, 430f));

        GameObject stageAccent = GameUiFactory.CreateNode(stagePanel.transform, "StageAccent", typeof(CanvasRenderer), typeof(Image));
        Image stageAccentImage = stageAccent.GetComponent<Image>();
        stageAccentImage.color = new Color(0.23f, 0.48f, 0.79f, 0.94f);
        stageAccentImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(stageAccent.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(18f, 0f));

        bottomPanelTitle = GameUiFactory.CreateText(stageContent, "SectionTitle", theme, 34, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(bottomPanelTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(-240f, 42f));
        SetTextIfChanged(bottomPanelTitle, "\uC6B4\uC601 \uD604\uD669");

        bottomPanelContent = GameUiFactory.CreateText(stageContent, "SectionSummary", theme, 18, theme.MutedInk, TextAnchor.UpperRight, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(bottomPanelContent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(0f, 30f));
        SetTextIfChanged(bottomPanelContent, "\ud68c\uc6d0 \ud750\ub984\uacfc \uc2dc\uc124 \uc0c1\ud0dc\ub97c \ud655\uc778\ud558\uc138\uc694");

        textScrollRect = GameUiFactory.CreateScrollView(stageContent, "TextScroll", theme, out RectTransform textRootRect);
        GameUiFactory.SetAnchoredRect(textScrollRect.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(0f, -88f));
        textRoot = textRootRect.gameObject;

        catalogScrollRect = GameUiFactory.CreateScrollView(stageContent, "CatalogScroll", theme, out RectTransform catalogRootRect);
        GameUiFactory.SetAnchoredRect(catalogScrollRect.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(0f, -88f));
        catalogRoot = catalogRootRect.gameObject;
        catalogScrollRect.gameObject.SetActive(false);

        RectTransform dockContent;
        GameObject dock = GameUiFactory.CreatePanel(parent, "NavDock", theme, theme.PanelFillAlt, out dockContent, 14f);
        RectTransform dockRect = dock.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(dockRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1000f, 124f));

        Text dockLabel = GameUiFactory.CreateText(dockContent, "DockLabel", theme, 16, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(dockLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(0f, 18f));
        dockLabel.text = "\uba54\uc778 \ud0ed";

        RectTransform navRow = GameUiFactory.CreateNode(dockContent, "NavRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(navRow, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(0f, -34f));
        HorizontalLayoutGroup navLayout = navRow.GetComponent<HorizontalLayoutGroup>();
        navLayout.spacing = 12f;
        navLayout.childControlWidth = true;
        navLayout.childControlHeight = true;
        navLayout.childForceExpandWidth = true;
        navLayout.childForceExpandHeight = false;
        navLayout.childAlignment = TextAnchor.MiddleCenter;

        operateTabBtn = CreateBottomTab(navRow, "OperateTab", "\uC6B4\uC601", iconClipboard);
        placementTabBtn = CreateBottomTab(navRow, "PlacementTab", "\uC124\uCE58", iconHammer);
        economyTabBtn = CreateBottomTab(navRow, "EconomyTab", "\uACBD\uC81C", iconGraph);
        reviewTabBtn = CreateBottomTab(navRow, "ReviewTab", "\uB9AC\uBDF0", iconStar);

        bottomScrollRect = textScrollRect;
    }

    private Button CreateBottomTab(Transform parent, string name, string label, Sprite iconSprite)
    {
        Button button = GameUiFactory.CreateButton(parent, name, theme, label, GameUiTone.Surface, out Text labelText, 12f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(button.gameObject);
        layout.preferredHeight = 58f;
        layout.flexibleWidth = 1f;
        labelText.fontSize = 18;
        GameUiFactory.ApplyMinimalHudText(labelText);
        ConfigureSingleLineHudText(labelText, false);
        ApplyButtonIcon(button, iconSprite, 20f, 12f, 8f);
        return button;
    }

    private void BuildSelectionHud(Transform parent)
    {
        RectTransform contentRoot;
        selectionPanel = GameUiFactory.CreatePanel(parent, "SelectionPanel", theme, theme.PanelFillAlt, out contentRoot, 18f);
        RectTransform rect = selectionPanel.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -228f), new Vector2(378f, 318f));

        selectionEyebrowText = GameUiFactory.CreateText(contentRoot, "Eyebrow", theme, 18, theme.Warning, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(selectionEyebrowText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(0f, 22f));

        selectionTitleText = GameUiFactory.CreateText(contentRoot, "Title", theme, 28, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(selectionTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -32f), new Vector2(0f, 34f));

        selectionStatusText = GameUiFactory.CreateText(contentRoot, "Status", theme, 18, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(selectionStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -74f), new Vector2(0f, 48f));

        selectionDetailText = GameUiFactory.CreateText(contentRoot, "Detail", theme, 16, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(selectionDetailText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -128f), new Vector2(0f, 34f));

        selectionActionButtons[0] = CreateSelectionActionButton(contentRoot, "PrimaryAction", new Vector2(0f, 0.20f), new Vector2(0.48f, 0.40f), out Text primaryLabel);
        selectionActionButtons[1] = CreateSelectionActionButton(contentRoot, "SecondaryAction", new Vector2(0.52f, 0.20f), new Vector2(1f, 0.40f), out Text secondaryLabel);
        selectionActionButtons[2] = CreateSelectionActionButton(contentRoot, "TertiaryAction", new Vector2(0f, 0f), new Vector2(0.48f, 0.18f), out Text tertiaryLabel);
        selectionActionButtons[3] = CreateSelectionActionButton(contentRoot, "QuaternaryAction", new Vector2(0.52f, 0f), new Vector2(1f, 0.18f), out Text quaternaryLabel);
        selectionActionLabels[0] = primaryLabel;
        selectionActionLabels[1] = secondaryLabel;
        selectionActionLabels[2] = tertiaryLabel;
        selectionActionLabels[3] = quaternaryLabel;

        selectionPanel.SetActive(false);
    }

    private Button CreateSelectionActionButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out Text labelText)
    {
        Button button = GameUiFactory.CreateButton(parent, name, theme, "--", GameUiTone.Surface, out labelText, 8f);
        RectTransform rect = button.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
        labelText.fontSize = 18;
        return button;
    }

    private void BuildRelocationHud(Transform parent)
    {
        RectTransform contentRoot;
        relocationPanel = GameUiFactory.CreatePanel(parent, "RelocationPanel", theme, theme.PanelFillAlt, out contentRoot, 16f);
        RectTransform rect = relocationPanel.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(470f, 110f));

        relocationTitleText = GameUiFactory.CreateText(contentRoot, "Title", theme, 24, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(relocationTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(-156f, 28f));

        relocationStatusText = GameUiFactory.CreateText(contentRoot, "Status", theme, 18, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(relocationStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(-156f, 28f));

        relocationSkipButton = GameUiFactory.CreateButton(contentRoot, "SkipButton", theme, "\uC989\uC2DC \uC644\uB8CC", GameUiTone.Warning, out relocationSkipLabel, 8f);
        GameUiFactory.SetAnchoredRect(relocationSkipButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(138f, 58f));
        relocationSkipLabel.fontSize = 18;

        relocationPanel.SetActive(false);
    }

    private void RefreshContextHud()
    {
        RefreshPlayfieldBoards();
        RefreshSelectionHud();
        RefreshRelocationHud();
    }

    private void RefreshPlayfieldBoards()
    {
        if (economyManager == null)
        {
            return;
        }

        string modeTitle = BuildPlayModeManager.IsBuildMode ? "\uc124\uce58 \ud50c\ub85c\uc5b4" : "\uc6b4\uc601 \ud50c\ub85c\uc5b4";
        string modeBody = $"{economyManager.GetCurrentLocationPreviewLabel()}  |  {economyManager.GetOperationStatusLabel()}";
        SetTextIfChanged(floorTitleText, modeTitle);
        SetTextIfChanged(floorStatusText, modeBody);

        int hiredCount = staffManager != null ? staffManager.HiredStaff.Count : 0;
        int applicantCount = staffManager != null ? staffManager.AvailableApplicants.Count : 0;
        string summaryBody =
            $"\ubd80\uc9c0  {economyManager.GetCurrentLocationPreviewLabel()}\n" +
            $"\ud68c\uc6d0  {economyManager.GetActiveMemberCount()}\uba85   \ub300\uae30  {economyManager.GetWaitingCustomersCount()}\uba85\n" +
            $"\uc9c1\uc6d0  {hiredCount}\uba85   \ud2b8\ub808\uc774\ub108  {economyManager.GetCurrentTrainerCount()}\uba85\n" +
            $"\uae30\uad6c  {economyManager.GetMachineCountEstimate()}\ub300   \uc218\uc6a9  {economyManager.GetCurrentCapacityEstimate()}\n" +
            $"\uccad\uacb0  {economyManager.GetCleanliness01() * 100f:0}%   \ud3c9\ud310  {economyManager.GetCurrentReputationStars():0.0}\uc810";
        SetTextIfChanged(leftSummaryTitleText, BuildPlayModeManager.IsBuildMode ? "\ud604\uc7a5 \uc694\uc57d" : "\uc624\ub298\uc758 \uc6b4\uc601");
        SetTextIfChanged(leftSummaryBodyText, summaryBody);

        SetTextIfChanged(leftActionTitleText, GetActionBoardTitle());
        SetTextIfChanged(leftActionBodyText, BuildActionBoardBody());
        SetTextIfChanged(rightMemoTitleText, GetMemoBoardTitle());
        SetTextIfChanged(rightMemoBodyText, BuildMemoBoardBody());
    }

    private string GetActionBoardTitle()
    {
        switch (activeTabIndex)
        {
            case 1:
                return "\uc124\uce58 \uc900\ube44";
            case 2:
                return "\uc218\uc775 \ud750\ub984";
            case 3:
                return "\ub9ac\ubdf0 \uccb4\ud06c";
            default:
                return "\uc6b4\uc601 \uccb4\ud06c";
        }
    }

    private string BuildActionBoardBody()
    {
        if (economyManager == null)
        {
            return "\ub370\uc774\ud130\ub97c \ubd88\ub7ec\uc624\ub294 \uc911\uc785\ub2c8\ub2e4.";
        }

        if (activeTabIndex == 1)
        {
            EquipmentDefinition selected = EquipmentSelectionState.CurrentDefinition;
            if (selected == null)
            {
                return "\uc544\ub798 \uce74\ud0c8\ub85c\uadf8\uc5d0\uc11c \uae30\uad6c\ub97c \uace0\ub978 \ub4a4\n\ud50c\ub85c\uc5b4 \ube48 \uce78\uc744 \ub20c\ub7ec \uc124\uce58\ud558\uc138\uc694.\n\uc124\uce58 \ubaa8\ub4dc\uac00 \uc790\ub3d9\uc73c\ub85c \uc720\uc9c0\ub429\ub2c8\ub2e4.";
            }

            return $"{selected.DisplayName}\n" +
                   $"\uc124\uce58\ube44 {selected.InstallCost:N0} G   \ud06c\uae30 {selected.Width}x{selected.Height}\uce78\n" +
                   $"\ub4f1\uae09 {selected.BrandTierLabel}\n" +
                   "\ube48 \uce78\uc744 \ub20c\ub7ec \ubc30\uce58\ud558\uace0, \uc774\ubbf8 \ub193\uc778 \uae30\uad6c\ub294 \uc120\ud0dd \ud6c4 \uc774\ub3d9\ud558\uac70\ub098 \ucca0\uac70\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4.";
        }

        if (activeTabIndex == 2)
        {
            return $"\ud68c\uc6d0\uad8c  {economyManager.GetDailyMembershipRevenue():N0} G\n" +
                   $"PT  {economyManager.GetDailyPtRevenue():N0} G\n" +
                   $"\ubd80\uac00 \uc218\uc785  {economyManager.GetDailyAncillaryRevenue():N0} G\n" +
                   $"\uc6b4\uc601\ube44  {economyManager.GetDailyVariableCost():N0} G";
        }

        if (activeTabIndex == 3)
        {
            return $"\ud3c9\uc810  {economyManager.GetCurrentReputationStars():0.0}\uc810\n" +
                   $"\uc804\uc77c \ud3c9\uc810  {economyManager.GetLastDailyReviewStars():0.0}\uc810\n" +
                   $"\ud3c9\uade0 \ub300\uae30  {economyManager.GetAverageWaitSeconds():0.0}\ucd08\n" +
                   $"\ucd94\uc138  {economyManager.GetReviewTrendLabel()}";
        }

        int hiredCount = staffManager != null ? staffManager.HiredStaff.Count : 0;
        int applicantCount = staffManager != null ? staffManager.AvailableApplicants.Count : 0;
        return $"\uc608\uc0c1 \uc21c\uc774\uc775  {economyManager.GetPreviewDailyNetRevenue():N0} G\n" +
               $"\ub9cc\uc871\ub3c4  {economyManager.GetSatisfaction01() * 100f:0}%\n" +
               $"\uc9c1\uc6d0  {hiredCount}\uba85   \uc9c0\uc6d0\uc790  {applicantCount}\uba85\n" +
               $"\ube0c\ub79c\ub4dc  {economyManager.GetAverageBrandLabel()}";
    }

    private string GetMemoBoardTitle()
    {
        if (activeTabIndex == 1 && EquipmentSelectionState.CurrentDefinition != null)
        {
            return "\uc120\ud0dd \uae30\uad6c";
        }

        return "\ud604\uc7a5 \uba54\ubaa8";
    }

    private string BuildMemoBoardBody()
    {
        if (activeTabIndex == 1 && EquipmentSelectionState.CurrentDefinition != null)
        {
            EquipmentDefinition selected = EquipmentSelectionState.CurrentDefinition;
            return $"{selected.DisplayName}\n" +
                   $"\ubc30\uce58 \ud6c4\uc5d0\ub294 \uc120\ud0dd \ud328\ub110\uc5d0\uc11c \uc774\ub3d9, \ucca0\uac70, \uc218\ub9ac\uae4c\uc9c0 \ubc14\ub85c \ucc98\ub9ac\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4.\n" +
                   $"\ud604\uc7ac \ubaa8\ub4dc  {(BuildPlayModeManager.IsBuildMode ? "\uc124\uce58 \uc911" : "\uc6b4\uc601 \uc911")}";
        }

        if (activeTabIndex == 3 && economyManager != null)
        {
            IReadOnlyList<GymEconomyManager.CustomerReview> reviews = economyManager.GetRecentReviews();
            if (reviews.Count > 0)
            {
                GymEconomyManager.CustomerReview latest = reviews[reviews.Count - 1];
                string author = string.IsNullOrEmpty(latest.authorName) ? "\ud68c\uc6d0" : latest.authorName;
                return $"{latest.stars:0.0}\uc810  {author}\n{latest.text}\n\uc791\uc131\uc77c  {latest.month:D2}/{latest.day:D2}";
            }
        }

        IReadOnlyList<string> events = eventManager != null ? eventManager.RecentEventLog : null;
        if (events == null || events.Count == 0)
        {
            return "\uc624\ub298\uc740 \ud070 \ubb38\uc81c \uc5c6\uc774 \uc6b4\uc601 \uc911\uc785\ub2c8\ub2e4.\n\uc9c1\uc6d0 \ubc84\ud2bc\uc73c\ub85c \ucc44\uc6a9 \ud604\ud669\uc744 \ud655\uc778\ud558\uace0\n\uba54\ub274 \ubc84\ud2bc\uc73c\ub85c \uc800\uc7a5\uacfc \uc774\uc0ac\ub97c \uc9c4\ud589\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4.";
        }

        StringBuilder builder = new StringBuilder(160);
        int start = Mathf.Max(0, events.Count - 3);
        for (int i = start; i < events.Count; i++)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append("\u2022 ").Append(events[i]);
        }

        return builder.ToString();
    }

    private void RefreshSelectionHud()
    {
        if (selectionPanel == null)
        {
            return;
        }

        if (InGameMenuManager.IsMenuOpen || !BuildPlayModeManager.IsBuildMode || placementManager == null)
        {
            selectionPanel.SetActive(false);
            return;
        }

        if (!placementManager.TryGetSelectedObjectHudState(out PlacementManager.SelectedObjectHudState state))
        {
            selectionPanel.SetActive(false);
            return;
        }

        selectionPanel.SetActive(true);
        SetTextIfChanged(selectionEyebrowText, state.eyebrow);
        SetTextIfChanged(selectionTitleText, state.title);
        SetTextIfChanged(selectionStatusText, state.status);
        SetTextIfChanged(selectionDetailText, state.detail);

        ApplySelectionActionButton(0, state.primaryAction);
        ApplySelectionActionButton(1, state.secondaryAction);
        ApplySelectionActionButton(2, state.tertiaryAction);
        ApplySelectionActionButton(3, state.quaternaryAction);
    }

    private void RefreshRelocationHud()
    {
        if (relocationPanel == null)
        {
            return;
        }

        if (InGameMenuManager.IsMenuOpen || relocationManager == null || !relocationManager.TryGetActiveRelocationHudState(out RelocationManager.ActiveRelocationHudState state))
        {
            relocationPanel.SetActive(false);
            return;
        }

        relocationPanel.SetActive(true);
        SetTextIfChanged(relocationTitleText, state.title);
        SetTextIfChanged(relocationStatusText, state.status);
        SetTextIfChanged(relocationSkipLabel, state.actionLabel);

        BindButton(relocationSkipButton, () =>
        {
            relocationManager?.TrySkipRelocationWithStarCoin();
            RefreshRelocationHud();
            RefreshStructuredScreen(true);
        });

        relocationSkipButton.interactable = state.canSkip;
        GameUiTone tone = state.canSkip ? GameUiTone.Warning : GameUiTone.Surface;
        SetFillColor(relocationSkipButton.gameObject, theme.GetToneFill(tone));
        relocationSkipLabel.color = state.canSkip ? theme.GetToneInk(tone) : new Color(theme.Ink.r, theme.Ink.g, theme.Ink.b, 0.64f);
    }

    private void ApplySelectionActionButton(int index, PlacementManager.HudActionDescriptor descriptor)
    {
        Button button = selectionActionButtons[index];
        Text label = selectionActionLabels[index];
        if (button == null || label == null)
        {
            return;
        }

        bool visible = descriptor.actionId != PlacementManager.HudActionId.None && !string.IsNullOrWhiteSpace(descriptor.label);
        button.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        GameUiTone tone = GetToneForAction(descriptor.actionId);
        SetTextIfChanged(label, descriptor.label);
        button.interactable = descriptor.isEnabled;
        SetFillColor(button.gameObject, descriptor.isEnabled ? theme.GetToneFill(tone) : theme.TabIdle);
        label.color = descriptor.isEnabled ? theme.GetToneInk(tone) : new Color(theme.Ink.r, theme.Ink.g, theme.Ink.b, 0.64f);

        BindButton(button, () =>
        {
            placementManager?.ExecuteHudAction(descriptor.actionId);
            equipmentCatalogUiController?.RefreshCatalog();
            RefreshSelectionHud();
            RefreshStructuredScreen(true);
        });
    }

    private static GameUiTone GetToneForAction(PlacementManager.HudActionId actionId)
    {
        switch (actionId)
        {
            case PlacementManager.HudActionId.BeginMove:
                return GameUiTone.AccentAlt;
            case PlacementManager.HudActionId.CancelMove:
            case PlacementManager.HudActionId.Sell:
                return GameUiTone.Danger;
            case PlacementManager.HudActionId.Repair:
            case PlacementManager.HudActionId.SkipConstruction:
                return GameUiTone.Warning;
            default:
                return GameUiTone.Surface;
        }
    }

    private void CreateAmbientBand(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, float rotation, Color color)
    {
        GameObject node = GameUiFactory.CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image image = node.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = node.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, size);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void SetFillColor(GameObject surface, Color fillColor)
    {
        if (surface == null)
        {
            return;
        }

        Transform fill = surface.transform.Find("Fill");
        if (fill == null)
        {
            return;
        }

        Image fillImage = fill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = fillColor;
        }
    }

    private void BindButtons()
    {
        BindButton(staffButton, OpenStaffPopup);
        BindButton(menuButton, OpenMenuPopup);
        BindButton(btnPlay, () => timeManager?.SetSpeedPreset(0));
        BindButton(btnFast, () => timeManager?.SetSpeedPreset(1));
        BindButton(btnVeryFast, () => timeManager?.SetSpeedPreset(2));
        BindButton(operateTabBtn, () => SwitchTab(0));
        BindButton(placementTabBtn, HandlePlacementTabPressed);
        BindButton(economyTabBtn, () => SwitchTab(2));
        BindButton(reviewTabBtn, () => SwitchTab(3));
    }

    private void OpenStaffPopup()
    {
        CacheSiblingControllers();
        ConfigureSiblingControllers();
        staffUiController?.OpenPopup();
    }

    private void OpenMenuPopup()
    {
        CacheSiblingControllers();
        ConfigureSiblingControllers();
        menuUiController?.OpenMenu();
    }

    private void HandlePlacementTabPressed()
    {
        if (activeTabIndex == 1)
        {
            SwitchTab(0);
            return;
        }

        SwitchTab(1);
    }

    private void SwitchTab(int index)
    {
        activeTabIndex = index;
        EnsurePrefabBindings();
        CacheSiblingControllers();
        ConfigureSiblingControllers();
        UpdateLegacyBottomPanelBackdrop();
        bool placement = index == 1;
        bool usesSharedBottomScroll = textScrollRect != null && textScrollRect == catalogScrollRect;

        if (placement)
        {
            BuildPlayModeManager.EnterBuildMode();
            equipmentCatalogUiController?.ForceRebuildCatalog();
            GameObject placementScrollRoot = ResolveCatalogScrollRoot();
            EnsureScrollContentLayout(placementScrollRoot);
            RebindBottomScrollContent(placementScrollRoot);
            if (usesSharedBottomScroll)
            {
                if (textScrollRect != null)
                {
                    textScrollRect.gameObject.SetActive(true);
                }
            }
            else
            {
                if (textScrollRect != null) textScrollRect.gameObject.SetActive(false);
                if (catalogScrollRect != null) catalogScrollRect.gameObject.SetActive(true);
            }

            bottomScrollRect = catalogScrollRect ?? textScrollRect;
            if (catalogRoot != null) catalogRoot.SetActive(true);
            if (textRoot != null) textRoot.SetActive(false);
            SetTextIfChanged(bottomPanelTitle, "\uC124\uCE58 \uCE74\uD0C8\uB85C\uADF8");
            SetTextIfChanged(bottomPanelContent, "\uAE30\uAD6C\uB97C \uACE0\uB978 \uB4A4 \uB9F5\uC5D0 \uBC14\uB85C \uBC30\uCE58\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4");
        }
        else
        {
            BuildPlayModeManager.EnterPlayMode();
            RebindBottomScrollContent(textRoot);
            if (usesSharedBottomScroll)
            {
                if (textScrollRect != null)
                {
                    textScrollRect.gameObject.SetActive(true);
                }
            }
            else
            {
                if (textScrollRect != null) textScrollRect.gameObject.SetActive(true);
                if (catalogScrollRect != null) catalogScrollRect.gameObject.SetActive(false);
            }

            bottomScrollRect = textScrollRect;
            if (catalogRoot != null) catalogRoot.SetActive(false);
            if (textRoot != null) textRoot.SetActive(true);
            RefreshStructuredScreen(true);
        }

        UpdateTabVisuals(index);
        UpdateSpeedVisuals();
        RefreshModeUiImmediate();

        if (bottomScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            bottomScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void RebindBottomScrollContent(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return;
        }

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            return;
        }

        if (textScrollRect != null && textScrollRect == catalogScrollRect)
        {
            if (textScrollRect.content != rootRect)
            {
                textScrollRect.content = rootRect;
            }

            bottomScrollRect = textScrollRect;
            return;
        }

        if (rootObject == textRoot && textScrollRect != null && textScrollRect.content != rootRect)
        {
            textScrollRect.content = rootRect;
        }

        bool isCatalogContent =
            rootObject == catalogRoot ||
            (catalogRoot != null && rootObject.transform.IsChildOf(catalogRoot.transform));

        if (isCatalogContent && catalogScrollRect != null && catalogScrollRect.content != rootRect)
        {
            catalogScrollRect.content = rootRect;
        }
    }

    private GameObject ResolveCatalogScrollRoot()
    {
        if (equipmentCatalogUiController != null)
        {
            GameObject scrollTarget = equipmentCatalogUiController.GetScrollContentTargetObject();
            if (scrollTarget != null)
            {
                return scrollTarget;
            }
        }

        return catalogRoot;
    }

    public void RefreshModeUiImmediate()
    {
        RefreshTopHud();
        RefreshContextHud();
    }

    private void RefreshTopHud()
    {
        if (walletManager != null)
        {
            SetTextIfChanged(cashText, $"{walletManager.CurrentCash:N0} G");
            SetTextIfChanged(starCoinText, $"{walletManager.CurrentStarCoin:N0}");
        }

        if (timeManager != null)
        {
            SetTextIfChanged(dateText, $"{timeManager.CurrentYear}/{timeManager.CurrentMonth:D2}/{timeManager.CurrentDay:D2}");
            SetTextIfChanged(timeText, BuildPlayModeManager.IsBuildMode ? "\uC124\uCE58 \uBAA8\uB4DC" : GetSpeedLabelKorean(timeManager.CurrentSpeedPresetIndex));
        }

        ApplyMetricIcon(dateText, iconClock, "MetricIcon");
        ApplyMetricIcon(cashText, iconCash, "MetricIcon");
        ApplyMetricIcon(starCoinText, iconStarCoin, "MetricIcon");
        ApplyMetricIcon(timeText, ResolveSpeedMetricIcon(), "MetricIcon");
        UpdateSpeedVisuals();
    }

    private static string GetSpeedLabelKorean(int speedIndex)
    {
        switch (speedIndex)
        {
            case 1:
                return "2\uBC30\uC18D";
            case 2:
                return "4\uBC30\uC18D";
            default:
                return "1\uBC30\uC18D";
        }
    }

    private void UpdateTabVisuals(int index)
    {
        UpdateTab(operateTabBtn, index == 0, GameUiTone.AccentAlt);
        UpdateTab(placementTabBtn, index == 1, GameUiTone.Accent);
        UpdateTab(economyTabBtn, index == 2, GameUiTone.Warning);
        UpdateTab(reviewTabBtn, index == 3, GameUiTone.Danger);
    }

    private void UpdateTab(Button button, bool active, GameUiTone tone)
    {
        if (button == null)
        {
            return;
        }

        Image buttonImage = button.GetComponent<Image>();
        Sprite nextSprite = active
            ? (bottomTabActiveSprite != null ? bottomTabActiveSprite : activeTabSprite)
            : (bottomTabInactiveSprite != null ? bottomTabInactiveSprite : inactiveTabSprite);

        if (buttonImage != null && nextSprite != null)
        {
            buttonImage.sprite = nextSprite;
            buttonImage.type = Image.Type.Simple;
            buttonImage.color = Color.white;
        }

        Transform fill = button.transform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = active ? theme.GetToneFill(tone) : theme.TabIdle;
            }
        }

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = active ? theme.BrightInk : theme.Ink;
        }

        UpdateButtonIconTint(button, Color.white);
    }

    private void UpdateSpeedVisuals()
    {
        int active = timeManager != null ? timeManager.CurrentSpeedPresetIndex : 0;
        UpdateSpeedButton(btnPlay, active == 0);
        UpdateSpeedButton(btnFast, active == 1);
        UpdateSpeedButton(btnVeryFast, active == 2);
    }

    private void UpdateSpeedButton(Button button, bool active)
    {
        if (button == null)
        {
            return;
        }

        Image buttonImage = button.GetComponent<Image>();
        Sprite nextSprite = active ? (topHudActiveButtonSprite != null ? topHudActiveButtonSprite : speedButtonSprite) : (topHudInactiveButtonSprite != null ? topHudInactiveButtonSprite : speedButtonSprite);
        if (buttonImage != null && nextSprite != null)
        {
            buttonImage.sprite = nextSprite;
            buttonImage.type = Image.Type.Simple;
            buttonImage.color = Color.white;
        }

        Transform fill = button.transform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = active ? theme.Accent : theme.TabIdle;
            }
        }

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = active ? theme.BrightInk : theme.Ink;
        }

        UpdateButtonIconTint(button, Color.white);
    }

    private void RefreshHudIconDecorations()
    {
        ApplyMetricIcon(dateText, iconClock, "MetricIcon");
        ApplyMetricIcon(cashText, iconCash, "MetricIcon");
        ApplyMetricIcon(starCoinText, iconStarCoin, "MetricIcon");
        ApplyMetricIcon(timeText, ResolveSpeedMetricIcon(), "MetricIcon");

        ApplyButtonIcon(staffButton, iconUser, 22f, 10f, 8f);
        ApplyButtonIcon(menuButton, iconMenu, 22f, 10f, 8f);
        ApplyButtonIcon(btnPlay, iconPlay, 18f, 8f, 6f);
        ApplyButtonIcon(btnFast, iconFastForward, 18f, 8f, 6f);
        ApplyButtonIcon(btnVeryFast, iconFastForward, 18f, 8f, 6f);
        ApplyButtonIcon(operateTabBtn, iconClipboard, 20f, 12f, 8f);
        ApplyButtonIcon(placementTabBtn, iconHammer, 20f, 12f, 8f);
        ApplyButtonIcon(economyTabBtn, iconGraph, 20f, 12f, 8f);
        ApplyButtonIcon(reviewTabBtn, iconStar, 20f, 12f, 8f);

        UpdateButtonIconTint(staffButton, Color.white);
        UpdateButtonIconTint(menuButton, Color.white);
    }

    private Sprite ResolveSpeedMetricIcon()
    {
        if (BuildPlayModeManager.IsBuildMode)
        {
            return iconHammer != null ? iconHammer : iconPlay;
        }

        int active = timeManager != null ? timeManager.CurrentSpeedPresetIndex : 0;
        return active == 0 ? iconPlay : (iconFastForward != null ? iconFastForward : iconPlay);
    }

    private void ApplyMetricIcon(Text valueText, Sprite iconSprite, string childName)
    {
        if (valueText == null || iconSprite == null)
        {
            return;
        }

        Transform contentRoot = valueText.transform.parent;
        if (contentRoot == null)
        {
            return;
        }

        Image iconImage = EnsureSpriteImage(contentRoot, childName, iconSprite);
        if (iconImage == null)
        {
            return;
        }

        iconImage.color = Color.white;
        GameUiFactory.SetAnchoredRect(iconImage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-2f, -4f), new Vector2(24f, 24f));
    }

    private void ApplyButtonIcon(Button button, Sprite iconSprite, float iconSize, float leftPadding, float gap)
    {
        if (button == null || iconSprite == null)
        {
            return;
        }

        Transform content = button.transform.Find("Content");
        if (content == null)
        {
            return;
        }

        Image iconImage = EnsureSpriteImage(content, "Icon", iconSprite);
        if (iconImage == null)
        {
            return;
        }

        iconImage.color = Color.white;
        GameUiFactory.SetAnchoredRect(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(leftPadding, 0f), new Vector2(iconSize, iconSize));

        Transform labelTransform = content.Find("Label");
        Text labelText = labelTransform != null ? labelTransform.GetComponent<Text>() : button.GetComponentInChildren<Text>();
        if (labelText == null)
        {
            return;
        }

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.offsetMin = new Vector2(leftPadding + iconSize + gap, 0f);
        labelRect.offsetMax = new Vector2(-6f, 0f);
        labelText.alignment = TextAnchor.MiddleLeft;
    }

    private void UpdateButtonIconTint(Button button, Color tint)
    {
        if (button == null)
        {
            return;
        }

        Transform iconTransform = button.transform.Find("Content/Icon");
        if (iconTransform == null)
        {
            return;
        }

        Image iconImage = iconTransform.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = tint;
        }
    }

    private Image EnsureSpriteImage(Transform parent, string childName, Sprite sprite)
    {
        if (parent == null || sprite == null)
        {
            return null;
        }

        Transform existing = parent.Find(childName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image == null)
        {
            GameObject iconObject = GameUiFactory.CreateNode(parent, childName, typeof(CanvasRenderer), typeof(Image));
            image = iconObject.GetComponent<Image>();
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void EnsureHudIconSprites()
    {
#if UNITY_EDITOR
        iconCash ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Cash.png");
        iconClock ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Clock.png");
        iconStarCoin ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_StarCoin.png");
        iconUser ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_User.png");
        iconMenu ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_MenuPixel.png");
        iconPlay ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Play.png");
        iconFastForward ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_FastForward.png");
        iconClipboard ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Clipboard.png");
        iconHammer ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Hammer.png");
        iconGraph ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Graph.png");
        iconStar ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Star.png");
#endif
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorSpriteFlexible(string path)
    {
        Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (single != null)
        {
            return single;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
        }

        return null;
    }
#endif

    private void RefreshStructuredScreen(bool force)
    {
        if (textRoot == null || economyManager == null || activeTabIndex == 1)
        {
            return;
        }

        string next = BuildSignature();
        if (!force && next == activeSignature)
        {
            return;
        }

        activeSignature = next;
        GameUiFactory.ClearChildren(textRoot.transform);
        UpdateLegacyBottomPanelBackdrop();

        if (activeTabIndex == 0)
        {
            SetTextIfChanged(bottomPanelTitle, "\uc6b4\uc601 \ud604\ud669");
            SetTextIfChanged(bottomPanelContent, "\ud68c\uc6d0 \ud750\ub984\uacfc \uc9c1\uc6d0 \uc0c1\ud0dc\ub97c \ud55c\ub208\uc5d0 \ubd05\ub2c8\ub2e4");
            BuildOperationsScreen();
        }
        else if (activeTabIndex == 2)
        {
            SetTextIfChanged(bottomPanelTitle, "\uacbd\uc81c \ud604\ud669");
            SetTextIfChanged(bottomPanelContent, "\uc218\uc785 \uad6c\uc870\uc640 \uc2dc\uc124 \ud6a8\uc728\uc744 \ud655\uc778\ud569\ub2c8\ub2e4");
            BuildEconomyScreen();
        }
        else
        {
            SetTextIfChanged(bottomPanelTitle, "\ub9ac\ubdf0 \ud604\ud669");
            SetTextIfChanged(bottomPanelContent, "\ud3c9\ud310 \ubcc0\ud654\uc640 \ucd5c\uadfc \ud6c4\uae30\ub97c \ud655\uc778\ud569\ub2c8\ub2e4");
            BuildReviewScreen();
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRoot.GetComponent<RectTransform>());
    }

    private string BuildSignature()
    {
        StringBuilder builder = new StringBuilder(256);
        builder.Append(activeTabIndex).Append('|');
        builder.Append(economyManager.GetActiveMemberCount()).Append('|');
        builder.Append(economyManager.GetSatisfaction01()).Append('|');
        builder.Append(economyManager.GetCleanliness01()).Append('|');
        builder.Append(economyManager.GetPreviewDailyNetRevenue()).Append('|');
        builder.Append(economyManager.GetCurrentReputationStars()).Append('|');
        builder.Append(economyManager.GetReviewTrendLabel()).Append('|');
        builder.Append(economyManager.GetRecentReviews().Count).Append('|');
        builder.Append(eventManager != null ? eventManager.RecentEventLog.Count : 0).Append('|');
        builder.Append(staffManager != null ? staffManager.HiredStaff.Count : 0).Append('|');
        builder.Append(staffManager != null ? staffManager.AvailableApplicants.Count : 0).Append('|');
        builder.Append(settlementManager != null ? settlementManager.LastSettlementText : string.Empty);
        return builder.ToString();
    }

    private void BuildOperationsScreen()
    {
        EnsureScrollContentLayout(textRoot);

        BuildLegacyOperationsSurface(
            textRoot.transform,
            "\uC6B4\uC601 \uD604\uD669",
            GetOperationsStatusTabLabel(),
            new[]
            {
                "\ud68c\uc6d0\uc218",
                "\uc218\uc775",
                "\ub9cc\uc871\ub3c4",
                "\ud3c9\ud310"
            },
            new[]
            {
                $"{economyManager.GetActiveMemberCount()}\uba85",
                $"{economyManager.GetPreviewDailyNetRevenue():N0} G",
                $"{economyManager.GetSatisfaction01() * 100f:0}%",
                $"Lv.{Mathf.Max(1, economyManager.GetCurrentPrestigeEstimate())}"
            },
            "\uc77c\uc77c \ubaa9\ud45c",
            $"{GetOperationsDailyTarget()} \uba85 \ubc29\ubb38",
            $"\ud604\uc7ac {economyManager.GetActiveMemberCount()}\uba85 \uc774\uc6a9 / PT \ub300\uc751 \uc6b0\uc120",
            "\uc2dc\uc124 \uc810\uac80",
            GetOperationsFacilityHeadline(),
            $"\uccad\uacb0 {economyManager.GetCleanliness01() * 100f:0}% / \ud3c9\uade0 \ub300\uae30 {economyManager.GetAverageWaitSeconds():0.0}\ucd08",
            BuildOperationsMemoLines());
    }

    private void BuildLegacyOperationsSurface(
        Transform parent,
        string titleLabel,
        string statusLabel,
        string[] summaryLabels,
        string[] summaryValues,
        string leftTitle,
        string leftHighlight,
        string leftDetail,
        string rightTitle,
        string rightHighlight,
        string rightDetail,
        IReadOnlyList<string> memoLines)
    {
        float panelWidth = GetLegacyPanelContentWidth() + 26f;
        Vector2 baseSize = GetLegacyModuleSizeForWidth(lowerContentBaseSprite != null ? lowerContentBaseSprite : panelSprite, panelWidth);
        float contentWidth = baseSize.x;
        float unifiedModuleWidth = Mathf.Max(680f, contentWidth - 140f);
        Vector2 summarySize = GetLegacyModuleSizeForWidth(lowerSummaryRowSprite != null ? lowerSummaryRowSprite : panelSprite, unifiedModuleWidth);
        Vector2 featureSize = GetLegacyModuleSizeForWidth(lowerFeatureRowSprite != null ? lowerFeatureRowSprite : panelSprite, unifiedModuleWidth);
        Vector2 memoSize = GetLegacyModuleSizeForWidth(lowerMemoPanelSprite != null ? lowerMemoPanelSprite : panelSprite, unifiedModuleWidth);

        float tabHeight = Mathf.Round(Mathf.Clamp(contentWidth * 0.145f, 54f, 76f));
        float headerHeight = tabHeight + 10f;
        float topInset = 18f;
        float summaryTop = topInset + headerHeight + 16f;
        float featureTop = summaryTop + summarySize.y + 18f;
        float memoTop = featureTop + featureSize.y + 18f;
        float totalHeight = Mathf.Max(baseSize.y, memoTop + memoSize.y + 24f);

        RectTransform surface = CreateLegacyAbsoluteContentCanvas(parent, "OperationsSurface", totalHeight);
        CreateLegacyAbsoluteSpriteStrip(surface, "OperationsBase", lowerContentBaseSprite != null ? lowerContentBaseSprite : panelSprite, 0f, baseSize);

        CreateLegacyHeaderStripAbsolute(surface, titleLabel, statusLabel, topInset, headerHeight);

        RectTransform summaryStrip = CreateLegacyAbsoluteSpriteStrip(surface, "OperationsSummaryStrip", lowerSummaryRowSprite != null ? lowerSummaryRowSprite : panelSprite, summaryTop, summarySize);
        PopulateLegacySummaryStrip(summaryStrip, summaryLabels, summaryValues);

        RectTransform featureStrip = CreateLegacyAbsoluteSpriteStrip(surface, "OperationsFeatureStrip", lowerFeatureRowSprite != null ? lowerFeatureRowSprite : panelSprite, featureTop, featureSize);
        CreateLegacyFeatureSlot(featureStrip.transform, "LeftFeatureSlot", 0f, 0.5f, leftTitle, leftHighlight, leftDetail);
        CreateLegacyFeatureSlot(featureStrip.transform, "RightFeatureSlot", 0.5f, 1f, rightTitle, rightHighlight, rightDetail);

        RectTransform memoPanel = CreateLegacyAbsoluteSpriteStrip(surface, "OperationsMemoPanel", lowerMemoPanelSprite != null ? lowerMemoPanelSprite : panelSprite, memoTop, memoSize);
        PopulateLegacyMemoPanel(memoPanel, memoLines);
    }

    private RectTransform CreateLegacyAbsoluteContentCanvas(Transform parent, string name, float height)
    {
        GameObject root = GameUiFactory.CreateNode(parent, name, typeof(LayoutElement));
        RectTransform rect = root.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, height));

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;
        return rect;
    }

    private void CreateLegacyHeaderStripAbsolute(Transform parent, string titleLabel, string statusLabel, float topOffset, float rowHeight)
    {
        float panelWidth = GetLegacyPanelContentWidth();
        float operateVisibleAspect = 1309f / 306f;
        float statusVisibleAspect = 1369f / 295f;
        float tabHeight = Mathf.Round(Mathf.Clamp(panelWidth * 0.072f, 62f, 78f));
        float titleTabWidth = Mathf.Round(tabHeight * operateVisibleAspect);
        float statusTabWidth = Mathf.Round(tabHeight * statusVisibleAspect);

        GameObject row = GameUiFactory.CreateNode(parent, "OperationsSelectorRow", typeof(HorizontalLayoutGroup));
        RectTransform rowRect = row.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rowRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -topOffset), new Vector2(0f, rowHeight));

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.padding = new RectOffset(18, 12, 4, 0);

        CreateLegacyHeaderTab(row.transform, "OperateHeaderTab", titleLabel, true, lowerHeaderOperateTabSprite, titleTabWidth, tabHeight);
        CreateLegacyHeaderTab(row.transform, "StatusHeaderTab", statusLabel, false, lowerHeaderStatusTabSprite, statusTabWidth, tabHeight);

        GameObject spacer = GameUiFactory.CreateNode(row.transform, "Spacer", typeof(LayoutElement));
        LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
        spacerLayout.flexibleWidth = 1f;
        spacerLayout.preferredHeight = tabHeight;
    }

    private Vector2 GetLegacyModuleSize(Sprite sprite)
    {
        float contentWidth = GetLegacyPanelContentWidth();
        float safeWidth = Mathf.Max(280f, contentWidth - 26f);
        float safeHeight = sprite != null
            ? Mathf.Round(safeWidth * (sprite.rect.height / sprite.rect.width))
            : 100f;
        return new Vector2(safeWidth, safeHeight);
    }

    private Vector2 GetLegacyModuleSizeForWidth(Sprite sprite, float width)
    {
        float safeWidth = Mathf.Max(280f, width);
        float safeHeight = sprite != null
            ? Mathf.Round(safeWidth * (sprite.rect.height / sprite.rect.width))
            : 100f;
        return new Vector2(safeWidth, safeHeight);
    }

    private RectTransform CreateLegacyAbsoluteSpriteStrip(Transform parent, string name, Sprite sprite, float topOffset, Vector2 size)
    {
        GameObject strip = GameUiFactory.CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(
            stripRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -topOffset),
            size);

        Image image = strip.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = false;
        return stripRect;
    }

    private void PopulateLegacySummaryStrip(RectTransform strip, string[] labels, string[] values)
    {
        int slotCount = Mathf.Min(labels.Length, values.Length);
        for (int i = 0; i < slotCount; i++)
        {
            float slotMin = i / 4f;
            float slotMax = (i + 1) / 4f;

            RectTransform slot = GameUiFactory.CreateNode(strip.transform, $"SummarySlot{i}").GetComponent<RectTransform>();
            GameUiFactory.SetAnchoredRect(slot, new Vector2(slotMin, 0f), new Vector2(slotMax, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -6f));

            Text labelText = GameUiFactory.CreateText(slot, "Label", theme, 25, new Color(0.20f, 0.22f, 0.30f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
            GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 0.60f), new Vector2(1f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-2f, -2f));
            labelText.text = labels[i];
            GameUiFactory.ApplyLightHudText(labelText);
            ConfigureSingleLineHudText(labelText, false);

            Text valueText = GameUiFactory.CreateText(slot, "Value", theme, 35, new Color(0.19f, 0.23f, 0.32f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
            GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.69f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-2f, -2f));
            valueText.text = values[i];
            GameUiFactory.ApplyLightHudText(valueText);
            ConfigureSingleLineHudText(valueText, false);
        }
    }

    private void PopulateLegacyMemoPanel(RectTransform panel, IReadOnlyList<string> memoLines)
    {
        int rowCount = Mathf.Min(3, memoLines != null ? memoLines.Count : 0);
        for (int i = 0; i < rowCount; i++)
        {
            float anchorMaxY = 1f - (i * (1f / 3f));
            float anchorMinY = anchorMaxY - (1f / 3f);

            RectTransform row = GameUiFactory.CreateNode(panel.transform, $"MemoRow{i}").GetComponent<RectTransform>();
            GameUiFactory.SetAnchoredRect(row, new Vector2(0f, anchorMinY), new Vector2(1f, anchorMaxY), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(-36f, -12f));

            GameObject bullet = GameUiFactory.CreateNode(row, "Bullet", typeof(CanvasRenderer), typeof(Image));
            RectTransform bulletRect = bullet.GetComponent<RectTransform>();
            GameUiFactory.SetAnchoredRect(bulletRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, -2f), new Vector2(12f, 12f));
            bulletRect.localEulerAngles = new Vector3(0f, 0f, 45f);

            Image bulletImage = bullet.GetComponent<Image>();
            bulletImage.color = new Color(0.96f, 0.69f, 0.12f, 1f);
            bulletImage.raycastTarget = false;

            Text lineText = GameUiFactory.CreateText(row, "Text", theme, 21, new Color(0.19f, 0.23f, 0.32f, 1f), TextAnchor.MiddleLeft, FontStyle.Normal);
            GameUiFactory.SetAnchoredRect(lineText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(52f, -2f), new Vector2(-70f, -6f));
            lineText.text = memoLines[i];
        }
    }

    private void CreateLegacyHeaderStrip(Transform parent, string titleLabel, string statusLabel)
    {
        float panelWidth = GetLegacyPanelContentWidth();
        float titleTabWidth = Mathf.Round(panelWidth * 0.34f);
        float statusTabWidth = Mathf.Round(panelWidth * 0.30f);
        float tabHeight = Mathf.Round(Mathf.Clamp(panelWidth * 0.155f, 54f, 76f));

        GameObject row = GameUiFactory.CreateNode(parent, "OperationsSelectorRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.padding = new RectOffset(4, 4, 8, 0);

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = tabHeight + 12f;
        rowLayout.flexibleWidth = 1f;

        CreateLegacyHeaderTab(row.transform, "OperateHeaderTab", titleLabel, true, lowerHeaderOperateTabSprite, titleTabWidth, tabHeight);
        CreateLegacyHeaderTab(row.transform, "StatusHeaderTab", statusLabel, false, lowerHeaderStatusTabSprite, statusTabWidth, tabHeight);

        GameObject spacer = GameUiFactory.CreateNode(row.transform, "Spacer", typeof(LayoutElement));
        LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
        spacerLayout.flexibleWidth = 1f;
        spacerLayout.preferredHeight = tabHeight;
    }

    private void CreateLegacyHeaderTab(Transform parent, string name, string label, bool isActive, Sprite sprite, float preferredWidth, float preferredHeight)
    {
        GameObject tab = GameUiFactory.CreateNode(parent, name, typeof(LayoutElement), typeof(RectMask2D));
        RectTransform tabRect = tab.GetComponent<RectTransform>();
        tabRect.sizeDelta = new Vector2(preferredWidth, preferredHeight);

        LayoutElement layout = tab.GetComponent<LayoutElement>();
        layout.minWidth = preferredWidth;
        layout.minHeight = preferredHeight;
        layout.preferredHeight = preferredHeight;
        layout.preferredWidth = preferredWidth;
        layout.flexibleWidth = 0f;

        GameObject visual = GameUiFactory.CreateNode(tab.transform, "Visual", typeof(CanvasRenderer), typeof(Image));
        Image image = visual.GetComponent<Image>();
        image.sprite = sprite != null ? sprite : (isActive ? lowerSummaryRowSprite : panelSprite);
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = false;

        RectTransform visualRect = visual.GetComponent<RectTransform>();
        float nativeWidth = image.sprite != null ? image.sprite.rect.width : preferredWidth;
        float nativeHeight = image.sprite != null ? image.sprite.rect.height : preferredHeight;
        float trimLeft = isActive ? 113f : 77f;
        float trimTop = isActive ? 359f : 332f;
        float trimWidth = isActive ? 1309f : 1369f;
        float trimHeight = isActive ? 306f : 295f;
        bool spriteAlreadyTrimmed =
            image.sprite != null &&
            Mathf.Abs(nativeWidth - trimWidth) <= 12f &&
            Mathf.Abs(nativeHeight - trimHeight) <= 12f;

        if (spriteAlreadyTrimmed)
        {
            GameUiFactory.Stretch(visualRect);
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;
            visualRect.localScale = Vector3.one;
        }
        else
        {
            float scale = trimWidth > 0.01f ? preferredWidth / trimWidth : 1f;
            float trimCenterX = trimLeft + (trimWidth * 0.5f);
            float trimCenterY = trimTop + (trimHeight * 0.5f);
            float offsetX = ((nativeWidth * 0.5f) - trimCenterX) * scale;
            float offsetY = (trimCenterY - (nativeHeight * 0.5f)) * scale;
            GameUiFactory.SetAnchoredRect(
                visualRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(offsetX, offsetY),
                new Vector2(nativeWidth, nativeHeight));
            visualRect.localScale = new Vector3(scale, scale, 1f);
        }

        Text text = GameUiFactory.CreateText(tab.transform, "Label", theme, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
        GameUiFactory.Stretch(text.rectTransform, 20f, 20f, 10f, 4f);
        text.color = Color.white;
        text.text = label;
        GameUiFactory.ApplyLightHudText(text);
        ConfigureSingleLineHudText(text, true);
    }

    private void CreateLegacySummaryStrip(Transform parent, string[] labels, string[] values)
    {
        RectTransform strip = CreateLegacyFullSpriteStrip(
            parent,
            "OperationsSummaryStrip",
            lowerSummaryRowSprite != null ? lowerSummaryRowSprite : panelSprite);

        PopulateLegacySummaryStrip(strip, labels, values);
    }

    private void CreateLegacyFeatureStrip(Transform parent, string leftTitle, string leftHighlight, string leftDetail, string rightTitle, string rightHighlight, string rightDetail)
    {
        RectTransform strip = CreateLegacyFullSpriteStrip(
            parent,
            "OperationsFeatureStrip",
            lowerFeatureRowSprite != null ? lowerFeatureRowSprite : panelSprite);

        CreateLegacyFeatureSlot(strip.transform, "LeftFeatureSlot", 0f, 0.5f, leftTitle, leftHighlight, leftDetail);
        CreateLegacyFeatureSlot(strip.transform, "RightFeatureSlot", 0.5f, 1f, rightTitle, rightHighlight, rightDetail);
    }

    private void CreateLegacyFeatureSlot(Transform parent, string name, float minX, float maxX, string title, string highlight, string detail)
    {
        RectTransform slot = GameUiFactory.CreateNode(parent, name).GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(slot, new Vector2(minX, 0f), new Vector2(maxX, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, -12f));

        Text titleText = GameUiFactory.CreateText(slot, "Title", theme, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
        GameUiFactory.SetAnchoredRect(titleText.rectTransform, new Vector2(0f, 0.73f), new Vector2(1f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -2f));
        titleText.text = title;
        GameUiFactory.ApplyLightHudText(titleText);
        ConfigureSingleLineHudText(titleText, false);

        Text highlightText = GameUiFactory.CreateText(slot, "Highlight", theme, 30, new Color(0.20f, 0.24f, 0.34f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
        GameUiFactory.SetAnchoredRect(highlightText.rectTransform, new Vector2(0f, 0.16f), new Vector2(1f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(-10f, -2f));
        highlightText.text = highlight;
        GameUiFactory.ApplyLightHudText(highlightText);
        ConfigureSingleLineHudText(highlightText, true);

        Text detailText = GameUiFactory.CreateText(slot, "Detail", theme, 19, new Color(0.28f, 0.33f, 0.42f, 1f), TextAnchor.MiddleCenter, FontStyle.Normal);
        GameUiFactory.SetAnchoredRect(detailText.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.26f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(-8f, -2f));
        detailText.text = detail;
        GameUiFactory.ApplyLightHudText(detailText);
        ConfigureSingleLineHudText(detailText, true);
    }

    private static void ConfigureSingleLineHudText(Text text, bool alignByGeometry = false)
    {
        if (text == null)
        {
            return;
        }

        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = false;
        text.alignByGeometry = alignByGeometry;
    }

    private void CreateLegacyMemoPanel(Transform parent, IReadOnlyList<string> memoLines)
    {
        RectTransform panel = CreateLegacyFullSpriteStrip(
            parent,
            "OperationsMemoPanel",
            lowerMemoPanelSprite != null ? lowerMemoPanelSprite : panelSprite);

        PopulateLegacyMemoPanel(panel, memoLines);
    }

    private string GetOperationsStatusTabLabel()
    {
        float satisfaction = economyManager != null ? economyManager.GetSatisfaction01() : 0f;
        return $"\ub9cc\uc871\ub3c4: {GetSatisfactionLabel(satisfaction)}";
    }

    private string GetSatisfactionLabel(float satisfaction)
    {
        if (satisfaction >= 0.88f)
        {
            return "\ub9e4\uc6b0 \uc88b\uc74c";
        }

        if (satisfaction >= 0.72f)
        {
            return "\uc88b\uc74c";
        }

        if (satisfaction >= 0.56f)
        {
            return "\ubcf4\ud1b5";
        }

        if (satisfaction >= 0.4f)
        {
            return "\uc8fc\uc758";
        }

        return "\ub0ae\uc74c";
    }

    private int GetOperationsDailyTarget()
    {
        if (economyManager == null)
        {
            return 20;
        }

        return Mathf.Max(20, economyManager.GetCurrentCapacityEstimate() / 2);
    }

    private string GetOperationsFacilityHeadline()
    {
        float cleanliness = economyManager != null ? economyManager.GetCleanliness01() : 0f;
        if (cleanliness >= 0.82f)
        {
            return "\uae30\uad6c \uccad\uc18c \uc911";
        }

        if (cleanliness >= 0.6f)
        {
            return "\uc810\uac80 \uc608\uc57d \uc815\uc0c1";
        }

        return "\uc815\ube44 \uc6b0\uc120 \ud544\uc694";
    }

    private IReadOnlyList<string> BuildOperationsMemoLines()
    {
        List<string> lines = new List<string>(3);
        IReadOnlyList<string> events = eventManager != null ? eventManager.RecentEventLog : null;
        if (events != null)
        {
            for (int i = Mathf.Max(0, events.Count - 3); i < events.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(events[i]))
                {
                    lines.Add(events[i]);
                }
            }
        }

        if (lines.Count == 0)
        {
            lines.Add($"\uc0c8\ub85c\uc6b4 PT \uc218\uc5c5 \uc900\ube44 \uc911");
            lines.Add($"\ud68c\uc6d0 \ud53c\ub4dc\ubc31: {GetSatisfactionLabel(economyManager != null ? economyManager.GetSatisfaction01() : 0f)}");
            lines.Add($"\uc6b4\ub3d9 \uae30\uad6c {Mathf.Max(1, economyManager != null ? economyManager.GetMachineCountEstimate() : 1)}\ub300 \uac00\ub3d9 \uc911");
        }

        while (lines.Count < 3)
        {
            lines.Add("\ud604\uc7a5 \uc6b4\uc601 \ub85c\uadf8 \ub300\uae30 \uc911");
        }

        return lines;
    }

    private RectTransform CreateLegacyFullSpriteStrip(Transform parent, string name, Sprite sprite)
    {
        float contentWidth = GetLegacyPanelContentWidth();
        float safeWidth = Mathf.Max(280f, contentWidth - 26f);
        float safeHeight = sprite != null
            ? Mathf.Round(safeWidth * (sprite.rect.height / sprite.rect.width))
            : 100f;

        GameObject row = GameUiFactory.CreateNode(parent, $"{name}Row", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 0f;
        rowLayout.padding = new RectOffset(0, 0, 0, 0);
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        LayoutElement rowLayoutElement = row.GetComponent<LayoutElement>();
        rowLayoutElement.preferredHeight = safeHeight;
        rowLayoutElement.flexibleWidth = 1f;

        GameObject strip = GameUiFactory.CreateNode(row.transform, name, typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        LayoutElement layout = strip.GetComponent<LayoutElement>();
        layout.preferredWidth = safeWidth;
        layout.preferredHeight = safeHeight;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image image = strip.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = false;
        GameUiFactory.SetAnchoredRect(
            stripRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(safeWidth, safeHeight));

        return stripRect;
    }

    private float GetLegacyPanelContentWidth()
    {
        RectTransform textRootRect = textRoot != null ? textRoot.GetComponent<RectTransform>() : null;
        float width = textRootRect != null ? textRootRect.rect.width : 0f;
        if (width <= 1f && textScrollRect != null && textScrollRect.viewport != null)
        {
            width = textScrollRect.viewport.rect.width;
        }

        if (width <= 1f)
        {
            Transform contentPanelTransform = FindDeepChild(transform, "ContentPanel");
            RectTransform contentPanelRect = contentPanelTransform != null ? contentPanelTransform.GetComponent<RectTransform>() : null;
            if (contentPanelRect != null)
            {
                width = contentPanelRect.rect.width - 28f;
            }
        }

        return Mathf.Max(320f, width);
    }

    private void BuildEconomyScreen()
    {
        EnsureScrollContentLayout(textRoot);

        int membershipRevenue = economyManager.GetDailyMembershipRevenue();
        int ptRevenue = economyManager.GetDailyPtRevenue();
        int ancillaryRevenue = economyManager.GetDailyAncillaryRevenue();
        int variableCost = economyManager.GetDailyVariableCost();
        int totalRevenue = membershipRevenue + ptRevenue + ancillaryRevenue;
        int netRevenue = economyManager.GetPreviewDailyNetRevenue();
        int payrollCost = Mathf.RoundToInt(variableCost * 0.64f);
        int upkeepCost = Mathf.Max(0, variableCost - payrollCost);

        CreateLegacyHeaderStrip(textRoot.transform, "\uACBD\uC81C \uACB0\uC0B0", "\uC8FC\uAC04");
        CreateLegacyMetricRow(
            "EconomyMetricRow",
            new[]
            {
                "\ud68c\uc6d0\uad8c",
                "PT \uc218\uc775",
                "\ubd80\uac00 \ub9e4\ucd9c",
                "\uc77c\uc77c \uc21c\uc774\uc775"
            },
            new[]
            {
                $"{membershipRevenue:N0} G",
                $"{ptRevenue:N0} G",
                $"{ancillaryRevenue:N0} G",
                $"{netRevenue:N0} G"
            });

        CreateLegacyDualInfoRow(
            "EconomyOverviewRow",
            "\ub9e4\ucd9c \uc9c0\ud45c",
            new[]
            {
                $"\ubcf4\uc720 \uc790\uae08|{(walletManager != null ? $"{walletManager.CurrentCash:N0} G" : "0 G")}",
                $"\uc2a4\ud0c0\ucf54\uc778|{(walletManager != null ? $"{walletManager.CurrentStarCoin:N0}" : "0")}",
                $"\ud68c\uc6d0 \ud750\ub984|{economyManager.GetActiveMemberCount()}\uba85 / \ub300\uae30 {economyManager.GetWaitingCustomersCount()}\uba85"
            },
            "\ube44\uc6a9 \uad6c\uc870",
            new[]
            {
                $"\ubcc0\ub3d9\ube44|{variableCost:N0} G",
                $"\uc6d4 \uae09\uc5ec|{payrollCost:N0} G",
                $"\uc720\uc9c0\ube44|{upkeepCost:N0} G"
            });

        IReadOnlyList<string> memoLines = BuildEconomyMemoLines(totalRevenue, netRevenue, variableCost);
        CreateLegacyTextSection(
            "EconomyDetailSection",
            "\uacb0\uc0b0 \uc0c1\uc138",
            new[]
            {
                $"\ucd5c\uadfc \uacb0\uc0b0|{GetSettlementSummaryLine()}",
                $"\uc8fc \uc218\uc785\uc6d0|{membershipRevenue:N0} G / PT {ptRevenue:N0} G",
                $"\ubd80\uac00 \ub9e4\ucd9c|{ancillaryRevenue:N0} G / \uae30\uad6c {economyManager.GetMachineCountEstimate()}\ub300",
                $"\ud604\uc7a5 \uba54\ubaa8|{memoLines[0]}"
            },
            228f);
    }

    private void BuildReviewScreen()
    {
        EnsureScrollContentLayout(textRoot);

        IReadOnlyList<GymEconomyManager.CustomerReview> reviews = economyManager.GetRecentReviews();
        List<string> memoLines = new List<string>();
        if (reviews != null)
        {
            for (int i = reviews.Count - 1; i >= 0 && memoLines.Count < 2; i--)
            {
                GymEconomyManager.CustomerReview review = reviews[i];
                string author = string.IsNullOrWhiteSpace(review.authorName) ? "\uD68C\uC6D0" : review.authorName;
                memoLines.Add($"{author}: {review.text}");
            }
        }

        IReadOnlyList<string> events = eventManager != null ? eventManager.RecentEventLog : null;
        if (events != null)
        {
            for (int i = events.Count - 1; i >= 0 && memoLines.Count < 3; i--)
            {
                if (!string.IsNullOrWhiteSpace(events[i]))
                {
                    memoLines.Add(events[i]);
                }
            }
        }

        while (memoLines.Count < 3)
        {
            memoLines.Add(memoLines.Count == 0
                ? "\uC544\uC9C1 \uB4F1\uB85D\uB41C \uB9AC\uBDF0\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4."
                : $"\uCD94\uC138: {economyManager.GetReviewTrendLabel()}");
        }

        int positiveReviews = 0;
        if (reviews != null)
        {
            for (int i = 0; i < reviews.Count; i++)
            {
                if (reviews[i].stars >= 4f)
                {
                    positiveReviews++;
                }
            }
        }

        CreateLegacyHeaderStrip(textRoot.transform, "\uB9AC\uBDF0 \uBCF4\uB4DC", "\uC624\uB298");
        CreateLegacyMetricRow(
            "ReviewMetricRow",
            new[]
            {
                "\ud604\uc7ac \ud3c9\ud310",
                "\uc624\ub298 \ubc18\uc751",
                "\ucd5c\uadfc \ucd94\uc138",
                "\uc774\ubca4\ud2b8"
            },
            new[]
            {
                $"{economyManager.GetCurrentReputationStars():0.0}\u2605",
                $"{economyManager.GetLastDailyReviewStars():0.0}\u2605",
                economyManager.GetReviewTrendLabel(),
                events != null && events.Count > 0 ? "\uc788\uc74c" : "\uc5c6\uc74c"
            });

        List<string> reviewLines = new List<string>();
        if (reviews != null)
        {
            for (int i = reviews.Count - 1; i >= 0 && reviewLines.Count < 3; i--)
            {
                GymEconomyManager.CustomerReview review = reviews[i];
                string author = string.IsNullOrWhiteSpace(review.authorName) ? "\ud68c\uc6d0" : review.authorName;
                reviewLines.Add($"{author} {review.stars:0.0}\u2605|{review.text}");
            }
        }

        if (reviewLines.Count == 0)
        {
            reviewLines.Add("\ub9ac\ubdf0 \ub300\uae30|\uc544\uc9c1 \uC218\uC9D1\uB41C \ub9ac\ubdf0\uac00 \uc5c6\uc2b5\ub2c8\ub2e4. \uc6b4\uc601\uc744 \uc9c4\ud589\ud558\uba74 \ubc18\uc751\uc774 \uc313\uc785\ub2c8\ub2e4.");
        }

        List<string> eventLines = new List<string>();
        if (events != null)
        {
            for (int i = Mathf.Max(0, events.Count - 3); i < events.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(events[i]))
                {
                    eventLines.Add($"\uae30\ub85d|{events[i]}");
                }
            }
        }

        if (eventLines.Count == 0)
        {
            eventLines.Add("\uae30\ub85d \uc5c6\uc74c|\ucd5c\uadfc \uc774\ubca4\ud2b8 \uae30\ub85d\uc774 \uc5c6\uc2b5\ub2c8\ub2e4.");
        }

        CreateLegacyTextSection("ReviewListSection", "\ucd5c\uadfc \ub9ac\ubdf0", reviewLines, 188f);
        CreateLegacyTextSection("ReviewEventSection", "\uc774\ubca4\ud2b8 \ub85c\uadf8", eventLines, 188f);
    }

    private void CreateLegacyMetricRow(string name, IReadOnlyList<string> labels, IReadOnlyList<string> values)
    {
        GameObject row = GameUiFactory.CreateNode(textRoot.transform, name, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 116f;
        rowLayout.flexibleWidth = 1f;

        int slotCount = Mathf.Min(labels.Count, values.Count);
        for (int i = 0; i < slotCount; i++)
        {
            RectTransform contentRoot;
            GameObject cell = CreateLegacySectionFrame(row.transform, $"{name}_{i}", 88f, out contentRoot, 10f);
            LayoutElement cellLayout = GameUiFactory.GetOrAdd<LayoutElement>(cell);
            cellLayout.flexibleWidth = 1f;

            Text labelText = GameUiFactory.CreateText(contentRoot, "Label", theme, 18, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(0f, 22f));
            GameUiFactory.ApplyMinimalHudText(labelText);
            GameUiFactory.ConfigureSingleLineText(labelText, TextAnchor.UpperLeft);
            labelText.text = labels[i];

            Text valueText = GameUiFactory.CreateText(contentRoot, "Value", theme, 30, theme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 6f), new Vector2(0f, -34f));
            GameUiFactory.ApplyMinimalHudText(valueText);
            GameUiFactory.ConfigureSingleLineText(valueText, TextAnchor.LowerLeft);
            valueText.text = values[i];
        }
    }

    private void CreateLegacyDualInfoRow(string name, string leftTitle, IReadOnlyList<string> leftLines, string rightTitle, IReadOnlyList<string> rightLines)
    {
        GameObject row = GameUiFactory.CreateNode(textRoot.transform, name, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 168f;
        rowLayout.flexibleWidth = 1f;

        CreateLegacyInfoColumn(row.transform, $"{name}_Left", leftTitle, leftLines);
        CreateLegacyInfoColumn(row.transform, $"{name}_Right", rightTitle, rightLines);
    }

    private void CreateLegacyInfoColumn(Transform parent, string name, string title, IReadOnlyList<string> lines)
    {
        RectTransform contentRoot;
        GameObject frame = CreateLegacySectionFrame(parent, name, 168f, out contentRoot, 14f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(frame);
        layout.flexibleWidth = 1f;

        CreateLegacySectionHeader(contentRoot, title);

        GameObject list = GameUiFactory.CreateNode(contentRoot, "List", typeof(VerticalLayoutGroup));
        RectTransform listRect = list.GetComponent<RectTransform>();
        GameUiFactory.Stretch(listRect, 0f, 0f, 28f, 0f);

        VerticalLayoutGroup listLayout = list.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 4f;
        listLayout.padding = new RectOffset(0, 0, 8, 0);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        if (lines == null || lines.Count == 0)
        {
            CreateLegacyInfoLine(list.transform, "\ub370\uc774\ud130", "--");
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            string[] pieces = lines[i].Split('|');
            string left = pieces.Length > 0 ? pieces[0] : string.Empty;
            string right = pieces.Length > 1 ? pieces[1] : string.Empty;
            CreateLegacyInfoLine(list.transform, left, right);
        }
    }

    private void CreateLegacyInfoLine(Transform parent, string left, string right)
    {
        GameObject row = GameUiFactory.CreateNode(parent, "InfoLine", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 34f;

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        Text leftText = GameUiFactory.CreateText(row.transform, "Left", theme, 19, theme.Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
        LayoutElement leftLayout = GameUiFactory.GetOrAdd<LayoutElement>(leftText.gameObject);
        leftLayout.flexibleWidth = 1f;
        GameUiFactory.ApplyMinimalHudText(leftText);
        GameUiFactory.ConfigureSingleLineText(leftText, TextAnchor.MiddleLeft);
        leftText.text = left;

        Text rightText = GameUiFactory.CreateText(row.transform, "Right", theme, 19, theme.MutedInk, TextAnchor.MiddleRight, FontStyle.Normal);
        LayoutElement rightLayout = GameUiFactory.GetOrAdd<LayoutElement>(rightText.gameObject);
        rightLayout.preferredWidth = 188f;
        GameUiFactory.ApplyMinimalHudText(rightText);
        GameUiFactory.ConfigureSingleLineText(rightText, TextAnchor.MiddleRight);
        rightText.text = right;
    }

    private void CreateLegacyTextSection(string name, string title, IReadOnlyList<string> lines, float preferredHeight)
    {
        RectTransform contentRoot;
        CreateLegacySectionFrame(textRoot.transform, name, preferredHeight, out contentRoot, 10f);
        CreateLegacySectionHeader(contentRoot, title);

        GameObject list = GameUiFactory.CreateNode(contentRoot, "List", typeof(VerticalLayoutGroup));
        RectTransform listRect = list.GetComponent<RectTransform>();
        GameUiFactory.Stretch(listRect, 0f, 0f, 28f, 0f);

        VerticalLayoutGroup listLayout = list.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.padding = new RectOffset(0, 0, 8, 0);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        if (lines == null || lines.Count == 0)
        {
            CreateLegacyTextRow(list.transform, "\ub370\uc774\ud130", "--");
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            string[] pieces = lines[i].Split('|');
            string titleText = pieces.Length > 0 ? pieces[0] : string.Empty;
            string bodyText = pieces.Length > 1 ? pieces[1] : string.Empty;
            CreateLegacyTextRow(list.transform, titleText, bodyText);
        }
    }

    private void CreateLegacyTextRow(Transform parent, string title, string body)
    {
        GameObject row = GameUiFactory.CreateNode(parent, "TextRow", typeof(VerticalLayoutGroup), typeof(LayoutElement));
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 62f;

        VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text titleText = GameUiFactory.CreateText(row.transform, "Title", theme, 18, theme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement titleLayout = GameUiFactory.GetOrAdd<LayoutElement>(titleText.gameObject);
        titleLayout.preferredHeight = 20f;
        GameUiFactory.ApplyMinimalHudText(titleText);
        GameUiFactory.ConfigureSingleLineText(titleText, TextAnchor.MiddleLeft);
        titleText.text = title;

        Text bodyText = GameUiFactory.CreateText(row.transform, "Body", theme, 17, theme.MutedInk, TextAnchor.MiddleLeft, FontStyle.Normal);
        LayoutElement bodyLayout = GameUiFactory.GetOrAdd<LayoutElement>(bodyText.gameObject);
        bodyLayout.preferredHeight = 24f;
        GameUiFactory.ApplyMinimalHudText(bodyText);
        GameUiFactory.ConfigureWrappedText(bodyText, TextAnchor.UpperLeft);
        bodyText.text = body;
    }

    private GameObject CreateLegacySectionFrame(Transform parent, string name, float preferredHeight, out RectTransform contentRoot, float padding)
    {
        GameObject frame = GameUiFactory.CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        Image borderImage = frame.GetComponent<Image>();
        borderImage.color = new Color(0.25f, 0.22f, 0.16f, 0.95f);
        borderImage.raycastTarget = false;

        LayoutElement layout = frame.GetComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;

        GameObject fill = GameUiFactory.CreateNode(frame.transform, "Fill", typeof(CanvasRenderer), typeof(Image));
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.97f, 0.95f, 0.88f, 1f);
        fillImage.raycastTarget = false;
        GameUiFactory.Stretch(fill.GetComponent<RectTransform>(), 2f, 2f, 2f, 2f);

        GameObject content = GameUiFactory.CreateNode(frame.transform, "Content");
        contentRoot = content.GetComponent<RectTransform>();
        GameUiFactory.Stretch(contentRoot, padding, padding, padding + 4f, padding + 4f);
        return frame;
    }

    private void CreateLegacySectionHeader(RectTransform parent, string title)
    {
        Text headerText = GameUiFactory.CreateText(parent, "Header", theme, 20, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(headerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(0f, 22f));
        GameUiFactory.ApplyMinimalHudText(headerText);
        GameUiFactory.ConfigureSingleLineText(headerText, TextAnchor.UpperLeft);
        headerText.text = title;

        GameObject divider = GameUiFactory.CreateNode(parent, "Divider", typeof(CanvasRenderer), typeof(Image));
        Image dividerImage = divider.GetComponent<Image>();
        dividerImage.color = new Color(0.28f, 0.25f, 0.18f, 0.7f);
        dividerImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 1f));
    }

    private IReadOnlyList<string> BuildEconomyMemoLines(int totalRevenue, int netRevenue, int variableCost)
    {
        List<string> lines = new List<string>(3)
        {
            $"\ucd1d \uc218\uc785 {totalRevenue:N0} G / \uc21c\uc774\uc775 {netRevenue:N0} G",
            $"\uc77c\uc77c \uc6b4\uc601\ube44 {variableCost:N0} G / \ud2b8\ub808\uc774\ub108 {economyManager.GetCurrentTrainerCount()}\uba85",
            $"\ube0c\ub79c\ub4dc {economyManager.GetAverageBrandLabel()} / \uae30\uad6c {economyManager.GetMachineCountEstimate()}\ub300 \uac00\ub3d9"
        };

        return lines;
    }

    private IReadOnlyList<string> BuildReviewMemoLines(IReadOnlyList<GymEconomyManager.CustomerReview> reviews)
    {
        List<string> lines = new List<string>(3);
        if (reviews != null)
        {
            for (int i = reviews.Count - 1; i >= 0 && lines.Count < 3; i--)
            {
                GymEconomyManager.CustomerReview review = reviews[i];
                string author = string.IsNullOrWhiteSpace(review.authorName) ? "\ud68c\uc6d0" : review.authorName;
                lines.Add($"{author}: {review.text}");
            }
        }

        while (lines.Count < 3)
        {
            if (lines.Count == 0)
            {
                lines.Add("\uc544\uc9c1 \ub4f1\ub85d\ub41c \ub9ac\ubdf0\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.");
            }
            else if (lines.Count == 1)
            {
                lines.Add($"\ucd94\uc138: {economyManager.GetReviewTrendLabel()}");
            }
            else
            {
                lines.Add($"\ube0c\ub79c\ub4dc: {economyManager.GetAverageBrandLabel()} / \uc785\uc9c0: {economyManager.GetCurrentLocationPreviewLabel()}");
            }
        }

        return lines;
    }

    private string GetSettlementSummaryLine()
    {
        if (settlementManager == null || string.IsNullOrWhiteSpace(settlementManager.LastSettlementText))
        {
            return "\ucd5c\uadfc \uacb0\uc0b0 \uae30\ub85d\uc774 \uc544\uc9c1 \uc5c6\uc2b5\ub2c8\ub2e4";
        }

        string compact = settlementManager.LastSettlementText.Replace('\r', ' ').Replace('\n', ' ');
        return compact.Length > 52 ? compact.Substring(0, 52) + "..." : compact;
    }

    private void BuildReviewBoardCard(IReadOnlyList<GymEconomyManager.CustomerReview> reviews)
    {
        RectTransform reviewContent;
        GameObject reviewCard = GameUiFactory.CreateCard(textRoot.transform, "ReviewBoardCard", theme, out reviewContent, 388f);
        CreateCardTitle(reviewContent, "\ucd5c\uadfc \ub9ac\ubdf0");
        Transform reviewList = CreateCardList(reviewContent, 48f);

        if (reviews != null && reviews.Count > 0)
        {
            for (int i = reviews.Count - 1; i >= 0 && i >= reviews.Count - 3; i--)
            {
                GymEconomyManager.CustomerReview review = reviews[i];
                string author = string.IsNullOrWhiteSpace(review.authorName) ? "\ud68c\uc6d0" : review.authorName;
                CreateReviewRow(
                    reviewList,
                    $"{author}  {review.stars:0.0}\uc810",
                    review.text,
                    $"{review.month:D2}/{review.day:D2}");
            }
        }
        else
        {
            CreateMiniRow(reviewList, "\uc548\ub0b4", "\uc544\uc9c1 \ub4f1\ub85d\ub41c \ub9ac\ubdf0\uac00 \uc5c6\uc2b5\ub2c8\ub2e4", GameUiTone.Surface);
            CreateMiniRow(reviewList, "\ucd94\uc138", economyManager.GetReviewTrendLabel(), GameUiTone.AccentAlt);
            CreateMiniRow(reviewList, "\ube0c\ub79c\ub4dc", economyManager.GetAverageBrandLabel(), GameUiTone.Surface);
        }
    }

    private void BuildReviewEventLogCard()
    {
        RectTransform logContent;
        GameObject logCard = GameUiFactory.CreateCard(textRoot.transform, "ReviewEventLogCard", theme, out logContent, 244f);
        CreateCardTitle(logContent, "\uc774\ubca4\ud2b8 \ub85c\uadf8");
        Transform logList = CreateCardList(logContent, 48f);

        IReadOnlyList<string> events = eventManager != null ? eventManager.RecentEventLog : null;
        if (events != null)
        {
            for (int i = Mathf.Max(0, events.Count - 3); i < events.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(events[i]))
                {
                    CreateMiniRow(logList, "\ub85c\uadf8", events[i], GameUiTone.Surface);
                }
            }
        }

        if (logList.childCount == 0)
        {
            CreateMiniRow(logList, "\ud604\ud669", "\uc624\ub298\uc740 \ud070 \uc774\uc288 \uc5c6\uc774 \uc6b4\uc601 \uc911\uc785\ub2c8\ub2e4", GameUiTone.Surface);
            CreateMiniRow(logList, "\ub9ac\ubdf0", $"\ucd5c\uadfc \ucd94\uc138 {economyManager.GetReviewTrendLabel()}", GameUiTone.AccentAlt);
            CreateMiniRow(logList, "\uc6b4\uc601", $"\ub300\uae30 {economyManager.GetAverageWaitSeconds():0.0}\ucd08 / \uccad\uacb0 {economyManager.GetCleanliness01() * 100f:0}%", GameUiTone.Surface);
        }
    }

    private void CreateEconomySummaryCard(
        Transform parent,
        string name,
        string topLabel,
        string topValue,
        Color topValueColor,
        string bottomLabel,
        string bottomValue,
        Color bottomValueColor)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(parent, name, theme, out contentRoot, 156f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(card);
        layout.preferredHeight = 156f;
        layout.flexibleWidth = 1f;

        CreateEconomyMetricPair(contentRoot, topLabel, topValue, topValueColor, 1f, 0.50f);
        CreateEconomyMetricPair(contentRoot, bottomLabel, bottomValue, bottomValueColor, 0.50f, 0f);

        GameObject divider = GameUiFactory.CreateNode(contentRoot, "Divider", typeof(CanvasRenderer), typeof(Image));
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(dividerRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, 2f));
        Image dividerImage = divider.GetComponent<Image>();
        dividerImage.color = theme.Divider;
        dividerImage.raycastTarget = false;
    }

    private void CreateEconomyMetricPair(RectTransform parent, string label, string value, Color valueColor, float anchorTop, float anchorBottom)
    {
        RectTransform slot = GameUiFactory.CreateNode(parent, $"{label}_Metric").GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(slot, new Vector2(0f, anchorBottom), new Vector2(1f, anchorTop), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));

        Text labelText = GameUiFactory.CreateText(slot, "Label", theme, 19, theme.MutedInk, TextAnchor.MiddleLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.46f, 1f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(-8f, -8f));
        labelText.text = label;

        Text valueText = GameUiFactory.CreateText(slot, "Value", theme, 24, valueColor, TextAnchor.MiddleRight, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0.42f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(-8f, -8f));
        valueText.text = value;
    }

    private void CreateEconomyBreakdownCard(Transform parent, string name, string title, GameUiTone tone, IReadOnlyList<string> rows)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(parent, name, theme, out contentRoot, 248f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(card);
        layout.preferredHeight = 248f;
        layout.flexibleWidth = 1f;

        GameObject titleChip = GameUiFactory.CreateStateChip(contentRoot, "TitleChip", theme, title, tone, out Text chipText);
        RectTransform chipRect = titleChip.GetComponent<RectTransform>();
        chipRect.anchorMin = new Vector2(0f, 1f);
        chipRect.anchorMax = new Vector2(1f, 1f);
        chipRect.pivot = new Vector2(0.5f, 1f);
        chipRect.anchoredPosition = new Vector2(0f, -4f);
        chipRect.sizeDelta = new Vector2(-24f, 40f);
        chipText.alignment = TextAnchor.MiddleCenter;

        Transform list = CreateCardList(contentRoot, 52f);
        for (int i = 0; i < rows.Count; i++)
        {
            string[] pieces = rows[i].Split('|');
            string left = pieces.Length > 0 ? pieces[0] : string.Empty;
            string center = pieces.Length > 1 ? pieces[1] : string.Empty;
            string right = pieces.Length > 2 ? pieces[2] : string.Empty;
            CreateEconomyBreakdownRow(list, left, center, right, tone);
        }
    }

    private void CreateEconomyBreakdownRow(Transform parent, string label, string value, string note, GameUiTone tone)
    {
        RectTransform contentRoot;
        GameObject row = GameUiFactory.CreateListRow(parent, $"{label}_Row", theme, out contentRoot, 48f);
        Transform fill = row.transform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = tone == GameUiTone.Warning ? theme.PanelFillAlt : theme.PanelFill;
            }
        }

        Text labelText = GameUiFactory.CreateText(contentRoot, "Label", theme, 17, theme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0.34f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(-6f, -4f));
        labelText.text = label;

        Text valueText = GameUiFactory.CreateText(contentRoot, "Value", theme, 18, theme.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.76f, 1f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(-10f, -4f));
        valueText.text = value;

        Text noteText = GameUiFactory.CreateText(contentRoot, "Note", theme, 15, tone == GameUiTone.Warning ? theme.Warning : theme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(noteText.rectTransform, new Vector2(0.74f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(-4f, -4f));
        noteText.text = note;
    }

    private void CreateEconomyHistoryCard(int totalRevenue, int netRevenue, int membershipRevenue, int ptRevenue, int ancillaryRevenue, int variableCost, int payrollCost)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(textRoot.transform, "EconomyHistoryCard", theme, out contentRoot, 304f);
        CreateCardTitle(contentRoot, "\uCD5C\uADFC \uACB0\uC0B0 \uB0B4\uC5ED");

        int[] samples =
        {
            Mathf.Max(600, membershipRevenue / 2),
            Mathf.Max(800, membershipRevenue),
            Mathf.Max(900, membershipRevenue + ancillaryRevenue),
            Mathf.Max(1200, totalRevenue - (variableCost / 2)),
            Mathf.Max(1400, totalRevenue),
            Mathf.Max(1500, totalRevenue + ptRevenue),
            Mathf.Max(1600, netRevenue + payrollCost)
        };

        float maxSample = 1f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxSample = Mathf.Max(maxSample, samples[i]);
        }

        GameObject chartRoot = GameUiFactory.CreateNode(contentRoot, "ChartRoot", typeof(RectMask2D));
        RectTransform chartRect = chartRoot.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(chartRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(0f, -120f));

        GameObject baseline = GameUiFactory.CreateNode(chartRoot.transform, "Baseline", typeof(CanvasRenderer), typeof(Image));
        Image baselineImage = baseline.GetComponent<Image>();
        baselineImage.color = theme.Divider;
        baselineImage.raycastTarget = false;
        GameUiFactory.SetAnchoredRect(baseline.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(0f, 2f));

        for (int i = 0; i < samples.Length; i++)
        {
            float x01 = samples.Length == 1 ? 0.5f : i / (samples.Length - 1f);
            float ratio = Mathf.Clamp01(samples[i] / maxSample);
            CreateGraphBar(chartRect, $"{i + 1}\uC8FC", ratio, theme.AccentAlt, Mathf.Lerp(0.12f, 0.88f, x01));
        }

        Text footer = GameUiFactory.CreateText(contentRoot, "Footer", theme, 17, theme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 24f));
        footer.text = $"\uCD5C\uADFC \uD750\uB984  {Mathf.Min(netRevenue, totalRevenue - variableCost):N0} G  ~  {Mathf.Max(netRevenue, totalRevenue):N0} G";
    }

    private static string GetRatioLabel(int part, int total)
    {
        if (total <= 0)
        {
            return "0%";
        }

        return $"{(part / (float)total) * 100f:0}%";
    }

    private void CreateReviewScoreCard(float rating, float previousRating, float averageWaitSeconds, float cleanliness01)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(textRoot.transform, "ReviewScoreCard", theme, out contentRoot, 122f);

        Text starsText = GameUiFactory.CreateText(contentRoot, "Stars", theme, 26, theme.Warning, TextAnchor.MiddleLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(starsText.rectTransform, new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(-8f, -4f));
        starsText.text = $"\u2605 {rating:0.0}";

        Text summaryText = GameUiFactory.CreateText(contentRoot, "Summary", theme, 16, theme.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(summaryText.rectTransform, new Vector2(0.34f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(-8f, -4f));
        summaryText.text = $"\uC804\uC77C {previousRating:0.0}\uC810  |  \uB300\uAE30 {averageWaitSeconds:0.0}\uCD08  |  \uCCAD\uACB0 {cleanliness01 * 100f:0}%";
    }

    private void CreateHeadline(string title, string subtitle)
    {
        RectTransform contentRoot;
        GameObject header = GameUiFactory.CreateCard(textRoot.transform, "Headline", theme, out contentRoot, 116f);
        Text titleText = GameUiFactory.CreateText(contentRoot, "Title", theme, 32, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 36f));
        titleText.text = title;

        Text subtitleText = GameUiFactory.CreateText(contentRoot, "Subtitle", theme, 22, theme.MutedInk, TextAnchor.LowerLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(subtitleText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(0f, -38f));
        subtitleText.text = subtitle;
    }

    private void CreateMetricCell(Transform parent, string label, string value)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(parent, label.Replace(" ", string.Empty), theme, out contentRoot, 122f);
        GameUiFactory.GetOrAdd<LayoutElement>(card).flexibleWidth = 1f;
        GameUiFactory.GetOrAdd<LayoutElement>(card).preferredHeight = 122f;

        Text labelText = GameUiFactory.CreateText(contentRoot, "Label", theme, 18, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 22f));
        labelText.text = label;

        Text valueText = GameUiFactory.CreateText(contentRoot, "Value", theme, 28, theme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(0f, -24f));
        valueText.text = value;
    }

    private Transform CreateSplitRow(string name, float height)
    {
        GameObject row = GameUiFactory.CreateNode(textRoot.transform, name, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 1f;
        return row.transform;
    }

    private void CreateFocusCard(Transform parent, string title, string headline, string body, GameUiTone tone)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(parent, title.Replace(" ", string.Empty), theme, out contentRoot, 214f);
        GameUiFactory.GetOrAdd<LayoutElement>(card).flexibleWidth = 1f;
        Transform fill = card.transform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = tone == GameUiTone.AccentAlt ? theme.PanelFillAlt : theme.PanelFill;
            }
        }

        CreateCardTitle(contentRoot, title);

        GameObject chip = GameUiFactory.CreateStateChip(contentRoot, "HeadlineChip", theme, headline, tone, out _);
        RectTransform chipRect = chip.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(chipRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(-24f, 44f));

        Text bodyText = GameUiFactory.CreateText(contentRoot, "Body", theme, 18, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.Stretch(bodyText.rectTransform, 0f, 0f, 98f, 0f);
        bodyText.text = body;
    }

    private void CreateInfoMemoCard(string name, string title, IReadOnlyList<string> labels, IReadOnlyList<string> lines)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(textRoot.transform, name, theme, out contentRoot, 244f);
        CreateCardTitle(contentRoot, title);
        Transform list = CreateCardList(contentRoot, 48f);

        int rowCount = Mathf.Min(labels.Count, lines.Count);
        for (int i = 0; i < rowCount; i++)
        {
            CreateMiniRow(list, labels[i], lines[i], i == 1 ? GameUiTone.AccentAlt : GameUiTone.Surface);
        }
    }

    private void CreateMixGraph(Transform parent, string title, float general, float middle, float upper)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreateCard(parent, "MixGraph", theme, out contentRoot, 310f);
        GameUiFactory.GetOrAdd<LayoutElement>(card).flexibleWidth = 1f;

        CreateCardTitle(contentRoot, title);
        CreateGraphBar(contentRoot, "?쇰컲", general, theme.TabIdle, 0.18f);
        CreateGraphBar(contentRoot, "以묎툒", middle, theme.AccentAlt, 0.50f);
        CreateGraphBar(contentRoot, "?곴툒", upper, theme.Warning, 0.82f);
    }

    private void CreateGraphBar(RectTransform parent, string label, float ratio, Color color, float x01)
    {
        GameObject root = GameUiFactory.CreateNode(parent, $"{label}BarRoot");
        RectTransform rootRect = root.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(rootRect, new Vector2(x01, 0f), new Vector2(x01, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 0f));

        GameObject bar = GameUiFactory.CreateNode(root.transform, "Bar", typeof(CanvasRenderer), typeof(Image));
        Image barImage = bar.GetComponent<Image>();
        barImage.color = color;
        barImage.raycastTarget = false;

        float height = Mathf.Lerp(24f, 96f, Mathf.Clamp01(ratio));
        RectTransform barRect = bar.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(barRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(28f, height));

        Text valueText = GameUiFactory.CreateText(root.transform, "Value", theme, 14, theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(valueText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 18f));
        valueText.text = $"{ratio * 100f:0}%";

        Text labelText = GameUiFactory.CreateText(root.transform, "Label", theme, 14, theme.MutedInk, TextAnchor.MiddleCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 18f));
        labelText.text = label;
    }

    private void CreateCardTitle(RectTransform parent, string title)
    {
        Text text = GameUiFactory.CreateText(parent, "Title", theme, 26, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 30f));
        text.text = title;
    }

    private Transform CreateCardList(RectTransform parent, float topInset)
    {
        GameObject root = GameUiFactory.CreateNode(parent, "List", typeof(VerticalLayoutGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        GameUiFactory.Stretch(rect, 0f, 0f, topInset, 0f);

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return root.transform;
    }

    private void CreateMiniRow(Transform parent, string label, string body, GameUiTone tone)
    {
        RectTransform contentRoot;
        GameObject row = GameUiFactory.CreateListRow(parent, label.Replace(" ", string.Empty), theme, out contentRoot, 62f);
        Transform fill = row.transform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = tone == GameUiTone.Surface ? theme.PanelFill : theme.GetToneFill(tone);
            }
        }

        Text labelText = GameUiFactory.CreateText(contentRoot, "Label", theme, 18, tone == GameUiTone.Surface ? theme.Ink : theme.GetToneInk(tone), TextAnchor.MiddleLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(140f, 0f));
        labelText.text = label;

        Text bodyText = GameUiFactory.CreateText(contentRoot, "Body", theme, 18, tone == GameUiTone.Surface ? theme.MutedInk : theme.BrightInk, TextAnchor.MiddleLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(76f, 0f), new Vector2(-76f, 0f));
        bodyText.text = body;
    }

    private void CreateReviewRow(Transform parent, string title, string body, string badge)
    {
        RectTransform contentRoot;
        GameObject row = GameUiFactory.CreateListRow(parent, "ReviewRow", theme, out contentRoot, 106f);

        Text titleText = GameUiFactory.CreateText(contentRoot, "Title", theme, 20, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(-150f, 24f));
        titleText.text = title;

        Text bodyText = GameUiFactory.CreateText(contentRoot, "Body", theme, 18, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(-150f, -34f));
        bodyText.text = body;

        GameObject badgeChip = GameUiFactory.CreateBadge(contentRoot, "Badge", theme, badge, GameUiTone.AccentAlt, out _);
        RectTransform badgeRect = badgeChip.GetComponent<RectTransform>();
        GameUiFactory.SetAnchoredRect(badgeRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(112f, 42f));
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

    private void SetTextIfChanged(Text target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
