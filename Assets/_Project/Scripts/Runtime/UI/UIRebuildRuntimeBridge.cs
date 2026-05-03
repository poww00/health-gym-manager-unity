using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(1000)]
public sealed class UIRebuildRuntimeBridge : MonoBehaviour
{
    private const string TitleScreenPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Title/PF_UI_TitleScreen.prefab";
    private const string TopHudPrefabPath = "Assets/_Project/Prefabs/UIRebuild/HUD/PF_UI_TopHUD.prefab";
    private const string BottomNavPrefabPath = "Assets/_Project/Prefabs/UIRebuild/BottomNav/PF_UI_BottomNav.prefab";
    private const string OperatePanelPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_OperatePanel.prefab";
    private const string InstallPanelPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_InstallPanel.prefab";
    private const string EconomyPanelPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_EconomyPanel.prefab";
    private const string ReviewPanelPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Panels/PF_UI_ReviewPanel.prefab";
    private const string GameMenuPopupPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Popups/PF_UI_GameMenuPopup.prefab";
    private const string StaffPopupPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Popups/PF_UI_StaffPopup.prefab";
    private const string RecruitPopupPrefabPath = "Assets/_Project/Prefabs/UIRebuild/Popups/PF_UI_RecruitPopup.prefab";

    private static readonly HashSet<string> ResolveLogCache = new HashSet<string>();

    [Header("Mode")]
    [SerializeField] private bool enableTitleSetup;
    [SerializeField] private bool enableGameplaySetup;

    [Header("UIRebuild Prefabs")]
    [SerializeField] private UnityEngine.Object previewCanvasPrefab;
    [SerializeField] private UnityEngine.Object titleScreenPrefab;
    [SerializeField] private UnityEngine.Object topHudPrefab;
    [SerializeField] private UnityEngine.Object bottomNavPrefab;
    [SerializeField] private UnityEngine.Object operatePanelPrefab;
    [SerializeField] private UnityEngine.Object installPanelPrefab;
    [SerializeField] private UnityEngine.Object economyPanelPrefab;
    [SerializeField] private UnityEngine.Object reviewPanelPrefab;
    [SerializeField] private UnityEngine.Object gameMenuPopupPrefab;
    [SerializeField] private UnityEngine.Object staffPopupPrefab;
    [SerializeField] private UnityEngine.Object recruitPopupPrefab;

    [Header("Legacy Scene Objects")]
    [SerializeField] private string legacyTitleCanvasName = "TitleScreen_Canvas";
    [SerializeField] private string legacyHudCanvasName = "MainHUD_Canvas";
    [SerializeField] private string legacyStaffCanvasName = "StaffUI_Canvas";

    private TitleMenuUIController titleController;
    private MainHUDController hudController;
    private StaffUIController legacyStaffController;
    private GameMenuUIController legacyMenuController;
    private WalletManager walletManager;
    private TimeManager timeManager;
    private GymEconomyManager economyManager;
    private StaffManager staffManager;
    private InGameMenuManager menuManager;
    private RelocationManager relocationManager;

    private GameObject titleScreenInstance;
    private GameObject topHudInstance;
    private GameObject bottomNavInstance;
    private GameObject operatePanelInstance;
    private GameObject installPanelInstance;
    private GameObject economyPanelInstance;
    private GameObject reviewPanelInstance;
    private GameObject gameMenuPopupInstance;
    private GameObject staffPopupInstance;
    private GameObject recruitPopupInstance;

    private GameObject activePanelInstance;
    private string installSelectionName = "런닝머신";
    private string installSelectionPrice = "4,800 G";
    private float nextRefreshAt;

    private void Start()
    {
#if UNITY_EDITOR
        AssignFallbackPrefabsIfMissing();
#endif
        StartCoroutine(InitializeAfterLegacyUi());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AssignFallbackPrefabsIfMissing();
    }

    private void AssignFallbackPrefabsIfMissing()
    {
        AssignPrefabIfMissing(ref previewCanvasPrefab, "Assets/_Project/Prefabs/UIRebuild/PF_UIRoot_Canvas.prefab");
        AssignPrefabIfMissing(ref titleScreenPrefab, TitleScreenPrefabPath);
        AssignPrefabIfMissing(ref topHudPrefab, TopHudPrefabPath);
        AssignPrefabIfMissing(ref bottomNavPrefab, BottomNavPrefabPath);
        AssignPrefabIfMissing(ref operatePanelPrefab, OperatePanelPrefabPath);
        AssignPrefabIfMissing(ref installPanelPrefab, InstallPanelPrefabPath);
        AssignPrefabIfMissing(ref economyPanelPrefab, EconomyPanelPrefabPath);
        AssignPrefabIfMissing(ref reviewPanelPrefab, ReviewPanelPrefabPath);
        AssignPrefabIfMissing(ref gameMenuPopupPrefab, GameMenuPopupPrefabPath);
        AssignPrefabIfMissing(ref staffPopupPrefab, StaffPopupPrefabPath);
        AssignPrefabIfMissing(ref recruitPopupPrefab, RecruitPopupPrefabPath);
    }

    private static void AssignPrefabIfMissing(ref UnityEngine.Object target, string assetPath)
    {
        if (target != null)
        {
            return;
        }

        target = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }
#endif

    private IEnumerator InitializeAfterLegacyUi()
    {
        yield return null;
        CacheManagers();

        if (enableTitleSetup)
        {
            SetupTitleScene();
        }

        if (enableGameplaySetup)
        {
            SetupGameplayScene();
        }

        RefreshAllVisibleTexts(force: true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshAt)
        {
            return;
        }

        nextRefreshAt = Time.unscaledTime + 0.25f;
        CacheManagers();
        RefreshAllVisibleTexts(force: false);
    }

    private void CacheManagers()
    {
        walletManager ??= FindFirstObjectByType<WalletManager>(FindObjectsInactive.Include);
        timeManager ??= FindFirstObjectByType<TimeManager>(FindObjectsInactive.Include);
        economyManager ??= FindFirstObjectByType<GymEconomyManager>(FindObjectsInactive.Include);
        staffManager ??= FindFirstObjectByType<StaffManager>(FindObjectsInactive.Include);
        menuManager ??= FindFirstObjectByType<InGameMenuManager>(FindObjectsInactive.Include);
        relocationManager ??= FindFirstObjectByType<RelocationManager>(FindObjectsInactive.Include);
    }

    private void SetupTitleScene()
    {
#if UNITY_EDITOR
        AssignFallbackPrefabsIfMissing();
#endif
        Debug.Log(
            $"[UIRebuildRuntimeBridge] SetupTitleScene scene={SceneManager.GetActiveScene().name}, bridge={gameObject.name}, " +
            $"previewCanvasPrefabNull={previewCanvasPrefab == null}, titleScreenPrefabNull={titleScreenPrefab == null}"
        );

        GameObject legacyTitleRoot = GameObject.Find(legacyTitleCanvasName);
        if (legacyTitleRoot != null)
        {
            titleController = legacyTitleRoot.GetComponentInChildren<TitleMenuUIController>(true);
            HideLegacyCanvas(legacyTitleRoot);
        }

        if (titleScreenPrefab == null || titleScreenInstance != null)
        {
            return;
        }

        titleScreenInstance = InstantiateUiPrefab(titleScreenPrefab, transform, "UIRebuild_TitleScreen_Runtime", 220, true);
        if (titleScreenInstance == null)
        {
            return;
        }

        Button continueButton = EnsureButton(titleScreenInstance, "ContinueButton");
        Button newGameButton = EnsureButton(titleScreenInstance, "NewGameButton");
        Button slot1Button = EnsureButton(titleScreenInstance, "Slot_1");
        Button slot2Button = EnsureButton(titleScreenInstance, "Slot_2");

        BindProxyButton(continueButton, () => titleController?.continueButton?.onClick.Invoke());
        BindProxyButton(newGameButton, () => titleController?.newGameButton?.onClick.Invoke());
        BindProxyButton(slot1Button, () => titleController?.slot1Button?.onClick.Invoke());
        BindProxyButton(slot2Button, () => titleController?.slot2Button?.onClick.Invoke());
    }

    private void SetupGameplayScene()
    {
        GameObject legacyHudRoot = GameObject.Find(legacyHudCanvasName);
        if (legacyHudRoot != null)
        {
            hudController = legacyHudRoot.GetComponentInChildren<MainHUDController>(true);
            legacyMenuController = legacyHudRoot.GetComponentInChildren<GameMenuUIController>(true);
            HideLegacyCanvas(legacyHudRoot);
        }

        GameObject legacyStaffRoot = GameObject.Find(legacyStaffCanvasName);
        if (legacyStaffRoot != null)
        {
            legacyStaffController = legacyStaffRoot.GetComponentInChildren<StaffUIController>(true);
            HideLegacyCanvas(legacyStaffRoot);
        }

        topHudInstance ??= InstantiateUiPrefab(topHudPrefab, transform, "UIRebuild_TopHUD_Runtime", 200, true);
        bottomNavInstance ??= InstantiateUiPrefab(bottomNavPrefab, transform, "UIRebuild_BottomNav_Runtime", 210, true);
        operatePanelInstance ??= InstantiateUiPrefab(operatePanelPrefab, transform, "UIRebuild_OperatePanel_Runtime", 205, true);
        installPanelInstance ??= InstantiateUiPrefab(installPanelPrefab, transform, "UIRebuild_InstallPanel_Runtime", 205, false);
        economyPanelInstance ??= InstantiateUiPrefab(economyPanelPrefab, transform, "UIRebuild_EconomyPanel_Runtime", 205, false);
        reviewPanelInstance ??= InstantiateUiPrefab(reviewPanelPrefab, transform, "UIRebuild_ReviewPanel_Runtime", 205, false);
        gameMenuPopupInstance ??= InstantiateUiPrefab(gameMenuPopupPrefab, transform, "UIRebuild_GameMenuPopup_Runtime", 320, false);
        staffPopupInstance ??= InstantiateUiPrefab(staffPopupPrefab, transform, "UIRebuild_StaffPopup_Runtime", 330, false);
        recruitPopupInstance ??= InstantiateUiPrefab(recruitPopupPrefab, transform, "UIRebuild_RecruitPopup_Runtime", 331, false);

        activePanelInstance = operatePanelInstance;

        BindGameplayButtons();
        BindInstallPanel();
        BindGameMenuPopup();
        BindStaffPopup();
        BindRecruitPopup();
    }

    private void BindGameplayButtons()
    {
        if (topHudInstance != null)
        {
            BindProxyButton(EnsureButton(topHudInstance, "StaffButton"), OpenStaffPopup);
            BindProxyButton(EnsureButton(topHudInstance, "MenuButton"), OpenGameMenuPopup);
            BindProxyButton(EnsureButton(topHudInstance, "BuildButton"), () =>
            {
                BuildPlayModeManager.EnterBuildMode();
                hudController?.placementTabBtn?.onClick.Invoke();
                ShowPanel(installPanelInstance);
            });

            BindProxyButton(EnsureButton(topHudInstance, "SpeedChip_1x"), () => SetSpeedPreset(0));
            BindProxyButton(EnsureButton(topHudInstance, "SpeedChip_2x"), () => SetSpeedPreset(1));
            BindProxyButton(EnsureButton(topHudInstance, "SpeedChip_4x"), () => SetSpeedPreset(2));
        }

        if (bottomNavInstance != null)
        {
            BindProxyButton(EnsureButton(bottomNavInstance, "Tab_Operate"), () =>
            {
                BuildPlayModeManager.EnterPlayMode();
                hudController?.operateTabBtn?.onClick.Invoke();
                ShowPanel(operatePanelInstance);
            });

            BindProxyButton(EnsureButton(bottomNavInstance, "Tab_Install"), () =>
            {
                BuildPlayModeManager.EnterBuildMode();
                hudController?.placementTabBtn?.onClick.Invoke();
                ShowPanel(installPanelInstance);
            });

            BindProxyButton(EnsureButton(bottomNavInstance, "Tab_Finance"), () =>
            {
                hudController?.economyTabBtn?.onClick.Invoke();
                ShowPanel(economyPanelInstance);
            });

            BindProxyButton(EnsureButton(bottomNavInstance, "Tab_Review"), () =>
            {
                hudController?.reviewTabBtn?.onClick.Invoke();
                ShowPanel(reviewPanelInstance);
            });
        }
    }

    private void BindInstallPanel()
    {
        if (installPanelInstance == null)
        {
            return;
        }
        EnsureInstallSampleCards();

        BindProxyButton(EnsureButton(installPanelInstance, "Category_Cardio"), () => SetInstallCategory("유산소"));
        BindProxyButton(EnsureButton(installPanelInstance, "Category_Weights"), () => SetInstallCategory("근력"));
        BindProxyButton(EnsureButton(installPanelInstance, "Category_Recovery"), () => SetInstallCategory("회복"));
        BindProxyButton(EnsureButton(installPanelInstance, "Category_Convenience"), () => SetInstallCategory("편의"));

        BindInstallCard("Card01", "런닝머신", "4,800 G");
        BindInstallCard("Card02", "벤치프레스", "7,500 G");
        BindInstallCard("Card03", "스트레치 매트", "2,100 G");
        BindInstallCard("Card04", "정수기", "1,400 G");

        BindInstallCard("Card05", "Spin Bike", "5,600 G");
        BindInstallCard("Card06", "Locker Bench", "3,200 G");
        BindProxyButton(EnsureButton(installPanelInstance, "ConfirmPlacement"), () =>
        {
            BuildPlayModeManager.EnterBuildMode();
            hudController?.placementTabBtn?.onClick.Invoke();
            SetText(installPanelInstance, "SelectionLabel", "배치 준비 완료");
        });
    }

    private void EnsureInstallSampleCards()
    {
        RectTransform content = FindDescendantComponent<RectTransform>(installPanelInstance, "Content");
        if (content == null)
        {
            return;
        }

        CloneInstallCardIfMissing(content, "Card03", "Card05", new Vector2(18f, -420f), "C", "Cardio", "Spin Bike", "5,600 G", "Place");
        CloneInstallCardIfMissing(content, "Card04", "Card06", new Vector2(426f, -420f), "F", "Convenience", "Locker Bench", "3,200 G", "Place");

        Vector2 size = content.sizeDelta;
        if (size.y < 1014f)
        {
            content.sizeDelta = new Vector2(size.x, 1014f);
        }
    }

    private void CloneInstallCardIfMissing(
        RectTransform content,
        string templateName,
        string newName,
        Vector2 anchoredPosition,
        string glyph,
        string category,
        string title,
        string price,
        string actionLabel)
    {
        if (content == null || FindDescendant(installPanelInstance, newName) != null)
        {
            return;
        }

        Transform template = FindDescendant(content, templateName);
        if (template == null)
        {
            return;
        }

        GameObject clone = Instantiate(template.gameObject, content, false);
        clone.name = newName;

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (cloneRect != null)
        {
            cloneRect.anchoredPosition = anchoredPosition;
        }

        RenameInstallCardDescendants(clone.transform, templateName, newName);
        SetText(installPanelInstance, $"{newName}_Glyph", glyph);
        SetText(installPanelInstance, $"{newName}_Category", category);
        SetText(installPanelInstance, $"{newName}_Title", title);
        SetText(installPanelInstance, $"{newName}_Price", price);
        SetText(installPanelInstance, $"{newName}_Action_Label", actionLabel);
    }

    private static void RenameInstallCardDescendants(Transform root, string oldPrefix, string newPrefix)
    {
        if (root == null)
        {
            return;
        }

        if (root.name.StartsWith(oldPrefix))
        {
            root.name = newPrefix + root.name.Substring(oldPrefix.Length);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            RenameInstallCardDescendants(root.GetChild(i), oldPrefix, newPrefix);
        }
    }
    private void BindInstallCard(string prefix, string itemName, string priceText)
    {
        Button actionButton = EnsureButton(installPanelInstance, $"{prefix}_Action");
        BindProxyButton(actionButton, () =>
        {
            installSelectionName = itemName;
            installSelectionPrice = priceText;
            SetText(installPanelInstance, "SelectionName", itemName);
            SetText(installPanelInstance, "SelectionPrice", priceText);
            SetText(installPanelInstance, "SelectionSub", "선택됨");
        });
    }

    private void BindGameMenuPopup()
    {
        if (gameMenuPopupInstance == null)
        {
            return;
        }

        BindProxyButton(EnsureButton(gameMenuPopupInstance, "GameMenuPopup_Close"), CloseAllPopups);
        BindProxyButton(EnsureButton(gameMenuPopupInstance, "ClosePopupButton"), CloseAllPopups);
        BindProxyButton(EnsureButton(gameMenuPopupInstance, "ReturnTitleButton"), () =>
        {
            menuManager?.ReturnToTitleScene();
        });
        BindProxyButton(EnsureButton(gameMenuPopupInstance, "RelocateButton"), () =>
        {
            if (menuManager != null)
            {
                menuManager.ExecuteCurrentRelocation();
            }
        });
    }

    private void BindStaffPopup()
    {
        if (staffPopupInstance == null)
        {
            return;
        }

        BindProxyButton(EnsureButton(staffPopupInstance, "StaffPopup_Close"), CloseAllPopups);
        BindProxyButton(EnsureButton(staffPopupInstance, "StaffCloseButton"), CloseAllPopups);
        BindProxyButton(EnsureButton(staffPopupInstance, "StaffTab_Working"), () => OpenStaffPopup());
        BindProxyButton(EnsureButton(staffPopupInstance, "StaffTab_Applicants"), () => OpenRecruitPopup());
    }

    private void BindRecruitPopup()
    {
        if (recruitPopupInstance == null)
        {
            return;
        }

        BindProxyButton(EnsureButton(recruitPopupInstance, "RecruitPopup_Close"), CloseAllPopups);
        BindProxyButton(EnsureButton(recruitPopupInstance, "RecruitCloseButton"), CloseAllPopups);
        BindProxyButton(EnsureButton(recruitPopupInstance, "RecruitTab_Working"), () => OpenStaffPopup());
        BindProxyButton(EnsureButton(recruitPopupInstance, "RecruitTab_Applicants"), () => OpenRecruitPopup());
    }

    private void RefreshAllVisibleTexts(bool force)
    {
        if (enableTitleSetup && titleScreenInstance != null)
        {
            RefreshTitleScreen();
        }

        if (!enableGameplaySetup)
        {
            return;
        }

        RefreshTopHud();
        RefreshOperatePanel();
        RefreshEconomyPanel();
        RefreshReviewPanel();
        RefreshGameMenuPopup();
        RefreshStaffPopup();
        RefreshRecruitPopup();

        if (force && installPanelInstance != null)
        {
            SetInstallCategory("유산소");
            SetText(installPanelInstance, "SelectionName", installSelectionName);
            SetText(installPanelInstance, "SelectionPrice", installSelectionPrice);
            SetText(installPanelInstance, "SelectionSub", "탭에서 카테고리를 고를 수 있습니다");
        }
    }

    private void RefreshTitleScreen()
    {
        if (titleController == null)
        {
            return;
        }

        Button continueButton = FindDescendantComponent<Button>(titleScreenInstance, "ContinueButton");
        if (continueButton != null && titleController.continueButton != null)
        {
            continueButton.interactable = titleController.continueButton.interactable;
        }

        SetText(titleScreenInstance, "StateValue_1", titleController.slot1StatusText != null ? titleController.slot1StatusText.text : "불러오기 가능");
        SetText(titleScreenInstance, "StateValue_2", titleController.slot2StatusText != null ? titleController.slot2StatusText.text : "빈 슬롯");
        SetText(titleScreenInstance, "StateValue_3", "새로운 운영 시작");
    }

    private void RefreshTopHud()
    {
        if (topHudInstance == null)
        {
            return;
        }

        if (walletManager != null)
        {
            SetText(topHudInstance, "CashValue", $"{walletManager.CurrentCash:N0} G");
            SetText(topHudInstance, "StarValue", $"{walletManager.CurrentStarCoin:N0}");
        }

        if (timeManager != null)
        {
            SetText(topHudInstance, "DateValue", $"{timeManager.CurrentYear}/{timeManager.CurrentMonth:D2}/{timeManager.CurrentDay:D2}");
            SetText(topHudInstance, "SpeedValue", BuildPlayModeManager.IsBuildMode ? "설치 모드" : GetSpeedLabel(timeManager.CurrentSpeedPresetIndex));
        }
    }

    private void RefreshOperatePanel()
    {
        if (operatePanelInstance == null || economyManager == null)
        {
            return;
        }

        SetText(operatePanelInstance, "MetricValue_Members", $"{economyManager.GetActiveMemberCount():N0}");
        SetText(operatePanelInstance, "MetricValue_Visits", $"{economyManager.GetUsingCustomersCount():N0}");
        SetText(operatePanelInstance, "MetricValue_Satisfaction", $"{economyManager.GetSatisfaction01() * 100f:0}%");
        SetText(operatePanelInstance, "MetricValue_Queue", $"{economyManager.GetWaitingCustomersCount():N0}");
        SetText(operatePanelInstance, "DualLeftTitle", economyManager.GetCurrentLocationPreviewLabel());
        SetText(operatePanelInstance, "DualRightTitle", economyManager.GetOperationStatusLabel());
        SetText(
            operatePanelInstance,
            "MemoBody",
            $"브랜드 {economyManager.GetAverageBrandLabel()}  |  기구 {economyManager.GetMachineCountEstimate()}대\n" +
            $"청결 {economyManager.GetCleanliness01() * 100f:0}%  |  평판 {economyManager.GetCurrentReputationStars():0.0}점"
        );
    }

    private void RefreshEconomyPanel()
    {
        if (economyPanelInstance == null || economyManager == null)
        {
            return;
        }

        SetText(economyPanelInstance, "SummaryValue_Revenue", $"{economyManager.GetDailyMembershipRevenue():N0} G");
        SetText(economyPanelInstance, "SummaryValue_Spend", $"{economyManager.GetDailyVariableCost():N0} G");
        SetText(economyPanelInstance, "SummaryValue_Net", $"{economyManager.GetPreviewDailyNetRevenue():N0} G");
        SetText(economyPanelInstance, "SummaryValue_Passes", $"{economyManager.GetDailyPtRevenue():N0} G");
        SetText(economyPanelInstance, "MemberMixTitle", $"회원층  일반 {economyManager.GetGeneralMemberCount()} / 중간 {economyManager.GetMiddleMemberCount()} / 상류 {economyManager.GetUpperMemberCount()}");
        SetText(economyPanelInstance, "CostTitle", $"비용  변동 {economyManager.GetDailyVariableCost():N0} G  |  PT {economyManager.GetDailyPtRevenue():N0} G");
        SetText(economyPanelInstance, "DetailRow_1", $"회원권 매출  {economyManager.GetDailyMembershipRevenue():N0} G");
        SetText(economyPanelInstance, "DetailRow_2", $"PT 매출  {economyManager.GetDailyPtRevenue():N0} G");
        SetText(economyPanelInstance, "DetailRow_3", $"부가 매출  {economyManager.GetDailyAncillaryRevenue():N0} G");
        SetText(economyPanelInstance, "DetailRow_4", $"운영비  {economyManager.GetDailyVariableCost():N0} G");
        SetText(economyPanelInstance, "DetailRow_5", $"순이익  {economyManager.GetPreviewDailyNetRevenue():N0} G");
        SetText(economyPanelInstance, "DetailRow_6", $"브랜드  {economyManager.GetAverageBrandLabel()}");
        SetText(economyPanelInstance, "DetailRow_7", $"입지  {economyManager.GetCurrentLocationPreviewLabel()}");
    }

    private void RefreshReviewPanel()
    {
        if (reviewPanelInstance == null || economyManager == null)
        {
            return;
        }

        IReadOnlyList<GymEconomyManager.CustomerReview> reviews = economyManager.GetRecentReviews();
        SetText(reviewPanelInstance, "SummaryValue_Score", $"{economyManager.GetCurrentReputationStars():0.0}");
        SetText(reviewPanelInstance, "SummaryValue_Reviews", $"{reviews.Count}");
        SetText(reviewPanelInstance, "SummaryValue_Return", economyManager.GetReviewTrendLabel());
        SetText(reviewPanelInstance, "SummaryValue_Events", $"{economyManager.GetWaitingCustomersCount():N0}");

        if (reviews.Count > 0)
        {
            SetText(reviewPanelInstance, "ReviewLine1", BuildReviewLine(reviews, 0));
            SetText(reviewPanelInstance, "ReviewLine2", BuildReviewLine(reviews, 1));
            SetText(reviewPanelInstance, "ReviewLine3", BuildReviewLine(reviews, 2));
            SetText(reviewPanelInstance, "EmptyText", "리뷰가 실제 데이터로 갱신되고 있습니다");
            SetText(reviewPanelInstance, "EmptySub", "최근 손님 의견과 운영 로그를 확인하세요");
        }
        else
        {
            SetText(reviewPanelInstance, "ReviewLine1", "아직 등록된 리뷰가 없습니다");
            SetText(reviewPanelInstance, "ReviewLine2", "회원 수를 늘리고 만족도를 관리해 보세요");
            SetText(reviewPanelInstance, "ReviewLine3", "청결과 대기열 관리가 핵심입니다");
            SetText(reviewPanelInstance, "EmptyText", "첫 리뷰를 기다리는 중");
            SetText(reviewPanelInstance, "EmptySub", "운영을 시작하면 리뷰 박스가 채워집니다");
        }

        SetText(reviewPanelInstance, "EventLine1", $"대기 인원  {economyManager.GetWaitingCustomersCount():N0}명");
        SetText(reviewPanelInstance, "EventLine2", $"이용 중  {economyManager.GetUsingCustomersCount():N0}명");
        SetText(reviewPanelInstance, "EventLine3", $"평균 대기  {economyManager.GetAverageWaitSeconds():0.0}초");
    }

    private void RefreshGameMenuPopup()
    {
        if (gameMenuPopupInstance == null || menuManager == null)
        {
            return;
        }

        SetText(gameMenuPopupInstance, "BranchValue", menuManager.GetCurrentSiteLabelText());
        SetText(gameMenuPopupInstance, "SummaryLine1", menuManager.GetSelectedTargetLocationLabelText());
        SetText(gameMenuPopupInstance, "SummaryLine2", menuManager.GetSelectedTargetLocationSummaryText());

        if (menuManager.TryGetCurrentRelocationQuote(out RelocationManager.RelocationQuote quote))
        {
            SetText(gameMenuPopupInstance, "SummaryLine3", $"이사 비용  {quote.totalCost:N0} G");
        }
        else
        {
            SetText(gameMenuPopupInstance, "SummaryLine3", "현재 이사 가능한 부지가 없습니다");
        }
    }

    private void RefreshStaffPopup()
    {
        if (staffPopupInstance == null || staffManager == null)
        {
            return;
        }

        PopulateStaffRows(
            staffPopupInstance,
            "StaffRow",
            staffManager.HiredStaff,
            isHired: true
        );
    }

    private void RefreshRecruitPopup()
    {
        if (recruitPopupInstance == null || staffManager == null)
        {
            return;
        }

        PopulateStaffRows(
            recruitPopupInstance,
            "RecruitRow",
            staffManager.AvailableApplicants,
            isHired: false
        );
    }

    private void PopulateStaffRows(GameObject root, string prefix, IReadOnlyList<StaffData> items, bool isHired)
    {
        int visibleCount = Mathf.Min(7, items != null ? items.Count : 0);
        for (int i = 1; i <= 7; i++)
        {
            Transform row = FindDescendant(root, $"{prefix}{i:00}");
            if (row == null)
            {
                continue;
            }

            bool visible = i <= visibleCount;
            row.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            StaffData staff = items[i - 1];
            SetText(root, $"{prefix}{i:00}_Title", staff.staffName);
            SetText(root, $"{prefix}{i:00}_Meta", $"{staffManager.GetRoleNameKOR(staff.role)}  |  급여 {staff.monthlySalary:N0} G");
            SetText(root, $"{prefix}{i:00}_Right", isHired ? $"담당 {staff.ptMemberCount}명" : "오늘 바로 채용 가능");
            SetText(root, $"{prefix}{i:00}_Action_Label", isHired ? "해고" : "채용");

            Button action = EnsureButton(root, $"{prefix}{i:00}_Action");
            if (action == null)
            {
                continue;
            }

            action.onClick.RemoveAllListeners();
            if (isHired)
            {
                string staffId = staff.staffId;
                action.onClick.AddListener(() => staffManager.FireStaff(staffId));
            }
            else
            {
                StaffData applicant = staff;
                action.onClick.AddListener(() => staffManager.HireApplicant(applicant));
            }
        }
    }

    private void OpenGameMenuPopup()
    {
        menuManager?.SetMenuOpen(true);
        ShowPopup(gameMenuPopupInstance);
    }

    private void OpenStaffPopup()
    {
        ShowPopup(staffPopupInstance);
        if (recruitPopupInstance != null)
        {
            recruitPopupInstance.SetActive(false);
        }
    }

    private void OpenRecruitPopup()
    {
        ShowPopup(recruitPopupInstance);
        if (staffPopupInstance != null)
        {
            staffPopupInstance.SetActive(false);
        }
    }

    private void CloseAllPopups()
    {
        if (gameMenuPopupInstance != null)
        {
            SetUiActive(gameMenuPopupInstance, false);
        }

        if (staffPopupInstance != null)
        {
            SetUiActive(staffPopupInstance, false);
        }

        if (recruitPopupInstance != null)
        {
            SetUiActive(recruitPopupInstance, false);
        }

        menuManager?.SetMenuOpen(false);
    }

    private void ShowPopup(GameObject popup)
    {
        if (popup == null)
        {
            return;
        }

        CloseAllPopups();
        SetUiActive(popup, true);
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        if (operatePanelInstance != null) SetUiActive(operatePanelInstance, panel == operatePanelInstance);
        if (installPanelInstance != null) SetUiActive(installPanelInstance, panel == installPanelInstance);
        if (economyPanelInstance != null) SetUiActive(economyPanelInstance, panel == economyPanelInstance);
        if (reviewPanelInstance != null) SetUiActive(reviewPanelInstance, panel == reviewPanelInstance);
        activePanelInstance = panel;
    }

    private void SetInstallCategory(string categoryLabel)
    {
        SetText(installPanelInstance, "PanelTitle", $"설치  |  {categoryLabel}");
        SetText(installPanelInstance, "SelectionLabel", $"{categoryLabel} 카테고리");
    }

    private void SetSpeedPreset(int presetIndex)
    {
        timeManager?.SetSpeedPreset(presetIndex);
    }

    private static string GetSpeedLabel(int presetIndex)
    {
        switch (presetIndex)
        {
            case 1:
                return "2x";
            case 2:
                return "4x";
            default:
                return "1x";
        }
    }

    private static string BuildReviewLine(IReadOnlyList<GymEconomyManager.CustomerReview> reviews, int index)
    {
        if (reviews == null || index < 0 || index >= reviews.Count)
        {
            return "새 리뷰를 기다리는 중";
        }

        GymEconomyManager.CustomerReview review = reviews[index];
        string author = string.IsNullOrWhiteSpace(review.authorName) ? "회원" : review.authorName;
        return $"{review.stars:0.0}점  |  {author}  |  {review.text}";
    }

    private GameObject InstantiateUiPrefab(UnityEngine.Object prefabRef, Transform parent, string runtimeName, int sortingOrder, bool active, int siblingIndex = -1)
    {
        GameObject prefabRoot = ResolvePrefabRoot(prefabRef, runtimeName);
        if (prefabRoot == null)
        {
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate((UnityEngine.Object)prefabRoot, parent, false) as GameObject;
        if (instance == null)
        {
            Debug.LogError($"[UIRebuildRuntimeBridge] Instantiate failed for {runtimeName}: prefab root was not created as GameObject.");
            return null;
        }

        instance.name = runtimeName;
        if (siblingIndex >= 0)
        {
            instance.transform.SetSiblingIndex(siblingIndex);
        }

        ConfigureCanvas(instance, sortingOrder, active);
        return instance;
    }

    private GameObject ResolvePrefabRoot(UnityEngine.Object source, string label)
    {
        if (source is GameObject prefabGameObject)
        {
            Transform root = prefabGameObject.transform.root;
            return root != null ? root.gameObject : prefabGameObject;
        }

        if (source is Component prefabComponent)
        {
            Transform root = prefabComponent.transform != null ? prefabComponent.transform.root : null;
            if (root != null)
            {
                return root.gameObject;
            }

            return prefabComponent.gameObject;
        }

#if UNITY_EDITOR
        if (source == null)
        {
            string fallbackPath = GetFallbackPrefabPath(label);
            if (!string.IsNullOrEmpty(fallbackPath))
            {
                GameObject fallbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
                if (fallbackPrefab != null)
                {
                    LogResolveMessageOnce(label, $"[UIRebuildRuntimeBridge] Recovered prefab by path for {label}: {fallbackPath}");
                    return fallbackPrefab;
                }
            }
        }
#endif

        return null;
    }

    private static void LogResolveMessageOnce(string runtimeName, string message)
    {
        if (ResolveLogCache.Add(runtimeName))
        {
            Debug.LogWarning(message);
        }
    }

    private static string GetFallbackPrefabPath(string runtimeName)
    {
        switch (runtimeName)
        {
            case "UIRebuild_TitleScreen_Runtime":
                return TitleScreenPrefabPath;
            case "UIRebuild_TopHUD_Runtime":
                return TopHudPrefabPath;
            case "UIRebuild_BottomNav_Runtime":
                return BottomNavPrefabPath;
            case "UIRebuild_OperatePanel_Runtime":
                return OperatePanelPrefabPath;
            case "UIRebuild_InstallPanel_Runtime":
                return InstallPanelPrefabPath;
            case "UIRebuild_EconomyPanel_Runtime":
                return EconomyPanelPrefabPath;
            case "UIRebuild_ReviewPanel_Runtime":
                return ReviewPanelPrefabPath;
            case "UIRebuild_GameMenuPopup_Runtime":
                return GameMenuPopupPrefabPath;
            case "UIRebuild_StaffPopup_Runtime":
                return StaffPopupPrefabPath;
            case "UIRebuild_RecruitPopup_Runtime":
                return RecruitPopupPrefabPath;
            default:
                return string.Empty;
        }
    }

    private static void ConfigureCanvas(GameObject instance, int sortingOrder, bool active)
    {
        if (instance == null)
        {
            return;
        }

        Canvas canvas = instance.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        GraphicRaycaster raycaster = instance.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = active || sortingOrder >= 300;
        }

        instance.SetActive(active);
    }

    private static void SetUiActive(GameObject instance, bool active)
    {
        if (instance == null)
        {
            return;
        }

        instance.SetActive(active);
        GraphicRaycaster raycaster = instance.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = active;
        }
    }

    private static void HideLegacyCanvas(GameObject root)
    {
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

    private static void BindProxyButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static Button EnsureButton(GameObject root, string name)
    {
        Transform target = FindDescendant(root, name);
        if (target == null)
        {
            return null;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.gameObject.AddComponent<Button>();
            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                button.targetGraphic = image;
            }
        }

        return button;
    }

    private static void SetText(GameObject root, string name, string value)
    {
        Text text = FindDescendantComponent<Text>(root, name);
        if (text != null)
        {
            text.text = value;
        }
    }

    private static T FindDescendantComponent<T>(GameObject root, string name) where T : Component
    {
        Transform target = FindDescendant(root, name);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindDescendant(GameObject root, string name)
    {
        if (root == null)
        {
            return null;
        }

        return FindDescendant(root.transform, name);
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform match = FindDescendant(child, name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
