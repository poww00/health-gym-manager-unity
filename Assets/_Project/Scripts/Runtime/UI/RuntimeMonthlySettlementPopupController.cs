using UnityEngine;
using UnityEngine.UI;

public partial class RuntimeGameUIController
{
    private const string MonthlySummaryCardSprite = "GeneratedRuntimeUI/ui_v2/monthly_settlement/monthly_summary_card_base";
    private const string MonthlyBreakdownPanelSprite = "GeneratedRuntimeUI/ui_v2/monthly_settlement/monthly_breakdown_panel_base";
    private const string MonthlyCommentBubbleSprite = "GeneratedRuntimeUI/ui_v2/monthly_settlement/monthly_comment_bubble_base";

    private Transform monthlySettlementPopupRoot;
    private MonthlySettlementManager monthlySettlementManager;
    private MonthlySettlementManager boundMonthlySettlementManager;

    private Text monthlyIncomeValueText;
    private Text monthlyExpenseValueText;
    private Text monthlyNetValueText;
    private Text monthlySubtitleText;
    private Text monthlyCommentText;
    private readonly Text[] monthlyIncomeRowValues = new Text[3];
    private readonly Text[] monthlyExpenseRowValues = new Text[4];

    private readonly Color monthlyPositiveColor = new Color(0.20f, 0.56f, 0.18f, 1f);
    private readonly Color monthlyNegativeColor = new Color(0.70f, 0.20f, 0.16f, 1f);

    private void BindMonthlySettlementPopupEvents()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveMonthlySettlementReferences();
        EnsureMonthlySettlementPopup();
        HideRuntimeMenuPopup(monthlySettlementPopupRoot);

        if (boundMonthlySettlementManager == monthlySettlementManager)
        {
            return;
        }

        UnbindMonthlySettlementPopupEvents();
        boundMonthlySettlementManager = monthlySettlementManager;

        if (boundMonthlySettlementManager != null)
        {
            boundMonthlySettlementManager.SettlementCompleted += HandleMonthlySettlementCompleted;
        }
    }

    private void UnbindMonthlySettlementPopupEvents()
    {
        if (boundMonthlySettlementManager != null)
        {
            boundMonthlySettlementManager.SettlementCompleted -= HandleMonthlySettlementCompleted;
            boundMonthlySettlementManager = null;
        }
    }

    private void HandleMonthlySettlementCompleted()
    {
        OpenMonthlySettlementPopup();
    }

#if UNITY_EDITOR
    public void PreviewMonthlySettlementPopupForEditMode()
    {
        if (Application.isPlaying)
        {
            OpenMonthlySettlementPopup();
            return;
        }

        EnsureMenuPopupPreviewRoot();
        DestroyRuntimeMenuPopup(ref menuPopupRoot, "RuntimeGameMenuPopupRoot");
        DestroyRuntimeMenuPopup(ref relocationPopupRoot, "RuntimeRelocationPopupRoot");
        DestroyRuntimeMenuPopup(ref settingsPopupRoot, "RuntimeSettingsPopupRoot");

        monthlySettlementPopupRoot = FindSavedMonthlySettlementPopupRoot();
        EnsureMonthlySettlementPopup();
        RefreshMonthlySettlementPopup();
        ShowRuntimeMenuPopup(monthlySettlementPopupRoot);
    }

    public void CloseAllRuntimePopupPreviewsForEditMode()
    {
        if (Application.isPlaying)
        {
            CloseRuntimeMenuPopups();
            CloseMonthlySettlementPopup();
            return;
        }

        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        DestroyRuntimeMenuPopup(ref menuPopupRoot, "RuntimeGameMenuPopupRoot");
        DestroyRuntimeMenuPopup(ref relocationPopupRoot, "RuntimeRelocationPopupRoot");
        DestroyRuntimeMenuPopup(ref settingsPopupRoot, "RuntimeSettingsPopupRoot");
        DestroyRuntimeMenuPopup(ref monthlySettlementPopupRoot, "RuntimeMonthlySettlementPopupRoot");
    }
#endif

    private void OpenMonthlySettlementPopup()
    {
        EnsureMonthlySettlementPopup();
        RefreshMonthlySettlementPopup();

        HideRuntimeMenuPopup(menuPopupRoot);
        HideRuntimeMenuPopup(relocationPopupRoot);
        HideRuntimeMenuPopup(settingsPopupRoot);
        ShowRuntimeMenuPopup(monthlySettlementPopupRoot);

        CacheMenuManager();
        if (inGameMenuManager != null)
        {
            inGameMenuManager.SetMenuOpen(true);
        }
        else if (timeManager != null)
        {
            timeManager.SetPaused(true);
        }

        HideToast();
    }

    private void CloseMonthlySettlementPopup()
    {
        HideRuntimeMenuPopup(monthlySettlementPopupRoot);

        CacheMenuManager();
        if (inGameMenuManager != null)
        {
            inGameMenuManager.SetMenuOpen(false);
        }
        else if (timeManager != null)
        {
            timeManager.SetPaused(false);
        }
    }

    private void EnsureMonthlySettlementPopup()
    {
        if (runtimeRoot == null)
        {
            Transform existingRoot = transform.Find("RuntimeGameUIRoot");
            if (existingRoot != null)
            {
                runtimeRoot = existingRoot;
            }
        }

        if (runtimeRoot == null)
        {
            return;
        }

        if (monthlySettlementPopupRoot == null)
        {
            monthlySettlementPopupRoot = FindSavedMonthlySettlementPopupRoot();
            if (monthlySettlementPopupRoot != null)
            {
                BindExistingMonthlySettlementPopup();
            }
            else
            {
                BuildMonthlySettlementPopup();
            }
        }
    }

    private void BuildMonthlySettlementPopup()
    {
        monthlySettlementPopupRoot = CreatePopupRoot("RuntimeMonthlySettlementPopupRoot");
        GameObject frame = CreateGeneratedImage(monthlySettlementPopupRoot, "MonthlySettlementFrame", MenuPanelSprite, 0f, 0f, menuWindowSize.x, menuWindowSize.y, false, true);

        CreateText(frame.transform, "MonthlySettlementTitle", "월말 결산", 47, theme.Ink, TextAnchor.MiddleCenter, 0f, menuTitleY, 430f, 62f, true);
        monthlySubtitleText = CreateText(frame.transform, "MonthlySettlementSubtitle", "", 27, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 380f, 520f, 34f, true);
        monthlySubtitleText.resizeTextForBestFit = true;
        monthlySubtitleText.resizeTextMinSize = 21;
        monthlySubtitleText.resizeTextMaxSize = 27;

        GameObject closeNode = CreateGeneratedImage(frame.transform, "MonthlySettlementClose", "GeneratedRuntimeUI/ui_v2/staff/staff_close_button", menuClosePosition.x, menuClosePosition.y, 86f, 86f, true, true);
        Image closeImage = closeNode.GetComponent<Image>();
        closeImage.raycastTarget = true;
        Button closeButton = closeNode.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(CloseMonthlySettlementPopup);

        monthlyIncomeValueText = CreateMonthlySummaryCard(frame.transform, "MonthlyIncomeCard", -220f, 258f, "총수입", monthlyPositiveColor);
        monthlyExpenseValueText = CreateMonthlySummaryCard(frame.transform, "MonthlyExpenseCard", 0f, 258f, "총지출", monthlyNegativeColor);
        monthlyNetValueText = CreateMonthlySummaryCard(frame.transform, "MonthlyNetCard", 220f, 258f, "순이익", monthlyPositiveColor);

        GameObject breakdownPanel = CreateGeneratedImage(frame.transform, "MonthlyBreakdownPanel", MonthlyBreakdownPanelSprite, 0f, -20f, 650f, 380f, false, true);
        CreateSolid(breakdownPanel.transform, "IncomeHeaderDot", monthlyPositiveColor, -285f, 130f, 15f, 15f, true);
        CreateText(breakdownPanel.transform, "IncomeHeader", "수입 내역", 31, theme.Ink, TextAnchor.MiddleLeft, -150f, 128f, 230f, 42f, true);
        CreateSolid(breakdownPanel.transform, "ExpenseHeaderDot", monthlyNegativeColor, 35f, 130f, 15f, 15f, true);
        CreateText(breakdownPanel.transform, "ExpenseHeader", "지출 내역", 31, theme.Ink, TextAnchor.MiddleLeft, 170f, 130f, 230f, 42f, true);
        CreateMonthlyBreakdownDivider(breakdownPanel.transform, 0f, 6f, 260f);

        monthlyIncomeRowValues[0] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "MembershipIncome", -164f, 70f, "회원권 수익", monthlyPositiveColor, "GeneratedRuntimeUI/ui_v2/economy/economy_icon_income_up");
        monthlyIncomeRowValues[1] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "PtIncome", -164f, 10f, "PT 수익", monthlyPositiveColor, "GeneratedRuntimeUI/ui_v2/review/icon_review_like");
        monthlyIncomeRowValues[2] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "AncillaryIncome", -164f, -50f, "기타 수익", monthlyPositiveColor, "GeneratedRuntimeUI/ui_v2/economy/economy_icon_money_bag");

        monthlyExpenseRowValues[0] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "RentExpense", 164f, 70f, "월세", monthlyNegativeColor, "GeneratedRuntimeUI/ui_v2/economy/economy_icon_expense_down");
        monthlyExpenseRowValues[1] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "LaborExpense", 164f, 10f, "직원 급여", monthlyNegativeColor, "GeneratedRuntimeUI/ui_v2/staff/portraits/male/staff_male_00");
        monthlyExpenseRowValues[2] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "MaintenanceExpense", 164f, -50f, "유지비", monthlyNegativeColor, "GeneratedRuntimeUI/ui_v2/monthly_settlement/icon_monthly_maintenance");
        monthlyExpenseRowValues[3] = CreateMonthlyBreakdownRow(breakdownPanel.transform, "VariableExpense", 164f, -110f, "전기세/관리비", monthlyNegativeColor, "GeneratedRuntimeUI/ui_v2/monthly_settlement/icon_monthly_electricity");

        CreateGeneratedImage(frame.transform, "MonthlyCommentPortrait", "GeneratedRuntimeUI/ui_v2/economy/economy_manager_character", -262f, -308f, 116f, 155f, true, true);
        GameObject commentBubble = CreateGeneratedImage(frame.transform, "MonthlyCommentBubble", MonthlyCommentBubbleSprite, 70f, -310f, 500f, 118f, true, true);
        monthlyCommentText = CreateText(commentBubble.transform, "MonthlyCommentText", "", 27, theme.Ink, TextAnchor.MiddleLeft, 15f, 0f, 390f, 76f, true);
        monthlyCommentText.resizeTextForBestFit = true;
        monthlyCommentText.resizeTextMinSize = 22;
        monthlyCommentText.resizeTextMaxSize = 27;

        Button confirmButton = CreateSpriteButton(frame.transform, "MonthlyConfirmButton", MenuBeigeButtonSprite, "확인", -150f, -426f, 250f, 78f, theme.Ink, out Text confirmLabel, 32);
        confirmLabel.fontSize = 34;
        confirmButton.onClick.AddListener(CloseMonthlySettlementPopup);

        Button nextMonthButton = CreateSpriteButton(frame.transform, "MonthlyNextMonthButton", MenuGreenButtonSprite, "다음 달 시작", 150f, -426f, 250f, 78f, theme.BrightInk, out Text nextLabel, 29);
        nextLabel.fontSize = 31;
        nextMonthButton.onClick.AddListener(CloseMonthlySettlementPopup);

        SetRuntimeMenuTextNormal(monthlySettlementPopupRoot);
        monthlySettlementPopupRoot.gameObject.SetActive(false);
    }

    private Transform FindSavedMonthlySettlementPopupRoot()
    {
        if (runtimeRoot == null)
        {
            Transform existingRoot = transform.Find("RuntimeGameUIRoot");
            if (existingRoot != null)
            {
                runtimeRoot = existingRoot;
            }
        }

        return runtimeRoot != null ? runtimeRoot.Find("RuntimeMonthlySettlementPopupRoot") : null;
    }

    private void BindExistingMonthlySettlementPopup()
    {
        if (monthlySettlementPopupRoot == null)
        {
            return;
        }

        monthlySubtitleText = FindMonthlyText("MonthlySettlementSubtitle");
        monthlyIncomeValueText = FindMonthlyText("Value", "MonthlyIncomeCard");
        monthlyExpenseValueText = FindMonthlyText("Value", "MonthlyExpenseCard");
        monthlyNetValueText = FindMonthlyText("Value", "MonthlyNetCard");
        monthlyIncomeRowValues[0] = FindMonthlyText("MembershipIncome_Value");
        monthlyIncomeRowValues[1] = FindMonthlyText("PtIncome_Value");
        monthlyIncomeRowValues[2] = FindMonthlyText("AncillaryIncome_Value");
        monthlyExpenseRowValues[0] = FindMonthlyText("RentExpense_Value");
        monthlyExpenseRowValues[1] = FindMonthlyText("LaborExpense_Value");
        monthlyExpenseRowValues[2] = FindMonthlyText("MaintenanceExpense_Value");
        monthlyExpenseRowValues[3] = FindMonthlyText("VariableExpense_Value");
        monthlyCommentText = FindMonthlyText("MonthlyCommentText");

        AssignMonthlyImage("MaintenanceExpense_Icon", "GeneratedRuntimeUI/ui_v2/monthly_settlement/icon_monthly_maintenance");
        AssignMonthlyImage("VariableExpense_Icon", "GeneratedRuntimeUI/ui_v2/monthly_settlement/icon_monthly_electricity");

        BindMonthlyButton("MonthlySettlementClose", CloseMonthlySettlementPopup);
        BindMonthlyButton("MonthlyConfirmButton", CloseMonthlySettlementPopup);
        BindMonthlyButton("MonthlyNextMonthButton", CloseMonthlySettlementPopup);
        SetRuntimeMenuTextNormal(monthlySettlementPopupRoot);
    }

    private Text FindMonthlyText(string objectName, string parentName = null)
    {
        Transform target = FindDeepChild(monthlySettlementPopupRoot, objectName, parentName);
        return target != null ? target.GetComponent<Text>() : null;
    }

    private void AssignMonthlyImage(string objectName, string spritePath)
    {
        Transform target = FindDeepChild(monthlySettlementPopupRoot, objectName);
        Image image = target != null ? target.GetComponent<Image>() : null;
        if (image != null)
        {
            GeneratedRuntimeSprites.Assign(image, spritePath, true);
        }
    }

    private void BindMonthlyButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        Transform target = FindDeepChild(monthlySettlementPopupRoot, objectName);
        if (target == null)
        {
            return;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            button = target.gameObject.AddComponent<Button>();
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private Text CreateMonthlySummaryCard(Transform parent, string name, float x, float y, string label, Color valueColor)
    {
        GameObject card = CreateGeneratedImage(parent, name, MonthlySummaryCardSprite, x, y, 196f, 164f, false, true);
        Text labelText = CreateText(card.transform, "Label", label, 28, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 38f, 150f, 34f, true);
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 24;
        labelText.resizeTextMaxSize = 28;

        Text valueText = CreateText(card.transform, "Value", "0G", 33, valueColor, TextAnchor.MiddleCenter, 0f, -24f, 156f, 48f, true);
        valueText.resizeTextForBestFit = true;
        valueText.resizeTextMinSize = 25;
        valueText.resizeTextMaxSize = 33;
        return valueText;
    }

    private Text CreateMonthlyBreakdownRow(Transform parent, string name, float x, float y, string label, Color valueColor, string iconPath)
    {
        if (!string.IsNullOrWhiteSpace(iconPath))
        {
            float iconX = x < 0f ? x - 121f : x - 129f;
            CreateGeneratedImage(parent, $"{name}_Icon", iconPath, iconX, y, 34f, 34f, true, true);
        }
        else
        {
            CreateSolid(parent, $"{name}_Dot", valueColor, x - 116f, y + 1f, 12f, 12f, true);
        }

        float labelX = x < 0f ? x - 26f : x - 34f;
        Text labelText = CreateText(parent, $"{name}_Label", label, 26, theme.Ink, TextAnchor.MiddleLeft, labelX, y, 150f, 36f, true);
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 21;
        labelText.resizeTextMaxSize = 26;

        Text value = CreateText(parent, $"{name}_Value", "0G", 25, valueColor, TextAnchor.MiddleRight, x + 86f, y, 136f, 36f, true);
        value.resizeTextForBestFit = true;
        value.resizeTextMinSize = 20;
        value.resizeTextMaxSize = 25;
        return value;
    }

    private void CreateMonthlyBreakdownDivider(Transform parent, float x, float y, float height)
    {
        const float dashHeight = 18f;
        const float gap = 10f;
        int dashCount = Mathf.Max(1, Mathf.FloorToInt((height + gap) / (dashHeight + gap)));
        float totalHeight = (dashCount * dashHeight) + ((dashCount - 1) * gap);
        float startY = y + (totalHeight * 0.5f) - (dashHeight * 0.5f);
        Color dashColor = new Color(0.42f, 0.28f, 0.10f, 0.45f);

        for (int i = 0; i < dashCount; i++)
        {
            CreateSolid(parent, "MonthlyBreakdownDividerDash_" + i, dashColor, x, startY - (i * (dashHeight + gap)), 3f, dashHeight, true);
        }
    }

    private void RefreshMonthlySettlementPopup()
    {
        ResolveMonthlySettlementReferences();

        MonthlySettlementManager.MonthlySettlementPopupData data = monthlySettlementManager != null
            ? monthlySettlementManager.GetMonthlySettlementPopupData()
            : CreateFallbackMonthlySettlementData();

        SetText(monthlySubtitleText, $"{data.monthNumber}월 · {data.locationLabel}");
        SetText(monthlyIncomeValueText, FormatMonthlyCurrency(data.totalIncome));
        SetText(monthlyExpenseValueText, FormatMonthlyCurrency(data.totalExpense));
        SetText(monthlyNetValueText, FormatMonthlyCurrency(data.netProfit));

        if (monthlyNetValueText != null)
        {
            monthlyNetValueText.color = data.netProfit >= 0 ? monthlyPositiveColor : monthlyNegativeColor;
        }

        SetText(monthlyIncomeRowValues[0], FormatMonthlyCurrency(data.membershipIncome));
        SetText(monthlyIncomeRowValues[1], FormatMonthlyCurrency(data.ptIncome));
        SetText(monthlyIncomeRowValues[2], FormatMonthlyCurrency(data.ancillaryIncome));

        SetText(monthlyExpenseRowValues[0], FormatMonthlyCurrency(data.rentExpense));
        SetText(monthlyExpenseRowValues[1], FormatMonthlyCurrency(data.laborExpense));
        SetText(monthlyExpenseRowValues[2], FormatMonthlyCurrency(data.maintenanceExpense));
        SetText(monthlyExpenseRowValues[3], FormatMonthlyCurrency(data.variableExpense));

        SetText(monthlyCommentText, data.comment);
    }

    private void ResolveMonthlySettlementReferences()
    {
        if (monthlySettlementManager == null)
        {
            monthlySettlementManager = FindFirstObjectByType<MonthlySettlementManager>();
        }
    }

    private MonthlySettlementManager.MonthlySettlementPopupData CreateFallbackMonthlySettlementData()
    {
        return new MonthlySettlementManager.MonthlySettlementPopupData
        {
            monthNumber = timeManager != null ? Mathf.Max(1, timeManager.CurrentMonth) : 1,
            locationLabel = "동네 헬스장",
            totalIncome = 0,
            totalExpense = 0,
            netProfit = 0,
            membershipIncome = 0,
            ptIncome = 0,
            ancillaryIncome = 0,
            rentExpense = 0,
            laborExpense = 0,
            maintenanceExpense = 0,
            variableExpense = 0,
            activeMembers = 0,
            placedObjectCount = 0,
            settlementWasCapped = false,
            usesProjectedOperatingData = true,
            comment = "월말 결산 데이터를 불러오는 중입니다."
        };
    }

    private static string FormatMonthlyCurrency(int value)
    {
        return $"{value:N0}G";
    }
}
