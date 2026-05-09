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
        PressOperateTab,
        ExplainOperateInfo,
        PressEconomyTab,
        ExplainEconomyInfo,
        PressReviewTab,
        ExplainReviewInfo,
        PressStaffButton,
        ExplainStaff,
        PressStaffHire,
        PressMenuButton,
        ExplainMenu,
        ExplainRelocation,
        FinalMessage
    }

    private struct InstallTutorialLayout
    {
        public bool hasFocus;
        public Rect focusRect;
        public readonly List<Rect> focusRects;
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
            focusRects = new List<Rect>();
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

    private const string InstallTutorialCompletedKey = "GymInstallTutorial.Completed.v3";
    private const string InstallTutorialRootName = "InstallTutorialOverlayRoot";
    private const string InstallTutorialStepBadgeSprite = "GeneratedRuntimeUI/ui_v2/tutorial/Tutorial_StepBadge_Green";
    private const string InstallTutorialArrowSprite = "GeneratedRuntimeUI/ui_v2/tutorial/Tutorial_Arrow_Curved";
    private const string InstallTutorialMessageBoxSprite = "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base";
    private const string InstallTutorialHighlightSprite = "GeneratedRuntimeUI/ui_v2/tab_active_green_base";

    private static readonly Rect InstallTutorialScreenRect = new Rect(-540f, -960f, 1080f, 1920f);
    private static readonly string[] LegacyInstallTutorialCompletedKeys =
    {
        "GymInstallTutorial.Completed.v1",
        "GymInstallTutorial.Completed.v2"
    };

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

        ClearInstallTutorialCompletionFlags();
        PlayerPrefs.Save();
        StartInstallTutorial(force: true);
    }

    public void ResetInstallTutorial()
    {
        ClearInstallTutorialCompletionFlags();
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

    private static void ClearInstallTutorialCompletionFlags()
    {
        PlayerPrefs.DeleteKey(InstallTutorialCompletedKey);
        for (int i = 0; i < LegacyInstallTutorialCompletedKeys.Length; i++)
        {
            PlayerPrefs.DeleteKey(LegacyInstallTutorialCompletedKeys[i]);
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

        ShowInstallTutorialStep(InstallTutorialStep.PressOperateTab);
    }

    private void NotifyInstallTutorialPanelOpened(PanelMode panelMode)
    {
        if (!installTutorialRunning)
        {
            return;
        }

        if (installTutorialStep == InstallTutorialStep.PressOperateTab && panelMode == PanelMode.Operate)
        {
            ShowInstallTutorialStep(InstallTutorialStep.ExplainOperateInfo);
            return;
        }

        if (installTutorialStep == InstallTutorialStep.PressEconomyTab && panelMode == PanelMode.Economy)
        {
            ShowInstallTutorialStep(InstallTutorialStep.ExplainEconomyInfo);
            return;
        }

        if (installTutorialStep == InstallTutorialStep.PressReviewTab && panelMode == PanelMode.Review)
        {
            ShowInstallTutorialStep(InstallTutorialStep.ExplainReviewInfo);
        }
    }

    private void NotifyInstallTutorialStaffPopupOpened()
    {
        if (!installTutorialRunning || installTutorialStep != InstallTutorialStep.PressStaffButton)
        {
            return;
        }

        staffShowingApplicants = true;
        RefreshStaffPopup();
        ShowInstallTutorialStep(InstallTutorialStep.PressStaffHire);
    }

    private void NotifyInstallTutorialStaffHired()
    {
        if (!installTutorialRunning || installTutorialStep != InstallTutorialStep.PressStaffHire)
        {
            return;
        }

        CloseStaffPopup();
        ShowInstallTutorialStep(InstallTutorialStep.PressMenuButton);
    }

    private void NotifyInstallTutorialMenuOpened()
    {
        if (!installTutorialRunning || installTutorialStep != InstallTutorialStep.PressMenuButton)
        {
            return;
        }

        ShowInstallTutorialStep(InstallTutorialStep.ExplainMenu);
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
                ShowInstallTutorialStep(InstallTutorialStep.PressEconomyTab);
                break;
            case InstallTutorialStep.ExplainEconomyInfo:
                ShowInstallTutorialStep(InstallTutorialStep.PressReviewTab);
                break;
            case InstallTutorialStep.ExplainReviewInfo:
                ShowInstallTutorialStep(InstallTutorialStep.PressStaffButton);
                break;
            case InstallTutorialStep.ExplainMenu:
                ShowInstallTutorialStep(InstallTutorialStep.ExplainRelocation);
                break;
            case InstallTutorialStep.ExplainRelocation:
                CloseRuntimeMenuPopups();
                ShowInstallTutorialStep(InstallTutorialStep.FinalMessage);
                break;
            case InstallTutorialStep.FinalMessage:
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

        if (!TryResolveInstallTutorialLayout(step, out InstallTutorialLayout layout) &&
            !TryResolveAdditionalInstallTutorialLayout(step, out layout))
        {
            installTutorialRoot.gameObject.SetActive(false);
            return;
        }

        ApplyInstallTutorialLayoutFixups(step, ref layout);

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

    private bool TryResolveAdditionalInstallTutorialLayout(InstallTutorialStep step, out InstallTutorialLayout layout)
    {
        layout = new InstallTutorialLayout(true);

        switch (step)
        {
            case InstallTutorialStep.PressOperateTab:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("OperateTabButton", new Vector2(14f, 14f), new Rect(-520f, -940f, 260f, 146f));
                ConfigureFocusedActionStep(
                    ref layout,
                    targetRect,
                    targetRect.center + new Vector2(220f, 198f),
                    "4",
                    "운영 탭을 눌러 현재 헬스장 상태를 확인해보세요",
                    700f,
                    142f,
                    showHighlight: false);
                return true;
            }

            case InstallTutorialStep.PressEconomyTab:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("EconomyTabButton", new Vector2(14f, 14f), new Rect(-8f, -940f, 260f, 146f));
                ConfigureFocusedActionStep(
                    ref layout,
                    targetRect,
                    targetRect.center + new Vector2(0f, 198f),
                    "5",
                    "경제 탭을 눌러 수입과 지출 흐름을 살펴보세요",
                    700f,
                    142f,
                    showHighlight: false);
                return true;
            }

            case InstallTutorialStep.ExplainEconomyInfo:
            {
                Rect targetRect = GetSharedPanelTutorialRect();
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, 565f),
                    "i",
                    "경제 패널에서는 자금 변화와 수익 흐름을 확인할 수 있어요.",
                    760f,
                    150f);
                return true;
            }

            case InstallTutorialStep.PressReviewTab:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("ReviewTabButton", new Vector2(14f, 14f), new Rect(248f, -940f, 260f, 146f));
                ConfigureFocusedActionStep(
                    ref layout,
                    targetRect,
                    targetRect.center + new Vector2(-210f, 198f),
                    "6",
                    "리뷰 탭을 눌러 회원 반응과 평가를 확인해보세요",
                    700f,
                    142f,
                    showHighlight: false);
                return true;
            }

            case InstallTutorialStep.ExplainReviewInfo:
            {
                Rect targetRect = GetSharedPanelTutorialRect();
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, 565f),
                    "i",
                    "리뷰 패널에서는 만족도, 신규 후기, 추천 비율을 볼 수 있어요.",
                    760f,
                    150f);
                return true;
            }

            case InstallTutorialStep.PressStaffButton:
            {
                Rect targetRect = GetInstallTutorialTightNamedTargetRect("StaffButton", new Rect(300f, 805f, 108f, 128f));
                ConfigureFocusedActionStep(
                    ref layout,
                    targetRect,
                    targetRect.center + new Vector2(-260f, -220f),
                    "7",
                    "직원 버튼을 눌러 직원 채용 화면을 열어보세요",
                    730f,
                    150f,
                    showHighlight: false);
                return true;
            }

            case InstallTutorialStep.PressStaffHire:
            {
                Rect targetRect = GetInstallTutorialTargetRect(FindInstallTutorialStaffHireButton(), new Vector2(14f, 14f), new Rect(182f, -60f, 180f, 92f));
                ConfigureFocusedActionStep(
                    ref layout,
                    targetRect,
                    targetRect.center + new Vector2(-260f, 190f),
                    "8",
                    "채용 버튼을 눌러 첫 직원을 고용해보세요",
                    700f,
                    142f,
                    showHighlight: true);
                return true;
            }

            case InstallTutorialStep.PressMenuButton:
            {
                Rect targetRect = GetInstallTutorialTightNamedTargetRect("MenuButton", new Rect(406f, 805f, 108f, 128f));
                ConfigureFocusedActionStep(
                    ref layout,
                    targetRect,
                    targetRect.center + new Vector2(-275f, -220f),
                    "9",
                    "메뉴 버튼을 눌러 관리 기능을 확인해보세요",
                    730f,
                    150f,
                    showHighlight: false);
                return true;
            }

            case InstallTutorialStep.ExplainRelocation:
            {
                Rect targetRect = GetInstallTutorialNamedTargetRect("RelocateButton", Vector2.zero, new Rect(-320f, 120f, 640f, 104f));
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, -720f),
                    "i",
                    "지점 이전에서는 더 넓은 지역으로 옮겨 성장 기회를 만들 수 있어요.",
                    780f,
                    130f);
                return true;
            }

            case InstallTutorialStep.FinalMessage:
                layout.badgeText = "!";
                layout.message = "기본 안내는 여기까지예요.\n이제 헬스장을 직접 운영해보세요!";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 760f;
                layout.calloutHeight = 176f;
                layout.canTapCallout = true;
                return true;

            default:
                return false;
        }
    }

    private void ApplyInstallTutorialLayoutFixups(InstallTutorialStep step, ref InstallTutorialLayout layout)
    {
        switch (step)
        {
            case InstallTutorialStep.IntroGreeting:
                layout.badgeText = "!";
                layout.message = "어서 오세요!\n오늘부터 작은 헬스장을 함께 운영해볼 거예요.";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 760f;
                layout.calloutHeight = 190f;
                layout.canTapCallout = true;
                break;

            case InstallTutorialStep.IntroGameGoal:
                layout.badgeText = "!";
                layout.message = "기구를 설치하고, 회원을 늘리고,\n자금과 평판을 키우는 경영 게임이에요.";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 780f;
                layout.calloutHeight = 190f;
                layout.canTapCallout = true;
                break;

            case InstallTutorialStep.IntroFirstAction:
                layout.badgeText = "!";
                layout.message = "그럼 첫 번째 기구를 설치해볼까요?";
                layout.calloutCenter = new Vector2(0f, -120f);
                layout.calloutWidth = 700f;
                layout.calloutHeight = 156f;
                layout.canTapCallout = true;
                break;

            case InstallTutorialStep.PressInstallTab:
                layout.message = "설치 탭을 눌러 기구 목록을 열어보세요";
                EnsureLayoutHasFocusRects(ref layout);
                break;

            case InstallTutorialStep.SelectTreadmill:
                layout.message = "러닝머신을 선택해 배치 준비를 해보세요";
                EnsureLayoutHasFocusRects(ref layout);
                break;

            case InstallTutorialStep.PlaceTreadmill:
                ConfigurePlaceTreadmillStep(ref layout);
                break;

            case InstallTutorialStep.ExplainOperateInfo:
            {
                Rect targetRect = GetSharedPanelTutorialRect();
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, 565f),
                    "i",
                    "운영 패널에서는 목표, 운영 상태, 청결, 기구 사용률,\n직원 상태와 오늘 수익을 한눈에 볼 수 있어요.",
                    780f,
                    170f);
                break;
            }

            case InstallTutorialStep.ExplainMenu:
            {
                Rect targetRect = GetInstallTutorialTargetRect(
                    menuPopupRoot != null ? menuPopupRoot.GetComponent<RectTransform>() : null,
                    new Vector2(10f, 10f),
                    new Rect(-390f, -500f, 780f, 1040f));
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, -720f),
                    "i",
                    "메뉴에서는 지점 이전, 설정, 타이틀 이동 같은 관리 기능을 사용할 수 있어요.",
                    780f,
                    130f);
                break;
            }

            default:
                EnsureLayoutHasFocusRects(ref layout);
                break;
        }
    }

    private void ConfigureFocusedActionStep(
        ref InstallTutorialLayout layout,
        Rect targetRect,
        Vector2 calloutCenter,
        string badgeText,
        string message,
        float calloutWidth,
        float calloutHeight,
        bool showHighlight)
    {
        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, targetRect);
        layout.passThroughRects.Clear();
        layout.passThroughRects.Add(targetRect);
        layout.targetCenter = targetRect.center;
        layout.calloutWidth = calloutWidth;
        layout.calloutHeight = calloutHeight;
        layout.calloutCenter = ClampInstallTutorialCallout(calloutCenter, calloutWidth, calloutHeight);
        layout.badgeText = badgeText;
        layout.message = message;
        layout.showArrow = true;
        layout.showTargetHighlight = showHighlight;
        layout.canTapCallout = false;
    }

    private void ConfigureFocusedMessageStep(
        ref InstallTutorialLayout layout,
        Rect targetRect,
        Vector2 calloutCenter,
        string badgeText,
        string message,
        float calloutWidth,
        float calloutHeight)
    {
        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, targetRect);
        layout.passThroughRects.Clear();
        layout.targetCenter = targetRect.center;
        layout.calloutWidth = calloutWidth;
        layout.calloutHeight = calloutHeight;
        layout.calloutCenter = ClampInstallTutorialCallout(calloutCenter, calloutWidth, calloutHeight);
        layout.badgeText = badgeText;
        layout.message = message;
        layout.showArrow = true;
        layout.showTargetHighlight = false;
        layout.canTapCallout = true;
    }

    private void ConfigurePlaceTreadmillStep(ref InstallTutorialLayout layout)
    {
        Rect fallbackGridRect = new Rect(-452f, -248f, 904f, 520f);
        List<Rect> cellRects = GetInstallTutorialGridCellFocusRects(fallbackGridRect, out Rect gridBounds);
        Rect actionRect = GetInstallTutorialTargetRect(
            placementActionRoot != null ? placementActionRoot.GetComponent<RectTransform>() : null,
            new Vector2(18f, 18f),
            new Rect(-376f, -720f, 752f, 324f));

        ResetInstallTutorialFocus(ref layout);
        if (cellRects.Count > 0)
        {
            for (int i = 0; i < cellRects.Count; i++)
            {
                AddInstallTutorialFocusRect(ref layout, cellRects[i]);
            }
        }
        else
        {
            AddInstallTutorialFocusRect(ref layout, fallbackGridRect);
            gridBounds = fallbackGridRect;
        }

        AddInstallTutorialFocusRect(ref layout, actionRect);

        layout.passThroughRects.Clear();
        layout.passThroughRects.Add(gridBounds);
        layout.passThroughRects.Add(actionRect);
        layout.targetCenter = gridBounds.center + new Vector2(30f, 30f);
        layout.calloutWidth = 700f;
        layout.calloutHeight = 142f;
        layout.calloutCenter = ClampInstallTutorialCallout(gridBounds.center + new Vector2(210f, 285f), layout.calloutWidth, layout.calloutHeight);
        layout.badgeText = "3";
        layout.message = "빈 공간을 누른 뒤 설치 버튼으로 배치하세요";
        layout.showArrow = true;
        layout.showTargetHighlight = false;
        layout.canTapCallout = false;
    }

    private Rect GetSharedPanelTutorialRect()
    {
        return GetInstallTutorialTargetRect(
            sharedPanelRoot != null ? sharedPanelRoot.GetComponent<RectTransform>() : null,
            new Vector2(8f, 8f),
            new Rect(-520f, -600f, 1040f, 752f));
    }

    private RectTransform FindInstallTutorialStaffHireButton()
    {
        if (staffPopupRoot == null)
        {
            return null;
        }

        Button[] buttons = staffPopupRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.name == "StaffActionButton")
            {
                return button.GetComponent<RectTransform>();
            }
        }

        return null;
    }

    private void EnsureLayoutHasFocusRects(ref InstallTutorialLayout layout)
    {
        if (!layout.hasFocus || layout.focusRects.Count > 0)
        {
            return;
        }

        AddInstallTutorialFocusRect(ref layout, layout.focusRect);
    }

    private static void ResetInstallTutorialFocus(ref InstallTutorialLayout layout)
    {
        layout.hasFocus = false;
        layout.focusRect = Rect.zero;
        layout.focusRects.Clear();
    }

    private static void AddInstallTutorialFocusRect(ref InstallTutorialLayout layout, Rect rect)
    {
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        Rect clamped = ClampRectToScreen(rect);
        layout.focusRects.Add(clamped);
        layout.focusRect = layout.hasFocus ? UnionRects(layout.focusRect, clamped) : clamped;
        layout.hasFocus = true;
    }

    private List<Rect> GetInstallTutorialGridCellFocusRects(Rect fallbackGridRect, out Rect gridBounds)
    {
        List<Rect> rects = new List<Rect>();
        gridBounds = fallbackGridRect;

        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            return rects;
        }

        bool hasBounds = false;
        Rect bounds = Rect.zero;
        float halfSize = Mathf.Max(0.01f, gridManager.CellSize * 0.46f);

        for (int y = 0; y < gridManager.Height; y++)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell == null)
                {
                    continue;
                }

                if (!TryGetInstallTutorialWorldRect(cell.transform.position, halfSize, out Rect cellRect))
                {
                    continue;
                }

                rects.Add(cellRect);
                bounds = hasBounds ? UnionRects(bounds, cellRect) : cellRect;
                hasBounds = true;
            }
        }

        if (hasBounds)
        {
            gridBounds = bounds;
        }

        return rects;
    }

    private bool TryGetInstallTutorialWorldRect(Vector3 worldCenter, float halfSize, out Rect rect)
    {
        rect = Rect.zero;

        Vector3[] worldCorners =
        {
            new Vector3(worldCenter.x - halfSize, worldCenter.y - halfSize, worldCenter.z),
            new Vector3(worldCenter.x - halfSize, worldCenter.y + halfSize, worldCenter.z),
            new Vector3(worldCenter.x + halfSize, worldCenter.y - halfSize, worldCenter.z),
            new Vector3(worldCenter.x + halfSize, worldCenter.y + halfSize, worldCenter.z)
        };

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < worldCorners.Length; i++)
        {
            if (!TryWorldPointToInstallTutorialLocal(worldCorners[i], out Vector2 localPoint))
            {
                continue;
            }

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        if (float.IsInfinity(min.x) || float.IsInfinity(max.x))
        {
            return false;
        }

        rect = ClampRectToScreen(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
        return rect.width > 1f && rect.height > 1f;
    }

    private bool TryWorldPointToInstallTutorialLocal(Vector3 worldPoint, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        RectTransform overlayRect = installTutorialRoot != null ? installTutorialRoot.GetComponent<RectTransform>() : null;
        if (overlayRect == null)
        {
            return false;
        }

        Camera worldCamera = Camera.main != null ? Camera.main : GetInstallTutorialEventCamera();
        if (worldCamera == null)
        {
            return false;
        }

        Vector2 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRect,
            screenPoint,
            GetInstallTutorialEventCamera(),
            out localPoint);
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

    private Rect GetInstallTutorialTightNamedTargetRect(string targetName, Rect fallback)
    {
        return GetInstallTutorialTargetRect(
            FindDeepChild(runtimeRoot, targetName)?.GetComponent<RectTransform>(),
            Vector2.zero,
            fallback);
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

        List<Rect> dimRects = new List<Rect> { InstallTutorialScreenRect };
        List<Rect> focusRects = layout.focusRects.Count > 0
            ? layout.focusRects
            : new List<Rect> { layout.focusRect };

        for (int i = 0; i < focusRects.Count; i++)
        {
            Rect hole = ClampRectToScreen(focusRects[i]);
            if (hole.width <= 0f || hole.height <= 0f)
            {
                continue;
            }

            List<Rect> nextRects = new List<Rect>();
            for (int j = 0; j < dimRects.Count; j++)
            {
                SubtractRect(dimRects[j], hole, nextRects);
            }

            dimRects = nextRects;
        }

        for (int i = 0; i < dimRects.Count; i++)
        {
            CreateTutorialDimBlock($"InstallTutorialDim_{i}", dimRects[i]);
        }
    }

    private static void SubtractRect(Rect source, Rect hole, List<Rect> results)
    {
        float overlapMinX = Mathf.Max(source.xMin, hole.xMin);
        float overlapMaxX = Mathf.Min(source.xMax, hole.xMax);
        float overlapMinY = Mathf.Max(source.yMin, hole.yMin);
        float overlapMaxY = Mathf.Min(source.yMax, hole.yMax);

        if (overlapMinX >= overlapMaxX || overlapMinY >= overlapMaxY)
        {
            results.Add(source);
            return;
        }

        AddRectIfVisible(results, Rect.MinMaxRect(source.xMin, overlapMaxY, source.xMax, source.yMax));
        AddRectIfVisible(results, Rect.MinMaxRect(source.xMin, source.yMin, source.xMax, overlapMinY));
        AddRectIfVisible(results, Rect.MinMaxRect(source.xMin, overlapMinY, overlapMinX, overlapMaxY));
        AddRectIfVisible(results, Rect.MinMaxRect(overlapMaxX, overlapMinY, source.xMax, overlapMaxY));
    }

    private static void AddRectIfVisible(List<Rect> rects, Rect rect)
    {
        if (rect.width > 0.5f && rect.height > 0.5f)
        {
            rects.Add(rect);
        }
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
