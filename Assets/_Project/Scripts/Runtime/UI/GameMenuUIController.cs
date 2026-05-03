using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameMenuUIController : MonoBehaviour
{
    [Header("Popup")]
    public GameObject popupPanel;
    public Button dimCloseButton;
    public Button closeButton;

    [Header("Buttons")]
    public Button saveSlot1Button;
    public Button saveSlot2Button;
    public Button titleButton;
    public Button footerCloseButton;
    public Button prevLocationButton;
    public Button nextLocationButton;
    public Button quoteToggleButton;
    public Button executeRelocationButton;
    public Button collapseQuoteButton;

    [Header("Labels")]
    public Text currentSiteText;
    public Text targetLocationText;
    public Text locationSummaryText;
    public Text nextSiteText;
    public Text quoteToggleText;
    public Text quoteDetailsText;

    [Header("States")]
    public GameObject quoteSection;

    private const string HeaderLabel = "\uac8c\uc784 \uba54\ub274";
    private const string RelocationLabel = "\uc774\uc0ac";
    private const string QuoteOpenLabel = "\uacac\uc801 \ubcf4\uae30";
    private const string QuoteCloseLabel = "\uacac\uc801 \ub2eb\uae30";
    private const string ExecuteRelocationLabel = "\uc774\uc0ac \uc9c4\ud589";
    private const string SaveSlot1Label = "\uc218\ub3d9 \uc800\uc7a5 1";
    private const string SaveSlot2Label = "\uc218\ub3d9 \uc800\uc7a5 2";
    private const string TitleButtonLabel = "\ud0c0\uc774\ud2c0\ub85c";
    private const string CloseLabel = "\ub2eb\uae30";
    private const string MenuMissingLabel = "\uba54\ub274 \uc2dc\uc2a4\ud15c\uc744 \ucc3e\uc9c0 \ubabb\ud588\uc2b5\ub2c8\ub2e4";
    private const string RelocationUnavailableLabel = "\uc774\uc0ac\ub97c \uc9c4\ud589\ud560 \uc218 \uc5c6\uc74c";
    private const string RelocationBlockedLabel = "\uc774\uc0ac \ubd88\uac00";
    private const string CurrentSitePrefix = "\ud604\uc7ac \uc9c0\uc810  ";
    private const string TargetSitePrefix = "\ubaa9\ud45c \uc9c0\uc810  ";
    private const string NextSitePrefix = "\ub2e4\uc74c \uc9c0\uc810  ";

    private GameUiTheme theme;
    private InGameMenuManager menuManager;
    private bool isQuoteExpanded;
    private bool isBuilt;
    private LayoutElement quoteSectionLayout;

    public void Configure(GameUiTheme uiTheme)
    {
        theme = uiTheme ?? GameUiTheme.CreateDefault();
    }

    public void BuildUi(Transform canvasRoot)
    {
        if (isBuilt)
        {
            return;
        }

        theme ??= GameUiTheme.CreateDefault();

        popupPanel = GameUiFactory.CreateNode(canvasRoot, "MenuModalRoot", typeof(CanvasRenderer), typeof(Image));
        Image overlay = popupPanel.GetComponent<Image>();
        overlay.color = theme.Overlay;
        overlay.raycastTarget = true;
        GameUiFactory.Stretch(popupPanel.GetComponent<RectTransform>());

        dimCloseButton = GameUiFactory.GetOrAdd<Button>(popupPanel);
        dimCloseButton.onClick.AddListener(CloseMenu);

        RectTransform shellContent;
        GameObject shell = GameUiFactory.CreatePanel(popupPanel.transform, "MenuShell", theme, theme.PanelFillAlt, out shellContent, 24f);
        GameUiFactory.SetAnchoredRect(shell.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 1040f));

        VerticalLayoutGroup shellLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(shellContent.gameObject);
        shellLayout.padding = new RectOffset(0, 0, 0, 0);
        shellLayout.spacing = 14f;
        shellLayout.childControlWidth = true;
        shellLayout.childControlHeight = false;
        shellLayout.childForceExpandWidth = true;
        shellLayout.childForceExpandHeight = false;
        shellLayout.childAlignment = TextAnchor.UpperLeft;

        GameObject headerRow = GameUiFactory.CreateNode(shellContent, "HeaderRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        GameUiFactory.GetOrAdd<LayoutElement>(headerRow).preferredHeight = 56f;

        Text header = GameUiFactory.CreateText(headerRow.transform, "Header", theme, 32, theme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement headerTextLayout = GameUiFactory.GetOrAdd<LayoutElement>(header.gameObject);
        headerTextLayout.flexibleWidth = 1f;
        headerTextLayout.preferredHeight = 56f;
        header.text = HeaderLabel;

        closeButton = GameUiFactory.CreateButton(headerRow.transform, "CloseButton", theme, "X", GameUiTone.Surface, out Text closeLabel, 6f);
        LayoutElement closeLayout = GameUiFactory.GetOrAdd<LayoutElement>(closeButton.gameObject);
        closeLayout.preferredWidth = 56f;
        closeLayout.preferredHeight = 56f;
        closeLabel.alignment = TextAnchor.MiddleCenter;

        RectTransform summaryContent;
        GameObject summaryCard = GameUiFactory.CreateCard(shellContent, "SummaryCard", theme, out summaryContent, 222f);
        VerticalLayoutGroup summaryLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(summaryContent.gameObject);
        summaryLayout.padding = new RectOffset(0, 0, 0, 0);
        summaryLayout.spacing = 8f;
        summaryLayout.childControlWidth = true;
        summaryLayout.childControlHeight = false;
        summaryLayout.childForceExpandWidth = true;
        summaryLayout.childForceExpandHeight = false;

        currentSiteText = CreateStackLabel(summaryContent, "CurrentSite", 28, theme.Ink, 36f);
        targetLocationText = CreateStackLabel(summaryContent, "TargetSite", 24, theme.AccentAlt, 34f);
        locationSummaryText = CreateStackBody(summaryContent, "LocationSummary", 20, theme.MutedInk, 104f);

        GameObject navigatorRow = GameUiFactory.CreateNode(shellContent, "NavigatorRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup navigatorLayout = navigatorRow.GetComponent<HorizontalLayoutGroup>();
        navigatorLayout.spacing = 10f;
        navigatorLayout.childControlWidth = false;
        navigatorLayout.childControlHeight = true;
        navigatorLayout.childForceExpandWidth = false;
        navigatorLayout.childForceExpandHeight = false;
        navigatorLayout.childAlignment = TextAnchor.MiddleCenter;
        GameUiFactory.GetOrAdd<LayoutElement>(navigatorRow).preferredHeight = 66f;

        prevLocationButton = GameUiFactory.CreateButton(navigatorRow.transform, "PrevLocation", theme, "<", GameUiTone.Surface, out _, 6f);
        LayoutElement prevLayout = GameUiFactory.GetOrAdd<LayoutElement>(prevLocationButton.gameObject);
        prevLayout.preferredWidth = 72f;
        prevLayout.preferredHeight = 62f;

        RectTransform navigatorContent;
        GameObject navigatorCard = GameUiFactory.CreateCard(navigatorRow.transform, "NavigatorCard", theme, out navigatorContent, 62f);
        LayoutElement navigatorCardLayout = GameUiFactory.GetOrAdd<LayoutElement>(navigatorCard);
        navigatorCardLayout.flexibleWidth = 1f;
        navigatorCardLayout.preferredHeight = 62f;
        nextSiteText = GameUiFactory.CreateText(navigatorContent, "NextSite", theme, 22, theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        GameUiFactory.Stretch(nextSiteText.rectTransform);

        nextLocationButton = GameUiFactory.CreateButton(navigatorRow.transform, "NextLocation", theme, ">", GameUiTone.Surface, out _, 6f);
        LayoutElement nextLayout = GameUiFactory.GetOrAdd<LayoutElement>(nextLocationButton.gameObject);
        nextLayout.preferredWidth = 72f;
        nextLayout.preferredHeight = 62f;

        RectTransform relocationContent;
        GameObject relocationCard = GameUiFactory.CreateCard(shellContent, "RelocationCard", theme, out relocationContent, 394f);
        VerticalLayoutGroup relocationLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(relocationContent.gameObject);
        relocationLayout.padding = new RectOffset(0, 0, 0, 0);
        relocationLayout.spacing = 12f;
        relocationLayout.childControlWidth = true;
        relocationLayout.childControlHeight = false;
        relocationLayout.childForceExpandWidth = true;
        relocationLayout.childForceExpandHeight = false;

        Text relocationTitle = CreateStackLabel(relocationContent, "RelocationTitle", 28, theme.Ink, 34f);
        relocationTitle.text = RelocationLabel;

        quoteToggleButton = GameUiFactory.CreateButton(relocationContent, "QuoteToggle", theme, QuoteOpenLabel, GameUiTone.Accent, out quoteToggleText);
        LayoutElement quoteToggleLayout = GameUiFactory.GetOrAdd<LayoutElement>(quoteToggleButton.gameObject);
        quoteToggleLayout.preferredHeight = 68f;
        quoteToggleText.alignment = TextAnchor.MiddleCenter;

        quoteSection = GameUiFactory.CreateNode(relocationContent, "QuoteSection", typeof(LayoutElement));
        quoteSectionLayout = quoteSection.GetComponent<LayoutElement>();
        quoteSectionLayout.preferredHeight = 0f;
        quoteSectionLayout.flexibleWidth = 1f;

        RectTransform quoteCardContent;
        GameObject quoteCard = GameUiFactory.CreateCard(quoteSection.transform, "QuoteCard", theme, out quoteCardContent, 192f);
        GameUiFactory.Stretch(quoteCard.GetComponent<RectTransform>());
        quoteDetailsText = GameUiFactory.CreateText(quoteCardContent, "QuoteDetails", theme, 18, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        GameUiFactory.Stretch(quoteDetailsText.rectTransform);

        GameObject actionRow = GameUiFactory.CreateNode(relocationContent, "ActionRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 12f;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = true;
        actionLayout.childForceExpandHeight = false;
        GameUiFactory.GetOrAdd<LayoutElement>(actionRow).preferredHeight = 62f;

        collapseQuoteButton = GameUiFactory.CreateButton(actionRow.transform, "CollapseQuote", theme, QuoteCloseLabel, GameUiTone.Surface, out _);
        GameUiFactory.GetOrAdd<LayoutElement>(collapseQuoteButton.gameObject).preferredHeight = 62f;
        executeRelocationButton = GameUiFactory.CreateButton(actionRow.transform, "ExecuteRelocation", theme, ExecuteRelocationLabel, GameUiTone.Warning, out _);
        GameUiFactory.GetOrAdd<LayoutElement>(executeRelocationButton.gameObject).preferredHeight = 62f;

        RectTransform footerContent;
        GameObject footer = GameUiFactory.CreateCard(shellContent, "FooterCard", theme, out footerContent, 154f);
        VerticalLayoutGroup footerLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(footerContent.gameObject);
        footerLayout.padding = new RectOffset(0, 0, 0, 0);
        footerLayout.spacing = 10f;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = false;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = false;

        GameObject saveRow = CreateButtonRow(footerContent, "SaveRow");
        saveSlot1Button = GameUiFactory.CreateButton(saveRow.transform, "SaveSlot1Button", theme, SaveSlot1Label, GameUiTone.AccentAlt, out _);
        GameUiFactory.GetOrAdd<LayoutElement>(saveSlot1Button.gameObject).preferredHeight = 62f;
        saveSlot2Button = GameUiFactory.CreateButton(saveRow.transform, "SaveSlot2Button", theme, SaveSlot2Label, GameUiTone.AccentAlt, out _);
        GameUiFactory.GetOrAdd<LayoutElement>(saveSlot2Button.gameObject).preferredHeight = 62f;

        GameObject footerRow = CreateButtonRow(footerContent, "FooterRow");
        titleButton = GameUiFactory.CreateButton(footerRow.transform, "TitleButton", theme, TitleButtonLabel, GameUiTone.Surface, out _);
        GameUiFactory.GetOrAdd<LayoutElement>(titleButton.gameObject).preferredHeight = 62f;
        footerCloseButton = GameUiFactory.CreateButton(footerRow.transform, "FooterCloseButton", theme, CloseLabel, GameUiTone.Accent, out _);
        GameUiFactory.GetOrAdd<LayoutElement>(footerCloseButton.gameObject).preferredHeight = 62f;

        quoteSection.SetActive(false);
        popupPanel.SetActive(false);

        menuManager = FindFirstObjectByType<InGameMenuManager>();
        BindButtons();
        RefreshView();
        isBuilt = true;
    }

    private Text CreateStackLabel(Transform parent, string name, int fontSize, Color color, float preferredHeight)
    {
        Text label = GameUiFactory.CreateText(parent, name, theme, fontSize, color, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(label.gameObject);
        layout.preferredHeight = preferredHeight;
        return label;
    }

    private Text CreateStackBody(Transform parent, string name, int fontSize, Color color, float preferredHeight)
    {
        Text label = GameUiFactory.CreateText(parent, name, theme, fontSize, color, TextAnchor.UpperLeft, FontStyle.Bold);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(label.gameObject);
        layout.preferredHeight = preferredHeight;
        return label;
    }

    private static GameObject CreateButtonRow(Transform parent, string name)
    {
        GameObject row = GameUiFactory.CreateNode(parent, name, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        GameUiFactory.GetOrAdd<LayoutElement>(row).preferredHeight = 62f;
        return row;
    }

    private void Start()
    {
        menuManager = FindFirstObjectByType<InGameMenuManager>();
        RefreshView();
    }

    private void BindButtons()
    {
        BindButton(dimCloseButton, CloseMenu);
        BindButton(closeButton, CloseMenu);
        BindButton(saveSlot1Button, () => SaveSlot(1));
        BindButton(saveSlot2Button, () => SaveSlot(2));
        BindButton(titleButton, GoToTitle);
        BindButton(footerCloseButton, CloseMenu);
        BindButton(prevLocationButton, () => StepLocation(-1));
        BindButton(nextLocationButton, () => StepLocation(1));
        BindButton(quoteToggleButton, ToggleQuoteSection);
        BindButton(executeRelocationButton, ExecuteRelocation);
        BindButton(collapseQuoteButton, CollapseQuoteSection);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void OpenMenu()
    {
        EnsureBuiltFromSceneCanvas();
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();

        if (menuManager != null)
        {
            menuManager.SetMenuOpen(true);
        }

        isQuoteExpanded = false;

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling();
        }

        RefreshView();
    }

    private void EnsureBuiltFromSceneCanvas()
    {
        if (isBuilt)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas != null)
        {
            BuildUi(canvas.transform);
        }
    }

    public void CloseMenu()
    {
        if (menuManager != null)
        {
            menuManager.SetMenuOpen(false);
        }

        CloseMenuImmediate();
    }

    private void CloseMenuImmediate()
    {
        isQuoteExpanded = false;

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    private void SaveSlot(int slot)
    {
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();
        menuManager?.SaveManualSlot(slot);
        RefreshView();
    }

    private void GoToTitle()
    {
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();
        menuManager?.ReturnToTitleScene();
    }

    private void StepLocation(int direction)
    {
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();
        menuManager?.StepTargetLocationSelectionBy(direction);
        RefreshView();
    }

    private void ToggleQuoteSection()
    {
        isQuoteExpanded = !isQuoteExpanded;
        RefreshView();
    }

    private void CollapseQuoteSection()
    {
        isQuoteExpanded = false;
        RefreshView();
    }

    private void ExecuteRelocation()
    {
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();
        if (menuManager != null && menuManager.ExecuteCurrentRelocation())
        {
            CloseMenu();
            return;
        }

        RefreshView();
    }

    private void Update()
    {
        if (popupPanel == null || !popupPanel.activeSelf)
        {
            return;
        }

        RefreshView();
    }

    private void RefreshView()
    {
        menuManager ??= FindFirstObjectByType<InGameMenuManager>();
        if (menuManager == null)
        {
            SetText(currentSiteText, MenuMissingLabel);
            SetText(targetLocationText, string.Empty);
            SetText(locationSummaryText, string.Empty);
            SetText(nextSiteText, string.Empty);
            SetText(quoteToggleText, RelocationUnavailableLabel);
            SetText(quoteDetailsText, string.Empty);
            SetInteractable(quoteToggleButton, false);
            SetInteractable(executeRelocationButton, false);
            SetInteractable(collapseQuoteButton, false);
            if (quoteSection != null) quoteSection.SetActive(false);
            if (quoteSectionLayout != null) quoteSectionLayout.preferredHeight = 0f;
            return;
        }

        SetText(currentSiteText, CurrentSitePrefix + menuManager.GetCurrentSiteLabelText());
        SetText(targetLocationText, TargetSitePrefix + menuManager.GetSelectedTargetLocationLabelText());
        SetText(locationSummaryText, menuManager.GetSelectedTargetLocationSummaryText());

        RelocationManager.RelocationQuote quote;
        bool hasQuote = menuManager.TryGetCurrentRelocationQuote(out quote);

        if (hasQuote)
        {
            SetText(nextSiteText, NextSitePrefix + quote.targetSiteLabel);
            SetText(quoteToggleText, isQuoteExpanded ? QuoteCloseLabel : QuoteOpenLabel);
            SetText(quoteDetailsText, BuildQuoteDetails(quote));
            SetInteractable(quoteToggleButton, true);
            SetInteractable(executeRelocationButton, quote.isValid && quote.shortageAmount <= 0);
            SetInteractable(collapseQuoteButton, isQuoteExpanded);
            if (quoteSection != null) quoteSection.SetActive(isQuoteExpanded);
            if (quoteSectionLayout != null) quoteSectionLayout.preferredHeight = isQuoteExpanded ? 192f : 0f;
        }
        else
        {
            SetText(nextSiteText, quote.failReason);
            SetText(quoteToggleText, RelocationBlockedLabel);
            SetText(quoteDetailsText, quote.failReason);
            SetInteractable(quoteToggleButton, false);
            SetInteractable(executeRelocationButton, false);
            SetInteractable(collapseQuoteButton, false);
            isQuoteExpanded = false;

            if (quoteSection != null)
            {
                quoteSection.SetActive(false);
            }

            if (quoteSectionLayout != null)
            {
                quoteSectionLayout.preferredHeight = 0f;
            }
        }
    }

    private static void SetText(Text label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private static string BuildQuoteDetails(RelocationManager.RelocationQuote quote)
    {
        StringBuilder builder = new StringBuilder(256);
        builder.AppendLine("\uc774\uc0ac \uacac\uc801");
        builder.AppendLine($"\ud604\uc7ac \uc9c0\uc810: {quote.currentSiteLabel}");
        builder.AppendLine($"\ubaa9\ud45c \uc9c0\uc810: {quote.targetSiteLabel}");
        builder.AppendLine($"\uacf5\uac04 \ud06c\uae30: {quote.currentGridWidth}x{quote.currentGridHeight} -> {quote.targetGridWidth}x{quote.targetGridHeight}");
        builder.AppendLine($"\uc62e\uae38 \uae30\uad6c \uc218: {quote.placedEquipmentCount}");
        builder.AppendLine($"\uacc4\uc57d \ube44\uc6a9: {quote.contractFee:N0} G");
        builder.AppendLine($"\uc6b4\uc1a1 \ube44\uc6a9: {quote.transportFeeTotal:N0} G");
        builder.AppendLine($"\ucd1d \ube44\uc6a9: {quote.totalCost:N0} G");

        if (quote.shortageAmount > 0)
        {
            builder.AppendLine($"\ubd80\uc871 \uae08\uc561: {quote.shortageAmount:N0} G");
        }
        else
        {
            builder.AppendLine("\ubc14\ub85c \uc9c4\ud589 \uac00\ub2a5\ud569\ub2c8\ub2e4");
        }

        return builder.ToString();
    }
}
