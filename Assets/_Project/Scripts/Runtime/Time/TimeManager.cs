using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float secondsPerDay = 30.0f;
    [SerializeField] private int daysPerMonth = 30;
    [SerializeField] private int startYear = 1;
    [SerializeField] private int startMonth = 1;
    [SerializeField] private int startDay = 1;
    [SerializeField] private bool startPaused = false;

    [Header("Speed Preset (Prototype)")]
    [SerializeField] private float normalSpeedMultiplier = 1f;
    [SerializeField] private float fastSpeedMultiplier = 2f;
    [SerializeField] private float veryFastSpeedMultiplier = 4f;
    [SerializeField] private int defaultSpeedPresetIndex = 0;

    [Header("Debug UI")]
    [SerializeField] private bool showDebugInfo = false;

    private float dayTimer = 0f;
    private bool isPaused = false;
    private bool isInitialized = false;

    private int currentYear;
    private int currentMonth;
    private int currentDay;

    private int currentSpeedPresetIndex = 0;

    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    private GUIStyle smallButtonStyle;

    public event Action<int> DayChanged;
    public event Action<int> MonthEnded;
    public event Action<float, string> SpeedChanged;

    public int CurrentYear => currentYear;
    public int CurrentMonth => currentMonth;
    public int CurrentDay => currentDay;
    public int DaysPerMonth => daysPerMonth;
    public float SecondsPerDay => secondsPerDay;

    /// <summary>
    /// [프로토타입/MVP]
    /// 설치 모드에서는 시간이 멈춰야 하므로, 수동 일시정지 + 설치 모드 정지를 함께 반영한다.
    /// </summary>
    public bool IsPaused => isPaused || IsBuildModeTimeFrozen;

    public bool IsBuildModeTimeFrozen => BuildPlayModeManager.IsBuildMode;

    public float CurrentSpeedMultiplier => GetSpeedMultiplierByIndex(currentSpeedPresetIndex);
    public string CurrentSpeedLabel => GetSpeedLabelByIndex(currentSpeedPresetIndex);
    public int CurrentSpeedPresetIndex => currentSpeedPresetIndex;

    public void InitializeTime()
    {
        if (isInitialized)
        {
            return;
        }

        currentYear = Mathf.Max(1, startYear);
        currentMonth = Mathf.Clamp(startMonth, 1, 12);
        currentDay = Mathf.Clamp(startDay, 1, Mathf.Max(1, daysPerMonth));
        isPaused = startPaused;

        currentSpeedPresetIndex = Mathf.Clamp(defaultSpeedPresetIndex, 0, 2);

        dayTimer = 0f;
        isInitialized = true;

        Debug.Log(
            $"[TimeManager] 시간 시스템 초기화 완료 / 날짜: {currentYear}년 {currentMonth}월 {currentDay}일 / " +
            $"기본 속도: {CurrentSpeedLabel}"
        );
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void SetDate(int month, int day)
    {
        SetDate(currentYear <= 0 ? 1 : currentYear, month, day);
    }

    public void SetDate(int year, int month, int day)
    {
        NormalizeYearMonth(ref year, ref month);

        currentYear = year;
        currentMonth = month;
        currentDay = Mathf.Clamp(day, 1, Mathf.Max(1, daysPerMonth));
        dayTimer = 0f;

        Debug.Log($"[TimeManager] 날짜 설정: {currentYear}년 {currentMonth}월 {currentDay}일");
    }

    public void CycleSpeedPreset()
    {
        SetSpeedPreset((currentSpeedPresetIndex + 1) % 3);
    }

    public void SetSpeedPreset(int presetIndex)
    {
        int clamped = Mathf.Clamp(presetIndex, 0, 2);
        if (currentSpeedPresetIndex == clamped)
        {
            return;
        }

        currentSpeedPresetIndex = clamped;

        SpeedChanged?.Invoke(CurrentSpeedMultiplier, CurrentSpeedLabel);

        Debug.Log($"[TimeManager] 시간 속도 변경: {CurrentSpeedLabel}");
    }

    /// <summary>
    /// [프로토타입/MVP]
    /// 설치 모드에서는 게임이 멈춘 것으로 취급해야 하므로 시뮬레이션 델타도 0을 반환한다.
    /// </summary>
    public float GetSimulationDeltaTime()
    {
        if (IsPaused)
        {
            return 0f;
        }

        return Time.deltaTime * CurrentSpeedMultiplier;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (IsPaused)
        {
            return;
        }

        if (secondsPerDay <= 0f)
        {
            return;
        }

        dayTimer += Time.deltaTime * CurrentSpeedMultiplier;

        while (dayTimer >= secondsPerDay)
        {
            dayTimer -= secondsPerDay;
            AdvanceOneDay();
        }
    }

    private void AdvanceOneDay()
    {
        currentDay += 1;

        if (currentDay > daysPerMonth)
        {
            int endedMonth = currentMonth;

            currentDay = 1;

            if (currentMonth >= 12)
            {
                currentMonth = 1;
                currentYear += 1;
            }
            else
            {
                currentMonth += 1;
            }

            MonthEnded?.Invoke(endedMonth);
        }

        DayChanged?.Invoke(currentDay);

        Debug.Log($"[TimeManager] 날짜 진행: {currentYear}년 {currentMonth}월 {currentDay}일");
    }

    private void NormalizeYearMonth(ref int year, ref int month)
    {
        year = Mathf.Max(1, year);

        if (month < 1)
        {
            month = 1;
        }

        while (month > 12)
        {
            month -= 12;
            year += 1;
        }
    }

    private float GetSpeedMultiplierByIndex(int index)
    {
        switch (index)
        {
            case 1:
                return Mathf.Max(1f, fastSpeedMultiplier);

            case 2:
                return Mathf.Max(1f, veryFastSpeedMultiplier);

            default:
                return Mathf.Max(0.1f, normalSpeedMultiplier);
        }
    }

    private string GetSpeedLabelByIndex(int index)
    {
        switch (index)
        {
            case 1:
                return "x2";

            case 2:
                return "x4";

            default:
                return "x1";
        }
    }

    // Legacy UI Disabled
    private void Disabled_OnGUI()
    {
        if (!showDebugInfo)
        {
            return;
        }

        EnsureStyles();

        float boxX = 12f;
        float boxY = 86f;
        float boxWidth = 190f;
        float boxHeight = 102f;

        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), GUIContent.none, boxStyle);

        GUI.Label(
            new Rect(boxX + 12f, boxY + 10f, boxWidth - 24f, 20f),
            $"날짜: {currentMonth}월 {currentDay}일",
            labelStyle
        );

        string timeStateText;
        if (IsBuildModeTimeFrozen)
        {
            timeStateText = "시간: 설치 모드(정지)";
        }
        else if (isPaused)
        {
            timeStateText = "시간: 일시정지";
        }
        else
        {
            timeStateText = $"시간: 자동 ({secondsPerDay:0.0}초/일)";
        }

        GUI.Label(
            new Rect(boxX + 12f, boxY + 30f, boxWidth - 24f, 20f),
            timeStateText,
            labelStyle
        );

        GUI.Label(
            new Rect(boxX + 12f, boxY + 50f, boxWidth - 24f, 20f),
            $"연도: {currentYear}",
            labelStyle
        );

        GUI.Label(
            new Rect(boxX + 12f, boxY + 72f, 72f, 20f),
            "속도:",
            labelStyle
        );

        if (GUI.Button(
            new Rect(boxX + 62f, boxY + 70f, 56f, 24f),
            CurrentSpeedLabel,
            smallButtonStyle))
        {
            CycleSpeedPreset();
        }
    }

    private void EnsureStyles()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(12, 12, 12, 12);
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 14;
            labelStyle.normal.textColor = Color.white;
        }

        if (smallButtonStyle == null)
        {
            smallButtonStyle = new GUIStyle(GUI.skin.button);
            smallButtonStyle.fontSize = 12;
            smallButtonStyle.alignment = TextAnchor.MiddleCenter;
        }
    }
}
