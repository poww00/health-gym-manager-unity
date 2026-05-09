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

    private Text relocationCurrentText;
    private Text relocationTargetText;
    private Text relocationSummaryText;
    private Text relocationCostText;
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
        relocationPopupRoot = CreatePopupRoot("RuntimeRelocationPopupRoot");
        GameObject frame = CreateGeneratedImage(relocationPopupRoot, "RelocationPopupFrame", MenuPanelSprite, 0f, 0f, menuWindowSize.x, menuWindowSize.y, false, true);

        CreateText(frame.transform, "RelocationTitle", "지점 이전", 43, theme.Ink, TextAnchor.MiddleCenter, 0f, menuTitleY, 430f, 62f, true);
        Button closeButton = CreateIconButton(frame.transform, "RelocationClose", "X", menuClosePosition.x, menuClosePosition.y, 78f, 78f);
        closeButton.onClick.AddListener(CloseRuntimeMenuPopups);

        GameObject currentBox = CreateGeneratedImage(frame.transform, "RelocationCurrentBox", MenuInfoBoxSprite, 0f, 226f, 590f, 164f, false, true);
        relocationCurrentText = CreateText(currentBox.transform, "CurrentInfo", "", 27, theme.Ink, TextAnchor.MiddleLeft, 0f, 12f, 510f, 104f, true);

        Button prevButton = CreateIconButton(frame.transform, "RelocationPrev", "<", -254f, 84f, 72f, 72f);
        prevButton.onClick.AddListener(() =>
        {
            CacheMenuManager();
            inGameMenuManager?.StepTargetLocationSelectionBy(-1);
            RefreshRelocationPopup();
        });

        Button nextButton = CreateIconButton(frame.transform, "RelocationNext", ">", 254f, 84f, 72f, 72f);
        nextButton.onClick.AddListener(() =>
        {
            CacheMenuManager();
            inGameMenuManager?.StepTargetLocationSelectionBy(1);
            RefreshRelocationPopup();
        });

        GameObject targetBox = CreateGeneratedImage(frame.transform, "RelocationTargetBox", MenuInfoBoxSprite, 0f, 62f, 420f, 138f, false, true);
        relocationTargetText = CreateText(targetBox.transform, "TargetLabel", "", 31, theme.Ink, TextAnchor.MiddleCenter, 0f, 34f, 360f, 42f, true);
        relocationSummaryText = CreateText(targetBox.transform, "TargetSummary", "", 22, theme.MutedInk, TextAnchor.MiddleCenter, 0f, -24f, 360f, 58f, true);

        GameObject quoteBox = CreateGeneratedImage(frame.transform, "RelocationQuoteBox", MenuInfoBoxSprite, 0f, -118f, 590f, 164f, false, true);
        relocationCostText = CreateText(quoteBox.transform, "QuoteText", "", 26, theme.Ink, TextAnchor.MiddleLeft, 0f, 8f, 510f, 116f, true);

        relocationExecuteButton = CreateSpriteButton(frame.transform, "RelocationExecuteButton", MenuGreenButtonSprite, "이전하기", -150f, -278f, 250f, 78f, theme.BrightInk, out Text executeLabel, 32);
        executeLabel.fontSize = 32;
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

        Button cancelButton = CreateSpriteButton(frame.transform, "RelocationCancelButton", MenuBeigeButtonSprite, "취소", 150f, -278f, 250f, 78f, theme.Ink, out Text cancelLabel, 32);
        cancelLabel.fontSize = 32;
        cancelButton.onClick.AddListener(CloseRuntimeMenuPopups);

        CreateText(frame.transform, "RelocationHint", "기존 이사 시스템의 견적/실행 지점에 연결되어 있습니다", 22, theme.MutedInk, TextAnchor.MiddleCenter, 0f, -370f, 600f, 42f, true);
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
        int cash = walletManager != null ? walletManager.CurrentCash : 0;
        SetText(relocationCurrentText, $"현재 지점:  {currentSite}\n현재 크기:  {currentSize}\n보유 자금:  {cash:N0}G");

        string target = inGameMenuManager != null ? inGameMenuManager.GetSelectedTargetLocationLabelText() : "목표 지점 없음";
        string summary = inGameMenuManager != null ? inGameMenuManager.GetSelectedTargetLocationSummaryText() : string.Empty;
        SetText(relocationTargetText, target);
        SetText(relocationSummaryText, summary);

        bool canExecute = false;
        if (inGameMenuManager != null && inGameMenuManager.TryGetCurrentRelocationQuote(out RelocationManager.RelocationQuote quote))
        {
            canExecute = quote.isValid && quote.shortageAmount <= 0;
            SetText(
                relocationCostText,
                $"선택 지점:  {quote.targetSiteLabel}\n이전 비용:  {quote.totalCost:N0}G\n예상 변화:  {quote.targetGridWidth}x{quote.targetGridHeight} 공간"
            );
        }
        else
        {
            SetText(relocationCostText, "현재 이사 가능한 부지가 없습니다.");
        }

        if (relocationExecuteButton != null)
        {
            relocationExecuteButton.interactable = canExecute;
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
