using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class RuntimeGameUIController
{
    private const string MenuPanelSprite = "GeneratedRuntimeUI/ui_v2/staff/staff_window_base";
    private const string MenuInfoBoxSprite = "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base";
    private const string MenuGreenButtonSprite = "GeneratedRuntimeUI/ui_v2/button_green_base";
    private const string MenuBeigeButtonSprite = "GeneratedRuntimeUI/ui_v2/button_beige_base";
    private const string SliderTrackSprite = "GeneratedRuntimeUI/ui_v2/settings/settings_slider_track_base";
    private const string SliderFillSprite = "GeneratedRuntimeUI/ui_v2/settings/settings_slider_fill_green";
    private const string SliderKnobSprite = "GeneratedRuntimeUI/ui_v2/settings/settings_slider_knob_green";

    [Header("Menu Popup Layout")]
    [SerializeField] private Vector2 menuWindowSize = new Vector2(770f, 1010f);
    [SerializeField] private float menuTitleY = 418f;
    [SerializeField] private Vector2 menuClosePosition = new Vector2(310f, 394f);
    [SerializeField] private Vector2 menuButtonSize = new Vector2(620f, 88f);
    [SerializeField] private Vector2 menuSummarySize = new Vector2(650f, 190f);
    [SerializeField] private float menuSummaryY = 258f;
    [SerializeField] private float menuFooterY = -376f;
    [SerializeField] private Vector2 menuSummaryIconPosition = new Vector2(-242f, 0f);
    [SerializeField] private Vector2 menuSummaryIconSize = new Vector2(126f, 114f);
    [SerializeField] private float menuSummaryTextX = 8f;
    [SerializeField] private float menuSummaryNameY = 46f;
    [SerializeField] private float menuSummaryMemberY = -6f;
    [SerializeField] private float menuSummaryCashY = -56f;

    private Transform menuPopupRoot;
    private Transform relocationPopupRoot;
    private Transform settingsPopupRoot;

    private Image menuLocationIconImage;
    private Text menuGymNameText;
    private Text menuMemberText;
    private Text menuCashText;

    private Image relocationCurrentIconImage;
    private Image relocationTargetIconImage;
    private Text relocationCurrentText;
    private Text relocationTargetText;
    private Text relocationTargetRiskText;
    private Text relocationTargetFlowText;
    private Text relocationTargetRentText;
    private Text relocationCostText;
    private RectTransform relocationCurrentSizeBadge;
    private RectTransform relocationTargetSizeBadge;
    private Button relocationExecuteButton;
    private RuntimeSettingsSliderControl backgroundMusicSlider;
    private RuntimeSettingsSliderControl effectSoundSlider;

    private InGameMenuManager inGameMenuManager;

    private void OpenMenuPopup()
    {
        EnsureMenuPopups();
        ResolveReferences();
        CacheMenuManager();
        HideToast();

        RefreshMenuPopup();
        HideRuntimeMenuPopup(relocationPopupRoot);
        HideRuntimeMenuPopup(settingsPopupRoot);
        ShowRuntimeMenuPopup(menuPopupRoot);
        inGameMenuManager?.SetMenuOpen(true);
        NotifyInstallTutorialMenuOpened();
    }

    private void OpenRelocationPopup()
    {
        EnsureMenuPopups();
        CacheMenuManager();
        RefreshRelocationPopup();
        HideRuntimeMenuPopup(menuPopupRoot);
        HideRuntimeMenuPopup(settingsPopupRoot);
        ShowRuntimeMenuPopup(relocationPopupRoot);
        inGameMenuManager?.SetMenuOpen(true);
    }

    private void OpenSettingsPopup()
    {
        EnsureMenuPopups();
        CacheMenuManager();
        HideRuntimeMenuPopup(menuPopupRoot);
        HideRuntimeMenuPopup(relocationPopupRoot);
        ShowRuntimeMenuPopup(settingsPopupRoot);
        inGameMenuManager?.SetMenuOpen(true);
    }

    private void CloseRuntimeMenuPopups()
    {
        HideRuntimeMenuPopup(menuPopupRoot);
        HideRuntimeMenuPopup(relocationPopupRoot);
        HideRuntimeMenuPopup(settingsPopupRoot);
        inGameMenuManager?.SetMenuOpen(false);
    }

    private void RemoveSavedRuntimeMenuPopupRoots()
    {
        DestroySavedRuntimeMenuPopupRoot("RuntimeGameMenuPopupRoot");
        DestroySavedRuntimeMenuPopupRoot("RuntimeRelocationPopupRoot");
        DestroySavedRuntimeMenuPopupRoot("RuntimeSettingsPopupRoot");

        menuPopupRoot = null;
        relocationPopupRoot = null;
        settingsPopupRoot = null;
    }

    private void DestroySavedRuntimeMenuPopupRoot(string rootName)
    {
        if (runtimeRoot == null)
        {
            return;
        }

        for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
        {
            Transform popup = runtimeRoot.GetChild(i);
            if (popup.name != rootName)
            {
                continue;
            }

            popup.gameObject.SetActive(false);
            GameUiFactory.DestroyObject(popup.gameObject);
        }
    }

    private void EnsureMenuPopups()
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

        if (menuPopupRoot == null)
        {
            BuildMenuPopup();
        }

        if (relocationPopupRoot == null)
        {
            BuildRelocationPopup();
        }

        if (settingsPopupRoot == null)
        {
            BuildSettingsPopup();
        }
    }

#if UNITY_EDITOR
    public void PreviewMenuPopupForEditMode()
    {
        if (Application.isPlaying)
        {
            OpenMenuPopup();
            return;
        }

        EnsureMenuPopupPreviewRoot();
        RebuildRuntimeMenuPopupsForEditMode();
        RefreshMenuPopup();
        HideRuntimeMenuPopup(relocationPopupRoot);
        HideRuntimeMenuPopup(settingsPopupRoot);
        ShowRuntimeMenuPopup(menuPopupRoot);
    }

    public void RefreshMenuPopupPreviewForEditMode()
    {
        if (Application.isPlaying || !HasRuntimeMenuPopupPreview())
        {
            return;
        }

        bool showMenu = IsPopupActive(menuPopupRoot, "RuntimeGameMenuPopupRoot");
        bool showRelocation = IsPopupActive(relocationPopupRoot, "RuntimeRelocationPopupRoot");
        bool showSettings = IsPopupActive(settingsPopupRoot, "RuntimeSettingsPopupRoot");

        RebuildRuntimeMenuPopupsForEditMode();

        if (showRelocation)
        {
            RefreshRelocationPopup();
            HideRuntimeMenuPopup(menuPopupRoot);
            HideRuntimeMenuPopup(settingsPopupRoot);
            ShowRuntimeMenuPopup(relocationPopupRoot);
            return;
        }

        if (showSettings)
        {
            HideRuntimeMenuPopup(menuPopupRoot);
            HideRuntimeMenuPopup(relocationPopupRoot);
            ShowRuntimeMenuPopup(settingsPopupRoot);
            return;
        }

        if (showMenu)
        {
            RefreshMenuPopup();
            HideRuntimeMenuPopup(relocationPopupRoot);
            HideRuntimeMenuPopup(settingsPopupRoot);
            ShowRuntimeMenuPopup(menuPopupRoot);
        }
    }

    public void ApplyMenuPopupPreviewLayoutForEditMode()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        Transform previewRoot = FindRuntimeMenuPopupRoot("RuntimeGameMenuPopupRoot");
        if (previewRoot == null)
        {
            Debug.LogWarning("[RuntimeGameUIController] 메뉴 팝업 프리뷰가 없습니다. Preview Menu Popup을 먼저 실행하세요.", this);
            return;
        }

        RectTransform frame = FindMenuPreviewRect(previewRoot, "MenuPopupFrame");
        RectTransform title = FindMenuPreviewRect(previewRoot, "MenuPopupTitle");
        RectTransform summary = FindMenuPreviewRect(previewRoot, "MenuSummaryCard");
        RectTransform firstButton = FindMenuPreviewRect(previewRoot, "RelocateButton");
        RectTransform footer = FindMenuPreviewRect(previewRoot, "MenuAutosaveNotice");
        RectTransform icon = FindMenuPreviewRect(previewRoot, "LocationIcon");
        RectTransform gymName = FindMenuPreviewRect(previewRoot, "GymName");
        RectTransform memberCount = FindMenuPreviewRect(previewRoot, "MemberCount");
        RectTransform cashAmount = FindMenuPreviewRect(previewRoot, "CashAmount");

        menuWindowSize = GetMenuPreviewSize(frame, menuWindowSize);
        menuTitleY = GetMenuPreviewY(title, menuTitleY);
        menuButtonSize = GetMenuPreviewSize(firstButton != null ? firstButton : footer, menuButtonSize);
        menuSummarySize = GetMenuPreviewSize(summary, menuSummarySize);
        menuSummaryY = GetMenuPreviewY(summary, menuSummaryY);
        menuFooterY = GetMenuPreviewY(footer, menuFooterY);
        menuSummaryIconPosition = GetMenuPreviewPosition(icon, menuSummaryIconPosition);
        menuSummaryIconSize = GetMenuPreviewSize(icon, menuSummaryIconSize);
        menuSummaryTextX = GetMenuPreviewX(gymName, menuSummaryTextX);
        menuSummaryNameY = GetMenuPreviewY(gymName, menuSummaryNameY);
        menuSummaryMemberY = GetMenuPreviewY(memberCount, menuSummaryMemberY);
        menuSummaryCashY = GetMenuPreviewY(cashAmount, menuSummaryCashY);

        RefreshMenuPopupPreviewForEditMode();
    }

    public void CloseMenuPopupPreviewForEditMode()
    {
        if (Application.isPlaying)
        {
            CloseRuntimeMenuPopups();
            return;
        }

        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        DestroyRuntimeMenuPopup(ref menuPopupRoot, "RuntimeGameMenuPopupRoot");
        DestroyRuntimeMenuPopup(ref relocationPopupRoot, "RuntimeRelocationPopupRoot");
        DestroyRuntimeMenuPopup(ref settingsPopupRoot, "RuntimeSettingsPopupRoot");
    }

    private void EnsureMenuPopupPreviewRoot()
    {
        if (theme == null)
        {
            theme = GameUiTheme.CreateDefault();
        }

        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        if (runtimeRoot == null)
        {
            MaterializeForEditMode();
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }
    }

    private void RebuildRuntimeMenuPopupsForEditMode()
    {
        EnsureMenuPopupPreviewRoot();
        DestroyRuntimeMenuPopup(ref menuPopupRoot, "RuntimeGameMenuPopupRoot");
        DestroyRuntimeMenuPopup(ref relocationPopupRoot, "RuntimeRelocationPopupRoot");
        DestroyRuntimeMenuPopup(ref settingsPopupRoot, "RuntimeSettingsPopupRoot");
        EnsureMenuPopups();
    }

    private bool HasRuntimeMenuPopupPreview()
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        if (runtimeRoot == null)
        {
            return false;
        }

        return FindRuntimeMenuPopupRoot("RuntimeGameMenuPopupRoot") != null ||
               FindRuntimeMenuPopupRoot("RuntimeRelocationPopupRoot") != null ||
               FindRuntimeMenuPopupRoot("RuntimeSettingsPopupRoot") != null;
    }

    private static RectTransform FindMenuPreviewRect(Transform root, string name)
    {
        Transform target = FindDeepChild(root, name);
        return target != null ? target.GetComponent<RectTransform>() : null;
    }

    private static Vector2 GetMenuPreviewPosition(RectTransform rect, Vector2 fallback)
    {
        return rect != null ? rect.anchoredPosition : fallback;
    }

    private static Vector2 GetMenuPreviewSize(RectTransform rect, Vector2 fallback)
    {
        return rect != null ? rect.sizeDelta : fallback;
    }

    private static float GetMenuPreviewX(RectTransform rect, float fallback)
    {
        return rect != null ? rect.anchoredPosition.x : fallback;
    }

    private static float GetMenuPreviewY(RectTransform rect, float fallback)
    {
        return rect != null ? rect.anchoredPosition.y : fallback;
    }

    private bool IsPopupActive(Transform cachedRoot, string name)
    {
        Transform popup = cachedRoot != null ? cachedRoot : FindRuntimeMenuPopupRoot(name);
        return popup != null && popup.gameObject.activeSelf;
    }

    private Transform FindRuntimeMenuPopupRoot(string name)
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = transform.Find("RuntimeGameUIRoot");
        }

        return runtimeRoot != null ? runtimeRoot.Find(name) : null;
    }

    private void DestroyRuntimeMenuPopup(ref Transform cachedRoot, string name)
    {
        Transform popup = cachedRoot != null ? cachedRoot : FindRuntimeMenuPopupRoot(name);
        cachedRoot = null;

        if (popup == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(popup.gameObject);
        }
        else
        {
            DestroyImmediate(popup.gameObject);
        }
    }
#endif

    private Transform CreatePopupRoot(string name)
    {
        GameObject root = GameUiFactory.CreateNode(runtimeRoot, name);
        SetRect(root.GetComponent<RectTransform>(), 0f, 960f, 1080f, 1920f);

        GameObject dim = CreateSolid(root.transform, $"{name}_Dim", new Color(0.08f, 0.06f, 0.04f, 0.48f), 0f, 0f, 1080f, 1920f, true);
        Image dimImage = dim.GetComponent<Image>();
        if (dimImage != null)
        {
            dimImage.raycastTarget = true;
        }

        root.SetActive(false);
        return root.transform;
    }

    private void BuildMenuPopup()
    {
        menuPopupRoot = CreatePopupRoot("RuntimeGameMenuPopupRoot");
        GameObject frame = CreateGeneratedImage(menuPopupRoot, "MenuPopupFrame", MenuPanelSprite, 0f, 0f, menuWindowSize.x, menuWindowSize.y, false, true);

        CreateText(frame.transform, "MenuPopupTitle", "메뉴", 45, theme.Ink, TextAnchor.MiddleCenter, 0f, menuTitleY, 420f, 64f, true);

        GameObject summary = CreateGeneratedImage(frame.transform, "MenuSummaryCard", MenuInfoBoxSprite, 0f, menuSummaryY, menuSummarySize.x, menuSummarySize.y, false, true);
        menuLocationIconImage = CreateGeneratedImage(summary.transform, "LocationIcon", GetLocationIconPath(GymLocationType.Neighborhood), menuSummaryIconPosition.x, menuSummaryIconPosition.y, menuSummaryIconSize.x, menuSummaryIconSize.y, true, true).GetComponent<Image>();

        menuGymNameText = CreateText(summary.transform, "GymName", "동네 헬스장", 32, theme.Ink, TextAnchor.MiddleLeft, menuSummaryTextX, menuSummaryNameY, 410f, 48f, true);
        menuMemberText = CreateText(summary.transform, "MemberCount", "회원 0명", 28, theme.Ink, TextAnchor.MiddleLeft, menuSummaryTextX, menuSummaryMemberY, 410f, 42f, true);
        menuCashText = CreateText(summary.transform, "CashAmount", "자금 0G", 28, theme.Ink, TextAnchor.MiddleLeft, menuSummaryTextX, menuSummaryCashY, 410f, 42f, true);

        Button relocateButton = CreateMenuListButton(frame.transform, "RelocateButton", MenuGreenButtonSprite, "지점 이전", GetMenuButtonY(0), theme.BrightInk);
        relocateButton.onClick.AddListener(OpenRelocationPopup);

        Button titleButton = CreateMenuListButton(frame.transform, "ReturnTitleButton", MenuBeigeButtonSprite, "타이틀로", GetMenuButtonY(1), theme.Ink);
        titleButton.onClick.AddListener(() =>
        {
            CacheMenuManager();
            inGameMenuManager?.ReturnToTitleScene();
        });

        Button settingsButton = CreateMenuListButton(frame.transform, "SettingsButton", MenuBeigeButtonSprite, "설정", GetMenuButtonY(2), theme.Ink);
        settingsButton.onClick.AddListener(OpenSettingsPopup);

        Button closeListButton = CreateMenuListButton(frame.transform, "CloseListButton", MenuBeigeButtonSprite, "닫기", GetMenuButtonY(3), theme.Ink);
        closeListButton.onClick.AddListener(CloseRuntimeMenuPopups);

        GameObject footer = CreateGeneratedImage(frame.transform, "MenuAutosaveNotice", MenuInfoBoxSprite, 0f, menuFooterY, menuButtonSize.x, menuButtonSize.y, false, true);
        CreateText(footer.transform, "NoticeText", "현재 진행 상황은 자동 저장됩니다", 25, theme.MutedInk, TextAnchor.MiddleCenter, 0f, 0f, 560f, 46f, true);
        SetRuntimeMenuTextNormal(menuPopupRoot);
    }

    private void BuildRelocationPopup()
    {
        const float relocationWindowWidth = 800f;
        const float relocationWindowHeight = 1060f;
        const float relocationTitleY = 452f;
        const float relocationCurrentY = 240f;
        const float relocationTargetY = 0f;
        const float relocationQuoteY = -240f;
        const float relocationButtonY = -430f;

        relocationPopupRoot = CreatePopupRoot("RuntimeRelocationPopupRoot");
        GameObject frame = CreateGeneratedImage(relocationPopupRoot, "RelocationPopupFrame", MenuPanelSprite, 0f, 0f, relocationWindowWidth, relocationWindowHeight, false, true);

        CreateText(frame.transform, "RelocationTitle", "지점 이전", 44, theme.Ink, TextAnchor.MiddleCenter, 0f, relocationTitleY, 430f, 62f, true);

        GameObject currentBox = CreateGeneratedImage(frame.transform, "RelocationCurrentBox", MenuInfoBoxSprite, 0f, relocationCurrentY, 700f, 220f, false, true);
        relocationCurrentIconImage = CreateGeneratedImage(currentBox.transform, "CurrentLocationIcon", GetLocationIconPath(GymLocationType.Neighborhood), -278f, 3f, 120f, 120f, true, true).GetComponent<Image>();
        relocationCurrentText = CreateText(currentBox.transform, "CurrentInfo", "", 31, theme.Ink, TextAnchor.MiddleLeft, 0f, 3f, 430f, 150f, true);
        relocationCurrentSizeBadge = CreateRelocationSizeBadge(currentBox.transform, "CurrentSizeBadge", 238f, 3f, 150f, 150f);

        GameObject targetBox = CreateGeneratedImage(frame.transform, "RelocationTargetBox", MenuInfoBoxSprite, 0f, relocationTargetY, 700f, 220f, false, true);
        relocationTargetIconImage = CreateGeneratedImage(targetBox.transform, "TargetLocationIcon", GetLocationIconPath(GymLocationType.Neighborhood), -278f, -19f, 120f, 120f, true, true).GetComponent<Image>();
        relocationTargetText = CreateText(targetBox.transform, "TargetTitleRow", "", 34, theme.Ink, TextAnchor.MiddleCenter, -18f, 62f, 390f, 48f, true);
        relocationTargetRiskText = CreateText(targetBox.transform, "RiskText", "", 25, theme.MutedInk, TextAnchor.MiddleCenter, -18f, 26f, 350f, 36f, true);
        relocationTargetFlowText = CreateText(targetBox.transform, "FlowText", "", 25, theme.MutedInk, TextAnchor.MiddleCenter, -18f, -14f, 350f, 36f, true);
        relocationTargetRentText = CreateText(targetBox.transform, "RentText", "", 25, theme.MutedInk, TextAnchor.MiddleCenter, -18f, -54f, 350f, 36f, true);
        relocationTargetSizeBadge = CreateRelocationSizeBadge(targetBox.transform, "TargetSizeBadge", 240f, -19f, 150f, 150f);

        Button prevButton = CreateTransparentButton(targetBox.transform, "RelocationPrev", -178f, 62f, 90f, 66f);
        prevButton.onClick.AddListener(() =>
        {
            CacheMenuManager();
            inGameMenuManager?.StepTargetLocationSelectionBy(-1);
            RefreshRelocationPopup();
        });

        Button nextButton = CreateTransparentButton(targetBox.transform, "RelocationNext", 142f, 62f, 90f, 66f);
        nextButton.onClick.AddListener(() =>
        {
            CacheMenuManager();
            inGameMenuManager?.StepTargetLocationSelectionBy(1);
            RefreshRelocationPopup();
        });

        GameObject quoteBox = CreateGeneratedImage(frame.transform, "RelocationQuoteBox", MenuInfoBoxSprite, 0f, relocationQuoteY, 700f, 220f, false, true);
        relocationCostText = CreateText(quoteBox.transform, "QuoteText", "", 30, theme.Ink, TextAnchor.MiddleLeft, -18f, 8f, 620f, 146f, true);

        relocationExecuteButton = CreateSpriteButton(frame.transform, "RelocationExecuteButton", MenuGreenButtonSprite, "이전하기", -165f, relocationButtonY, 300f, 92f, theme.BrightInk, out Text executeLabel, 35);
        executeLabel.fontSize = 35;
        relocationExecuteButton.onClick.AddListener(() =>
        {
            CacheMenuManager();
            bool succeeded = inGameMenuManager != null && inGameMenuManager.ExecuteCurrentRelocation();
            if (succeeded)
            {
                CloseRuntimeMenuPopups();
                return;
            }

            RefreshRelocationPopup();
            ShowToast("지금은 지점 이전을 진행할 수 없습니다.");
        });

        Button cancelButton = CreateSpriteButton(frame.transform, "RelocationCancelButton", MenuBeigeButtonSprite, "취소", 165f, relocationButtonY, 300f, 92f, theme.Ink, out Text cancelLabel, 35);
        cancelLabel.fontSize = 35;
        cancelButton.onClick.AddListener(CloseRuntimeMenuPopups);

        SetRuntimeMenuTextNormal(relocationPopupRoot);
    }

    private void BuildSettingsPopup()
    {
        settingsPopupRoot = CreatePopupRoot("RuntimeSettingsPopupRoot");
        GameObject frame = CreateGeneratedImage(settingsPopupRoot, "SettingsPopupFrame", MenuPanelSprite, 0f, 0f, menuWindowSize.x, menuWindowSize.y, false, true);

        CreateText(frame.transform, "SettingsTitle", "설정", 45, theme.Ink, TextAnchor.MiddleCenter, 0f, menuTitleY, 420f, 64f, true);

        GameObject content = GameUiFactory.CreateNode(frame.transform, "SettingsContentRoot");
        SetRect(content.GetComponent<RectTransform>(), 0f, 44f, 646f, 604f, true);
        backgroundMusicSlider = CreateSettingsSliderRow(content.transform, "BackgroundMusic", "배경음", 156f, GameplayBgmPlayer.BackgroundVolume);
        CreateSettingsDivider(content.transform, 26f);
        effectSoundSlider = CreateSettingsSliderRow(content.transform, "EffectSound", "효과음", -126f, 0.80f);

        Button applyButton = CreateSpriteButton(frame.transform, "SettingsApplyButton", MenuGreenButtonSprite, "적용", -130f, -386f, 250f, 78f, theme.BrightInk, out Text applyLabel, 32);
        applyLabel.fontSize = 32;
        applyButton.onClick.AddListener(ApplySettingsSliderValues);

        Button closeListButton = CreateSpriteButton(frame.transform, "SettingsCloseButton", MenuBeigeButtonSprite, "닫기", 150f, -386f, 250f, 78f, theme.Ink, out Text closeLabel, 32);
        closeLabel.fontSize = 32;
        closeListButton.onClick.AddListener(CloseRuntimeMenuPopups);
        SetRuntimeMenuTextNormal(settingsPopupRoot);
    }

    private static void SetRuntimeMenuTextNormal(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].fontStyle = FontStyle.Normal;
        }
    }

    private float GetMenuButtonY(int index)
    {
        const int buttonCount = 4;
        const int gapCount = buttonCount + 1;

        float summaryBottomY = menuSummaryY - (menuSummarySize.y * 0.5f);
        float footerTopY = menuFooterY + (menuButtonSize.y * 0.5f);
        float availableGapSpace = summaryBottomY - footerTopY - (menuButtonSize.y * buttonCount);
        float equalGap = Mathf.Max(0f, availableGapSpace / gapCount);

        return summaryBottomY -
               equalGap -
               (menuButtonSize.y * 0.5f) -
               ((menuButtonSize.y + equalGap) * index);
    }

    private Button CreateMenuListButton(Transform parent, string name, string spritePath, string label, float y, Color textColor)
    {
        Button button = CreateSpriteButton(parent, name, spritePath, label, 0f, y, menuButtonSize.x, menuButtonSize.y, textColor, out Text labelText, 35);
        labelText.fontSize = 35;
        return button;
    }

    private Button CreateIconButton(Transform parent, string name, string label, float x, float y, float width, float height)
    {
        Button button = CreateSpriteButton(parent, name, MenuBeigeButtonSprite, label, x, y, width, height, theme.Ink, out Text labelText, 34);
        labelText.fontSize = 34;
        return button;
    }

    private Button CreateTransparentButton(Transform parent, string name, float x, float y, float width, float height)
    {
        GameObject node = CreateSolid(parent, name, new Color(1f, 1f, 1f, 0f), x, y, width, height, true);
        Image image = node.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = node.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.10f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Color.clear;
        button.colors = colors;
        return button;
    }

    private void CreateSettingsDivider(Transform parent, float y)
    {
        Color dashColor = new Color(0.70f, 0.42f, 0.16f, 0.62f);
        const int dashCount = 24;
        const float dashWidth = 16f;
        const float gap = 8f;
        float startX = -((dashCount - 1) * (dashWidth + gap)) * 0.5f;

        for (int i = 0; i < dashCount; i++)
        {
            CreateSolid(parent, "SettingsDividerDash_" + i, dashColor, startX + (i * (dashWidth + gap)), y, dashWidth, 3f, true);
        }
    }

    private RuntimeSettingsSliderControl CreateSettingsSliderRow(Transform parent, string name, string label, float y, float normalizedValue)
    {
        float value = Mathf.Clamp01(normalizedValue);
        const float trackX = 54f;
        const float trackWidth = 320f;
        const float trackHeight = 32f;
        float trackLeft = trackX - (trackWidth * 0.5f);
        float fillWidth = trackWidth * value;
        float fillX = trackLeft + (fillWidth * 0.5f);
        float knobX = trackLeft + (trackWidth * value);

        CreateText(parent, $"{name}_Label", label, 36, theme.Ink, TextAnchor.MiddleLeft, -240f, y, 160f, 54f, true);
        CreateGeneratedImage(parent, $"{name}_Track", SliderTrackSprite, trackX, y, trackWidth, trackHeight, false, true);
        RectTransform fillRect = CreateGeneratedImage(parent, $"{name}_Fill", SliderFillSprite, fillX, y, fillWidth, 28f, false, true).GetComponent<RectTransform>();
        RectTransform knobRect = CreateGeneratedImage(parent, $"{name}_Knob", SliderKnobSprite, knobX, y, 46f, 58f, true, true).GetComponent<RectTransform>();
        Text valueText = CreateText(parent, $"{name}_Value", $"{Mathf.RoundToInt(value * 100f)}%", 34, theme.Ink, TextAnchor.MiddleRight, 260f, y, 96f, 50f, true);

        GameObject hitArea = CreateSolid(parent, $"{name}_SliderHitArea", new Color(0f, 0f, 0f, 0f), trackX, y, trackWidth + 92f, 104f, true);
        Image hitImage = hitArea.GetComponent<Image>();
        if (hitImage != null)
        {
            hitImage.raycastTarget = true;
        }

        RuntimeSettingsSliderControl slider = hitArea.AddComponent<RuntimeSettingsSliderControl>();
        RectTransform trackRect = FindDeepChild(parent, $"{name}_Track")?.GetComponent<RectTransform>();
        slider.Initialize(trackRect, fillRect, knobRect, valueText, value);
        if (name == "BackgroundMusic")
        {
            slider.ValueChanged += GameplayBgmPlayer.SetBackgroundVolume;
        }

        hitArea.transform.SetAsLastSibling();
        return slider;
    }

    private void ApplySettingsSliderValues()
    {
        int bgm = backgroundMusicSlider != null ? Mathf.RoundToInt(backgroundMusicSlider.Value * 100f) : 70;
        int sfx = effectSoundSlider != null ? Mathf.RoundToInt(effectSoundSlider.Value * 100f) : 80;
        if (backgroundMusicSlider != null)
        {
            GameplayBgmPlayer.SetBackgroundVolume(backgroundMusicSlider.Value);
        }

        ShowToast($"설정값: 배경음 {bgm}% / 효과음 {sfx}%");
    }

    private void RefreshMenuPopup()
    {
        ResolveReferences();

        GymLocationType locationType = GetCurrentLocationTypeForMenu();
        if (menuLocationIconImage != null)
        {
            GeneratedRuntimeSprites.Assign(menuLocationIconImage, GetLocationIconPath(locationType), true);
        }

        SetText(menuGymNameText, GetGymNameForLocation(locationType));
        int memberCount = economyManager != null ? economyManager.GetActiveMemberCount() : 0;

        if (walletManager != null)
        {
            walletManager.InitializeWallet();
        }

        int cash = walletManager != null ? walletManager.CurrentCash : 0;
        SetText(menuMemberText, $"회원  {memberCount:N0}명");
        SetText(menuCashText, $"자금  {cash:N0}G");
    }

    private void RefreshRelocationPopup()
    {
        ResolveReferences();
        CacheMenuManager();

        if (walletManager != null)
        {
            walletManager.InitializeWallet();
        }

        string currentSite = inGameMenuManager != null ? inGameMenuManager.GetCurrentSiteLabelText() : "지점 정보 없음";
        string currentSize = siteManager != null ? $"{siteManager.CurrentGridWidth}x{siteManager.CurrentGridHeight}" : "-";
        int currentGridWidth = siteManager != null ? siteManager.CurrentGridWidth : 0;
        int currentGridHeight = siteManager != null ? siteManager.CurrentGridHeight : 0;
        GymLocationType currentLocationType = siteManager != null ? siteManager.CurrentLocationType : GymLocationType.Neighborhood;
        int cash = walletManager != null ? walletManager.CurrentCash : 0;

        if (relocationCurrentIconImage != null)
        {
            GeneratedRuntimeSprites.Assign(relocationCurrentIconImage, GetLocationIconPath(currentLocationType), true);
        }

        SetText(relocationCurrentText, $"현재 지점:  {currentSite}\n현재 크기:  {currentSize}\n보유 자금:  {cash:N0}G");
        ApplyRelocationSizeBadge(relocationCurrentSizeBadge, currentGridWidth, currentGridHeight, false);

        string target = inGameMenuManager != null ? inGameMenuManager.GetSelectedTargetLocationLabelText() : "목표 지점 없음";
        string summary = inGameMenuManager != null ? inGameMenuManager.GetSelectedTargetLocationSummaryText() : string.Empty;
        SetRelocationTargetDisplay(target, summary, 0, 0);

        bool canExecute = false;
        if (inGameMenuManager != null && inGameMenuManager.TryGetCurrentRelocationQuote(out RelocationManager.RelocationQuote quote))
        {
            canExecute = quote.isValid && quote.shortageAmount <= 0;

            if (relocationCurrentIconImage != null)
            {
                GeneratedRuntimeSprites.Assign(relocationCurrentIconImage, GetLocationIconPath(quote.currentLocationType), true);
            }

            if (relocationTargetIconImage != null)
            {
                GeneratedRuntimeSprites.Assign(relocationTargetIconImage, GetLocationIconPath(quote.targetLocationType), true);
            }

            SetText(relocationCurrentText, $"현재 지점:  {quote.currentSiteLabel}\n현재 크기:  {FormatGridSize(quote.currentGridWidth, quote.currentGridHeight)}\n보유 자금:  {cash:N0}G");
            SetRelocationTargetDisplay(quote.targetSiteLabel, quote.targetLocationSummary, quote.targetGridWidth, quote.targetGridHeight);
            SetText(
                relocationCostText,
                $"선택 지점:  {quote.targetSiteLabel}\n이전 비용:  {quote.totalCost:N0}G\n예상 변화:  {FormatGridSize(quote.targetGridWidth, quote.targetGridHeight)} 공간"
            );

            ApplyRelocationSizeBadge(relocationCurrentSizeBadge, quote.currentGridWidth, quote.currentGridHeight, false);
            ApplyRelocationSizeBadge(relocationTargetSizeBadge, quote.targetGridWidth, quote.targetGridHeight, true);
        }
        else
        {
            if (relocationTargetIconImage != null)
            {
                GeneratedRuntimeSprites.Assign(relocationTargetIconImage, GetLocationIconPath(currentLocationType), true);
            }

            SetRelocationTargetDisplay(target, summary, 0, 0);
            SetText(relocationCostText, "현재 이사 가능한 부지가 없습니다.");
            ApplyRelocationSizeBadge(relocationTargetSizeBadge, currentGridWidth, currentGridHeight, true);
        }

        if (relocationExecuteButton != null)
        {
            relocationExecuteButton.interactable = canExecute;
        }
    }

    private void SetRelocationTargetDisplay(string label, string summary, int gridWidth, int gridHeight)
    {
        SetText(relocationTargetText, BuildRelocationTargetTitle(label, gridWidth, gridHeight));
        SplitRelocationSummary(summary, out string firstLine, out string secondLine, out string thirdLine);
        SetText(relocationTargetRiskText, firstLine);
        SetText(relocationTargetFlowText, secondLine);
        SetText(relocationTargetRentText, thirdLine);
    }

    private static string BuildRelocationTargetTitle(string label, int gridWidth, int gridHeight)
    {
        string value = string.IsNullOrWhiteSpace(label) ? "목표 지점 없음" : label.Trim();
        if (!value.Contains("/") && gridWidth > 0 && gridHeight > 0)
        {
            value = $"{value} / {FormatGridSize(gridWidth, gridHeight)}";
        }

        return $"<  {value}  >";
    }

    private static void SplitRelocationSummary(string summary, out string firstLine, out string secondLine, out string thirdLine)
    {
        firstLine = string.Empty;
        secondLine = string.Empty;
        thirdLine = string.Empty;

        if (string.IsNullOrWhiteSpace(summary))
        {
            return;
        }

        string[] parts = summary.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            firstLine = parts[0].Trim();
        }

        if (parts.Length > 1)
        {
            secondLine = parts[1].Trim();
        }

        if (parts.Length > 2)
        {
            thirdLine = parts[2].Trim();
        }
    }

    private void CacheMenuManager()
    {
        if (inGameMenuManager == null)
        {
            inGameMenuManager = FindFirstObjectByType<InGameMenuManager>();
        }
    }

    private GymLocationType GetCurrentLocationTypeForMenu()
    {
        if (siteManager == null)
        {
            siteManager = FindFirstObjectByType<GymSiteManager>();
        }

        if (siteManager == null)
        {
            return GymLocationType.Neighborhood;
        }

        siteManager.InitializeSiteState();
        return siteManager.CurrentLocationType;
    }

    private static string GetLocationIconPath(GymLocationType locationType)
    {
        switch (locationType)
        {
            case GymLocationType.StationArea:
                return "GeneratedRuntimeUI/ui_v2/common/locations/icon_location_station";
            case GymLocationType.Downtown:
            case GymLocationType.Premium:
                return "GeneratedRuntimeUI/ui_v2/common/locations/icon_location_downtown";
            case GymLocationType.Neighborhood:
            default:
                return "GeneratedRuntimeUI/ui_v2/common/locations/icon_location_neighborhood";
        }
    }

    private static string GetGymNameForLocation(GymLocationType locationType)
    {
        return $"{GymSiteManager.GetLocationDisplayName(locationType)} 헬스장";
    }

    private RectTransform CreateRelocationSizeBadge(Transform parent, string name, float x, float y, float width, float height)
    {
        GameObject root = GameUiFactory.CreateNode(parent, name);
        SetRect(root.GetComponent<RectTransform>(), x, y, width, height, true);

        const float initialVisualSize = 112f;
        CreateSolid(root.transform, "SizeShadow", new Color(0.08f, 0.07f, 0.05f, 0.20f), 4f, -4f, initialVisualSize, initialVisualSize, true);
        GameObject square = CreateSolid(root.transform, "SizeSquare", new Color(0.56f, 0.56f, 0.54f, 0.94f), 0f, 0f, initialVisualSize, initialVisualSize, true);
        CreateSolid(square.transform, "SteelTopSheen", new Color(0.88f, 0.88f, 0.84f, 0.18f), 0f, initialVisualSize * 0.24f, initialVisualSize, initialVisualSize * 0.40f, true);
        CreateSolid(square.transform, "SteelBottomShade", new Color(0.22f, 0.22f, 0.21f, 0.16f), 0f, -initialVisualSize * 0.28f, initialVisualSize, initialVisualSize * 0.34f, true);
        CreateSolid(square.transform, "BadgeOutlineLeft", new Color(0.25f, 0.23f, 0.20f, 0.86f), -initialVisualSize * 0.5f, 0f, 2f, initialVisualSize, true);
        CreateSolid(square.transform, "BadgeOutlineRight", new Color(0.25f, 0.23f, 0.20f, 0.86f), initialVisualSize * 0.5f, 0f, 2f, initialVisualSize, true);
        CreateSolid(square.transform, "BadgeOutlineTop", new Color(0.25f, 0.23f, 0.20f, 0.86f), 0f, initialVisualSize * 0.5f, initialVisualSize, 2f, true);
        CreateSolid(square.transform, "BadgeOutlineBottom", new Color(0.25f, 0.23f, 0.20f, 0.86f), 0f, -initialVisualSize * 0.5f, initialVisualSize, 2f, true);

        Color dimensionColor = new Color(0.20f, 0.18f, 0.15f, 0.78f);
        CreateSolid(root.transform, "DimensionTopLineLeft", dimensionColor, -24f, 56f, 36f, 2f, true);
        CreateSolid(root.transform, "DimensionTopLineRight", dimensionColor, 24f, 56f, 36f, 2f, true);
        CreateSolid(root.transform, "DimensionTopTickLeft", dimensionColor, -56f, 50f, 2f, 12f, true);
        CreateSolid(root.transform, "DimensionTopTickRight", dimensionColor, 56f, 50f, 2f, 12f, true);
        CreateSolid(root.transform, "DimensionLeftLineTop", dimensionColor, -56f, 24f, 2f, 36f, true);
        CreateSolid(root.transform, "DimensionLeftLineBottom", dimensionColor, -56f, -24f, 2f, 36f, true);
        CreateSolid(root.transform, "DimensionLeftTickTop", dimensionColor, -50f, 56f, 12f, 2f, true);
        CreateSolid(root.transform, "DimensionLeftTickBottom", dimensionColor, -50f, -56f, 12f, 2f, true);
        CreateText(root.transform, "DimensionTopLabel", "", 20, new Color(0.16f, 0.14f, 0.11f, 0.96f), TextAnchor.MiddleCenter, 0f, 56f, 42f, 24f, true);
        CreateText(root.transform, "DimensionLeftLabel", "", 20, new Color(0.16f, 0.14f, 0.11f, 0.96f), TextAnchor.MiddleCenter, -56f, 0f, 42f, 24f, true);

        return root.GetComponent<RectTransform>();
    }

    private void ApplyRelocationSizeBadge(RectTransform root, int gridWidth, int gridHeight, bool highlighted)
    {
        if (root == null)
        {
            return;
        }

        float size = GetRelocationBadgeBoardSize(gridWidth, gridHeight);
        RectTransform squareRect = root.transform.Find("SizeSquare") as RectTransform;
        RectTransform shadowRect = root.transform.Find("SizeShadow") as RectTransform;

        if (shadowRect != null)
        {
            SetRect(shadowRect, 4f, -4f, size, size, true);
            Image shadowImage = shadowRect.GetComponent<Image>();
            if (shadowImage != null)
            {
                shadowImage.color = new Color(0.08f, 0.07f, 0.05f, 0.20f);
            }
        }

        if (squareRect == null)
        {
            return;
        }

        SetRect(squareRect, 0f, 0f, size, size, true);
        Image squareImage = squareRect.GetComponent<Image>();
        if (squareImage != null)
        {
            squareImage.color = highlighted
                ? new Color(0.58f, 0.58f, 0.55f, 0.94f)
                : new Color(0.56f, 0.56f, 0.54f, 0.94f);
        }

        Color outlineColor = highlighted
            ? new Color(0.30f, 0.27f, 0.21f, 0.88f)
            : new Color(0.25f, 0.23f, 0.20f, 0.86f);
        float half = size * 0.5f;
        float outlineThickness = size >= 120f ? 2.5f : 2f;

        ApplyRelocationBadgeLine(squareRect, "SteelTopSheen", 0f, size * 0.24f, size, size * 0.40f, new Color(0.88f, 0.88f, 0.84f, 0.18f), true);
        ApplyRelocationBadgeLine(squareRect, "SteelBottomShade", 0f, -size * 0.28f, size, size * 0.34f, new Color(0.22f, 0.22f, 0.21f, 0.16f), true);
        ApplyRelocationBadgeLine(squareRect, "BadgeOutlineLeft", -half, 0f, outlineThickness, size, outlineColor, true);
        ApplyRelocationBadgeLine(squareRect, "BadgeOutlineRight", half, 0f, outlineThickness, size, outlineColor, true);
        ApplyRelocationBadgeLine(squareRect, "BadgeOutlineTop", 0f, half, size, outlineThickness, outlineColor, true);
        ApplyRelocationBadgeLine(squareRect, "BadgeOutlineBottom", 0f, -half, size, outlineThickness, outlineColor, true);

        ApplyRelocationSizeDimensionLines(root, size, GetRelocationBadgeDimension(gridWidth, gridHeight));
    }

    private static float GetRelocationBadgeBoardSize(int gridWidth, int gridHeight)
    {
        int max = Mathf.Max(gridWidth, gridHeight);
        if (max <= 8)
        {
            return 64f;
        }

        if (max <= 16)
        {
            return 92f;
        }

        return 112f;
    }

    private static int GetRelocationBadgeDimension(int gridWidth, int gridHeight)
    {
        int max = Mathf.Max(gridWidth, gridHeight);
        if (max <= 8)
        {
            return 8;
        }

        if (max <= 16)
        {
            return 16;
        }

        return 32;
    }

    private void ApplyRelocationSizeDimensionLines(RectTransform root, float boardSize, int dimension)
    {
        if (root == null)
        {
            return;
        }

        const float lineOffset = 12f;
        const float maxLineCenter = 68f;
        const float labelGap = 34f;
        const float lineThickness = 2f;

        float half = boardSize * 0.5f;
        float topY = Mathf.Min(maxLineCenter, half + lineOffset);
        float leftX = Mathf.Max(-maxLineCenter, -half - lineOffset);
        float topSegmentWidth = Mathf.Max(7f, half - (labelGap * 0.5f));
        float topSegmentCenter = (half + (labelGap * 0.5f)) * 0.5f;
        float verticalSegmentHeight = topSegmentWidth;
        float verticalSegmentCenter = topSegmentCenter;
        float topTickHeight = Mathf.Max(6f, topY - half);
        float topTickY = half + (topTickHeight * 0.5f);
        float leftTickWidth = Mathf.Max(6f, -half - leftX);
        float leftTickX = leftX + (leftTickWidth * 0.5f);
        Color dimensionColor = new Color(0.20f, 0.18f, 0.15f, 0.78f);

        ApplyRelocationBadgeLine(root, "DimensionTopLineLeft", -topSegmentCenter, topY, topSegmentWidth, lineThickness, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionTopLineRight", topSegmentCenter, topY, topSegmentWidth, lineThickness, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionTopTickLeft", -half, topTickY, lineThickness, topTickHeight, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionTopTickRight", half, topTickY, lineThickness, topTickHeight, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionLeftLineTop", leftX, verticalSegmentCenter, lineThickness, verticalSegmentHeight, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionLeftLineBottom", leftX, -verticalSegmentCenter, lineThickness, verticalSegmentHeight, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionLeftTickTop", leftTickX, half, leftTickWidth, lineThickness, dimensionColor, true);
        ApplyRelocationBadgeLine(root, "DimensionLeftTickBottom", leftTickX, -half, leftTickWidth, lineThickness, dimensionColor, true);

        Text topLabel = root.transform.Find("DimensionTopLabel")?.GetComponent<Text>();
        if (topLabel != null)
        {
            SetText(topLabel, dimension.ToString());
            SetRect(topLabel.rectTransform, 0f, topY, 42f, 24f, true);
        }

        Text leftLabel = root.transform.Find("DimensionLeftLabel")?.GetComponent<Text>();
        if (leftLabel != null)
        {
            SetText(leftLabel, dimension.ToString());
            SetRect(leftLabel.rectTransform, leftX, 0f, 42f, 24f, true);
        }
    }

    private static void ApplyRelocationBadgeLine(RectTransform parent, string name, float x, float y, float width, float height, Color color, bool active)
    {
        Transform line = parent != null ? parent.Find(name) : null;
        if (line == null)
        {
            return;
        }

        line.gameObject.SetActive(active);
        Image image = line.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        RectTransform rect = line.GetComponent<RectTransform>();
        if (rect != null)
        {
            SetRect(rect, x, y, width, height, true);
        }
    }

    private static string FormatGridSize(int width, int height)
    {
        return $"{width}x{height}";
    }

    private static void ShowRuntimeMenuPopup(Transform popup)
    {
        if (popup == null)
        {
            return;
        }

        popup.gameObject.SetActive(true);
        popup.SetAsLastSibling();
    }

    private static void HideRuntimeMenuPopup(Transform popup)
    {
        if (popup != null)
        {
            popup.gameObject.SetActive(false);
        }
    }
}

internal sealed class RuntimeSettingsSliderControl : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public float Value { get; private set; }
    public event System.Action<float> ValueChanged;

    private RectTransform trackRect;
    private RectTransform fillRect;
    private RectTransform knobRect;
    private Text valueText;
    private float fillHeight;
    private float knobY;

    public void Initialize(RectTransform track, RectTransform fill, RectTransform knob, Text valueLabel, float initialValue)
    {
        trackRect = track;
        fillRect = fill;
        knobRect = knob;
        valueText = valueLabel;
        fillHeight = fillRect != null ? fillRect.sizeDelta.y : 28f;
        knobY = knobRect != null ? knobRect.anchoredPosition.y : 0f;
        SetValue(initialValue);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetValueFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        SetValueFromPointer(eventData);
    }

    private void SetValueFromPointer(PointerEventData eventData)
    {
        if (trackRect == null)
        {
            return;
        }

        Camera eventCamera = eventData != null ? eventData.pressEventCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        float width = Mathf.Max(1f, trackRect.rect.width);
        SetValue((localPoint.x + (width * 0.5f)) / width);
    }

    private void SetValue(float value)
    {
        Value = Mathf.Clamp01(value);
        float width = trackRect != null ? Mathf.Max(1f, trackRect.rect.width) : 1f;
        float centerX = trackRect != null ? trackRect.anchoredPosition.x : 0f;
        float left = centerX - (width * 0.5f);
        float fillWidth = width * Value;
        float knobX = left + (width * Value);

        if (fillRect != null)
        {
            fillRect.sizeDelta = new Vector2(fillWidth, fillHeight);
            fillRect.anchoredPosition = new Vector2(left + (fillWidth * 0.5f), fillRect.anchoredPosition.y);
        }

        if (knobRect != null)
        {
            knobRect.anchoredPosition = new Vector2(knobX, knobY);
        }

        if (valueText != null)
        {
            valueText.text = $"{Mathf.RoundToInt(Value * 100f)}%";
        }

        ValueChanged?.Invoke(Value);
    }
}
