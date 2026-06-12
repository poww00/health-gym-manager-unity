using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [?꾨줈?좏???MVP]
/// ?먮떂 理쒖냼 ?쒓컖??+ ?ㅼ쨷 湲곌뎄 ?ъ슜 猷⑦봽 + 湲곌뎄 ?곹깭 ?ㅻ쾭?덉씠 + ?湲??ш린 + ?좏샇 湲곌뎄 1李?
///
/// ?대쾲 踰꾩쟾 ?듭떖:
/// - ?뚮젅??紐⑤뱶?먯꽌留??먮떂 猷⑦봽 吏꾪뻾
/// - ?ㅼ튂 紐⑤뱶濡??ㅼ뼱媛硫??먮떂? ?щ씪吏吏 ?딄퀬 洹몃?濡??뺤?
/// - ?ㅼ튂 紐⑤뱶?먯꽌???덉빟/?ъ슜 ?ㅻ쾭?덉씠留??④?
/// - ?뚮젅??紐⑤뱶濡??뚯븘?ㅻ㈃ 硫덉톬???먮떂???댁뼱???ㅼ떆 ?吏곸엫
/// - ?먰븯??湲곌뎄媛 ?놁쑝硫??좉퉸 ?湲?/// - ?湲??쒓컙??吏?섎룄 紐??≪쑝硫??댁옣
/// - ?먮떂? ?좏샇 湲곌뎄 怨꾩뿴??癒쇱? 李얘퀬, ?놁쑝硫??ㅻⅨ 湲곌뎄瑜??ъ슜
/// - ?쇱옟 ?뚯뒪?몄슜 媛뺤젣 ?섏슂 紐⑤뱶??湲곕낯 OFF
/// - ?먮떂 ?곹깭蹂???媛꾨떒 ?꾩뒪 ?곗텧 異붽?
///
/// ?꾩쭅 ???섎뒗 ??
/// - ?뺢탳??湲몄갼湲?/// - 異⑸룎 ?뚰뵾
/// - ???遺덈윭?ㅺ린
/// - 寃쎌젣? 1:1 ?뺣? ?곕룞
/// - ?ㅼ젣 罹먮┃???ㅽ봽?쇱씠???좊땲硫붿씠??/// </summary>
[DefaultExecutionOrder(1200)]
public sealed class CustomerFlowManager : MonoBehaviour
{
    public enum CustomerState
    {
        MovingToMachine,
        UsingMachine,
        WaitingForMachine,
        Leaving,
    }

    public enum CustomerLeaveReason
    {
        None,
        CompletedVisit,
        WaitTimeout,
        NoMachinesAvailable,
        LayoutChanged,
    }

    public struct CustomerExperienceSnapshot
    {
        public int spawnedCustomers;
        public int completedVisits;
        public int abandonedVisits;
        public int waitingEvents;
        public int recoveredFromWaiting;
        public int peakWaitingCustomers;
        public float totalWaitSeconds;
        public float averageWaitSeconds;
        public float abandonmentRate;
        public float waitPressure01;
        public float recoveryRate;
    }

    private enum MachineOverlayState
    {
        None,
        Reserved,
        InUse
    }

    public enum MachinePreferenceGroup
    {
        Cardio,
        Push,
        Pull,
        Leg,
        FreeWeight,
        Recovery,
        Facility,
        Other
    }

    public sealed class ActiveCustomer
    {
        public GameObject visual;
        public SpriteRenderer renderer;

        public string targetMachineKey;
        public Vector3 targetMachineWorldPosition;
        public Vector3 worldPosition;

        public float remainingUseSeconds;
        public int remainingMachineStops;
        public CustomerState state;
        public CustomerLeaveReason leaveReason = CustomerLeaveReason.None;
        public bool isPtCustomer = false;
        public string assignedTrainerId = "";

        public int waitSlotIndex = -1;
        public float accumulatedWaitSeconds = 0f;
        public int experiencedWaitCount = 0;
        public float remainingWaitSeconds = 0f;
        public float retrySearchCountdown = 0f;

        public MachinePreferenceGroup primaryPreference = MachinePreferenceGroup.Other;
        public MachinePreferenceGroup secondaryPreference = MachinePreferenceGroup.Other;

        public readonly HashSet<string> visitedMachineKeys = new HashSet<string>();
    }

    private sealed class MachineRuntimeInfo
    {
        public string key;
        public PlacedObjectSaveData data;
        public Vector3 centerWorldPosition;
        public MachinePreferenceGroup preferenceGroup;
    }

    private sealed class OperationFeedEntry
    {
        public string message;
        public Color color;
        public float remainingSeconds;
    }

    [Header("References (鍮꾩썙?먮㈃ ?먮룞 ?먯깋)")]
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GymEconomyManager gymEconomyManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private GymEventManager gymEventManager;


    [Header("Prototype Runtime")]
    [SerializeField] private bool runOnlyInPlayMode = true;
    [SerializeField] private bool pauseWhenMenuOpen = true;

    [Header("Prototype Demand")]
    [SerializeField] private bool allowMinimumPrototypeCustomerWhenMachinesExist = true;
    [SerializeField] private int minimumPrototypeVisibleCustomers = 1;
    [SerializeField] private int activeMembersPerVisibleCustomer = 6;
    [SerializeField] private int maxConcurrentCustomers = 12;
    [SerializeField] private float spawnIntervalMinSeconds = 0.85f;
    [SerializeField] private float spawnIntervalMaxSeconds = 1.65f;

    [Header("Crowding Stress Test (Prototype)")]
    [SerializeField] private bool enableCrowdingStressTest = false;
    [SerializeField] private int forcedTargetVisibleCustomers = 10;

    [Header("Prototype Session")]
    [SerializeField] private int minMachinesPerVisit = 2;
    [SerializeField] private int maxMachinesPerVisit = 4;
    [SerializeField] private float machineUseDurationMinSeconds = 1.8f;
    [SerializeField] private float machineUseDurationMaxSeconds = 3.5f;

    [Header("Waiting / Congestion (Prototype)")]
    [SerializeField] private float waitingRetryIntervalSeconds = 0.75f;
    [SerializeField] private float maximumWaitingSeconds = 6.0f;
    [SerializeField] private int maxVisibleWaitingSlots = 8;
    [SerializeField] private float waitingSlotSpacingY = 0.8f;

    [Header("Prototype Motion")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float entryExitOffsetX = 1.6f;
    [SerializeField] private float entryExitHeightRatio = 0.18f;

    [Header("Prototype Visual")]
    [SerializeField] private Color movingCustomerColor = new Color(0.85f, 1f, 0.95f, 0.95f);
    [SerializeField] private Color usingCustomerColor = new Color(0.60f, 1f, 0.60f, 0.98f);
    [SerializeField] private Color waitingCustomerColor = new Color(1f, 0.95f, 0.75f, 0.95f);
    [SerializeField] private Color leavingCustomerColor = new Color(1f, 0.75f, 0.75f, 0.90f);
    [SerializeField] private Vector2 customerVisualSize = new Vector2(0.34f, 0.34f);
    [SerializeField] private int customerSortingOrder = 30;
    [SerializeField] private Vector3 layeredCustomerHeadLocalOffset = new Vector3(0f, 0.68f, 0f);
    [SerializeField] private float usingPulseAmplitude = 0.14f;
    [SerializeField] private float usingPulseSpeed = 8f;
    [SerializeField] private float waitingPulseAmplitude = 0.07f;
    [SerializeField] private float waitingPulseSpeed = 5f;

    [Header("Machine Overlay")]
    [SerializeField] private bool showMachineStateOverlay = false;
    [SerializeField] private Color reservedOverlayColor = new Color(0.45f, 0.75f, 1f, 0.30f);
    [SerializeField] private Color inUseOverlayColor = new Color(0.45f, 1f, 0.45f, 0.34f);
    [SerializeField] private int overlaySortingOrder = 12;

    [Header("Debug")]
    [SerializeField] private bool showDebugOnGUI = false;

    [Header("Operation Feedback (Prototype)")]
    [SerializeField] private bool showOperationFeed = true;
    [SerializeField] private int maxOperationFeedEntries = 5;
    [SerializeField] private float operationFeedEntryLifetimeSeconds = 6f;
    [SerializeField] private Color operationFeedInfoColor = new Color(0.82f, 1f, 0.92f, 1f);
    [SerializeField] private Color operationFeedWarnColor = new Color(1f, 0.93f, 0.64f, 1f);
    [SerializeField] private Color operationFeedAlertColor = new Color(1f, 0.72f, 0.72f, 1f);

    private static Sprite cachedWhiteSprite;

    public readonly List<ActiveCustomer> activeCustomers = new List<ActiveCustomer>();
    private readonly HashSet<string> reservedMachineKeys = new HashSet<string>();
    private readonly Dictionary<string, SpriteRenderer> machineOverlayMap = new Dictionary<string, SpriteRenderer>();

    private Transform runtimeRoot;
    private Transform overlayRoot;
    private float spawnCountdownSeconds;
    private bool isInitialized;

    private int dailySpawnedCustomers;
    private int dailyCompletedVisits;
    private int dailyAbandonedVisits;
    private int dailyWaitingEvents;
    private int dailyRecoveredFromWaiting;
    private int dailyPeakWaitingCustomers;
    private float dailyTotalWaitSeconds;

    private GUIStyle debugBoxStyle;
    private GUIStyle debugLabelStyle;
    private GUIStyle debugHeaderStyle;

    private readonly List<OperationFeedEntry> operationFeedEntries = new List<OperationFeedEntry>();
    private string currentFlowSummary = "?먮떂 ?놁쓬";
    private string currentBottleneckSummary = "蹂묐ぉ ?놁쓬";
    private string currentGuideSummary = "湲곌뎄 ?ㅼ튂 ???뚮젅???쒖옉";
    private string currentTopMachineSummary = "二쇱슂 湲곌뎄 ?놁쓬";

    public int ActiveCustomerCount => activeCustomers.Count;

    public int WaitingCustomerCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < activeCustomers.Count; i++)
            {
                if (activeCustomers[i] != null &&
                    activeCustomers[i].state == CustomerState.WaitingForMachine)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int UsingCustomerCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < activeCustomers.Count; i++)
            {
                if (activeCustomers[i] != null &&
                    activeCustomers[i].state == CustomerState.UsingMachine)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public float GetCongestionSignal01(int machineCountHint)
    {
        int denominator = Mathf.Max(1, Mathf.Max(machineCountHint, maxVisibleWaitingSlots));
        return Mathf.Clamp01((float)WaitingCustomerCount / denominator);
    }

    public float GetMachineEngagementSignal01(int machineCountHint)
    {
        int engagedCount = 0;

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            if (activeCustomers[i] == null)
            {
                continue;
            }

            if (activeCustomers[i].state == CustomerState.MovingToMachine ||
                activeCustomers[i].state == CustomerState.UsingMachine)
            {
                engagedCount++;
            }
        }

        int denominator = Mathf.Max(1, machineCountHint);
        return Mathf.Clamp01((float)engagedCount / denominator);
    }

    public CustomerExperienceSnapshot GetDailyExperienceSnapshot()
    {
        int resolvedVisits = Mathf.Max(0, dailyCompletedVisits + dailyAbandonedVisits);
        float averageWaitSeconds = dailyWaitingEvents > 0
            ? dailyTotalWaitSeconds / dailyWaitingEvents
            : 0f;
        float abandonmentRate = resolvedVisits > 0
            ? (float)dailyAbandonedVisits / resolvedVisits
            : 0f;
        float recoveryRate = dailyWaitingEvents > 0
            ? (float)dailyRecoveredFromWaiting / dailyWaitingEvents
            : 0f;
        float waitPressure01 = Mathf.Clamp01(
            ((averageWaitSeconds / Mathf.Max(0.5f, maximumWaitingSeconds)) * 0.60f) +
            (((float)dailyPeakWaitingCustomers / Mathf.Max(1, maxVisibleWaitingSlots)) * 0.40f)
        );

        return new CustomerExperienceSnapshot
        {
            spawnedCustomers = dailySpawnedCustomers,
            completedVisits = dailyCompletedVisits,
            abandonedVisits = dailyAbandonedVisits,
            waitingEvents = dailyWaitingEvents,
            recoveredFromWaiting = dailyRecoveredFromWaiting,
            peakWaitingCustomers = dailyPeakWaitingCustomers,
            totalWaitSeconds = dailyTotalWaitSeconds,
            averageWaitSeconds = averageWaitSeconds,
            abandonmentRate = abandonmentRate,
            waitPressure01 = waitPressure01,
            recoveryRate = recoveryRate,
        };
    }

    public void ResetDailyExperienceMetrics(string reason = "")
    {
        dailySpawnedCustomers = 0;
        dailyCompletedVisits = 0;
        dailyAbandonedVisits = 0;
        dailyWaitingEvents = 0;
        dailyRecoveredFromWaiting = 0;
        dailyPeakWaitingCustomers = 0;
        dailyTotalWaitSeconds = 0f;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[CustomerFlowManager] ?쇱씪 ?먮떂 寃쏀뿕 ?듦퀎 由ъ뀑 / ?ъ쑀: {reason}");
        }
    }

    public void InitializePrototype()
    {
        if (isInitialized)
        {
            return;
        }

        ResolveReferences();

        if (placementManager == null || gridManager == null)
        {
            Debug.LogWarning("[CustomerFlowManager] ?꾩슂??李몄“媛 ?꾩쭅 ?놁뼱??珥덇린?붾? 蹂대쪟??");
            return;
        }

        runtimeRoot = EnsureRuntimeRoot();
        overlayRoot = EnsureOverlayRoot();
        spawnCountdownSeconds = GetNextSpawnInterval();
        ResetDailyExperienceMetrics("珥덇린??);

        placementManager.PlayerPlacedObject -= HandlePlacementChanged;
        placementManager.PlayerPlacedObject += HandlePlacementChanged;

        isInitialized = true;
        Debug.Log("[CustomerFlowManager] ?먮떂 ?ㅼ쨷 湲곌뎄 + ?湲??ш린 ?꾨줈?좏???珥덇린???꾨즺");
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        InitializePrototype();
    }

    private void OnDestroy()
    {
        if (placementManager != null)
        {
            placementManager.PlayerPlacedObject -= HandlePlacementChanged;
        }

        ClearAllCustomersAndOverlays();
    }

    private void Update()
    {
        UpdateOperationFeedLifetimes(Time.unscaledDeltaTime);

        if (!isInitialized)
        {
            InitializePrototype();
            if (!isInitialized)
            {
                return;
            }
        }

        ResolveReferences();

        if (pauseWhenMenuOpen && InGameMenuManager.IsMenuOpen)
        {
            return;
        }

        if (timeManager != null && timeManager.IsPaused)
        {
            return;
        }

        IReadOnlyList<PlacedObjectSaveData> placedObjects = placementManager != null
            ? placementManager.GetPlacedObjectRuntimeData()
            : null;

        List<MachineRuntimeInfo> machines = BuildMachineRuntimeInfos(placedObjects);

        bool isBuildMode = runOnlyInPlayMode && BuildPlayModeManager.IsBuildMode;
        if (isBuildMode)
        {
            UpdateOperationRuntimeSummary(machines, true);
            HideAllMachineOverlays();
            return;
        }

        if (machines.Count <= 0)
        {
            if (activeCustomers.Count > 0)
            {
                ConvertAllCustomersToLeaving();
            }

            UpdateCustomers(timeManager != null ? timeManager.GetSimulationDeltaTime() : Time.deltaTime, machines);
            RebuildMachineOverlays(machines);
            UpdateOperationRuntimeSummary(machines, false);
            return;
        }

        float simulationDeltaTime = timeManager != null
            ? timeManager.GetSimulationDeltaTime()
            : Time.deltaTime;

        UpdateCustomers(simulationDeltaTime, machines);
        TrySpawnCustomer(simulationDeltaTime, machines);
        RebuildMachineOverlays(machines);
        UpdateOperationRuntimeSummary(machines, false);
    }

    private void HandlePlacementChanged()
    {
        IReadOnlyList<PlacedObjectSaveData> placedObjects = placementManager != null
            ? placementManager.GetPlacedObjectRuntimeData()
            : null;

        List<MachineRuntimeInfo> machines = BuildMachineRuntimeInfos(placedObjects);
        ReconcileCustomersAfterLayoutChanged(machines);
        spawnCountdownSeconds = GetNextSpawnInterval();
    }

    private void ResolveReferences()
    {
        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (gymEconomyManager == null)
        {
            gymEconomyManager = FindFirstObjectByType<GymEconomyManager>();
        }

        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }

        if (gymEventManager == null)
        {
            gymEventManager = FindFirstObjectByType<GymEventManager>();
        }
    }

    private RelocationManager cachedRelocationManager;

    private void TrySpawnCustomer(float simulationDeltaTime, List<MachineRuntimeInfo> machines)
    {
        if (machines == null || machines.Count <= 0)
        {
            return;
        }

        // ?댁궗 以묒뿉???좉퇋 ?먮떂 ?ㅽ룿 以묐떒
        if (cachedRelocationManager == null)
        {
            cachedRelocationManager = FindFirstObjectByType<RelocationManager>();
        }
        if (cachedRelocationManager != null && cachedRelocationManager.IsUnderRelocation)
        {
            return;
        }

        spawnCountdownSeconds -= simulationDeltaTime;
        if (spawnCountdownSeconds > 0f)
        {
            return;
        }

        int targetVisibleCustomerCount = GetTargetVisibleCustomerCount(machines.Count);
        if (activeCustomers.Count >= targetVisibleCustomerCount)
        {
            spawnCountdownSeconds = GetNextSpawnInterval();
            return;
        }

        ActiveCustomer customer = CreateCustomer(GetEntryWorldPosition());
        AssignPreferencesFromMachines(customer, machines);
        customer.remainingMachineStops = GetPlannedMachineStops(machines.Count);

        StaffManager tempStaffMgr = FindFirstObjectByType<StaffManager>();
        if (tempStaffMgr != null)
        {
            var trainers = tempStaffMgr.HiredStaff.Where(s => s.role == StaffRole.Trainer && s.ptMemberCount > 0).ToList();
            if (trainers.Count > 0)
            {
                int totalAssigned = trainers.Sum(t => t.ptMemberCount);
                if (UnityEngine.Random.value < (totalAssigned * 0.05f))
                {
                    var chosenTrainer = trainers[UnityEngine.Random.Range(0, trainers.Count)];
                    customer.isPtCustomer = true;
                    customer.assignedTrainerId = chosenTrainer.staffId;
                }
            }
        }

        if (!TryAssignNextMachine(customer, machines))
        {
            EnterWaitingState(customer);
        }

        activeCustomers.Add(customer);
        dailySpawnedCustomers += 1;
        spawnCountdownSeconds = GetNextSpawnInterval();
    }

    private int GetTargetVisibleCustomerCount(int machineCount)
    {
        if (enableCrowdingStressTest)
        {
            return Mathf.Max(1, forcedTargetVisibleCustomers);
        }

        int activeMembers = gymEconomyManager != null ? gymEconomyManager.GetActiveMemberCount() : 0;
        int targetByMembers = activeMembers / Mathf.Max(1, activeMembersPerVisibleCustomer);

        // 湲곌뎄媛 ?덇퀬 理쒖냼 ??紐낆? 諛⑸Ц?섎룄濡??덉슜?섎뒗 ?듭뀡
        if (allowMinimumPrototypeCustomerWhenMachinesExist && machineCount > 0 && targetByMembers < minimumPrototypeVisibleCustomers)
        {
            targetByMembers = minimumPrototypeVisibleCustomers;
        }

        float eventMultiplier = 1f;
        if (gymEventManager != null)
        {
            eventMultiplier = gymEventManager.ActiveSpawnMultiplier;
        }

        // ?ㅼ젣 ?뚯썝 ??湲곕컲 ?寃잛뿉 ?대깽??諛곗쑉 ?곸슜
        float scaled = targetByMembers * Mathf.Max(0.1f, eventMultiplier);
        
        // 理쒕? ?숈떆 ?묒냽?????쒗븳
        return Mathf.Clamp(Mathf.RoundToInt(scaled), 0, maxConcurrentCustomers);
    }

    private int GetPlannedMachineStops(int machineCount)
    {
        int safeMin = Mathf.Clamp(minMachinesPerVisit, 1, machineCount);
        int safeMax = Mathf.Clamp(maxMachinesPerVisit, safeMin, machineCount);
        int rolled = Random.Range(safeMin, safeMax + 1);
        return Mathf.Clamp(rolled, 1, Mathf.Max(1, machineCount));
    }

    private ActiveCustomer CreateCustomer(Vector3 spawnWorldPosition)
    {
        GameObject visual = new GameObject("Customer");
        visual.transform.SetParent(EnsureRuntimeRoot(), false);
        visual.transform.position = spawnWorldPosition;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = customerVisualSize;
        renderer.color = movingCustomerColor;
        renderer.sortingOrder = customerSortingOrder;
        renderer.enabled = false; // Disable debug visual as we use LayeredCustomerAnimator now

        ActiveCustomer customer = new ActiveCustomer
        {
            visual = visual,
            renderer = renderer,
            state = CustomerState.MovingToMachine,
            worldPosition = spawnWorldPosition,
            leaveReason = CustomerLeaveReason.None,
            waitSlotIndex = -1,
        };

        // 새로 생성된 애니메이터 부착
        LayeredCustomerAnimator animator = visual.AddComponent<LayeredCustomerAnimator>();
        animator.Initialize(customer);

        return customer;
    }

    private void UpdateCustomers(float simulationDeltaTime, List<MachineRuntimeInfo> machines)
    {
        if (simulationDeltaTime <= 0f)
        {
            return;
        }

        float moveStep = Mathf.Max(0.01f, moveSpeed) * simulationDeltaTime;
        Vector3 exitPos = GetExitWorldPosition();

        for (int i = activeCustomers.Count - 1; i >= 0; i--)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null)
            {
                activeCustomers.RemoveAt(i);
                continue;
            }

            switch (customer.state)
            {
                case CustomerState.MovingToMachine:
                {
                    Vector3 target = customer.targetMachineWorldPosition;
                    float dist = Vector3.Distance(customer.worldPosition, target);

                    if (dist <= moveStep)
                    {
                        customer.worldPosition = target;
                        customer.state = CustomerState.UsingMachine;
                        ApplyCustomerVisualForState(customer);
                    }
                    else
                    {
                        customer.worldPosition = Vector3.MoveTowards(customer.worldPosition, target, moveStep);
                    }

                    if (customer.visual != null)
                    {
                        customer.visual.transform.position = customer.worldPosition;
                    }

                    break;
                }

                case CustomerState.UsingMachine:
                {
                    customer.remainingUseSeconds -= simulationDeltaTime;
                    UpdateCustomerVisualAnimation(customer);

                    if (customer.remainingUseSeconds <= 0f)
                    {
                        if (machines != null)
                        {
                            var machine = machines.FirstOrDefault(m => m.key == customer.targetMachineKey);
                            if (machine != null && machine.data != null && machine.data.runtimeDefinition != null)
                            {
                                float breakdownChance = EquipmentBrandTierRules.GetBreakdownChancePerUse(machine.data.runtimeDefinition.BrandTier);
                                if (UnityEngine.Random.value < breakdownChance)
                                {
                                    machine.data.isBroken = true;
                                    Debug.Log($"[CustomerFlowManager] 湲곌뎄 怨좎옣 諛쒖깮! ({machine.data.runtimeDefinition.DisplayName})");
                                    PushOperationFeed("湲곌뎄媛 怨좎옣?ъ뒿?덈떎! ?섎━媛 ?꾩슂??, operationFeedAlertColor);
                                }
                            }
                        }

                        ReleaseReservationIfNeeded(customer);
                        customer.remainingMachineStops -= 1;

                        if (customer.remainingMachineStops > 0 && machines != null && machines.Count > 0)
                        {
                            if (!TryAssignNextMachine(customer, machines))
                            {
                                EnterWaitingState(customer);
                            }
                        }
                        else
                        {
                            BeginLeaving(customer, CustomerLeaveReason.CompletedVisit);
                        }
                    }

                    break;
                }

                case CustomerState.WaitingForMachine:
                {
                    customer.remainingWaitSeconds -= simulationDeltaTime;
                    customer.retrySearchCountdown -= simulationDeltaTime;
                    dailyTotalWaitSeconds += simulationDeltaTime;
                    UpdateCustomerVisualAnimation(customer);

                    // Move to waiting slot visual position
                    if (customer.waitSlotIndex >= 0)
                    {
                        Vector3 slotPos = GetWaitingWorldPosition(customer.waitSlotIndex);
                        customer.worldPosition = Vector3.MoveTowards(customer.worldPosition, slotPos, moveStep);
                        if (customer.visual != null)
                        {
                            customer.visual.transform.position = customer.worldPosition;
                        }
                    }

                    if (customer.retrySearchCountdown <= 0f)
                    {
                        customer.retrySearchCountdown = Mathf.Max(0.15f, waitingRetryIntervalSeconds);

                        if (TryAssignNextMachine(customer, machines))
                        {
                            break;
                        }
                    }

                    if (customer.remainingWaitSeconds <= 0f)
                    {
                        BeginLeaving(customer, CustomerLeaveReason.WaitTimeout);
                    }

                    break;
                }

                case CustomerState.Leaving:
                {
                    float dist = Vector3.Distance(customer.worldPosition, exitPos);

                    if (dist <= moveStep)
                    {
                        FinalizeCustomerDeparture(customer);

                        if (customer.visual != null)
                        {
                            Destroy(customer.visual);
                        }

                        activeCustomers.RemoveAt(i);
                    }
                    else
                    {
                        customer.worldPosition = Vector3.MoveTowards(customer.worldPosition, exitPos, moveStep);

                        if (customer.visual != null)
                        {
                            customer.visual.transform.position = customer.worldPosition;
                        }
                    }

                    break;
                }
            }
        }
    }

    private bool TryAssignNextMachine(ActiveCustomer customer, List<MachineRuntimeInfo> machines)
    {
        if (customer == null || machines == null || machines.Count <= 0)
        {
            return false;
        }

        if (!TryPickAvailableMachine(customer, machines, out MachineRuntimeInfo targetMachine))
        {
            return false;
        }

        bool wasWaiting = customer.state == CustomerState.WaitingForMachine;

        customer.targetMachineKey = targetMachine.key;
        
        Vector3 finalPos = targetMachine.centerWorldPosition;
        if (targetMachine.data != null && targetMachine.data.runtimeDefinition != null)
        {
            finalPos += new Vector3(targetMachine.data.runtimeDefinition.CustomerUseOffset.x, targetMachine.data.runtimeDefinition.CustomerUseOffset.y, 0f);
        }
        customer.targetMachineWorldPosition = finalPos;
        
        customer.remainingUseSeconds = Random.Range(machineUseDurationMinSeconds, machineUseDurationMaxSeconds);
        customer.state = CustomerState.MovingToMachine;
        customer.currentPath = null;
        customer.leaveReason = CustomerLeaveReason.None;
        customer.waitSlotIndex = -1;
        customer.remainingWaitSeconds = 0f;
        customer.retrySearchCountdown = 0f;
        customer.currentPath = null;
        customer.currentPathIndex = 0;

        if (wasWaiting)
        {
            dailyRecoveredFromWaiting += 1;
            PushOperationFeed(
                $"?湲??댁냼 쨌 {GetMachineDisplayName(targetMachine)} ?뺣낫",
                operationFeedInfoColor
            );
        }

        customer.visitedMachineKeys.Add(targetMachine.key);
        reservedMachineKeys.Add(targetMachine.key);

        ApplyCustomerVisualForState(customer);
        return true;
    }

    private bool TryPickAvailableMachine(
        ActiveCustomer customer,
        List<MachineRuntimeInfo> machines,
        out MachineRuntimeInfo targetMachine)
    {
        targetMachine = null;

        List<MachineRuntimeInfo> preferredPrimary = new List<MachineRuntimeInfo>();
        List<MachineRuntimeInfo> preferredSecondary = new List<MachineRuntimeInfo>();
        List<MachineRuntimeInfo> fallback = new List<MachineRuntimeInfo>();

        for (int i = 0; i < machines.Count; i++)
        {
            MachineRuntimeInfo machine = machines[i];
            if (machine == null || string.IsNullOrWhiteSpace(machine.key))
            {
                continue;
            }

            if (reservedMachineKeys.Contains(machine.key))
            {
                continue;
            }

            if (machine.data != null && (machine.data.isUnderConstruction || machine.data.isBroken))
            {
                continue;
            }

            if (machine.data != null && machine.data.isBroken)
            {
                continue;
            }

            if (customer != null && customer.visitedMachineKeys.Contains(machine.key))
            {
                continue;
            }

            if (machine.preferenceGroup == customer.primaryPreference)
            {
                preferredPrimary.Add(machine);
            }
            else if (machine.preferenceGroup == customer.secondaryPreference)
            {
                preferredSecondary.Add(machine);
            }
            else
            {
                fallback.Add(machine);
            }
        }

        List<MachineRuntimeInfo> pool = null;

        if (preferredPrimary.Count > 0)
        {
            pool = preferredPrimary;
        }
        else if (preferredSecondary.Count > 0)
        {
            pool = preferredSecondary;
        }
        else if (fallback.Count > 0)
        {
            pool = fallback;
        }

        if (pool == null || pool.Count <= 0)
        {
            return false;
        }

        targetMachine = pool[Random.Range(0, pool.Count)];
        return targetMachine != null;
    }

    private void AssignPreferencesFromMachines(ActiveCustomer customer, List<MachineRuntimeInfo> machines)
    {
        if (customer == null || machines == null || machines.Count <= 0)
        {
            customer.primaryPreference = MachinePreferenceGroup.Other;
            customer.secondaryPreference = MachinePreferenceGroup.Other;
            return;
        }

        List<MachinePreferenceGroup> groups = new List<MachinePreferenceGroup>();

        for (int i = 0; i < machines.Count; i++)
        {
            MachinePreferenceGroup group = machines[i].preferenceGroup;
            if (!groups.Contains(group))
            {
                groups.Add(group);
            }
        }

        if (groups.Count <= 0)
        {
            customer.primaryPreference = MachinePreferenceGroup.Other;
            customer.secondaryPreference = MachinePreferenceGroup.Other;
            return;
        }

        customer.primaryPreference = groups[Random.Range(0, groups.Count)];

        if (groups.Count == 1)
        {
            customer.secondaryPreference = customer.primaryPreference;
            return;
        }

        List<MachinePreferenceGroup> secondaryCandidates = new List<MachinePreferenceGroup>();
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i] != customer.primaryPreference)
            {
                secondaryCandidates.Add(groups[i]);
            }
        }

        customer.secondaryPreference = secondaryCandidates[Random.Range(0, secondaryCandidates.Count)];
    }

    private void EnterWaitingState(ActiveCustomer customer)
    {
        if (customer == null)
        {
            return;
        }

        ReleaseReservationIfNeeded(customer);

        customer.state = CustomerState.WaitingForMachine;
        customer.leaveReason = CustomerLeaveReason.None;
        customer.waitSlotIndex = GetAvailableWaitingSlotIndex(customer);
        customer.remainingWaitSeconds = Mathf.Max(0.5f, maximumWaitingSeconds);
        customer.retrySearchCountdown = Mathf.Max(0.15f, waitingRetryIntervalSeconds);
        customer.experiencedWaitCount += 1;
        dailyWaitingEvents += 1;

        ApplyCustomerVisualForState(customer);
        RefreshDailyWaitingPeak();

        PushOperationFeed(
            $"?湲?諛쒖깮 쨌 ?꾩옱 {WaitingCustomerCount}紐??湲?,
            WaitingCustomerCount >= 4 ? operationFeedAlertColor : operationFeedWarnColor
        );
    }

    private int GetAvailableWaitingSlotIndex(ActiveCustomer self)
    {
        HashSet<int> usedSlots = new HashSet<int>();

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null || customer == self)
            {
                continue;
            }

            if (customer.state == CustomerState.WaitingForMachine && customer.waitSlotIndex >= 0)
            {
                usedSlots.Add(customer.waitSlotIndex);
            }
        }

        int safeMaxSlots = Mathf.Max(1, maxVisibleWaitingSlots);

        for (int slot = 0; slot < safeMaxSlots; slot++)
        {
            if (!usedSlots.Contains(slot))
            {
                return slot;
            }
        }

        return safeMaxSlots - 1;
    }

    private void BeginLeaving(ActiveCustomer customer, CustomerLeaveReason reason)
    {
        if (customer == null)
        {
            return;
        }

        ReleaseReservationIfNeeded(customer);
        customer.state = CustomerState.Leaving;
        customer.currentPath = null;
        customer.waitSlotIndex = -1;
        customer.remainingWaitSeconds = 0f;
        customer.retrySearchCountdown = 0f;
        customer.leaveReason = reason;
        ApplyCustomerVisualForState(customer);
    }

    private void FinalizeCustomerDeparture(ActiveCustomer customer)
    {
        if (customer == null)
        {
            return;
        }

        switch (customer.leaveReason)
        {
            case CustomerLeaveReason.CompletedVisit:
                dailyCompletedVisits += 1;
                break;

            case CustomerLeaveReason.WaitTimeout:
                dailyAbandonedVisits += 1;
                PushOperationFeed(
                    $"?ш린 ?댁옣 쨌 ?꾩쟻 {dailyAbandonedVisits}??,
                    operationFeedAlertColor
                );
                break;
        }
    }

    private void RefreshDailyWaitingPeak()
    {
        dailyPeakWaitingCustomers = Mathf.Max(dailyPeakWaitingCustomers, WaitingCustomerCount);
    }

    private void ApplyCustomerVisualForState(ActiveCustomer customer)
    {
        if (customer == null || customer.renderer == null)
        {
            return;
        }

        switch (customer.state)
        {
            case CustomerState.UsingMachine:
                customer.renderer.color = usingCustomerColor;
                break;

            case CustomerState.WaitingForMachine:
                customer.renderer.color = waitingCustomerColor;
                break;

            case CustomerState.Leaving:
                customer.renderer.color = leavingCustomerColor;
                break;

            default:
                customer.renderer.color = movingCustomerColor;
                break;
        }
    }

    private void UpdateCustomerVisualAnimation(ActiveCustomer customer)
    {
        if (customer == null || customer.visual == null)
        {
            return;
        }

        float scale = 1f;

        switch (customer.state)
        {
            case CustomerState.UsingMachine:
                // scale += Mathf.Sin(Time.time * Mathf.Max(0.1f, usingPulseSpeed)) * Mathf.Max(0f, usingPulseAmplitude);
                break;

            case CustomerState.WaitingForMachine:
                // scale += Mathf.Sin(Time.time * Mathf.Max(0.1f, waitingPulseSpeed)) * Mathf.Max(0f, waitingPulseAmplitude);
                break;
        }

        // LayeredCustomerAnimator를 위해 크기가 펄럭이는 것을 중단
        customer.visual.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Transform EnsureRuntimeRoot()
    {
        if (runtimeRoot != null)
        {
            return runtimeRoot;
        }

        GameObject rootObject = new GameObject("PrototypeCustomersRoot");
        Transform parent = transform.parent != null ? transform.parent : transform;
        rootObject.transform.SetParent(parent, false);
        runtimeRoot = rootObject.transform;
        return runtimeRoot;
    }

    private Transform EnsureOverlayRoot()
    {
        if (overlayRoot != null)
        {
            return overlayRoot;
        }

        GameObject rootObject = new GameObject("PrototypeMachineOverlayRoot");
        Transform parent = transform.parent != null ? transform.parent : transform;
        rootObject.transform.SetParent(parent, false);
        overlayRoot = rootObject.transform;
        return overlayRoot;
    }

    private Vector3 GetEntryWorldPosition()
    {
        float halfWidth = gridManager.Width * gridManager.CellSize * 0.5f;
        float halfHeight = gridManager.Height * gridManager.CellSize * 0.5f;

        return new Vector3(
            -halfWidth - Mathf.Max(0.8f, entryExitOffsetX),
            -halfHeight + (gridManager.Height * gridManager.CellSize * Mathf.Clamp01(entryExitHeightRatio)),
            0f
        );
    }

    private Vector3 GetExitWorldPosition()
    {
        // 손님은 들어온 입구(왼쪽)로 다시 나가야 하므로, GetEntryWorldPosition과 동일한 위치를 반환합니다.
        float halfWidth = gridManager.Width * gridManager.CellSize * 0.5f;
        float halfHeight = gridManager.Height * gridManager.CellSize * 0.5f;

        return new Vector3(
            -halfWidth - Mathf.Max(0.8f, entryExitOffsetX),
            -halfHeight + (gridManager.Height * gridManager.CellSize * Mathf.Clamp01(entryExitHeightRatio)),
            0f
        );
    }

    private Vector3 GetWaitingWorldPosition(int slotIndex)
    {
        Vector3 entry = GetEntryWorldPosition();
        int safeSlotIndex = Mathf.Max(0, slotIndex);

        return new Vector3(
            entry.x + 0.8f,
            entry.y + (safeSlotIndex * Mathf.Max(0.2f, waitingSlotSpacingY)),
            0f
        );
    }

    private List<MachineRuntimeInfo> BuildMachineRuntimeInfos(IReadOnlyList<PlacedObjectSaveData> placedObjects)
    {
        List<MachineRuntimeInfo> results = new List<MachineRuntimeInfo>();

        if (placedObjects == null || gridManager == null)
        {
            return results;
        }

        for (int i = 0; i < placedObjects.Count; i++)
        {
            PlacedObjectSaveData data = placedObjects[i];
            if (data == null)
            {
                continue;
            }

            results.Add(new MachineRuntimeInfo
            {
                key = BuildMachineKey(data),
                data = data,
                centerWorldPosition = gridManager.GetAreaCenterWorldPosition(data.anchorX, data.anchorY, data.width, data.height),
                preferenceGroup = GetPreferenceGroupForPlacedObject(data)
            });
        }

        return results;
    }

    private Dictionary<string, MachineRuntimeInfo> BuildMachineLookup(List<MachineRuntimeInfo> machines)
    {
        Dictionary<string, MachineRuntimeInfo> lookup = new Dictionary<string, MachineRuntimeInfo>();

        if (machines == null)
        {
            return lookup;
        }

        for (int i = 0; i < machines.Count; i++)
        {
            MachineRuntimeInfo machine = machines[i];
            if (machine == null || string.IsNullOrWhiteSpace(machine.key))
            {
                continue;
            }

            lookup[machine.key] = machine;
        }

        return lookup;
    }

    private bool TryRefreshCustomerTargetPosition(
        ActiveCustomer customer,
        Dictionary<string, MachineRuntimeInfo> machineLookup)
    {
        if (customer == null ||
            machineLookup == null ||
            string.IsNullOrWhiteSpace(customer.targetMachineKey))
        {
            return false;
        }

        if (!machineLookup.TryGetValue(customer.targetMachineKey, out MachineRuntimeInfo machine))
        {
            return false;
        }

        customer.targetMachineWorldPosition = machine.centerWorldPosition;
        return true;
    }

    private void ReconcileCustomersAfterLayoutChanged(List<MachineRuntimeInfo> machines)
    {
        Dictionary<string, MachineRuntimeInfo> machineLookup = BuildMachineLookup(machines);

        reservedMachineKeys.Clear();

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null)
            {
                continue;
            }

            if (customer.state == CustomerState.Leaving)
            {
                continue;
            }

            if (customer.state == CustomerState.WaitingForMachine)
            {
                customer.retrySearchCountdown = 0f;
                continue;
            }

            if (TryRefreshCustomerTargetPosition(customer, machineLookup))
            {
                if (!string.IsNullOrWhiteSpace(customer.targetMachineKey))
                {
                    reservedMachineKeys.Add(customer.targetMachineKey);
                }

                continue;
            }

            customer.targetMachineKey = null;

            if (customer.remainingMachineStops > 0)
            {
                if (!TryAssignNextMachine(customer, machines))
                {
                    EnterWaitingState(customer);
                }
            }
            else
            {
                BeginLeaving(customer, CustomerLeaveReason.LayoutChanged);
            }
        }

        RebuildMachineOverlays(machines);
    }

    private void ConvertAllCustomersToLeaving()
    {
        reservedMachineKeys.Clear();

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null)
            {
                continue;
            }

            customer.targetMachineKey = null;
            BeginLeaving(customer, CustomerLeaveReason.NoMachinesAvailable);
        }
    }

    private void RebuildMachineOverlays(List<MachineRuntimeInfo> machines)
    {
        HideAllMachineOverlays();
        return;

        HashSet<string> validKeys = new HashSet<string>();
        Dictionary<string, MachineOverlayState> stateByKey = new Dictionary<string, MachineOverlayState>();

        if (machines != null)
        {
            for (int i = 0; i < machines.Count; i++)
            {
                MachineRuntimeInfo machine = machines[i];
                if (machine == null || string.IsNullOrWhiteSpace(machine.key))
                {
                    continue;
                }

                validKeys.Add(machine.key);
                stateByKey[machine.key] = MachineOverlayState.None;
            }
        }

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null || string.IsNullOrWhiteSpace(customer.targetMachineKey))
            {
                continue;
            }

            if (!stateByKey.ContainsKey(customer.targetMachineKey))
            {
                continue;
            }

            if (customer.state == CustomerState.UsingMachine)
            {
                stateByKey[customer.targetMachineKey] = MachineOverlayState.InUse;
            }
            else if (customer.state == CustomerState.MovingToMachine &&
                     stateByKey[customer.targetMachineKey] != MachineOverlayState.InUse)
            {
                stateByKey[customer.targetMachineKey] = MachineOverlayState.Reserved;
            }
        }

        RemoveStaleOverlays(validKeys);

        if (machines == null)
        {
            return;
        }

        for (int i = 0; i < machines.Count; i++)
        {
            MachineRuntimeInfo machine = machines[i];
            if (machine == null || machine.data == null || string.IsNullOrWhiteSpace(machine.key))
            {
                continue;
            }

            MachineOverlayState state = stateByKey.TryGetValue(machine.key, out MachineOverlayState foundState)
                ? foundState
                : MachineOverlayState.None;

            if (state == MachineOverlayState.None)
            {
                HideOverlay(machine.key);
                continue;
            }

            SpriteRenderer overlay = GetOrCreateOverlay(machine.key);
            overlay.gameObject.SetActive(true);
            overlay.transform.position = machine.centerWorldPosition;

            float width = Mathf.Max(1f, machine.data.width * gridManager.CellSize);
            float height = Mathf.Max(1f, machine.data.height * gridManager.CellSize);
            overlay.size = new Vector2(width, height);
            overlay.color = state == MachineOverlayState.InUse ? inUseOverlayColor : reservedOverlayColor;
        }
    }

    private SpriteRenderer GetOrCreateOverlay(string key)
    {
        if (machineOverlayMap.TryGetValue(key, out SpriteRenderer existing) && existing != null)
        {
            return existing;
        }

        GameObject overlayObject = new GameObject($"MachineOverlay_{key}");
        overlayObject.transform.SetParent(EnsureOverlayRoot(), false);

        SpriteRenderer renderer = overlayObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.sortingOrder = overlaySortingOrder;

        machineOverlayMap[key] = renderer;
        return renderer;
    }

    private void HideOverlay(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (machineOverlayMap.TryGetValue(key, out SpriteRenderer renderer) && renderer != null)
        {
            renderer.gameObject.SetActive(false);
        }
    }

    private void HideAllMachineOverlays()
    {
        foreach (KeyValuePair<string, SpriteRenderer> pair in machineOverlayMap)
        {
            if (pair.Value != null)
            {
                pair.Value.gameObject.SetActive(false);
            }
        }
    }

    private void RemoveStaleOverlays(HashSet<string> validKeys)
    {
        List<string> staleKeys = new List<string>();

        foreach (KeyValuePair<string, SpriteRenderer> pair in machineOverlayMap)
        {
            if (!validKeys.Contains(pair.Key))
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }

                staleKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            machineOverlayMap.Remove(staleKeys[i]);
        }
    }

    private void ReleaseReservationIfNeeded(ActiveCustomer customer)
    {
        if (customer == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(customer.targetMachineKey))
        {
            reservedMachineKeys.Remove(customer.targetMachineKey);
            customer.targetMachineKey = null;
        }
    }

    private void ClearAllCustomersAndOverlays()
    {
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer != null && customer.visual != null)
            {
                Destroy(customer.visual);
            }
        }

        activeCustomers.Clear();
        reservedMachineKeys.Clear();

        foreach (KeyValuePair<string, SpriteRenderer> pair in machineOverlayMap)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        machineOverlayMap.Clear();
    }

    private float GetNextSpawnInterval()
    {
        float min = Mathf.Max(0.2f, spawnIntervalMinSeconds);
        float max = Mathf.Max(min, spawnIntervalMaxSeconds);
        return Random.Range(min, max);
    }

    public static string BuildMachineKey(PlacedObjectSaveData data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        string equipmentId = string.IsNullOrWhiteSpace(data.equipmentId) ? "unknown" : data.equipmentId;

        return
            equipmentId + "|" +
            data.anchorX + "|" +
            data.anchorY + "|" +
            data.width + "|" +
            data.height;
    }

    public bool IsMachineInUse(string machineKey)
    {
        if (string.IsNullOrEmpty(machineKey)) return false;
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            var customer = activeCustomers[i];
            if (customer != null && customer.state == CustomerState.UsingMachine && customer.targetMachineKey == machineKey)
            {
                return true;
            }
        }
        return false;
    }

    private MachinePreferenceGroup GetPreferenceGroupForPlacedObject(PlacedObjectSaveData data)
    {
        if (data == null)
        {
            return MachinePreferenceGroup.Other;
        }

        string source = ((data.equipmentId ?? string.Empty) + " " + (data.displayName ?? string.Empty)).ToLowerInvariant();

        if (ContainsAny(source, "treadmill", "bike", "cycle", "ellipt", "cardio", "run", "rower"))
        {
            return MachinePreferenceGroup.Cardio;
        }

        if (ContainsAny(source, "bench", "press", "chest", "push", "shoulder", "dip"))
        {
            return MachinePreferenceGroup.Push;
        }

        if (ContainsAny(source, "row", "pull", "lat", "chin", "bicep", "curl"))
        {
            return MachinePreferenceGroup.Pull;
        }

        if (ContainsAny(source, "squat", "leg", "calf", "lunge", "hack", "glute"))
        {
            return MachinePreferenceGroup.Leg;
        }

        if (ContainsAny(source, "dumbbell", "barbell", "plate", "kettlebell", "free"))
        {
            return MachinePreferenceGroup.FreeWeight;
        }

        if (ContainsAny(source, "stretch", "recovery", "massage", "foam"))
        {
            return MachinePreferenceGroup.Recovery;
        }

        if (ContainsAny(source, "locker", "counter", "desk", "facility", "water", "vending"))
        {
            return MachinePreferenceGroup.Facility;
        }

        return MachinePreferenceGroup.Other;
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(source) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (source.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateOperationFeedLifetimes(float deltaTime)
    {
        if (deltaTime <= 0f || operationFeedEntries.Count <= 0)
        {
            return;
        }

        for (int i = operationFeedEntries.Count - 1; i >= 0; i--)
        {
            OperationFeedEntry entry = operationFeedEntries[i];
            if (entry == null)
            {
                operationFeedEntries.RemoveAt(i);
                continue;
            }

            entry.remainingSeconds -= deltaTime;
            if (entry.remainingSeconds <= 0f)
            {
                operationFeedEntries.RemoveAt(i);
            }
        }
    }

    private void PushOperationFeed(string message, Color color)
    {
        if (!showOperationFeed || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (operationFeedEntries.Count > 0 && operationFeedEntries[0] != null && operationFeedEntries[0].message == message)
        {
            operationFeedEntries[0].remainingSeconds = Mathf.Max(1f, operationFeedEntryLifetimeSeconds);
            operationFeedEntries[0].color = color;
            return;
        }

        operationFeedEntries.Insert(0, new OperationFeedEntry
        {
            message = message,
            color = color,
            remainingSeconds = Mathf.Max(1f, operationFeedEntryLifetimeSeconds)
        });

        int safeMax = Mathf.Max(1, maxOperationFeedEntries);
        while (operationFeedEntries.Count > safeMax)
        {
            operationFeedEntries.RemoveAt(operationFeedEntries.Count - 1);
        }
    }

    private void UpdateOperationRuntimeSummary(List<MachineRuntimeInfo> machines, bool isBuildMode)
    {
        int machineCount = machines != null ? machines.Count : 0;
        int waiting = WaitingCustomerCount;
        int usingCount = UsingCustomerCount;
        int engaged = 0;

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null)
            {
                continue;
            }

            if (customer.state == CustomerState.MovingToMachine || customer.state == CustomerState.UsingMachine)
            {
                engaged++;
            }
        }

        currentFlowSummary = isBuildMode
            ? "?ㅼ튂 紐⑤뱶 ?뺤?"
            : waiting > 0
                ? $"?湲?{waiting}紐?쨌 ?ъ슜 {usingCount}紐?
                : usingCount > 0
                    ? $"?ъ슜 {usingCount}紐?쨌 ?먰솢"
                    : engaged > 0
                        ? $"?대룞 {engaged}紐?쨌 ?湲??놁쓬"
                        : "?먮떂 ?놁쓬";

        currentBottleneckSummary = BuildBottleneckSummary(machines, waiting, out string topMachineSummary);
        currentTopMachineSummary = topMachineSummary;

        if (isBuildMode)
        {
            currentGuideSummary = machineCount > 0
                ? "諛곗튂 議곗젙 ???뚮젅???꾪솚"
                : "湲곌뎄 ?ㅼ튂 ???뚮젅???쒖옉";
            return;
        }

        if (machineCount <= 0)
        {
            currentGuideSummary = "湲곌뎄 ?ㅼ튂 ???먮떂 ?먮쫫 ?뺤씤";
            return;
        }

        if (waiting >= 4)
        {
            currentGuideSummary = "蹂묐ぉ ?ы븿 쨌 ?숈씪 怨꾩뿴 蹂닿컯 ?먮뒗 ?댁궗 寃??;
        }
        else if (waiting >= 2)
        {
            currentGuideSummary = "?湲?諛쒖깮 쨌 ?멸린 湲곌뎄 1? ??蹂닿컯";
        }
        else if (engaged >= Mathf.Max(1, machineCount))
        {
            currentGuideSummary = "媛?숇쪧 ?믪쓬 쨌 ?쇱옟 吏곸쟾 ?곹깭";
        }
        else if (activeCustomers.Count <= 0)
        {
            currentGuideSummary = "?먮떂 ?곸쓬 쨌 ?뚯썝 利앷? 異붿씠 ?뺤씤";
        }
        else
        {
            currentGuideSummary = "?댁쁺 ?먰솢 쨌 ?꾩옱 諛곗튂 ?좎? 媛??;
        }
    }

    private string BuildBottleneckSummary(List<MachineRuntimeInfo> machines, int waitingCount, out string topMachineSummary)
    {
        topMachineSummary = "二쇱슂 湲곌뎄 ?놁쓬";

        if (machines == null || machines.Count <= 0)
        {
            return waitingCount > 0 ? "湲곌뎄 ?놁쓬 ?湲? : "蹂묐ぉ ?놁쓬";
        }

        Dictionary<string, int> engagedByMachine = new Dictionary<string, int>();
        Dictionary<string, string> labelByMachine = new Dictionary<string, string>();

        for (int i = 0; i < machines.Count; i++)
        {
            MachineRuntimeInfo machine = machines[i];
            if (machine == null || string.IsNullOrWhiteSpace(machine.key))
            {
                continue;
            }

            engagedByMachine[machine.key] = 0;
            labelByMachine[machine.key] = GetMachineDisplayName(machine);
        }

        for (int i = 0; i < activeCustomers.Count; i++)
        {
            ActiveCustomer customer = activeCustomers[i];
            if (customer == null || string.IsNullOrWhiteSpace(customer.targetMachineKey))
            {
                continue;
            }

            if (customer.state != CustomerState.MovingToMachine && customer.state != CustomerState.UsingMachine)
            {
                continue;
            }

            if (!engagedByMachine.ContainsKey(customer.targetMachineKey))
            {
                engagedByMachine[customer.targetMachineKey] = 0;
                labelByMachine[customer.targetMachineKey] = customer.targetMachineKey;
            }

            engagedByMachine[customer.targetMachineKey] += 1;
        }

        string topKey = null;
        int topCount = 0;

        foreach (KeyValuePair<string, int> pair in engagedByMachine)
        {
            if (pair.Value > topCount)
            {
                topKey = pair.Key;
                topCount = pair.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(topKey))
        {
            string label = labelByMachine.TryGetValue(topKey, out string foundLabel) ? foundLabel : topKey;
            topMachineSummary = $"{label} 횞{topCount}";
        }

        if (waitingCount >= 4)
        {
            return topCount >= 2 ? $"{topMachineSummary} 吏묒쨷" : "湲곌뎄 ??遺議??湲??ы솕";
        }

        if (waitingCount >= 2)
        {
            return topCount >= 2 ? $"{topMachineSummary} 蹂묐ぉ" : "媛踰쇱슫 蹂묐ぉ 諛쒖깮";
        }

        if (topCount >= 2)
        {
            return $"{topMachineSummary} 吏묒쨷";
        }

        return "蹂묐ぉ ?놁쓬";
    }

    private string GetMachineDisplayName(MachineRuntimeInfo machine)
    {
        if (machine == null)
        {
            return "?????녿뒗 湲곌뎄";
        }

        if (machine.data != null && !string.IsNullOrWhiteSpace(machine.data.displayName))
        {
            return machine.data.displayName;
        }

        if (machine.data != null && !string.IsNullOrWhiteSpace(machine.data.equipmentId))
        {
            return machine.data.equipmentId;
        }

        return string.IsNullOrWhiteSpace(machine.key) ? "?????녿뒗 湲곌뎄" : machine.key;
    }

    private void OnGUI()
{
        // Runtime HUD owns operational overlays now.
        return;

        if (showOperationFeed && operationFeedEntries != null && operationFeedEntries.Count > 0)
        {
            float feedY = 120f;
            for (int i = 0; i < operationFeedEntries.Count; i++)
            {
                var entry = operationFeedEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.message)) continue;
                
                GUI.color = entry.color;
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = Screen.height > Screen.width ? 14 : 16;
                style.fontStyle = FontStyle.Bold;
                GUI.Label(new Rect(20f, feedY, Screen.width - 40f, 30f), entry.message, style);
                feedY += 25f;
            }
            GUI.color = Color.white;
        }


        if (!showDebugOnGUI)
        {
            return;
        }

        EnsureDebugStyles();

        CustomerExperienceSnapshot snapshot = GetDailyExperienceSnapshot();

        Rect boxRect = new Rect(Screen.width - 236f, 86f, 224f, 198f);
        GUI.Box(boxRect, GUIContent.none, debugBoxStyle);
        GUI.Label(
            new Rect(boxRect.x + 10f, boxRect.y + 8f, boxRect.width - 20f, boxRect.height - 16f),
            $"?먮떂 猷⑦봽\n" +
            $"?쒖꽦 ?먮떂: {activeCustomers.Count}\n" +
            $"?湲??먮떂: {WaitingCustomerCount}\n" +
            $"?ъ슜 ?먮떂: {UsingCustomerCount}\n" +
            $"?덉빟 湲곌뎄: {reservedMachineKeys.Count}\n" +
            $"?ㅻ쾭?덉씠: {machineOverlayMap.Count}\n" +
            $"?湲??대깽?? {snapshot.waitingEvents}\n" +
            $"?꾩＜/?ш린: {snapshot.completedVisits}/{snapshot.abandonedVisits}\n" +
            $"?됯퇏 ?湲? {snapshot.averageWaitSeconds:0.0}s\n" +
            $"理쒕? ?湲곗뿴: {snapshot.peakWaitingCustomers}\n" +
            $"?ㅽ듃?덉뒪 ?뚯뒪?? {(enableCrowdingStressTest ? "ON" : "OFF")}\n" +
            $"?띾룄: {(timeManager != null ? timeManager.CurrentSpeedLabel : "x1")}",
            debugLabelStyle
        );
    }


    private void EnsureDebugStyles()
    {
        if (debugBoxStyle == null)
        {
            debugBoxStyle = new GUIStyle(GUI.skin.box);
        }

        if (debugLabelStyle == null)
        {
            debugLabelStyle = new GUIStyle(GUI.skin.label);
            debugLabelStyle.wordWrap = true;
            debugLabelStyle.normal.textColor = Color.white;
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null)
        {
            return cachedWhiteSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        cachedWhiteSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return cachedWhiteSprite;
    }

    private Vector3 GetPathfindingImmediateTarget(ActiveCustomer customer, Vector3 finalTarget, bool allowTargetOccupied)
    {
        Vector3 immediateTarget = finalTarget;
        
        if (customer.currentPath == null)
        {
            int startX, startY;
            bool validStart = gridManager.TryGetCellIndexFromWorldPosition(customer.worldPosition, out startX, out startY);
            if (!validStart)
            {
                // Clamp to nearest cell
                Vector2 originOffset = new Vector2(
                    -(gridManager.Width * gridManager.CellSize) / 2f + gridManager.CellSize / 2f,
                    -(gridManager.Height * gridManager.CellSize) / 2f + gridManager.CellSize / 2f
                );
                startX = Mathf.Clamp(Mathf.RoundToInt((customer.worldPosition.x - originOffset.x) / gridManager.CellSize), 0, gridManager.Width - 1);
                startY = Mathf.Clamp(Mathf.RoundToInt((customer.worldPosition.y - originOffset.y) / gridManager.CellSize), 0, gridManager.Height - 1);
            }

            if (gridManager.TryGetCellIndexFromWorldPosition(finalTarget, out int targetX, out int targetY))
            {
                var cellPath = AStarPathfinder.FindPath(gridManager, new Vector2Int(startX, startY), new Vector2Int(targetX, targetY), allowTargetOccupied);
                if (cellPath != null && cellPath.Count > 0)
                {
                    customer.currentPath = new List<Vector3>();
                    foreach (var cellPos in cellPath)
                    {
                        customer.currentPath.Add(gridManager.GetAreaCenterWorldPosition(cellPos.x, cellPos.y, 1, 1));
                    }
                    if (customer.currentPath.Count > 0)
                    {
                        customer.currentPath[customer.currentPath.Count - 1] = finalTarget;
                    }
                    customer.currentPathIndex = 0;
                }
            }
            
            if (customer.currentPath == null)
            {
                customer.currentPath = new List<Vector3> { finalTarget };
                customer.currentPathIndex = 0;
            }
        }

        if (customer.currentPathIndex < customer.currentPath.Count)
        {
            immediateTarget = customer.currentPath[customer.currentPathIndex];
        }
        else
        {
            immediateTarget = finalTarget;
        }

        return immediateTarget;
    }
    public bool IsMachineInUse(string machineKey)
    {
        for (int i = 0; i < activeCustomers.Count; i++)
        {
            var customer = activeCustomers[i];
            if (customer.state == CustomerState.UsingMachine && customer.targetMachineKey == machineKey)
            {
                return true;
            }
        }
        return false;
    }
}




