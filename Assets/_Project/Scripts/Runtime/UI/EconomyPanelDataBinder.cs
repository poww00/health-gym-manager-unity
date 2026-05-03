using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class EconomyPanelDataBinder : MonoBehaviour
{
    private const float RefreshInterval = 0.35f;
    private const float DefaultChartBaselineY = -58f;
    private const float DefaultMaxBarHeight = 112f;

    private WalletManager walletManager;
    private GymEconomyManager economyManager;
    private StaffManager staffManager;
    private TimeManager timeManager;

    private float nextRefreshAt;
    private EconomyHistoryFile loadedHistory;
    private bool historyLoaded;

    private Text incomeSummaryValue;
    private Text expenseSummaryValue;
    private Text profitSummaryValue;
    private Text cashSummaryValue;

    private Text membershipValue;
    private Text dailyValue;
    private Text incomeTotalValue;

    private Text salaryValue;
    private Text maintenanceValue;
    private Text expenseTotalValue;

    private Text memoText;

    private readonly List<EconomyGraphSample> chartSamples = new List<EconomyGraphSample>(7);

    private void OnEnable()
    {
        ResolveManagers();
        ResolveTexts();
        RefreshNow();
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < nextRefreshAt)
        {
            return;
        }

        nextRefreshAt = Time.realtimeSinceStartup + RefreshInterval;
        RefreshNow();
    }

    public void RefreshNow()
    {
        ResolveManagers();
        ResolveTexts();

        EconomyValues values = ReadEconomyValues();

        SetText(incomeSummaryValue, FormatMoney(values.todayIncome));
        SetText(expenseSummaryValue, FormatMoney(values.todayExpense));
        SetText(profitSummaryValue, FormatSignedMoney(values.todayNet));
        SetText(cashSummaryValue, FormatMoney(values.currentCash));

        SetText(membershipValue, FormatMoney(values.membershipRevenue));
        SetText(dailyValue, FormatMoney(values.dailyOtherRevenue));
        SetText(incomeTotalValue, FormatMoney(values.todayIncome));

        SetText(salaryValue, FormatMoney(values.staffSalaryCost));
        SetText(maintenanceValue, FormatMoney(values.maintenanceCost));
        SetText(expenseTotalValue, FormatMoney(values.todayExpense));

        SetText(memoText, BuildMemo(values));

        CaptureActualHistory(values);
        RefreshGraph(values);
    }

    private void ResolveManagers()
    {
        if (walletManager == null)
        {
            walletManager = FindFirstObjectByType<WalletManager>();
        }

        if (economyManager == null)
        {
            economyManager = FindFirstObjectByType<GymEconomyManager>();
        }

        if (staffManager == null)
        {
            staffManager = FindFirstObjectByType<StaffManager>();
        }

        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }
    }

    private void ResolveTexts()
    {
        if (incomeSummaryValue == null)
        {
            incomeSummaryValue = FindText("Value", "EconomyIncomeSummaryCard");
        }

        if (expenseSummaryValue == null)
        {
            expenseSummaryValue = FindText("Value", "EconomyExpenseSummaryCard");
        }

        if (profitSummaryValue == null)
        {
            profitSummaryValue = FindText("Value", "EconomyProfitSummaryCard");
        }

        if (cashSummaryValue == null)
        {
            cashSummaryValue = FindText("Value", "EconomyCashSummaryCard");
        }

        if (membershipValue == null)
        {
            membershipValue = FindText("MembershipValue", "EconomyIncomeDetailBox");
        }

        if (dailyValue == null)
        {
            dailyValue = FindText("DailyValue", "EconomyIncomeDetailBox");
        }

        if (incomeTotalValue == null)
        {
            incomeTotalValue = FindText("TotalValue", "EconomyIncomeDetailBox");
        }

        if (salaryValue == null)
        {
            salaryValue = FindText("SalaryValue", "EconomyExpenseDetailBox");
        }

        if (maintenanceValue == null)
        {
            maintenanceValue = FindText("MaintenanceValue", "EconomyExpenseDetailBox");
        }

        if (expenseTotalValue == null)
        {
            expenseTotalValue = FindText("TotalValue", "EconomyExpenseDetailBox");
        }

        if (memoText == null)
        {
            memoText = FindText("MemoText", "EconomyMemoBox");
        }
    }

    private EconomyValues ReadEconomyValues()
    {
        EconomyValues values = new EconomyValues();
        FillCurrentDate(ref values);

        values.currentCash = walletManager != null ? walletManager.CurrentCash : 0;

        if (economyManager == null)
        {
            values.staffSalaryCost = GetDailyStaffSalaryFallback();
            values.todayExpense = values.staffSalaryCost;
            values.maintenanceCost = 0;
            values.todayIncome = 0;
            values.todayNet = -values.todayExpense;
            return values;
        }

        int membershipRevenue = ReadReportInt("membershipRevenue", 0);
        int joiningRevenue = ReadReportInt("joiningRevenue", 0);
        int ptRevenue = ReadReportInt("ptGymRevenue", 0);
        int ancillaryRevenue = ReadReportInt("ancillaryRevenue", 0);
        int variableCost = ReadReportInt("variableCost", economyManager.GetDailyVariableCost());
        int netRevenue = ReadReportInt("netRevenue", economyManager.GetPreviewDailyNetRevenue());
        int trainerBaseWage = ReadReportInt("trainerBaseWage", 0);
        int equipmentOperatingCost = ReadReportInt("equipmentOperatingCost", 0);
        int consumableCost = ReadReportInt("consumableCost", 0);
        int serviceCost = ReadReportInt("serviceCost", 0);

        int publicIncome =
            economyManager.GetDailyMembershipRevenue() +
            economyManager.GetDailyPtRevenue() +
            economyManager.GetDailyAncillaryRevenue();

        values.membershipRevenue = membershipRevenue + joiningRevenue;
        if (values.membershipRevenue <= 0)
        {
            values.membershipRevenue = economyManager.GetDailyMembershipRevenue();
        }

        values.dailyOtherRevenue = ptRevenue + ancillaryRevenue;
        if (values.dailyOtherRevenue <= 0)
        {
            values.dailyOtherRevenue = economyManager.GetDailyPtRevenue() + economyManager.GetDailyAncillaryRevenue();
        }

        values.todayIncome = values.membershipRevenue + values.dailyOtherRevenue;
        if (values.todayIncome <= 0)
        {
            values.todayIncome = publicIncome;
        }

        values.todayExpense = Mathf.Max(0, variableCost);
        values.todayNet = netRevenue;

        values.staffSalaryCost = Mathf.Max(0, trainerBaseWage);
        if (values.staffSalaryCost <= 0)
        {
            values.staffSalaryCost = Mathf.Min(values.todayExpense, GetDailyStaffSalaryFallback());
        }

        int calculatedMaintenance = equipmentOperatingCost + consumableCost + serviceCost;
        if (calculatedMaintenance <= 0)
        {
            calculatedMaintenance = Mathf.Max(0, values.todayExpense - values.staffSalaryCost);
        }

        values.maintenanceCost = Mathf.Max(0, calculatedMaintenance);

        if (values.staffSalaryCost + values.maintenanceCost != values.todayExpense)
        {
            values.maintenanceCost = Mathf.Max(0, values.todayExpense - values.staffSalaryCost);
        }

        return values;
    }

    private void FillCurrentDate(ref EconomyValues values)
    {
        if (timeManager != null)
        {
            values.year = Mathf.Max(1, timeManager.CurrentYear);
            values.month = Mathf.Clamp(timeManager.CurrentMonth, 1, 12);
            values.day = Mathf.Clamp(timeManager.CurrentDay, 1, Mathf.Max(1, timeManager.DaysPerMonth));
            return;
        }

        values.year = 1;
        values.month = 1;
        values.day = 1;
    }

    private int ReadReportInt(string fieldName, int fallback)
    {
        return ReadReportIntFromReport("previewReport", fieldName, fallback);
    }

    private int ReadReportIntFromReport(string reportFieldName, string valueFieldName, int fallback)
    {
        if (economyManager == null || string.IsNullOrEmpty(reportFieldName) || string.IsNullOrEmpty(valueFieldName))
        {
            return fallback;
        }

        FieldInfo reportField = typeof(GymEconomyManager).GetField(reportFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (reportField == null)
        {
            return fallback;
        }

        object report = reportField.GetValue(economyManager);
        if (report == null)
        {
            return fallback;
        }

        FieldInfo valueField = report.GetType().GetField(valueFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueField == null)
        {
            return fallback;
        }

        object value = valueField.GetValue(report);
        if (value is int intValue)
        {
            return intValue;
        }

        return fallback;
    }

    private bool TryReadReportSnapshot(string reportFieldName, out EconomyHistoryRecord record)
    {
        record = default;

        if (economyManager == null || string.IsNullOrEmpty(reportFieldName))
        {
            return false;
        }

        FieldInfo reportField = typeof(GymEconomyManager).GetField(reportFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (reportField == null)
        {
            return false;
        }

        object report = reportField.GetValue(economyManager);
        if (report == null)
        {
            return false;
        }

        int year = ReadIntField(report, "year", 0);
        int month = ReadIntField(report, "month", 0);
        int day = ReadIntField(report, "day", 0);
        if (year <= 0 || month <= 0 || day <= 0)
        {
            return false;
        }

        int membershipRevenue = ReadIntField(report, "membershipRevenue", 0);
        int joiningRevenue = ReadIntField(report, "joiningRevenue", 0);
        int ptGymRevenue = ReadIntField(report, "ptGymRevenue", 0);
        int ancillaryRevenue = ReadIntField(report, "ancillaryRevenue", 0);
        int variableCost = ReadIntField(report, "variableCost", 0);
        int netRevenue = ReadIntField(report, "netRevenue", 0);

        record = new EconomyHistoryRecord
        {
            year = year,
            month = month,
            day = day,
            income = Mathf.Max(0, membershipRevenue + joiningRevenue + ptGymRevenue + ancillaryRevenue),
            expense = Mathf.Max(0, variableCost),
            net = netRevenue
        };

        return true;
    }

    private static int ReadIntField(object instance, string fieldName, int fallback)
    {
        if (instance == null || string.IsNullOrEmpty(fieldName))
        {
            return fallback;
        }

        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            return fallback;
        }

        object value = field.GetValue(instance);
        return value is int intValue ? intValue : fallback;
    }

    private int GetDailyStaffSalaryFallback()
    {
        if (staffManager == null)
        {
            return 0;
        }

        int monthlySalary = staffManager.GetTotalMonthlySalary();
        int days = timeManager != null ? Mathf.Max(1, timeManager.DaysPerMonth) : 30;
        return Mathf.CeilToInt(monthlySalary / (float)days);
    }

    private void CaptureActualHistory(EconomyValues values)
    {
        EnsureHistoryLoaded();

        // 오늘 표시값은 현재 GymEconomyManager previewReport에서 읽은 실제 예측/집계값이다.
        UpsertHistory(new EconomyHistoryRecord
        {
            year = values.year,
            month = values.month,
            day = values.day,
            income = values.todayIncome,
            expense = values.todayExpense,
            net = values.todayNet
        });

        // 하루가 넘어가며 실제로 반영된 마지막 결산값이 있으면 그것도 기록한다.
        if (TryReadReportSnapshot("lastAppliedReport", out EconomyHistoryRecord lastApplied))
        {
            UpsertHistory(lastApplied);
        }

        TrimHistory(90);

        if (Application.isPlaying)
        {
            SaveHistory();
        }
    }

    private void RefreshGraph(EconomyValues currentValues)
    {
        EnsureHistoryLoaded();
        BuildLastSevenSamples(currentValues);

        Transform chartBox = FindDeepChild(transform, "EconomyChartBox");
        if (chartBox == null)
        {
            return;
        }

        float maxIncome = 0f;
        for (int i = 0; i < chartSamples.Count; i++)
        {
            if (chartSamples[i].hasData)
            {
                maxIncome = Mathf.Max(maxIncome, chartSamples[i].income);
            }
        }

        maxIncome = Mathf.Max(1f, maxIncome);

        float baselineY = ResolveChartBaselineY(chartBox);
        float maxBarHeight = ResolveMaxBarHeight(chartBox);

        for (int i = 0; i < 7; i++)
        {
            EconomyGraphSample sample = i < chartSamples.Count ? chartSamples[i] : default;

            Transform bar = FindDirectChild(chartBox, $"Bar_{i}");
            if (bar != null)
            {
                RectTransform barRect = bar.GetComponent<RectTransform>();
                if (barRect != null)
                {
                    if (sample.hasData)
                    {
                        float ratio = Mathf.Clamp01(sample.income / maxIncome);
                        float height = Mathf.Max(8f, Mathf.Round(maxBarHeight * ratio));
                        Vector2 size = barRect.sizeDelta;
                        size.y = height;
                        barRect.sizeDelta = size;
                        barRect.anchoredPosition = new Vector2(barRect.anchoredPosition.x, baselineY + height * 0.5f);
                        if (!bar.gameObject.activeSelf)
                        {
                            bar.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        if (bar.gameObject.activeSelf)
                        {
                            bar.gameObject.SetActive(false);
                        }
                    }
                }
            }

            Transform date = FindDirectChild(chartBox, $"Date_{i}");
            if (date != null)
            {
                Text dateText = date.GetComponent<Text>();
                if (dateText != null)
                {
                    dateText.text = $"{sample.month}/{sample.day}";
                }
            }
        }
    }

    private void BuildLastSevenSamples(EconomyValues currentValues)
    {
        chartSamples.Clear();

        int daysPerMonth = timeManager != null ? Mathf.Max(1, timeManager.DaysPerMonth) : 30;
        int year = currentValues.year;
        int month = currentValues.month;
        int day = currentValues.day;

        DateTriple start = new DateTriple(year, month, day);
        for (int i = 6; i >= 1; i--)
        {
            start = PreviousDate(start, daysPerMonth);
        }

        DateTriple cursor = start;
        for (int i = 0; i < 7; i++)
        {
            EconomyGraphSample sample = new EconomyGraphSample
            {
                year = cursor.year,
                month = cursor.month,
                day = cursor.day,
                hasData = TryFindHistory(cursor.year, cursor.month, cursor.day, out EconomyHistoryRecord record),
                income = 0,
                expense = 0,
                net = 0
            };

            if (sample.hasData)
            {
                sample.income = record.income;
                sample.expense = record.expense;
                sample.net = record.net;
            }

            chartSamples.Add(sample);
            cursor = NextDate(cursor, daysPerMonth);
        }
    }

    private float ResolveChartBaselineY(Transform chartBox)
    {
        Transform axis = FindDirectChild(chartBox, "Axis");
        if (axis != null)
        {
            RectTransform axisRect = axis.GetComponent<RectTransform>();
            if (axisRect != null)
            {
                return axisRect.anchoredPosition.y;
            }
        }

        return DefaultChartBaselineY;
    }

    private float ResolveMaxBarHeight(Transform chartBox)
    {
        float max = 0f;
        for (int i = 0; i < 7; i++)
        {
            Transform bar = FindDirectChild(chartBox, $"Bar_{i}");
            if (bar == null)
            {
                continue;
            }

            RectTransform rect = bar.GetComponent<RectTransform>();
            if (rect != null)
            {
                max = Mathf.Max(max, rect.sizeDelta.y);
            }
        }

        return max > 20f ? max : DefaultMaxBarHeight;
    }

    private void EnsureHistoryLoaded()
    {
        if (historyLoaded)
        {
            return;
        }

        historyLoaded = true;
        loadedHistory = new EconomyHistoryFile();
        loadedHistory.records = new List<EconomyHistoryRecord>();

        string path = GetHistoryPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            EconomyHistoryFile file = JsonUtility.FromJson<EconomyHistoryFile>(json);
            if (file != null && file.records != null)
            {
                loadedHistory = file;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EconomyPanelDataBinder] 경제 그래프 기록 로드 실패: {path}\n{e}", this);
            loadedHistory.records = new List<EconomyHistoryRecord>();
        }
    }

    private void SaveHistory()
    {
        string path = GetHistoryPath();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(loadedHistory, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EconomyPanelDataBinder] 경제 그래프 기록 저장 실패: {path}\n{e}", this);
        }
    }

    private string GetHistoryPath()
    {
        if (string.IsNullOrEmpty(Application.persistentDataPath))
        {
            return null;
        }

        return Path.Combine(Application.persistentDataPath, "economy_ui_daily_graph_history.json");
    }

    private void UpsertHistory(EconomyHistoryRecord record)
    {
        if (record.year <= 0 || record.month <= 0 || record.day <= 0)
        {
            return;
        }

        if (loadedHistory.records == null)
        {
            loadedHistory.records = new List<EconomyHistoryRecord>();
        }

        for (int i = 0; i < loadedHistory.records.Count; i++)
        {
            EconomyHistoryRecord existing = loadedHistory.records[i];
            if (existing.year == record.year && existing.month == record.month && existing.day == record.day)
            {
                loadedHistory.records[i] = record;
                return;
            }
        }

        loadedHistory.records.Add(record);
    }

    private bool TryFindHistory(int year, int month, int day, out EconomyHistoryRecord record)
    {
        record = default;
        if (loadedHistory.records == null)
        {
            return false;
        }

        for (int i = 0; i < loadedHistory.records.Count; i++)
        {
            EconomyHistoryRecord candidate = loadedHistory.records[i];
            if (candidate.year == year && candidate.month == month && candidate.day == day)
            {
                record = candidate;
                return true;
            }
        }

        return false;
    }

    private void TrimHistory(int maxRecords)
    {
        if (loadedHistory.records == null || loadedHistory.records.Count <= maxRecords)
        {
            return;
        }

        loadedHistory.records.Sort((a, b) => CompareDate(a.year, a.month, a.day, b.year, b.month, b.day));
        int removeCount = loadedHistory.records.Count - maxRecords;
        loadedHistory.records.RemoveRange(0, removeCount);
    }

    private static int CompareDate(int ay, int am, int ad, int by, int bm, int bd)
    {
        if (ay != by) return ay.CompareTo(by);
        if (am != bm) return am.CompareTo(bm);
        return ad.CompareTo(bd);
    }

    private static DateTriple PreviousDate(DateTriple date, int daysPerMonth)
    {
        date.day -= 1;
        if (date.day >= 1)
        {
            return date;
        }

        date.month -= 1;
        if (date.month < 1)
        {
            date.month = 12;
            date.year = Mathf.Max(1, date.year - 1);
        }

        date.day = Mathf.Max(1, daysPerMonth);
        return date;
    }

    private static DateTriple NextDate(DateTriple date, int daysPerMonth)
    {
        date.day += 1;
        if (date.day <= Mathf.Max(1, daysPerMonth))
        {
            return date;
        }

        date.day = 1;
        date.month += 1;
        if (date.month > 12)
        {
            date.month = 1;
            date.year += 1;
        }

        return date;
    }

    private Text FindText(string objectName, string parentName)
    {
        Transform found = FindDeepChild(transform, objectName, parentName);
        return found != null ? found.GetComponent<Text>() : null;
    }

    private static Transform FindDeepChild(Transform root, string objectName, string parentName = null)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        if (root.name == objectName && (string.IsNullOrEmpty(parentName) || HasParentNamed(root, parentName)))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName, parentName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
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

    private static void SetText(Text text, string value)
    {
        if (text != null && text.text != value)
        {
            text.text = value;
        }
    }

    private static string FormatMoney(int value)
    {
        return $"{Mathf.Max(0, value):N0}G";
    }

    private static string FormatSignedMoney(int value)
    {
        return value >= 0 ? $"+{value:N0}G" : $"-{Mathf.Abs(value):N0}G";
    }

    private static string BuildMemo(EconomyValues values)
    {
        if (values.todayIncome <= 0 && values.todayExpense <= 0)
        {
            return "경제 데이터가\n아직 집계되지\n않았어요.";
        }

        if (values.todayNet < 0)
        {
            return "지출이 수익보다\n높아요. 운영 상태를\n확인해 주세요!";
        }

        if (values.todayNet == 0)
        {
            return "수익과 지출이\n비슷해요. 추가 수익을\n노려보세요!";
        }

        return "회원 증가로\n수익이 안정적으로\n오르고 있어요!";
    }

    [Serializable]
    private sealed class EconomyHistoryFile
    {
        public List<EconomyHistoryRecord> records;
    }

    [Serializable]
    private struct EconomyHistoryRecord
    {
        public int year;
        public int month;
        public int day;
        public int income;
        public int expense;
        public int net;
    }

    private struct EconomyValues
    {
        public int year;
        public int month;
        public int day;
        public int todayIncome;
        public int todayExpense;
        public int todayNet;
        public int currentCash;
        public int membershipRevenue;
        public int dailyOtherRevenue;
        public int staffSalaryCost;
        public int maintenanceCost;
    }

    private struct EconomyGraphSample
    {
        public int year;
        public int month;
        public int day;
        public bool hasData;
        public int income;
        public int expense;
        public int net;
    }

    private struct DateTriple
    {
        public int year;
        public int month;
        public int day;

        public DateTriple(int year, int month, int day)
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }
    }
}
