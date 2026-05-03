using System;
using UnityEngine;

public class MonthlySettlementManager : MonoBehaviour
{
    private enum BottomHudTab
    {
        Settlement,
        Equipment,
        Economy,
        Review
    }

    private struct SettlementGuide
    {
        public string action;
        public string reason;
        public string expected;
    }

    private static bool pendingOpenEquipmentTabRequest = false;

    public static void RequestOpenEquipmentTab()
    {
        pendingOpenEquipmentTabRequest = true;
    }

    [Header("References")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private WalletManager walletManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private GymEconomyManager gymEconomyManager;
    [SerializeField] private GymSiteManager gymSiteManager;
    [SerializeField] private EquipmentCatalog equipmentCatalog;
    [SerializeField] private StaffManager staffManager;

    [Header("Prototype Balance Preset")]
    [SerializeField] private bool forceRecommendedPrototypeBalanceOnAwake = true;
    [SerializeField] private bool logBalancePresetApplied = true;

    [Header("Settlement Settings")]
    [SerializeField] private int baseRent = 820;
    [SerializeField] private int rentPerPlacedObject = 100;

    [SerializeField] private int baseOperationalStaffCost = 130;
    [SerializeField] private int laborPerTrainer = 250;

    [SerializeField] private int baseFacilityMaintenance = 150;
    [SerializeField] private int maintenancePerPlacedObject = 26;
    [SerializeField] private int memberSupportCostPerMember = 5;

    [Header("Prototype Safety Guard")]
    [SerializeField] private bool applyEarlyMonthRamp = true;
    [SerializeField] private bool enableSettlementSafetyCap = true;
    [SerializeField] private int minimumSettlementBase = 460;
    [SerializeField] private int minimumSettlementPerPlacedObject = 50;
    [SerializeField] private int reinvestmentBufferBase = 1500;
    [SerializeField] private int reinvestmentBufferPerPlacedObject = 115;

    [Header("Bottom HUD (Prototype)")]
    [SerializeField] private bool showBottomHud = true;
    [SerializeField] private bool showNextEstimate = true;
    [SerializeField] private bool startWithHudOpen = true;
    [SerializeField] private float portraitPanelHeight = 184f;
    [SerializeField] private float landscapePanelWidth = 360f;
    [SerializeField] private float landscapePanelHeight = 220f;

    private const string DefaultSettlementText = "아직 결산 없음";

    private bool isInitialized = false;
    private string lastSettlementText = DefaultSettlementText;

    private bool isHudOpen;
    private BottomHudTab selectedTab = BottomHudTab.Settlement;
    private Vector2 settlementScrollPosition = Vector2.zero;

    private GUIStyle boxStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle tabButtonStyle;
    private GUIStyle headerButtonStyle;

    private bool hasLastSettlementBreakdown;
    private SettlementBreakdown lastSettlementBreakdown;

    public event Action SettlementCompleted;

    public string LastSettlementText => lastSettlementText;

    private struct SettlementBreakdown
    {
        public int monthNumber;
        public int placedObjectCount;
        public int activeMembers;
        public int trainerCount;

        public GymLocationType locationType;
        public string locationLabel;
        public int locationRentFlatAdditive;
        public float locationRentMultiplier;

        public int nominalRentCost;
        public int nominalLaborCost;
        public int nominalMaintenanceCost;
        public int nominalTotalCost;

        public int rentCost;
        public int laborCost;
        public int maintenanceCost;
        public int totalCost;

        public float rampFactor;
        public bool settlementWasCapped;

        public int previewDailyNet;
        public int previewMonthlyOperatingNet;
        public int previewMonthlyAfterSettlement;
    }

    private void Awake()
    {
        ApplyRecommendedPrototypeBalancePresetIfNeeded();
        isHudOpen = startWithHudOpen;
    }

    public void InitializeSettlement()
    {
        if (isInitialized)
        {
            return;
        }

        ResolveReferences();

        if (timeManager == null)
        {
            Debug.LogError("[MonthlySettlementManager] TimeManager를 찾지 못했어.");
            return;
        }

        if (walletManager == null)
        {
            Debug.LogError("[MonthlySettlementManager] WalletManager를 찾지 못했어.");
            return;
        }

        timeManager.MonthEnded += HandleMonthEnded;
        isInitialized = true;

        Debug.Log("[MonthlySettlementManager] 월말 결산 시스템 초기화 완료");
    }

    public void SetLastSettlementText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            lastSettlementText = DefaultSettlementText;
        }
        else
        {
            lastSettlementText = text;
        }
    }

    public int GetPreviewMonthlySettlementCost()
    {
        int currentMonth = timeManager != null ? Mathf.Max(1, timeManager.CurrentMonth) : 1;
        SettlementBreakdown breakdown = BuildSettlementBreakdown(currentMonth);
        return breakdown.totalCost;
    }

    private void ResolveReferences()
    {
        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }

        if (walletManager == null)
        {
            walletManager = FindFirstObjectByType<WalletManager>();
        }

        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (gymEconomyManager == null)
        {
            gymEconomyManager = FindFirstObjectByType<GymEconomyManager>();
        }

        if (gymSiteManager == null)
        {
            gymSiteManager = FindFirstObjectByType<GymSiteManager>();
        }

        if (equipmentCatalog == null)
        {
            equipmentCatalog = FindFirstObjectByType<EquipmentCatalog>();
        }

        if (staffManager == null)
        {
            staffManager = FindFirstObjectByType<StaffManager>();
        }
    }

    private void ApplyRecommendedPrototypeBalancePresetIfNeeded()
    {
        if (!forceRecommendedPrototypeBalanceOnAwake)
        {
            return;
        }

        baseRent = 820;
        rentPerPlacedObject = 100;

        baseOperationalStaffCost = 130;
        laborPerTrainer = 250;

        baseFacilityMaintenance = 150;
        maintenancePerPlacedObject = 26;
        memberSupportCostPerMember = 5;

        applyEarlyMonthRamp = true;
        enableSettlementSafetyCap = true;
        minimumSettlementBase = 460;
        minimumSettlementPerPlacedObject = 50;
        reinvestmentBufferBase = 1500;
        reinvestmentBufferPerPlacedObject = 115;

        if (logBalancePresetApplied)
        {
            Debug.Log(
                "[MonthlySettlementManager] 통합 밸런스 프리셋 적용 완료. " +
                "월말 결산 완화는 남기되, 초반 2개월에 첫 이사가 너무 빨리 열리지 않도록 다시 눌렀어."
            );
        }
    }

    private void HandleMonthEnded(int endedMonth)
    {
        SettlementBreakdown breakdown = BuildSettlementBreakdown(endedMonth);

        walletManager.SpendMandatory(breakdown.totalCost, $"{endedMonth}월 월말 결산");

        hasLastSettlementBreakdown = true;
        lastSettlementBreakdown = breakdown;

        string cappedSuffix = breakdown.settlementWasCapped ? " (안전장치 적용)" : string.Empty;

        lastSettlementText =
            $"{endedMonth}월 결산{cappedSuffix}\n" +
            $"입지: {breakdown.locationLabel}\n" +
            $"월세: {breakdown.rentCost:N0}\n" +
            $"운영 인건비: {breakdown.laborCost:N0}\n" +
            $"시설 유지비: {breakdown.maintenanceCost:N0}\n" +
            $"합계: {breakdown.totalCost:N0}\n" +
            $"결산 후 잔액: {walletManager.CurrentCash:N0}";

        Debug.Log(
            $"[MonthlySettlementManager] {endedMonth}월 결산 완료 / " +
            $"입지: {breakdown.locationLabel}, 월세: {breakdown.rentCost:N0}, 운영 인건비: {breakdown.laborCost:N0}, 시설 유지비: {breakdown.maintenanceCost:N0}, " +
            $"합계: {breakdown.totalCost:N0}, 잔액: {walletManager.CurrentCash:N0}, " +
            $"Capped={breakdown.settlementWasCapped}"
        );

        SettlementCompleted?.Invoke();
    }

    private SettlementBreakdown BuildSettlementBreakdown(int monthNumber)
    {
        int placedObjectCount = placementManager != null ? placementManager.PlacedObjectCount : 0;
        int activeMembers = gymEconomyManager != null ? Mathf.Max(0, gymEconomyManager.GetActiveMemberCount()) : 0;
        int trainerCount = gymEconomyManager != null ? Mathf.Max(0, gymEconomyManager.GetCurrentTrainerCount()) : 0;
        int previewDailyNet = gymEconomyManager != null ? gymEconomyManager.GetPreviewDailyNetRevenue() : 0;

        GymLocationPrototypeRules locationRules = GetCurrentLocationRules();
        string locationLabel = GetCurrentLocationLabel();

        int rawBaseRent = baseRent + (placedObjectCount * rentPerPlacedObject);
        int locationAdjustedRentBase = Mathf.Max(0, rawBaseRent + locationRules.monthlyRentFlatAdditive);
        int nominalRentCost = Mathf.RoundToInt(locationAdjustedRentBase * Mathf.Max(0f, locationRules.monthlyRentMultiplier));

        int nominalLaborCost = 0;
        if (staffManager != null)
        {
            nominalLaborCost = staffManager.GetTotalMonthlySalary();
        }
        else
        {
            if (placedObjectCount > 0 || activeMembers > 0)
            {
                nominalLaborCost += baseOperationalStaffCost;
            }
            nominalLaborCost += trainerCount * laborPerTrainer;
        }

        int nominalMaintenanceCost =
            baseFacilityMaintenance +
            (placedObjectCount * maintenancePerPlacedObject) +
            (activeMembers * memberSupportCostPerMember);

        int nominalTotalCost = nominalRentCost + nominalLaborCost + nominalMaintenanceCost;

        float rampFactor = GetMonthRampFactor(monthNumber);

        int rampedRentCost = Mathf.RoundToInt(nominalRentCost * rampFactor);
        int rampedLaborCost = Mathf.RoundToInt(nominalLaborCost * rampFactor);
        int rampedMaintenanceCost = Mathf.RoundToInt(nominalMaintenanceCost * rampFactor);
        int rampedNominalTotal = rampedRentCost + rampedLaborCost + rampedMaintenanceCost;

        int finalTotalCost = rampedNominalTotal;
        bool settlementWasCapped = false;

        int daysPerMonth = timeManager != null ? Mathf.Max(1, timeManager.DaysPerMonth) : 30;
        int previewMonthlyOperatingNet = previewDailyNet * daysPerMonth;

        if (enableSettlementSafetyCap && placedObjectCount > 0)
        {
            int minimumSettlement = minimumSettlementBase + (placedObjectCount * minimumSettlementPerPlacedObject);
            int reinvestmentBuffer = reinvestmentBufferBase + (placedObjectCount * reinvestmentBufferPerPlacedObject);
            int affordableCap = previewMonthlyOperatingNet - reinvestmentBuffer;

            int cappedTotal = Mathf.Clamp(affordableCap, minimumSettlement, rampedNominalTotal);
            if (cappedTotal < finalTotalCost)
            {
                finalTotalCost = cappedTotal;
                settlementWasCapped = true;
            }
        }

        int finalRentCost = rampedRentCost;
        int finalLaborCost = rampedLaborCost;
        int finalMaintenanceCost = rampedMaintenanceCost;

        if (settlementWasCapped && rampedNominalTotal > 0)
        {
            float ratio = (float)finalTotalCost / rampedNominalTotal;
            finalRentCost = Mathf.RoundToInt(rampedRentCost * ratio);
            finalLaborCost = Mathf.RoundToInt(rampedLaborCost * ratio);
            finalMaintenanceCost = finalTotalCost - finalRentCost - finalLaborCost;
        }

        int previewMonthlyAfterSettlement = previewMonthlyOperatingNet - finalTotalCost;

        return new SettlementBreakdown
        {
            monthNumber = monthNumber,
            placedObjectCount = placedObjectCount,
            activeMembers = activeMembers,
            trainerCount = trainerCount,
            locationType = gymSiteManager != null ? gymSiteManager.CurrentLocationType : GymLocationType.Neighborhood,
            locationLabel = locationLabel,
            locationRentFlatAdditive = locationRules.monthlyRentFlatAdditive,
            locationRentMultiplier = locationRules.monthlyRentMultiplier,

            nominalRentCost = nominalRentCost,
            nominalLaborCost = nominalLaborCost,
            nominalMaintenanceCost = nominalMaintenanceCost,
            nominalTotalCost = nominalTotalCost,

            rentCost = finalRentCost,
            laborCost = finalLaborCost,
            maintenanceCost = finalMaintenanceCost,
            totalCost = finalTotalCost,

            rampFactor = rampFactor,
            settlementWasCapped = settlementWasCapped,

            previewDailyNet = previewDailyNet,
            previewMonthlyOperatingNet = previewMonthlyOperatingNet,
            previewMonthlyAfterSettlement = previewMonthlyAfterSettlement
        };
    }

    private float GetMonthRampFactor(int monthNumber)
    {
        if (!applyEarlyMonthRamp)
        {
            return 1f;
        }

        if (monthNumber <= 1) return 0.70f;
        if (monthNumber <= 2) return 0.82f;
        if (monthNumber <= 3) return 0.94f;

        return 1.00f;
    }

    private string BuildPreviewText()
    {
        int currentMonth = timeManager != null ? Mathf.Max(1, timeManager.CurrentMonth) : 1;
        SettlementBreakdown breakdown = BuildSettlementBreakdown(currentMonth);
        SettlementGuide guide = BuildSettlementGuide(breakdown);

        string safetyText = breakdown.settlementWasCapped ? "적용 중" : "없음";
        string monthScaleText = $"{Mathf.RoundToInt(breakdown.rampFactor * 100f)}%";

        return
            "[다음 결산 예상]\n" +
            $"입지 {breakdown.locationLabel} · 월 {breakdown.monthNumber}\n" +
            $"기구 {breakdown.placedObjectCount}대 · 회원 {breakdown.activeMembers}명 · 트레이너 {breakdown.trainerCount}명\n" +
            $"월세 {breakdown.rentCost:N0} · 인건비 {breakdown.laborCost:N0} · 유지비 {breakdown.maintenanceCost:N0}\n" +
            $"예상 합계 {breakdown.totalCost:N0} · 월 운영손익 {breakdown.previewMonthlyOperatingNet:N0}\n" +
            $"결산 후 예상 월손익 {breakdown.previewMonthlyAfterSettlement:N0}\n" +
            $"초반 완화 {monthScaleText} · 안전장치 {safetyText}\n\n" +
            "[다음 권장]\n" +
            $"지금: {guide.action}\n" +
            $"이유: {guide.reason}\n" +
            $"기대: {guide.expected}";
    }

    private string BuildLastSettlementSummary()
    {
        if (hasLastSettlementBreakdown)
        {
            SettlementBreakdown breakdown = lastSettlementBreakdown;
            string safetyText = breakdown.settlementWasCapped ? "적용" : "없음";

            return
                "[최근 결산]\n" +
                $"{breakdown.monthNumber}월 · 입지 {breakdown.locationLabel}\n" +
                $"월세 {breakdown.rentCost:N0} · 인건비 {breakdown.laborCost:N0} · 유지비 {breakdown.maintenanceCost:N0}\n" +
                $"합계 {breakdown.totalCost:N0} · 안전장치 {safetyText}";
        }

        if (!string.IsNullOrWhiteSpace(lastSettlementText) && !string.Equals(lastSettlementText, DefaultSettlementText, StringComparison.Ordinal))
        {
            return "[최근 결산]\n" + lastSettlementText;
        }

        return "[최근 결산]\n아직 결산 없음";
    }

    private SettlementGuide BuildSettlementGuide(SettlementBreakdown breakdown)
    {
        int previewAfterSettlement = breakdown.previewMonthlyAfterSettlement;
        int previewOperatingNet = breakdown.previewMonthlyOperatingNet;

        if (breakdown.placedObjectCount <= 0)
        {
            return new SettlementGuide
            {
                action = "기구 먼저 설치",
                reason = "결산 전에 운영 기반이 아직 없음",
                expected = "회원 유입 시작 · 월 운영손익 형성"
            };
        }

        if (BuildPlayModeManager.IsBuildMode)
        {
            return new SettlementGuide
            {
                action = "플레이 모드로 전환",
                reason = "설치 상태에선 실제 손님 흐름 확인이 멈춤",
                expected = "월 손익 변화 · 결산 위험도 확인"
            };
        }

        if (previewAfterSettlement < 0)
        {
            if (breakdown.locationType == GymLocationType.Downtown || breakdown.locationType == GymLocationType.StationArea)
            {
                return new SettlementGuide
                {
                    action = "기구 보강 후 입지 유지 가능성 점검",
                    reason = $"결산 후 예상 월손익 {previewAfterSettlement:N0} · 현재 입지 부담 큼",
                    expected = "방문 회복 · 적자 축소 · 입지 유지 판단"
                };
            }

            return new SettlementGuide
            {
                action = "기구 보강 후 흑자 전환 시도",
                reason = $"결산 후 예상 월손익 {previewAfterSettlement:N0} · 운영손익 {previewOperatingNet:N0}",
                expected = "방문 회복 · 순이익 개선"
            };
        }

        if (breakdown.settlementWasCapped)
        {
            return new SettlementGuide
            {
                action = "안전장치에 기대지 말고 수익 기반 강화",
                reason = $"결산 완화 적용 중 · 합계 {breakdown.totalCost:N0}",
                expected = "다음 달 실결산 버틸 체력 확보"
            };
        }

        if (previewAfterSettlement < 1500)
        {
            return new SettlementGuide
            {
                action = "소규모 기구 보강으로 완충 구간 확보",
                reason = $"결산 후 예상 여유 {previewAfterSettlement:N0}로 낮음",
                expected = "다음 투자 전 안정 자금 확보"
            };
        }

        if (breakdown.activeMembers < Mathf.Max(4, breakdown.placedObjectCount * 2))
        {
            return new SettlementGuide
            {
                action = "현재 규모에 맞는 회원 수 확보",
                reason = $"기구 {breakdown.placedObjectCount}대 대비 회원 {breakdown.activeMembers}명",
                expected = "월세 부담 완화 · 운영손익 개선"
            };
        }

        return new SettlementGuide
        {
            action = "현재 성장 흐름 유지",
            reason = $"결산 후 예상 월손익 {previewAfterSettlement:N0}로 안정권",
            expected = "현금 축적 후 다음 투자 준비"
        };
    }

    private string BuildSettlementPanelText()
    {
        string content = BuildLastSettlementSummary();

        if (showNextEstimate)
        {
            content += "\n\n----------------\n" + BuildPreviewText();
        }

        return content;
    }

    private GymLocationPrototypeRules GetCurrentLocationRules()
    {
        if (gymSiteManager == null)
        {
            gymSiteManager = FindFirstObjectByType<GymSiteManager>();
        }

        if (gymSiteManager == null)
        {
            return GymLocationPrototypeRules.CreateDefault();
        }

        gymSiteManager.InitializeSiteState();
        return gymSiteManager.GetCurrentLocationRules();
    }

    private string GetCurrentLocationLabel()
    {
        if (gymSiteManager == null)
        {
            gymSiteManager = FindFirstObjectByType<GymSiteManager>();
        }

        if (gymSiteManager == null)
        {
            return GymSiteManager.GetLocationDisplayName(GymLocationType.Neighborhood);
        }

        gymSiteManager.InitializeSiteState();
        return GymSiteManager.GetLocationDisplayName(gymSiteManager.CurrentLocationType);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!isInitialized || timeManager == null)
        {
            return;
        }

        timeManager.MonthEnded -= HandleMonthEnded;
        isInitialized = false;
    }

    private void Disabled_OnGUI()
    {
        if (!showBottomHud)
        {
            return;
        }

        if (InGameMenuManager.IsMenuOpen)
        {
            return;
        }

        ResolveReferences();
        EnsureStyles();

        if (pendingOpenEquipmentTabRequest)
        {
            isHudOpen = true;
            selectedTab = BottomHudTab.Equipment;
            pendingOpenEquipmentTabRequest = false;
        }

        if (placementManager != null && placementManager.IsRelocatingSelectedObject)
        {
            isHudOpen = true;
            selectedTab = BottomHudTab.Equipment;
        }

        bool isPortrait = Screen.height > Screen.width;

        if (!isHudOpen)
        {
            DrawCollapsedOpenButton(isPortrait);
            return;
        }

        DrawBottomHud(isPortrait);
    }

    private void DrawCollapsedOpenButton(bool isPortrait)
    {
        float width = isPortrait ? 110f : 120f;
        float height = 28f;
        float x = isPortrait ? 12f : Screen.width - width - 12f;
        float y = isPortrait ? Screen.height - height - 12f : 12f;

        Rect rect = new Rect(x, y, width, height);
        ScreenUiBlocker.RegisterRect(GetInstanceID(), rect);

        if (GUI.Button(rect, "하단 패널 열기", headerButtonStyle))
        {
            isHudOpen = true;
        }
    }

    private void DrawBottomHud(bool isPortrait)
    {
        float boxWidth = isPortrait ? Screen.width - 24f : landscapePanelWidth;
        float boxHeight = isPortrait ? portraitPanelHeight : landscapePanelHeight;
        float boxX = isPortrait ? 12f : Screen.width - boxWidth - 12f;
        float boxY = isPortrait ? Screen.height - boxHeight - 12f : 12f;

        Rect boxRect = new Rect(boxX, boxY, boxWidth, boxHeight);
        GUI.Box(boxRect, GUIContent.none, boxStyle);
        ScreenUiBlocker.RegisterRect(GetInstanceID(), boxRect);

        bool isBuildMode = BuildPlayModeManager.IsBuildMode;

        GUI.Label(
            new Rect(boxRect.x + 12f, boxRect.y + 8f, boxRect.width - 86f, 22f),
            $"하단 운영 패널 [{(isBuildMode ? "설치" : "플레이")}]",
            titleStyle
        );

        Rect closeRect = new Rect(boxRect.xMax - 70f, boxRect.y + 8f, 58f, 22f);

        if (GUI.Button(closeRect, "닫기", headerButtonStyle))
        {
            isHudOpen = false;
            return;
        }

        float tabY = boxRect.y + 34f;
        float tabX = boxRect.x + 12f;
        float tabAreaWidth = boxRect.width - 24f;
        float tabHeight = 28f;
        float tabGap = 6f;
        float tabWidth = (tabAreaWidth - (tabGap * 3f)) / 4f;

        DrawTabButton(new Rect(tabX, tabY, tabWidth, tabHeight), "월말 결산", BottomHudTab.Settlement);

        Rect buildEntryRect = new Rect(tabX + tabWidth + tabGap, tabY, tabWidth, tabHeight);
        bool isMiddleSelected = selectedTab == BottomHudTab.Equipment;
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = isMiddleSelected
            ? new Color(0.28f, 0.75f, 1f, 1f)
            : Color.white;

        string buildEntryLabel = isBuildMode ? "플레이" : "설치";
        if (GUI.Button(buildEntryRect, buildEntryLabel, tabButtonStyle))
        {
            if (!BuildPlayModeManager.IsBuildMode)
            {
                BuildPlayModeManager.EnterBuildMode();
                selectedTab = BottomHudTab.Equipment;
            }
            else
            {
                BuildPlayModeManager.EnterPlayMode();

                if (selectedTab == BottomHudTab.Equipment)
                {
                    selectedTab = BottomHudTab.Settlement;
                }
            }

            isHudOpen = true;
            isBuildMode = BuildPlayModeManager.IsBuildMode;
        }

        GUI.backgroundColor = previous;

        DrawTabButton(new Rect(tabX + (tabWidth + tabGap) * 2f, tabY, tabWidth, tabHeight), "경제", BottomHudTab.Economy);
        DrawTabButton(new Rect(tabX + (tabWidth + tabGap) * 3f, tabY, tabWidth, tabHeight), "리뷰", BottomHudTab.Review);

        Rect contentRect = new Rect(
            boxRect.x + 12f,
            tabY + tabHeight + 8f,
            boxRect.width - 24f,
            boxRect.height - 34f - tabHeight - 18f
        );

        switch (selectedTab)
        {
            case BottomHudTab.Equipment:
                if (placementManager != null && placementManager.HasSelectedPlacedObject)
                {
                    placementManager.DrawBottomHudContent(contentRect);
                }
                else if (equipmentCatalog != null)
                {
                    equipmentCatalog.DrawBottomHudContent(contentRect);
                }
                else
                {
                    GUI.Box(contentRect, GUIContent.none, boxStyle);
                    GUI.Label(contentRect, "EquipmentCatalog를 찾지 못했어.", labelStyle);
                }
                break;

            case BottomHudTab.Economy:
                if (gymEconomyManager != null)
                {
                    gymEconomyManager.DrawBottomHudContent(contentRect);
                }
                else
                {
                    GUI.Box(contentRect, GUIContent.none, boxStyle);
                    GUI.Label(contentRect, "GymEconomyManager를 찾지 못했어.", labelStyle);
                }
                break;

            case BottomHudTab.Review:
                if (gymEconomyManager != null)
                {
                    gymEconomyManager.DrawReviewTabContent(contentRect);
                }
                break;

            default:
                DrawSettlementHudContent(contentRect);
                break;
        }
    }

    private void DrawSettlementHudContent(Rect contentRect)
    {
        GUI.Box(contentRect, GUIContent.none, boxStyle);

        string content = BuildSettlementPanelText();
        float availableWidth = contentRect.width - 20f;
        float contentHeight = Mathf.Max(
            contentRect.height - 4f,
            labelStyle.CalcHeight(new GUIContent(content), availableWidth)
        );

        Rect viewRect = new Rect(
            contentRect.x + 8f,
            contentRect.y + 8f,
            contentRect.width - 16f,
            contentRect.height - 16f
        );

        Rect scrollContentRect = new Rect(0f, 0f, availableWidth, contentHeight + 4f);

        settlementScrollPosition = GUI.BeginScrollView(
            viewRect,
            settlementScrollPosition,
            scrollContentRect
        );

        GUI.Label(
            new Rect(0f, 0f, availableWidth, contentHeight + 4f),
            content,
            labelStyle
        );

        GUI.EndScrollView();
    }

    private void DrawTabButton(Rect rect, string label, BottomHudTab tab)
    {
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = selectedTab == tab
            ? new Color(0.28f, 0.75f, 1f, 1f)
            : Color.white;

        if (GUI.Button(rect, label, tabButtonStyle))
        {
            if (placementManager != null && placementManager.IsRelocatingSelectedObject && tab != BottomHudTab.Equipment)
            {
                selectedTab = BottomHudTab.Equipment;
            }
            else
            {
                selectedTab = tab;
            }
        }

        GUI.backgroundColor = previous;
    }

    private void EnsureStyles()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.normal.textColor = Color.white;
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.UpperLeft;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.wordWrap = true;
            labelStyle.normal.textColor = Color.white;
        }

        if (tabButtonStyle == null)
        {
            tabButtonStyle = new GUIStyle(GUI.skin.button);
            tabButtonStyle.alignment = TextAnchor.MiddleCenter;
            tabButtonStyle.wordWrap = false;
        }

        if (headerButtonStyle == null)
        {
            headerButtonStyle = new GUIStyle(GUI.skin.button);
            headerButtonStyle.alignment = TextAnchor.MiddleCenter;
            headerButtonStyle.wordWrap = false;
        }

        bool isPortrait = Screen.height > Screen.width;

        boxStyle.fontSize = isPortrait ? 13 : 15;
        titleStyle.fontSize = isPortrait ? 14 : 16;
        labelStyle.fontSize = isPortrait ? 12 : 14;
        tabButtonStyle.fontSize = isPortrait ? 12 : 13;
        headerButtonStyle.fontSize = isPortrait ? 11 : 12;
    }
}
