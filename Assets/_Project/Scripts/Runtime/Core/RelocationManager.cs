using System;
using System.Collections.Generic;
using UnityEngine;

public class RelocationManager : MonoBehaviour
{
    public struct RelocationQuote
    {
        public bool isValid;
        public string failReason;

        public GymLocationType currentLocationType;
        public GymLocationType targetLocationType;
        public GymSiteTier currentTier;
        public GymSiteTier targetTier;

        public string currentSiteLabel;
        public string targetSiteLabel;
        public string targetLocationSummary;

        public int currentGridWidth;
        public int currentGridHeight;
        public int targetGridWidth;
        public int targetGridHeight;

        public int placedEquipmentCount;
        public int siteBaseContractFee;
        public int locationContractSurcharge;
        public int contractFee;
        public int transportFeePerEquipment;
        public int transportFeeTotal;
        public int totalCost;
        public int currentCash;
        public int shortageAmount;
    }

    public struct ActiveRelocationHudState
    {
        public string title;
        public string status;
        public string actionLabel;
        public bool canSkip;
    }

    // ─── 이사 타이머 내부 상태 ───────────────────────────────────────
    private struct PendingRelocationState
    {
        public bool isActive;
        public long endTimeTicks;           // DateTime.UtcNow.Ticks 기준
        public GymSiteTier targetTier;
        public GymLocationType targetLocationType;
        public string targetSiteLabel;
        public List<PlacedObjectSaveData> placedObjectSnapshot;
        public GymSiteTier previousTier;
        public GymLocationType previousLocationType;
        public int paidCost;
    }

    [Header("References")]
    [SerializeField] private GymSiteManager gymSiteManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private WalletManager walletManager;
    [SerializeField] private SaveManager saveManager;

    [Header("Prototype Relocation Cost")]
    [SerializeField] private int contractFeeTo16x16 = 15000;
    [SerializeField] private int contractFeeTo32x32 = 60000;
    [SerializeField] private int contractFeeTo64x64 = 180000;

    [SerializeField] private int transportFeePerEquipmentTo16x16 = 1500;
    [SerializeField] private int transportFeePerEquipmentTo32x32 = 3500;
    [SerializeField] private int transportFeePerEquipmentTo64x64 = 8000;

    [Header("Prototype Relocation Timer (현실 시간 기준)")]
    [SerializeField] private int relocationMinutesTo16x16 = 5;
    [SerializeField] private int relocationMinutesTo32x32 = 15;
    [SerializeField] private int relocationMinutesTo64x64 = 30;

    [Header("Prototype Target Selection")]
    [SerializeField] private GymLocationType selectedTargetLocation = GymLocationType.Neighborhood;

    private bool hasSelectedTargetLocation = false;
    private PendingRelocationState pendingRelocation;

    // ─── 이사 중 상태 공개 프로퍼티 ────────────────────────────────────
    public bool IsUnderRelocation => pendingRelocation.isActive;

    public float RelocationRemainingSeconds
    {
        get
        {
            if (!pendingRelocation.isActive) return 0f;
            double remaining = (new DateTime(pendingRelocation.endTimeTicks, DateTimeKind.Utc) - DateTime.UtcNow).TotalSeconds;
            return Mathf.Max(0f, (float)remaining);
        }
    }

    public int RelocationSkipStarCoinCost
    {
        get
        {
            float remainingMinutes = RelocationRemainingSeconds / 60f;
            return Mathf.Max(1, Mathf.CeilToInt(remainingMinutes / 5f));
        }
    }

    public bool TryGetActiveRelocationHudState(out ActiveRelocationHudState state)
    {
        state = default;

        if (!pendingRelocation.isActive)
        {
            return false;
        }

        CacheReferences();

        float remaining = RelocationRemainingSeconds;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        int skipCost = RelocationSkipStarCoinCost;
        bool canSkip = walletManager == null || walletManager.CurrentStarCoin >= skipCost;

        state.title = pendingRelocation.targetSiteLabel;
        state.status = $"이사 진행 중  |  남은 시간 {minutes:D2}:{seconds:D2}";
        state.actionLabel = $"즉시 완료 ({skipCost} SC)";
        state.canSkip = canSkip;
        return true;
    }

    // ─── Unity 라이프사이클 ─────────────────────────────────────────
    private void Update()
    {
        if (!pendingRelocation.isActive) return;

        if (DateTime.UtcNow.Ticks >= pendingRelocation.endTimeTicks)
        {
            CompleteRelocation();
        }
    }

    // ─── 공개 메서드 ────────────────────────────────────────────────
    public string GetCurrentSiteLabel()
    {
        CacheReferences();
        if (gymSiteManager == null) return "부지 정보 없음";
        return gymSiteManager.GetCurrentSiteLabel();
    }

    public string GetSelectedTargetLocationLabel()
    {
        CacheReferences();
        EnsureTargetLocationSelection();
        if (gymSiteManager == null) return "목표 부지 없음";
        return GymSiteManager.GetLocationDisplayName(selectedTargetLocation);
    }

    public string GetSelectedTargetLocationSummary()
    {
        CacheReferences();
        EnsureTargetLocationSelection();
        if (gymSiteManager == null) return "목표 입지 정보 없음";
        return gymSiteManager.GetLocationSummary(selectedTargetLocation);
    }

    public bool StepTargetLocationSelection(int direction)
    {
        CacheReferences();
        if (gymSiteManager == null) return false;

        EnsureTargetLocationSelection();

        GymLocationType nextLocation;
        bool changed = gymSiteManager.TryGetAdjacentSelectableLocation(selectedTargetLocation, direction, out nextLocation);

        selectedTargetLocation = nextLocation;
        hasSelectedTargetLocation = true;

        if (changed)
        {
            Debug.Log($"[RelocationManager] 이사 입지 선택 변경: {GymSiteManager.GetLocationDisplayName(selectedTargetLocation)}");
        }

        return changed;
    }

    public bool TryGetNextRelocationQuote(out RelocationQuote quote)
    {
        quote = default;
        CacheReferences();

        if (gymSiteManager == null)
        {
            quote.failReason = "GymSiteManager를 찾을 수 없습니다.";
            return false;
        }

        gymSiteManager.InitializeSiteState();
        EnsureTargetLocationSelection();

        GymSiteTier nextTier;
        if (!gymSiteManager.TryGetNextTier(out nextTier))
        {
            quote.failReason = "더 이상 확장할 수 있는 부지가 없습니다.";
            return false;
        }

        GymLocationPrototypeRules targetLocationRules = gymSiteManager.GetLocationRules(selectedTargetLocation);

        int placedEquipmentCount = placementManager != null ? placementManager.PlacedObjectCount : 0;
        int siteBaseContractFee = GetContractFeeForTargetTier(nextTier);
        int locationContractSurcharge = Mathf.Max(0, targetLocationRules.relocationContractSurcharge);
        int contractFee = siteBaseContractFee + locationContractSurcharge;
        int transportFeePerEquipment = GetTransportFeePerEquipmentForTargetTier(nextTier);
        int transportFeeTotal = transportFeePerEquipment * placedEquipmentCount;
        int totalCost = contractFee + transportFeeTotal;
        int currentCash = walletManager != null ? walletManager.CurrentCash : 0;

        quote.isValid = true;
        quote.failReason = string.Empty;

        quote.currentLocationType = gymSiteManager.CurrentLocationType;
        quote.targetLocationType = selectedTargetLocation;
        quote.currentTier = gymSiteManager.CurrentSiteTier;
        quote.targetTier = nextTier;

        quote.currentSiteLabel = gymSiteManager.GetCurrentSiteLabel();
        quote.targetSiteLabel = gymSiteManager.GetSiteLabel(selectedTargetLocation, nextTier);
        quote.targetLocationSummary = gymSiteManager.GetLocationSummary(selectedTargetLocation);

        quote.currentGridWidth = gymSiteManager.CurrentGridWidth;
        quote.currentGridHeight = gymSiteManager.CurrentGridHeight;
        quote.targetGridWidth = GymSiteManager.GetGridWidthForTier(nextTier);
        quote.targetGridHeight = GymSiteManager.GetGridHeightForTier(nextTier);

        quote.placedEquipmentCount = placedEquipmentCount;
        quote.siteBaseContractFee = siteBaseContractFee;
        quote.locationContractSurcharge = locationContractSurcharge;
        quote.contractFee = contractFee;
        quote.transportFeePerEquipment = transportFeePerEquipment;
        quote.transportFeeTotal = transportFeeTotal;
        quote.totalCost = totalCost;
        quote.currentCash = currentCash;
        quote.shortageAmount = Mathf.Max(0, totalCost - currentCash);

        return true;
    }

    /// <summary>
    /// 이사를 시작합니다. 비용을 차감하고 타이머를 시작합니다.
    /// 이사 완료는 Update()에서 자동으로 처리됩니다.
    /// </summary>
    public bool ExecuteNextRelocation()
    {
        CacheReferences();

        if (pendingRelocation.isActive)
        {
            Debug.LogWarning("[RelocationManager] 이미 이사가 진행 중입니다.");
            return false;
        }

        RelocationQuote quote;
        if (!TryGetNextRelocationQuote(out quote))
        {
            Debug.LogWarning($"[RelocationManager] 이사 실패: {quote.failReason}");
            return false;
        }

        if (gymSiteManager == null || gridManager == null || placementManager == null || walletManager == null)
        {
            Debug.LogError("[RelocationManager] 필수 참조가 누락되어 이사를 진행할 수 없습니다.");
            return false;
        }

        if (!walletManager.TrySpend(quote.totalCost, $"{quote.targetSiteLabel} 이사 착수금"))
        {
            Debug.LogWarning("[RelocationManager] 잔고가 부족하여 이사를 진행할 수 없습니다.");
            return false;
        }

        // 기구 스냅샷 보관 (이사 완료 시 재배치)
        List<PlacedObjectSaveData> snapshot = placementManager.GetPlacedObjectSaveDataList();
        placementManager.ClearAllPlacedObjects();

        // 타이머 세팅 (현실 시간 기준 – 게임 속도와 무관)
        int relocationMinutes = GetRelocationMinutesForTargetTier(quote.targetTier);
        long endTimeTicks = relocationMinutes > 0
            ? DateTime.UtcNow.AddMinutes(relocationMinutes).Ticks
            : DateTime.UtcNow.Ticks; // 0분이면 즉시

        pendingRelocation = new PendingRelocationState
        {
            isActive = true,
            endTimeTicks = endTimeTicks,
            targetTier = quote.targetTier,
            targetLocationType = quote.targetLocationType,
            targetSiteLabel = quote.targetSiteLabel,
            placedObjectSnapshot = snapshot,
            previousTier = quote.currentTier,
            previousLocationType = quote.currentLocationType,
            paidCost = quote.totalCost,
        };

        selectedTargetLocation = quote.targetLocationType;
        hasSelectedTargetLocation = true;

        Debug.Log(
            $"[RelocationManager] 이사 시작 / {quote.currentSiteLabel} → {quote.targetSiteLabel} / " +
            $"예상 완료까지 {relocationMinutes}분 / 비용: {quote.totalCost:N0}"
        );

        return true;
    }

    /// <summary>
    /// 스타코인을 소모하여 이사 타이머를 즉시 완료합니다.
    /// </summary>
    public bool TrySkipRelocationWithStarCoin()
    {
        if (!pendingRelocation.isActive)
        {
            Debug.LogWarning("[RelocationManager] 진행 중인 이사가 없습니다.");
            return false;
        }

        CacheReferences();
        if (walletManager == null) return false;

        int cost = RelocationSkipStarCoinCost;
        if (!walletManager.TrySpendStarCoin(cost, "이사 즉시 완료"))
        {
            Debug.LogWarning($"[RelocationManager] 스타코인 부족. 필요: {cost}");
            return false;
        }

        // 타이머를 과거로 당겨 즉시 완료 트리거
        pendingRelocation.endTimeTicks = DateTime.UtcNow.Ticks - 1;
        CompleteRelocation();
        return true;
    }

    // ─── 이사 완료 처리 ─────────────────────────────────────────────
    private void CompleteRelocation()
    {
        if (!pendingRelocation.isActive) return;

        CacheReferences();

        GymSiteTier previousTier = pendingRelocation.previousTier;
        GymLocationType previousLocation = pendingRelocation.previousLocationType;

        gymSiteManager.PromoteToTier(pendingRelocation.targetTier, $"이사 완료: {pendingRelocation.targetSiteLabel}");
        gymSiteManager.SetCurrentLocation(pendingRelocation.targetLocationType, $"이사 완료: {pendingRelocation.targetSiteLabel}");
        gymSiteManager.ApplyCurrentSiteToGridManager(gridManager);

        bool rebuildSucceeded = gridManager.RebuildGrid($"이사 완료: {pendingRelocation.targetSiteLabel}");

        if (!rebuildSucceeded)
        {
            Debug.LogError("[RelocationManager] 그리드 재생성 실패, 이사를 롤백합니다.");

            walletManager.AddCash(pendingRelocation.paidCost, "이사 실패 환불");
            gymSiteManager.PromoteToTier(previousTier, "이사 실패 롤백");
            gymSiteManager.SetCurrentLocation(previousLocation, "이사 실패 롤백");
            gymSiteManager.ApplyCurrentSiteToGridManager(gridManager);
            gridManager.RebuildGrid("이사 실패 롤백");
            placementManager.LoadPlacedObjects(pendingRelocation.placedObjectSnapshot);

            pendingRelocation = default;
            return;
        }

        placementManager.LoadPlacedObjects(pendingRelocation.placedObjectSnapshot);

        string completedLabel = pendingRelocation.targetSiteLabel;
        pendingRelocation = default; // 상태 초기화

        if (saveManager != null)
        {
            saveManager.SaveAutoSave($"이사 완료: {completedLabel}");
        }

        Debug.Log($"[RelocationManager] 이사 완료! → {completedLabel}");
    }

    // ─── OnGUI: 이사 진행 중 팝업 ────────────────────────────────────
    private GUIStyle relocationBoxStyle;
    private GUIStyle relocationLabelStyle;
    private GUIStyle relocationButtonStyle;

    private void OnGUI()
    {
        // The runtime HUD now renders relocation progress and skip affordances.
        return;
    }

    private void EnsureRelocationStyles()
    {
        if (relocationBoxStyle != null) return;

        relocationBoxStyle = new GUIStyle(GUI.skin.box);
        relocationBoxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.15f, 0.93f));
        relocationBoxStyle.border = new RectOffset(4, 4, 4, 4);

        relocationLabelStyle = new GUIStyle(GUI.skin.label);
        relocationLabelStyle.fontSize = 14;
        relocationLabelStyle.normal.textColor = new Color(1f, 0.92f, 0.6f, 1f);
        relocationLabelStyle.wordWrap = true;

        relocationButtonStyle = new GUIStyle(GUI.skin.button);
        relocationButtonStyle.fontSize = 13;
        relocationButtonStyle.normal.textColor = new Color(0.3f, 1f, 0.85f, 1f);
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    // ─── 내부 헬퍼 ─────────────────────────────────────────────────
    private void EnsureTargetLocationSelection()
    {
        if (gymSiteManager == null) return;

        gymSiteManager.InitializeSiteState();

        if (!hasSelectedTargetLocation)
        {
            selectedTargetLocation = gymSiteManager.CurrentLocationType;
            hasSelectedTargetLocation = true;
            return;
        }

        selectedTargetLocation = gymSiteManager.SanitizeLocationForPrototype(selectedTargetLocation);
    }

    private int GetContractFeeForTargetTier(GymSiteTier tier)
    {
        switch (tier)
        {
            case GymSiteTier.Expansion16x16: return Mathf.Max(0, contractFeeTo16x16);
            case GymSiteTier.FullOpen32x32:  return Mathf.Max(0, contractFeeTo32x32);
            case GymSiteTier.Mega64x64:      return Mathf.Max(0, contractFeeTo64x64);
            default: return 0;
        }
    }

    private int GetTransportFeePerEquipmentForTargetTier(GymSiteTier tier)
    {
        switch (tier)
        {
            case GymSiteTier.Expansion16x16: return Mathf.Max(0, transportFeePerEquipmentTo16x16);
            case GymSiteTier.FullOpen32x32:  return Mathf.Max(0, transportFeePerEquipmentTo32x32);
            case GymSiteTier.Mega64x64:      return Mathf.Max(0, transportFeePerEquipmentTo64x64);
            default: return 0;
        }
    }

    private int GetRelocationMinutesForTargetTier(GymSiteTier tier)
    {
        switch (tier)
        {
            case GymSiteTier.Expansion16x16: return relocationMinutesTo16x16;
            case GymSiteTier.FullOpen32x32:  return relocationMinutesTo32x32;
            case GymSiteTier.Mega64x64:      return relocationMinutesTo64x64;
            default: return 0;
        }
    }

    private void CacheReferences()
    {
        if (gymSiteManager == null)   gymSiteManager   = FindFirstObjectByType<GymSiteManager>();
        if (gridManager == null)       gridManager       = FindFirstObjectByType<GridManager>();
        if (placementManager == null)  placementManager  = FindFirstObjectByType<PlacementManager>();
        if (walletManager == null)     walletManager     = FindFirstObjectByType<WalletManager>();
        if (saveManager == null)       saveManager       = FindFirstObjectByType<SaveManager>();
    }
}
