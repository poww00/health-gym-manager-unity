using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const string AutoSaveFileName = "autosave.json";
    private const string ManualSaveFilePattern = "manual_slot_{0}.json";
    private const string DefaultSettlementText = "월말 결산 없음";

    public const int ManualSlotMin = 1;
    public const int ManualSlotMax = 2;

    [Header("References")]
    [SerializeField] private WalletManager walletManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private StaffManager staffManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private MonthlySettlementManager monthlySettlementManager;
    [SerializeField] private GymSiteManager gymSiteManager;

    [Header("Auto Save")]
    [SerializeField] private bool autoSaveOnPlacement = true;
    [SerializeField] private bool autoSaveOnSettlement = true;
    [SerializeField] private bool autoSaveOnPauseOrFocusLost = true;

    [Header("Debug")]
    [SerializeField] private bool debugAutoLoadAutoSaveWhenOpeningGameSceneDirectly = false;
    [SerializeField] private bool prettyPrintJson = true;

    private bool isInitialized = false;
    private bool isSubscribed = false;
    private bool bootstrapPrepared = false;

    private GameEntryRequest.EntryMode bootEntryMode = GameEntryRequest.EntryMode.None;
    private GameSaveData pendingBootSaveData;
    private string pendingBootLoadReason = string.Empty;
    private bool hasPendingBootSaveData = false;

    public static bool HasAutoSaveFile()
    {
        return File.Exists(GetAutoSavePath());
    }

    public static bool HasManualSaveFile(int slot)
    {
        if (!IsValidManualSlot(slot))
        {
            return false;
        }

        return File.Exists(GetManualSavePath(slot));
    }

    public static bool IsValidManualSlot(int slot)
    {
        return slot >= ManualSlotMin && slot <= ManualSlotMax;
    }

    public static string GetAutoSavePath()
    {
        return Path.Combine(Application.persistentDataPath, AutoSaveFileName);
    }

    public static string GetManualSavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, string.Format(ManualSaveFilePattern, slot));
    }

    public void PrepareBootstrapStateBeforeGridGeneration()
    {
        if (bootstrapPrepared)
        {
            return;
        }

        CacheReferences();

        if (gymSiteManager != null)
        {
            gymSiteManager.InitializeSiteState();
        }

        bootEntryMode = GameEntryRequest.Consume();

        pendingBootSaveData = null;
        pendingBootLoadReason = string.Empty;
        hasPendingBootSaveData = false;

        bool shouldResetToDefaultSite = true;

        switch (bootEntryMode)
        {
            case GameEntryRequest.EntryMode.NewGame:
                Debug.Log("[SaveManager] 새 게임 시작");
                shouldResetToDefaultSite = true;
                break;

            case GameEntryRequest.EntryMode.ContinueFromAutoSave:
                shouldResetToDefaultSite = !TryPreparePendingBootSave(
                    GetAutoSavePath(),
                    "타이틀에서 이어하기"
                );
                break;

            case GameEntryRequest.EntryMode.LoadManualSlot1:
                shouldResetToDefaultSite = !TryPreparePendingBootManualSlot(1);
                break;

            case GameEntryRequest.EntryMode.LoadManualSlot2:
                shouldResetToDefaultSite = !TryPreparePendingBootManualSlot(2);
                break;

            default:
                if (debugAutoLoadAutoSaveWhenOpeningGameSceneDirectly && HasAutoSaveFile())
                {
                    shouldResetToDefaultSite = !TryPreparePendingBootSave(
                        GetAutoSavePath(),
                        "자동 자동 저장 불러오기"
                    );
                }
                break;
        }

        if (shouldResetToDefaultSite && gymSiteManager != null)
        {
            gymSiteManager.ResetToDefaultSite("빈 부지로 초기화");
        }

        bootstrapPrepared = true;
    }

    public void InitializeSaveSystem()
    {
        if (isInitialized)
        {
            return;
        }

        if (!bootstrapPrepared)
        {
            PrepareBootstrapStateBeforeGridGeneration();
        }

        CacheReferences();

        if (walletManager == null || timeManager == null || placementManager == null)
        {
            Debug.LogError("[SaveManager] 저장 시스템을 초기화하기 위한 매니저들을 찾을 수 없습니다.");
            return;
        }

        SubscribeEvents();
        isInitialized = true;

        if (hasPendingBootSaveData)
        {
            ApplySaveData(pendingBootSaveData, false);

            Debug.Log($"[SaveManager] 데이터 로드 완료 / 사유: {pendingBootLoadReason}");

            pendingBootSaveData = null;
            pendingBootLoadReason = string.Empty;
            hasPendingBootSaveData = false;
            return;
        }

        Debug.Log("[SaveManager] 저장 시스템 초기화 완료");
    }

    public void SaveAutoSave(string reason = "")
    {
        if (!EnsureReady())
        {
            return;
        }

        SaveToPath(GetAutoSavePath(), "자동 저장", reason);
    }

    public void SaveManualSave(int slot)
    {
        if (!EnsureReady())
        {
            return;
        }

        if (!IsValidManualSlot(slot))
        {
            Debug.LogWarning($"[SaveManager] 잘못된 수동 저장 슬롯: {slot}");
            return;
        }

        SaveToPath(GetManualSavePath(slot), $"수동 저장 {slot}", "수동 저장 성공");
    }

    private void SaveToPath(string path, string label, string reason)
    {
        if (placementManager == null || staffManager == null || walletManager == null)
        {
            Debug.LogWarning("[SaveManager] 필수 컴포넌트 참조가 누락되어 저장할 수 없습니다.");
            return;
        }

        GameSaveData saveData = new GameSaveData
        {
            year = Mathf.Max(1, timeManager.CurrentYear),
            month = Mathf.Max(1, timeManager.CurrentMonth),
            day = Mathf.Max(1, timeManager.CurrentDay),
            cash = walletManager.CurrentCash,
            lastSettlementText = monthlySettlementManager != null
                ? monthlySettlementManager.LastSettlementText
                : DefaultSettlementText,
            siteState = gymSiteManager != null
                ? gymSiteManager.BuildSaveData()
                : GymSiteSaveData.CreateDefault(),
            totalMaintenanceCost = 0,
            
            currentStarCoin = walletManager != null ? walletManager.CurrentStarCoin : 0,

            placedObjects = placementManager.GetPlacedObjectSaveDataList(),
            hiredStaff = new System.Collections.Generic.List<StaffData>(staffManager.HiredStaff)
        };

        string json = JsonUtility.ToJson(saveData, prettyPrintJson);
        File.WriteAllText(path, json);

        Debug.Log($"[SaveManager] {label} 저장 완료 / 사유: {reason}");
    }

    private bool TryPreparePendingBootSave(string path, string reason)
    {
        if (!File.Exists(path))
        {
            Debug.Log($"[SaveManager] 파일 없음: {path}");
            return false;
        }

        GameSaveData saveData;
        if (!TryReadSaveDataFromPath(path, out saveData))
        {
            return false;
        }

        pendingBootSaveData = saveData;
        pendingBootLoadReason = reason;
        hasPendingBootSaveData = true;

        if (gymSiteManager != null)
        {
            gymSiteManager.ApplySaveData(saveData.siteState, $"{reason} / 사이트 적용");
        }

        return true;
    }

    private bool TryPreparePendingBootManualSlot(int slot)
    {
        return TryPreparePendingBootSave(
            GetManualSavePath(slot),
            $"타이틀에서 수동 {slot} 불러오기"
        );
    }

    private bool TryReadSaveDataFromPath(string path, out GameSaveData saveData)
    {
        saveData = null;

        try
        {
            string json = File.ReadAllText(path);
            saveData = JsonUtility.FromJson<GameSaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("[SaveManager] 파일 파싱 실패");
                return false;
            }

            EnsureSaveDataDefaults(saveData);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveManager] 읽기 실패 : {path}\n{exception}");
            saveData = null;
            return false;
        }
    }

    private void EnsureSaveDataDefaults(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (saveData.siteState == null)
        {
            saveData.siteState = GymSiteSaveData.CreateDefault();
        }

        if (saveData.placedObjects == null)
        {
            saveData.placedObjects = new List<PlacedObjectSaveData>();
        }

        if (string.IsNullOrWhiteSpace(saveData.lastSettlementText))
        {
            saveData.lastSettlementText = DefaultSettlementText;
        }

        NormalizeCalendarValues(saveData);
    }

    private void NormalizeCalendarValues(GameSaveData saveData)
    {
        saveData.year = Mathf.Max(1, saveData.year);

        if (saveData.month < 1)
        {
            saveData.month = 1;
        }

        while (saveData.month > 12)
        {
            saveData.month -= 12;
            saveData.year += 1;
        }

        saveData.day = Mathf.Clamp(saveData.day, 1, Mathf.Max(1, timeManager != null ? timeManager.DaysPerMonth : 30));
    }

    private void ApplySaveData(GameSaveData saveData, bool applySiteState)
    {
        EnsureSaveDataDefaults(saveData);
        CacheReferences();

        if (placementManager == null || walletManager == null || timeManager == null)
        {
            Debug.LogError("[SaveManager] ApplySaveData 필수 참조 누락.");
            return;
        }

        if (applySiteState && gymSiteManager != null)
        {
            gymSiteManager.ApplySaveData(saveData.siteState, "로드 적용");
        }

        placementManager.ClearAllPlacedObjects();

        if (walletManager != null)
        {
            walletManager.LoadWallet(saveData.cash, saveData.currentStarCoin == 0 ? walletManager.startingStarCoin : saveData.currentStarCoin);
        }
        timeManager.SetDate(saveData.year, saveData.month, saveData.day);

        if (saveData.placedObjects != null)
        {
            placementManager.LoadPlacedObjects(saveData.placedObjects);
        }

        if (staffManager != null && saveData.hiredStaff != null)
        {
            staffManager.LoadStaff(saveData.hiredStaff);
        }

        if (monthlySettlementManager != null)
        {
            monthlySettlementManager.SetLastSettlementText(saveData.lastSettlementText);
        }
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

        if (staffManager == null)
        {
            staffManager = FindFirstObjectByType<StaffManager>();
        }

        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (monthlySettlementManager == null)
        {
            monthlySettlementManager = FindFirstObjectByType<MonthlySettlementManager>();
        }

        if (gymSiteManager == null)
        {
            gymSiteManager = FindFirstObjectByType<GymSiteManager>();
        }
    }

    private bool EnsureReady()
    {
        if (!isInitialized)
        {
            InitializeSaveSystem();
        }

        CacheReferences();

        if (!isInitialized || walletManager == null || timeManager == null || placementManager == null)
        {
            Debug.LogError("[SaveManager] 저장 매니저가 준비되지 않았습니다.");
            return false;
        }

        return true;
    }

    private void SubscribeEvents()
    {
        if (isSubscribed)
        {
            return;
        }

        if (placementManager != null)
        {
            placementManager.PlayerPlacedObject += HandlePlayerPlacedObject;
        }

        if (monthlySettlementManager != null)
        {
            monthlySettlementManager.SettlementCompleted += HandleSettlementCompleted;
        }

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (placementManager != null)
        {
            placementManager.PlayerPlacedObject -= HandlePlayerPlacedObject;
        }

        if (monthlySettlementManager != null)
        {
            monthlySettlementManager.SettlementCompleted -= HandleSettlementCompleted;
        }

        isSubscribed = false;
    }

    private void HandlePlayerPlacedObject()
    {
        if (autoSaveOnPlacement)
        {
            SaveAutoSave("기구 배치 완료");
        }
    }

    private void HandleSettlementCompleted()
    {
        if (autoSaveOnSettlement)
        {
            SaveAutoSave("월말 결산 완료");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            return;
        }

        if (autoSaveOnPauseOrFocusLost && isInitialized)
        {
            SaveAutoSave("앱 백그라운드 전환");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            return;
        }

        if (autoSaveOnPauseOrFocusLost && isInitialized)
        {
            SaveAutoSave("앱 포커스 상실");
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}
