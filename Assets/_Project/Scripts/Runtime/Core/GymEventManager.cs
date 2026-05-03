using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [인게임 이벤트 시스템]
/// GDD 14절 기반 — 퇴근 러시, 다이어트 시즌, 판타지 이벤트를 구현합니다.
/// TimeManager.DayChanged 훅을 통해 날짜/월 기반으로 이벤트를 발생시킵니다.
/// 이벤트는 CustomerFlowManager의 스폰률 및 GymEconomyManager의 성장 수치에 일시적으로 영향을 줍니다.
/// </summary>
[DefaultExecutionOrder(1150)]
public class GymEventManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // 공개 인터페이스: 현재 활성 이벤트 효과
    // ─────────────────────────────────────────

    /// <summary>현재 활성 이벤트로 인한 손님 스폰 배율 (기본 1.0)</summary>
    public float ActiveSpawnMultiplier { get; private set; } = 1f;

    /// <summary>현재 활성 이벤트로 인한 만족도 보정 (0 중립)</summary>
    public float ActiveSatisfactionBonus { get; private set; } = 0f;

    /// <summary>현재 활성 이벤트로 인한 리뷰/평판 보정 (0 중립)</summary>
    public float ActiveReputationBonus { get; private set; } = 0f;

    /// <summary>현재 활성 이벤트 이름. 없으면 null</summary>
    public string ActiveEventName { get; private set; } = null;

    // UI용 피드 — HUD에서 읽을 수 있도록 공개
    public IReadOnlyList<string> RecentEventLog => recentEventLog;

    // ─────────────────────────────────────────
    // Inspector 참조
    // ─────────────────────────────────────────

    [Header("References")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private CustomerFlowManager customerFlowManager;

    [Header("직장인 집중 방문일 (Busy Day)")]
    [SerializeField] private bool enableBusyDay = true;
    [SerializeField, Range(1, 28)] private int busyDayOfMonth = 5; // 매 5, 10, 15...
    [SerializeField, Range(1f, 4f)] private float busyDaySpawnMultiplier = 2.2f;
    [SerializeField, Range(0, 30)] private int busyDayDurationDays = 1;

    [Header("다이어트 시즌 (Diet Season)")]
    [SerializeField] private bool enableDietSeason = true;
    [SerializeField, Range(1, 12)] private int dietSeasonMonth = 1; // 1월 = 신년 다짐
    [SerializeField, Range(1f, 3f)] private float dietSeasonSpawnMultiplier = 1.8f;
    [SerializeField, Range(0f, 0.15f)] private float dietSeasonSatisfactionBonus = 0.05f;
    [SerializeField, Range(0, 30)] private int dietSeasonDurationDays = 21;

    [Header("방학 특수 (School Vacation)")]
    [SerializeField] private bool enableVacationSeason = true;
    [SerializeField, Range(1, 12)] private int summerVacationMonth = 8; // 8월 여름방학
    [SerializeField, Range(1, 12)] private int winterVacationMonth = 2; // 2월 겨울방학
    [SerializeField, Range(1f, 3f)] private float vacationSeasonSpawnMultiplier = 1.6f;
    [SerializeField, Range(0f, 0.15f)] private float vacationSeasonSatisfactionBonus = 0.04f;
    [SerializeField, Range(0, 30)] private int vacationSeasonDurationDays = 21;

    [Header("판타지 이벤트 (Fantasy Event)")]
    [SerializeField] private bool enableFantasyEvent = true;
    [SerializeField, Range(0f, 1f)] private float fantasyEventDailyChance = 0.08f; // 8% 확률/일
    [SerializeField, Range(1f, 2f)] private float fantasyEventSpawnMultiplier = 1.4f;
    [SerializeField, Range(0f, 0.15f)] private float fantasyEventReputationBonus = 0.08f;
    [SerializeField, Range(0, 7)] private int fantasyEventDurationDays = 2;

    [Header("이벤트 로그")]
    [SerializeField] private int maxEventLogEntries = 6;

    // ─────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────

    private int eventRemainingDays = 0;
    private readonly List<string> recentEventLog = new List<string>();

    private bool isInitialized = false;

    // 판타지 이벤트 이름 풀
    private static readonly string[] FantasyEventNames =
    {
        "⚔️ 전사족 단체 방문",
        "🧙 마법사 길드 견학",
        "🐉 드래곤 슬레이어 PT",
        "👻 몬스터 헌터 투어",
        "🧝 엘프 요원 방문",
    };

    // ─────────────────────────────────────────
    // 초기화 / 구독
    // ─────────────────────────────────────────

    private void Awake() => ResolveReferences();

    private void Start()
    {
        if (isInitialized) return;
        ResolveReferences();
        Subscribe();
        isInitialized = true;
        Debug.Log("[GymEventManager] 인게임 이벤트 시스템 초기화 완료");
    }

    private void OnDestroy() => Unsubscribe();

    private void ResolveReferences()
    {
        if (timeManager == null) timeManager = FindFirstObjectByType<TimeManager>();
        if (customerFlowManager == null) customerFlowManager = FindFirstObjectByType<CustomerFlowManager>();
    }

    private void Subscribe()
    {
        if (timeManager == null) return;
        timeManager.DayChanged -= HandleDayChanged;
        timeManager.DayChanged += HandleDayChanged;
    }

    private void Unsubscribe()
    {
        if (timeManager == null) return;
        timeManager.DayChanged -= HandleDayChanged;
    }

    // ─────────────────────────────────────────
    // 핵심 로직: 날짜 변경 시 이벤트 판정
    // ─────────────────────────────────────────

    private void HandleDayChanged(int newDay)
    {
        // 기존 이벤트 남은 기간 감소
        if (eventRemainingDays > 0)
        {
            eventRemainingDays--;
            if (eventRemainingDays <= 0)
            {
                EndCurrentEvent();
            }
            return; // 이미 이벤트 중이면 새 이벤트 판정 안 함
        }

        // 판정 순서: 다이어트 시즌 > 퇴근 러시 > 판타지 (우선순위)
        int month = timeManager != null ? timeManager.CurrentMonth : 1;

        if (enableDietSeason && month == dietSeasonMonth && newDay == 1)
        {
            StartEvent(
                "🏋️ 신년 다이어트 시즌",
                dietSeasonSpawnMultiplier,
                dietSeasonSatisfactionBonus,
                0f,
                dietSeasonDurationDays
            );
            return;
        }

        if (enableVacationSeason && (month == summerVacationMonth || month == winterVacationMonth) && newDay == 1)
        {
            string seasonName = month == summerVacationMonth ? "🌻 여름 방학 특수" : "⛄ 겨울 방학 특수";
            StartEvent(
                seasonName,
                vacationSeasonSpawnMultiplier,
                vacationSeasonSatisfactionBonus,
                0f,
                vacationSeasonDurationDays
            );
            return;
        }

        if (enableBusyDay && (newDay % busyDayOfMonth == 0))
        {
            StartEvent(
                "💼 직장인 집중 방문일",
                busyDaySpawnMultiplier,
                0f,
                0f,
                busyDayDurationDays
            );
            return;
        }

        if (enableFantasyEvent && UnityEngine.Random.value < fantasyEventDailyChance)
        {
            string name = FantasyEventNames[UnityEngine.Random.Range(0, FantasyEventNames.Length)];
            StartEvent(
                name,
                fantasyEventSpawnMultiplier,
                0f,
                fantasyEventReputationBonus,
                fantasyEventDurationDays
            );
        }
    }

    private void StartEvent(string eventName, float spawnMult, float satisfBonus, float repBonus, int durationDays)
    {
        ActiveSpawnMultiplier = spawnMult;
        ActiveSatisfactionBonus = satisfBonus;
        ActiveReputationBonus = repBonus;
        ActiveEventName = eventName;
        eventRemainingDays = Mathf.Max(1, durationDays);

        string logEntry = $"[{timeManager?.CurrentMonth}월 {timeManager?.CurrentDay}일] {eventName} 시작 ({durationDays}일)";
        PushLog(logEntry);
        Debug.Log($"[GymEventManager] {logEntry} / 스폰x{spawnMult:0.0} 만족도+{satisfBonus:0.00} 평판+{repBonus:0.00}");
    }

    private void EndCurrentEvent()
    {
        string ended = ActiveEventName ?? "이벤트";
        ActiveSpawnMultiplier = 1f;
        ActiveSatisfactionBonus = 0f;
        ActiveReputationBonus = 0f;
        ActiveEventName = null;
        eventRemainingDays = 0;

        string logEntry = $"[{timeManager?.CurrentMonth}월 {timeManager?.CurrentDay}일] {ended} 종료";
        PushLog(logEntry);
        Debug.Log($"[GymEventManager] {logEntry}");
    }

    private void PushLog(string entry)
    {
        recentEventLog.Add(entry);
        while (recentEventLog.Count > maxEventLogEntries)
        {
            recentEventLog.RemoveAt(0);
        }
    }

    // ─────────────────────────────────────────
    // 헬퍼: 다른 매니저가 이벤트 효과를 쿼리할 수 있도록
    // ─────────────────────────────────────────

    /// <summary>이벤트가 현재 활성 상태인지 여부</summary>
    public bool IsEventActive => eventRemainingDays > 0;

    /// <summary>남은 이벤트 기간 (일)</summary>
    public int EventRemainingDays => eventRemainingDays;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Header("디버그")]
    [SerializeField] private bool showEventDebugHud = false;
    private Rect debugRect = new Rect(Screen.width - 320, 100, 280, 250);

    private void Disabled_OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 150, Screen.height - 250, 140, 40), "이벤트 토글")) showEventDebugHud = !showEventDebugHud;
        if (showEventDebugHud) debugRect = GUILayout.Window(2001, debugRect, DrawDebugWindow, "이벤트 디버그 (드래그)");
    }

    private void DrawDebugWindow(int windowID)
    {
        if (IsEventActive)
        {
            GUILayout.Label($"✅ 활성: {ActiveEventName}");
            GUILayout.Label($"   남은 기간: {EventRemainingDays}일");
            GUILayout.Label($"   스폰x{ActiveSpawnMultiplier:0.0}  만족+{ActiveSatisfactionBonus:0.00}  평판+{ActiveReputationBonus:0.00}");
        }
        else
        {
            GUILayout.Label("⬜ 이벤트 없음");
        }

        GUILayout.Space(4);
        GUILayout.Label("최근 이벤트:");
        for (int i = recentEventLog.Count - 1; i >= Mathf.Max(0, recentEventLog.Count - 3); i--)
        {
            GUILayout.Label($"  {recentEventLog[i]}");
        }

        GUILayout.Space(4);
        if (GUILayout.Button("직장인 몰림 강제 발동"))
        {
            StartEvent("💼 [강제] 직장인 집중 방문일", busyDaySpawnMultiplier, 0f, 0f, 1);
        }
        if (GUILayout.Button("판타지 이벤트 강제 발동"))
        {
            string name = FantasyEventNames[UnityEngine.Random.Range(0, FantasyEventNames.Length)];
            StartEvent(name, fantasyEventSpawnMultiplier, 0f, fantasyEventReputationBonus, 2);
        }

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
#endif
}
