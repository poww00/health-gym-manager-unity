using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class RuntimeGameUIController
{
    private enum InstallTutorialStep
    {
        None,
        IntroGreeting,
        IntroGameGoal,
        IntroFirstAction,
        PressInstallTab,
        SelectTreadmill,
        PlaceTreadmill,
        ExplainOperateInfo,
        ExplainStaff,
        ExplainMenu
    }

    private struct InstallTutorialLayout
    {
        public bool hasFocus;
        public Rect focusRect;
        public readonly List<Rect> passThroughRects;
        public Vector2 targetCenter;
        public Vector2 calloutCenter;
        public string badgeText;
        public string message;
        public float calloutWidth;
        public float calloutHeight;
        public bool showArrow;
        public bool showTargetHighlight;
        public bool canTapCallout;

        public InstallTutorialLayout(bool initialize)
        {
            hasFocus = false;
            focusRect = Rect.zero;
            passThroughRects = new List<Rect>();
            targetCenter = Vector2.zero;
            calloutCenter = Vector2.zero;
            badgeText = string.Empty;
            message = string.Empty;
            calloutWidth = 650f;
            calloutHeight = 142f;
            showArrow = false;
            showTargetHighlight = false;
            canTapCallout = false;
        }
    }

    private const string InstallTutorialCompletedKey = "GymInstallTutorial.Completed.v2";
    private const string InstallTutorialRootName = "InstallTutorialOverlayRoot";
    private const string InstallTutorialStepBadgeSprite = "GeneratedRuntimeUI/ui_v2/tutorial/Tutorial_StepBadge_Green";
    private const string InstallTutorialArrowSprite = "GeneratedRuntimeUI/ui_v2/tutorial/Tutorial_Arrow_Curved";
    private const string InstallTutorialMessageBoxSprite = "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base";
    private const string InstallTutorialHighlightSprite = "GeneratedRuntimeUI/ui_v2/tab_active_green_base";

    private static readonly Rect InstallTutorialScreenRect = new Rect(-540f, -960f, 1080f, 1920f);

    private Transform installTutorialRoot;
    private PlacementManager installTutorialBoundPlacementManager;
    private InstallTutorialStep installTutorialStep = InstallTutorialStep.None;
    private bool installTutorialRunning;

    public void StartInstallTutorialForDebug()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[InstallTutorial] 튜토리얼 시작은 Play Mode에서 확인해 주세요.", this);
            return;
        }

        PlayerPrefs.DeleteKey(InstallTutorialCompletedKey);
        PlayerPrefs.Save();
        StartInstallTutorial(force: true);
    }

    public void ResetInstallTutorial()
    {
        PlayerPrefs.DeleteKey(InstallTutorialCompletedKey);
        PlayerPrefs.DeleteKey("GymInstallTutorial.Completed.v1");
        PlayerPrefs.Save();
        EndInstallTutorial(markCompleted: false);
        Debug.Log("[InstallTutorial] 설치 튜토리얼 완료 플래그를 초기화했습니다.", this);
    }

    private void InitializeInstallTutorial()
    {
        if (!Application.isPlaying || runtimeRoot == null)
        {
            return;
        }

        BindInstallTutorialEvents();

        if (PlayerPrefs.GetInt(InstallTutorialCompletedKey, 0) == 0)
        {
            StartInstallTutorial(force: false);
            return;
        }

        EnsureInstallTutorialRoot();
        if (installTutorialRoot != null)
        {
            installTutorialRoot.gameObject.SetActive(false);
        }
    }

    private void BindInstallTutorialEvents()
    {
        ResolveReferences();

        if (installTutorialBoundPlacementManager == placementManager)
        {
            return;
        }

        if (installTutorialBoundPlacementManager != null)
        {
            installTutorialBoundPlacementManager.ObjectPlaced -= HandleInstallTutorialObjectPlaced;
        }

        installTutorialBoundPlacementManager = placementManager;

        if (installTutorialBoundPlacementManager != null)
        {
            installTutorialBoundPlacementManager.ObjectPlaced -= HandleInstallTutorialObjectPlaced;
            installTutorialBoundPlacementManager.ObjectPlaced += HandleInstallTutorialObjectPlaced;
        }
    }

    private void UnbindInstallTutorialEvents()
    {
        if (installTutorialBoundPlacementManager != null)
        {
            installTutorialBoundPlacementManager.ObjectPlaced -= HandleInstallTutorialObjectPlaced;
            installTutorialBoundPlacementManager = null;
        }
    }

    private void StartInstallTutorial(bool force)
    {
        if (!Application.isPlaying || runtimeRoot == null)
        {
            return;
        }

        if (!force && PlayerPrefs.GetInt(InstallTutorialCompletedKey, 0) != 0)
        {
            return;
        }

        BindInstallTutorialEvents();
        installTutorialRunning = true;
        ShowInstallTutorialStep(InstallTutorialStep.IntroGreeting);
    }

    private void EndInstallTutorial(bool markCompleted)
    {
        installTutorialRunning = false;
        installTutorialStep = InstallTutorialStep.None;

        if (markCompleted)
        {
            PlayerPrefs.SetInt(InstallTutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        if (installTutorialRoot != null)
        {
            installTutorialRoot.gameObject.SetActive(false);
            GameUiFactory.ClearChildren(installTutorialRoot);
        }
    }

    private void NotifyInstallTutorialInstallTabOpened()
    {
        if (!installTutorialRunning || installTutorialStep != InstallTutorialStep.PressInstallTab)
        {
            return;
        }

        ShowInstallTutorialStep(InstallTutorialStep.SelectTreadmill);
    }

    private void NotifyInstallTutorialEquipmentSelected(EquipmentDefinition definition)
    {
        if (!installTutorialRunning || installTutorialStep != InstallTutorialStep.SelectTreadmill)
        {
            return;
        }

        if (!IsInstallTutorialTreadmill(definition))
        {
            return;
        }

        ShowInstallTutorialStep(InstallTutorialStep.PlaceTreadmill);
    }

    private void HandleInstallTutorialObjectPlaced(EquipmentDefinition definition)
    {
        if (!installTutorialRunning || installTutorialStep != InstallTutorialStep.PlaceTreadmill)
        {
            return;
        }

        if (!IsInstallTutorialTreadmill(definition))
        {
            return;
        }

        ShowOperatePanel();
        ShowInstallTutorialStep(InstallTutorialStep.ExplainOperateInfo);
    }

    private void AdvanceInstallTutorialMessageStep()
    {
        if (!installTutorialRunning)
        {
            return;
        }

        switch (installTutorialStep)
        {
            case InstallTutorialStep.IntroGreeting:
                ShowInstallTutorialStep(InstallTutorialStep.IntroGameGoal);
                break;
            case InstallTutorialStep.IntroGameGoal:
                ShowInstallTutorialStep(InstallTutorialStep.IntroFirstAction);
                break;
            case InstallTutorialStep.IntroFirstAction:
                ShowInstallTutorialStep(InstallTutorialStep.PressInstallTab);
                break;
            case InstallTutorialStep.ExplainOperateInfo:
                ShowInstallTutorialStep(InstallTutorialStep.ExplainStaff);
                break;
            case InstallTutorialStep.ExplainStaff:
                ShowInstallTutorialStep(InstallTutorialStep.ExplainMenu);
                break;
            case InstallTutorialStep.ExplainMenu:
                EndInstallTutorial(markCompleted: true);
                break;
        }
    }

    private void ShowInstallTutorialStep(InstallTutorialStep step)
    {
        installTutorialStep = step;
        EnsureInstallTutorialRoot();

        if (installTutorialRoot == null)
        {
            return;
        }

        installTutorialRoot.gameObject.SetActive(true);
        installTutorialRoot.SetAsLastSibling();
        GameUiFactory.ClearChildren(installTutorialRoot);

        if (!TryResolveInstallTutorialLayout(step, out InstallTutorialLayout layout))
        {
            installTutorialRoot.gameObject.SetActive(false);
            return;
        }

        DrawInstallTutorialDim(layout);
        DrawInstallTutorialInputBlocker(layout.passThroughRects);

        if (layout.showTargetHighlight)
        {
            DrawInstallTutorialHighlight(layout.focusRect);
        }

        if (layout.showArrow)
        {
            DrawInstallTutorialArrow(layout.targetCenter, layout.calloutCenter);
        }

        DrawInstallTutorialCallout(layout);
    }

    private void EnsureInstallTutorialRoot()
    {
        if (runtimeRoot == null)
        {
            return;
        }

        if (installTutorialRoot == null)
        {
            installTutorialRoot = FindDeepChild(runtimeRoot, InstallTutorialRootName);
        }

        if (installTutorialRoot != null)
        {
            return;
        }

        installTutorialRoot = GameUiFactory.CreateNode(runtimeRoot, InstallTutorialRootName).transform;
        SetRect(installTutorialRoot.GetComponent<RectTransform>(), 0f, 960f, 1080f, 1920f);
        installTutorialRoot.gameObject.SetActive(false);
    }

    private bool TryResolveInstallTutorialLayout(InstallTutorialStep step, out InstallTutorialLayout layout)
    {
        layout = new InstallTutorialLayout(true);

        switch (step)
        {
            case InstallTutorialStep.IntroGreeting:
                layout.badgeText = "!";
                layout.message = "어서 오세요!\n오늘부터 작은 헬스장을 함께 운영해 볼 거예요.";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 760f;
                layout.calloutHeight = 190f;
                layout.canTapCallout = true;
                return true;

            case InstallTutorialStep.IntroGameGoal:
                layout.badgeText = "!";
                layout.message = "기구를 설치하고, 회원을 늘리고,\n자금과 평판을 키우는 경영 게임입니다.";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 780f;
                layout.calloutHeight = 190f;
                layout.canTapCallout = true;
                return true;

            case InstallTutorialStep.IntroFirstAction:
                layout.badgeText = "!";
                layout.message = "그럼 첫 번째 기구를 설치해 볼까요?";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 700f;
                layout.calloutHeight = 156f;
                layout.canTapCallout = true;
                return true;

            case InstallTutorialStep.PressInstallTab:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("InstallTabButton", new Vector2(18f, 18f), new Rect(-270f, -940f, 284f, 146f));
                layout.hasFocus = true;
                layout.focusRect = targetRect;
                layout.passThroughRects.Add(targetRect);
                layout.targetCenter = targetRect.center;
                layout.calloutCenter = ClampInstallTutorialCallout(targetRect.center + new Vector2(24f, 178f), 650f, 142f);
                layout.badgeText = "1";
                layout.message = "설치 탭을 눌러 기구 목록을 열어보세요";
                layout.showArrow = true;
                layout.showTargetHighlight = true;
                return true;
            }

            case InstallTutorialStep.SelectTreadmill:
            {
                PrepareInstallTutorialTreadmillList();
                Rect targetRect = GetInstallTutorialTargetRect(FindInstallTutorialTreadmillCard(), new Vector2(16f, 16f), new Rect(-500f, -360f, 516f, 132f));
                layout.hasFocus = true;
                layout.focusRect = targetRect;
                layout.passThroughRects.Add(targetRect);
                layout.targetCenter = targetRect.center;
                layout.calloutCenter = ClampInstallTutorialCallout(targetRect.center + new Vector2(246f, -160f), 650f, 142f);
                layout.badgeText = "2";
                layout.message = "러닝머신을 선택해 배치 준비를 해보세요";
                layout.showArrow = true;
                layout.showTargetHighlight = true;
                return true;
            }

            case InstallTutorialStep.PlaceTreadmill:
            {
                Rect gridRect = new Rect(-452f, -248f, 904f, 520f);
                Rect actionRect = GetInstallTutorialTargetRect(
                    placementActionRoot != null ? placementActionRoot.GetComponent<RectTransform>() : null,
                    new Vector2(18f, 18f),
                    new Rect(-376f, -720f, 752f, 324f));
                Rect focusRect = UnionRects(gridRect, actionRect);
                layout.hasFocus = true;
                layout.focusRect = focusRect;
                layout.passThroughRects.Add(gridRect);
                layout.passThroughRects.Add(actionRect);
                layout.targetCenter = gridRect.center + new Vector2(40f, 28f);
                layout.calloutCenter = ClampInstallTutorialCallout(gridRect.center + new Vector2(205f, 265f), 680f, 142f);
                layout.badgeText = "3";
                layout.message = "빈 공간을 누른 뒤 설치 버튼으로 배치하세요";
                layout.showArrow = true;
                layout.showTargetHighlight = false;
                return true;
            }

            case InstallTutorialStep.ExplainOperateInfo:
            {
                Rect targetRect = GetInstallTutorialTargetRect(
                    sharedPanelRoot != null ? sharedPanelRoot.GetComponent<RectTransform>() : null,
                    new Vector2(8f, 8f),
                    new Rect(-520f, -600f, 1040f, 752f));
                layout.hasFocus = true;
                layout.focusRect = targetRect;
                layout.targetCenter = targetRect.center;
                layout.calloutCenter = ClampInstallTutorialCallout(targetRect.center + new Vector2(0f, 250f), 760f, 164f);
                layout.badgeText = "i";
                layout.message = "운영 패널에서는 목표, 청결도, 기구 사용률,\n대기 회원 같은 현재 상황을 확인할 수 있어요.";
                layout.showArrow = true;
                layout.canTapCallout = true;
                return true;
            }

            case InstallTutorialStep.ExplainStaff:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("StaffButton", new Vector2(14f, 14f), new Rect(286f, 790f, 132f, 154f));
                layout.hasFocus = true;
                layout.focusRect = targetRect;
                layout.targetCenter = targetRect.center;
                layout.calloutCenter = ClampInstallTutorialCallout(targetRect.center + new Vector2(-260f, -220f), 730f, 164f);
                layout.badgeText = "i";
                layout.message = "직원 버튼에서는 직원을 고용하고 관리할 수 있어요.\n회원이 늘면 운영을 도와줄 직원이 중요해집니다.";
                layout.showArrow = true;
                layout.canTapCallout = true;
                return true;
            }

            case InstallTutorialStep.ExplainMenu:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("MenuButton", new Vector2(14f, 14f), new Rect(392f, 790f, 132f, 154f));
                layout.hasFocus = true;
                layout.focusRect = targetRect;
                layout.targetCenter = targetRect.center;
                layout.calloutCenter = ClampInstallTutorialCallout(targetRect.center + new Vector2(-275f, -220f), 730f, 164f);
                layout.badgeText = "i";
                layout.message = "메뉴에서는 지점 이전, 설정, 타이틀 이동 같은\n관리 기능을 확인할 수 있어요.";
                layout.showArrow = true;
                layout.canTapCallout = true;
                return true;
            }

            default:
                return false;
        }
    }

    private void PrepareInstallTutorialTreadmillList()
    {
        EquipmentDefinition treadmill = FindInstallTutorialTreadmillDefinition();
        if (treadmill != null)
        {
            for (int i = 0; i < InstallCategories.Length; i++)
            {
                if (InstallCategories[i] == treadmill.Category)
                {
                    selectedCategoryIndex = i;
                    break;
                }
            }

            selectedDefinition = treadmill;
        }

        if (installPanelRoot != null && !installPanelRoot.gameObject.activeSelf)
        {
            installPanelRoot.gameObject.SetActive(true);
        }

        RefreshInstallPanel();
    }

    private RectTransform FindInstallTutorialTreadmillCard()
    {
        if (installListRoot == null)
        {
            return null;
        }

        Button[] buttons = installListRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !IsInstallTutorialTreadmillObjectName(button.name))
            {
                continue;
            }

            return button.GetComponent<RectTransform>();
        }

        return buttons.Length > 0 && buttons[0] != null ? buttons[0].GetComponent<RectTransform>() : null;
    }

    private EquipmentDefinition FindInstallTutorialTreadmillDefinition()
    {
        if (IsInstallTutorialTreadmill(selectedDefinition))
        {
            return selectedDefinition;
        }

        if (equipmentCatalog != null && equipmentCatalog.Definitions != null)
        {
            IReadOnlyList<EquipmentDefinition> definitions = equipmentCatalog.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                EquipmentDefinition definition = definitions[i];
                if (IsInstallTutorialTreadmill(definition))
                {
                    return definition;
                }
            }
        }

        return GetFirstDefinitionForCategory(EquipmentCategory.Cardio);
    }

    private Rect GetInstallTutorialNamedTargetRect(string targetName, Vector2 padding, Rect fallback)
    {
        RectTransform target = FindDeepChild(runtimeRoot, targetName)?.GetComponent<RectTransform>();
        return GetInstallTutorialTargetRect(target, padding, fallback);
    }

    private Rect GetInstallTutorialTargetRect(RectTransform target, Vector2 padding, Rect fallback)
    {
        RectTransform overlayRect = installTutorialRoot != null ? installTutorialRoot.GetComponent<RectTransform>() : null;
        if (target == null || overlayRect == null)
        {
            return fallback;
        }

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Camera eventCamera = GetInstallTutorialEventCamera();
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPoint, eventCamera, out Vector2 localPoint))
            {
                continue;
            }

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        if (float.IsInfinity(min.x) || float.IsInfinity(max.x))
        {
            return fallback;
        }

        return Rect.MinMaxRect(
            min.x - padding.x,
            min.y - padding.y,
            max.x + padding.x,
            max.y + padding.y);
    }

    private Camera GetInstallTutorialEventCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private Vector2 ClampInstallTutorialCallout(Vector2 position, float width, float height)
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        return new Vector2(
            Mathf.Clamp(position.x, InstallTutorialScreenRect.xMin + halfWidth + 20f, InstallTutorialScreenRect.xMax - halfWidth - 20f),
            Mathf.Clamp(position.y, InstallTutorialScreenRect.yMin + halfHeight + 24f, InstallTutorialScreenRect.yMax - halfHeight - 24f));
    }

    private void DrawInstallTutorialDim(InstallTutorialLayout layout)
    {
        if (!layout.hasFocus)
        {
            CreateTutorialDimBlock("InstallTutorialDimFull", InstallTutorialScreenRect);
            return;
        }

        Rect focus = ClampRectToScreen(layout.focusRect);
        float left = InstallTutorialScreenRect.xMin;
        float right = InstallTutorialScreenRect.xMax;
        float bottom = InstallTutorialScreenRect.yMin;
        float top = InstallTutorialScreenRect.yMax;

        CreateTutorialDimBlock("InstallTutorialDimTop", Rect.MinMaxRect(left, focus.yMax, right, top));
        CreateTutorialDimBlock("InstallTutorialDimBottom", Rect.MinMaxRect(left, bottom, right, focus.yMin));
        CreateTutorialDimBlock("InstallTutorialDimLeft", Rect.MinMaxRect(left, focus.yMin, focus.xMin, focus.yMax));
        CreateTutorialDimBlock("InstallTutorialDimRight", Rect.MinMaxRect(focus.xMax, focus.yMin, right, focus.yMax));
    }

    private void CreateTutorialDimBlock(string name, Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        GameObject block = CreateSolid(
            installTutorialRoot,
            name,
            new Color(0f, 0f, 0f, 0.48f),
            rect.center.x,
            rect.center.y,
            rect.width,
            rect.height,
            true);
        block.GetComponent<Image>().raycastTarget = false;
    }

    private void DrawInstallTutorialInputBlocker(List<Rect> passThroughRects)
    {
        GameObject blocker = CreateSolid(
            installTutorialRoot,
            "InstallTutorialInputBlocker",
            new Color(0f, 0f, 0f, 0.001f),
            0f,
            0f,
            1080f,
            1920f,
            true);
        Image blockerImage = blocker.GetComponent<Image>();
        blockerImage.raycastTarget = true;
        RuntimeTutorialInputMask inputMask = blocker.AddComponent<RuntimeTutorialInputMask>();
        inputMask.SetPassThrough(passThroughRects);
    }

    private void DrawInstallTutorialHighlight(Rect focusRect)
    {
        GameObject highlight = CreateGeneratedImage(
            installTutorialRoot,
            "InstallTutorialTargetHighlight",
            InstallTutorialHighlightSprite,
            focusRect.center.x,
            focusRect.center.y,
            focusRect.width,
            focusRect.height,
            false,
            true);
        Image image = highlight.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.72f);
        image.raycastTarget = false;
    }

    private void DrawInstallTutorialArrow(Vector2 targetCenter, Vector2 calloutCenter)
    {
        Vector2 direction = targetCenter - calloutCenter;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector2 arrowCenter = Vector2.Lerp(calloutCenter, targetCenter, 0.56f);
        GameObject arrow = CreateGeneratedImage(
            installTutorialRoot,
            "InstallTutorialArrow",
            InstallTutorialArrowSprite,
            arrowCenter.x,
            arrowCenter.y,
            116f,
            116f,
            true,
            true);
        RectTransform rect = arrow.GetComponent<RectTransform>();
        rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        arrow.GetComponent<Image>().raycastTarget = false;
    }

    private void DrawInstallTutorialCallout(InstallTutorialLayout layout)
    {
        GameObject box = CreateGeneratedImage(
            installTutorialRoot,
            "InstallTutorialMessageBox",
            InstallTutorialMessageBoxSprite,
            layout.calloutCenter.x,
            layout.calloutCenter.y,
            layout.calloutWidth,
            layout.calloutHeight,
            false,
            true);
        Image boxImage = box.GetComponent<Image>();
        boxImage.raycastTarget = layout.canTapCallout;

        if (layout.canTapCallout)
        {
            Button button = box.AddComponent<Button>();
            button.targetGraphic = boxImage;
            button.onClick.AddListener(AdvanceInstallTutorialMessageStep);
        }

        GameObject badge = CreateGeneratedImage(
            box.transform,
            "InstallTutorialStepBadge",
            InstallTutorialStepBadgeSprite,
            -layout.calloutWidth * 0.5f + 68f,
            layout.calloutHeight > 164f ? 26f : 0f,
            64f,
            64f,
            true,
            true);
        badge.GetComponent<Image>().raycastTarget = false;

        Text numberText = CreateInstallTutorialText(
            badge.transform,
            "InstallTutorialStepNumber",
            layout.badgeText,
            layout.badgeText == "i" ? 30 : 34,
            Color.white,
            TextAnchor.MiddleCenter,
            0f,
            0f,
            54f,
            54f);
        numberText.fontStyle = FontStyle.Normal;

        float messageY = layout.calloutHeight > 164f ? 18f : 0f;
        float messageHeight = layout.canTapCallout ? layout.calloutHeight - 64f : layout.calloutHeight - 34f;
        Text messageText = CreateInstallTutorialText(
            box.transform,
            "InstallTutorialMessage",
            layout.message,
            31,
            theme.Ink,
            TextAnchor.MiddleLeft,
            58f,
            messageY,
            layout.calloutWidth - 150f,
            messageHeight);
        messageText.lineSpacing = 0.94f;

        if (layout.canTapCallout)
        {
            CreateInstallTutorialText(
                box.transform,
                "InstallTutorialContinueHint",
                "터치해서 계속",
                20,
                theme.MutedInk,
                TextAnchor.MiddleRight,
                layout.calloutWidth * 0.5f - 128f,
                -layout.calloutHeight * 0.5f + 30f,
                170f,
                28f);
        }
    }

    private Text CreateInstallTutorialText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        Color color,
        TextAnchor alignment,
        float x,
        float y,
        float width,
        float height)
    {
        Text text = GameUiFactory.CreateText(parent, name, theme, fontSize, color, alignment, FontStyle.Normal);
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.9f;
        GameUiFactory.ApplyLightHudText(text);
        SetRect(text.rectTransform, x, y, width, height, true);
        return text;
    }

    private static Rect ClampRectToScreen(Rect rect)
    {
        return Rect.MinMaxRect(
            Mathf.Clamp(rect.xMin, InstallTutorialScreenRect.xMin, InstallTutorialScreenRect.xMax),
            Mathf.Clamp(rect.yMin, InstallTutorialScreenRect.yMin, InstallTutorialScreenRect.yMax),
            Mathf.Clamp(rect.xMax, InstallTutorialScreenRect.xMin, InstallTutorialScreenRect.xMax),
            Mathf.Clamp(rect.yMax, InstallTutorialScreenRect.yMin, InstallTutorialScreenRect.yMax));
    }

    private static Rect UnionRects(Rect a, Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    private static bool IsInstallTutorialTreadmill(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        return IsInstallTutorialTreadmillObjectName(definition.EquipmentId) ||
               IsInstallTutorialTreadmillObjectName(definition.DisplayName) ||
               IsInstallTutorialTreadmillObjectName(definition.name);
    }

    private static bool IsInstallTutorialTreadmillObjectName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.IndexOf("treadmill", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("running", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.Contains("러닝") ||
               value.Contains("트레드밀");
    }
}

internal sealed class RuntimeTutorialInputMask : MonoBehaviour, ICanvasRaycastFilter
{
    private readonly List<Rect> passThroughRects = new List<Rect>();
    private RectTransform rectTransform;

    public void SetPassThrough(List<Rect> rects)
    {
        passThroughRects.Clear();
        if (rects == null)
        {
            return;
        }

        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i].width > 0f && rects[i].height > 0f)
            {
                passThroughRects.Add(rects[i]);
            }
        }
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rectTransform == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return true;
        }

        for (int i = 0; i < passThroughRects.Count; i++)
        {
            if (passThroughRects[i].Contains(localPoint))
            {
                return false;
            }
        }

        return true;
    }
}
