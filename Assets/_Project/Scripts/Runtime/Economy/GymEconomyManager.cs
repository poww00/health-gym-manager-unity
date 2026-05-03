using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// [프로토타입 통합 밸런스 3차 + 리뷰/평판 1차]
/// 회원 성장 / 일일 수익 / 일일 운영비를 다시 정렬한 버전.
/// 회원 계층 3차: 혼잡 시 고계층 억제 강화 + 저표본 퍼센트 출렁임 완화.
/// 리뷰/평판 1차: 혼잡/품질/가치/서비스 축을 평판 점수로 누적하고 유입·이탈에 반영.
///
/// 정식 완성본 아님:
/// - 청결, 가격 정책, 직원 서비스는 아직 전용 시스템이 없어 대기/브랜드/만족/트레이너를 이용한 프로토타입 대리값
/// - 입지 효과도 "전역 보정치" 수준의 프로토타입 반영
/// - 목적은 이사 2차에서 선택한 입지(동네/역세권/번화가)가
///   실제 운영 결과(유입/부가매출/이탈/만족도)에 보이게 만드는 것
/// </summary>
[DefaultExecutionOrder(1100)]
public sealed class GymEconomyManager : MonoBehaviour
{
    private const string EntryModeKey = "GYM_ENTRY_MODE";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Header("External References (비워두면 자동 탐색)")]
    [SerializeField] private WalletManager walletManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private EquipmentCatalog equipmentCatalog;
    [SerializeField] private GymSiteManager gymSiteManager;
    [SerializeField] private CustomerFlowManager customerFlowManager;
    [SerializeField] private StaffManager staffManager;
    [SerializeField] private GymEventManager gymEventManager;

    [Header("Prototype Balance Preset")]
    [SerializeField] private bool forceRecommendedPrototypeBalanceOnAwake = true;
    [SerializeField] private bool logBalancePresetApplied = true;

    [Header("Legacy Fallback (구형 데이터 호환용)")]
    [SerializeField] private int cellsPerMachine = 4;
    [SerializeField] private float machineCountRefreshInterval = 0.25f;

    [Header("SO Runtime Global Multipliers")]
    [SerializeField, Range(0.10f, 2.0f)] private float soCapacityScale = 0.60f;
    [SerializeField, Range(0.10f, 2.0f)] private float soPrestigeScale = 0.55f;
    [SerializeField, Range(0.01f, 2.0f)] private float soElectricityCostScale = 0.04f;
    [SerializeField, Range(0.01f, 2.0f)] private float soMaintenanceCostScale = 0.04f;
    [SerializeField, Range(0.00f, 2.0f)] private float soPtDemandScale = 0.20f;

    [Header("Brand Tier Quality (Prototype 1차)")]
    [SerializeField, Range(0f, 0.20f)] private float brandLeadQualityBonus = 0.08f;
    [SerializeField, Range(0f, 0.30f)] private float brandAncillaryQualityBonus = 0.12f;
    [SerializeField, Range(0f, 0.15f)] private float brandChurnReduction = 0.05f;
    [SerializeField, Range(0f, 0.10f)] private float brandSatisfactionTargetBonus = 0.03f;

    [Header("Member Tier Prototype (기획서 9.1 / 9.2 1차)")]
    [SerializeField, Range(0f, 0.20f)] private float neighborhoodMiddleBase = 0.01f;
    [SerializeField, Range(0f, 0.20f)] private float stationMiddleBase = 0.05f;
    [SerializeField, Range(0f, 0.25f)] private float downtownMiddleBase = 0.08f;
    [SerializeField, Range(0f, 0.30f)] private float premiumMiddleBase = 0.11f;
    [SerializeField, Range(0f, 0.08f)] private float neighborhoodUpperBase = 0.00f;
    [SerializeField, Range(0f, 0.12f)] private float stationUpperBase = 0.01f;
    [SerializeField, Range(0f, 0.18f)] private float downtownUpperBase = 0.03f;
    [SerializeField, Range(0f, 0.25f)] private float premiumUpperBase = 0.06f;
    [SerializeField, Range(0f, 0.30f)] private float prestigeMiddleInfluence = 0.14f;
    [SerializeField, Range(0f, 0.20f)] private float prestigeUpperInfluence = 0.10f;
    [SerializeField, Range(0f, 0.30f)] private float brandMiddleInfluence = 0.24f;
    [SerializeField, Range(0f, 0.20f)] private float brandUpperInfluence = 0.18f;
    [SerializeField, Range(0f, 0.15f)] private float congestionMiddlePenalty = 0.05f;
    [SerializeField, Range(0f, 0.15f)] private float congestionUpperPenalty = 0.10f;
    [SerializeField, Range(1.00f, 1.20f)] private float middleMembershipRevenueMultiplier = 1.03f;
    [SerializeField, Range(1.00f, 1.30f)] private float upperMembershipRevenueMultiplier = 1.09f;
    [SerializeField, Range(1.00f, 1.20f)] private float middleJoiningRevenueMultiplier = 1.06f;
    [SerializeField, Range(1.00f, 1.30f)] private float upperJoiningRevenueMultiplier = 1.16f;
    [SerializeField, Range(1.00f, 1.30f)] private float middleAncillaryRevenueMultiplier = 1.16f;
    [SerializeField, Range(1.00f, 1.60f)] private float upperAncillaryRevenueMultiplier = 1.40f;
    [SerializeField, Range(1.00f, 1.30f)] private float middlePtDemandMultiplier = 1.12f;
    [SerializeField, Range(1.00f, 1.60f)] private float upperPtDemandMultiplier = 1.50f;
    [SerializeField, Range(0.70f, 1.00f)] private float middleTierChurnMultiplier = 0.95f;
    [SerializeField, Range(0.60f, 1.00f)] private float upperTierChurnMultiplier = 0.80f;
    [SerializeField, Range(0f, 0.03f)] private float middleTierSatisfactionTargetBonus = 0.015f;
    [SerializeField, Range(0f, 0.06f)] private float upperTierSatisfactionTargetBonus = 0.04f;
    [SerializeField, Range(0f, 0.20f)] private float middleCrowdingSuppression = 0.08f;
    [SerializeField, Range(0f, 0.35f)] private float upperCrowdingSuppression = 0.18f;
    [SerializeField, Range(0f, 1.00f)] private float severeCrowdingMiddleDecay = 0.45f;
    [SerializeField, Range(0f, 1.00f)] private float severeCrowdingUpperDecay = 0.75f;
    [SerializeField, Range(0f, 32f)] private float tierSampleStabilizeStartMembers = 8f;
    [SerializeField, Range(1f, 64f)] private float tierSampleStabilizeFullMembers = 22f;
    [SerializeField, Range(0f, 1.00f)] private float lowSampleMiddleFallbackWeight = 0.60f;
    [SerializeField, Range(0f, 1.00f)] private float lowSampleUpperFallbackWeight = 0.35f;

    [Header("Review / Reputation Prototype (기획서 9.3 1차)")]
    [SerializeField, Range(0f, 1f)] private float startingReputation = 0.58f;
    [SerializeField, Range(0.02f, 0.50f)] private float reviewDailyLerp = 0.16f;
    [SerializeField, Range(0.30f, 0.80f)] private float reviewNeutralPoint = 0.56f;
    [SerializeField, Range(0f, 0.30f)] private float reviewLeadMaxBonus = 0.12f;
    [SerializeField, Range(0f, 0.40f)] private float reviewLeadMaxPenalty = 0.18f;
    [SerializeField, Range(0f, 0.20f)] private float reviewChurnReduction = 0.05f;
    [SerializeField, Range(0f, 0.35f)] private float reviewChurnMaxPenalty = 0.14f;
    [SerializeField, Range(0f, 0.20f)] private float reviewAncillaryMaxBonus = 0.08f;
    [SerializeField, Range(0f, 0.20f)] private float reviewAncillaryMaxPenalty = 0.06f;
    [SerializeField, Range(0f, 0.05f)] private float reviewSatisfactionBonus = 0.015f;
    [SerializeField, Range(0f, 0.08f)] private float reviewSatisfactionPenalty = 0.028f;
    [SerializeField, Range(0f, 0.15f)] private float middleTierReviewWeight = 0.05f;
    [SerializeField, Range(0f, 0.40f)] private float upperTierReviewWeight = 0.20f;
    [SerializeField, Range(0f, 32f)] private float reviewSampleStabilizeStart = 6f;
    [SerializeField, Range(1f, 64f)] private float reviewSampleStabilizeFull = 24f;
    [SerializeField, Range(0f, 0.30f)] private float trainerServiceReviewBonus = 0.10f;
    [SerializeField, Range(0f, 0.50f)] private float reviewOccupancyPenaltyWeight = 0.26f;
    [SerializeField, Range(0f, 0.60f)] private float reviewWaitPressurePenaltyWeight = 0.34f;
    [SerializeField, Range(0f, 0.40f)] private float reviewCongestionPenaltyWeight = 0.18f;
    [SerializeField, Range(0f, 0.50f)] private float reviewAbandonPenaltyWeight = 0.28f;
    [SerializeField, Range(0f, 0.50f)] private float reviewExperiencePenaltyWeight = 0.30f;
    [SerializeField, Range(0f, 0.30f)] private float reviewSevereCrowdingExtraPenalty = 0.12f;
    [SerializeField, Range(0f, 0.40f)] private float reviewMachineSupportPenaltyWeight = 0.18f;
    [SerializeField, Range(0f, 0.30f)] private float reviewSevereSupportExtraPenalty = 0.08f;
    [SerializeField, Range(0.40f, 1.00f)] private float reviewImmediateBlendMin = 0.72f;
    [SerializeField, Range(0.50f, 2.50f)] private float reviewNegativeLerpBoost = 1.70f;
    [SerializeField, Range(0.20f, 1.00f)] private float reviewPositiveLerpDamping = 0.72f;


    [Header("Membership Growth")]
    [SerializeField] private int maxMembersPerMachine = 4;
    [SerializeField] private int prestigePerMachine = 5;
    [SerializeField] private float leadBase = 0.90f;
    [SerializeField] private float leadPerMachine = 0.15f;
    [SerializeField] private float leadPerPrestige = 0.018f;
    [SerializeField] private float satisfactionJoinMultiplier = 0.50f;
    [SerializeField] private float baseChurnRate = 0.006f;
    [SerializeField] private float crowdingChurnMultiplier = 0.18f;
    [SerializeField] private float lowSatisfactionChurnMultiplier = 0.10f;
    [SerializeField] private float dailyVisitRate = 0.11f;

    [Header("Membership Revenue")]
    [SerializeField] private int membershipPricePerDay = 50;
    [SerializeField] private int joiningFee = 90;

    [Header("Opening Growth Throttle (Prototype)")]
    [SerializeField] private bool useOpeningGrowthThrottle = true;
    [SerializeField, Range(0.10f, 1.00f)] private float openingLeadScaleMonth1 = 0.52f;
    [SerializeField, Range(0.10f, 1.00f)] private float openingLeadScaleMonth2 = 0.75f;
    [SerializeField, Range(0.10f, 1.00f)] private float openingLeadScaleMonth3 = 0.88f;
    [SerializeField] private int maxJoinsPerDayMonth1 = 1;
    [SerializeField] private int maxJoinsPerDayMonth2 = 1;

    [Header("Revenue Recognition (Prototype)")]
    [SerializeField, Range(0.00f, 1.00f)] private float sameDayJoinMembershipRevenueFactor = 0.25f;
    [SerializeField, Range(0.00f, 1.00f)] private float sameDayJoinServiceCostFactor = 0.25f;
    [SerializeField, Range(0.00f, 1.00f)] private float leavingMemberProrationFactor = 0.50f;

    [Header("PT Prototype")]
    [SerializeField] private int trainerCount = 0;
    [SerializeField] private int trainerPtSessionsPerDay = 2;
    [SerializeField] private float ptInterestRate = 0.01f;
    [SerializeField] private int ptPricePerSession = 6000;
    [SerializeField, Range(0f, 1f)] private float gymPtRevenueShare = 0.15f;
    [SerializeField] private int trainerBaseWagePerDay = 1800;
    [SerializeField] private int ptDemandBonusPerMachine = 0;

    [Header("Ancillary / Daily Cost")]
    [SerializeField] private float ancillaryPurchaseRate = 0.04f;
    [SerializeField] private int ancillaryAverageSpend = 100;
    [SerializeField] private int electricityCostPerMachinePerDay = 10;
    [SerializeField] private int maintenanceCostPerMachinePerDay = 7;
    [SerializeField] private int consumableCostPerVisitor = 18;
    [SerializeField] private int serviceCostPerActiveMemberPerDay = 18;

    [Header("Customer Flow Congestion (Prototype)")]
    [SerializeField] private bool useCustomerFlowSignals = true;
    [SerializeField, Range(0f, 0.50f)] private float congestionLeadPenalty = 0.18f;
    [SerializeField, Range(0f, 0.10f)] private float congestionExtraChurn = 0.030f;
    [SerializeField, Range(0f, 0.25f)] private float congestionSatisfactionPenalty = 0.08f;
    [SerializeField, Range(0f, 0.20f)] private float engagementSatisfactionBonus = 0.04f;
    [SerializeField, Range(0.5f, 1.5f)] private float engagementAncillaryMultiplierMin = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] private float engagementAncillaryMultiplierMax = 1.08f;

    [Header("Customer Experience Memory (Prototype)")]
    [SerializeField, Range(0f, 0.40f)] private float customerExperienceLeadPenalty = 0.12f;
    [SerializeField, Range(0f, 0.12f)] private float abandonmentExtraChurn = 0.040f;
    [SerializeField, Range(0f, 0.25f)] private float customerExperienceSatisfactionPenalty = 0.10f;
    [SerializeField, Range(0.50f, 1.00f)] private float customerExperienceAncillaryMinMultiplier = 0.90f;

    [Header("Satisfaction")]
    [SerializeField, Range(0f, 1f)] private float startingSatisfaction = 0.60f;
    [SerializeField, Range(0f, 1f)] private float satisfactionBaseTarget = 0.57f;
    [SerializeField, Range(0f, 1f)] private float baseSatisfactionGainFromPrestige = 0.0005f;
    [SerializeField, Range(0f, 1f)] private float crowdingSatisfactionPenalty = 0.22f;
    [SerializeField, Range(0f, 1f)] private float satisfactionLerp = 0.12f;

    [Header("Persistence (Prototype Sidecar Save)")]
    [SerializeField] private string autoEconomyFileName = "autosave_economy.json";
    [SerializeField] private string manualSlot1EconomyFileName = "manual_slot_1_economy.json";
    [SerializeField] private string manualSlot2EconomyFileName = "manual_slot_2_economy.json";
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnApplicationQuit = true;

    [Header("Debug (프로토타입 디버그 UI)")]
    [SerializeField] private bool showDebugOnGUI = true;
    [SerializeField] private bool startWithDebugPanelExpanded = false;
    [SerializeField] private bool allowDebugTrainerButtons = true;
    [SerializeField] private bool logDailySummary = true;
    [SerializeField] private bool useBottomHudHost = true;

    private SlotType sessionLoadSlot = SlotType.Auto;
    private bool isDebugPanelExpanded;
    private float nextMachineCountRefreshTime;

    private int cachedMachineCount;
    private int cachedCapacity;
    private int cachedPrestige;
    private int cachedElectricityCost;
    private int cachedMaintenanceCost;
    private int cachedPtDemandBonus;
    private float cachedBrandQualityScoreTotal;
    private int cachedHighTierMachineCount;

    private bool hasDateSnapshot;
    private int lastYear;
    private int lastMonth;
    private int lastDay;

    private EconomyState state;
    private DailyEconomyReport previewReport;
    private DailyEconomyReport lastAppliedReport;

    private readonly Dictionary<SlotType, DateTime> knownBaseSaveWriteTimes = new Dictionary<SlotType, DateTime>();
    private readonly List<EquipmentDebugEntry> cachedEquipmentEntries = new List<EquipmentDebugEntry>();

    private Vector2 economyBottomScrollPosition = Vector2.zero;
    private GUIStyle bottomLabelStyle;
    private GUIStyle bottomBoxStyle;

    private enum SlotType
    {
        Auto,
        Manual1,
        Manual2
    }

    [Serializable]
    public struct CustomerReview
    {
        public int year;
        public int month;
        public int day;
        public float stars;
        public string text;
        public string authorTier;
        public string authorName;
    }

    [Serializable]
    private struct EconomyState
    {
        public int activeMembers;
        public float satisfaction;
        public float cleanliness;
        public int trainerCount;
        public float reputation;
        public float lastDailyReviewScore;
        public float lastReviewDelta;
        public List<CustomerReview> recentReviews;
        public float pendingJoinProgress;
        public float pendingLeaveProgress;
        public int totalJoined;
        public int totalLeft;
        public int totalPtSessions;
        public int totalMembershipRevenue;
        public int totalPtGymRevenue;
        public int totalAncillaryRevenue;
        public int totalVariableCost;
        public int totalTrainerWages;
        public int totalNetRevenue;
    }

    [Serializable]
    private struct EconomySidecarSaveData
    {
        public int version;
        public EconomyState economyState;
    }

    private struct DailyEconomyReport
    {
        public int year;
        public int month;
        public int day;

        public GymLocationType locationType;
        public string locationLabel;
        public float locationLeadMultiplier;
        public float locationAncillaryMultiplier;
        public float locationChurnMultiplier;
        public float locationSatisfactionTargetOffset;

        public int machineCount;
        public int capacity;
        public int prestige;
        public float averageBrandQualityScore;
        public int highTierMachineCount;
        public string averageBrandLabel;
        public float generalMemberRatio;
        public float middleMemberRatio;
        public float upperMemberRatio;
        public int generalMembersAtEnd;
        public int middleMembersAtEnd;
        public int upperMembersAtEnd;
        public float tierMembershipRevenueMultiplier;
        public float tierJoiningRevenueMultiplier;
        public float tierAncillaryRevenueMultiplier;
        public float tierPtDemandMultiplier;
        public float tierChurnMultiplier;
        public float tierSatisfactionTargetBonus;
        public float reputationBefore;
        public float reputationAfter;
        public float dailyReviewScore;
        public float dailyReviewStars;
        public float reputationStars;
        public float reviewDelta;
        public float reviewCrowdingScore;
        public float reviewCrowdingRisk;
        public float reviewQualityScore;
        public float reviewValueScore;
        public float reviewServiceScore;
        public float reviewLeadMultiplier;
        public float reviewChurnMultiplier;
        public float reviewAncillaryMultiplier;
        public float reviewSatisfactionTargetOffset;
        public float reviewSensitivity;
        public float reviewInfluenceWeight;
        public List<CustomerReview> generatedReviews;

        public int activeMembersAtStart;
        public int joins;
        public int leaves;
        public int activeMembersAtEnd;
        public float pendingJoinProgressAfter;
        public float pendingLeaveProgressAfter;
        public int visitors;
        public int waitingCustomers;
        public int usingCustomers;
        public int completedVisits;
        public int abandonedVisits;
        public int waitingEvents;
        public int recoveredFromWaiting;
        public int peakWaitingCustomers;

        public int membershipRevenue;
        public int joiningRevenue;
        public int ancillaryRevenue;

        public int ptDemandEstimate;
        public int ptGrossRevenue;
        public int ptGymRevenue;
        public int trainerShareAmount;
        public int trainerBaseWage;
        public int ptSessions;

        public int electricityCost;
        public int maintenanceCost;
        public int equipmentOperatingCost;
        public int consumableCost;
        public int serviceCost;
        public int variableCost;
        public int netRevenue;

        public float occupancy;
        public float brandLeadBonusMultiplier;
        public float brandAncillaryBonusMultiplier;
        public float congestionSignal;
        public float engagementSignal;
        public float averageWaitSeconds;
        public float abandonmentRate;
        public float recoveryRate;
        public float waitPressure;
        public float customerExperiencePenalty;
        public float satisfactionBefore;
        public float satisfactionAfter;
    }

    private struct EquipmentDebugEntry
    {
        public string displayName;
        public int count;
    }

    private struct PlacementRuntimeInfo
    {
        public int width;
        public int height;
        public string equipmentId;
        public string displayName;
        public EquipmentDefinition definition;
    }

    private void Awake()
    {
        isDebugPanelExpanded = startWithDebugPanelExpanded;

        ApplyRecommendedPrototypeBalancePresetIfNeeded();
        CacheReferences();
        ResolveSessionLoadSlot();
        ForceRefreshMachineStats();
        LoadEconomyStateForSession();
        CacheBaseSaveWriteTimes();
    }

    private void Start()
    {
        if (TryReadDate(out int year, out int month, out int day))
        {
            lastYear = year;
            lastMonth = month;
            lastDay = day;
            hasDateSnapshot = true;
        }

        RefreshPreviewReport();
    }

    private void Update()
    {
        CacheReferences();
        RefreshMachineStatsIfNeeded();
        DetectBaseSaveWritesAndMirrorEconomy();

        if (!TryReadDate(out int currentYear, out int currentMonth, out int currentDay))
        {
            return;
        }

        previewReport = BuildDailyReport(currentYear, currentMonth, currentDay);

        if (!hasDateSnapshot)
        {
            lastYear = currentYear;
            lastMonth = currentMonth;
            lastDay = currentDay;
            hasDateSnapshot = true;
            return;
        }

        if (IsSameDate(currentYear, currentMonth, currentDay, lastYear, lastMonth, lastDay))
        {
            return;
        }

        if (!IsLaterDate(currentYear, currentMonth, currentDay, lastYear, lastMonth, lastDay))
        {
            lastYear = currentYear;
            lastMonth = currentMonth;
            lastDay = currentDay;
            RefreshPreviewReport();
            return;
        }

        DailyEconomyReport applied = BuildDailyReport(lastYear, lastMonth, lastDay);
        ApplyDailyEconomy(applied);

        if (customerFlowManager != null)
        {
            customerFlowManager.ResetDailyExperienceMetrics($"{lastYear}/{lastMonth}/{lastDay} 결산 완료");
        }

        lastYear = currentYear;
        lastMonth = currentMonth;
        lastDay = currentDay;

        RefreshPreviewReport();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnApplicationPause || !pauseStatus)
        {
            return;
        }

        SaveEconomyStateToSlot(sessionLoadSlot);
    }

    private void OnApplicationQuit()
    {
        if (!saveOnApplicationQuit)
        {
            return;
        }

        SaveEconomyStateToSlot(sessionLoadSlot);
    }

    private void Disabled_OnGUI()
    {
        if (!showDebugOnGUI)
        {
            return;
        }

        if (InGameMenuManager.IsMenuOpen)
        {
            return;
        }

        if (useBottomHudHost)
        {
            return;
        }

        DrawDebugToggleButton();

        if (!isDebugPanelExpanded)
        {
            return;
        }

        DrawCompactDebugPanel();
    }

    public void DrawBottomHudContent(Rect contentRect)
    {
        EnsureBottomHudStyles();

        GUI.Box(contentRect, GUIContent.none, bottomBoxStyle);

        string content = BuildBottomHudText();
        float availableWidth = contentRect.width - 20f;
        float contentHeight = Mathf.Max(
            contentRect.height - 4f,
            bottomLabelStyle.CalcHeight(new GUIContent(content), availableWidth)
        );

        Rect viewRect = new Rect(
            contentRect.x + 8f,
            contentRect.y + 8f,
            contentRect.width - 16f,
            contentRect.height - 16f
        );

        Rect scrollContentRect = new Rect(0f, 0f, availableWidth, contentHeight + 4f);

        economyBottomScrollPosition = GUI.BeginScrollView(
            viewRect,
            economyBottomScrollPosition,
            scrollContentRect
        );

        GUI.Label(
            new Rect(0f, 0f, availableWidth, contentHeight + 4f),
            content,
            bottomLabelStyle
        );

        GUI.EndScrollView();
    }

    private Vector2 reviewScrollPosition = Vector2.zero;

    public void DrawReviewTabContent(Rect contentRect)
    {
        EnsureBottomHudStyles();
        GUI.Box(contentRect, GUIContent.none, bottomBoxStyle);

        float topSectionHeight = 70f;
        float reviewHeight = 65f;
        float spacing = 4f;
        int reviewCount = state.recentReviews != null ? state.recentReviews.Count : 0;
        
        float totalScrollHeight = topSectionHeight + (reviewCount * (reviewHeight + spacing)) + 20f;
        
        Rect viewRect = new Rect(contentRect.x + 8f, contentRect.y + 8f, contentRect.width - 16f, contentRect.height - 16f);
        Rect scrollContentRect = new Rect(0, 0, viewRect.width - 24f, totalScrollHeight);
        
        reviewScrollPosition = GUI.BeginScrollView(viewRect, reviewScrollPosition, scrollContentRect);

        string topStats =
            $"[전체 평점 요약]\n" +
            $"오늘 평판 점수 {ToReviewStars(state.reputation):0.0}★ ({state.reputation * 100f:0})\n" +
            $"최근 리뷰 추세: {BuildReviewTrendLabel(state.lastReviewDelta)}";
            
        GUI.Label(new Rect(0f, 0f, scrollContentRect.width, topSectionHeight), topStats, bottomLabelStyle);

        float currentY = topSectionHeight + 8f;

        if (reviewCount == 0)
        {
            GUI.Label(new Rect(0f, currentY, scrollContentRect.width, 30f), "아직 등록된 리뷰가 없습니다.", bottomLabelStyle);
        }
        else
        {
            for (int i = reviewCount - 1; i >= 0; i--)
            {
                CustomerReview r = state.recentReviews[i];
                Rect reviewRect = new Rect(0f, currentY, scrollContentRect.width, reviewHeight);
                
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
                GUI.Box(reviewRect, GUIContent.none, GUI.skin.box);
                GUI.backgroundColor = previous;
                
                Rect textRect = new Rect(8f, currentY + 6f, reviewRect.width - 16f, reviewHeight - 12f);
                string authorName = string.IsNullOrEmpty(r.authorName) ? "회원" : r.authorName;
                string reviewText = $"★ {r.stars:0.0} | {authorName} [{r.authorTier}] | {r.year}/{r.month}/{r.day}\n{r.text}";
                GUI.Label(textRect, reviewText, bottomLabelStyle);

                currentY += reviewHeight + spacing;
            }
        }

        GUI.EndScrollView();
    }

    public int GetActiveMemberCount()
    {
        return state.activeMembers;
    }

    public float GetSatisfaction01()
    {
        return state.satisfaction;
    }

    public int GetCurrentTrainerCount()
    {
        return staffManager != null ? staffManager.GetHiredTrainerCount() : trainerCount;
    }

    public float GetCleanliness01()
    {
        return state.cleanliness;
    }

    public int GetCurrentPrestigeEstimate()
    {
        return cachedPrestige;
    }

    public int GetCurrentCapacityEstimate()
    {
        return cachedCapacity;
    }

    public int GetMachineCountEstimate()
    {
        return cachedMachineCount;
    }

    public int GetPreviewDailyNetRevenue()
    {
        return previewReport.netRevenue;
    }

    public string GetOperationStatusLabel()
    {
        return BuildOperationRating();
    }

    public string GetCurrentLocationPreviewLabel()
    {
        return previewReport.locationLabel;
    }

    public string GetAverageBrandLabel()
    {
        return previewReport.averageBrandLabel;
    }

    public int GetHighTierMachineCount()
    {
        return previewReport.highTierMachineCount;
    }

    public int GetGeneralMemberCount()
    {
        return previewReport.generalMembersAtEnd;
    }

    public int GetMiddleMemberCount()
    {
        return previewReport.middleMembersAtEnd;
    }

    public int GetUpperMemberCount()
    {
        return previewReport.upperMembersAtEnd;
    }

    public float GetGeneralMemberRatio01()
    {
        return previewReport.generalMemberRatio;
    }

    public float GetMiddleMemberRatio01()
    {
        return previewReport.middleMemberRatio;
    }

    public float GetUpperMemberRatio01()
    {
        return previewReport.upperMemberRatio;
    }

    public int GetDailyMembershipRevenue()
    {
        return previewReport.membershipRevenue + previewReport.joiningRevenue;
    }

    public int GetDailyPtRevenue()
    {
        return previewReport.ptGymRevenue;
    }

    public int GetDailyAncillaryRevenue()
    {
        return previewReport.ancillaryRevenue;
    }

    public int GetDailyVariableCost()
    {
        return previewReport.variableCost;
    }

    public int GetTotalNetRevenue()
    {
        return state.totalNetRevenue;
    }

    public int GetWaitingCustomersCount()
    {
        return previewReport.waitingCustomers;
    }

    public int GetUsingCustomersCount()
    {
        return previewReport.usingCustomers;
    }

    public int GetPeakWaitingCustomersCount()
    {
        return previewReport.peakWaitingCustomers;
    }

    public float GetAverageWaitSeconds()
    {
        return previewReport.averageWaitSeconds;
    }

    public float GetCurrentReputation01()
    {
        return state.reputation;
    }

    public float GetCurrentReputationStars()
    {
        return ToReviewStars(state.reputation);
    }

    public float GetLastDailyReviewStars()
    {
        return ToReviewStars(state.lastDailyReviewScore);
    }

    public float GetLastReviewDelta01()
    {
        return state.lastReviewDelta;
    }

    public string GetReviewTrendLabel()
    {
        return BuildReviewTrendLabel(state.lastReviewDelta);
    }

    public IReadOnlyList<CustomerReview> GetRecentReviews()
    {
        if (state.recentReviews == null || state.recentReviews.Count <= 0)
        {
            return Array.Empty<CustomerReview>();
        }

        return state.recentReviews;
    }

    public string BuildBottomHudText()
    {
        string operationRating = BuildOperationRating();
        OperationGuide guide = BuildOperationGuide();

        string leadDeltaText = FormatMultiplierDeltaPercent(previewReport.locationLeadMultiplier, invert: false);
        string churnDeltaText = FormatMultiplierDeltaPercent(previewReport.locationChurnMultiplier, invert: true);
        string ancillaryDeltaText = FormatMultiplierDeltaPercent(previewReport.locationAncillaryMultiplier, invert: false);
        string satisfactionTargetText = FormatSignedPercentPoint(previewReport.locationSatisfactionTargetOffset);

        string brandLeadText = FormatMultiplierDeltaPercent(previewReport.brandLeadBonusMultiplier, invert: false);
        string brandAncillaryText = FormatMultiplierDeltaPercent(previewReport.brandAncillaryBonusMultiplier, invert: false);
        string tierMembershipText = FormatMultiplierDeltaPercent(previewReport.tierMembershipRevenueMultiplier, invert: false);
        string tierAncillaryText = FormatMultiplierDeltaPercent(previewReport.tierAncillaryRevenueMultiplier, invert: false);
        string tierPtText = FormatMultiplierDeltaPercent(previewReport.tierPtDemandMultiplier, invert: false);
        string tierChurnText = FormatMultiplierDeltaPercent(previewReport.tierChurnMultiplier, invert: true);
        string tierSatisfactionText = FormatSignedPercentPoint(previewReport.tierSatisfactionTargetBonus);
        string reviewLeadText = FormatMultiplierDeltaPercent(previewReport.reviewLeadMultiplier, invert: false);
        string reviewChurnText = FormatMultiplierDeltaPercent(previewReport.reviewChurnMultiplier, invert: true);
        string reviewAncillaryText = FormatMultiplierDeltaPercent(previewReport.reviewAncillaryMultiplier, invert: false);
        string reviewSatisfactionText = FormatSignedPercentPoint(previewReport.reviewSatisfactionTargetOffset);
        string reviewTrendText = BuildReviewTrendLabel(previewReport.reviewDelta);

        int rCount = 0;
        int tCount = 0;
        int cCount = 0;
        if (staffManager != null) {
            for(int i=0; i<staffManager.HiredStaff.Count; i++) {
                if (staffManager.HiredStaff[i].role == StaffRole.Receptionist) rCount++;
                else if (staffManager.HiredStaff[i].role == StaffRole.Trainer) tCount++;
                else if (staffManager.HiredStaff[i].role == StaffRole.Cleaner) cCount++;
            }
        }
        int totalStaff = rCount + tCount + cCount;
        string staffInfo = totalStaff > 0 ? "직원 " + totalStaff + "명 (안내 " + rCount + ", 트 " + tCount + ", 청소 " + cCount + ")" : "직원 0명";
        return
            "[운영 상태]\n" +
            $"입지 {previewReport.locationLabel} · 평가 {operationRating}\n" +
            $"회원 {state.activeMembers}명 · 만족 {(state.satisfaction * 100f):0}% · 청결 {(state.cleanliness * 100f):0}% · {staffInfo}\n" +
            $"기구 {cachedMachineCount}대 · 수용 {previewReport.capacity}명 · 위신 {previewReport.prestige}\n" +
            $"브랜드 평균 {previewReport.averageBrandLabel} · 상급 기구 {previewReport.highTierMachineCount}대\n\n" +
            "[회원 계층]\n" +
            $"일반 {previewReport.generalMembersAtEnd}명 · 중산 {previewReport.middleMembersAtEnd}명 · 상류 {previewReport.upperMembersAtEnd}명\n" +
            $"중산 {(previewReport.middleMemberRatio * 100f):0}% · 상류 {(previewReport.upperMemberRatio * 100f):0}%\n" +
            $"계층 회비 {tierMembershipText} · 계층 객단가 {tierAncillaryText}\n" +
            $"계층 PT {tierPtText} · 계층 이탈 {tierChurnText} · 계층 만족 {tierSatisfactionText}\n\n" +
            "[리뷰/평판]\n" +
            $"평판 {previewReport.reputationStars:0.0}★ · 점수 {(previewReport.reputationAfter * 100f):0} · 추세 {reviewTrendText}\n" +
            $"오늘 리뷰 {previewReport.dailyReviewStars:0.0}★ · 민감도 x{previewReport.reviewSensitivity:0.00}\n" +
            $"(상세 리뷰 목록은 하단 [리뷰] 탭에서 확인하세요)\n" +
            $"혼잡 리스크 {(previewReport.reviewCrowdingRisk * 100f):0} · 대응 {(previewReport.reviewCrowdingScore * 100f):0}\n" +
            $"품질 {(previewReport.reviewQualityScore * 100f):0} · 가치 {(previewReport.reviewValueScore * 100f):0} · 서비스 {(previewReport.reviewServiceScore * 100f):0}\n" +
            $"리뷰 유입 {reviewLeadText} · 리뷰 이탈 {reviewChurnText} · 리뷰 객단가 {reviewAncillaryText} · 리뷰 만족 {reviewSatisfactionText}\n\n" +
            "[손님 흐름]\n" +
            $"대기 {previewReport.waitingCustomers}명 · 사용 {previewReport.usingCustomers}명\n" +
            $"대기 진입 {previewReport.waitingEvents}회 · 포기 {previewReport.abandonedVisits}회 · 최대 {previewReport.peakWaitingCustomers}명\n" +
            $"평균 {previewReport.averageWaitSeconds:0.0}초 · 회복 {(previewReport.recoveryRate * 100f):0}% · 체감 페널티 {(previewReport.customerExperiencePenalty * 100f):0}%\n" +
            $"혼잡 {(previewReport.congestionSignal * 100f):0}% · 이용 {(previewReport.engagementSignal * 100f):0}% · 압력 {(previewReport.waitPressure * 100f):0}%\n\n" +
            "[운영 영향]\n" +
            $"입지 유입 {leadDeltaText} · 입지 이탈 {churnDeltaText}\n" +
            $"입지 부가매출 {ancillaryDeltaText} · 목표 만족 {satisfactionTargetText}\n" +
            $"브랜드 유입 {brandLeadText} · 브랜드 객단가 {brandAncillaryText}\n\n" +
            "[수익 요약]\n" +
            $"일일 순이익 {previewReport.netRevenue:N0}\n" +
            $"회원권 {previewReport.membershipRevenue:N0} · 가입비 {previewReport.joiningRevenue:N0}\n" +
            $"부가매출 {previewReport.ancillaryRevenue:N0} · PT 수익 {previewReport.ptGymRevenue:N0}\n" +
            $"전기 {previewReport.electricityCost:N0} · 유지 {previewReport.maintenanceCost:N0} · 기타 {previewReport.variableCost:N0}\n" +
            $"누적 순이익 {state.totalNetRevenue:N0}\n\n" +
            "[운영 가이드]\n" +
            $"지금: {guide.action}\n" +
            $"이유: {guide.reason}\n" +
            $"기대: {guide.expectedEffect}";
    }

    private struct OperationGuide
    {
        public string action;
        public string reason;
        public string expectedEffect;
    }


    private struct CustomerTierPrototypeSnapshot
    {
        public float generalRatio;
        public float middleRatio;
        public float upperRatio;
        public int generalCount;
        public int middleCount;
        public int upperCount;
    }

    private string BuildOperationRating()
    {
        if (cachedMachineCount <= 0)
        {
            return "준비";
        }

        if (BuildPlayModeManager.IsBuildMode)
        {
            return "정지";
        }

        if (previewReport.waitPressure >= 0.55f || previewReport.abandonedVisits >= 2 || previewReport.customerExperiencePenalty >= 0.50f)
        {
            return "혼잡";
        }

        if (previewReport.waitPressure >= 0.25f || previewReport.waitingCustomers >= 2 || previewReport.abandonedVisits >= 1)
        {
            return "주의";
        }

        return "원활";
    }

    private OperationGuide BuildOperationGuide()
    {
        if (BuildPlayModeManager.IsBuildMode)
        {
            return new OperationGuide
            {
                action = "플레이 모드로 전환",
                reason = "설치 상태에선 실제 손님 흐름 확인이 멈춤",
                expectedEffect = "실제 혼잡과 유입 변화를 확인"
            };
        }

        if (cachedMachineCount <= 0)
        {
            return new OperationGuide
            {
                action = "기구 먼저 설치",
                reason = "회원 유입을 만들 운영 기반이 아직 없음",
                expectedEffect = "회원 유입 시작 · 일일 손익 형성"
            };
        }

        if (previewReport.waitPressure >= 0.55f || previewReport.abandonedVisits >= 2)
        {
            return new OperationGuide
            {
                action = "기구 추가 또는 이사 검토",
                reason = $"대기 {previewReport.waitingCustomers}명 · 포기 {previewReport.abandonedVisits}회 · 압력 {(previewReport.waitPressure * 100f):0}%",
                expectedEffect = "포기 감소 · 유입 회복 · 부가매출 개선"
            };
        }

        if (previewReport.waitingCustomers >= 2 || previewReport.waitPressure >= 0.25f)
        {
            return new OperationGuide
            {
                action = "인기 기구 1대 더 보강",
                reason = $"대기 {previewReport.waitingCustomers}명 · 평균 {previewReport.averageWaitSeconds:0.0}초 대기",
                expectedEffect = "대기 완화 · 체감 페널티 감소"
            };
        }

        if (previewReport.reputationAfter < 0.45f || previewReport.reviewLeadMultiplier < 0.96f)
        {
            return new OperationGuide
            {
                action = "평판 회복 우선",
                reason = $"평판 {previewReport.reputationStars:0.0}★ · 리뷰 유입 {FormatMultiplierDeltaPercent(previewReport.reviewLeadMultiplier, invert: false)}",
                expectedEffect = "신규 유입 방어 · 이탈 완화"
            };
        }

        if (previewReport.netRevenue < 0)
        {
            if (cachedMachineCount >= 6 && state.activeMembers < Mathf.Max(6, cachedMachineCount * 2))
            {
                return new OperationGuide
                {
                    action = "추가 구매 중단 후 회원 확보",
                    reason = $"기구 {cachedMachineCount}대 대비 회원 {state.activeMembers}명 · 일일 손익 {previewReport.netRevenue:N0}",
                    expectedEffect = "회원 밀도 회복 · 적자 완화"
                };
            }

            return new OperationGuide
            {
                action = "기구 보강 후 흑자 전환 시도",
                reason = $"일일 손익 {previewReport.netRevenue:N0} · 운영비 {previewReport.variableCost:N0}",
                expectedEffect = "방문 회복 · 순이익 개선"
            };
        }

        return new OperationGuide
        {
            action = "현재 운영 유지",
            reason = "혼잡이 낮고 손님 흐름이 안정적",
            expectedEffect = "현금 누적 후 다음 투자 준비"
        };
    }

    private string FormatMultiplierDeltaPercent(float multiplier, bool invert)
    {
        float delta = (multiplier - 1f) * 100f;
        if (invert)
        {
            delta = -delta;
        }

        if (Mathf.Abs(delta) < 0.5f)
        {
            return "0%";
        }

        return $"{(delta > 0f ? "+" : string.Empty)}{delta:0}%";
    }

    private string FormatSignedPercentPoint(float value01)
    {
        float points = value01 * 100f;
        if (Mathf.Abs(points) < 0.5f)
        {
            return "0%p";
        }

        return $"{(points > 0f ? "+" : string.Empty)}{points:0}%p";
    }

    private string GetRandomCustomerName()
    {
        if (UnityEngine.Random.value < 0.2f)
        {
            string[] adjectives = { "열정적인", "헬린이", "지루한", "근육빵빵", "다이어트", "물렁물렁", "득근하는", "벌크업", "단백질 찾는", "쇠질하는", "초보", "강력한", "날렵한", "게으른", "야근한" };
            string[] nouns = { "엘프", "오크", "슬라임", "드워프", "용사", "마법사", "고블린", "수인", "요정", "드래곤", "마왕", "기사", "도적", "성기사" };
            string adj = adjectives[UnityEngine.Random.Range(0, adjectives.Length)];
            string noun = nouns[UnityEngine.Random.Range(0, nouns.Length)];
            return $"{adj} {noun}";
        }
        
        string[] lastNames = { "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임", "한", "오", "서", "신", "권", "황", "안", "송", "전", "홍" };
        string[] firstNames = { "민준", "서준", "도윤", "예준", "시우", "하준", "주원", "지호", "지훈", "준우", "서연", "서윤", "지우", "서현", "하은", "하윤", "민서", "지민", "지유", "채원", "도진", "은우", "수아", "지아", "다은" };
        return lastNames[UnityEngine.Random.Range(0, lastNames.Length)] + firstNames[UnityEngine.Random.Range(0, firstNames.Length)];
    }

    private List<CustomerReview> GenerateDailyReviews(
        int year, int month, int day,
        float dailyStars,
        float reviewDelta,
        float waitPressure,
        float brandQuality,
        float crowdingRisk,
        int joins,
        CustomerTierPrototypeSnapshot tierSnapshot)
    {
        List<CustomerReview> reviews = new List<CustomerReview>();
        if (joins <= 0) return reviews;

        float baseChance = Mathf.Lerp(0.02f, 0.30f, Mathf.Clamp01((dailyStars - 1f) / 4f));

        for (int i=0; i<joins; i++)
        {
            if (UnityEngine.Random.value > baseChance) continue;

            CustomerReview r = new CustomerReview
            {
                year = year,
                month = month,
                day = day,
                stars = Mathf.Clamp(dailyStars + UnityEngine.Random.Range(-1.0f, 1.0f), 1f, 5f),
                authorName = GetRandomCustomerName()
            };

            float rnd = UnityEngine.Random.value;
            if (rnd <= tierSnapshot.upperRatio) 
                r.authorTier = "상류층";
            else if (rnd <= tierSnapshot.upperRatio + tierSnapshot.middleRatio) 
                r.authorTier = "중산층";
            else 
                r.authorTier = "일반";

            if (crowdingRisk >= 0.45f || waitPressure >= 0.5f)
            {
                r.text = "사람이 너무 많아 기구를 쓸 수가 없어요ㅜㅜ";
                if (r.stars > 3f) r.stars = 2.5f;
            }
            else if (r.stars >= 4.5f)
            {
                r.text = brandQuality >= 0.8f ? "최고급 명품 기구! 엄청 잘됩니다." : "관리가 잘 되어있어 매일 갑니다.";
            }
            else if (r.stars >= 3.5f)
            {
                r.text = "무난하게 다닐만한 동네 헬스장입니다.";
            }
            else if (r.stars <= 2.5f)
            {
                r.text = "기구도 별로고 관리도 좀 아쉽네요.";
            }
            else
            {
                r.text = "조금 더 개선되었으면 좋겠습니다.";
            }

            reviews.Add(r);
        }

        return reviews;
    }

    private string BuildReviewTrendLabel(float delta)
    {
        if (delta >= 0.015f)
        {
            return "상승";
        }

        if (delta <= -0.015f)
        {
            return "하락";
        }

        return "보합";
    }

    private float ToReviewStars(float score01)
    {
        return 1f + Mathf.Clamp01(score01) * 4f;
    }

    private string GetAverageBrandLabel(float averageBrandQualityScore)
    {
        if (cachedMachineCount <= 0)
        {
            return "-";
        }

        if (averageBrandQualityScore >= 0.85f)
        {
            return "SS";
        }

        if (averageBrandQualityScore >= 0.53f)
        {
            return "S";
        }

        if (averageBrandQualityScore >= 0.18f)
        {
            return "A";
        }

        return "B";
    }


    private CustomerTierPrototypeSnapshot BuildCustomerTierSnapshot(
        int totalMembers,
        GymLocationType locationType,
        float averageBrandQualityScore,
        int prestige,
        int machineCount,
        float congestionSignal,
        float customerExperiencePenalty)
    {
        CustomerTierPrototypeSnapshot snapshot = new CustomerTierPrototypeSnapshot();

        if (totalMembers <= 0)
        {
            snapshot.generalRatio = 1f;
            snapshot.middleRatio = 0f;
            snapshot.upperRatio = 0f;
            snapshot.generalCount = 0;
            snapshot.middleCount = 0;
            snapshot.upperCount = 0;
            return snapshot;
        }

        float prestigeNormalized = 0f;
        if (machineCount > 0)
        {
            prestigeNormalized = Mathf.Clamp01((float)prestige / Mathf.Max(12f, machineCount * 7f));
        }

        float brandTier01 = Mathf.Clamp01((averageBrandQualityScore - 0.12f) / 0.78f);
        float prestigeTier01 = Mathf.Clamp01((prestigeNormalized - 0.08f) / 0.92f);
        float congestionSeverity = Mathf.Clamp01((congestionSignal * 0.60f) + (customerExperiencePenalty * 0.95f));

        float middleRatio = GetMiddleTierBase(locationType) +
                            (brandTier01 * brandMiddleInfluence) +
                            (prestigeTier01 * prestigeMiddleInfluence);

        float upperRatio = GetUpperTierBase(locationType) +
                           ((brandTier01 * brandTier01) * brandUpperInfluence) +
                           ((prestigeTier01 * prestigeTier01) * prestigeUpperInfluence);

        if (averageBrandQualityScore < 0.10f)
        {
            middleRatio *= 0.75f;
        }

        if (averageBrandQualityScore < 0.18f)
        {
            upperRatio *= 0.10f;
        }

        switch (locationType)
        {
            case GymLocationType.StationArea:
                upperRatio *= 0.90f;
                break;
            case GymLocationType.Downtown:
                upperRatio *= 1.00f;
                break;
            case GymLocationType.Premium:
                upperRatio *= 1.15f;
                break;
            default:
                upperRatio *= 0.35f;
                break;
        }

        middleRatio -= congestionSignal * congestionMiddlePenalty;
        middleRatio -= customerExperiencePenalty * (congestionMiddlePenalty * 1.15f);
        upperRatio -= congestionSignal * congestionUpperPenalty;
        upperRatio -= customerExperiencePenalty * (congestionUpperPenalty * 1.35f);

        // [회원 계층 3차]
        // 혼잡 시엔 중산층보다 상류층이 더 민감하게 줄어들어야 하고,
        // 회원 수가 적을 때는 퍼센트가 출렁이지 않도록 보수적인 기준으로 일부 되돌린다.
        middleRatio -= congestionSeverity * middleCrowdingSuppression;
        upperRatio -= congestionSeverity * upperCrowdingSuppression;

        middleRatio *= Mathf.Lerp(1f, 1f - severeCrowdingMiddleDecay, congestionSeverity);
        upperRatio *= Mathf.Lerp(1f, 1f - severeCrowdingUpperDecay, congestionSeverity);

        float sampleConfidence = Mathf.InverseLerp(
            Mathf.Max(0f, tierSampleStabilizeStartMembers),
            Mathf.Max(tierSampleStabilizeStartMembers + 1f, tierSampleStabilizeFullMembers),
            totalMembers);

        float conservativeMiddleRatio = GetMiddleTierBase(locationType) * lowSampleMiddleFallbackWeight;
        float conservativeUpperRatio = GetUpperTierBase(locationType) * lowSampleUpperFallbackWeight;

        middleRatio = Mathf.Lerp(conservativeMiddleRatio, middleRatio, sampleConfidence);
        upperRatio = Mathf.Lerp(conservativeUpperRatio, upperRatio, sampleConfidence);

        if (congestionSeverity >= 0.45f && totalMembers <= 8)
        {
            upperRatio *= 0.35f;
        }

        if (congestionSeverity >= 0.60f && totalMembers <= 12)
        {
            middleRatio *= 0.80f;
        }

        middleRatio = Mathf.Clamp01(middleRatio);
        upperRatio = Mathf.Clamp01(upperRatio);

        float sum = middleRatio + upperRatio;
        if (sum > 0.78f)
        {
            float scale = 0.78f / sum;
            middleRatio *= scale;
            upperRatio *= scale;
        }

        float generalRatio = Mathf.Clamp01(1f - middleRatio - upperRatio);
        float ratioSum = generalRatio + middleRatio + upperRatio;
        if (ratioSum <= 0f)
        {
            generalRatio = 1f;
            middleRatio = 0f;
            upperRatio = 0f;
        }
        else
        {
            generalRatio /= ratioSum;
            middleRatio /= ratioSum;
            upperRatio /= ratioSum;
        }

        DistributeByRatios(totalMembers, generalRatio, middleRatio, upperRatio, out int generalCount, out int middleCount, out int upperCount);

        snapshot.generalRatio = generalRatio;
        snapshot.middleRatio = middleRatio;
        snapshot.upperRatio = upperRatio;
        snapshot.generalCount = generalCount;
        snapshot.middleCount = middleCount;
        snapshot.upperCount = upperCount;
        return snapshot;
    }

    private float GetMiddleTierBase(GymLocationType locationType)
    {
        switch (locationType)
        {
            case GymLocationType.StationArea:
                return stationMiddleBase;
            case GymLocationType.Downtown:
                return downtownMiddleBase;
            case GymLocationType.Premium:
                return premiumMiddleBase;
            default:
                return neighborhoodMiddleBase;
        }
    }

    private float GetUpperTierBase(GymLocationType locationType)
    {
        switch (locationType)
        {
            case GymLocationType.StationArea:
                return stationUpperBase;
            case GymLocationType.Downtown:
                return downtownUpperBase;
            case GymLocationType.Premium:
                return premiumUpperBase;
            default:
                return neighborhoodUpperBase;
        }
    }

    private float GetLocationReviewSensitivity(GymLocationType locationType)
    {
        switch (locationType)
        {
            case GymLocationType.StationArea:
                return 1.00f;
            case GymLocationType.Downtown:
                return 1.28f;
            case GymLocationType.Premium:
                return 1.10f;
            default:
                return 0.72f;
        }
    }

    private void DistributeByRatios(
        int total,
        float generalRatio,
        float middleRatio,
        float upperRatio,
        out int generalCount,
        out int middleCount,
        out int upperCount)
    {
        if (total <= 0)
        {
            generalCount = 0;
            middleCount = 0;
            upperCount = 0;
            return;
        }

        float generalFloat = total * generalRatio;
        float middleFloat = total * middleRatio;
        float upperFloat = total * upperRatio;

        generalCount = Mathf.FloorToInt(generalFloat);
        middleCount = Mathf.FloorToInt(middleFloat);
        upperCount = Mathf.FloorToInt(upperFloat);

        int assigned = generalCount + middleCount + upperCount;
        int remaining = total - assigned;
        if (remaining <= 0)
        {
            return;
        }

        float generalRemainder = generalFloat - generalCount;
        float middleRemainder = middleFloat - middleCount;
        float upperRemainder = upperFloat - upperCount;

        while (remaining > 0)
        {
            if (generalRemainder >= middleRemainder && generalRemainder >= upperRemainder)
            {
                generalCount++;
                generalRemainder = -1f;
            }
            else if (middleRemainder >= upperRemainder)
            {
                middleCount++;
                middleRemainder = -1f;
            }
            else
            {
                upperCount++;
                upperRemainder = -1f;
            }

            remaining--;
        }
    }

    private void EnsureBottomHudStyles()
    {
        if (bottomBoxStyle == null)
        {
            bottomBoxStyle = new GUIStyle(GUI.skin.box);
            bottomBoxStyle.padding = new RectOffset(8, 8, 8, 8);
        }

        if (bottomLabelStyle == null)
        {
            bottomLabelStyle = new GUIStyle(GUI.skin.label);
            bottomLabelStyle.wordWrap = true;
            bottomLabelStyle.normal.textColor = Color.white;
        }

        bool isPortrait = Screen.height > Screen.width;
        bottomLabelStyle.fontSize = isPortrait ? 12 : 14;
    }

    private void ApplyRecommendedPrototypeBalancePresetIfNeeded()
    {
        if (!forceRecommendedPrototypeBalanceOnAwake)
        {
            return;
        }

        soCapacityScale = 0.60f;
        soPrestigeScale = 0.55f;
        soElectricityCostScale = 0.04f;
        soMaintenanceCostScale = 0.04f;
        soPtDemandScale = 0.20f;

        neighborhoodMiddleBase = 0.01f;
        stationMiddleBase = 0.05f;
        downtownMiddleBase = 0.08f;
        premiumMiddleBase = 0.11f;
        neighborhoodUpperBase = 0.00f;
        stationUpperBase = 0.01f;
        downtownUpperBase = 0.03f;
        premiumUpperBase = 0.06f;
        prestigeMiddleInfluence = 0.14f;
        prestigeUpperInfluence = 0.10f;
        brandMiddleInfluence = 0.24f;
        brandUpperInfluence = 0.18f;
        congestionMiddlePenalty = 0.05f;
        congestionUpperPenalty = 0.10f;
        middleMembershipRevenueMultiplier = 1.03f;
        upperMembershipRevenueMultiplier = 1.09f;
        middleJoiningRevenueMultiplier = 1.06f;
        upperJoiningRevenueMultiplier = 1.16f;
        middleAncillaryRevenueMultiplier = 1.16f;
        upperAncillaryRevenueMultiplier = 1.40f;
        middlePtDemandMultiplier = 1.12f;
        upperPtDemandMultiplier = 1.50f;
        middleTierChurnMultiplier = 0.95f;
        upperTierChurnMultiplier = 0.80f;
        middleTierSatisfactionTargetBonus = 0.015f;
        upperTierSatisfactionTargetBonus = 0.04f;
        middleCrowdingSuppression = 0.08f;
        upperCrowdingSuppression = 0.18f;
        severeCrowdingMiddleDecay = 0.45f;
        severeCrowdingUpperDecay = 0.75f;
        tierSampleStabilizeStartMembers = 8f;
        tierSampleStabilizeFullMembers = 22f;
        lowSampleMiddleFallbackWeight = 0.60f;
        lowSampleUpperFallbackWeight = 0.35f;

        startingReputation = 0.58f;
        reviewDailyLerp = 0.16f;
        reviewNeutralPoint = 0.56f;
        reviewLeadMaxBonus = 0.12f;
        reviewLeadMaxPenalty = 0.18f;
        reviewChurnReduction = 0.05f;
        reviewChurnMaxPenalty = 0.14f;
        reviewAncillaryMaxBonus = 0.08f;
        reviewAncillaryMaxPenalty = 0.06f;
        reviewSatisfactionBonus = 0.015f;
        reviewSatisfactionPenalty = 0.028f;
        middleTierReviewWeight = 0.05f;
        upperTierReviewWeight = 0.20f;
        reviewSampleStabilizeStart = 6f;
        reviewSampleStabilizeFull = 24f;
        trainerServiceReviewBonus = 0.10f;
        reviewOccupancyPenaltyWeight = 0.26f;
        reviewWaitPressurePenaltyWeight = 0.34f;
        reviewCongestionPenaltyWeight = 0.18f;
        reviewAbandonPenaltyWeight = 0.28f;
        reviewExperiencePenaltyWeight = 0.30f;
        reviewSevereCrowdingExtraPenalty = 0.12f;

        maxMembersPerMachine = 4;
        prestigePerMachine = 5;

        leadBase = 0.90f;
        leadPerMachine = 0.15f;
        leadPerPrestige = 0.013f;
        satisfactionJoinMultiplier = 0.50f;
        baseChurnRate = 0.008f;
        crowdingChurnMultiplier = 0.15f;
        lowSatisfactionChurnMultiplier = 0.10f;
        dailyVisitRate = 0.11f;

        membershipPricePerDay = 50;
        joiningFee = 90;

        useOpeningGrowthThrottle = true;
        openingLeadScaleMonth1 = 0.52f;
        openingLeadScaleMonth2 = 0.75f;
        openingLeadScaleMonth3 = 0.88f;
        maxJoinsPerDayMonth1 = 1;
        maxJoinsPerDayMonth2 = 1;

        sameDayJoinMembershipRevenueFactor = 0.25f;
        sameDayJoinServiceCostFactor = 0.25f;
        leavingMemberProrationFactor = 0.50f;

        trainerPtSessionsPerDay = 2;
        ptInterestRate = 0.01f;
        ptPricePerSession = 6000;
        gymPtRevenueShare = 0.15f;
        trainerBaseWagePerDay = 1800;
        ptDemandBonusPerMachine = 0;

        ancillaryPurchaseRate = 0.03f;
        ancillaryAverageSpend = 100;
        electricityCostPerMachinePerDay = 10;
        maintenanceCostPerMachinePerDay = 7;
        consumableCostPerVisitor = 18;
        serviceCostPerActiveMemberPerDay = 18;

        customerExperienceLeadPenalty = 0.12f;
        abandonmentExtraChurn = 0.040f;
        customerExperienceSatisfactionPenalty = 0.10f;
        customerExperienceAncillaryMinMultiplier = 0.90f;

        startingSatisfaction = 0.60f;
        satisfactionBaseTarget = 0.57f;
        baseSatisfactionGainFromPrestige = 0.0005f;
        crowdingSatisfactionPenalty = 0.22f;
        satisfactionLerp = 0.12f;

        if (logBalancePresetApplied)
        {
            Debug.Log(
                "[GymEconomyManager] 통합 밸런스 프리셋 적용 완료. " +
                "회원 계층 3차를 유지하면서 리뷰/평판 1차를 붙여서 유입·이탈에 장기 운영 결과가 보이게 만들었어."
            );
        }
    }

    private void DrawDebugToggleButton()
    {
        float margin = 10f;
        float buttonWidth = 110f;
        float buttonHeight = 28f;

        float buttonX = Screen.width - buttonWidth - margin;
        float buttonY = 48f;

        string label = isDebugPanelExpanded ? "경제 닫기" : "경제 열기";

        if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), label))
        {
            isDebugPanelExpanded = !isDebugPanelExpanded;
        }
    }

    private void DrawCompactDebugPanel()
    {
        float margin = 10f;
        float panelWidth = Mathf.Min(260f, Screen.width - 20f);
        float entryHeight = 18f;
        float extraListHeight = Mathf.Min(4, cachedEquipmentEntries.Count) * entryHeight;
        float panelHeight = (allowDebugTrainerButtons ? 450f : 420f) + extraListHeight;

        float panelX = Screen.width - panelWidth - margin;
        float panelY = 82f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "경제 디버그 [입지 반영 프로토타입]");

        Rect contentRect = new Rect(panelX + 10f, panelY + 24f, panelWidth - 20f, panelHeight - 30f);
        GUILayout.BeginArea(contentRect);

        GUILayout.Label($"슬롯: {sessionLoadSlot}");
        GUILayout.Label($"입지: {previewReport.locationLabel}");
        GUILayout.Label($"기구: {cachedMachineCount}  |  수용: {previewReport.capacity}");
        GUILayout.Label($"Prestige: {previewReport.prestige}  |  PT보정: +{cachedPtDemandBonus}");
        GUILayout.Label($"회원: {state.activeMembers}");
        GUILayout.Label($"만족도: {(state.satisfaction * 100f):0}%");
        GUILayout.Label($"평판: {ToReviewStars(state.reputation):0.0}★ ({state.reputation * 100f:0})");
        GUILayout.Label($"트레이너(임시): {trainerCount}");

        GUILayout.Space(4f);
        GUILayout.Label(
            $"입지 보정: 유입 x{previewReport.locationLeadMultiplier:0.00}, " +
            $"부가매출 x{previewReport.locationAncillaryMultiplier:0.00}"
        );
        GUILayout.Label(
            $"입지 보정: 이탈 x{previewReport.locationChurnMultiplier:0.00}, " +
            $"만족도목표 {(previewReport.locationSatisfactionTargetOffset >= 0f ? "+" : "")}{previewReport.locationSatisfactionTargetOffset:0.00}"
        );

        GUILayout.Space(4f);
        GUILayout.Label($"예상 가입/이탈: +{previewReport.joins} / -{previewReport.leaves}");
        GUILayout.Label($"예상 방문자: {previewReport.visitors}");
        GUILayout.Label($"예상 PT수요/실행: {previewReport.ptDemandEstimate} / {previewReport.ptSessions}");
        GUILayout.Label($"대기 이벤트/포기: {previewReport.waitingEvents} / {previewReport.abandonedVisits}");
        GUILayout.Label($"평균 대기/최대열: {previewReport.averageWaitSeconds:0.0}s / {previewReport.peakWaitingCustomers}");
        GUILayout.Label($"회복률/경험 페널티: {(previewReport.recoveryRate * 100f):0}% / {(previewReport.customerExperiencePenalty * 100f):0}%");

        GUILayout.Space(4f);
        GUILayout.Label($"회원권+가입: {(previewReport.membershipRevenue + previewReport.joiningRevenue):N0}");
        GUILayout.Label($"PT 헬스장 몫: {previewReport.ptGymRevenue:N0}");
        GUILayout.Label($"부가매출: {previewReport.ancillaryRevenue:N0}");

        GUILayout.Space(2f);
        GUILayout.Label($"기구 운영비(일): -{previewReport.equipmentOperatingCost:N0}");
        GUILayout.Label($"서비스비(일): -{previewReport.serviceCost:N0}");
        GUILayout.Label($"소모품(일): -{previewReport.consumableCost:N0}");
        GUILayout.Label($"트레이너 급여(일): -{previewReport.trainerBaseWage:N0}");
        GUILayout.Label($"예상 일일 순이익: {previewReport.netRevenue:N0}");

        GUILayout.Space(4f);
        GUILayout.Label($"SO 배율: 수용 x{soCapacityScale:0.00}, Prestige x{soPrestigeScale:0.00}");
        GUILayout.Label($"SO 운영비 배율: 전기 x{soElectricityCostScale:0.00}, 유지 x{soMaintenanceCostScale:0.00}");

        GUILayout.Space(4f);
        GUILayout.Label("기구 구성:");
        if (cachedEquipmentEntries.Count == 0)
        {
            GUILayout.Label("- 없음");
        }
        else
        {
            int count = Mathf.Min(4, cachedEquipmentEntries.Count);
            for (int i = 0; i < count; i++)
            {
                EquipmentDebugEntry entry = cachedEquipmentEntries[i];
                GUILayout.Label($"- {entry.displayName} x{entry.count}");
            }
        }

        if (lastAppliedReport.year > 0)
        {
            GUILayout.Space(4f);
            GUILayout.Label($"최근 반영: {lastAppliedReport.month}/{lastAppliedReport.day}");
        }

        if (allowDebugTrainerButtons)
        {
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("- 트레이너", GUILayout.Height(24f)))
            {
                trainerCount = Mathf.Max(0, trainerCount - 1);
                state.trainerCount = trainerCount;
                RefreshPreviewReport();
            }

            if (GUILayout.Button("+ 트레이너", GUILayout.Height(24f)))
            {
                trainerCount += 1;
                state.trainerCount = trainerCount;
                RefreshPreviewReport();
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }

    private void RefreshPreviewReport()
    {
        if (TryReadDate(out int year, out int month, out int day))
        {
            previewReport = BuildDailyReport(year, month, day);
            return;
        }

        if (hasDateSnapshot)
        {
            previewReport = BuildDailyReport(lastYear, lastMonth, lastDay);
            return;
        }

        previewReport = BuildDailyReport(1, 1, 1);
    }

    private DailyEconomyReport BuildDailyReport(int year, int month, int day)
    {
        GymLocationPrototypeRules locationRules = GetCurrentLocationRules();
        GymLocationType currentLocationType = gymSiteManager != null ? gymSiteManager.CurrentLocationType : GymLocationType.Neighborhood;

        int machineCount = Mathf.Max(0, cachedMachineCount);
        int capacity = Mathf.Max(0, cachedCapacity);
        int prestige = Mathf.Max(0, cachedPrestige);
        float averageBrandQualityScore = machineCount > 0
            ? Mathf.Clamp01(cachedBrandQualityScoreTotal / machineCount)
            : 0f;
        int highTierMachineCount = Mathf.Max(0, cachedHighTierMachineCount);
        string averageBrandLabel = GetAverageBrandLabel(averageBrandQualityScore);
        float brandLeadBonusMultiplier = Mathf.Lerp(1f, 1f + brandLeadQualityBonus, averageBrandQualityScore);
        float brandAncillaryBonusMultiplier = Mathf.Lerp(1f, 1f + brandAncillaryQualityBonus, averageBrandQualityScore);
        float brandChurnReductionMultiplier = Mathf.Lerp(1f, 1f - brandChurnReduction, averageBrandQualityScore);

        int activeMembersAtStart = Mathf.Max(0, state.activeMembers);
        float satisfactionBefore = Mathf.Clamp01(state.satisfaction);

        int waitingCustomers = 0;
        int usingCustomers = 0;
        int completedVisits = 0;
        int abandonedVisits = 0;
        int waitingEvents = 0;
        int recoveredFromWaiting = 0;
        int peakWaitingCustomers = 0;
        float averageWaitSeconds = 0f;
        float abandonmentRate = 0f;
        float recoveryRate = 0f;
        float waitPressure = 0f;
        float customerExperiencePenalty = 0f;
        float congestionSignal = 0f;
        float engagementSignal = 0f;

        if (useCustomerFlowSignals && customerFlowManager != null)
        {
            waitingCustomers = Mathf.Max(0, customerFlowManager.WaitingCustomerCount);
            usingCustomers = Mathf.Max(0, customerFlowManager.UsingCustomerCount);
            congestionSignal = Mathf.Clamp01(customerFlowManager.GetCongestionSignal01(machineCount));
            engagementSignal = Mathf.Clamp01(customerFlowManager.GetMachineEngagementSignal01(machineCount));

            CustomerFlowManager.CustomerExperienceSnapshot experienceSnapshot = customerFlowManager.GetDailyExperienceSnapshot();
            completedVisits = Mathf.Max(0, experienceSnapshot.completedVisits);
            abandonedVisits = Mathf.Max(0, experienceSnapshot.abandonedVisits);
            waitingEvents = Mathf.Max(0, experienceSnapshot.waitingEvents);
            recoveredFromWaiting = Mathf.Max(0, experienceSnapshot.recoveredFromWaiting);
            peakWaitingCustomers = Mathf.Max(0, experienceSnapshot.peakWaitingCustomers);
            averageWaitSeconds = Mathf.Max(0f, experienceSnapshot.averageWaitSeconds);
            abandonmentRate = Mathf.Clamp01(experienceSnapshot.abandonmentRate);
            recoveryRate = Mathf.Clamp01(experienceSnapshot.recoveryRate);
            waitPressure = Mathf.Clamp01(experienceSnapshot.waitPressure01);
            customerExperiencePenalty = Mathf.Clamp01(
                (waitPressure * 0.55f) +
                (abandonmentRate * 0.85f) -
                (recoveryRate * 0.10f)
            );
        }

        CustomerTierPrototypeSnapshot tierStart = BuildCustomerTierSnapshot(
            activeMembersAtStart,
            currentLocationType,
            averageBrandQualityScore,
            prestige,
            machineCount,
            congestionSignal,
            customerExperiencePenalty);

        float tierMembershipRevenueMultiplier =
            tierStart.generalRatio +
            (tierStart.middleRatio * middleMembershipRevenueMultiplier) +
            (tierStart.upperRatio * upperMembershipRevenueMultiplier);
        float tierJoiningRevenueMultiplier =
            tierStart.generalRatio +
            (tierStart.middleRatio * middleJoiningRevenueMultiplier) +
            (tierStart.upperRatio * upperJoiningRevenueMultiplier);
        float tierAncillaryRevenueMultiplier =
            tierStart.generalRatio +
            (tierStart.middleRatio * middleAncillaryRevenueMultiplier) +
            (tierStart.upperRatio * upperAncillaryRevenueMultiplier);
        float tierPtDemandMultiplier =
            tierStart.generalRatio +
            (tierStart.middleRatio * middlePtDemandMultiplier) +
            (tierStart.upperRatio * upperPtDemandMultiplier);
        float tierChurnMultiplier =
            tierStart.generalRatio +
            (tierStart.middleRatio * middleTierChurnMultiplier) +
            (tierStart.upperRatio * upperTierChurnMultiplier);
        float tierSatisfactionTargetBonus =
            (tierStart.middleRatio * middleTierSatisfactionTargetBonus) +
            (tierStart.upperRatio * upperTierSatisfactionTargetBonus);

        float reputationBefore = state.reputation > 0f
            ? Mathf.Clamp01(state.reputation)
            : startingReputation;
        float reviewSensitivity = GetLocationReviewSensitivity(currentLocationType);
        float reviewPositive01 = Mathf.Clamp01(
            (reputationBefore - reviewNeutralPoint) /
            Mathf.Max(0.05f, 1f - reviewNeutralPoint));
        float reviewNegative01 = Mathf.Clamp01(
            (reviewNeutralPoint - reputationBefore) /
            Mathf.Max(0.05f, reviewNeutralPoint));

        float reviewLeadMultiplier = Mathf.Clamp(
            1f +
            (reviewPositive01 * reviewLeadMaxBonus) -
            (reviewNegative01 * reviewLeadMaxPenalty * reviewSensitivity),
            0.70f,
            1.25f);

        float reviewChurnMultiplier = Mathf.Clamp(
            1f -
            (reviewPositive01 * reviewChurnReduction) +
            (reviewNegative01 * reviewChurnMaxPenalty * reviewSensitivity),
            0.75f,
            1.40f);

        float reviewAncillaryMultiplier = Mathf.Clamp(
            1f +
            (reviewPositive01 * reviewAncillaryMaxBonus) -
            (reviewNegative01 * reviewAncillaryMaxPenalty * reviewSensitivity * 0.70f),
            0.80f,
            1.20f);

        float reviewSatisfactionTargetOffset =
            (reviewPositive01 * reviewSatisfactionBonus) -
            (reviewNegative01 * reviewSatisfactionPenalty * reviewSensitivity);

        float baseLeadPotential =
            (leadBase + (machineCount * leadPerMachine) + (prestige * leadPerPrestige)) *
            Mathf.Max(0.1f, locationRules.leadMultiplier) *
            Mathf.Lerp(0.5f, 1.3f, satisfactionBefore);

        int progressionMonth = GetProgressionMonthIndex(year, month);
        float openingLeadScale = GetOpeningLeadScale(progressionMonth);

        float effectiveLead = baseLeadPotential * openingLeadScale;
        effectiveLead *= brandLeadBonusMultiplier;
        effectiveLead *= reviewLeadMultiplier;

        int receptionistLooks = staffManager != null ? staffManager.GetTotalReceptionistLooks() : 0;
        effectiveLead *= 1f + (receptionistLooks * 0.08f);

        if (useCustomerFlowSignals)
        {
            effectiveLead *= Mathf.Lerp(1f, 1f - congestionLeadPenalty, congestionSignal);
            effectiveLead *= Mathf.Lerp(1f, 1f - customerExperienceLeadPenalty, customerExperiencePenalty);
        }

        int remainingCapacity = Mathf.Max(0, capacity - activeMembersAtStart);

        float rawJoinProgress = Mathf.Max(
            0f,
            (effectiveLead * satisfactionJoinMultiplier) + state.pendingJoinProgress
        );

        int joins = Mathf.Clamp(Mathf.FloorToInt(rawJoinProgress), 0, remainingCapacity);
        joins = Mathf.Min(joins, GetOpeningDailyJoinCap(progressionMonth));
        float pendingJoinProgressAfter = Mathf.Clamp(rawJoinProgress - joins, 0f, 1.25f);

        float occupancy = capacity > 0 ? Mathf.Clamp01((float)activeMembersAtStart / capacity) : 0f;
        float churnRate =
            baseChurnRate *
            Mathf.Max(0.1f, locationRules.churnMultiplier) +
            (occupancy * crowdingChurnMultiplier) +
            (Mathf.Max(0f, 0.55f - satisfactionBefore) * lowSatisfactionChurnMultiplier);

        churnRate *= brandChurnReductionMultiplier;
        churnRate *= Mathf.Max(0.70f, tierChurnMultiplier);
        churnRate *= reviewChurnMultiplier;

        if (useCustomerFlowSignals)
        {
            churnRate += congestionSignal * congestionExtraChurn;
            churnRate += abandonmentRate * abandonmentExtraChurn;
        }

        float rawLeaveProgress = Mathf.Max(
            0f,
            (activeMembersAtStart * churnRate) + state.pendingLeaveProgress
        );

        int leaves = Mathf.Clamp(Mathf.FloorToInt(rawLeaveProgress), 0, activeMembersAtStart);
        float pendingLeaveProgressAfter = Mathf.Clamp(rawLeaveProgress - leaves, 0f, 1.0f);

        int activeMembersAtEnd = Mathf.Max(0, activeMembersAtStart + joins - leaves);

        CustomerTierPrototypeSnapshot tierEnd = BuildCustomerTierSnapshot(
            activeMembersAtEnd,
            currentLocationType,
            averageBrandQualityScore,
            prestige,
            machineCount,
            congestionSignal,
            customerExperiencePenalty);

        tierMembershipRevenueMultiplier =
            tierEnd.generalRatio +
            (tierEnd.middleRatio * middleMembershipRevenueMultiplier) +
            (tierEnd.upperRatio * upperMembershipRevenueMultiplier);
        tierJoiningRevenueMultiplier =
            tierEnd.generalRatio +
            (tierEnd.middleRatio * middleJoiningRevenueMultiplier) +
            (tierEnd.upperRatio * upperJoiningRevenueMultiplier);
        tierAncillaryRevenueMultiplier =
            tierEnd.generalRatio +
            (tierEnd.middleRatio * middleAncillaryRevenueMultiplier) +
            (tierEnd.upperRatio * upperAncillaryRevenueMultiplier);
        tierPtDemandMultiplier =
            tierEnd.generalRatio +
            (tierEnd.middleRatio * middlePtDemandMultiplier) +
            (tierEnd.upperRatio * upperPtDemandMultiplier);
        tierSatisfactionTargetBonus =
            (tierEnd.middleRatio * middleTierSatisfactionTargetBonus) +
            (tierEnd.upperRatio * upperTierSatisfactionTargetBonus);

        float recognizedRevenueMembers = Mathf.Max(
            0f,
            activeMembersAtStart +
            (joins * sameDayJoinMembershipRevenueFactor) -
            (leaves * leavingMemberProrationFactor)
        );

        float recognizedServiceMembers = Mathf.Max(
            0f,
            activeMembersAtStart +
            (joins * sameDayJoinServiceCostFactor) -
            (leaves * leavingMemberProrationFactor)
        );

        int visitors = Mathf.Clamp(
            Mathf.RoundToInt(recognizedRevenueMembers * dailyVisitRate),
            0,
            activeMembersAtEnd
        );

        int membershipRevenue = Mathf.RoundToInt(recognizedRevenueMembers * membershipPricePerDay * tierMembershipRevenueMultiplier);
        int joiningRevenue = Mathf.RoundToInt(joins * joiningFee * tierJoiningRevenueMultiplier);

        float ancillaryFlowMultiplier = 1f;
        if (useCustomerFlowSignals)
        {
            ancillaryFlowMultiplier = Mathf.Lerp(engagementAncillaryMultiplierMin, engagementAncillaryMultiplierMax, engagementSignal);
            ancillaryFlowMultiplier *= Mathf.Lerp(1f, 0.92f, congestionSignal);
            ancillaryFlowMultiplier *= Mathf.Lerp(1f, customerExperienceAncillaryMinMultiplier, customerExperiencePenalty);
        }

        int ancillaryRevenue = Mathf.RoundToInt(
            visitors *
            ancillaryPurchaseRate *
            ancillaryAverageSpend *
            Mathf.Max(0f, locationRules.ancillaryRevenueMultiplier) *
            ancillaryFlowMultiplier *
            brandAncillaryBonusMultiplier *
            tierAncillaryRevenueMultiplier *
            reviewAncillaryMultiplier
        );

        int trainerLooks = staffManager != null ? staffManager.GetTotalTrainerLooks() : 0;
        float ptDemandBonusScale = 1f + (trainerLooks * 0.1f);
        float ptPriceBonusScale = 1f + ((staffManager != null ? staffManager.GetTotalTrainerLeadership() : 0) * 0.05f);

        int ptDemandEstimate = Mathf.RoundToInt(
            ((activeMembersAtEnd * ptInterestRate * tierPtDemandMultiplier) +
            cachedPtDemandBonus +
            ptDemandBonusPerMachine * Mathf.Max(0, machineCount)) * ptDemandBonusScale
        );

        int activeTrainers = staffManager != null ? staffManager.GetHiredTrainerCount() : trainerCount;
        int ptSessions = Mathf.Clamp(ptDemandEstimate, 0, activeTrainers * Mathf.Max(0, trainerPtSessionsPerDay));
        int ptGrossRevenue = Mathf.RoundToInt(ptSessions * ptPricePerSession * ptPriceBonusScale);
        int ptGymRevenue = Mathf.RoundToInt(ptGrossRevenue * gymPtRevenueShare);
        int trainerShareAmount = ptGrossRevenue - ptGymRevenue;
        int trainerBaseWage = staffManager != null ? 0 : (trainerCount * trainerBaseWagePerDay);


        int equipmentOperatingCost = cachedElectricityCost + cachedMaintenanceCost;
        int consumableCost = visitors * consumableCostPerVisitor;
        int serviceCost = Mathf.RoundToInt(recognizedServiceMembers * serviceCostPerActiveMemberPerDay);
        int variableCost = equipmentOperatingCost + consumableCost + serviceCost + trainerBaseWage;

        int netRevenue =
            membershipRevenue +
            joiningRevenue +
            ancillaryRevenue +
            ptGymRevenue -
            variableCost;

        int trainerLeadership = staffManager != null ? staffManager.GetTotalTrainerLeadership() : 0;
        float cleanlinessPenalty = (1f - state.cleanliness) * 0.15f; 

        float targetSatisfaction = Mathf.Clamp01(
            satisfactionBaseTarget +
            (prestige * baseSatisfactionGainFromPrestige) +
            (averageBrandQualityScore * brandSatisfactionTargetBonus) +
            tierSatisfactionTargetBonus +
            reviewSatisfactionTargetOffset -
            (occupancy * crowdingSatisfactionPenalty) +
            locationRules.satisfactionTargetOffset +
            (trainerLeadership * 0.015f) - 
            cleanlinessPenalty
        );

        if (useCustomerFlowSignals)
        {
            targetSatisfaction += Mathf.Lerp(-congestionSatisfactionPenalty, engagementSatisfactionBonus, engagementSignal);
            targetSatisfaction -= congestionSignal * congestionSatisfactionPenalty;
            targetSatisfaction -= customerExperiencePenalty * customerExperienceSatisfactionPenalty;
            targetSatisfaction = Mathf.Clamp01(targetSatisfaction);
        }

        // 인게임 이벤트 만족도 보너스
        if (gymEventManager == null) gymEventManager = FindFirstObjectByType<GymEventManager>();
        if (gymEventManager != null && gymEventManager.IsEventActive)
        {
            targetSatisfaction = Mathf.Clamp01(targetSatisfaction + gymEventManager.ActiveSatisfactionBonus);
        }

        float satisfactionAfter = Mathf.Lerp(satisfactionBefore, targetSatisfaction, satisfactionLerp);
        satisfactionAfter = Mathf.Clamp01(satisfactionAfter);

        float prestigeNormalizedForReview = 0f;
        if (machineCount > 0)
        {
            prestigeNormalizedForReview = Mathf.Clamp01((float)prestige / Mathf.Max(20f, machineCount * 7f));
        }

        float trainerCoverage01 = trainerCount > 0
            ? Mathf.Clamp01((float)trainerCount / Mathf.Max(1f, machineCount * 0.20f))
            : 0f;

        float machineSupportScore = activeMembersAtEnd > 0
            ? Mathf.Clamp01((machineCount * 1.30f) / Mathf.Max(1f, activeMembersAtEnd))
            : 1f;
        float machineSupportRisk = 1f - machineSupportScore;

        float reviewCrowdingRisk = Mathf.Clamp01(
            (occupancy * reviewOccupancyPenaltyWeight) +
            (waitPressure * reviewWaitPressurePenaltyWeight) +
            (congestionSignal * reviewCongestionPenaltyWeight) +
            (abandonmentRate * reviewAbandonPenaltyWeight) +
            (customerExperiencePenalty * reviewExperiencePenaltyWeight) +
            (machineSupportRisk * reviewMachineSupportPenaltyWeight));

        if (waitPressure >= 0.28f || abandonmentRate >= 0.05f || customerExperiencePenalty >= 0.18f)
        {
            reviewCrowdingRisk = Mathf.Clamp01(reviewCrowdingRisk + reviewSevereCrowdingExtraPenalty);
        }

        if (machineSupportScore <= 0.62f || occupancy >= 0.82f)
        {
            reviewCrowdingRisk = Mathf.Clamp01(reviewCrowdingRisk + reviewSevereSupportExtraPenalty);
        }

        float reviewCrowdingScore = Mathf.Clamp01(1f - reviewCrowdingRisk);

        float reviewQualityScore = Mathf.Clamp01(
            0.30f +
            (averageBrandQualityScore * 0.42f) +
            (prestigeNormalizedForReview * 0.20f) +
            (tierEnd.upperRatio * 0.08f) -
            (reviewCrowdingRisk * 0.08f));

        float reviewValueScore = Mathf.Clamp01(
            0.38f +
            (satisfactionAfter * 0.14f) +
            (averageBrandQualityScore * 0.14f) +
            (reviewCrowdingScore * 0.05f) -
            (reviewCrowdingRisk * 0.30f) -
            (customerExperiencePenalty * 0.24f) -
            (machineSupportRisk * 0.18f));

        float reviewServiceScore = Mathf.Clamp01(
            0.34f +
            (satisfactionAfter * 0.15f) +
            (trainerCoverage01 * trainerServiceReviewBonus) +
            (recoveryRate * 0.08f) -
            (reviewCrowdingRisk * 0.24f) -
            (abandonmentRate * 0.24f) -
            (machineSupportRisk * 0.14f));

        float rawDailyReviewScore = Mathf.Clamp01(
            (reviewCrowdingScore * 0.34f) +
            (reviewQualityScore * 0.23f) +
            (reviewValueScore * 0.20f) +
            (reviewServiceScore * 0.23f));

        float reviewInfluenceWeight =
            1f +
            (tierEnd.middleRatio * middleTierReviewWeight) +
            (tierEnd.upperRatio * upperTierReviewWeight);

        float reviewSampleConfidence = Mathf.InverseLerp(
            Mathf.Max(0f, reviewSampleStabilizeStart),
            Mathf.Max(reviewSampleStabilizeStart + 1f, reviewSampleStabilizeFull),
            Mathf.Max(activeMembersAtEnd, visitors));

        float dailyReviewBlend = Mathf.Lerp(reviewImmediateBlendMin, 1.00f, reviewSampleConfidence);
        float dailyReviewScore = Mathf.Lerp(reputationBefore, rawDailyReviewScore, dailyReviewBlend);

        float negativeGap01 = Mathf.Clamp01((reputationBefore - dailyReviewScore) / 0.28f);
        float positiveGap01 = Mathf.Clamp01((dailyReviewScore - reputationBefore) / 0.28f);
        float reviewDirectionScale = negativeGap01 > 0f
            ? Mathf.Lerp(1f, reviewNegativeLerpBoost, negativeGap01)
            : Mathf.Lerp(1f, reviewPositiveLerpDamping, positiveGap01);

        float effectiveReviewLerp = Mathf.Clamp01(
            reviewDailyLerp *
            reviewSensitivity *
            reviewInfluenceWeight *
            Mathf.Lerp(0.90f, 1.15f, reviewSampleConfidence) *
            reviewDirectionScale);

        effectiveReviewLerp = Mathf.Max(effectiveReviewLerp, negativeGap01 > 0f ? 0.06f : 0.03f);

        float reputationAfter = Mathf.Clamp01(Mathf.Lerp(reputationBefore, dailyReviewScore, effectiveReviewLerp));

        // 인게임 이벤트 평판 보너스
        if (gymEventManager != null && gymEventManager.IsEventActive && gymEventManager.ActiveReputationBonus > 0f)
        {
            reputationAfter = Mathf.Clamp01(reputationAfter + gymEventManager.ActiveReputationBonus * effectiveReviewLerp);
        }

        float reviewDelta = reputationAfter - reputationBefore;
        float dailyReviewStars = ToReviewStars(dailyReviewScore);
        float reputationStars = ToReviewStars(reputationAfter);

        List<CustomerReview> generatedReviews = GenerateDailyReviews(
            year, month, day, dailyReviewStars, reviewDelta, waitPressure, averageBrandQualityScore, reviewCrowdingRisk, joins, tierEnd);

        return new DailyEconomyReport
        {
            year = year,
            month = month,
            day = day,

            locationType = currentLocationType,
            locationLabel = GymSiteManager.GetLocationDisplayName(currentLocationType),
            locationLeadMultiplier = locationRules.leadMultiplier,
            locationAncillaryMultiplier = locationRules.ancillaryRevenueMultiplier,
            locationChurnMultiplier = locationRules.churnMultiplier,
            locationSatisfactionTargetOffset = locationRules.satisfactionTargetOffset,

            machineCount = machineCount,
            capacity = capacity,
            prestige = prestige,
            averageBrandQualityScore = averageBrandQualityScore,
            highTierMachineCount = highTierMachineCount,
            averageBrandLabel = averageBrandLabel,
            generalMemberRatio = tierEnd.generalRatio,
            middleMemberRatio = tierEnd.middleRatio,
            upperMemberRatio = tierEnd.upperRatio,
            generalMembersAtEnd = tierEnd.generalCount,
            middleMembersAtEnd = tierEnd.middleCount,
            upperMembersAtEnd = tierEnd.upperCount,
            tierMembershipRevenueMultiplier = tierMembershipRevenueMultiplier,
            tierJoiningRevenueMultiplier = tierJoiningRevenueMultiplier,
            tierAncillaryRevenueMultiplier = tierAncillaryRevenueMultiplier,
            tierPtDemandMultiplier = tierPtDemandMultiplier,
            tierChurnMultiplier = tierChurnMultiplier,
            tierSatisfactionTargetBonus = tierSatisfactionTargetBonus,
            reputationBefore = reputationBefore,
            reputationAfter = reputationAfter,
            dailyReviewScore = dailyReviewScore,
            dailyReviewStars = dailyReviewStars,
            reputationStars = reputationStars,
            reviewDelta = reviewDelta,
            reviewCrowdingScore = reviewCrowdingScore,
            reviewCrowdingRisk = reviewCrowdingRisk,
            reviewQualityScore = reviewQualityScore,
            reviewValueScore = reviewValueScore,
            reviewServiceScore = reviewServiceScore,
            reviewLeadMultiplier = reviewLeadMultiplier,
            reviewChurnMultiplier = reviewChurnMultiplier,
            reviewAncillaryMultiplier = reviewAncillaryMultiplier,
            reviewSatisfactionTargetOffset = reviewSatisfactionTargetOffset,
            reviewSensitivity = reviewSensitivity,
            reviewInfluenceWeight = reviewInfluenceWeight,
            generatedReviews = generatedReviews,

            activeMembersAtStart = activeMembersAtStart,
            joins = joins,
            leaves = leaves,
            activeMembersAtEnd = activeMembersAtEnd,
            pendingJoinProgressAfter = pendingJoinProgressAfter,
            pendingLeaveProgressAfter = pendingLeaveProgressAfter,
            visitors = visitors,
            waitingCustomers = waitingCustomers,
            usingCustomers = usingCustomers,
            completedVisits = completedVisits,
            abandonedVisits = abandonedVisits,
            waitingEvents = waitingEvents,
            recoveredFromWaiting = recoveredFromWaiting,
            peakWaitingCustomers = peakWaitingCustomers,

            membershipRevenue = membershipRevenue,
            joiningRevenue = joiningRevenue,
            ancillaryRevenue = ancillaryRevenue,

            ptDemandEstimate = ptDemandEstimate,
            ptGrossRevenue = ptGrossRevenue,
            ptGymRevenue = ptGymRevenue,
            trainerShareAmount = trainerShareAmount,
            trainerBaseWage = trainerBaseWage,
            ptSessions = ptSessions,

            electricityCost = cachedElectricityCost,
            maintenanceCost = cachedMaintenanceCost,
            equipmentOperatingCost = equipmentOperatingCost,
            consumableCost = consumableCost,
            serviceCost = serviceCost,
            variableCost = variableCost,
            netRevenue = netRevenue,

            occupancy = occupancy,
            brandLeadBonusMultiplier = brandLeadBonusMultiplier,
            brandAncillaryBonusMultiplier = brandAncillaryBonusMultiplier,
            congestionSignal = congestionSignal,
            engagementSignal = engagementSignal,
            averageWaitSeconds = averageWaitSeconds,
            abandonmentRate = abandonmentRate,
            recoveryRate = recoveryRate,
            waitPressure = waitPressure,
            customerExperiencePenalty = customerExperiencePenalty,
            satisfactionBefore = satisfactionBefore,
            satisfactionAfter = satisfactionAfter
        };
    }


    private int GetProgressionMonthIndex(int year, int month)
    {
        int safeYear = Mathf.Max(1, year);
        int safeMonth = Mathf.Clamp(month, 1, 12);
        return ((safeYear - 1) * 12) + safeMonth;
    }

    private float GetOpeningLeadScale(int progressionMonth)
    {
        if (!useOpeningGrowthThrottle)
        {
            return 1f;
        }

        if (progressionMonth <= 1)
        {
            return openingLeadScaleMonth1;
        }

        if (progressionMonth <= 2)
        {
            return openingLeadScaleMonth2;
        }

        if (progressionMonth <= 3)
        {
            return openingLeadScaleMonth3;
        }

        return 1f;
    }

    private int GetOpeningDailyJoinCap(int progressionMonth)
    {
        if (!useOpeningGrowthThrottle)
        {
            return int.MaxValue;
        }

        if (progressionMonth <= 1)
        {
            return Mathf.Max(1, maxJoinsPerDayMonth1);
        }

        if (progressionMonth <= 2)
        {
            return Mathf.Max(1, maxJoinsPerDayMonth2);
        }

        return int.MaxValue;
    }

    private void ApplyDailyEconomy(DailyEconomyReport report)
    {
        state.activeMembers = report.activeMembersAtEnd;
        state.pendingJoinProgress = report.pendingJoinProgressAfter;
        state.pendingLeaveProgress = report.pendingLeaveProgressAfter;
        state.satisfaction = report.satisfactionAfter;
        
        int cleaningSkill = staffManager != null ? staffManager.GetTotalCleaningSkill() : 0;
        state.cleanliness = Mathf.Clamp01(state.cleanliness - 0.1f + (cleaningSkill * 0.05f));
        
        state.reputation = report.reputationAfter;
        state.lastDailyReviewScore = report.dailyReviewScore;
        state.lastReviewDelta = report.reviewDelta;
        
        if (state.recentReviews == null) state.recentReviews = new System.Collections.Generic.List<CustomerReview>();
        if (report.generatedReviews != null)
        {
            state.recentReviews.AddRange(report.generatedReviews);
            if (state.recentReviews.Count > 30)
            {
                state.recentReviews.RemoveRange(0, state.recentReviews.Count - 30);
            }
        }
        
        state.trainerCount = trainerCount;
        state.totalJoined += report.joins;
        state.totalLeft += report.leaves;
        state.totalPtSessions += report.ptSessions;
        state.totalMembershipRevenue += report.membershipRevenue + report.joiningRevenue;
        state.totalPtGymRevenue += report.ptGymRevenue;
        state.totalAncillaryRevenue += report.ancillaryRevenue;
        state.totalVariableCost += report.variableCost;
        state.totalTrainerWages += report.trainerBaseWage;
        state.totalNetRevenue += report.netRevenue;

        TryModifyWallet(report.netRevenue);
        SaveEconomyStateToSlot(sessionLoadSlot);

        lastAppliedReport = report;

        if (logDailySummary)
        {
            Debug.Log(
                $"[GymEconomyManager][LocationPrototype] {report.year}/{report.month}/{report.day} | " +
                $"Location={report.locationLabel}, Machines={report.machineCount}, Capacity={report.capacity}, Prestige={report.prestige}, " +
                $"Members {report.activeMembersAtStart}->{report.activeMembersAtEnd} (G/M/U {report.generalMembersAtEnd}/{report.middleMembersAtEnd}/{report.upperMembersAtEnd}), Joins={report.joins}, Leaves={report.leaves}, Visitors={report.visitors}, " +
                $"WaitEvents={report.waitingEvents}, Abandons={report.abandonedVisits}, AvgWait={report.averageWaitSeconds:0.0}s, ExperiencePenalty={(report.customerExperiencePenalty * 100f):0}%, " +
                $"Reputation={report.reputationStars:0.0}★, ReviewDelta={(report.reviewDelta * 100f):0.0}bp, " +
                $"Membership={report.membershipRevenue + report.joiningRevenue:N0}, PTGym={report.ptGymRevenue:N0}, Ancillary={report.ancillaryRevenue:N0}, " +
                $"EquipOp={report.equipmentOperatingCost:N0}, Service={report.serviceCost:N0}, Consumable={report.consumableCost:N0}, " +
                $"TrainerWage={report.trainerBaseWage:N0}, Net={report.netRevenue:N0}"
            );
        }
    }

    private void LoadEconomyStateForSession()
    {
        state = CreateFreshState();

        if (IsNewGameEntry())
        {
            SaveEconomyStateToSlot(sessionLoadSlot);
            return;
        }

        if (!TryLoadEconomyStateFromSlot(sessionLoadSlot, out EconomyState loadedState))
        {
            SaveEconomyStateToSlot(sessionLoadSlot);
            return;
        }

        state = loadedState;
        trainerCount = loadedState.trainerCount;
    }

    private EconomyState CreateFreshState()
    {
        return new EconomyState
        {
            activeMembers = 0,
            satisfaction = startingSatisfaction,
            cleanliness = 1.0f,
            trainerCount = trainerCount,
            reputation = startingReputation,
            lastDailyReviewScore = startingReputation,
            lastReviewDelta = 0f,
            pendingJoinProgress = 0f,
            pendingLeaveProgress = 0f,
            totalJoined = 0,
            totalLeft = 0,
            totalPtSessions = 0,
            totalMembershipRevenue = 0,
            totalPtGymRevenue = 0,
            totalAncillaryRevenue = 0,
            totalVariableCost = 0,
            totalTrainerWages = 0,
            totalNetRevenue = 0
        };
    }

    private bool TryLoadEconomyStateFromSlot(SlotType slot, out EconomyState loadedState)
    {
        loadedState = default;
        string path = GetEconomySavePath(slot);

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            EconomySidecarSaveData data = JsonUtility.FromJson<EconomySidecarSaveData>(json);
            loadedState = data.economyState;

            if (loadedState.satisfaction <= 0f)
            {
                loadedState.satisfaction = startingSatisfaction;
            }

            if (loadedState.cleanliness <= 0f)
            {
                loadedState.cleanliness = 1.0f;
            }

            if (loadedState.reputation <= 0f)
            {
                loadedState.reputation = startingReputation;
                loadedState.lastDailyReviewScore = startingReputation;
                loadedState.lastReviewDelta = 0f;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GymEconomyManager] 경제 sidecar load 실패: {path}\n{e}");
            return false;
        }
    }

    private void SaveEconomyStateToSlot(SlotType slot)
    {
        string path = GetEconomySavePath(slot);

        EconomySidecarSaveData data = new EconomySidecarSaveData
        {
            version = 11,
            economyState = state
        };

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GymEconomyManager] 경제 sidecar save 실패: {path}\n{e}");
        }
    }

    private void DetectBaseSaveWritesAndMirrorEconomy()
    {
        DetectBaseSaveWriteAndMirror(SlotType.Auto, "autosave.json");
        DetectBaseSaveWriteAndMirror(SlotType.Manual1, "manual_slot_1.json");
        DetectBaseSaveWriteAndMirror(SlotType.Manual2, "manual_slot_2.json");
    }

    private void DetectBaseSaveWriteAndMirror(SlotType slot, string baseSaveFileName)
    {
        string path = Path.Combine(Application.persistentDataPath, baseSaveFileName);

        if (!File.Exists(path))
        {
            return;
        }

        DateTime writeTime = File.GetLastWriteTimeUtc(path);

        if (!knownBaseSaveWriteTimes.TryGetValue(slot, out DateTime previous))
        {
            knownBaseSaveWriteTimes[slot] = writeTime;
            return;
        }

        if (writeTime <= previous)
        {
            return;
        }

        knownBaseSaveWriteTimes[slot] = writeTime;
        SaveEconomyStateToSlot(slot);
    }

    private void CacheBaseSaveWriteTimes()
    {
        knownBaseSaveWriteTimes[SlotType.Auto] = GetBaseSaveWriteTime("autosave.json");
        knownBaseSaveWriteTimes[SlotType.Manual1] = GetBaseSaveWriteTime("manual_slot_1.json");
        knownBaseSaveWriteTimes[SlotType.Manual2] = GetBaseSaveWriteTime("manual_slot_2.json");
    }

    private DateTime GetBaseSaveWriteTime(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(path))
        {
            return DateTime.MinValue;
        }

        return File.GetLastWriteTimeUtc(path);
    }

    private string GetEconomySavePath(SlotType slot)
    {
        string fileName = autoEconomyFileName;

        switch (slot)
        {
            case SlotType.Manual1:
                fileName = manualSlot1EconomyFileName;
                break;

            case SlotType.Manual2:
                fileName = manualSlot2EconomyFileName;
                break;
        }

        return Path.Combine(Application.persistentDataPath, fileName);
    }

    private void ResolveSessionLoadSlot()
    {
        GameEntryRequest.EntryMode mode = (GameEntryRequest.EntryMode)PlayerPrefs.GetInt(EntryModeKey, 0);

        switch (mode)
        {
            case GameEntryRequest.EntryMode.LoadManualSlot1:
                sessionLoadSlot = SlotType.Manual1;
                return;

            case GameEntryRequest.EntryMode.LoadManualSlot2:
                sessionLoadSlot = SlotType.Manual2;
                return;

            case GameEntryRequest.EntryMode.NewGame:
            case GameEntryRequest.EntryMode.ContinueFromAutoSave:
            case GameEntryRequest.EntryMode.None:
            default:
                sessionLoadSlot = SlotType.Auto;
                return;
        }
    }

    private bool IsNewGameEntry()
    {
        GameEntryRequest.EntryMode mode = (GameEntryRequest.EntryMode)PlayerPrefs.GetInt(EntryModeKey, 0);
        return mode == GameEntryRequest.EntryMode.NewGame;
    }

    private void CacheReferences()
    {
        if (walletManager == null)
        {
            walletManager = FindFirstObjectByType<WalletManager>();
        }

        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }

        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (equipmentCatalog == null)
        {
            equipmentCatalog = FindFirstObjectByType<EquipmentCatalog>();
        }

        if (gymSiteManager == null)
        {
            gymSiteManager = FindFirstObjectByType<GymSiteManager>();
        }

        if (customerFlowManager == null)
        {
            customerFlowManager = FindFirstObjectByType<CustomerFlowManager>();
        }

        if (staffManager == null)
        {
            staffManager = FindFirstObjectByType<StaffManager>();
        }
    }

    private void RefreshMachineStatsIfNeeded()
    {
        if (Time.unscaledTime < nextMachineCountRefreshTime)
        {
            return;
        }

        ForceRefreshMachineStats();
    }

    private void ForceRefreshMachineStats()
    {
        nextMachineCountRefreshTime = Time.unscaledTime + machineCountRefreshInterval;

        cachedMachineCount = 0;
        cachedCapacity = 0;
        cachedPrestige = 0;
        cachedElectricityCost = 0;
        cachedMaintenanceCost = 0;
        cachedPtDemandBonus = 0;
        cachedBrandQualityScoreTotal = 0f;
        cachedHighTierMachineCount = 0;
        cachedEquipmentEntries.Clear();

        if (TryBuildMachineStatsFromPlacementData())
        {
            return;
        }

        ForceRefreshLegacyMachineStatsFromGrid();
    }

    private bool TryBuildMachineStatsFromPlacementData()
    {
        if (placementManager == null)
        {
            return false;
        }

        object runtimeDataObject = CallParameterlessMethod(placementManager, "GetPlacedObjectRuntimeData");
        if (runtimeDataObject == null)
        {
            runtimeDataObject = GetFieldValue(placementManager, "placedObjectDataList");
        }

        if (!(runtimeDataObject is IEnumerable enumerable))
        {
            return false;
        }

        Dictionary<string, int> debugCountMap = new Dictionary<string, int>();
        bool foundAny = false;

        foreach (object item in enumerable)
        {
            if (item == null)
            {
                continue;
            }

            foundAny = true;
            cachedMachineCount++;

            PlacementRuntimeInfo info = ExtractPlacementRuntimeInfo(item);
            EquipmentDefinition definition = info.definition != null ? info.definition : ResolveDefinitionById(info.equipmentId);

            if (definition != null)
            {
                int scaledCapacity = Mathf.Max(1, Mathf.RoundToInt(definition.MemberCapacityBonus * soCapacityScale));
                int scaledPrestige = Mathf.Max(1, Mathf.RoundToInt(definition.PrestigeBonus * soPrestigeScale));
                int scaledElectricity = Mathf.Max(0, Mathf.RoundToInt(definition.ElectricityCostPerDay * soElectricityCostScale));
                int scaledMaintenance = Mathf.Max(0, Mathf.RoundToInt(definition.MaintenanceCostPerDay * soMaintenanceCostScale));
                int scaledPtDemand = Mathf.Max(0, Mathf.RoundToInt(definition.PtDemandBonus * soPtDemandScale));

                cachedCapacity += scaledCapacity;
                cachedPrestige += scaledPrestige;
                cachedElectricityCost += scaledElectricity;
                cachedMaintenanceCost += scaledMaintenance;
                cachedPtDemandBonus += scaledPtDemand;
                cachedBrandQualityScoreTotal += Mathf.Clamp01(definition.BrandQualityScore01);

                if (definition.BrandTier == EquipmentBrandTier.S || definition.BrandTier == EquipmentBrandTier.SS)
                {
                    cachedHighTierMachineCount++;
                }

                AddDebugEntry(debugCountMap, $"[{definition.BrandTierLabel}] {definition.DisplayName}");
            }
            else
            {
                int machineUnits = EstimateLegacyMachineUnits(info.width, info.height);

                cachedCapacity += machineUnits * maxMembersPerMachine;
                cachedPrestige += machineUnits * prestigePerMachine;
                cachedElectricityCost += machineUnits * electricityCostPerMachinePerDay;
                cachedMaintenanceCost += machineUnits * maintenanceCostPerMachinePerDay;
                cachedPtDemandBonus += machineUnits * ptDemandBonusPerMachine;

                string label = !string.IsNullOrWhiteSpace(info.displayName)
                    ? info.displayName
                    : (!string.IsNullOrWhiteSpace(info.equipmentId) ? info.equipmentId : $"Legacy {info.width}x{info.height} 기구");

                AddDebugEntry(debugCountMap, label);
            }
        }

        if (!foundAny)
        {
            return true;
        }

        RebuildDebugEntries(debugCountMap);
        return true;
    }

    private PlacementRuntimeInfo ExtractPlacementRuntimeInfo(object item)
    {
        return new PlacementRuntimeInfo
        {
            width = Mathf.Max(1, GetIntMember(item, "width", 1)),
            height = Mathf.Max(1, GetIntMember(item, "height", 1)),
            equipmentId = GetStringMember(item, "equipmentId"),
            displayName = GetStringMember(item, "displayName"),
            definition = GetObjectMember<EquipmentDefinition>(item, "runtimeDefinition")
        };
    }

    private EquipmentDefinition ResolveDefinitionById(string equipmentId)
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            return null;
        }

        if (equipmentCatalog == null)
        {
            equipmentCatalog = FindFirstObjectByType<EquipmentCatalog>();
        }

        if (equipmentCatalog == null)
        {
            return null;
        }

        object exact = CallMethod(equipmentCatalog, "GetDefinitionById", new object[] { equipmentId });
        if (exact is EquipmentDefinition definitionFromMethod)
        {
            return definitionFromMethod;
        }

        object definitionsObject = GetFieldValue(equipmentCatalog, "definitions");
        if (!(definitionsObject is IEnumerable enumerable))
        {
            return null;
        }

        foreach (object item in enumerable)
        {
            if (!(item is EquipmentDefinition def) || def == null)
            {
                continue;
            }

            if (def.EquipmentId == equipmentId)
            {
                return def;
            }
        }

        return null;
    }

    private void ForceRefreshLegacyMachineStatsFromGrid()
    {
        GridCell[] cells = FindObjectsByType<GridCell>(FindObjectsSortMode.None);
        int occupiedCellCount = 0;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null && cells[i].IsOccupied)
            {
                occupiedCellCount++;
            }
        }

        cachedMachineCount = cellsPerMachine <= 0 ? 0 : occupiedCellCount / cellsPerMachine;
        cachedCapacity = cachedMachineCount * maxMembersPerMachine;
        cachedPrestige = cachedMachineCount * prestigePerMachine;
        cachedElectricityCost = cachedMachineCount * electricityCostPerMachinePerDay;
        cachedMaintenanceCost = cachedMachineCount * maintenanceCostPerMachinePerDay;
        cachedPtDemandBonus = cachedMachineCount * ptDemandBonusPerMachine;
        cachedBrandQualityScoreTotal = 0f;
        cachedHighTierMachineCount = 0;

        if (cachedMachineCount > 0)
        {
            cachedEquipmentEntries.Add(new EquipmentDebugEntry
            {
                displayName = "Legacy 2x2 기구",
                count = cachedMachineCount
            });
        }
    }

    private int EstimateLegacyMachineUnits(int width, int height)
    {
        int area = Mathf.Max(1, width) * Mathf.Max(1, height);

        if (cellsPerMachine <= 0)
        {
            return 1;
        }

        return Mathf.Max(1, Mathf.RoundToInt((float)area / cellsPerMachine));
    }

    private void AddDebugEntry(Dictionary<string, int> debugCountMap, string label)
    {
        string safeLabel = string.IsNullOrWhiteSpace(label) ? "이름없는 기구" : label;

        if (debugCountMap.TryGetValue(safeLabel, out int count))
        {
            debugCountMap[safeLabel] = count + 1;
        }
        else
        {
            debugCountMap.Add(safeLabel, 1);
        }
    }

    private void RebuildDebugEntries(Dictionary<string, int> debugCountMap)
    {
        cachedEquipmentEntries.Clear();

        foreach (KeyValuePair<string, int> pair in debugCountMap)
        {
            cachedEquipmentEntries.Add(new EquipmentDebugEntry
            {
                displayName = pair.Key,
                count = pair.Value
            });
        }

        cachedEquipmentEntries.Sort((a, b) =>
        {
            int countCompare = b.count.CompareTo(a.count);
            if (countCompare != 0)
            {
                return countCompare;
            }

            return string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
        });
    }

    private bool TryReadDate(out int year, out int month, out int day)
    {
        year = 1;
        month = 1;
        day = 1;

        if (timeManager == null)
        {
            return false;
        }

        year = Mathf.Max(1, timeManager.CurrentYear);
        month = Mathf.Max(1, timeManager.CurrentMonth);
        day = Mathf.Max(1, timeManager.CurrentDay);
        return true;
    }

    private bool TryModifyWallet(int delta)
    {
        if (walletManager == null || delta == 0)
        {
            return delta == 0;
        }

        if (delta > 0)
        {
            walletManager.AddCash(delta, "회원 경제 정산");
            return true;
        }

        walletManager.SpendMandatory(Mathf.Abs(delta), "회원 경제 정산");
        return true;
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

    private GymLocationType GetCurrentLocationType()
    {
        if (gymSiteManager == null)
        {
            gymSiteManager = FindFirstObjectByType<GymSiteManager>();
        }

        if (gymSiteManager == null)
        {
            return GymLocationType.Neighborhood;
        }

        gymSiteManager.InitializeSiteState();
        return gymSiteManager.CurrentLocationType;
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

    private bool IsSameDate(int yearA, int monthA, int dayA, int yearB, int monthB, int dayB)
    {
        return yearA == yearB && monthA == monthB && dayA == dayB;
    }

    private bool IsLaterDate(int yearA, int monthA, int dayA, int yearB, int monthB, int dayB)
    {
        if (yearA != yearB)
        {
            return yearA > yearB;
        }

        if (monthA != monthB)
        {
            return monthA > monthB;
        }

        return dayA > dayB;
    }

    private float StableHash01(int year, int month, int day, int machineCount, int salt)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + year;
            hash = hash * 31 + month;
            hash = hash * 31 + day;
            hash = hash * 31 + machineCount;
            hash = hash * 31 + salt;

            if (hash < 0)
            {
                hash = -hash;
            }

            return (hash % 1000) / 999f;
        }
    }

    private static object GetFieldValue(object target, string fieldName)
    {
        if (target == null)
        {
            return null;
        }

        FieldInfo field = target.GetType().GetField(fieldName, Flags);
        return field != null ? field.GetValue(target) : null;
    }

    private static object GetPropertyValue(object target, string propertyName)
    {
        if (target == null)
        {
            return null;
        }

        PropertyInfo property = target.GetType().GetProperty(propertyName, Flags);
        return property != null ? property.GetValue(target) : null;
    }

    private static object CallParameterlessMethod(object target, string methodName)
    {
        if (target == null)
        {
            return null;
        }

        MethodInfo method = target.GetType().GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
        return method != null ? method.Invoke(target, null) : null;
    }

    private static object CallMethod(object target, string methodName, object[] parameters)
    {
        if (target == null)
        {
            return null;
        }

        MethodInfo method = target.GetType().GetMethod(methodName, Flags);
        return method != null ? method.Invoke(target, parameters) : null;
    }

    private static int GetIntMember(object target, string name, int defaultValue)
    {
        object value = GetFieldValue(target, name) ?? GetPropertyValue(target, name);

        if (value is int intValue)
        {
            return intValue;
        }

        if (value is float floatValue)
        {
            return Mathf.RoundToInt(floatValue);
        }

        return defaultValue;
    }

    private static string GetStringMember(object target, string name)
    {
        object value = GetFieldValue(target, name) ?? GetPropertyValue(target, name);
        return value as string;
    }

    private static T GetObjectMember<T>(object target, string name) where T : class
    {
        object value = GetFieldValue(target, name) ?? GetPropertyValue(target, name);
        return value as T;
    }
}

