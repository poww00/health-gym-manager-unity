using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EquipmentCatalogUIController : MonoBehaviour
{
    private const string RuntimeCatalogHostName = "RuntimeCatalogHost";

    [Header("References")]
    public EquipmentCatalog equipmentCatalog;
    public MainHUDController hudController;

    [Header("UI Containers")]
    public Transform categoryTabContainer;
    public Transform equipmentGridContainer;

    [Header("UI Labels")]
    public Text budgetChipText;
    public Text categorySummaryText;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite cardioIcon;
    [SerializeField] private Sprite weightIcon;
    [SerializeField] private Sprite recoveryIcon;
    [SerializeField] private Sprite facilityIcon;

    private GameUiTheme theme;
    private readonly List<Button> categoryButtons = new List<Button>();
    private readonly List<EquipmentCategory> categoryOrder = new List<EquipmentCategory>();
    private readonly List<GameObject> activeCards = new List<GameObject>();
    private EquipmentCategory currentCategory = EquipmentCategory.Cardio;
    private WalletManager walletManager;
    private PlacementManager placementManager;
    private RectTransform runtimeCatalogHost;
    private bool isBuilt;
    private string lastSignature = string.Empty;

    private void Awake()
    {
        EnsureIconSprites();
    }

    private void OnValidate()
    {
        EnsureIconSprites();
    }

    public void Configure(GameUiTheme uiTheme, MainHUDController mainHudController)
    {
        theme = uiTheme ?? GameUiTheme.CreateDefault();
        hudController = mainHudController;
        EnsureIconSprites();
    }

    public void Tick(bool isVisible)
    {
        if (!isVisible)
        {
            return;
        }

        RefreshCatalog();
    }

    public void RefreshCatalog()
    {
        equipmentCatalog ??= FindFirstObjectByType<EquipmentCatalog>(FindObjectsInactive.Include);
        hudController ??= FindFirstObjectByType<MainHUDController>(FindObjectsInactive.Include);
        walletManager ??= FindFirstObjectByType<WalletManager>(FindObjectsInactive.Include);
        placementManager ??= FindFirstObjectByType<PlacementManager>(FindObjectsInactive.Include);
        theme ??= GameUiTheme.CreateDefault();
        EnsureIconSprites();

        if (hudController == null || hudController.catalogRoot == null)
        {
            return;
        }

        if (!HasValidRuntimeScaffold())
        {
            ResetRuntimeScaffoldState(false);
        }

        EnsureCatalogRootLayout();
        EnsureCatalogScaffold();

        bool forceRefresh = ShouldForceRefresh();

        if (equipmentCatalog == null || equipmentCatalog.Definitions.Count == 0)
        {
            if (categorySummaryText != null)
            {
                categorySummaryText.text = "\uAE30\uAD6C \uB370\uC774\uD130 \uD655\uC778 \uC911";
            }

            if (budgetChipText != null)
            {
                int currentCash = walletManager != null ? walletManager.CurrentCash : 0;
                budgetChipText.text = $"\uC608\uC0B0  {currentCash:N0} G";
            }

            if (forceRefresh)
            {
                RebuildPreviewCards();
                RebuildCatalogLayout();
            }

            return;
        }

        string nextSignature = BuildSignature();
        if (!forceRefresh && nextSignature == lastSignature)
        {
            return;
        }

        lastSignature = nextSignature;
        UpdateHeaderTexts();
        UpdateCategoryTabVisuals();
        RebuildCards();
        RebuildCatalogLayout();
    }

    public void SelectCategory(EquipmentCategory category)
    {
        currentCategory = category;
        lastSignature = string.Empty;
        RefreshCatalog();
    }

    public void ForceRebuildCatalog()
    {
        ResetRuntimeScaffoldState(true);
        RefreshCatalog();
    }

    public GameObject GetScrollContentTargetObject()
    {
        return runtimeCatalogHost != null ? runtimeCatalogHost.gameObject : (hudController != null ? hudController.catalogRoot : null);
    }

    public void EditorBuildPreviewCatalog()
    {
        theme ??= GameUiTheme.CreateDefault();
        hudController ??= FindFirstObjectByType<MainHUDController>();
        if (hudController == null || hudController.catalogRoot == null)
        {
            return;
        }

        currentCategory = EquipmentCategory.Cardio;
        ResetRuntimeScaffoldState(true);
        EnsureCatalogRootLayout();
        EnsureCatalogScaffold();
        if (categorySummaryText != null)
        {
            categorySummaryText.text = $"{GetCategoryDisplayName(currentCategory)}  3\uc885";
        }

        UpdateCategoryTabVisuals();
        RebuildPreviewCards();
        RebuildCatalogLayout();
    }

    private void EnsureCatalogScaffold()
    {
        EnsureRuntimeCatalogHost();
        if (isBuilt && HasValidRuntimeScaffold() && runtimeCatalogHost != null && runtimeCatalogHost.childCount > 0)
        {
            return;
        }

        ClearRuntimeCatalogChildren();
        categoryButtons.Clear();
        categoryOrder.Clear();
        activeCards.Clear();
        categoryTabContainer = null;
        equipmentGridContainer = null;
        budgetChipText = null;
        categorySummaryText = null;

        VerticalLayoutGroup rootLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(runtimeCatalogHost.gameObject);
        rootLayout.spacing = 12f;
        rootLayout.padding = new RectOffset(10, 10, 0, 0);
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter rootFitter = GameUiFactory.GetOrAdd<ContentSizeFitter>(runtimeCatalogHost.gameObject);
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateCategoryRow(runtimeCatalogHost);
        CreateHeaderRow(runtimeCatalogHost);
        CreateEquipmentGrid(runtimeCatalogHost);
        isBuilt = true;
    }

    private void EnsureCatalogRootLayout()
    {
        RectTransform rect = hudController.catalogRoot.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup wrapperLayout = GameUiFactory.GetOrAdd<VerticalLayoutGroup>(hudController.catalogRoot);
        wrapperLayout.spacing = 0f;
        wrapperLayout.padding = new RectOffset(0, 0, 0, 0);
        wrapperLayout.childControlWidth = true;
        wrapperLayout.childControlHeight = false;
        wrapperLayout.childForceExpandWidth = true;
        wrapperLayout.childForceExpandHeight = false;
        wrapperLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter wrapperFitter = GameUiFactory.GetOrAdd<ContentSizeFitter>(hudController.catalogRoot);
        wrapperFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        wrapperFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        EnsureRuntimeCatalogHost();
    }

    private void CreateHeaderRow(Transform parent)
    {
        GameObject row = GameUiFactory.CreateNode(parent, "InstallHeaderRow", typeof(VerticalLayoutGroup), typeof(LayoutElement));
        VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(2, 2, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 64f;
        rowLayout.flexibleWidth = 1f;

        GameObject topLine = GameUiFactory.CreateNode(row.transform, "TopLine", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup topLayout = topLine.GetComponent<HorizontalLayoutGroup>();
        topLayout.spacing = 8f;
        topLayout.padding = new RectOffset(0, 0, 0, 0);
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = false;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = false;
        topLayout.childAlignment = TextAnchor.MiddleLeft;

        LayoutElement topLineLayout = topLine.GetComponent<LayoutElement>();
        topLineLayout.preferredHeight = 18f;

        categorySummaryText = GameUiFactory.CreateText(topLine.transform, "CategorySummary", theme, 15, theme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement summaryTextLayout = GameUiFactory.GetOrAdd<LayoutElement>(categorySummaryText.gameObject);
        summaryTextLayout.flexibleWidth = 1f;
        summaryTextLayout.preferredHeight = 18f;
        GameUiFactory.ConfigureSingleLineText(categorySummaryText, TextAnchor.MiddleLeft);

        budgetChipText = GameUiFactory.CreateText(topLine.transform, "Budget", theme, 14, theme.Warning, TextAnchor.MiddleRight, FontStyle.Bold);
        LayoutElement budgetLayout = GameUiFactory.GetOrAdd<LayoutElement>(budgetChipText.gameObject);
        budgetLayout.preferredWidth = 170f;
        budgetLayout.preferredHeight = 18f;
        GameUiFactory.ConfigureSingleLineText(budgetChipText, TextAnchor.MiddleRight);

        Text listLabel = GameUiFactory.CreateText(row.transform, "ListLabel", theme, 18, theme.MutedInk, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement listLabelLayout = GameUiFactory.GetOrAdd<LayoutElement>(listLabel.gameObject);
        listLabelLayout.preferredHeight = 20f;
        GameUiFactory.ConfigureSingleLineText(listLabel, TextAnchor.MiddleLeft);
        listLabel.text = "\uae30\uad6c \ub9ac\uc2a4\ud2b8";
    }

    private void AddCategoryTab(string label, EquipmentCategory category)
    {
        Button button = GameUiFactory.CreateButton(categoryTabContainer, $"{label}Tab", theme, label, GameUiTone.Surface, out Text labelText, 10f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(button.gameObject);
        layout.preferredHeight = 56f;
        layout.flexibleWidth = 1f;
        layout.minHeight = 56f;

        if (labelText != null)
        {
            labelText.fontSize = 18;
            labelText.color = theme != null ? theme.Ink : Color.black;
            GameUiFactory.ConfigureSingleLineText(labelText, TextAnchor.MiddleLeft);
        }

        ApplyButtonIcon(button, GetCategoryIcon(category), 18f, 10f, 8f);
        button.onClick.AddListener(() => SelectCategory(category));
        categoryButtons.Add(button);
        categoryOrder.Add(category);
    }

    private string BuildSignature()
    {
        int definitionCount = equipmentCatalog != null ? equipmentCatalog.Definitions.Count : 0;
        int currentCash = walletManager != null ? walletManager.CurrentCash : 0;
        return $"{currentCategory}|{currentCash}|{definitionCount}";
    }

    private void UpdateHeaderTexts()
    {
        if (categorySummaryText != null)
        {
            int count = GetDefinitionsForCategory(currentCategory).Count;
            categorySummaryText.text = $"\uc120\ud0dd \uce74\ud14c\uace0\ub9ac: {GetCategoryDisplayName(currentCategory)} \u00b7 {count}\uc885";
        }

        if (budgetChipText != null)
        {
            int currentCash = walletManager != null ? walletManager.CurrentCash : 0;
            budgetChipText.text = $"\ubcf4\uc720 \uc790\uae08  {currentCash:N0} G";
        }
    }

    private void UpdateCategoryTabVisuals()
    {
        for (int i = 0; i < categoryButtons.Count && i < categoryOrder.Count; i++)
        {
            bool isActive = categoryOrder[i] == currentCategory;
            Transform fill = categoryButtons[i].transform.Find("Fill");
            if (fill != null)
            {
                Image fillImage = fill.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = isActive ? theme.Accent : theme.TabIdle;
                }
            }

            Text label = categoryButtons[i].GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = isActive ? theme.BrightInk : theme.Ink;
            }

            UpdateButtonIconTint(categoryButtons[i], Color.white);
        }
    }

    private void RebuildCards()
    {
        GameUiFactory.ClearChildren(equipmentGridContainer);
        activeCards.Clear();

        List<EquipmentDefinition> matches = GetDefinitionsForCategory(currentCategory);
        if (matches.Count == 0)
        {
            activeCards.Add(CreateEmptyCard());
            UpdateEquipmentGridLayout();
            return;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            GameObject card = CreateEquipmentCard(matches[i]);
            activeCards.Add(card);
        }

        UpdateEquipmentGridLayout();
    }

    private void RebuildPreviewCards()
    {
        GameUiFactory.ClearChildren(equipmentGridContainer);
        activeCards.Clear();

        PreviewEquipmentCard(
            "\ub7ec\ub2dd \uba38\uc2e0",
            "\u2605\u2605\u2606  |  2x3\uce78",
            "2,000 G",
            "\uc124\uce58 \uac00\ub2a5",
            GameUiTone.Accent,
            EquipmentCategory.Cardio);

        PreviewEquipmentCard(
            "\uc0ac\uc774\ud074 \uba38\uc2e0",
            "\u2605\u2605\u2606  |  2x2\uce78",
            "3,000 G",
            "\uc124\uce58 \uac00\ub2a5",
            GameUiTone.Accent,
            EquipmentCategory.Cardio);

        PreviewEquipmentCard(
            "\ubca4\uce58\ud504\ub808\uc2a4",
            "\u2605\u2605\u2606  |  3x2\uce78",
            "4,000 G",
            "\uc124\uce58 \uac00\ub2a5",
            GameUiTone.Accent,
            EquipmentCategory.Push);

        PreviewEquipmentCard(
            "\uc0cc\ub4dc\ubc31",
            "\u2605\u2605\u2606  |  2x2\uce78",
            "5,000 G",
            "\uc124\uce58 \uac00\ub2a5",
            GameUiTone.Accent,
            EquipmentCategory.Pull);

        UpdateEquipmentGridLayout();
    }

    private GameObject CreateEmptyCard()
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreatePanel(equipmentGridContainer, "EmptyCard", theme, theme.PanelFill, out contentRoot, 16f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(card);
        layout.preferredHeight = 224f;

        Text title = GameUiFactory.CreateText(contentRoot, "Title", theme, 26, theme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(title.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 0.86f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -8f));
        title.text = "\ud574\ub2f9 \uce74\ud14c\uace0\ub9ac \uc7a5\ube44 \uc5c6\uc74c";

        Text body = GameUiFactory.CreateText(contentRoot, "Body", theme, 19, theme.MutedInk, TextAnchor.MiddleCenter, FontStyle.Bold);
        GameUiFactory.SetAnchoredRect(body.rectTransform, new Vector2(0f, 0.16f), new Vector2(1f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-36f, -8f));
        body.text = "\uc790\uc0b0\uc744 \ucd94\uac00\ud558\uac70\ub098 \ub2e4\ub978 \ubd84\ub958\ub97c \ud655\uc778\ud574\uc8fc\uc138\uc694.";
        return card;
    }

    private GameObject CreateEquipmentCard(EquipmentDefinition definition)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreatePanel(equipmentGridContainer, definition.DisplayName.Replace(" ", string.Empty), theme, theme.PanelFill, out contentRoot, 14f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(card);
        layout.preferredHeight = 140f;
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.raycastTarget = true;
        }
        AttachScrollForwarder(card);
        bool isAffordable = walletManager == null || walletManager.CurrentCash >= definition.InstallCost;
        Button actionButton = BuildEquipmentCardVisual(
            contentRoot,
            definition.DisplayName,
            definition.Category,
            GetDefinitionCardIcon(definition),
            $"{GetBrandStars(definition.BrandTier)}  |  {definition.Width}x{definition.Height}\uce78",
            $"{definition.InstallCost:N0} G",
            isAffordable ? "\uc124\uce58 \uac00\ub2a5" : "\uc790\uae08 \ubd80\uc871",
            isAffordable ? GameUiTone.Accent : GameUiTone.Danger);
        actionButton.onClick.AddListener(() =>
        {
            if (!isAffordable)
            {
                return;
            }

            EquipmentSelectionState.Select(definition);
            placementManager ??= FindFirstObjectByType<PlacementManager>(FindObjectsInactive.Include);
            placementManager?.SetPlacementDefinition(definition);
            BuildPlayModeManager.EnterBuildMode();
            hudController?.RefreshModeUiImmediate();
        });

        return card;
    }

    private void PreviewEquipmentCard(string displayName, string detail, string price, string status, GameUiTone tone, EquipmentCategory category)
    {
        RectTransform contentRoot;
        GameObject card = GameUiFactory.CreatePanel(equipmentGridContainer, displayName.Replace(" ", string.Empty), theme, theme.PanelFill, out contentRoot, 14f);
        LayoutElement layout = GameUiFactory.GetOrAdd<LayoutElement>(card);
        layout.preferredHeight = 140f;
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.raycastTarget = true;
        }
        AttachScrollForwarder(card);

        Button actionButton = BuildEquipmentCardVisual(contentRoot, displayName, category, GetCategoryIcon(category), detail, price, status, tone);
        actionButton.interactable = false;

        activeCards.Add(card);
    }

    private Button BuildEquipmentCardVisual(RectTransform contentRoot, string displayName, EquipmentCategory category, Sprite iconSprite, string detail, string price, string actionLabel, GameUiTone tone)
    {
        HorizontalLayoutGroup contentLayout = GameUiFactory.GetOrAdd<HorizontalLayoutGroup>(contentRoot.gameObject);
        contentLayout.spacing = 10f;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;

        GameObject portraitPlate = GameUiFactory.CreatePanel(contentRoot, "PortraitPlate", theme, theme.PanelFillAlt, out RectTransform portraitContent, 8f);
        LayoutElement portraitLayout = GameUiFactory.GetOrAdd<LayoutElement>(portraitPlate);
        portraitLayout.preferredWidth = 60f;
        portraitLayout.preferredHeight = 60f;
        portraitLayout.minWidth = 60f;
        portraitLayout.minHeight = 60f;

        Image portraitIcon = EnsureSpriteImage(portraitContent, "Icon", iconSprite != null ? iconSprite : GetCategoryIcon(category));
        if (portraitIcon != null)
        {
            portraitIcon.color = Color.white;
            GameUiFactory.Stretch(portraitIcon.rectTransform, 6f, 6f, 6f, 6f);
        }

        GameObject infoColumn = GameUiFactory.CreateNode(contentRoot, "InfoColumn", typeof(VerticalLayoutGroup), typeof(LayoutElement));
        VerticalLayoutGroup infoLayout = infoColumn.GetComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 4f;
        infoLayout.padding = new RectOffset(0, 0, 0, 0);
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = false;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;
        infoLayout.childAlignment = TextAnchor.UpperLeft;

        LayoutElement infoElement = infoColumn.GetComponent<LayoutElement>();
        infoElement.flexibleWidth = 1f;
        infoElement.minWidth = 0f;

        Text categoryText = GameUiFactory.CreateText(infoColumn.transform, "Category", theme, 12, theme.AccentAlt, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement categoryLayout = GameUiFactory.GetOrAdd<LayoutElement>(categoryText.gameObject);
        categoryLayout.preferredHeight = 14f;
        GameUiFactory.ConfigureSingleLineText(categoryText, TextAnchor.MiddleLeft);
        categoryText.text = GetCategoryDisplayName(category);

        Text nameText = GameUiFactory.CreateText(infoColumn.transform, "Name", theme, 17, theme.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
        LayoutElement nameLayout = GameUiFactory.GetOrAdd<LayoutElement>(nameText.gameObject);
        nameLayout.preferredHeight = 40f;
        GameUiFactory.ConfigureWrappedText(nameText, TextAnchor.UpperLeft);
        nameText.text = displayName;

        Text detailText = GameUiFactory.CreateText(infoColumn.transform, "Detail", theme, 12, theme.MutedInk, TextAnchor.UpperLeft, FontStyle.Normal);
        LayoutElement detailLayout = GameUiFactory.GetOrAdd<LayoutElement>(detailText.gameObject);
        detailLayout.preferredHeight = 18f;
        GameUiFactory.ConfigureSingleLineText(detailText, TextAnchor.MiddleLeft);
        detailText.text = detail;

        GameObject spacer = GameUiFactory.CreateNode(infoColumn.transform, "Spacer", typeof(LayoutElement));
        LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
        spacerLayout.flexibleHeight = 1f;

        GameObject bottomRow = GameUiFactory.CreateNode(infoColumn.transform, "BottomRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup bottomLayout = bottomRow.GetComponent<HorizontalLayoutGroup>();
        bottomLayout.spacing = 8f;
        bottomLayout.padding = new RectOffset(0, 0, 0, 0);
        bottomLayout.childControlWidth = true;
        bottomLayout.childControlHeight = true;
        bottomLayout.childForceExpandWidth = false;
        bottomLayout.childForceExpandHeight = false;
        bottomLayout.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement bottomRowLayout = bottomRow.GetComponent<LayoutElement>();
        bottomRowLayout.preferredHeight = 30f;

        Text priceText = GameUiFactory.CreateText(bottomRow.transform, "Price", theme, 20, theme.Warning, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement priceLayout = GameUiFactory.GetOrAdd<LayoutElement>(priceText.gameObject);
        priceLayout.flexibleWidth = 1f;
        priceLayout.preferredHeight = 24f;
        GameUiFactory.ConfigureSingleLineText(priceText, TextAnchor.MiddleLeft);
        priceText.text = price;

        Button actionButton = GameUiFactory.CreateButton(bottomRow.transform, "ActionButton", theme, actionLabel, tone, out Text actionLabelText, 8f);
        LayoutElement buttonLayout = GameUiFactory.GetOrAdd<LayoutElement>(actionButton.gameObject);
        buttonLayout.preferredWidth = 96f;
        buttonLayout.minWidth = 88f;
        buttonLayout.preferredHeight = 26f;
        actionLabelText.fontSize = 12;
        GameUiFactory.ConfigureSingleLineText(actionLabelText, TextAnchor.MiddleCenter);
        return actionButton;
    }

    private void UpdateEquipmentGridLayout()
    {
        if (equipmentGridContainer == null)
        {
            return;
        }

        GridLayoutGroup grid = equipmentGridContainer.GetComponent<GridLayoutGroup>();
        LayoutElement layout = equipmentGridContainer.GetComponent<LayoutElement>();
        if (grid == null || layout == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float viewportWidth = hudController != null ? hudController.GetCatalogViewportWidth() : 0f;
        if (viewportWidth <= 1f && runtimeCatalogHost != null)
        {
            viewportWidth = runtimeCatalogHost.rect.width;
        }

        float usableWidth = Mathf.Max(620f, viewportWidth - 28f);
        int columns = usableWidth >= 720f ? 2 : 1;
        float totalSpacing = grid.spacing.x * Mathf.Max(0, columns - 1);
        float totalPadding = grid.padding.left + grid.padding.right;
        float cellWidth = Mathf.Floor((usableWidth - totalSpacing - totalPadding) / columns);
        cellWidth = Mathf.Clamp(cellWidth, columns == 2 ? 300f : 420f, columns == 2 ? 430f : 820f);

        float cellHeight = columns == 2
            ? Mathf.Clamp(Mathf.Round(cellWidth * 0.46f), 138f, 164f)
            : Mathf.Clamp(Mathf.Round(cellWidth * 0.34f), 142f, 176f);

        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellWidth, cellHeight);

        int childCount = Mathf.Max(1, equipmentGridContainer.childCount);
        int rows = Mathf.CeilToInt(childCount / (float)columns);
        float preferredHeight =
            grid.padding.top +
            grid.padding.bottom +
            (rows * cellHeight) +
            (Mathf.Max(0, rows - 1) * grid.spacing.y);

        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        UpdateRuntimeCatalogHostHeight();
    }

    private Sprite GetCategoryIcon(EquipmentCategory category)
    {
        switch (category)
        {
            case EquipmentCategory.Cardio:
                return cardioIcon;
            case EquipmentCategory.Push:
            case EquipmentCategory.Pull:
            case EquipmentCategory.Legs:
                return weightIcon;
            case EquipmentCategory.Recovery:
                return recoveryIcon;
            case EquipmentCategory.Other:
            default:
                return facilityIcon;
        }
    }

    private Sprite GetDefinitionCardIcon(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return facilityIcon;
        }

#if UNITY_EDITOR
        if (definition.IconReference != null)
        {
            Sprite editorSprite = definition.IconReference.editorAsset as Sprite;
            if (editorSprite != null)
            {
                return editorSprite;
            }
        }
#endif

        return GetCategoryIcon(definition.Category);
    }

    private void ApplyButtonIcon(Button button, Sprite iconSprite, float iconSize, float leftPadding, float gap)
    {
        if (button == null || iconSprite == null)
        {
            return;
        }

        Transform content = button.transform.Find("Content");
        if (content == null)
        {
            return;
        }

        Image iconImage = EnsureSpriteImage(content, "Icon", iconSprite);
        if (iconImage == null)
        {
            return;
        }

        iconImage.color = Color.white;
        GameUiFactory.SetAnchoredRect(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(leftPadding, 0f), new Vector2(iconSize, iconSize));

        Transform labelTransform = content.Find("Label");
        Text labelText = labelTransform != null ? labelTransform.GetComponent<Text>() : button.GetComponentInChildren<Text>();
        if (labelText == null)
        {
            return;
        }

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.offsetMin = new Vector2(leftPadding + iconSize + gap, 0f);
        labelRect.offsetMax = new Vector2(-6f, 0f);
        labelText.alignment = TextAnchor.MiddleLeft;
    }

    private void UpdateButtonIconTint(Button button, Color tint)
    {
        if (button == null)
        {
            return;
        }

        Transform iconTransform = button.transform.Find("Content/Icon");
        if (iconTransform == null)
        {
            return;
        }

        Image iconImage = iconTransform.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = tint;
        }
    }

    private Image EnsureSpriteImage(Transform parent, string childName, Sprite sprite)
    {
        if (parent == null || sprite == null)
        {
            return null;
        }

        Transform existing = parent.Find(childName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image == null)
        {
            GameObject iconObject = GameUiFactory.CreateNode(parent, childName, typeof(CanvasRenderer), typeof(Image));
            image = iconObject.GetComponent<Image>();
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void EnsureIconSprites()
    {
#if UNITY_EDITOR
        cardioIcon ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Cardio.png");
        weightIcon ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Weight.png");
        recoveryIcon ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Recovery.png");
        facilityIcon ??= LoadEditorSpriteFlexible("Assets/_Project/Sprites/UI/Icon_Clipboard.png");
#endif
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorSpriteFlexible(string path)
    {
        Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (single != null)
        {
            return single;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
        }

        return null;
    }
#endif

    private void AttachScrollForwarder(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        ScrollRect scrollRect = hudController != null ? hudController.GetCatalogScrollRect() : null;
        if (scrollRect == null)
        {
            return;
        }

        ScrollDragForwarder forwarder = target.GetComponent<ScrollDragForwarder>();
        if (forwarder == null)
        {
            forwarder = target.AddComponent<ScrollDragForwarder>();
        }

        forwarder.Bind(scrollRect);
    }

    private List<EquipmentDefinition> GetDefinitionsForCategory(EquipmentCategory selected)
    {
        List<EquipmentDefinition> matches = new List<EquipmentDefinition>();
        if (equipmentCatalog == null)
        {
            return matches;
        }

        for (int i = 0; i < equipmentCatalog.Definitions.Count; i++)
        {
            EquipmentDefinition definition = equipmentCatalog.Definitions[i];
            if (MatchesCategory(selected, definition.Category))
            {
                matches.Add(definition);
            }
        }

        return matches;
    }

    private static bool MatchesCategory(EquipmentCategory selected, EquipmentCategory actual)
    {
        if (selected == actual)
        {
            return true;
        }

        return false;
    }

    private static string GetCategoryDisplayName(EquipmentCategory category)
    {
        switch (category)
        {
            case EquipmentCategory.Cardio:
                return "카디오";
            case EquipmentCategory.Push:
                return "푸쉬";
            case EquipmentCategory.Pull:
                return "풀";
            case EquipmentCategory.Legs:
                return "하체";
            case EquipmentCategory.Recovery:
                return "회복";
            case EquipmentCategory.Other:
                return "기타";
            default:
                return "기타";
        }
    }


    private static string GetBrandStars(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.SS:
                return "\u2605\u2605\u2605";
            case EquipmentBrandTier.S:
                return "\u2605\u2605\u2606";
            case EquipmentBrandTier.A:
                return "\u2605\u2606\u2606";
            default:
                return "\u2606\u2606\u2606";
        }
    }

    private void EnsureRuntimeCatalogHost()
    {
        if (hudController == null || hudController.catalogRoot == null)
        {
            runtimeCatalogHost = null;
            return;
        }

        RectTransform catalogRootRect = hudController.catalogRoot.GetComponent<RectTransform>();
        if (catalogRootRect == null)
        {
            runtimeCatalogHost = null;
            return;
        }
        runtimeCatalogHost = catalogRootRect;
        runtimeCatalogHost.gameObject.SetActive(true);
        runtimeCatalogHost.anchorMin = new Vector2(0f, 1f);
        runtimeCatalogHost.anchorMax = new Vector2(1f, 1f);
        runtimeCatalogHost.pivot = new Vector2(0.5f, 1f);
        runtimeCatalogHost.anchoredPosition = Vector2.zero;
        runtimeCatalogHost.sizeDelta = Vector2.zero;

        LayoutElement hostLayout = GameUiFactory.GetOrAdd<LayoutElement>(runtimeCatalogHost.gameObject);
        hostLayout.flexibleWidth = 1f;
        hostLayout.flexibleHeight = 0f;

        HideLegacyCatalogChildren();
        DropSerializedPrefabReferences();
    }

    private void UpdateRuntimeCatalogHostHeight()
    {
        if (runtimeCatalogHost == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        VerticalLayoutGroup layout = runtimeCatalogHost.GetComponent<VerticalLayoutGroup>();
        float totalHeight = 0f;
        int activeChildCount = 0;

        if (layout != null)
        {
            totalHeight += layout.padding.top + layout.padding.bottom;
        }

        for (int i = 0; i < runtimeCatalogHost.childCount; i++)
        {
            RectTransform child = runtimeCatalogHost.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(child);
            float childHeight = LayoutUtility.GetPreferredHeight(child);
            if (childHeight <= 0f)
            {
                childHeight = child.rect.height;
            }

            totalHeight += childHeight;
            activeChildCount++;
        }

        if (layout != null && activeChildCount > 1)
        {
            totalHeight += layout.spacing * (activeChildCount - 1);
        }

        LayoutElement hostLayout = GameUiFactory.GetOrAdd<LayoutElement>(runtimeCatalogHost.gameObject);
        hostLayout.preferredHeight = totalHeight;
        hostLayout.minHeight = totalHeight;
        hostLayout.flexibleHeight = 0f;
        runtimeCatalogHost.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
    }

    private void HideLegacyCatalogChildren()
    {
        // The runtime catalog now renders directly in catalogRoot.
    }

    private void DropSerializedPrefabReferences()
    {
        if (runtimeCatalogHost == null)
        {
            categoryTabContainer = null;
            equipmentGridContainer = null;
            budgetChipText = null;
            categorySummaryText = null;
            return;
        }

        if (categoryTabContainer != null && !categoryTabContainer.IsChildOf(runtimeCatalogHost))
        {
            categoryTabContainer = null;
        }

        if (equipmentGridContainer != null && !equipmentGridContainer.IsChildOf(runtimeCatalogHost))
        {
            equipmentGridContainer = null;
        }

        if (budgetChipText != null && !budgetChipText.transform.IsChildOf(runtimeCatalogHost))
        {
            budgetChipText = null;
        }

        if (categorySummaryText != null && !categorySummaryText.transform.IsChildOf(runtimeCatalogHost))
        {
            categorySummaryText = null;
        }
    }

    private bool HasValidRuntimeScaffold()
    {
        if (runtimeCatalogHost == null || hudController == null || hudController.catalogRoot == null)
        {
            return false;
        }

        RectTransform catalogRootRect = hudController.catalogRoot.GetComponent<RectTransform>();
        if (catalogRootRect == null)
        {
            return false;
        }

        bool isCatalogRoot = runtimeCatalogHost == catalogRootRect;
        if (!isCatalogRoot && !runtimeCatalogHost.IsChildOf(hudController.catalogRoot.transform))
        {
            return false;
        }

        return categoryTabContainer != null &&
               equipmentGridContainer != null &&
               budgetChipText != null &&
               categorySummaryText != null &&
               categoryTabContainer.IsChildOf(runtimeCatalogHost) &&
               equipmentGridContainer.IsChildOf(runtimeCatalogHost) &&
               budgetChipText.transform.IsChildOf(runtimeCatalogHost) &&
               categorySummaryText.transform.IsChildOf(runtimeCatalogHost);
    }

    private bool ShouldForceRefresh()
    {
        if (!HasValidRuntimeScaffold() || runtimeCatalogHost == null)
        {
            return true;
        }

        if (runtimeCatalogHost.childCount < 3)
        {
            return true;
        }

        return equipmentGridContainer == null || equipmentGridContainer.childCount == 0;
    }

    private void ResetRuntimeScaffoldState(bool clearRuntimeHost)
    {
        EnsureRuntimeCatalogHost();
        if (clearRuntimeHost && runtimeCatalogHost != null)
        {
            ClearRuntimeCatalogChildren();
        }

        categoryButtons.Clear();
        categoryOrder.Clear();
        activeCards.Clear();
        categoryTabContainer = null;
        equipmentGridContainer = null;
        budgetChipText = null;
        categorySummaryText = null;
        isBuilt = false;
        lastSignature = string.Empty;
    }

    private void RebuildCatalogLayout()
    {
        Canvas.ForceUpdateCanvases();
        UpdateRuntimeCatalogHostHeight();
        if (runtimeCatalogHost != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(runtimeCatalogHost);
        }

        if (hudController != null && hudController.catalogRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(hudController.catalogRoot.GetComponent<RectTransform>());
        }
    }

    private void ClearRuntimeCatalogChildren()
    {
        if (runtimeCatalogHost == null)
        {
            return;
        }

        for (int i = 0; i < runtimeCatalogHost.childCount; i++)
        {
            runtimeCatalogHost.GetChild(i).gameObject.SetActive(false);
        }

        GameUiFactory.ClearChildren(runtimeCatalogHost);
        LayoutElement hostLayout = GameUiFactory.GetOrAdd<LayoutElement>(runtimeCatalogHost.gameObject);
        hostLayout.preferredHeight = 0f;
        hostLayout.minHeight = 0f;
    }

    private void CreateCategoryRow(Transform parent)
    {
        GameObject row = GameUiFactory.CreateNode(parent, "InstallCategoryRow", typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 54f;
        rowLayout.flexibleWidth = 1f;

        categoryTabContainer = row.transform;
        AddCategoryTab("카디오", EquipmentCategory.Cardio);
        AddCategoryTab("푸쉬", EquipmentCategory.Push);
        AddCategoryTab("풀", EquipmentCategory.Pull);
        AddCategoryTab("하체", EquipmentCategory.Legs);
        AddCategoryTab("회복", EquipmentCategory.Recovery);
        AddCategoryTab("기타", EquipmentCategory.Other);
    }

    private void CreateEquipmentGrid(Transform parent)
    {
        GameObject gridObject = GameUiFactory.CreateNode(parent, "EquipmentGrid", typeof(GridLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        LayoutElement layout = gridObject.GetComponent<LayoutElement>();
        layout.preferredHeight = 0f;
        layout.minHeight = 0f;
        layout.flexibleWidth = 1f;

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(404f, 148f);
        grid.spacing = new Vector2(16f, 14f);
        grid.padding = new RectOffset(0, 0, 2, 6);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = gridObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        equipmentGridContainer = gridObject.transform;
    }
}


public sealed class ScrollDragForwarder : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScrollRect boundScrollRect;

    public void Bind(ScrollRect scrollRect)
    {
        boundScrollRect = scrollRect;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        boundScrollRect?.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (boundScrollRect == null || !boundScrollRect.IsActive())
        {
            return;
        }

        boundScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (boundScrollRect == null || !boundScrollRect.IsActive())
        {
            return;
        }

        boundScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (boundScrollRect == null || !boundScrollRect.IsActive())
        {
            return;
        }

        boundScrollRect.OnEndDrag(eventData);
    }
}
