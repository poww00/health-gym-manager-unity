using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// [중간 단계 / 임시 저장 안정화용]
/// dirty flag + 주기 autosave 매니저.
///
/// 정식 SaveManager 전면 개편 전까지,
/// 기존 SaveManager 위에 덧대는 방식으로
/// "변경 감지 -> 잠깐 안정화 -> autosave"를 수행한다.
///
/// 이 스크립트는 완성형 저장 시스템이 아니라 중간 단계 보강용이다.
/// - 기존 SaveManager 흐름은 유지
/// - reflection으로 SaveManager autosave 메서드를 찾음
/// - 날짜 / 돈 / 그리드 점유 / GymEconomy 상태가 바뀌면 dirty
/// - quiet time 이후 autosave 시도
/// - pause / focus lost / quit 시 강제 autosave 시도
/// </summary>
[DefaultExecutionOrder(1200)]
public sealed class AutoSaveHeartbeatManager : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const string GridCellTypeName = "GridCell";

    [Header("External References (비워두면 자동 탐색)")]
    [SerializeField] private MonoBehaviour saveManager;
    [SerializeField] private MonoBehaviour timeManager;
    [SerializeField] private MonoBehaviour walletManager;
    [SerializeField] private MonoBehaviour gymEconomyManager;

    [Header("Dirty Tracking")]
    [SerializeField] private float bootstrapDelay = 1.0f;
    [SerializeField] private float fingerprintCheckInterval = 1.0f;
    [SerializeField] private float quietSecondsBeforeSave = 1.5f;

    [Header("Auto Save Cadence")]
    [SerializeField] private float minSecondsBetweenSaves = 8.0f;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnApplicationFocusLost = true;
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private bool alwaysSaveOnLifecycleEvents = true;

    [Header("Fingerprint Sources")]
    [SerializeField] private bool includeDate = true;
    [SerializeField] private bool includeWalletMoney = true;
    [SerializeField] private bool includeGridOccupancy = true;
    [SerializeField] private bool includeGymEconomyState = true;
    [SerializeField] private int cellsPerMachine = 4;

    [Header("Debug")]
    [SerializeField] private bool logDirtyDetected = false;
    [SerializeField] private bool logAutoSave = true;
    [SerializeField] private bool logResolvedSaveMethod = true;

    private float bootTime;
    private float nextFingerprintCheckTime;
    private float lastDirtyTime = -999f;
    private float lastSaveTime = -999f;

    private bool baselineReady;
    private bool dirty;
    private bool hasLoggedMissingSaveMethod;
    private bool hasLoggedResolvedSaveMethod;

    private int lastFingerprint;
    private string lastDirtySummary = string.Empty;

    private MethodInfo cachedResolvedSaveMethod;
    private bool cachedResolvedSaveMethodUsesReason;

    private static readonly string[] YearNames = { "CurrentYear", "currentYear", "Year", "year" };
    private static readonly string[] MonthNames = { "CurrentMonth", "currentMonth", "Month", "month" };
    private static readonly string[] DayNames = { "CurrentDay", "currentDay", "Day", "day" };
    private static readonly string[] OccupiedNames = { "IsOccupied", "isOccupied", "Occupied", "occupied", "IsFilled", "isFilled" };
    private static readonly string[] MoneyNames = { "CurrentMoney", "currentMoney", "Money", "money", "Balance", "balance", "CurrentBalance", "currentBalance" };

    private void Awake()
    {
        bootTime = Time.unscaledTime;
        AutoResolveManagers();
    }

    private void Start()
    {
        if (bootstrapDelay <= 0f)
        {
            EstablishBaseline();
        }
    }

    private void Update()
    {
        AutoResolveManagers();

        if (!baselineReady)
        {
            if (Time.unscaledTime - bootTime >= bootstrapDelay)
            {
                EstablishBaseline();
            }

            return;
        }

        if (Time.unscaledTime >= nextFingerprintCheckTime)
        {
            nextFingerprintCheckTime = Time.unscaledTime + fingerprintCheckInterval;
            CheckFingerprintAndMarkDirty();
        }

        TryFlushDirtyAutoSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnApplicationPause || !pauseStatus)
        {
            return;
        }

        HandleLifecycleSave("Pause");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!saveOnApplicationFocusLost || hasFocus)
        {
            return;
        }

        HandleLifecycleSave("FocusLost");
    }

    private void OnApplicationQuit()
    {
        if (!saveOnApplicationQuit)
        {
            return;
        }

        HandleLifecycleSave("Quit");
    }

    /// <summary>
    /// 다른 시스템에서 직접 dirty를 표시할 때 사용 가능.
    /// </summary>
    public void MarkDirty(string reason = "ManualMarkDirty")
    {
        if (!baselineReady)
        {
            return;
        }

        dirty = true;
        lastDirtyTime = Time.unscaledTime;
        lastDirtySummary = string.IsNullOrEmpty(reason) ? "ManualMarkDirty" : reason;

        if (logDirtyDetected)
        {
            Debug.Log($"[AutoSaveHeartbeatManager] Dirty 수동 표시: {lastDirtySummary}");
        }
    }

    /// <summary>
    /// 다른 시스템에서 강제 autosave가 필요할 때 사용 가능.
    /// </summary>
    public void ForceAutoSaveNow(string reason = "ManualForceAutoSave")
    {
        TryPerformAutoSave(reason, ignoreCadence: true);
    }

    private void HandleLifecycleSave(string reason)
    {
        if (!baselineReady)
        {
            EstablishBaseline();
        }

        if (!dirty && !alwaysSaveOnLifecycleEvents)
        {
            return;
        }

        TryPerformAutoSave($"Lifecycle:{reason}", ignoreCadence: true);
    }

    private void EstablishBaseline()
    {
        if (!TryBuildFingerprint(out int fingerprint, out string summary))
        {
            return;
        }

        baselineReady = true;
        dirty = false;
        lastFingerprint = fingerprint;
        lastDirtySummary = summary;
        nextFingerprintCheckTime = Time.unscaledTime + fingerprintCheckInterval;
    }

    private void CheckFingerprintAndMarkDirty()
    {
        if (!TryBuildFingerprint(out int fingerprint, out string summary))
        {
            return;
        }

        if (fingerprint == lastFingerprint)
        {
            return;
        }

        lastFingerprint = fingerprint;
        dirty = true;
        lastDirtyTime = Time.unscaledTime;
        lastDirtySummary = summary;

        if (logDirtyDetected)
        {
            Debug.Log($"[AutoSaveHeartbeatManager] Dirty 감지: {summary}");
        }
    }

    private void TryFlushDirtyAutoSave()
    {
        if (!dirty)
        {
            return;
        }

        if (Time.unscaledTime - lastDirtyTime < quietSecondsBeforeSave)
        {
            return;
        }

        if (Time.unscaledTime - lastSaveTime < minSecondsBetweenSaves)
        {
            return;
        }

        TryPerformAutoSave("DirtyFlush", ignoreCadence: false);
    }

    private bool TryPerformAutoSave(string reason, bool ignoreCadence)
    {
        AutoResolveManagers();

        if (saveManager == null)
        {
            if (!hasLoggedMissingSaveMethod)
            {
                Debug.LogWarning("[AutoSaveHeartbeatManager] SaveManager를 찾지 못했습니다.");
                hasLoggedMissingSaveMethod = true;
            }

            return false;
        }

        if (!ignoreCadence && Time.unscaledTime - lastSaveTime < minSecondsBetweenSaves)
        {
            return false;
        }

        if (!TryInvokeResolvedAutoSave(saveManager, reason))
        {
            if (!hasLoggedMissingSaveMethod)
            {
                Debug.LogWarning("[AutoSaveHeartbeatManager] autosave 호출 메서드를 찾지 못했습니다. SaveManager 메서드명을 확인하세요.");
                hasLoggedMissingSaveMethod = true;
            }

            return false;
        }

        hasLoggedMissingSaveMethod = false;

        // GymEconomyManager의 autosave sidecar도 같은 타이밍에 맞춤
        TryMirrorEconomyAutoSlot();

        dirty = false;
        lastSaveTime = Time.unscaledTime;

        if (TryBuildFingerprint(out int fingerprint, out _))
        {
            lastFingerprint = fingerprint;
            baselineReady = true;
        }

        if (logAutoSave)
        {
            Debug.Log($"[AutoSaveHeartbeatManager] Autosave 완료. Reason={reason}" +
                      (!string.IsNullOrEmpty(lastDirtySummary) ? $" | Snapshot={lastDirtySummary}" : string.Empty));
        }

        return true;
    }

    private bool TryInvokeResolvedAutoSave(object target, string reason)
    {
        if (target == null)
        {
            return false;
        }

        if (cachedResolvedSaveMethod != null)
        {
            return InvokeSaveMethod(target, cachedResolvedSaveMethod, cachedResolvedSaveMethodUsesReason, reason);
        }

        Type type = target.GetType();

        // 1차: 가장 가능성 높은 메서드명들 먼저 탐색
        string[] zeroArgCandidates =
        {
            "SaveAuto",
            "AutoSave",
            "SaveAutoSave",
            "WriteAutoSave",
            "SaveToAutoSlot",
            "SaveGameToAutoSlot",
            "SaveAutosave",
            "RequestAutoSave",
            "AutoSaveNow",
            "ForceAutoSave"
        };

        string[] reasonCandidates =
        {
            "SaveAuto",
            "AutoSave",
            "SaveAutoSave",
            "WriteAutoSave",
            "SaveToAutoSlot",
            "SaveGameToAutoSlot",
            "SaveAutosave",
            "RequestAutoSave",
            "AutoSaveNow",
            "ForceAutoSave",
            "TriggerAutoSave",
            "RequestSave",
            "SaveNow"
        };

        for (int i = 0; i < zeroArgCandidates.Length; i++)
        {
            MethodInfo method = type.GetMethod(zeroArgCandidates[i], Flags, null, Type.EmptyTypes, null);
            if (method == null)
            {
                continue;
            }

            if (InvokeSaveMethod(target, method, false, reason))
            {
                CacheResolvedMethod(method, false);
                return true;
            }
        }

        for (int i = 0; i < reasonCandidates.Length; i++)
        {
            MethodInfo method = type.GetMethod(reasonCandidates[i], Flags, null, new[] { typeof(string) }, null);
            if (method == null)
            {
                continue;
            }

            if (InvokeSaveMethod(target, method, true, reason))
            {
                CacheResolvedMethod(method, true);
                return true;
            }
        }

        // 2차 fallback:
        // 이름에 save가 들어가고,
        // 파라미터가 0개 또는 string 1개인 메서드를 자동 탐색
        MethodInfo[] methods = type.GetMethods(Flags);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];

            if (method == null)
            {
                continue;
            }

            string methodName = method.Name.ToLowerInvariant();
            if (!methodName.Contains("save"))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                if (InvokeSaveMethod(target, method, false, reason))
                {
                    CacheResolvedMethod(method, false);
                    return true;
                }
            }
            else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                if (InvokeSaveMethod(target, method, true, reason))
                {
                    CacheResolvedMethod(method, true);
                    return true;
                }
            }
        }

        return false;
    }

    private void CacheResolvedMethod(MethodInfo method, bool usesReason)
    {
        cachedResolvedSaveMethod = method;
        cachedResolvedSaveMethodUsesReason = usesReason;

        if (!hasLoggedResolvedSaveMethod && logResolvedSaveMethod)
        {
            string modeText = usesReason ? "string reason" : "no args";
            Debug.Log($"[AutoSaveHeartbeatManager] SaveManager autosave 메서드 연결 성공: {method.Name} ({modeText})");
            hasLoggedResolvedSaveMethod = true;
        }
    }

    private bool InvokeSaveMethod(object target, MethodInfo method, bool usesReason, string reason)
    {
        if (target == null || method == null)
        {
            return false;
        }

        try
        {
            object result = usesReason
                ? method.Invoke(target, new object[] { reason })
                : method.Invoke(target, null);

            if (method.ReturnType == typeof(bool) && result is bool success)
            {
                return success;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryBuildFingerprint(out int fingerprint, out string summary)
    {
        fingerprint = 17;
        List<string> parts = new List<string>(8);
        bool hasAny = false;

        if (includeDate && TryReadDate(out int year, out int month, out int day))
        {
            AppendHash(ref fingerprint, year);
            AppendHash(ref fingerprint, month);
            AppendHash(ref fingerprint, day);
            parts.Add($"Date={year}/{month}/{day}");
            hasAny = true;
        }

        if (includeWalletMoney && TryReadIntValue(walletManager, MoneyNames, out int money))
        {
            AppendHash(ref fingerprint, money);
            parts.Add($"Money={money}");
            hasAny = true;
        }

        if (includeGridOccupancy)
        {
            int occupiedCellCount = CountOccupiedCells();
            int machineCount = cellsPerMachine <= 0 ? 0 : occupiedCellCount / cellsPerMachine;

            AppendHash(ref fingerprint, occupiedCellCount);
            AppendHash(ref fingerprint, machineCount);

            parts.Add($"Occupied={occupiedCellCount}");
            parts.Add($"Machines={machineCount}");
            hasAny = true;
        }

        if (includeGymEconomyState && gymEconomyManager != null)
        {
            bool hasEconomyValue = false;

            if (TryInvokeZeroArgInt(gymEconomyManager, "GetActiveMemberCount", out int activeMembers))
            {
                AppendHash(ref fingerprint, activeMembers);
                parts.Add($"Members={activeMembers}");
                hasEconomyValue = true;
            }

            if (TryInvokeZeroArgInt(gymEconomyManager, "GetCurrentTrainerCount", out int trainerCount))
            {
                AppendHash(ref fingerprint, trainerCount);
                parts.Add($"Trainers={trainerCount}");
                hasEconomyValue = true;
            }

            if (TryInvokeZeroArgFloat(gymEconomyManager, "GetSatisfaction01", out float satisfaction))
            {
                int satisfactionMilli = Mathf.RoundToInt(satisfaction * 1000f);
                AppendHash(ref fingerprint, satisfactionMilli);
                parts.Add($"Satisfaction={(satisfaction * 100f):0.#}%");
                hasEconomyValue = true;
            }

            hasAny |= hasEconomyValue;
        }

        summary = hasAny ? string.Join(" | ", parts.ToArray()) : "NoFingerprintSources";
        return hasAny;
    }

    private int CountOccupiedCells()
    {
        int occupiedCellCount = 0;

        foreach (MonoBehaviour behaviour in FindSceneBehaviours())
        {
            if (behaviour == null || behaviour.GetType().Name != GridCellTypeName)
            {
                continue;
            }

            if (TryReadBoolValue(behaviour, OccupiedNames, out bool isOccupied) && isOccupied)
            {
                occupiedCellCount++;
            }
        }

        return occupiedCellCount;
    }

    private void TryMirrorEconomyAutoSlot()
    {
        if (gymEconomyManager == null)
        {
            return;
        }

        Type type = gymEconomyManager.GetType();
        MethodInfo saveMethod = type.GetMethod("SaveEconomyStateToSlot", Flags);

        if (saveMethod == null)
        {
            return;
        }

        ParameterInfo[] parameters = saveMethod.GetParameters();
        if (parameters.Length != 1)
        {
            return;
        }

        Type slotType = parameters[0].ParameterType;
        if (!slotType.IsEnum)
        {
            return;
        }

        object autoValue;
        try
        {
            autoValue = Enum.Parse(slotType, "Auto");
        }
        catch
        {
            return;
        }

        try
        {
            saveMethod.Invoke(gymEconomyManager, new[] { autoValue });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AutoSaveHeartbeatManager] GymEconomy sidecar autosave 미러 실패.\n{e}");
        }
    }

    private void AutoResolveManagers()
    {
        if (saveManager == null)
        {
            saveManager = FindBehaviourByTypeName("SaveManager");
        }

        if (timeManager == null)
        {
            timeManager = FindBehaviourByTypeName("TimeManager");
        }

        if (walletManager == null)
        {
            walletManager = FindBehaviourByTypeName("WalletManager");
        }

        if (gymEconomyManager == null)
        {
            gymEconomyManager = FindBehaviourByTypeName("GymEconomyManager");
        }
    }

    private MonoBehaviour FindBehaviourByTypeName(string typeName)
    {
        foreach (MonoBehaviour behaviour in FindSceneBehaviours())
        {
            if (behaviour != null && behaviour.GetType().Name == typeName)
            {
                return behaviour;
            }
        }

        return null;
    }

    private IEnumerable<MonoBehaviour> FindSceneBehaviours()
    {
        MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour behaviour = all[i];

            if (behaviour == null || behaviour.gameObject == null || !behaviour.gameObject.scene.IsValid())
            {
                continue;
            }

            yield return behaviour;
        }
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

        bool hasMonth = TryReadIntValue(timeManager, MonthNames, out month);
        bool hasDay = TryReadIntValue(timeManager, DayNames, out day);

        if (!TryReadIntValue(timeManager, YearNames, out year))
        {
            year = 1;
        }

        return hasMonth && hasDay;
    }

    private bool TryReadIntValue(object target, string[] candidateNames, out int value)
    {
        value = 0;

        if (target == null)
        {
            return false;
        }

        Type type = target.GetType();

        for (int i = 0; i < candidateNames.Length; i++)
        {
            string name = candidateNames[i];

            PropertyInfo property = type.GetProperty(name, Flags);
            if (property != null && property.CanRead)
            {
                object raw = property.GetValue(target, null);
                if (TryConvertToInt(raw, out value))
                {
                    return true;
                }
            }

            FieldInfo field = type.GetField(name, Flags);
            if (field != null)
            {
                object raw = field.GetValue(target);
                if (TryConvertToInt(raw, out value))
                {
                    return true;
                }
            }

            MethodInfo method = type.GetMethod(name, Flags, null, Type.EmptyTypes, null);
            if (method != null)
            {
                object raw = method.Invoke(target, null);
                if (TryConvertToInt(raw, out value))
                {
                    return true;
                }
            }

            string getterName = "Get" + name;
            method = type.GetMethod(getterName, Flags, null, Type.EmptyTypes, null);
            if (method != null)
            {
                object raw = method.Invoke(target, null);
                if (TryConvertToInt(raw, out value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryReadBoolValue(object target, string[] candidateNames, out bool value)
    {
        value = false;

        if (target == null)
        {
            return false;
        }

        Type type = target.GetType();

        for (int i = 0; i < candidateNames.Length; i++)
        {
            string name = candidateNames[i];

            PropertyInfo property = type.GetProperty(name, Flags);
            if (property != null && property.CanRead)
            {
                object raw = property.GetValue(target, null);
                if (TryConvertToBool(raw, out value))
                {
                    return true;
                }
            }

            FieldInfo field = type.GetField(name, Flags);
            if (field != null)
            {
                object raw = field.GetValue(target);
                if (TryConvertToBool(raw, out value))
                {
                    return true;
                }
            }

            MethodInfo method = type.GetMethod(name, Flags, null, Type.EmptyTypes, null);
            if (method != null)
            {
                object raw = method.Invoke(target, null);
                if (TryConvertToBool(raw, out value))
                {
                    return true;
                }
            }

            string getterName = "Get" + name;
            method = type.GetMethod(getterName, Flags, null, Type.EmptyTypes, null);
            if (method != null)
            {
                object raw = method.Invoke(target, null);
                if (TryConvertToBool(raw, out value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryInvokeZeroArgInt(object target, string methodName, out int value)
    {
        value = 0;

        if (target == null)
        {
            return false;
        }

        MethodInfo method = target.GetType().GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
        if (method == null)
        {
            return false;
        }

        try
        {
            object raw = method.Invoke(target, null);
            return TryConvertToInt(raw, out value);
        }
        catch
        {
            return false;
        }
    }

    private bool TryInvokeZeroArgFloat(object target, string methodName, out float value)
    {
        value = 0f;

        if (target == null)
        {
            return false;
        }

        MethodInfo method = target.GetType().GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
        if (method == null)
        {
            return false;
        }

        try
        {
            object raw = method.Invoke(target, null);
            return TryConvertToFloat(raw, out value);
        }
        catch
        {
            return false;
        }
    }

    private bool TryConvertToInt(object raw, out int value)
    {
        value = 0;

        if (raw == null)
        {
            return false;
        }

        try
        {
            value = Convert.ToInt32(raw);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryConvertToFloat(object raw, out float value)
    {
        value = 0f;

        if (raw == null)
        {
            return false;
        }

        try
        {
            value = Convert.ToSingle(raw);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryConvertToBool(object raw, out bool value)
    {
        value = false;

        if (raw == null)
        {
            return false;
        }

        try
        {
            value = Convert.ToBoolean(raw);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void AppendHash(ref int hash, int value)
    {
        unchecked
        {
            hash = hash * 31 + value;
        }
    }
}