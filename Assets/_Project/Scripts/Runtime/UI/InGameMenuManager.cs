using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenuManager : MonoBehaviour
{
    public static bool IsMenuOpen { get; private set; }

    [Header("References")]
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private RelocationManager relocationManager;

    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("Menu")]
    [SerializeField] private bool showMenuButton = false; // UGUI 메뉴 버튼 사용 중 - OnGUI 버튼 비활성화

    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle infoStyle;
    private GUIStyle centeredInfoStyle;
    private GUIStyle dimBackgroundStyle;

    private bool showRelocationConfirmPanel = false;
    private Vector2 menuScrollPosition = Vector2.zero;

    private void Start()
    {
        CacheReferences();
        showMenuButton = false;
    }

    private void CacheReferences()
    {
        if (saveManager == null)
        {
            saveManager = FindFirstObjectByType<SaveManager>();
        }

        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }

        if (relocationManager == null)
        {
            relocationManager = FindFirstObjectByType<RelocationManager>();
        }
    }

    public void SetMenuOpen(bool open)
    {
        IsMenuOpen = open;

        if (!open)
        {
            showRelocationConfirmPanel = false;
            menuScrollPosition = Vector2.zero;
        }

        if (timeManager != null)
        {
            timeManager.SetPaused(open);
        }
    }

    public void SaveManualSlot(int slot)
    {
        CacheReferences();
        saveManager?.SaveManualSave(slot);
    }

    public void ReturnToTitleScene()
    {
        GoToTitle();
    }

    public string GetCurrentSiteLabelText()
    {
        CacheReferences();
        return relocationManager != null ? relocationManager.GetCurrentSiteLabel() : "N/A";
    }

    public string GetSelectedTargetLocationLabelText()
    {
        CacheReferences();
        return relocationManager != null ? relocationManager.GetSelectedTargetLocationLabel() : "N/A";
    }

    public string GetSelectedTargetLocationSummaryText()
    {
        CacheReferences();
        return relocationManager != null ? relocationManager.GetSelectedTargetLocationSummary() : "N/A";
    }

    public bool StepTargetLocationSelectionBy(int direction)
    {
        CacheReferences();
        return relocationManager != null && relocationManager.StepTargetLocationSelection(direction);
    }

    public bool TryGetCurrentRelocationQuote(out RelocationManager.RelocationQuote quote)
    {
        CacheReferences();

        if (relocationManager != null)
        {
            return relocationManager.TryGetNextRelocationQuote(out quote);
        }

        quote = default;
        quote.failReason = "RelocationManager missing";
        return false;
    }

    public bool ExecuteCurrentRelocation()
    {
        CacheReferences();
        return relocationManager != null && relocationManager.ExecuteNextRelocation();
    }

    private void GoToTitle()
    {
        if (saveManager != null)
        {
            saveManager.SaveAutoSave("타이틀로 이동");
        }

        SetMenuOpen(false);
        SceneManager.LoadScene(titleSceneName);
    }

    private void OnGUI()
    {
        // [UGUI 도입] 기존 OnGUI 메뉴 버튼은 더 이상 사용하지 않으므로 무조건 차단합니다.
        CacheReferences();
        EnsureStyles();
        return;

        float menuButtonWidth = 72f;
        float menuButtonHeight = 30f;
        float menuButtonX = Screen.width - menuButtonWidth - 12f;
        float menuButtonY = 12f;

        if (!IsMenuOpen && !showMenuButton)
        {
            return;
        }

        Rect menuButtonRect = new Rect(menuButtonX, menuButtonY, menuButtonWidth, menuButtonHeight);
        ScreenUiBlocker.RegisterRect(GetInstanceID(), menuButtonRect);

        if (!IsMenuOpen)
        {
            if (!showMenuButton)
            {
                return;
            }
            if (GUI.Button(menuButtonRect, "메뉴", buttonStyle))
            {
                SetMenuOpen(true);
            }

            return;
        }

        DrawDimBackground();

        RelocationManager.RelocationQuote relocationQuote;
        bool hasRelocationQuote = false;

        if (relocationManager != null)
        {
            hasRelocationQuote = relocationManager.TryGetNextRelocationQuote(out relocationQuote);
        }
        else
        {
            relocationQuote = default;
            relocationQuote.failReason = "이사 시스템을 찾지 못했어.";
        }

        if (showRelocationConfirmPanel && !hasRelocationQuote)
        {
            showRelocationConfirmPanel = false;
        }

        float outerMargin = 12f;
        float boxWidth = Mathf.Min(Screen.width - (outerMargin * 2f), 340f);
        float boxHeight = Mathf.Min(Screen.height - 60f, 620f);
        float boxX = (Screen.width - boxWidth) * 0.5f;
        float boxY = 48f;

        Rect boxRect = new Rect(boxX, boxY, boxWidth, boxHeight);
        GUI.Box(boxRect, GUIContent.none, boxStyle);
        ScreenUiBlocker.RegisterRect(GetInstanceID(), boxRect);

        GUI.Label(
            new Rect(boxX + 12f, boxY + 10f, boxWidth - 24f, 24f),
            "게임 메뉴",
            labelStyle
        );

        float footerHeight = showRelocationConfirmPanel ? 114f : 68f;
        float contentViewportX = boxX + 12f;
        float contentViewportY = boxY + 40f;
        float contentViewportWidth = boxWidth - 24f;
        float contentViewportHeight = boxHeight - 52f - footerHeight;

        if (contentViewportHeight < 100f)
        {
            contentViewportHeight = 100f;
        }

        Rect scrollViewportRect = new Rect(
            contentViewportX,
            contentViewportY,
            contentViewportWidth,
            contentViewportHeight
        );

        float contentWidth = contentViewportWidth - 18f;
        float estimatedContentHeight = showRelocationConfirmPanel ? 520f : 300f;
        Rect scrollContentRect = new Rect(0f, 0f, contentWidth, estimatedContentHeight);

        ScreenUiBlocker.RegisterRect(GetInstanceID(), scrollViewportRect);

        menuScrollPosition = GUI.BeginScrollView(
            scrollViewportRect,
            menuScrollPosition,
            scrollContentRect
        );

        DrawScrollableMenuContent(contentWidth, relocationQuote, hasRelocationQuote);

        GUI.EndScrollView();

        DrawFixedFooter(boxRect, footerHeight, relocationQuote, hasRelocationQuote);
    }

    private void DrawScrollableMenuContent(
        float contentWidth,
        RelocationManager.RelocationQuote relocationQuote,
        bool hasRelocationQuote)
    {
        float buttonHeight = 30f;
        float gap = 8f;
        float y = 0f;

        Rect saveSlot1Rect = new Rect(0f, y, contentWidth, buttonHeight);
        if (GUI.Button(saveSlot1Rect, "슬롯 1에 저장", buttonStyle))
        {
            if (saveManager != null)
            {
                saveManager.SaveManualSave(1);
            }
        }

        y += buttonHeight + gap;

        Rect saveSlot2Rect = new Rect(0f, y, contentWidth, buttonHeight);
        if (GUI.Button(saveSlot2Rect, "슬롯 2에 저장", buttonStyle))
        {
            if (saveManager != null)
            {
                saveManager.SaveManualSave(2);
            }
        }

        y += buttonHeight + 12f;

        string currentSiteText = relocationManager != null
            ? $"현재 부지: {relocationManager.GetCurrentSiteLabel()}"
            : "현재 부지: 정보 없음";

        GUI.Label(
            new Rect(0f, y, contentWidth, 20f),
            currentSiteText,
            infoStyle
        );

        y += 24f;

        GUI.Label(
            new Rect(0f, y, contentWidth, 20f),
            "이사 목표 입지",
            infoStyle
        );

        y += 22f;

        float selectorButtonWidth = 34f;
        float selectorLabelWidth = contentWidth - (selectorButtonWidth * 2f) - 8f;

        Rect prevLocationRect = new Rect(0f, y, selectorButtonWidth, buttonHeight);
        if (GUI.Button(prevLocationRect, "<", buttonStyle))
        {
            if (relocationManager != null)
            {
                relocationManager.StepTargetLocationSelection(-1);
            }
        }

        Rect centerRect = new Rect(selectorButtonWidth + 4f, y, selectorLabelWidth, buttonHeight);
        GUI.Box(centerRect, GUIContent.none, boxStyle);

        GUI.Label(
            centerRect,
            relocationManager != null ? relocationManager.GetSelectedTargetLocationLabel() : "입지 정보 없음",
            centeredInfoStyle
        );

        Rect nextLocationRect = new Rect(selectorButtonWidth + 4f + selectorLabelWidth + 4f, y, selectorButtonWidth, buttonHeight);
        if (GUI.Button(nextLocationRect, ">", buttonStyle))
        {
            if (relocationManager != null)
            {
                relocationManager.StepTargetLocationSelection(1);
            }
        }

        y += buttonHeight + 6f;

        string targetLocationSummary = relocationManager != null
            ? relocationManager.GetSelectedTargetLocationSummary()
            : "입지 특성 정보 없음";

        GUI.Label(
            new Rect(0f, y, contentWidth, 40f),
            targetLocationSummary,
            infoStyle
        );

        y += 44f;

        string nextSiteText = hasRelocationQuote
            ? $"다음 부지: {relocationQuote.targetSiteLabel}"
            : $"다음 부지: {relocationQuote.failReason}";

        GUI.Label(
            new Rect(0f, y, contentWidth, 34f),
            nextSiteText,
            infoStyle
        );

        y += 38f;

        if (!showRelocationConfirmPanel)
        {
            GUI.enabled = hasRelocationQuote;

            Rect quoteButtonRect = new Rect(0f, y, contentWidth, buttonHeight);
            if (GUI.Button(quoteButtonRect, "이사 견적 보기", buttonStyle))
            {
                showRelocationConfirmPanel = true;
                menuScrollPosition = Vector2.zero;
            }

            GUI.enabled = true;
            y += buttonHeight + gap;
        }
        else
        {
            float quoteBoxHeight = 220f;

            Rect quoteBoxRect = new Rect(0f, y, contentWidth, quoteBoxHeight);
            GUI.Box(quoteBoxRect, GUIContent.none, boxStyle);

            GUI.Label(
                new Rect(8f, y + 8f, contentWidth - 16f, quoteBoxHeight - 16f),
                BuildRelocationQuoteText(relocationQuote),
                infoStyle
            );

            y += quoteBoxHeight + gap;
        }
    }

    private void DrawFixedFooter(
        Rect boxRect,
        float footerHeight,
        RelocationManager.RelocationQuote relocationQuote,
        bool hasRelocationQuote)
    {
        float footerX = boxRect.x + 12f;
        float footerY = boxRect.yMax - footerHeight;
        float footerWidth = boxRect.width - 24f;
        float buttonHeight = 30f;
        float gap = 8f;

        Rect footerRect = new Rect(footerX, footerY, footerWidth, footerHeight - 8f);
        ScreenUiBlocker.RegisterRect(GetInstanceID(), footerRect);

        if (showRelocationConfirmPanel)
        {
            bool canExecuteRelocation = relocationQuote.isValid && relocationQuote.shortageAmount <= 0;

            bool previousEnabled = GUI.enabled;
            GUI.enabled = canExecuteRelocation;

            Rect executeRelocationRect = new Rect(footerX, footerY, footerWidth, buttonHeight);
            if (GUI.Button(executeRelocationRect, "이사 실행", buttonStyle))
            {
                if (relocationManager != null)
                {
                    bool relocationSucceeded = relocationManager.ExecuteNextRelocation();

                    if (relocationSucceeded)
                    {
                        showRelocationConfirmPanel = false;
                        SetMenuOpen(false);
                        return;
                    }
                }
            }

            GUI.enabled = previousEnabled;

            Rect cancelRelocationRect = new Rect(footerX, footerY + buttonHeight + gap, (footerWidth * 0.5f) - 4f, buttonHeight);
            if (GUI.Button(cancelRelocationRect, "이사 취소", buttonStyle))
            {
                showRelocationConfirmPanel = false;
            }

            Rect closeMenuRect = new Rect(cancelRelocationRect.xMax + 8f, footerY + buttonHeight + gap, (footerWidth * 0.5f) - 4f, buttonHeight);
            if (GUI.Button(closeMenuRect, "닫기", buttonStyle))
            {
                SetMenuOpen(false);
            }
        }
        else
        {
            Rect goToTitleRect = new Rect(footerX, footerY, (footerWidth * 0.5f) - 4f, buttonHeight);
            if (GUI.Button(goToTitleRect, "타이틀로", buttonStyle))
            {
                GoToTitle();
            }

            Rect closeMenuRect = new Rect(goToTitleRect.xMax + 8f, footerY, (footerWidth * 0.5f) - 4f, buttonHeight);
            if (GUI.Button(closeMenuRect, "닫기", buttonStyle))
            {
                SetMenuOpen(false);
            }
        }
    }

    private void DrawDimBackground()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.28f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private static string BuildRelocationQuoteText(RelocationManager.RelocationQuote quote)
    {
        if (!quote.isValid)
        {
            return string.IsNullOrWhiteSpace(quote.failReason)
                ? "현재 이사 견적을 계산할 수 없어."
                : quote.failReason;
        }

        string affordabilityText = quote.shortageAmount > 0
            ? $"부족 금액: {quote.shortageAmount:N0}"
            : "현재 현금으로 이사 가능";

        return
            $"이동: {quote.currentSiteLabel} -> {quote.targetSiteLabel}\n" +
            $"입지 특성: {quote.targetLocationSummary}\n" +
            $"옮길 기구: {quote.placedEquipmentCount}개\n" +
            $"부지 기본 계약비: {quote.siteBaseContractFee:N0}\n" +
            $"입지 추가 계약비: {quote.locationContractSurcharge:N0}\n" +
            $"운송비: {quote.transportFeeTotal:N0} ({quote.transportFeePerEquipment:N0} x {quote.placedEquipmentCount})\n" +
            $"총 이사비: {quote.totalCost:N0}\n" +
            affordabilityText;
    }

    private void EnsureStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 15;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.wordWrap = true;
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(12, 12, 12, 12);
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 16;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = Color.white;
        }

        if (infoStyle == null)
        {
            infoStyle = new GUIStyle(GUI.skin.label);
            infoStyle.fontSize = 13;
            infoStyle.alignment = TextAnchor.UpperLeft;
            infoStyle.wordWrap = true;
            infoStyle.normal.textColor = Color.white;
        }

        if (centeredInfoStyle == null)
        {
            centeredInfoStyle = new GUIStyle(infoStyle);
            centeredInfoStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (dimBackgroundStyle == null)
        {
            dimBackgroundStyle = new GUIStyle(GUI.skin.box);
        }
    }

    private void OnDisable()
    {
        showRelocationConfirmPanel = false;
        menuScrollPosition = Vector2.zero;

        if (IsMenuOpen && timeManager != null)
        {
            timeManager.SetPaused(false);
        }

        IsMenuOpen = false;
    }

    private void OnDestroy()
    {
        showRelocationConfirmPanel = false;
        menuScrollPosition = Vector2.zero;

        if (IsMenuOpen && timeManager != null)
        {
            timeManager.SetPaused(false);
        }

        IsMenuOpen = false;
    }
}
