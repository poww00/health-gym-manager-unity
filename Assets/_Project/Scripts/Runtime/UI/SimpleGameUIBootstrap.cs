using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SimpleGameUIBootstrap
{
    internal const string TargetSceneName = "TestSandbox";
    internal const string CanvasName = "RuntimeGameUI_Canvas";
    private const string LegacyCanvasName = "SimpleGameUI_Canvas";
    private const string LegacyHudCanvasName = "MainHUD_Canvas";
    private const string LegacyStaffCanvasName = "StaffUI_Canvas";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBuild(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBuild(scene);
    }

    private static void TryBuild(Scene scene)
    {
        if (!scene.IsValid() || scene.name != TargetSceneName)
        {
            return;
        }

        GameObject legacyCanvas = GameObject.Find(LegacyCanvasName);
        if (legacyCanvas != null)
        {
            legacyCanvas.SetActive(false);
        }

        HideLegacySceneCanvases();

        GameObject canvasObject = GameObject.Find(CanvasName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = GameUiFactory.GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GameUiFactory.GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        GameUiFactory.GetOrAdd<GraphicRaycaster>(canvasObject);
        GameUiFactory.Stretch(canvasObject.GetComponent<RectTransform>());
        GameUiFactory.EnsureEventSystem(null, "RuntimeGameUI_EventSystem");

        GameRuntimeUIController controller = GameUiFactory.GetOrAdd<GameRuntimeUIController>(canvasObject);
        controller.Initialize();
    }

    internal static void HideLegacySceneCanvases()
    {
        HideLegacyCanvas(LegacyHudCanvasName);
        HideLegacyCanvas(LegacyStaffCanvasName);
    }

    private static void HideLegacyCanvas(string canvasName)
    {
        GameObject root = GameObject.Find(canvasName);
        if (root == null)
        {
            return;
        }

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = root.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].overrideSorting = true;
            canvases[i].sortingOrder = -1000 - i;
        }
    }
}

public partial class RuntimeGameUIController
{
    private enum PanelMode
    {
        Operate,
        Install,
        Economy,
        Review
    }

    private static readonly EquipmentCategory[] InstallCategories =
    {
        EquipmentCategory.Cardio,
        EquipmentCategory.Push,
        EquipmentCategory.Pull,
        EquipmentCategory.Legs,
        EquipmentCategory.Recovery,
        EquipmentCategory.Other
    };

    private static readonly string[] InstallCategoryLabels =
    {
        "카디오",
        "푸쉬",
        "풀",
        "하체",
        "회복",
        "기타"
    };

    private const float InstallCategoryTabStartX = -415f;
    private const float InstallCategoryTabSpacingX = 166f;
    private const float InstallCategoryTabY = 274f;
    private const float InstallCategoryTabWidth = 158f;
    private const float InstallCategoryTabHeight = 68f;
    private const int InstallCategoryTabFontSize = 22;

    private GameUiTheme theme;
    private WalletManager walletManager;
    private TimeManager timeManager;
    private GymEconomyManager economyManager;
    private GymSiteManager siteManager;
    private EquipmentCatalog equipmentCatalog;
    private PlacementManager placementManager;
    private StaffManager staffManager;

    private Transform runtimeRoot;
    private Transform gymSceneRoot;
    private Transform floorRoot;
    private Transform wallRoot;
    private Transform equipmentRoot;
    private Transform decorationRoot;
    private Transform characterRoot;
    private Transform topHudRoot;
    private Transform sharedPanelRoot;
    private Transform sharedPanelContentRoot;
    private Transform operatePanelRoot;
    private Transform installPanelRoot;
    private Transform installListRoot;
    private Transform comingSoonPanelRoot;
    private Transform economyPanelRoot;
    private Transform reviewPanelRoot;
    private Transform bottomNavRoot;
    private Transform toastRoot;
    private Transform placementActionRoot;
    private Transform staffPopupRoot;
    private Transform staffPopupListRoot;
    private ScrollRect staffPopupScrollRect;

    private Text sharedPanelTitleText;
    private Text dateValueText;
    private Text branchValueText;
    private Text cashValueText;
    private Text starCoinValueText;
    private Text memberValueText;
    private Text operateGoalText;
    private Text operateStatusText;
    private Text operateCrowdText;
    private Text operateCleanText;
    private Text operateUsageText;
    private Text operateWaitText;
    private Text operateStaffText;
    private Text operateRevenueText;
    private Text selectedItemNameText;
    private Text selectedItemPriceText;
    private Text selectedItemDescText;
    private Text comingSoonText;
    private Text toastText;
    private CanvasGroup toastCanvasGroup;
    private Text placementEyebrowText;
    private Text placementTitleText;
    private Text placementStatusText;
    private Text placementDetailText;
    private Text staffRecruitTabText;
    private Text staffCurrentTabText;
    private Image selectedItemIconImage;

    private Button operateTabButton;
    private Button installTabButton;
    private Button economyTabButton;
    private Button reviewTabButton;
    private Button staffRecruitTabButton;
    private Button staffCurrentTabButton;
    private Text operateTabText;
    private Text installTabText;
    private Text economyTabText;
    private Text reviewTabText;
    private readonly List<Button> placementActionButtons = new List<Button>();
    private readonly List<Text> placementActionButtonTexts = new List<Text>();
    private readonly List<Button> categoryButtons = new List<Button>();
    private readonly List<Text> categoryTexts = new List<Text>();

    private EquipmentDefinition selectedDefinition;
    private int selectedCategoryIndex;
    private PanelMode activePanel;
    private float nextDataRefreshAt;
    private RectTransform sharedPanelRectTransform;
    private CanvasGroup sharedPanelCanvasGroup;
    private Vector2 sharedPanelOpenAnchoredPosition;
    private float sharedPanelVelocityY;
    private bool sharedPanelPositionCaptured;
    private bool sharedPanelCollapsed;
    private bool sharedPanelAnimating;
    private bool sharedPanelPauseApplied;
    private bool staffShowingApplicants = true;
    private StaffManager boundStaffManager;

    private const float SharedPanelClosedExtraOffset = 190f;
    private const float SharedPanelSpringStrength = 62f;
    private const float SharedPanelDamping = 12f;
    private const float ToastVisibleSeconds = 3f;
    private const float ToastFadeSeconds = 0.7f;

    private float toastVisibleUntil;
    private float toastFadeStartedAt;
    private bool toastFading;

    public void Initialize()
    {
        theme = GameUiTheme.CreateDefault();
        GameplayBgmPlayer.EnsurePlaying();
        SimpleGameUIBootstrap.HideLegacySceneCanvases();
        ResolveReferences();

        selectedCategoryIndex = 0;
        selectedDefinition = EquipmentSelectionState.CurrentDefinition ?? GetFirstDefinitionForCategory(InstallCategories[selectedCategoryIndex]);

        if (TryBindExistingUi())
        {
            RemoveSavedRuntimeMenuPopupRoots();
            HideLegacyRuntimeFloorMockup();
            HideToast();
            EnsureEconomyPanelContent();
            EnsurePlacementActionOverlay();
            EnsureStaffPopup();
            BindMonthlySettlementPopupEvents();
            EnsureInstallCategoryTabs();
            BindStaticButtons();
            ShowOperatePanel();
            RefreshAllData();
            InitializeInstallTutorial();
            return;
        }

        Transform existingRoot = transform.Find("RuntimeGameUIRoot");
        GameObject rootObject = existingRoot != null
            ? existingRoot.gameObject
            : GameUiFactory.CreateNode(transform, "RuntimeGameUIRoot");
        rootObject.SetActive(true);
        GameUiFactory.ClearChildren(rootObject.transform);
        runtimeRoot = rootObject.transform;
        GameUiFactory.Stretch(rootObject.GetComponent<RectTransform>());

        BuildGymSceneRoot();
        BuildTopHud();
        BuildSharedContentPanel();
        BuildOperateContent();
        BuildInstallContent();
        BuildComingSoonContent();
        EnsureEconomyPanelContent();
        BuildBottomNav();
        BuildToast();
        EnsurePlacementActionOverlay();
        EnsureStaffPopup();
        BindMonthlySettlementPopupEvents();
        HideLegacyRuntimeFloorMockup();
        HideToast();

        ShowOperatePanel();
        RefreshAllData();
        InitializeInstallTutorial();
    }

    public void MaterializeForEditMode()
    {
        if (Application.isPlaying)
        {
            return;
        }

        theme = GameUiTheme.CreateDefault();
        SimpleGameUIBootstrap.HideLegacySceneCanvases();
        ResolveReferences();

        selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, InstallCategories.Length - 1);
        selectedDefinition ??= EquipmentSelectionState.CurrentDefinition ?? GetFirstDefinitionForCategory(InstallCategories[selectedCategoryIndex]);

        if (TryBindExistingUi())
        {
            EnsurePlacementActionOverlay();
            BindStaticButtons();
            PreviewOperatePanelForEditMode();
            RefreshAllData();
            return;
        }

        Initialize();
    }

    public void PreviewOperatePanelForEditMode()
    {
        if (Application.isPlaying || !TryBindExistingUi())
        {
            return;
        }

        activePanel = PanelMode.Operate;
        sharedPanelRoot.gameObject.SetActive(true);
        SetSharedPanelTitle("?댁쁺 ?꾪솴");
        operatePanelRoot.gameObject.SetActive(true);
        installPanelRoot.gameObject.SetActive(false);
        SetOptionalPanelActive("EconomyPanelRoot", false);
        SetOptionalPanelActive("ReviewPanelRoot", false);
        comingSoonPanelRoot.gameObject.SetActive(false);
        RefreshBottomTabs();
        RefreshOperatePanel();
        RefreshPlacementActionOverlay();
    }

    public void PreviewInstallPanelForEditMode()
    {
        if (Application.isPlaying || !TryBindExistingUi())
        {
            return;
        }

        activePanel = PanelMode.Install;
        selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, InstallCategories.Length - 1);
        selectedDefinition ??= EquipmentSelectionState.CurrentDefinition ?? GetFirstDefinitionForCategory(InstallCategories[selectedCategoryIndex]);
        sharedPanelRoot.gameObject.SetActive(true);
        SetSharedPanelTitle("?ㅼ튂");
        operatePanelRoot.gameObject.SetActive(false);
        installPanelRoot.gameObject.SetActive(true);
        SetOptionalPanelActive("EconomyPanelRoot", false);
        SetOptionalPanelActive("ReviewPanelRoot", false);
        comingSoonPanelRoot.gameObject.SetActive(false);
        RefreshBottomTabs();
        RefreshInstallPanel();
        RefreshPlacementActionOverlay();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveReferences();
        ApplySharedPanelPauseState();
        UpdateSharedPanelMotion();
        RefreshPlacementActionOverlay();
        UpdateToastFade();

        if (Time.unscaledTime < nextDataRefreshAt)
        {
            return;
        }

        nextDataRefreshAt = Time.unscaledTime + 0.5f;
        RefreshHud();
        RefreshOperatePanel();
    }

    private void OnDestroy()
    {
        UnbindInstallTutorialEvents();
        UnbindMonthlySettlementPopupEvents();

        if (boundStaffManager == null)
        {
            return;
        }

        boundStaffManager.ApplicantsChanged -= HandleStaffDataChanged;
        boundStaffManager.HiredStaffChanged -= HandleStaffDataChanged;
        boundStaffManager = null;
    }

    private void ResolveReferences()
    {
        walletManager ??= FindFirstObjectByType<WalletManager>();
        timeManager ??= FindFirstObjectByType<TimeManager>();
        economyManager ??= FindFirstObjectByType<GymEconomyManager>();
        siteManager ??= FindFirstObjectByType<GymSiteManager>();
        equipmentCatalog ??= FindFirstObjectByType<EquipmentCatalog>();
        placementManager ??= FindFirstObjectByType<PlacementManager>();
        staffManager ??= FindFirstObjectByType<StaffManager>();
    }

    private bool TryBindExistingUi()
    {
        Transform root = transform.Find("RuntimeGameUIRoot");
        if (root == null)
        {
            return false;
        }

        runtimeRoot = root;
        gymSceneRoot = FindDeepChild(root, "GymSceneRoot");
        floorRoot = FindDeepChild(root, "FloorRoot");
        wallRoot = FindDeepChild(root, "WallRoot");
        equipmentRoot = FindDeepChild(root, "EquipmentRoot");
        decorationRoot = FindDeepChild(root, "DecorationRoot");
        characterRoot = FindDeepChild(root, "CharacterRoot");
        topHudRoot = FindDeepChild(root, "TopHUDRoot");
        sharedPanelRoot = FindDeepChild(root, "SharedContentPanelRoot");
        sharedPanelContentRoot = FindDeepChild(root, "SharedPanelContentRoot");
        operatePanelRoot = FindDeepChild(root, "OperatePanelRoot");
        installPanelRoot = FindDeepChild(root, "InstallPanelRoot");
        installListRoot = FindDeepChild(root, "EquipmentCardList");
        comingSoonPanelRoot = FindDeepChild(root, "ComingSoonPanelRoot");
        economyPanelRoot = FindDeepChild(root, "EconomyPanelRoot");
        reviewPanelRoot = FindDeepChild(root, "ReviewPanelRoot");
        bottomNavRoot = FindDeepChild(root, "BottomNavRoot");
        toastRoot = FindDeepChild(root, "RuntimeToast");
        placementActionRoot = FindDeepChild(root, "PlacementActionRoot");

        dateValueText = FindDeepComponent<Text>(root, "Value", "DateBox");
        ConfigureDateHudValueText();
        branchValueText = FindDeepComponent<Text>(root, "Value", "BranchBox");
        cashValueText = FindDeepComponent<Text>(root, "Value", "MoneyBox");
        starCoinValueText = FindDeepComponent<Text>(root, "Value", "StarCoinBox");
        memberValueText = FindDeepComponent<Text>(root, "Value", "MemberBox");
        ConfigureMemberHudValueText();
        sharedPanelTitleText = FindDeepComponent<Text>(root, "SharedPanelTitle");
        operateGoalText = FindDeepComponent<Text>(root, "Value", "GoalCard");
        operateStatusText = FindDeepComponent<Text>(root, "Value", "StatusCard");
        operateCrowdText = FindDeepComponent<Text>(root, "Value", "CrowdCard");
        operateCleanText = FindDeepComponent<Text>(root, "Value", "CleanCard");
        operateUsageText = FindDeepComponent<Text>(root, "Value", "UsageCard");
        operateWaitText = FindDeepComponent<Text>(root, "Value", "WaitCard");
        operateStaffText = FindDeepComponent<Text>(root, "Value", "StaffCard");
        operateRevenueText = FindDeepComponent<Text>(root, "Value", "RevenueCard");
        selectedItemNameText = FindDeepComponent<Text>(root, "SelectedName");
        selectedItemPriceText = FindDeepComponent<Text>(root, "SelectedPrice");
        selectedItemDescText = FindDeepComponent<Text>(root, "SelectedDesc");
        comingSoonText = FindDeepComponent<Text>(root, "ComingSoonMessage");
        toastText = FindDeepComponent<Text>(root, "Message", "ToastFrame");
        EnsureToastCanvasGroup();
        placementEyebrowText = FindDeepComponent<Text>(root, "PlacementActionEyebrow");
        placementTitleText = FindDeepComponent<Text>(root, "PlacementActionTitle");
        placementStatusText = FindDeepComponent<Text>(root, "PlacementActionStatus");
        placementDetailText = FindDeepComponent<Text>(root, "PlacementActionDetail");
        selectedItemIconImage = FindDeepComponent<Image>(root, "SelectedIcon");

        operateTabButton = FindDeepComponent<Button>(root, "OperateTabButton");
        installTabButton = FindDeepComponent<Button>(root, "InstallTabButton");
        economyTabButton = FindDeepComponent<Button>(root, "EconomyTabButton");
        reviewTabButton = FindDeepComponent<Button>(root, "ReviewTabButton");
        operateTabText = FindDeepComponent<Text>(root, "Label", "OperateTabButton");
        installTabText = FindDeepComponent<Text>(root, "Label", "InstallTabButton");
        economyTabText = FindDeepComponent<Text>(root, "Label", "EconomyTabButton");
        reviewTabText = FindDeepComponent<Text>(root, "Label", "ReviewTabButton");

        categoryButtons.Clear();
        categoryTexts.Clear();
        for (int i = 0; i < InstallCategoryLabels.Length; i++)
        {
            Transform category = FindDeepChild(root, $"CategoryTab_{InstallCategoryLabels[i]}");
            if (category == null)
            {
                continue;
            }

            Button button = category.GetComponent<Button>();
            Text text = FindDeepComponent<Text>(category, "Label");
            if (button != null)
            {
                categoryButtons.Add(button);
                categoryTexts.Add(text);
            }
        }

        return gymSceneRoot != null &&
               topHudRoot != null &&
               sharedPanelRoot != null &&
               sharedPanelContentRoot != null &&
               operatePanelRoot != null &&
               installPanelRoot != null &&
               installListRoot != null &&
               bottomNavRoot != null &&
               operateTabButton != null &&
               installTabButton != null &&
               sharedPanelTitleText != null;
    }

    private void BindStaticButtons()
    {
        BindButton(FindDeepComponent<Button>(runtimeRoot, "StaffButton"), OpenStaffPopup);
        BindButton(FindDeepComponent<Button>(runtimeRoot, "MenuButton"), OpenMenuPopup);
        BindButton(operateTabButton, ToggleOperatePanel);
        BindButton(installTabButton, ToggleInstallPanel);
        BindButton(economyTabButton, ToggleEconomyPanel);
        BindButton(reviewTabButton, ToggleReviewPanel);

        for (int i = 0; i < categoryButtons.Count; i++)
        {
            int index = i;
            BindButton(categoryButtons[i], () =>
            {
                selectedCategoryIndex = index;
                selectedDefinition = GetFirstDefinitionForCategory(InstallCategories[selectedCategoryIndex]);
                RefreshInstallPanel();
            });
        }

        BindButton(FindDeepComponent<Button>(runtimeRoot, "InstallButton"), EnterPlacementReady);
        BindButton(FindDeepComponent<Button>(runtimeRoot, "CancelButton"), CancelInstallSelection);
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

    private static T FindDeepComponent<T>(Transform root, string objectName, string parentName = null)
        where T : Component
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

        if (root.name == objectName && (string.IsNullOrEmpty(parentName) || HasParentNamed(root, parentName)))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindDeepChild(child, objectName, parentName);
            if (found != null)
            {
                return found;
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

    private void BuildGymSceneRoot()
    {
        gymSceneRoot = GameUiFactory.CreateNode(runtimeRoot, "GymSceneRoot").transform;
        GameUiFactory.Stretch(gymSceneRoot.GetComponent<RectTransform>(), 0f, 0f, 204f, 184f);

        floorRoot = GameUiFactory.CreateNode(gymSceneRoot, "FloorRoot").transform;
        wallRoot = GameUiFactory.CreateNode(gymSceneRoot, "WallRoot").transform;
        equipmentRoot = GameUiFactory.CreateNode(gymSceneRoot, "EquipmentRoot").transform;
        decorationRoot = GameUiFactory.CreateNode(gymSceneRoot, "DecorationRoot").transform;
        characterRoot = GameUiFactory.CreateNode(gymSceneRoot, "CharacterRoot").transform;

        GameUiFactory.Stretch(floorRoot.GetComponent<RectTransform>());
        GameUiFactory.Stretch(wallRoot.GetComponent<RectTransform>());
        GameUiFactory.Stretch(equipmentRoot.GetComponent<RectTransform>());
        GameUiFactory.Stretch(decorationRoot.GetComponent<RectTransform>());
        GameUiFactory.Stretch(characterRoot.GetComponent<RectTransform>());

        BuildGymVisualStage();
    }

    private void BuildGymVisualStage()
    {
        CreateSolid(wallRoot, "EmptyWorldBackdrop", new Color(0.18f, 0.27f, 0.40f, 1f), 0f, 360f, 1020f, 760f, true);
    }

    private void BuildTopHud()
    {
        topHudRoot = GameUiFactory.CreateNode(runtimeRoot, "TopHUDRoot").transform;
        SetRect(topHudRoot.GetComponent<RectTransform>(), 0f, 1822f, 1056f, 190f);

        GameObject frame = CreateGeneratedImage(topHudRoot, "TopHUDFrame", "GeneratedRuntimeUI/ui_v2/hud_base_bar", 0f, 0f, 1056f, 190f, false, true);

        dateValueText = CreateHudBox(frame.transform, "DateBox", "날짜", -410f, 0f, 188f, 124f);
        ConfigureDateHudValueText();

        branchValueText = CreateHudBox(frame.transform, "BranchBox", "지점", -208f, 0f, 202f, 124f);
        cashValueText = CreateHudBox(frame.transform, "MoneyBox", "자금", -40f, 0f, 126f, 124f);
        starCoinValueText = CreateHudBox(frame.transform, "StarCoinBox", "스타코인", 94f, 0f, 126f, 124f);
        memberValueText = CreateHudBox(frame.transform, "MemberBox", "회원", 228f, 0f, 126f, 124f);
        ConfigureMemberHudValueText();
        CreateHudButton(frame.transform, "StaffButton", "직원", 350f, 0f, 104f, 124f, OpenStaffPopup);
        CreateHudButton(frame.transform, "MenuButton", "메뉴", 456f, 0f, 104f, 124f, OpenMenuPopup);
    }

    private void BuildSharedContentPanel()
    {
        sharedPanelRoot = GameUiFactory.CreateNode(runtimeRoot, "SharedContentPanelRoot").transform;
        SetRect(sharedPanelRoot.GetComponent<RectTransform>(), 0f, 548f, 1040f, 752f);

        GameObject frame = CreateGeneratedImage(sharedPanelRoot, "SharedPanelFrame", "GeneratedRuntimeUI/ui_v2/panel_large_base", 0f, 0f, 1040f, 752f, false, true);
        CreateGeneratedImage(frame.transform, "SharedPanelHeaderBar", "GeneratedRuntimeUI/ui_v2/header_bar_blue", 0f, 390f, 952f, 74f, false, true);
        sharedPanelTitleText = CreateText(frame.transform, "SharedPanelTitle", "운영 현황", 45, Color.white, TextAnchor.MiddleCenter, 0f, 390f, 870f, 64f, true);

        sharedPanelContentRoot = GameUiFactory.CreateNode(frame.transform, "SharedPanelContentRoot").transform;
        SetRect(sharedPanelContentRoot.GetComponent<RectTransform>(), 0f, -30f, 1010f, 682f, true);
    }

    private void BuildOperateContent()
    {
        operatePanelRoot = GameUiFactory.CreateNode(sharedPanelContentRoot, "OperatePanelRoot").transform;
        SetRect(operatePanelRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f, true);

        operateGoalText = CreateOperateCard(operatePanelRoot, "GoalCard", -253f, 226f, "오늘의 목표", "GeneratedRuntimeUI/objects/wall_poster");
        operateStatusText = CreateOperateCard(operatePanelRoot, "StatusCard", 253f, 226f, "운영 상태", "GeneratedRuntimeUI/objects/potted_plant");
        operateCrowdText = CreateOperateCard(operatePanelRoot, "CrowdCard", -253f, 78f, "혼잡도", "GeneratedRuntimeUI/objects/door");
        operateCleanText = CreateOperateCard(operatePanelRoot, "CleanCard", 253f, 78f, "청결", "GeneratedRuntimeUI/objects/water_cooler");
        operateUsageText = CreateOperateCard(operatePanelRoot, "UsageCard", -253f, -70f, "기구 사용률", "GeneratedRuntimeUI/objects/treadmill");
        operateWaitText = CreateOperateCard(operatePanelRoot, "WaitCard", 253f, -70f, "대기 회원", "GeneratedRuntimeUI/objects/window");
        operateStaffText = CreateOperateCard(operatePanelRoot, "StaffCard", -253f, -218f, "직원 상태", "GeneratedRuntimeUI/objects/reception_desk");
        operateRevenueText = CreateOperateCard(operatePanelRoot, "RevenueCard", 253f, -218f, "오늘 수익", "GeneratedRuntimeUI/ui_v2/category_tab_small_base");
    }

    private void BuildInstallContent()
    {
        installPanelRoot = GameUiFactory.CreateNode(sharedPanelContentRoot, "InstallPanelRoot").transform;
        SetRect(installPanelRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f, true);

        BuildInstallCategoryTabs();

        installListRoot = GameUiFactory.CreateNode(installPanelRoot, "EquipmentCardList").transform;
        SetRect(installListRoot.GetComponent<RectTransform>(), 0f, 92f, 1010f, 420f, true);

        GameObject selectedFrame = CreateGeneratedImage(installPanelRoot, "SelectedInfoBox", "GeneratedRuntimeUI/ui_v2/selected_info_box_base", 0f, -214f, 982f, 198f, false, true);
        CreateGeneratedImage(selectedFrame.transform, "SelectedIconBack", "GeneratedRuntimeUI/ui_v2/button_small_beige_base", -430f, 0f, 122f, 122f, false, true);
        selectedItemIconImage = CreateGeneratedImage(selectedFrame.transform, "SelectedIcon", "GeneratedRuntimeUI/objects/treadmill", -430f, 2f, 100f, 98f, true, true).GetComponent<Image>();
        selectedItemNameText = CreateText(selectedFrame.transform, "SelectedName", "선택 아이템: -", 26, theme.Ink, TextAnchor.MiddleLeft, -76f, 58f, 586f, 38f, true);
        selectedItemPriceText = CreateText(selectedFrame.transform, "SelectedPrice", "가격: -", 24, theme.Ink, TextAnchor.MiddleLeft, -76f, 18f, 586f, 34f, true);
        selectedItemDescText = CreateText(selectedFrame.transform, "SelectedDesc", "설치할 기구를 선택하세요.", 18, theme.MutedInk, TextAnchor.MiddleLeft, -76f, -44f, 586f, 72f, true);

        CreateInstallActionButton(selectedFrame.transform, "InstallButton", "설치하기", 374f, 48f, 200f, 64f, GameUiTone.Positive, EnterPlacementReady);
        CreateInstallActionButton(selectedFrame.transform, "CancelButton", "취소", 374f, -42f, 200f, 52f, GameUiTone.Surface, CancelInstallSelection);

        installPanelRoot.gameObject.SetActive(false);
    }

    private void BuildInstallCategoryTabs()
    {
        categoryButtons.Clear();
        categoryTexts.Clear();
        for (int i = 0; i < InstallCategoryLabels.Length; i++)
        {
            int index = i;
            Text labelText;
            Button button = CreateSpriteButton(
                installPanelRoot,
                $"CategoryTab_{InstallCategoryLabels[i]}",
                "GeneratedRuntimeUI/ui_v2/category_tab_beige_base",
                InstallCategoryLabels[i],
                InstallCategoryTabStartX + (i * InstallCategoryTabSpacingX),
                InstallCategoryTabY,
                InstallCategoryTabWidth,
                InstallCategoryTabHeight,
                theme.Ink,
                out labelText,
                InstallCategoryTabFontSize);
            button.onClick.AddListener(() =>
            {
                selectedCategoryIndex = index;
                selectedDefinition = GetFirstDefinitionForCategory(InstallCategories[selectedCategoryIndex]);
                RefreshInstallPanel();
            });
            categoryButtons.Add(button);
            categoryTexts.Add(labelText);
            ApplyInstallCategoryTabStyle(button, labelText, i);
        }
    }

    private void EnsureInstallCategoryTabs()
    {
        if (installPanelRoot == null)
        {
            return;
        }

        bool needsRebuild = categoryButtons.Count != InstallCategoryLabels.Length;
        for (int i = 0; i < InstallCategoryLabels.Length && !needsRebuild; i++)
        {
            needsRebuild = FindDeepChild(installPanelRoot, $"CategoryTab_{InstallCategoryLabels[i]}") == null;
        }

        if (!needsRebuild)
        {
            for (int i = 0; i < categoryButtons.Count; i++)
            {
                Text labelText = i < categoryTexts.Count ? categoryTexts[i] : null;
                ApplyInstallCategoryTabStyle(categoryButtons[i], labelText, i);
            }
            return;
        }

        for (int i = installPanelRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = installPanelRoot.GetChild(i);
            if (child.name.StartsWith("CategoryTab_"))
            {
                GameUiFactory.DestroyObject(child.gameObject);
            }
        }

        BuildInstallCategoryTabs();
    }

    private void ApplyInstallCategoryTabStyle(Button button, Text labelText, int index)
    {
        if (button != null)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                SetRect(
                    rect,
                    InstallCategoryTabStartX + (index * InstallCategoryTabSpacingX),
                    InstallCategoryTabY,
                    InstallCategoryTabWidth,
                    InstallCategoryTabHeight,
                    true);
            }
        }

        if (labelText == null)
        {
            return;
        }

        labelText.fontStyle = FontStyle.Normal;
        labelText.fontSize = InstallCategoryTabFontSize;
        labelText.lineSpacing = 1f;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = labelText.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.16f);
            outline.effectDistance = new Vector2(0.45f, -0.45f);
        }

        Shadow shadow = labelText.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.12f);
            shadow.effectDistance = new Vector2(0.45f, -0.45f);
        }
    }

    private void BuildComingSoonContent()
    {
        comingSoonPanelRoot = GameUiFactory.CreateNode(sharedPanelContentRoot, "ComingSoonPanelRoot").transform;
        SetRect(comingSoonPanelRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f, true);
        comingSoonText = CreateText(comingSoonPanelRoot, "ComingSoonMessage", "", 30, theme.Ink, TextAnchor.MiddleCenter, 0f, 0f, 760f, 180f, true);
        comingSoonPanelRoot.gameObject.SetActive(false);
    }


    private void EnsureEconomyPanelContent()
    {
        if (sharedPanelContentRoot == null)
        {
            return;
        }

        if (economyPanelRoot == null)
        {
            economyPanelRoot = FindDeepChild(runtimeRoot, "EconomyPanelRoot");
        }

        if (economyPanelRoot == null)
        {
            economyPanelRoot = GameUiFactory.CreateNode(sharedPanelContentRoot, "EconomyPanelRoot").transform;
            SetRect(economyPanelRoot.GetComponent<RectTransform>(), 0f, 0f, 1010f, 682f, true);
        }

        if (FindDeepChild(economyPanelRoot, "EconomyIncomeSummaryCard") != null)
        {
            economyPanelRoot.gameObject.SetActive(false);
            return;
        }

        GameUiFactory.ClearChildren(economyPanelRoot);
        BuildEconomyContent();
        economyPanelRoot.gameObject.SetActive(false);
    }

    private void BuildEconomyContent()
    {
        Color positive = new Color(0.05f, 0.45f, 0.13f, 1f);
        Color negative = new Color(0.77f, 0.12f, 0.08f, 1f);

        CreateEconomySummaryCard("EconomyIncomeSummaryCard", -378f, 246f, "오늘 수익", "8,400G", positive);
        CreateEconomySummaryCard("EconomyExpenseSummaryCard", -126f, 246f, "오늘 지출", "2,100G", negative);
        CreateEconomySummaryCard("EconomyProfitSummaryCard", 126f, 246f, "순이익", "+6,300G", positive);
        CreateEconomySummaryCard("EconomyCashSummaryCard", 378f, 246f, "보유 자금", "32,500G", theme.Ink);

        GameObject incomeBox = CreateGeneratedImage(economyPanelRoot, "EconomyIncomeDetailBox", "GeneratedRuntimeUI/ui_v2/install_card_base", -253f, 42f, 488f, 218f, false, true);
        CreateText(incomeBox.transform, "Title", "수입 상세", 28, positive, TextAnchor.MiddleLeft, -176f, 78f, 260f, 38f, true);
        CreateSolid(incomeBox.transform, "DividerTop", new Color(0.42f, 0.28f, 0.10f, 0.60f), 0f, 52f, 388f, 3f, true);
        CreateText(incomeBox.transform, "MembershipLabel", "회원권 수익", 26, theme.Ink, TextAnchor.MiddleLeft, -154f, 16f, 210f, 32f, true);
        CreateText(incomeBox.transform, "MembershipValue", "6,000G", 26, theme.Ink, TextAnchor.MiddleRight, 126f, 16f, 170f, 32f, true);
        CreateText(incomeBox.transform, "DailyLabel", "일일 입장 수익", 26, theme.Ink, TextAnchor.MiddleLeft, -154f, -34f, 230f, 32f, true);
        CreateText(incomeBox.transform, "DailyValue", "2,400G", 26, theme.Ink, TextAnchor.MiddleRight, 126f, -34f, 170f, 32f, true);
        CreateSolid(incomeBox.transform, "DividerBottom", new Color(0.42f, 0.28f, 0.10f, 0.60f), 0f, -68f, 388f, 3f, true);
        CreateText(incomeBox.transform, "TotalLabel", "수입 합계", 27, positive, TextAnchor.MiddleLeft, -154f, -96f, 210f, 36f, true);
        CreateText(incomeBox.transform, "TotalValue", "8,400G", 27, positive, TextAnchor.MiddleRight, 126f, -96f, 170f, 36f, true);

        GameObject expenseBox = CreateGeneratedImage(economyPanelRoot, "EconomyExpenseDetailBox", "GeneratedRuntimeUI/ui_v2/install_card_base", 253f, 42f, 488f, 218f, false, true);
        CreateText(expenseBox.transform, "Title", "지출 상세", 28, negative, TextAnchor.MiddleLeft, -176f, 78f, 260f, 38f, true);
        CreateSolid(expenseBox.transform, "DividerTop", new Color(0.42f, 0.28f, 0.10f, 0.60f), 0f, 52f, 388f, 3f, true);
        CreateText(expenseBox.transform, "SalaryLabel", "직원 급여", 26, theme.Ink, TextAnchor.MiddleLeft, -154f, 16f, 210f, 32f, true);
        CreateText(expenseBox.transform, "SalaryValue", "1,200G", 26, theme.Ink, TextAnchor.MiddleRight, 126f, 16f, 170f, 32f, true);
        CreateText(expenseBox.transform, "MaintenanceLabel", "유지비", 26, theme.Ink, TextAnchor.MiddleLeft, -154f, -34f, 210f, 32f, true);
        CreateText(expenseBox.transform, "MaintenanceValue", "900G", 26, theme.Ink, TextAnchor.MiddleRight, 126f, -34f, 170f, 32f, true);
        CreateSolid(expenseBox.transform, "DividerBottom", new Color(0.42f, 0.28f, 0.10f, 0.60f), 0f, -68f, 388f, 3f, true);
        CreateText(expenseBox.transform, "TotalLabel", "지출 합계", 27, negative, TextAnchor.MiddleLeft, -154f, -96f, 210f, 36f, true);
        CreateText(expenseBox.transform, "TotalValue", "2,100G", 27, negative, TextAnchor.MiddleRight, 126f, -96f, 170f, 36f, true);

        GameObject chartBox = CreateGeneratedImage(economyPanelRoot, "EconomyChartBox", "GeneratedRuntimeUI/ui_v2/install_card_base", -253f, -200f, 488f, 218f, false, true);
        CreateText(chartBox.transform, "Title", "최근 7일 수익 추이", 28, theme.Ink, TextAnchor.MiddleLeft, -156f, 76f, 320f, 40f, true);
        CreateSolid(chartBox.transform, "Axis", new Color(0.25f, 0.16f, 0.07f, 0.70f), -8f, -58f, 360f, 4f, true);
        string[] dates = { "3/1", "3/2", "3/3", "3/4", "3/5", "3/6", "3/7" };
        float[] heights = { 68f, 80f, 56f, 84f, 62f, 80f, 110f };
        for (int i = 0; i < dates.Length; i++)
        {
            float x = -158f + i * 52f;
            Color barColor = i == dates.Length - 1 ? new Color(0.98f, 0.67f, 0.12f, 1f) : new Color(0.35f, 0.78f, 0.19f, 1f);
            CreateSolid(chartBox.transform, "Bar_" + i, barColor, x, -58f + heights[i] * 0.5f, 28f, heights[i], true);
            CreateText(chartBox.transform, "Date_" + i, dates[i], 20, theme.Ink, TextAnchor.MiddleCenter, x, -88f, 48f, 26f, true);
        }

        GameObject memoBox = CreateGeneratedImage(economyPanelRoot, "EconomyMemoBox", "GeneratedRuntimeUI/ui_v2/install_card_base", 253f, -200f, 488f, 218f, false, true);
        CreateText(memoBox.transform, "Title", "메모", 28, theme.Ink, TextAnchor.MiddleLeft, -176f, 76f, 220f, 40f, true);
        CreateSolid(memoBox.transform, "Divider", new Color(0.42f, 0.28f, 0.10f, 0.60f), 18f, 46f, 330f, 3f, true);
        CreateText(memoBox.transform, "MemoText", "회원 증가로\n수익이 안정적으로\n오르고 있어요!", 28, theme.Ink, TextAnchor.MiddleLeft, -156f, -18f, 250f, 130f, true);
        CreateGeneratedImage(memoBox.transform, "ManagerCharacter", "GeneratedRuntimeUI/ui_v2/economy/economy_manager_character", 154f, -16f, 140f, 170f, true, true);
    }

    private void CreateEconomySummaryCard(string name, float x, float y, string title, string value, Color valueColor)
    {
        GameObject card = CreateGeneratedImage(economyPanelRoot, name, "GeneratedRuntimeUI/ui_v2/install_card_base", x, y, 235f, 132f, false, true);
        CreateText(card.transform, "Label", title, 24, theme.Ink, TextAnchor.MiddleCenter, 0f, 24f, 190f, 32f, true);
        CreateText(card.transform, "Value", value, 34, valueColor, TextAnchor.MiddleCenter, 0f, -26f, 190f, 44f, true);
    }

    private void BuildBottomNav()
    {
        bottomNavRoot = GameUiFactory.CreateNode(runtimeRoot, "BottomNavRoot").transform;
        SetRect(bottomNavRoot.GetComponent<RectTransform>(), 0f, 86f, 1056f, 168f);

        GameObject frame = CreateGeneratedImage(bottomNavRoot, "BottomNavFrame", "GeneratedRuntimeUI/ui_v2/nav_base_bar", 0f, 0f, 1056f, 168f, false, true);
        operateTabButton = CreateTabButton(frame.transform, "OperateTabButton", "운영", "GeneratedRuntimeUI/objects/wall_poster", -384f, PanelMode.Operate, ToggleOperatePanel, out operateTabText);
        installTabButton = CreateTabButton(frame.transform, "InstallTabButton", "설치", "GeneratedRuntimeUI/objects/dumbbell_rack", -128f, PanelMode.Install, ToggleInstallPanel, out installTabText);
        economyTabButton = CreateTabButton(frame.transform, "EconomyTabButton", "경제", "GeneratedRuntimeUI/ui_v2/category_tab_small_base", 128f, PanelMode.Economy, ToggleEconomyPanel, out economyTabText);
        reviewTabButton = CreateTabButton(frame.transform, "ReviewTabButton", "리뷰", "GeneratedRuntimeUI/objects/wall_poster", 384f, PanelMode.Review, ToggleReviewPanel, out reviewTabText);
    }

    private void BuildToast()
    {
        toastRoot = GameUiFactory.CreateNode(runtimeRoot, "RuntimeToast").transform;
        SetRect(toastRoot.GetComponent<RectTransform>(), 0f, 1060f, 700f, 250f);
        EnsureToastCanvasGroup();
        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
        }

        GameObject frame = CreateGeneratedImage(toastRoot, "ToastFrame", "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base", 0f, 0f, 700f, 250f, false, true);
        toastText = CreateText(frame.transform, "Message", "", 26, theme.Ink, TextAnchor.MiddleCenter, 0f, 0f, 610f, 172f, true);
        toastRoot.gameObject.SetActive(false);
    }

    private void EnsureStaffPopup()
    {
        if (!Application.isPlaying || runtimeRoot == null)
        {
            return;
        }

        if (staffPopupRoot == null)
        {
            staffPopupRoot = FindDeepChild(runtimeRoot, "StaffPopupRoot");
        }

        if (staffPopupRoot == null)
        {
            staffPopupRoot = GameUiFactory.CreateNode(runtimeRoot, "StaffPopupRoot").transform;
            SetRect(staffPopupRoot.GetComponent<RectTransform>(), 0f, 960f, 1080f, 1920f);
            BuildStaffPopup();
        }
        else
        {
            BindExistingStaffPopup();
            if (FindDeepChild(staffPopupRoot, "StaffWindowFrame") == null ||
                FindDeepChild(staffPopupRoot, "StaffListViewport") == null)
            {
                GameUiFactory.ClearChildren(staffPopupRoot);
                BuildStaffPopup();
            }
        }

        BindStaffManagerEvents();
        BindButton(staffRecruitTabButton, () => SetStaffPopupTab(true));
        BindButton(staffCurrentTabButton, () => SetStaffPopupTab(false));
        BindButton(FindDeepComponent<Button>(staffPopupRoot, "StaffCloseButton"), CloseStaffPopup);
        staffPopupRoot.gameObject.SetActive(false);
    }

    private void BuildStaffPopup()
    {
        GameObject dim = CreateSolid(staffPopupRoot, "StaffPopupDim", new Color(0f, 0f, 0f, 0.32f), 0f, 0f, 1080f, 1920f, true);
        Image dimImage = dim.GetComponent<Image>();
        dimImage.raycastTarget = true;
        Button dimButton = dim.AddComponent<Button>();
        dimButton.targetGraphic = dimImage;
        dimButton.onClick.AddListener(CloseStaffPopup);

        GameObject frame = CreateGeneratedImage(staffPopupRoot, "StaffWindowFrame", "GeneratedRuntimeUI/ui_v2/staff/staff_window_base", 0f, 0f, 770f, 1010f, false, true);
        CreateStaffText(frame.transform, "StaffPopupTitle", "직원 관리", 46, theme.Ink, TextAnchor.MiddleCenter, 0f, 406f, 360f, 66f, true);

        GameObject closeNode = CreateGeneratedImage(frame.transform, "StaffCloseButton", "GeneratedRuntimeUI/ui_v2/staff/staff_close_button", 310f, 394f, 86f, 86f, true, true);
        Image closeImage = closeNode.GetComponent<Image>();
        closeImage.raycastTarget = true;
        Button closeButton = closeNode.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(CloseStaffPopup);

        staffRecruitTabButton = CreateSpriteButton(frame.transform, "StaffRecruitTabButton", "GeneratedRuntimeUI/ui_v2/button_green_base", "채용", -170f, 292f, 300f, 88f, theme.BrightInk, out staffRecruitTabText, 36);
        staffCurrentTabButton = CreateSpriteButton(frame.transform, "StaffCurrentTabButton", "GeneratedRuntimeUI/ui_v2/button_beige_base", "현재 직원", 170f, 292f, 300f, 88f, theme.Ink, out staffCurrentTabText, 36);
        SetStaffTextNormal(staffRecruitTabText);
        SetStaffTextNormal(staffCurrentTabText);

        GameObject viewport = GameUiFactory.CreateNode(frame.transform, "StaffListViewport", typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
        SetRect(viewport.GetComponent<RectTransform>(), 0f, -76f, 670f, 560f, true);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewportImage.raycastTarget = true;
        Mask viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        staffPopupScrollRect = viewport.GetComponent<ScrollRect>();
        staffPopupScrollRect.horizontal = false;
        staffPopupScrollRect.vertical = true;
        staffPopupScrollRect.movementType = ScrollRect.MovementType.Clamped;
        staffPopupScrollRect.inertia = true;
        staffPopupScrollRect.scrollSensitivity = 34f;
        staffPopupScrollRect.viewport = viewport.GetComponent<RectTransform>();

        staffPopupListRoot = GameUiFactory.CreateNode(viewport.transform, "StaffListRoot").transform;
        ConfigureStaffListContent(560f);
        staffPopupScrollRect.content = staffPopupListRoot.GetComponent<RectTransform>();

        CreateSolid(frame.transform, "StaffPopupFooterDivider", new Color(0.42f, 0.28f, 0.10f, 0.45f), 0f, -420f, 624f, 3f, true);
        CreateStaffText(frame.transform, "StaffPopupHint", "지원자를 채용하면 매장에 바로 배치됩니다", 25, theme.MutedInk, TextAnchor.MiddleCenter, 0f, -442f, 640f, 42f, true);

        BindExistingStaffPopup();
    }

    private void BindExistingStaffPopup()
    {
        staffPopupListRoot = FindDeepChild(staffPopupRoot, "StaffListRoot");
        staffRecruitTabButton = FindDeepComponent<Button>(staffPopupRoot, "StaffRecruitTabButton");
        staffCurrentTabButton = FindDeepComponent<Button>(staffPopupRoot, "StaffCurrentTabButton");
        staffRecruitTabText = FindDeepComponent<Text>(staffPopupRoot, "Label", "StaffRecruitTabButton");
        staffCurrentTabText = FindDeepComponent<Text>(staffPopupRoot, "Label", "StaffCurrentTabButton");
        staffPopupScrollRect = FindDeepComponent<ScrollRect>(staffPopupRoot, "StaffListViewport");
    }

    private void OpenStaffPopup()
    {
        ResolveReferences();
        EnsureStaffPopup();
        if (staffPopupRoot == null)
        {
            ShowToast("직원 시스템을 찾을 수 없습니다.");
            return;
        }

        if (staffManager != null && staffShowingApplicants && staffManager.AvailableApplicants.Count == 0)
        {
            staffManager.RefreshApplicants();
        }

        staffPopupRoot.gameObject.SetActive(true);
        staffPopupRoot.SetAsLastSibling();
        HideToast();
        RefreshStaffPopup();
    }

    private void CloseStaffPopup()
    {
        if (staffPopupRoot != null)
        {
            staffPopupRoot.gameObject.SetActive(false);
        }
    }

    private void SetStaffPopupTab(bool showApplicants)
    {
        if (staffShowingApplicants == showApplicants)
        {
            RefreshStaffTabVisuals();
            return;
        }

        staffShowingApplicants = showApplicants;
        RefreshStaffPopup();
    }

    private void RefreshStaffPopup()
    {
        if (staffPopupRoot == null)
        {
            return;
        }

        RefreshStaffTabVisuals();
        RefreshStaffRows();
    }

    private void RefreshStaffTabVisuals()
    {
        RefreshStaffTab(staffRecruitTabButton, staffRecruitTabText, staffShowingApplicants);
        RefreshStaffTab(staffCurrentTabButton, staffCurrentTabText, !staffShowingApplicants);
    }

    private void RefreshStaffTab(Button button, Text label, bool active)
    {
        if (button != null)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                GeneratedRuntimeSprites.Assign(image, active ? "GeneratedRuntimeUI/ui_v2/button_green_base" : "GeneratedRuntimeUI/ui_v2/button_beige_base", false);
            }
        }

        if (label != null)
        {
            label.color = active ? theme.BrightInk : theme.Ink;
            SetStaffTextNormal(label);
        }
    }

    private void RefreshStaffRows()
    {
        if (staffPopupListRoot == null)
        {
            return;
        }

        ClearStaffListChildren(staffPopupListRoot);

        if (staffManager == null)
        {
            CreateStaffEmptyMessage("직원 시스템 연결 대기");
            return;
        }

        IReadOnlyList<StaffData> source = staffShowingApplicants
            ? staffManager.AvailableApplicants
            : staffManager.HiredStaff;

        if (source == null || source.Count == 0)
        {
            CreateStaffEmptyMessage(staffShowingApplicants ? "채용 가능한 지원자가 없습니다." : "현재 고용 중인 직원이 없습니다.");
            return;
        }

        ConfigureStaffListContent(GetStaffListContentHeight(source.Count));
        for (int i = 0; i < source.Count; i++)
        {
            CreateStaffRow(source[i], staffShowingApplicants, i);
        }

        if (staffPopupScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            staffPopupScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void CreateStaffEmptyMessage(string message)
    {
        ConfigureStaffListContent(560f);
        GameObject emptyBox = CreateGeneratedImage(staffPopupListRoot, "StaffEmptyRow", "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base", 0f, 94f, 650f, 132f, false, true);
        SetStaffRowRect(emptyBox.GetComponent<RectTransform>(), 0, 650f, 132f);
        CreateStaffText(emptyBox.transform, "Message", message, 28, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 0f, 560f, 56f, true);
    }

    private void CreateStaffRow(StaffData staff, bool isApplicant, int index)
    {
        if (staff == null)
        {
            return;
        }

        GameObject row = CreateGeneratedImage(staffPopupListRoot, $"StaffRow_{index}", "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base", 0f, 0f, 650f, 128f, false, true);
        SetStaffRowRect(row.GetComponent<RectTransform>(), index, 650f, 128f);

        CreateGeneratedImage(row.transform, "StaffPortrait", GetStaffPortraitPath(staff), -252f, 0f, 92f, 92f, true, true);

        string staffName = string.IsNullOrWhiteSpace(staff.staffName) ? "이름 없음" : staff.staffName;
        CreateStaffText(row.transform, "StaffName", staffName, 31, theme.Ink, TextAnchor.MiddleLeft, -55f, 34f, 320f, 38f, true);
        CreateStaffText(row.transform, "StaffRole", GetStaffRoleLabel(staff.role), 24, theme.Ink, TextAnchor.MiddleLeft, -74f, -2f, 280f, 32f, true);
        CreateStaffText(row.transform, "StaffDailyPay", $"일급 {staff.monthlySalary:N0}G", 24, theme.Ink, TextAnchor.MiddleLeft, -74f, -36f, 280f, 32f, true);

        Text labelText;
        Button actionButton = CreateSpriteButton(row.transform, "StaffActionButton", "GeneratedRuntimeUI/ui_v2/button_beige_base", isApplicant ? "채용" : "해고", 238f, 0f, 136f, 64f, theme.Ink, out labelText, 29);
        SetStaffTextNormal(labelText);
        if (isApplicant)
        {
            StaffData applicant = staff;
            actionButton.onClick.AddListener(() =>
            {
                staffManager.HireApplicant(applicant);
                RefreshStaffPopup();
                RefreshHud();
                RefreshOperatePanel();
            });
        }
        else
        {
            string staffId = staff.staffId;
            actionButton.onClick.AddListener(() =>
            {
                staffManager.FireStaff(staffId);
                RefreshStaffPopup();
                RefreshHud();
                RefreshOperatePanel();
            });
        }
    }

    private void ConfigureStaffListContent(float height)
    {
        if (staffPopupListRoot == null)
        {
            return;
        }

        RectTransform rect = staffPopupListRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(670f, Mathf.Max(560f, height));
    }

    private static float GetStaffListContentHeight(int rowCount)
    {
        return Mathf.Max(560f, rowCount * 136f - 8f);
    }

    private static void SetStaffRowRect(RectTransform rect, int index, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 136f);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void ClearStaffListChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            child.SetActive(false);
            GameUiFactory.DestroyObject(child);
        }
    }

    private Text CreateStaffText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment, float x, float y, float width, float height, bool localParent)
    {
        Text text = CreateText(parent, name, value, fontSize, color, alignment, x, y, width, height, localParent);
        SetStaffTextNormal(text);
        return text;
    }

    private static void SetStaffTextNormal(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyle.Normal;

        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectDistance = new Vector2(0.55f, -0.55f);
        }

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectDistance = new Vector2(0.45f, -0.45f);
        }
    }

    private void BindStaffManagerEvents()
    {
        if (boundStaffManager == staffManager)
        {
            return;
        }

        if (boundStaffManager != null)
        {
            boundStaffManager.ApplicantsChanged -= HandleStaffDataChanged;
            boundStaffManager.HiredStaffChanged -= HandleStaffDataChanged;
        }

        boundStaffManager = staffManager;
        if (boundStaffManager == null)
        {
            return;
        }

        boundStaffManager.ApplicantsChanged += HandleStaffDataChanged;
        boundStaffManager.HiredStaffChanged += HandleStaffDataChanged;
    }

    private void HandleStaffDataChanged()
    {
        if (staffPopupRoot != null && staffPopupRoot.gameObject.activeInHierarchy)
        {
            RefreshStaffPopup();
        }

        RefreshHud();
        RefreshOperatePanel();
    }

    private string GetStaffRoleLabel(StaffRole role)
    {
        return staffManager != null ? staffManager.GetRoleNameKOR(role) : role.ToString();
    }

    private static string GetStaffPortraitPath(StaffData staff)
    {
        string gender = staff.gender == StaffGender.Female ? "female" : "male";
        int index = Mathf.Clamp(staff.portraitIndex, 0, 9);
        return $"GeneratedRuntimeUI/ui_v2/staff/portraits/{gender}/staff_{gender}_{index:00}";
    }

    private void EnsurePlacementActionOverlay()
    {
        if (!Application.isPlaying || runtimeRoot == null)
        {
            return;
        }

        if (placementActionRoot == null)
        {
            placementActionRoot = FindDeepChild(runtimeRoot, "PlacementActionRoot");
        }

        if (placementActionRoot == null)
        {
            placementActionRoot = GameUiFactory.CreateNode(runtimeRoot, "PlacementActionRoot").transform;
            SetRect(placementActionRoot.GetComponent<RectTransform>(), 0f, 402f, 700f, 286f);
        }

        GameUiFactory.ClearChildren(placementActionRoot);
        placementActionButtons.Clear();
        placementActionButtonTexts.Clear();

        GameObject frame = CreateGeneratedImage(placementActionRoot, "PlacementActionFrame", "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base", 0f, 0f, 700f, 260f, false, true);
        placementEyebrowText = CreateText(frame.transform, "PlacementActionEyebrow", "", 20, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 104f, 560f, 28f, true);
        placementTitleText = CreateText(frame.transform, "PlacementActionTitle", "", 32, theme.Ink, TextAnchor.MiddleCenter, 0f, 70f, 600f, 42f, true);
        placementStatusText = CreateText(frame.transform, "PlacementActionStatus", "", 25, theme.Ink, TextAnchor.MiddleCenter, 0f, 28f, 610f, 34f, true);
        placementDetailText = CreateText(frame.transform, "PlacementActionDetail", "", 21, theme.MutedInk, TextAnchor.MiddleCenter, 0f, -12f, 620f, 34f, true);

        for (int i = 0; i < 4; i++)
        {
            Text labelText;
            Button button = CreateSpriteButton(frame.transform, $"PlacementActionButton_{i}", "GeneratedRuntimeUI/ui_v2/button_beige_base", "", 0f, -88f, 156f, 58f, theme.Ink, out labelText, 26);
            placementActionButtons.Add(button);
            placementActionButtonTexts.Add(labelText);
            button.gameObject.SetActive(false);
        }

        placementActionRoot.gameObject.SetActive(false);
    }

    private void RefreshPlacementActionOverlay()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (placementActionRoot == null)
        {
            EnsurePlacementActionOverlay();
        }

        if (placementActionRoot == null)
        {
            return;
        }

        if (placementManager == null || !placementManager.TryGetPlacementHudState(out PlacementManager.SelectedObjectHudState state))
        {
            placementActionRoot.gameObject.SetActive(false);
            return;
        }

        placementActionRoot.gameObject.SetActive(true);
        RegisterPlacementActionBlocker();
        SetText(placementEyebrowText, state.eyebrow);
        SetText(placementTitleText, state.title);
        SetText(placementStatusText, state.status);
        SetText(placementDetailText, state.detail);

        PlacementManager.HudActionDescriptor[] actions =
        {
            state.primaryAction,
            state.secondaryAction,
            state.tertiaryAction,
            state.quaternaryAction
        };

        int visibleCount = 0;
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i].actionId != PlacementManager.HudActionId.None)
            {
                visibleCount++;
            }
        }

        float buttonWidth = visibleCount >= 4 ? 146f : 176f;
        float gap = 14f;
        float startX = -((buttonWidth * visibleCount) + (gap * Mathf.Max(0, visibleCount - 1))) * 0.5f + (buttonWidth * 0.5f);
        int visibleIndex = 0;

        for (int i = 0; i < placementActionButtons.Count; i++)
        {
            Button button = placementActionButtons[i];
            if (button == null)
            {
                continue;
            }

            if (i >= actions.Length || actions[i].actionId == PlacementManager.HudActionId.None)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            PlacementManager.HudActionDescriptor descriptor = actions[i];
            button.gameObject.SetActive(true);
            button.interactable = descriptor.isEnabled;

            RectTransform rect = button.GetComponent<RectTransform>();
            SetRect(rect, startX + visibleIndex * (buttonWidth + gap), -88f, buttonWidth, 58f, true);

            if (i < placementActionButtonTexts.Count)
            {
                SetText(placementActionButtonTexts[i], descriptor.label);
            }

            button.onClick.RemoveAllListeners();
            PlacementManager.HudActionId actionId = descriptor.actionId;
            button.onClick.AddListener(() => HandlePlacementAction(actionId));
            visibleIndex++;
        }
    }

    private void RegisterPlacementActionBlocker()
    {
        RectTransform rectTransform = placementActionRoot != null ? placementActionRoot.GetComponent<RectTransform>() : null;
        if (rectTransform == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float left = corners[0].x;
        float bottom = corners[0].y;
        float right = corners[2].x;
        float top = corners[2].y;
        Rect guiRect = new Rect(left, Screen.height - top, Mathf.Max(0f, right - left), Mathf.Max(0f, top - bottom));
        ScreenUiBlocker.RegisterRect(GetInstanceID(), guiRect);
    }

    private void HandlePlacementAction(PlacementManager.HudActionId actionId)
    {
        if (placementManager == null)
        {
            return;
        }

        bool succeeded = placementManager.ExecuteHudAction(actionId);
        if (!succeeded)
        {
            switch (actionId)
            {
                case PlacementManager.HudActionId.ConfirmPlacement:
                    ShowToast("이 위치에는 설치할 수 없거나 자금이 부족합니다.");
                    break;
                case PlacementManager.HudActionId.SkipConstruction:
                    ShowToast("스타코인이 부족합니다.");
                    break;
                case PlacementManager.HudActionId.Repair:
                    ShowToast("수리할 필요가 없거나 자금이 부족합니다.");
                    break;
                default:
                    ShowToast("지금은 실행할 수 없습니다.");
                    break;
            }
        }

        if (succeeded)
        {
            HideToast();
        }

        RefreshHud();
        RefreshInstallPanel();
        RefreshPlacementActionOverlay();
    }

    private Text CreateHudBox(Transform parent, string name, string label, float x, float y, float width, float height)
    {
        GameObject box = CreateGeneratedImage(parent, name, "GeneratedRuntimeUI/ui_v2/hud_info_box_base", x, y, width, height, false, true);
        CreateText(box.transform, "Label", label, 17, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 28f, width - 24f, 26f, true);
        return CreateText(box.transform, "Value", "-", 25, theme.Ink, TextAnchor.MiddleCenter, 0f, -16f, width - 24f, 46f, true);
    }

    private void ConfigureDateHudValueText()
    {
        if (dateValueText == null)
        {
            return;
        }

        dateValueText.fontSize = 22;
        dateValueText.resizeTextForBestFit = true;
        dateValueText.resizeTextMinSize = 18;
        dateValueText.resizeTextMaxSize = 22;
    }

    private void ConfigureMemberHudValueText()
    {
        if (memberValueText == null)
        {
            return;
        }

        memberValueText.fontSize = 22;
        memberValueText.resizeTextForBestFit = true;
        memberValueText.resizeTextMinSize = 17;
        memberValueText.resizeTextMaxSize = 22;
        memberValueText.horizontalOverflow = HorizontalWrapMode.Overflow;
        memberValueText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private Button CreateHudButton(Transform parent, string name, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
    {
        Text labelText;
        Button button = CreateSpriteButton(parent, name, "GeneratedRuntimeUI/ui_v2/button_beige_base", label, x, y, width, height, theme.Ink, out labelText, 27);
        labelText.fontSize = 26;
        button.onClick.AddListener(action);
        return button;
    }

    private Text CreateOperateCard(Transform parent, string name, float x, float y, string label, string iconPath)
    {
        GameObject card = CreateGeneratedImage(parent, name, "GeneratedRuntimeUI/ui_v2/install_card_base", x, y, 498f, 134f, false, true);
        CreateGeneratedImage(card.transform, "Icon", iconPath, -196f, 0f, 78f, 78f, true, true);
        CreateText(card.transform, "Label", label, 23, theme.MutedInk, TextAnchor.MiddleLeft, 68f, 28f, 346f, 34f, true);
        return CreateText(card.transform, "Value", "-", 32, theme.Ink, TextAnchor.MiddleLeft, 68f, -24f, 346f, 44f, true);
    }

    private Button CreateTabButton(Transform parent, string name, string label, string iconPath, float x, PanelMode mode, UnityEngine.Events.UnityAction action, out Text labelText)
    {
        bool active = mode == activePanel;
        GameObject node = CreateGeneratedImage(parent, name, active ? "GeneratedRuntimeUI/ui_v2/tab_active_green_base" : "GeneratedRuntimeUI/ui_v2/tab_inactive_beige_base", x, 0f, 244f, 132f, false, true);
        Image image = node.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = node.AddComponent<Button>();
        button.targetGraphic = image;

        CreateGeneratedImage(node.transform, "Icon", iconPath, 0f, 32f, 42f, 42f, true, true);
        labelText = CreateText(node.transform, "Label", label, 33, active ? theme.BrightInk : theme.Ink, TextAnchor.MiddleCenter, 0f, -34f, 190f, 42f, true);
        button.onClick.AddListener(action);
        return button;
    }

    private Button CreateInstallActionButton(Transform parent, string name, string label, float x, float y, float width, float height, GameUiTone tone, UnityEngine.Events.UnityAction action)
    {
        Text labelText;
        string spritePath = tone == GameUiTone.Positive
            ? "GeneratedRuntimeUI/ui_v2/button_green_base"
            : "GeneratedRuntimeUI/ui_v2/button_beige_base";
        Color textColor = tone == GameUiTone.Positive ? theme.BrightInk : theme.Ink;
        Button button = CreateSpriteButton(parent, name, spritePath, label, x, y, width, height, textColor, out labelText, height >= 60f ? 31 : 27);
        labelText.fontSize = height >= 60f ? 31 : 27;
        button.onClick.AddListener(action);
        return button;
    }

    private static void EnterPlayModeIfRuntime()
    {
        if (Application.isPlaying)
        {
            BuildPlayModeManager.EnterPlayMode();
        }
    }

    private void ShowSharedPanel()
    {
        SetSharedPanelCollapsed(false);
    }

    private void CollapseSharedPanel(bool immediate)
    {
        SetSharedPanelCollapsed(true, immediate);
    }

    private void SetSharedPanelCollapsed(bool collapsed, bool immediate = false)
    {
        if (sharedPanelRoot == null)
        {
            return;
        }

        EnsureSharedPanelMotionState();
        sharedPanelRoot.gameObject.SetActive(true);
        sharedPanelCollapsed = collapsed;

        if (sharedPanelCanvasGroup != null)
        {
            sharedPanelCanvasGroup.alpha = 1f;
            sharedPanelCanvasGroup.interactable = !collapsed;
            sharedPanelCanvasGroup.blocksRaycasts = !collapsed;
        }

        ApplySharedPanelPauseState();

        if (!Application.isPlaying || immediate || sharedPanelRectTransform == null)
        {
            SnapSharedPanelToTarget();
            return;
        }

        sharedPanelAnimating = true;
    }

    private void EnsureSharedPanelMotionState()
    {
        if (sharedPanelRoot == null)
        {
            return;
        }

        if (sharedPanelRectTransform == null || sharedPanelRectTransform.transform != sharedPanelRoot)
        {
            sharedPanelRectTransform = sharedPanelRoot.GetComponent<RectTransform>();
            sharedPanelPositionCaptured = false;
        }

        if (sharedPanelCanvasGroup == null && Application.isPlaying)
        {
            sharedPanelCanvasGroup = sharedPanelRoot.GetComponent<CanvasGroup>();
            if (sharedPanelCanvasGroup == null)
            {
                sharedPanelCanvasGroup = sharedPanelRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (!sharedPanelPositionCaptured && sharedPanelRectTransform != null)
        {
            sharedPanelOpenAnchoredPosition = sharedPanelRectTransform.anchoredPosition;
            sharedPanelPositionCaptured = true;
        }
    }

    private void UpdateSharedPanelMotion()
    {
        if (!sharedPanelAnimating || sharedPanelRectTransform == null)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
        Vector2 position = sharedPanelRectTransform.anchoredPosition;
        float targetY = GetSharedPanelTargetY();
        float displacement = targetY - position.y;
        float acceleration = (displacement * SharedPanelSpringStrength) - (sharedPanelVelocityY * SharedPanelDamping);

        sharedPanelVelocityY += acceleration * deltaTime;
        position.y += sharedPanelVelocityY * deltaTime;
        sharedPanelRectTransform.anchoredPosition = position;

        if (Mathf.Abs(displacement) < 0.6f && Mathf.Abs(sharedPanelVelocityY) < 4f)
        {
            SnapSharedPanelToTarget();
        }
    }

    private void SnapSharedPanelToTarget()
    {
        if (sharedPanelRectTransform == null)
        {
            return;
        }

        Vector2 position = sharedPanelOpenAnchoredPosition;
        position.y = GetSharedPanelTargetY();
        sharedPanelRectTransform.anchoredPosition = position;
        sharedPanelVelocityY = 0f;
        sharedPanelAnimating = false;
    }

    private float GetSharedPanelTargetY()
    {
        if (!sharedPanelCollapsed || sharedPanelRectTransform == null)
        {
            return sharedPanelOpenAnchoredPosition.y;
        }

        float panelHeight = Mathf.Max(0f, sharedPanelRectTransform.rect.height);
        return sharedPanelOpenAnchoredPosition.y - panelHeight - SharedPanelClosedExtraOffset;
    }

    private void ApplySharedPanelPauseState()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (timeManager == null)
        {
            ResolveReferences();
        }

        if (timeManager == null)
        {
            return;
        }

        bool shouldPauseForPanel =
            sharedPanelRoot != null &&
            sharedPanelRoot.gameObject.activeInHierarchy &&
            !sharedPanelCollapsed;

        if (shouldPauseForPanel)
        {
            timeManager.SetPaused(true);
            sharedPanelPauseApplied = true;
            return;
        }

        if (!sharedPanelPauseApplied)
        {
            return;
        }

        sharedPanelPauseApplied = false;
        if (!InGameMenuManager.IsMenuOpen)
        {
            timeManager.SetPaused(false);
        }
    }

    private void ToggleOperatePanel()
    {
        if (ShouldCollapsePanel(PanelMode.Operate))
        {
            CollapseSharedPanel(false);
            return;
        }

        ShowOperatePanel();
    }

    private void ToggleInstallPanel()
    {
        if (ShouldCollapsePanel(PanelMode.Install))
        {
            CloseInstallPanelAndPlacement();
            return;
        }

        ShowInstallPanel();
    }

    private void ToggleEconomyPanel()
    {
        if (ShouldCollapsePanel(PanelMode.Economy))
        {
            CollapseSharedPanel(false);
            return;
        }

        ShowEconomyPanel();
    }

    private void ToggleReviewPanel()
    {
        if (ShouldCollapsePanel(PanelMode.Review))
        {
            CollapseSharedPanel(false);
            return;
        }

        ShowReviewPanel();
    }

    private bool ShouldCollapsePanel(PanelMode mode)
    {
        return activePanel == mode &&
               sharedPanelRoot != null &&
               !sharedPanelCollapsed;
    }

    private void CloseInstallPanelAndPlacement()
    {
        activePanel = PanelMode.Install;

        if (Application.isPlaying)
        {
            if (placementManager != null)
            {
                placementManager.CancelCurrentPlacement();
            }
            else
            {
                BuildPlayModeManager.EnterPlayMode();
            }
        }

        CollapseSharedPanel(false);

        RefreshBottomTabs();
        RefreshPlacementActionOverlay();
    }

    private void ShowOperatePanel()
    {
        activePanel = PanelMode.Operate;
        EnterPlayModeIfRuntime();
        ShowSharedPanel();
        SetSharedPanelTitle("운영 현황");
        operatePanelRoot.gameObject.SetActive(true);
        installPanelRoot.gameObject.SetActive(false);
        SetOptionalPanelActive("EconomyPanelRoot", false);
        SetOptionalPanelActive("ReviewPanelRoot", false);
        comingSoonPanelRoot.gameObject.SetActive(false);
        RefreshBottomTabs();
        RefreshOperatePanel();
    }

    private void ShowInstallPanel()
    {
        activePanel = PanelMode.Install;
        EnterPlayModeIfRuntime();
        ShowSharedPanel();
        SetSharedPanelTitle("설치");

        if (selectedDefinition == null)
        {
            selectedDefinition = GetFirstDefinitionForCategory(InstallCategories[selectedCategoryIndex]);
        }

        operatePanelRoot.gameObject.SetActive(false);
        installPanelRoot.gameObject.SetActive(true);
        SetOptionalPanelActive("EconomyPanelRoot", false);
        SetOptionalPanelActive("ReviewPanelRoot", false);
        comingSoonPanelRoot.gameObject.SetActive(false);
        RefreshBottomTabs();
        RefreshInstallPanel();
        NotifyInstallTutorialInstallTabOpened();
    }

    private void ShowComingSoonPanel(PanelMode mode, string title)
    {
        activePanel = mode;
        EnterPlayModeIfRuntime();
        ShowSharedPanel();
        SetSharedPanelTitle(title);
        operatePanelRoot.gameObject.SetActive(false);
        installPanelRoot.gameObject.SetActive(false);
        SetOptionalPanelActive("EconomyPanelRoot", false);
        SetOptionalPanelActive("ReviewPanelRoot", false);
        comingSoonPanelRoot.gameObject.SetActive(true);
        SetText(comingSoonText, $"{title} 화면은 다음 구현 범위입니다.");
        RefreshBottomTabs();
    }

    private void ShowEconomyPanel()
    {
        activePanel = PanelMode.Economy;
        EnterPlayModeIfRuntime();
        ShowSharedPanel();
        SetSharedPanelTitle("경제");
        operatePanelRoot.gameObject.SetActive(false);
        installPanelRoot.gameObject.SetActive(false);
        SetOptionalPanelActive("ReviewPanelRoot", false);

        EnsureEconomyPanelContent();
        Transform economyPanel = economyPanelRoot != null ? economyPanelRoot : FindDeepChild(runtimeRoot, "EconomyPanelRoot");
        if (economyPanel != null)
        {
            economyPanel.gameObject.SetActive(true);
            comingSoonPanelRoot.gameObject.SetActive(false);

            EconomyPanelDataBinder binder = economyPanel.GetComponent<EconomyPanelDataBinder>();
            if (binder != null)
            {
                binder.RefreshNow();
            }
        }
        else
        {
            SetOptionalPanelActive("EconomyPanelRoot", false);
            comingSoonPanelRoot.gameObject.SetActive(true);
            SetText(comingSoonText, "경제 화면은 다음 구현 범위입니다.");
        }

        RefreshBottomTabs();
    }

    private void ShowReviewPanel()
    {
        activePanel = PanelMode.Review;
        EnterPlayModeIfRuntime();
        ShowSharedPanel();
        SetSharedPanelTitle("리뷰");
        operatePanelRoot.gameObject.SetActive(false);
        installPanelRoot.gameObject.SetActive(false);
        SetOptionalPanelActive("EconomyPanelRoot", false);

        Transform reviewPanel = reviewPanelRoot != null ? reviewPanelRoot : FindDeepChild(runtimeRoot, "ReviewPanelRoot");
        if (reviewPanel != null)
        {
            reviewPanel.gameObject.SetActive(true);
            comingSoonPanelRoot.gameObject.SetActive(false);

            ReviewPanelDataBinder binder = reviewPanel.GetComponent<ReviewPanelDataBinder>();
            if (binder != null)
            {
                binder.RefreshNow();
            }
        }
        else
        {
            SetOptionalPanelActive("ReviewPanelRoot", false);
            comingSoonPanelRoot.gameObject.SetActive(true);
            SetText(comingSoonText, "리뷰 화면은 다음 구현 범위입니다.");
        }

        RefreshBottomTabs();
    }

    private void SetOptionalPanelActive(string panelName, bool active)
    {
        Transform panel = FindDeepChild(runtimeRoot, panelName);
        if (panel != null)
        {
            panel.gameObject.SetActive(active);
        }
    }

    private void SetSharedPanelTitle(string title)
    {
        SetText(sharedPanelTitleText, title);
    }

    private void EnterPlacementReady()
    {
        if (selectedDefinition == null)
        {
            ShowToast("선택한 기구가 없습니다.");
            return;
        }

        EquipmentSelectionState.Select(selectedDefinition);
        if (placementManager != null)
        {
            placementManager.BeginPlacement(selectedDefinition);
        }

        activePanel = PanelMode.Install;
        SimpleGameUIBootstrap.HideLegacySceneCanvases();
        CollapseSharedPanel(false);
        operatePanelRoot.gameObject.SetActive(false);
        installPanelRoot.gameObject.SetActive(true);
        SetOptionalPanelActive("EconomyPanelRoot", false);
        SetOptionalPanelActive("ReviewPanelRoot", false);
        comingSoonPanelRoot.gameObject.SetActive(false);
        RefreshBottomTabs();
        RefreshPlacementActionOverlay();
        HideLegacyRuntimeFloorMockup();
        HideToast();
        NotifyInstallTutorialEquipmentSelected(selectedDefinition);
    }

    private void BeginPlacementForDefinition(EquipmentDefinition definition)
    {
        selectedDefinition = definition;
        EnterPlacementReady();
    }

    private void CancelInstallSelection()
    {
        selectedDefinition = null;
        ShowOperatePanel();
        HideToast();
    }

    private void RefreshAllData()
    {
        RefreshHud();
        RefreshOperatePanel();
        RefreshInstallPanel();
    }

    private void RefreshHud()
    {
        SetText(dateValueText, BuildDateLabel());
        SetText(branchValueText, BuildBranchLabel());
        SetText(cashValueText, walletManager != null ? $"{GetCash():N0}G" : "연결 대기");
        SetText(starCoinValueText, walletManager != null ? $"{GetStarCoin():N0}" : "연결 대기");
        SetText(memberValueText, economyManager != null ? $"총{GetActiveMembers():N0}명" : "연결 대기");
    }

    private void RefreshOperatePanel()
    {
        if (economyManager == null)
        {
            SetText(operateGoalText, "연결 대기");
            SetText(operateStatusText, "연결 대기");
            SetText(operateCrowdText, "연결 대기");
            SetText(operateCleanText, "연결 대기");
            SetText(operateUsageText, placementManager != null ? BuildMachineUsageLabel(0) : "연결 대기");
            SetText(operateWaitText, "연결 대기");
            SetText(operateStaffText, staffManager != null ? $"{GetStaffCount():N0}명 근무 중" : "연결 대기");
            SetText(operateRevenueText, "연결 대기");
            return;
        }

        int activeMembers = GetActiveMembers();
        int capacity = Mathf.Max(activeMembers + 2, economyManager.GetCurrentCapacityEstimate());
        int waiting = Mathf.Max(0, economyManager.GetWaitingCustomersCount());
        int usingCount = Mathf.Max(0, economyManager.GetUsingCustomersCount());
        float cleanliness = economyManager.GetCleanliness01();

        SetText(operateGoalText, $"회원 {activeMembers:N0}/{capacity:N0}명");
        SetText(operateStatusText, economyManager.GetOperationStatusLabel());
        SetText(operateCrowdText, BuildCrowdLabel(waiting, usingCount));
        SetText(operateCleanText, BuildCleanlinessLabel(cleanliness));
        SetText(operateUsageText, BuildMachineUsageLabel(usingCount));
        SetText(operateWaitText, $"{waiting:N0}명");
        SetText(operateStaffText, staffManager != null ? $"{GetStaffCount():N0}명 근무 중" : "연결 대기");
        SetText(operateRevenueText, $"{GetTodayRevenue():N0}G");
    }

    private void RefreshInstallPanel()
    {
        RefreshCategoryTabs();
        RebuildInstallCards();
        RefreshSelectedDefinitionText();
    }

    private void RefreshCategoryTabs()
    {
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            bool active = i == selectedCategoryIndex;
            Image image = categoryButtons[i].GetComponent<Image>();
            if (image != null)
            {
                GeneratedRuntimeSprites.Assign(image, active ? "GeneratedRuntimeUI/ui_v2/button_green_base" : "GeneratedRuntimeUI/ui_v2/category_tab_beige_base", false);
            }

            if (i < categoryTexts.Count && categoryTexts[i] != null)
            {
                categoryTexts[i].color = active ? theme.BrightInk : theme.Ink;
                categoryTexts[i].fontStyle = FontStyle.Normal;
            }
        }
    }

    private void RebuildInstallCards()
    {
        GameUiFactory.ClearChildren(installListRoot);

        List<EquipmentDefinition> definitions = GetDisplayDefinitionsForCategory(InstallCategories[selectedCategoryIndex]);
        if (definitions.Count <= 0)
        {
            GameObject empty = CreateGeneratedImage(installListRoot, "NoCatalogItemsCard", "GeneratedRuntimeUI/ui_v2/install_card_base", 0f, 34f, 890f, 98f, false, true);
            CreateText(empty.transform, "Message", "이 카테고리에 등록된 EquipmentDefinition이 없습니다.", 24, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 0f, 820f, 60f, true);
            return;
        }

        if (selectedDefinition == null || !definitions.Contains(selectedDefinition))
        {
            selectedDefinition = definitions[0];
        }

        for (int i = 0; i < definitions.Count && i < 6; i++)
        {
            EquipmentDefinition definition = definitions[i];
            float x = i % 2 == 0 ? -253f : 253f;
            float y = 140f - ((i / 2) * 108f);
            CreateInstallCard(definition, i, x, y);
        }
    }

    private void CreateInstallCard(EquipmentDefinition definition, int index, float x, float y)
    {
        GameObject card = CreateGeneratedImage(installListRoot, $"InstallCard_{index}_{definition.EquipmentId}", "GeneratedRuntimeUI/ui_v2/install_card_base", x, y, 480f, 100f, false, true);

        Image image = card.GetComponent<Image>();
        image.color = definition == selectedDefinition ? new Color(0.90f, 1.00f, 0.78f, 1f) : Color.white;
        image.raycastTarget = true;
        Button button = card.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            BeginPlacementForDefinition(definition);
        });

        GameObject icon = CreateGeneratedImage(card.transform, "Icon", GetEquipmentIconPath(definition), -198f, 8f, 80f, 78f, true, true);
        icon.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        SetInstallCardTextNormal(CreateText(card.transform, "Name", GetEquipmentDisplayName(definition), 24, theme.Ink, TextAnchor.MiddleLeft, 64f, 30f, 306f, 34f, true));
        SetInstallCardTextNormal(CreateText(card.transform, "Price", $"{definition.InstallCost:N0}G", 23, theme.Ink, TextAnchor.MiddleRight, 110f, -2f, 220f, 30f, true));
        SetInstallCardTextNormal(CreateText(card.transform, "Owned", placementManager != null ? $"보유 {GetOwnedCount(definition):N0}" : "보유 확인중", 19, theme.MutedInk, TextAnchor.MiddleRight, 110f, -29.9998f, 220f, 26f, true));
        SetInstallCardTextNormal(CreateText(card.transform, "Footprint", $"{definition.Width}x{definition.Height}칸", 16, theme.MutedInk, TextAnchor.MiddleCenter, -192f, -30f, 92f, 22f, true));
    }

    private static void SetInstallCardTextNormal(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyle.Normal;

        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            if (Application.isPlaying)
            {
                Destroy(outline);
            }
            else
            {
                DestroyImmediate(outline);
            }
        }

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow != null)
        {
            if (Application.isPlaying)
            {
                Destroy(shadow);
            }
            else
            {
                DestroyImmediate(shadow);
            }
        }
    }

    private void RefreshSelectedDefinitionText()
    {
        if (selectedDefinition == null)
        {
            SetText(selectedItemNameText, "선택 아이템: 없음");
            SetText(selectedItemPriceText, "가격: -");
            SetText(selectedItemDescText, "실제 EquipmentCatalog 데이터에서 기구를 선택하세요.");
            SetSelectedItemIcon("GeneratedRuntimeUI/objects/treadmill");
            return;
        }

        SetText(selectedItemNameText, $"선택 아이템: {GetEquipmentDisplayName(selectedDefinition)}");
        SetText(selectedItemPriceText, $"가격: {selectedDefinition.InstallCost:N0}G");
        SetText(selectedItemDescText, BuildDefinitionDescription(selectedDefinition));
        SetSelectedItemIcon(GetEquipmentIconPath(selectedDefinition));
    }

    private void RefreshBottomTabs()
    {
        RefreshTab(operateTabButton, operateTabText, PanelMode.Operate);
        RefreshTab(installTabButton, installTabText, PanelMode.Install);
        RefreshTab(economyTabButton, economyTabText, PanelMode.Economy);
        RefreshTab(reviewTabButton, reviewTabText, PanelMode.Review);
    }

    private void RefreshTab(Button button, Text text, PanelMode mode)
    {
        bool active = activePanel == mode;
        if (button != null)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                GeneratedRuntimeSprites.Assign(image, active ? "GeneratedRuntimeUI/ui_v2/tab_active_green_base" : "GeneratedRuntimeUI/ui_v2/tab_inactive_beige_base", false);
            }
        }

        if (text != null)
        {
            text.color = active ? theme.BrightInk : theme.Ink;
        }
    }

    private List<EquipmentDefinition> GetDefinitionsForCategory(EquipmentCategory category)
    {
        List<EquipmentDefinition> result = new List<EquipmentDefinition>();
        if (equipmentCatalog == null || equipmentCatalog.Definitions == null)
        {
            return result;
        }

        IReadOnlyList<EquipmentDefinition> definitions = equipmentCatalog.Definitions;
        for (int i = 0; i < definitions.Count; i++)
        {
            EquipmentDefinition definition = definitions[i];
            if (definition == null || !definition.UnlockedByDefault)
            {
                continue;
            }

            if (MatchesCategory(definition, category))
            {
                result.Add(definition);
            }
        }

        return result;
    }

    private List<EquipmentDefinition> GetDisplayDefinitionsForCategory(EquipmentCategory category)
    {
        return GetDefinitionsForCategory(category);
    }

    private EquipmentDefinition GetFirstDefinitionForCategory(EquipmentCategory category)
    {
        List<EquipmentDefinition> definitions = GetDefinitionsForCategory(category);
        return definitions.Count > 0 ? definitions[0] : null;
    }

    private static bool MatchesCategory(EquipmentDefinition definition, EquipmentCategory category)
    {
        if (definition.Category == category)
        {
            return true;
        }

        return false;
    }

    private string BuildDateLabel()
    {
        if (timeManager == null)
        {
            return "시간 대기";
        }

        int year = Mathf.Max(1, timeManager.CurrentYear);
        int month = Mathf.Clamp(timeManager.CurrentMonth, 1, 12);
        int day = Mathf.Clamp(timeManager.CurrentDay, 1, Mathf.Max(1, timeManager.DaysPerMonth));
        return $"{year}년 {month}월 {day}일";
    }

    private string BuildBranchLabel()
    {
        if (siteManager == null)
        {
            return "동네 헬스장";
        }

        return GetLocationGymLabel(siteManager.CurrentLocationType);
    }

    private int GetCash()
    {
        return walletManager != null ? walletManager.CurrentCash : 0;
    }

    private int GetStarCoin()
    {
        return walletManager != null ? walletManager.CurrentStarCoin : 0;
    }

    private int GetActiveMembers()
    {
        return economyManager != null ? economyManager.GetActiveMemberCount() : 0;
    }

    private int GetTodayRevenue()
    {
        return economyManager != null ? economyManager.GetPreviewDailyNetRevenue() : 0;
    }

    private int GetStaffCount()
    {
        return staffManager != null && staffManager.HiredStaff != null ? staffManager.HiredStaff.Count : 0;
    }

    private int GetOwnedCount(EquipmentDefinition definition)
    {
        if (definition == null || placementManager == null)
        {
            return 0;
        }

        IReadOnlyList<PlacedObjectSaveData> runtimeData = placementManager.GetPlacedObjectRuntimeData();
        if (runtimeData == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < runtimeData.Count; i++)
        {
            PlacedObjectSaveData data = runtimeData[i];
            if (data != null && data.equipmentId == definition.EquipmentId)
            {
                count++;
            }
        }

        return count;
    }

    private string BuildMachineUsageLabel(int usingCount)
    {
        if (placementManager == null)
        {
            return "연결 대기";
        }

        IReadOnlyList<PlacedObjectSaveData> runtimeData = placementManager.GetPlacedObjectRuntimeData();
        if (runtimeData == null)
        {
            return "연결 대기";
        }

        int installedCount = 0;
        for (int i = 0; i < runtimeData.Count; i++)
        {
            PlacedObjectSaveData data = runtimeData[i];
            if (data == null || data.isUnderConstruction)
            {
                continue;
            }

            installedCount++;
        }

        if (installedCount <= 0)
        {
            return "기구 없음";
        }

        int percent = Mathf.Clamp(Mathf.RoundToInt((usingCount / Mathf.Max(1f, installedCount)) * 100f), 0, 100);
        return $"{percent}% ({usingCount:N0}/{installedCount:N0})";
    }

    private static string BuildOperationLabel(int waiting, int usingCount, int capacity, float cleanliness01)
    {
        if (cleanliness01 < 0.45f || waiting >= 4)
        {
            return "점검 필요";
        }

        if (waiting > 0 || usingCount >= Mathf.Max(1, capacity) * 0.7f)
        {
            return "혼잡 주의";
        }

        return "원활";
    }

    private static string BuildCrowdLabel(int waiting, int usingCount)
    {
        if (waiting >= 4)
        {
            return "혼잡";
        }

        if (waiting > 0 || usingCount >= 4)
        {
            return "보통";
        }

        return "여유";
    }

    private static string BuildCleanlinessLabel(float cleanliness01)
    {
        if (cleanliness01 >= 0.75f)
        {
            return "양호";
        }

        if (cleanliness01 >= 0.45f)
        {
            return "보통";
        }

        return "주의";
    }

    private static string BuildDefinitionDescription(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return "선택한 기구가 없습니다.";
        }

        return $"{GetCategoryLabel(definition.Category)} / {definition.Width}x{definition.Height}칸 / 평판 +{definition.PrestigeBonus:N0}, 수용 +{definition.MemberCapacityBonus:N0}";
    }

    private static string GetCategoryLabel(EquipmentCategory category)
    {
        switch (category)
        {
            case EquipmentCategory.Cardio: return "카디오";
            case EquipmentCategory.Push: return "푸쉬";
            case EquipmentCategory.Pull: return "풀";
            case EquipmentCategory.Legs: return "하체";
            case EquipmentCategory.Recovery: return "회복";
            case EquipmentCategory.Other: return "기타";
            default: return "기타";
        }
    }

    private static string GetLocationGymLabel(GymLocationType locationType)
    {
        switch (locationType)
        {
            case GymLocationType.StationArea: return "역세권 헬스장";
            case GymLocationType.Downtown: return "상권 헬스장";
            case GymLocationType.Premium: return "프리미엄 헬스장";
            default: return "동네 헬스장";
        }
    }

    private static string GetEquipmentDisplayName(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return "-";
        }

        string id = (definition.EquipmentId ?? string.Empty).ToLowerInvariant();
        string baseName;
        if (id.Contains("treadmill"))
        {
            baseName = "러닝머신";
        }
        else if (id.Contains("benchpress") || id.Contains("bench_press"))
        {
            baseName = "벤치프레스";
        }
        else if (id.Contains("squat"))
        {
            baseName = "스쿼트 랙";
        }
        else if (!string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            baseName = definition.DisplayName;
        }
        else
        {
            baseName = "기구";
        }

        return $"{baseName} {definition.BrandTierLabel}";
    }

    private static string GetEquipmentIconPath(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return "GeneratedRuntimeUI/objects/treadmill";
        }

        string id = (definition.EquipmentId ?? string.Empty).ToLowerInvariant();
        if (id.Contains("treadmill"))
        {
            return "GeneratedRuntimeUI/objects/treadmill";
        }

        if (id.Contains("bike") || id.Contains("cycle"))
        {
            return "GeneratedRuntimeUI/objects/exercise_bike";
        }

        if (id.Contains("benchpress") || id.Contains("bench_press") || id.Contains("bench"))
        {
            return "GeneratedRuntimeUI/objects/bench_press";
        }

        if (id.Contains("squat") || id.Contains("rack") || definition.Category == EquipmentCategory.Legs || definition.Category == EquipmentCategory.Pull)
        {
            return "GeneratedRuntimeUI/objects/dumbbell_rack";
        }

        if (definition.Category == EquipmentCategory.Other)
        {
            return "GeneratedRuntimeUI/objects/reception_desk";
        }

        if (definition.Category == EquipmentCategory.Recovery)
        {
            return "GeneratedRuntimeUI/objects/potted_plant";
        }

        return definition.Category == EquipmentCategory.Cardio
            ? "GeneratedRuntimeUI/objects/treadmill"
            : "GeneratedRuntimeUI/objects/dumbbell_rack";
    }

    private void HideLegacyRuntimeFloorMockup()
    {
        Transform floor = floorRoot != null
            ? floorRoot
            : runtimeRoot != null
                ? FindDeepChild(runtimeRoot, "FloorRoot")
                : null;

        if (floor != null)
        {
            floor.gameObject.SetActive(false);
        }
    }

    private void HideToast()
    {
        toastVisibleUntil = 0f;
        toastFadeStartedAt = 0f;
        toastFading = false;

        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
        }

        if (toastRoot != null)
        {
            toastRoot.gameObject.SetActive(false);
        }
    }

    private void ShowToast(string message)
    {
        if (toastRoot == null || toastText == null)
        {
            Debug.Log(message);
            return;
        }

        toastText.text = message;
        EnsureToastCanvasGroup();
        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 1f;
        }

        toastVisibleUntil = Time.unscaledTime + ToastVisibleSeconds;
        toastFadeStartedAt = 0f;
        toastFading = false;
        toastRoot.gameObject.SetActive(true);
        toastRoot.SetAsLastSibling();
    }

    private void UpdateToastFade()
    {
        if (toastRoot == null || !toastRoot.gameObject.activeSelf)
        {
            return;
        }

        EnsureToastCanvasGroup();

        float now = Time.unscaledTime;
        if (!toastFading)
        {
            if (now < toastVisibleUntil)
            {
                return;
            }

            toastFading = true;
            toastFadeStartedAt = now;
        }

        float fadeProgress = ToastFadeSeconds <= 0f ? 1f : Mathf.Clamp01((now - toastFadeStartedAt) / ToastFadeSeconds);
        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 1f - fadeProgress;
        }

        if (fadeProgress >= 1f)
        {
            HideToast();
        }
    }

    private void EnsureToastCanvasGroup()
    {
        if (toastRoot == null)
        {
            return;
        }

        toastCanvasGroup = toastRoot.GetComponent<CanvasGroup>();
        if (toastCanvasGroup == null)
        {
            toastCanvasGroup = toastRoot.gameObject.AddComponent<CanvasGroup>();
        }

        toastCanvasGroup.interactable = false;
        toastCanvasGroup.blocksRaycasts = false;
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

    private Button CreateSpriteButton(Transform parent, string name, string spritePath, string label, float x, float y, float width, float height, Color textColor, out Text labelText, int fontSize)
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

        labelText = CreateText(node.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, 0f, 0f, width - 18f, height - 12f, true);
        return button;
    }

    private Text CreateText(Transform parent, string name, string value, int fontSize, Color color, TextAnchor alignment, float x, float y, float width, float height, bool localParent)
    {
        Text text = GameUiFactory.CreateText(parent, name, theme, fontSize, color, alignment, FontStyle.Bold);
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
            ? new Color(0.08f, 0.05f, 0.02f, 0.48f)
            : new Color(1.00f, 0.92f, 0.70f, 0.13f);
        outline.effectDistance = new Vector2(1f, -1f);
        shadow.effectColor = new Color(0.08f, 0.05f, 0.02f, luma > 0.6f ? 0.40f : 0.15f);
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

    private static void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void SetSelectedItemIcon(string spritePath)
    {
        if (selectedItemIconImage != null)
        {
            GeneratedRuntimeSprites.Assign(selectedItemIconImage, spritePath, true);
        }
    }
}
