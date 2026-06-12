using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class RuntimeGameUIController
{
    public void JumpToTutorialStep(int stepIndex)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[InstallTutorial] JumpToTutorialStep is available only in Play Mode.", this);
            return;
        }

        InstallTutorialStep[] orderedSteps = GetInstallTutorialOrderedSteps();
        if (orderedSteps.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(stepIndex, 1, orderedSteps.Length);
        installTutorialDebugStepIndex = clampedIndex;
        BindInstallTutorialEvents();
        installTutorialRunning = true;
        PrepareInstallTutorialStepContext(orderedSteps[clampedIndex - 1]);
        ShowInstallTutorialStep(orderedSteps[clampedIndex - 1]);
    }

    public void RebuildFocusNow()
    {
        if (Application.isPlaying && installTutorialRunning && installTutorialStep != InstallTutorialStep.None)
        {
            ShowInstallTutorialStep(installTutorialStep, forceRebuild: true);
        }
    }

    public void ToggleFocusDebugOutline()
    {
        installTutorialDebugOutlineVisible = !installTutorialDebugOutlineVisible;
        if (installTutorialDimmer != null)
        {
            installTutorialDimmer.SetDebugOutlineVisible(installTutorialDebugOutlineVisible);
        }
    }

    public void LogCurrentFocusRect()
    {
        if (installTutorialDimmer != null)
        {
            installTutorialDimmer.LogCurrentFocusRect();
            return;
        }

        Debug.Log("[InstallTutorial] TutorialDimmer is not active.", this);
    }

    private void LogInstallTutorialRuntimeRoute(string source)
    {
        string projectPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
        string scriptPath = System.IO.Path.Combine(projectPath, "Assets", "_Project", "Scripts", "Runtime", "UI", "RuntimeInstallTutorialController.cs");
        string rootName = installTutorialRoot != null ? installTutorialRoot.name : "null";
        string runtimeRootName = runtimeRoot != null ? runtimeRoot.name : "null";
        Debug.Log($"[InstallTutorialTrace] {source} projectPath=\"{projectPath}\" scriptPath=\"{scriptPath}\" this=\"{name}\" scene=\"{gameObject.scene.name}\" activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy} runtimeRoot=\"{runtimeRootName}\" installTutorialRoot=\"{rootName}\"", this);

        RuntimeGameUIController[] controllers = UnityEngine.Object.FindObjectsByType<RuntimeGameUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[InstallTutorialTrace] {source} RuntimeInstallTutorialController runtime instance count={controllers.Length}", this);
        for (int i = 0; i < controllers.Length; i++)
        {
            RuntimeGameUIController controller = controllers[i];
            Debug.Log($"[InstallTutorialTrace] {source} RuntimeInstallTutorialController instance[{i}] go=\"{controller.gameObject.name}\" scene=\"{controller.gameObject.scene.name}\" activeSelf={controller.gameObject.activeSelf} activeInHierarchy={controller.gameObject.activeInHierarchy}", controller);
        }

        TutorialDimmerController[] dimmers = UnityEngine.Object.FindObjectsByType<TutorialDimmerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[InstallTutorialTrace] {source} TutorialDimmerController instance count={dimmers.Length}", this);
        for (int i = 0; i < dimmers.Length; i++)
        {
            TutorialDimmerController dimmer = dimmers[i];
            Debug.Log($"[InstallTutorialTrace] {source} TutorialDimmerController instance[{i}] go=\"{dimmer.gameObject.name}\" scene=\"{dimmer.gameObject.scene.name}\" activeSelf={dimmer.gameObject.activeSelf} activeInHierarchy={dimmer.gameObject.activeInHierarchy}", dimmer);
        }

        int installRootCount = 0;
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != InstallTutorialRootName)
            {
                continue;
            }

            Debug.Log($"[InstallTutorialTrace] {source} installTutorialRoot instance[{installRootCount}] go=\"{candidate.gameObject.name}\" scene=\"{candidate.gameObject.scene.name}\" activeSelf={candidate.gameObject.activeSelf} activeInHierarchy={candidate.gameObject.activeInHierarchy} path=\"{GetInstallTutorialTransformPath(candidate)}\"", candidate);
            installRootCount++;
        }

        Debug.Log($"[InstallTutorialTrace] {source} installTutorialRoot instance count={installRootCount}", this);
    }

    private static string GetInstallTutorialTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "null";
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

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
        public Vector2 targetCenter;
        public Vector2 calloutCenter;
        public string badgeText;
        public string message;
        public float calloutWidth;
        public float calloutHeight;
        public bool showArrow;
        public Vector2 arrowOffset;
        public Vector2 arrowSizeDelta;
        public float arrowRotationOffset;
        public bool hasArrowTargetRect;
        public Rect arrowTargetRect;
        public bool hasArrowOverride;
        public Vector2 arrowOverrideCenter;
        public Vector2 arrowOverrideSize;
        public float arrowOverrideRotation;
        public bool canTapCallout;
        public TutorialFocusMode focusMode;
        public string focusTargetName;
        public string focusSource;
        public bool allowFocusInteraction;

        public InstallTutorialLayout(bool initialize)
        {
            hasFocus = false;
            focusRect = Rect.zero;
            focusRects = new List<Rect>();
            targetCenter = Vector2.zero;
            calloutCenter = Vector2.zero;
            badgeText = string.Empty;
            message = string.Empty;
            calloutWidth = 650f;
            calloutHeight = 142f;
            showArrow = false;
            arrowOffset = Vector2.zero;
            arrowSizeDelta = Vector2.zero;
            arrowRotationOffset = 0f;
            hasArrowTargetRect = false;
            arrowTargetRect = Rect.zero;
            hasArrowOverride = false;
            arrowOverrideCenter = Vector2.zero;
            arrowOverrideSize = Vector2.zero;
            arrowOverrideRotation = 0f;
            canTapCallout = false;
            focusMode = TutorialFocusMode.None;
            focusTargetName = string.Empty;
            focusSource = string.Empty;
            allowFocusInteraction = false;
        }
    }

    private const string InstallTutorialCompletedKey = "GymInstallTutorial.Completed.v9";
    private const string InstallTutorialRootName = "InstallTutorialOverlayRoot";
    private const string InstallTutorialDimmerRootName = "InstallTutorialDimmerRoot";
    private const string InstallTutorialContentRootName = "InstallTutorialContentRoot";
    private const string InstallTutorialStepBadgeSprite = "GeneratedRuntimeUI/ui_v2/tutorial/Tutorial_StepBadge_Green";
    private const string InstallTutorialArrowSprite = "GeneratedRuntimeUI/ui_v2/tutorial/Tutorial_Arrow_Pointer_Down_v2";
    private const string InstallTutorialMessageBoxSprite = "GeneratedRuntimeUI/ui_v2/staff/staff_list_row_base";
    private const int InstallTutorialOverlaySortingOrder = 32000;
    private const int InstallTutorialPlacementActionSortingOrder = InstallTutorialOverlaySortingOrder + 1;
    private const int InstallTutorialContentSortingOrder = InstallTutorialOverlaySortingOrder + 2;

    private static readonly Rect InstallTutorialScreenRect = new Rect(-540f, -960f, 1080f, 1920f);
    private static readonly Rect InstallTutorialSharedPanelAreaFallbackRect = new Rect(-520f, -788f, 1040f, 752f);
    private static readonly Rect InstallTutorialOperatePanelAreaFallbackRect = new Rect(-520f, -788f, 1040f, 650f);
    private static readonly Vector2 InstallTutorialArrowSize = new Vector2(72f, 72f);
    private const float InstallTutorialArrowTargetGap = 44f;

    [Header("Install Tutorial Panel Focus")]
    [HideInInspector]
    [SerializeField] private bool installTutorialUseManualSharedPanelFocus;
    [HideInInspector]
    [SerializeField] private Vector2 installTutorialSharedPanelFocusCenter = new Vector2(0f, -224f);
    [HideInInspector]
    [SerializeField] private Vector2 installTutorialSharedPanelFocusSize = new Vector2(1040f, 752f);
    [HideInInspector]
    [SerializeField] private Vector2 installTutorialSharedPanelFocusOffset = Vector2.zero;
    [HideInInspector]
    [SerializeField] private Vector2 installTutorialSharedPanelFocusSizeDelta = Vector2.zero;
    [HideInInspector]
    [SerializeField] private Vector2 installTutorialOperateCalloutCenter = new Vector2(0f, -820f);
    [HideInInspector]
    [SerializeField] private Vector2 installTutorialOperateCalloutSize = new Vector2(780f, 150f);
    private static readonly string[] LegacyInstallTutorialCompletedKeys =
    {
        "GymInstallTutorial.Completed.v1",
        "GymInstallTutorial.Completed.v2",
        "GymInstallTutorial.Completed.v3",
        "GymInstallTutorial.Completed.v4",
        "GymInstallTutorial.Completed.v5",
        "GymInstallTutorial.Completed.v6",
        "GymInstallTutorial.Completed.v7",
        "GymInstallTutorial.Completed.v8"
    };

    private Transform installTutorialRoot;
    private Transform installTutorialContentRoot;
    private TutorialDimmerController installTutorialDimmer;
    private Transform installTutorialLiftedPlacementActionRoot;
    private Transform installTutorialPlacementActionOriginalParent;
    private Canvas installTutorialPlacementActionCanvas;
    private GraphicRaycaster installTutorialPlacementActionRaycaster;
    private Transform installTutorialLiftedPanelRoot;
    private Transform installTutorialPanelOriginalParent;
    private Canvas installTutorialPanelCanvas;
    private GraphicRaycaster installTutorialPanelRaycaster;
    private PlacementManager installTutorialBoundPlacementManager;
    private InstallTutorialStep installTutorialStep = InstallTutorialStep.None;
    private InstallTutorialStep currentVisibleTutorialStep = InstallTutorialStep.None;
    private bool installTutorialRunning;
    private bool installTutorialDebugOutlineVisible;
    private bool installTutorialPlacementActionLifted;
    private bool installTutorialPlacementActionHadCanvas;
    private bool installTutorialPlacementActionHadRaycaster;
    private bool installTutorialPlacementActionOriginalOverrideSorting;
    private bool installTutorialPanelRootLifted;
    private bool installTutorialPanelHadCanvas;
    private bool installTutorialPanelHadRaycaster;
    private bool installTutorialPanelOriginalOverrideSorting;
    private int installTutorialPlacementActionOriginalSortingOrder;
    private int installTutorialPlacementActionOriginalSortingLayerID;
    private int installTutorialPlacementActionOriginalSiblingIndex = -1;
    private int installTutorialPanelOriginalSortingOrder;
    private int installTutorialPanelOriginalSortingLayerID;
    private int installTutorialPanelOriginalSiblingIndex = -1;
    [SerializeField, HideInInspector] private int installTutorialDebugStepIndex = 1;
    private bool installTutorialLastHadPlacementArea;
    private int installTutorialLastAnchorX = -1;
    private int installTutorialLastAnchorY = -1;
    private int installTutorialLastFootprintWidth = -1;
    private int installTutorialLastFootprintHeight = -1;
    private bool installTutorialLoggedDynamicStepEntry;
    private string installTutorialLastDynamicTraceSignature = string.Empty;
    private Coroutine installTutorialPanelFocusRetryCoroutine;
    private InstallTutorialStep installTutorialPanelFocusRetryStep = InstallTutorialStep.None;
    private int installTutorialPanelFocusRetryCount;
    private bool installTutorialLastSharedPanelAreaFallbackUsed;

    public void StartInstallTutorialForDebug()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[InstallTutorial] 튜토리얼 시작은 Play Mode에서 확인해 주세요.", this);
            return;
        }

        Debug.Log("[InstallTutorialTrace] StartInstallTutorialForDebug invoked. Inspector/ContextMenu route calls StartInstallTutorial(force:true).", this);
        LogInstallTutorialRuntimeRoute("StartInstallTutorialForDebug");
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

    public string GetInstallTutorialActiveStepNameForEditor()
    {
        return installTutorialRunning ? installTutorialStep.ToString() : InstallTutorialStep.None.ToString();
    }

    public bool HasActiveInstallTutorialStepForEditor()
    {
        return installTutorialRunning && installTutorialStep != InstallTutorialStep.None;
    }

    private void InitializeInstallTutorial()
    {
        Debug.Log($"[InstallTutorialTrace] InitializeInstallTutorial enter isPlaying={Application.isPlaying} runtimeRoot={(runtimeRoot != null ? runtimeRoot.name : "null")}", this);
        if (!Application.isPlaying || runtimeRoot == null)
        {
            Debug.Log("[InstallTutorialTrace] InitializeInstallTutorial aborted before binding because Play Mode or runtimeRoot is missing.", this);
            return;
        }

        LogInstallTutorialRuntimeRoute("InitializeInstallTutorial");
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
        Debug.Log($"[InstallTutorialTrace] StartInstallTutorial enter force={force} isPlaying={Application.isPlaying} runtimeRoot={(runtimeRoot != null ? runtimeRoot.name : "null")} completed={PlayerPrefs.GetInt(InstallTutorialCompletedKey, 0)}", this);
        if (!Application.isPlaying || runtimeRoot == null)
        {
            Debug.Log("[InstallTutorialTrace] StartInstallTutorial aborted because Play Mode or runtimeRoot is missing.", this);
            return;
        }

        if (!force && PlayerPrefs.GetInt(InstallTutorialCompletedKey, 0) != 0)
        {
            Debug.Log("[InstallTutorialTrace] StartInstallTutorial aborted because tutorial completion flag is set.", this);
            return;
        }

        LogInstallTutorialRuntimeRoute("StartInstallTutorial");
        BindInstallTutorialEvents();
        CleanupInstallTutorialPanelOverlayArtifacts("StartInstallTutorial");
        installTutorialRunning = true;
        currentVisibleTutorialStep = InstallTutorialStep.None;
        installTutorialLoggedDynamicStepEntry = false;
        installTutorialLastDynamicTraceSignature = string.Empty;
        ShowInstallTutorialStep(InstallTutorialStep.IntroGreeting);
    }

    private void EndInstallTutorial(bool markCompleted)
    {
        RestoreInstallTutorialPlacementActionOverlay("EndInstallTutorial");
        CleanupInstallTutorialPanelOverlayArtifacts("EndInstallTutorial");

        if (installTutorialPanelFocusRetryCoroutine != null)
        {
            StopCoroutine(installTutorialPanelFocusRetryCoroutine);
            installTutorialPanelFocusRetryCoroutine = null;
        }

        installTutorialPanelFocusRetryStep = InstallTutorialStep.None;
        installTutorialPanelFocusRetryCount = 0;
        installTutorialRunning = false;
        installTutorialStep = InstallTutorialStep.None;
        currentVisibleTutorialStep = InstallTutorialStep.None;
        installTutorialLastHadPlacementArea = false;
        installTutorialLastAnchorX = -1;
        installTutorialLastAnchorY = -1;
        installTutorialLastFootprintWidth = -1;
        installTutorialLastFootprintHeight = -1;
        installTutorialLoggedDynamicStepEntry = false;
        installTutorialLastDynamicTraceSignature = string.Empty;

        if (markCompleted)
        {
            PlayerPrefs.SetInt(InstallTutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }

        if (installTutorialRoot != null)
        {
            if (installTutorialDimmer != null)
            {
                installTutorialDimmer.Hide();
            }

            if (installTutorialContentRoot != null)
            {
                GameUiFactory.ClearChildren(installTutorialContentRoot);
            }

            installTutorialRoot.gameObject.SetActive(false);
        }

        installTutorialDimmer = null;
    }

    private void RefreshInstallTutorialDynamicStep()
    {
        if (!installTutorialRunning ||
            installTutorialStep != InstallTutorialStep.PlaceTreadmill)
        {
            return;
        }

        if (installTutorialRoot == null ||
            !installTutorialRoot.gameObject.activeInHierarchy ||
            placementManager == null)
        {
            string inactiveSignature = $"inactive|root={(installTutorialRoot != null ? installTutorialRoot.name : "null")}|rootActive={(installTutorialRoot != null && installTutorialRoot.gameObject.activeInHierarchy)}|placement={(placementManager != null ? placementManager.name : "null")}";
            if (installTutorialLastDynamicTraceSignature != inactiveSignature)
            {
                Debug.Log($"[InstallTutorialTrace] RefreshInstallTutorialDynamicStep skipped in Step6 rootOrPlacementMissing {inactiveSignature}", this);
                installTutorialLastDynamicTraceSignature = inactiveSignature;
            }

            return;
        }

        bool hasPlacementArea = placementManager.TryGetCurrentPlacementArea(
            out int anchorX,
            out int anchorY,
            out int footprintWidth,
            out int footprintHeight);

        bool placementAreaChanged =
            hasPlacementArea != installTutorialLastHadPlacementArea ||
            anchorX != installTutorialLastAnchorX ||
            anchorY != installTutorialLastAnchorY ||
            footprintWidth != installTutorialLastFootprintWidth ||
            footprintHeight != installTutorialLastFootprintHeight;

        int rootChildCount = installTutorialRoot != null ? installTutorialRoot.childCount : -1;
        bool dimmerExists = installTutorialDimmer != null;
        bool dimmerActive = installTutorialDimmer != null && installTutorialDimmer.gameObject.activeInHierarchy;
        string dimmerPanels = installTutorialDimmer != null ? installTutorialDimmer.GetDebugPanelStateSummary() : "dimmer=null";
        string dynamicSignature = $"{hasPlacementArea}|{anchorX}|{anchorY}|{footprintWidth}|{footprintHeight}|{rootChildCount}|{dimmerExists}|{dimmerActive}|{dimmerPanels}";

        if (!installTutorialLoggedDynamicStepEntry ||
            placementAreaChanged ||
            installTutorialLastDynamicTraceSignature != dynamicSignature)
        {
            Debug.Log($"[InstallTutorialTrace] RefreshInstallTutorialDynamicStep enter step={installTutorialStep} visibleStep={currentVisibleTutorialStep} placementAreaChanged={placementAreaChanged} hasPlacementArea={hasPlacementArea} hoverOrAnchor=({anchorX},{anchorY}) footprint={footprintWidth}x{footprintHeight} rootChildCount={rootChildCount} dimmerExists={dimmerExists} dimmerActive={dimmerActive} dimmerPanels={dimmerPanels}. This method does not call ShowInstallTutorialStep.", this);
            installTutorialLoggedDynamicStepEntry = true;
            installTutorialLastDynamicTraceSignature = dynamicSignature;
        }

        installTutorialLastHadPlacementArea = hasPlacementArea;
        installTutorialLastAnchorX = anchorX;
        installTutorialLastAnchorY = anchorY;
        installTutorialLastFootprintWidth = footprintWidth;
        installTutorialLastFootprintHeight = footprintHeight;

        Transform actionRoot = FindInstallTutorialPlacementActionRoot();
        if (actionRoot != null && actionRoot.gameObject.activeInHierarchy)
        {
            LiftInstallTutorialPlacementActionOverlayForStep6();
        }
        else if (installTutorialPlacementActionLifted)
        {
            RestoreInstallTutorialPlacementActionOverlay("Step6PlacementActionInactive");
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

    private InstallTutorialStep[] GetInstallTutorialOrderedSteps()
    {
        return new[]
        {
            InstallTutorialStep.IntroGreeting,
            InstallTutorialStep.IntroGameGoal,
            InstallTutorialStep.IntroFirstAction,
            InstallTutorialStep.PressInstallTab,
            InstallTutorialStep.SelectTreadmill,
            InstallTutorialStep.PlaceTreadmill,
            InstallTutorialStep.PressOperateTab,
            InstallTutorialStep.ExplainOperateInfo,
            InstallTutorialStep.PressEconomyTab,
            InstallTutorialStep.ExplainEconomyInfo,
            InstallTutorialStep.PressReviewTab,
            InstallTutorialStep.ExplainReviewInfo,
            InstallTutorialStep.PressStaffButton,
            InstallTutorialStep.ExplainStaff,
            InstallTutorialStep.PressStaffHire,
            InstallTutorialStep.PressMenuButton,
            InstallTutorialStep.ExplainMenu,
            InstallTutorialStep.ExplainRelocation,
            InstallTutorialStep.FinalMessage
        };
    }

    private void PrepareInstallTutorialStepContext(InstallTutorialStep step)
    {
        switch (step)
        {
            case InstallTutorialStep.SelectTreadmill:
            case InstallTutorialStep.PlaceTreadmill:
                ShowInstallPanel();
                PrepareInstallTutorialTreadmillList();
                break;

            case InstallTutorialStep.PressOperateTab:
            case InstallTutorialStep.ExplainOperateInfo:
                ShowOperatePanel();
                break;

            case InstallTutorialStep.PressEconomyTab:
            case InstallTutorialStep.ExplainEconomyInfo:
                ShowEconomyPanel();
                break;

            case InstallTutorialStep.PressReviewTab:
            case InstallTutorialStep.ExplainReviewInfo:
            case InstallTutorialStep.PressStaffButton:
            case InstallTutorialStep.PressMenuButton:
                ShowReviewPanel();
                break;

            case InstallTutorialStep.ExplainStaff:
            case InstallTutorialStep.PressStaffHire:
                ShowReviewPanel();
                OpenStaffPopup();
                break;

            case InstallTutorialStep.ExplainMenu:
            case InstallTutorialStep.ExplainRelocation:
                ShowReviewPanel();
                OpenMenuPopup();
                break;

            default:
                ShowOperatePanel();
                break;
        }
    }

    private TutorialStepDefinition GetInstallTutorialStepDefinition(InstallTutorialStep step)
    {
        InstallTutorialStep[] orderedSteps = GetInstallTutorialOrderedSteps();
        int stepIndex = 0;
        for (int i = 0; i < orderedSteps.Length; i++)
        {
            if (orderedSteps[i] == step)
            {
                stepIndex = i + 1;
                break;
            }
        }

        switch (step)
        {
            case InstallTutorialStep.IntroGameGoal:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.None, string.Empty, false, Vector2.zero);
            case InstallTutorialStep.PressInstallTab:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "InstallTabButton", true, Vector2.zero);
            case InstallTutorialStep.SelectTreadmill:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "TreadmillCard", true, Vector2.zero);
            case InstallTutorialStep.PlaceTreadmill:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.TileBoardOnly, "TileBoard", true, Vector2.zero);
            case InstallTutorialStep.PressOperateTab:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "OperateTabButton", true, Vector2.zero);
            case InstallTutorialStep.ExplainOperateInfo:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.PanelOnly, "SharedContentPanelRoot", false, Vector2.zero);
            case InstallTutorialStep.PressEconomyTab:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "EconomyTabButton", true, Vector2.zero);
            case InstallTutorialStep.ExplainEconomyInfo:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.PanelOnly, "SharedContentPanelRoot", false, Vector2.zero);
            case InstallTutorialStep.PressReviewTab:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "ReviewTabButton", true, Vector2.zero);
            case InstallTutorialStep.ExplainReviewInfo:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.PanelOnly, "SharedContentPanelRoot", false, Vector2.zero);
            case InstallTutorialStep.PressStaffButton:
            case InstallTutorialStep.ExplainStaff:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "StaffButton", step == InstallTutorialStep.PressStaffButton, Vector2.zero);
            case InstallTutorialStep.PressStaffHire:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "StaffActionButton", true, Vector2.zero);
            case InstallTutorialStep.PressMenuButton:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "MenuButton", true, Vector2.zero);
            case InstallTutorialStep.ExplainMenu:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.PanelOnly, "MenuPopupFrame", false, Vector2.zero);
            case InstallTutorialStep.ExplainRelocation:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.ButtonOnly, "RelocateButton", false, Vector2.zero);
            default:
                return new TutorialStepDefinition(stepIndex, step.ToString(), TutorialFocusMode.None, string.Empty, false, Vector2.zero);
        }
    }

    private void ShowInstallTutorialStep(InstallTutorialStep step, bool forceRebuild = false)
    {
        if (installTutorialPanelFocusRetryCoroutine != null &&
            installTutorialPanelFocusRetryStep != step)
        {
            StopCoroutine(installTutorialPanelFocusRetryCoroutine);
            installTutorialPanelFocusRetryCoroutine = null;
            installTutorialPanelFocusRetryStep = InstallTutorialStep.None;
            installTutorialPanelFocusRetryCount = 0;
        }

        bool sameVisibleStep =
            currentVisibleTutorialStep == step &&
            installTutorialRoot != null &&
            installTutorialRoot.gameObject.activeInHierarchy &&
            installTutorialContentRoot != null &&
            installTutorialContentRoot.childCount > 0;
        int rootChildCount = installTutorialRoot != null ? installTutorialRoot.childCount : -1;
        int contentChildCount = installTutorialContentRoot != null ? installTutorialContentRoot.childCount : -1;
        Debug.Log($"[InstallTutorialTrace] ShowInstallTutorialStep enter step={step} forceRebuild={forceRebuild} currentVisible={currentVisibleTutorialStep} sameVisibleStep={sameVisibleStep} root={(installTutorialRoot != null ? installTutorialRoot.name : "null")} rootActive={(installTutorialRoot != null && installTutorialRoot.gameObject.activeInHierarchy)} rootChildCount={rootChildCount} contentChildCount={contentChildCount}", this);

        if (step != InstallTutorialStep.PlaceTreadmill && installTutorialPlacementActionLifted)
        {
            RestoreInstallTutorialPlacementActionOverlay($"StepChangedTo{step}");
        }

        CleanupInstallTutorialPanelOverlayArtifacts($"ShowStep:{step}");

        if (!forceRebuild &&
            sameVisibleStep)
        {
            Debug.Log($"[InstallTutorialTrace] ShowInstallTutorialStep guard return step={step} currentVisible={currentVisibleTutorialStep} forceRebuild={forceRebuild}. ClearChildren will NOT run.", this);
            if (step == InstallTutorialStep.PlaceTreadmill)
            {
                Debug.Log("[InstallTutorialTrace] Step6 ShowInstallTutorialStep same-step request ignored. ClearChildren will NOT run.", this);
                LiftInstallTutorialPlacementActionOverlayForStep6();
            }

            installTutorialStep = step;
            return;
        }

        installTutorialStep = step;
        EnsureInstallTutorialRoot();

        if (installTutorialRoot == null)
        {
            return;
        }

        EnsureInstallTutorialContentRoot();
        if (installTutorialContentRoot == null)
        {
            return;
        }

        installTutorialRoot.gameObject.SetActive(true);
        installTutorialRoot.SetAsLastSibling();
        int clearChildCountBefore = installTutorialContentRoot.childCount;
        Debug.Log($"[InstallTutorialTrace] ClearChildren installTutorialContentRoot step={step} isStep6={step == InstallTutorialStep.PlaceTreadmill} childCountBefore={clearChildCountBefore} rootChildCount={installTutorialRoot.childCount}", this);
        GameUiFactory.ClearChildren(installTutorialContentRoot);
        Debug.Log($"[InstallTutorialTrace] ClearChildren installTutorialContentRoot finished step={step} childCountAfterImmediate={installTutorialContentRoot.childCount} rootChildCount={installTutorialRoot.childCount} destroyMayBeDeferredInPlayMode={Application.isPlaying}", this);
        installTutorialLoggedDynamicStepEntry = false;
        installTutorialLastDynamicTraceSignature = string.Empty;
        ForceInstallTutorialLayoutRefresh();

        if (!TryResolveInstallTutorialLayout(step, out InstallTutorialLayout layout) &&
            !TryResolveAdditionalInstallTutorialLayout(step, out layout))
        {
            installTutorialRoot.gameObject.SetActive(false);
            currentVisibleTutorialStep = InstallTutorialStep.None;
            return;
        }

        ApplyInstallTutorialLayoutFixups(step, ref layout);
        ForceInstallTutorialLayoutRefresh();

        bool waitingForPanelFocus = ShouldRetryInstallTutorialPanelFocus(step, layout);
        if (waitingForPanelFocus)
        {
            ScheduleInstallTutorialPanelFocusRetry(step, layout);
        }
        else
        {
            ResetInstallTutorialPanelFocusRetry(step);
            ShowInstallTutorialDimmer(layout);
        }

        if (step == InstallTutorialStep.PlaceTreadmill)
        {
            LiftInstallTutorialPlacementActionOverlayForStep6();
        }

        DrawInstallTutorialArrow(layout);
        DrawInstallTutorialCallout(layout);
        currentVisibleTutorialStep = step;
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
            ConfigureInstallTutorialRootCanvas();
            EnsureInstallTutorialContentRoot();
            return;
        }

        installTutorialRoot = GameUiFactory.CreateNode(runtimeRoot, InstallTutorialRootName).transform;
        ConfigureInstallTutorialRootCanvas();
        EnsureInstallTutorialContentRoot();
        installTutorialRoot.gameObject.SetActive(false);
    }

    private void ConfigureInstallTutorialRootCanvas()
    {
        if (installTutorialRoot == null)
        {
            return;
        }

        SetRect(installTutorialRoot.GetComponent<RectTransform>(), 0f, 960f, 1080f, 1920f);

        Canvas canvas = installTutorialRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = installTutorialRoot.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = InstallTutorialOverlaySortingOrder;

        if (installTutorialRoot.GetComponent<GraphicRaycaster>() == null)
        {
            installTutorialRoot.gameObject.AddComponent<GraphicRaycaster>();
        }

        ConfigureInstallTutorialChildRoot(installTutorialContentRoot);
    }

    private void EnsureInstallTutorialContentRoot()
    {
        if (installTutorialRoot == null)
        {
            return;
        }

        if (installTutorialContentRoot == null)
        {
            Transform existing = installTutorialRoot.Find(InstallTutorialContentRootName);
            installTutorialContentRoot = existing != null
                ? existing
                : GameUiFactory.CreateNode(installTutorialRoot, InstallTutorialContentRootName).transform;
        }

        ConfigureInstallTutorialChildRoot(installTutorialContentRoot);
        ConfigureInstallTutorialContentCanvas();
        installTutorialContentRoot.SetAsLastSibling();
    }

    private void ConfigureInstallTutorialContentCanvas()
    {
        if (installTutorialContentRoot == null)
        {
            return;
        }

        Canvas canvas = installTutorialContentRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = installTutorialContentRoot.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = InstallTutorialContentSortingOrder;

        if (installTutorialContentRoot.GetComponent<GraphicRaycaster>() == null)
        {
            installTutorialContentRoot.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void ConfigureInstallTutorialChildRoot(Transform childRoot)
    {
        if (childRoot == null)
        {
            return;
        }

        SetRect(childRoot.GetComponent<RectTransform>(), 0f, 0f, 1080f, 1920f, true);
    }

    private void ForceInstallTutorialLayoutRefresh()
    {
        Canvas.ForceUpdateCanvases();
        RectTransform rootRect = runtimeRoot != null ? runtimeRoot.GetComponent<RectTransform>() : null;
        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        RectTransform tutorialRect = installTutorialRoot != null ? installTutorialRoot.GetComponent<RectTransform>() : null;
        if (tutorialRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(tutorialRect);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void EnsureInstallTutorialDimmer()
    {
        if (installTutorialRoot == null)
        {
            return;
        }

        if (installTutorialDimmer == null)
        {
            Transform existing = installTutorialRoot.Find(InstallTutorialDimmerRootName);
            bool created = existing == null;
            GameObject dimmerObject = existing != null
                ? existing.gameObject
                : GameUiFactory.CreateNode(installTutorialRoot, InstallTutorialDimmerRootName);
            Debug.Log($"[InstallTutorialTrace] EnsureInstallTutorialDimmer root=\"{installTutorialRoot.name}\" createdDimmerRoot={created} dimmerRoot=\"{dimmerObject.name}\" activeSelf={dimmerObject.activeSelf} activeInHierarchy={dimmerObject.activeInHierarchy} rootChildCount={installTutorialRoot.childCount}", this);
            installTutorialDimmer = dimmerObject.GetComponent<TutorialDimmerController>();
            if (installTutorialDimmer == null)
            {
                installTutorialDimmer = dimmerObject.AddComponent<TutorialDimmerController>();
                Debug.Log($"[InstallTutorialTrace] EnsureInstallTutorialDimmer added TutorialDimmerController to \"{dimmerObject.name}\".", this);
            }
        }

        RectTransform dimmerRect = installTutorialDimmer.GetComponent<RectTransform>();
        SetRect(dimmerRect, 0f, 0f, 1080f, 1920f, true);
        installTutorialDimmer.transform.SetAsFirstSibling();
        EnsureInstallTutorialContentRoot();
        installTutorialDimmer.Initialize(InstallTutorialScreenRect, new Color(0f, 0f, 0f, 0.58f));
        installTutorialDimmer.SetDebugOutlineVisible(installTutorialDebugOutlineVisible);
    }

    private void ShowInstallTutorialDimmer(InstallTutorialLayout layout)
    {
        EnsureInstallTutorialDimmer();
        if (installTutorialDimmer == null)
        {
            return;
        }

        Debug.Log($"[InstallTutorialTrace] ShowInstallTutorialDimmer step={installTutorialStep} mode={layout.focusMode} target=\"{layout.focusTargetName}\" hasFocus={layout.hasFocus} focusRect={layout.focusRect} allowFocusInteraction={layout.allowFocusInteraction}", this);
        TutorialFocusTarget focusTarget = new TutorialFocusTarget(
            layout.focusMode,
            layout.hasFocus ? layout.focusRect : Rect.zero,
            layout.allowFocusInteraction,
            layout.focusTargetName);

        installTutorialDimmer.Show(focusTarget);
        LogInstallTutorialRectDebug(layout);
    }

    private Transform FindInstallTutorialPlacementActionRoot()
    {
        if (placementActionRoot == null && runtimeRoot != null)
        {
            placementActionRoot = FindDeepChild(runtimeRoot, "PlacementActionRoot");
        }

        return placementActionRoot;
    }

    private void LiftInstallTutorialPlacementActionOverlayForStep6()
    {
        if (!Application.isPlaying ||
            installTutorialStep != InstallTutorialStep.PlaceTreadmill)
        {
            return;
        }

        Transform actionRoot = FindInstallTutorialPlacementActionRoot();
        if (actionRoot == null)
        {
            Debug.LogWarning("[InstallTutorialTrace] Step6 placement action overlay not found. PlacementActionRoot is null.", this);
            return;
        }

        if (!actionRoot.gameObject.activeInHierarchy)
        {
            Debug.Log($"[InstallTutorialTrace] Step6 placement action overlay found but inactive go=\"{actionRoot.name}\" activeSelf={actionRoot.gameObject.activeSelf} activeInHierarchy={actionRoot.gameObject.activeInHierarchy}", this);
            return;
        }

        if (installTutorialPlacementActionLifted &&
            installTutorialLiftedPlacementActionRoot == actionRoot)
        {
            EnsureInstallTutorialPlacementActionOverlayAboveDimmer(actionRoot);
            return;
        }

        installTutorialLiftedPlacementActionRoot = actionRoot;
        installTutorialPlacementActionOriginalParent = actionRoot.parent;
        installTutorialPlacementActionOriginalSiblingIndex = actionRoot.GetSiblingIndex();

        installTutorialPlacementActionCanvas = actionRoot.GetComponent<Canvas>();
        installTutorialPlacementActionHadCanvas = installTutorialPlacementActionCanvas != null;
        if (installTutorialPlacementActionCanvas == null)
        {
            installTutorialPlacementActionCanvas = actionRoot.gameObject.AddComponent<Canvas>();
        }

        installTutorialPlacementActionOriginalOverrideSorting = installTutorialPlacementActionCanvas.overrideSorting;
        installTutorialPlacementActionOriginalSortingOrder = installTutorialPlacementActionCanvas.sortingOrder;
        installTutorialPlacementActionOriginalSortingLayerID = installTutorialPlacementActionCanvas.sortingLayerID;

        installTutorialPlacementActionRaycaster = actionRoot.GetComponent<GraphicRaycaster>();
        installTutorialPlacementActionHadRaycaster = installTutorialPlacementActionRaycaster != null;
        if (installTutorialPlacementActionRaycaster == null)
        {
            installTutorialPlacementActionRaycaster = actionRoot.gameObject.AddComponent<GraphicRaycaster>();
        }

        installTutorialPlacementActionLifted = true;
        Debug.Log(
            $"[InstallTutorialTrace] Step6 placement action overlay found go=\"{actionRoot.name}\" parent=\"{(installTutorialPlacementActionOriginalParent != null ? installTutorialPlacementActionOriginalParent.name : "null")}\" sibling={installTutorialPlacementActionOriginalSiblingIndex} hadCanvas={installTutorialPlacementActionHadCanvas} originalOverride={installTutorialPlacementActionOriginalOverrideSorting} originalSortingOrder={installTutorialPlacementActionOriginalSortingOrder}",
            this);

        EnsureInstallTutorialPlacementActionOverlayAboveDimmer(actionRoot);
    }

    private void EnsureInstallTutorialPlacementActionOverlayAboveDimmer(Transform actionRoot)
    {
        if (actionRoot == null)
        {
            return;
        }

        actionRoot.SetAsLastSibling();

        Canvas canvas = actionRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = actionRoot.gameObject.AddComponent<Canvas>();
            installTutorialPlacementActionCanvas = canvas;
            installTutorialPlacementActionHadCanvas = false;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = InstallTutorialPlacementActionSortingOrder;

        if (actionRoot.GetComponent<GraphicRaycaster>() == null)
        {
            installTutorialPlacementActionRaycaster = actionRoot.gameObject.AddComponent<GraphicRaycaster>();
            installTutorialPlacementActionHadRaycaster = false;
        }

        bool installButtonAboveDimmer = canvas.overrideSorting && canvas.sortingOrder > InstallTutorialOverlaySortingOrder;
        Debug.Log(
            $"[InstallTutorialTrace] Step6 placement action overlay lifted above dimmer go=\"{actionRoot.name}\" parent=\"{(actionRoot.parent != null ? actionRoot.parent.name : "null")}\" sibling={actionRoot.GetSiblingIndex()} sortingOrder={canvas.sortingOrder} contentSortingOrder={InstallTutorialContentSortingOrder} installButtonAboveDimmer={installButtonAboveDimmer}",
            this);
    }

    private void RestoreInstallTutorialPlacementActionOverlay(string reason)
    {
        if (!installTutorialPlacementActionLifted)
        {
            return;
        }

        Transform actionRoot = installTutorialLiftedPlacementActionRoot;
        if (actionRoot != null)
        {
            if (installTutorialPlacementActionRaycaster != null &&
                !installTutorialPlacementActionHadRaycaster)
            {
                DestroyInstallTutorialComponent(installTutorialPlacementActionRaycaster);
            }

            if (installTutorialPlacementActionCanvas != null)
            {
                installTutorialPlacementActionCanvas.overrideSorting = installTutorialPlacementActionOriginalOverrideSorting;
                installTutorialPlacementActionCanvas.sortingOrder = installTutorialPlacementActionOriginalSortingOrder;
                installTutorialPlacementActionCanvas.sortingLayerID = installTutorialPlacementActionOriginalSortingLayerID;

                if (!installTutorialPlacementActionHadCanvas)
                {
                    DestroyInstallTutorialCanvasAfterRaycaster(
                        installTutorialPlacementActionCanvas,
                        installTutorialPlacementActionRaycaster != null && !installTutorialPlacementActionHadRaycaster,
                        reason,
                        actionRoot.name);
                }
            }

            if (installTutorialPlacementActionOriginalParent != null &&
                actionRoot.parent == installTutorialPlacementActionOriginalParent &&
                installTutorialPlacementActionOriginalSiblingIndex >= 0)
            {
                int restoredSiblingIndex = Mathf.Clamp(
                    installTutorialPlacementActionOriginalSiblingIndex,
                    0,
                    Mathf.Max(0, installTutorialPlacementActionOriginalParent.childCount - 1));
                actionRoot.SetSiblingIndex(restoredSiblingIndex);
            }

            Debug.Log(
                $"[InstallTutorialTrace] Step6 placement action overlay restored reason={reason} go=\"{actionRoot.name}\" parent=\"{(actionRoot.parent != null ? actionRoot.parent.name : "null")}\" sibling={actionRoot.GetSiblingIndex()}",
                this);
        }
        else
        {
            Debug.Log($"[InstallTutorialTrace] Step6 placement action overlay restore skipped reason={reason} because lifted root was destroyed.", this);
        }

        installTutorialLiftedPlacementActionRoot = null;
        installTutorialPlacementActionOriginalParent = null;
        installTutorialPlacementActionCanvas = null;
        installTutorialPlacementActionRaycaster = null;
        installTutorialPlacementActionLifted = false;
        installTutorialPlacementActionHadCanvas = false;
        installTutorialPlacementActionHadRaycaster = false;
        installTutorialPlacementActionOriginalOverrideSorting = false;
        installTutorialPlacementActionOriginalSortingOrder = 0;
        installTutorialPlacementActionOriginalSortingLayerID = 0;
        installTutorialPlacementActionOriginalSiblingIndex = -1;
    }

    private static bool IsSharedPanelTutorialInfoStep(InstallTutorialStep step)
    {
        return step == InstallTutorialStep.ExplainOperateInfo ||
               step == InstallTutorialStep.ExplainEconomyInfo ||
               step == InstallTutorialStep.ExplainReviewInfo;
    }

    private void CleanupInstallTutorialPanelOverlayArtifacts(string reason)
    {
        RestoreInstallTutorialPanelOverlay(reason);

        CleanupInstallTutorialPanelOverlayArtifactsForRoot(sharedPanelRoot, reason);
        CleanupInstallTutorialPanelOverlayArtifactsForRoot(sharedPanelContentRoot, reason);
        CleanupInstallTutorialPanelOverlayArtifactsForRoot(operatePanelRoot, reason);
        CleanupInstallTutorialPanelOverlayArtifactsForRoot(economyPanelRoot, reason);
        CleanupInstallTutorialPanelOverlayArtifactsForRoot(reviewPanelRoot, reason);

        if (runtimeRoot != null)
        {
            CleanupInstallTutorialPanelOverlayArtifactsForRoot(FindDeepChild(runtimeRoot, "SharedContentPanelRoot"), reason);
            CleanupInstallTutorialPanelOverlayArtifactsForRoot(FindDeepChild(runtimeRoot, "SharedPanelContentRoot"), reason);
            CleanupInstallTutorialPanelOverlayArtifactsForRoot(FindDeepChild(runtimeRoot, "OperatePanelRoot"), reason);
            CleanupInstallTutorialPanelOverlayArtifactsForRoot(FindDeepChild(runtimeRoot, "EconomyPanelRoot"), reason);
            CleanupInstallTutorialPanelOverlayArtifactsForRoot(FindDeepChild(runtimeRoot, "ReviewPanelRoot"), reason);
        }
    }

    private void CleanupInstallTutorialPanelOverlayArtifactsForRoot(Transform root, string reason)
    {
        if (root == null)
        {
            return;
        }

        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        bool looksLikeTemporaryTutorialCanvas =
            canvas.overrideSorting &&
            canvas.sortingOrder == InstallTutorialPlacementActionSortingOrder;
        if (!looksLikeTemporaryTutorialCanvas)
        {
            return;
        }

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        bool hadRaycaster = raycaster != null;
        if (raycaster != null)
        {
            DestroyInstallTutorialComponent(raycaster);
        }

        DestroyInstallTutorialCanvasAfterRaycaster(canvas, hadRaycaster, reason, root.name);
        Debug.Log($"[InstallTutorialTrace] PanelOnly temporary Canvas cleanup reason={reason} go=\"{root.name}\" hadRaycaster={hadRaycaster}", this);
    }

    private void DestroyInstallTutorialCanvasAfterRaycaster(Canvas canvas, bool waitOneFrame, string reason, string rootName)
    {
        if (canvas == null)
        {
            return;
        }

        if (Application.isPlaying && waitOneFrame)
        {
            StartCoroutine(DestroyInstallTutorialCanvasAfterRaycasterNextFrame(canvas, reason, rootName));
            return;
        }

        DestroyInstallTutorialComponent(canvas);
    }

    private IEnumerator DestroyInstallTutorialCanvasAfterRaycasterNextFrame(Canvas canvas, string reason, string rootName)
    {
        yield return null;

        if (canvas == null)
        {
            yield break;
        }

        GraphicRaycaster remainingRaycaster = canvas.GetComponent<GraphicRaycaster>();
        if (remainingRaycaster != null)
        {
            DestroyInstallTutorialComponent(remainingRaycaster);
            yield return null;
        }

        if (canvas != null)
        {
            DestroyInstallTutorialComponent(canvas);
            Debug.Log($"[InstallTutorialTrace] PanelOnly temporary Canvas destroyed after raycaster reason={reason} go=\"{rootName}\"", this);
        }
    }

    private static void DestroyInstallTutorialComponent(Component component)
    {
        if (component == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(component);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private void RestoreInstallTutorialPanelOverlay(string reason)
    {
        if (!installTutorialPanelRootLifted)
        {
            return;
        }

        Transform panelRoot = installTutorialLiftedPanelRoot;
        if (panelRoot != null)
        {
            if (installTutorialPanelRaycaster != null &&
                !installTutorialPanelHadRaycaster)
            {
                DestroyInstallTutorialComponent(installTutorialPanelRaycaster);
            }

            if (installTutorialPanelCanvas != null)
            {
                installTutorialPanelCanvas.overrideSorting = installTutorialPanelOriginalOverrideSorting;
                installTutorialPanelCanvas.sortingOrder = installTutorialPanelOriginalSortingOrder;
                installTutorialPanelCanvas.sortingLayerID = installTutorialPanelOriginalSortingLayerID;

                if (!installTutorialPanelHadCanvas)
                {
                    DestroyInstallTutorialCanvasAfterRaycaster(
                        installTutorialPanelCanvas,
                        installTutorialPanelRaycaster != null && !installTutorialPanelHadRaycaster,
                        reason,
                        panelRoot.name);
                }
            }

            if (installTutorialPanelOriginalParent != null &&
                panelRoot.parent == installTutorialPanelOriginalParent &&
                installTutorialPanelOriginalSiblingIndex >= 0)
            {
                int restoredSiblingIndex = Mathf.Clamp(
                    installTutorialPanelOriginalSiblingIndex,
                    0,
                    Mathf.Max(0, installTutorialPanelOriginalParent.childCount - 1));
                panelRoot.SetSiblingIndex(restoredSiblingIndex);
            }

            Debug.Log(
                $"[InstallTutorialTrace] PanelOnly root restored reason={reason} go=\"{panelRoot.name}\" parent=\"{(panelRoot.parent != null ? panelRoot.parent.name : "null")}\" sibling={panelRoot.GetSiblingIndex()}",
                this);
        }
        else
        {
            Debug.Log($"[InstallTutorialTrace] PanelOnly root restore skipped reason={reason} because lifted root was destroyed.", this);
        }

        installTutorialLiftedPanelRoot = null;
        installTutorialPanelOriginalParent = null;
        installTutorialPanelCanvas = null;
        installTutorialPanelRaycaster = null;
        installTutorialPanelRootLifted = false;
        installTutorialPanelHadCanvas = false;
        installTutorialPanelHadRaycaster = false;
        installTutorialPanelOriginalOverrideSorting = false;
        installTutorialPanelOriginalSortingOrder = 0;
        installTutorialPanelOriginalSortingLayerID = 0;
        installTutorialPanelOriginalSiblingIndex = -1;
    }

    private bool ShouldRetryInstallTutorialPanelFocus(InstallTutorialStep step, InstallTutorialLayout layout)
    {
        return Application.isPlaying &&
               installTutorialRunning &&
               IsInstallTutorialPanelOnlyStep(step) &&
               layout.focusMode == TutorialFocusMode.PanelOnly &&
               !IsInstallTutorialFocusRectUsable(layout);
    }

    private static bool IsInstallTutorialPanelOnlyStep(InstallTutorialStep step)
    {
        return step == InstallTutorialStep.ExplainOperateInfo ||
               step == InstallTutorialStep.ExplainEconomyInfo ||
               step == InstallTutorialStep.ExplainReviewInfo ||
               step == InstallTutorialStep.ExplainMenu;
    }

    private static bool IsInstallTutorialFocusRectUsable(InstallTutorialLayout layout)
    {
        if (!layout.hasFocus)
        {
            return false;
        }

        if (layout.focusMode == TutorialFocusMode.PanelOnly)
        {
            return IsInstallTutorialPanelFocusRectUsable(layout.focusRect);
        }

        return layout.focusRect.width > 1f &&
               layout.focusRect.height > 1f;
    }

    private static bool IsInstallTutorialPanelFocusRectUsable(Rect rect)
    {
        return rect.width >= 500f &&
               rect.height >= 300f;
    }

    private void ScheduleInstallTutorialPanelFocusRetry(InstallTutorialStep step, InstallTutorialLayout layout)
    {
        if (installTutorialPanelFocusRetryStep != step)
        {
            installTutorialPanelFocusRetryStep = step;
            installTutorialPanelFocusRetryCount = 0;
        }

        installTutorialPanelFocusRetryCount++;
        Debug.LogWarning($"[InstallTutorialTrace] PanelOnly focus rect not ready. step={step} attempt={installTutorialPanelFocusRetryCount} hasFocus={layout.hasFocus} focusRect={layout.focusRect}. Retry next frame before drawing dimmer.", this);

        if (installTutorialPanelFocusRetryCoroutine != null)
        {
            StopCoroutine(installTutorialPanelFocusRetryCoroutine);
            installTutorialPanelFocusRetryCoroutine = null;
        }

        if (installTutorialPanelFocusRetryCount > 6)
        {
            Debug.LogWarning($"[InstallTutorialTrace] PanelOnly focus rect retry stopped. step={step} target={layout.focusTargetName}. Dimmer was not redrawn with a zero rect.", this);
            return;
        }

        installTutorialPanelFocusRetryCoroutine = StartCoroutine(RetryInstallTutorialPanelFocusNextFrame(step));
    }

    private IEnumerator RetryInstallTutorialPanelFocusNextFrame(InstallTutorialStep step)
    {
        yield return null;
        installTutorialPanelFocusRetryCoroutine = null;

        if (!installTutorialRunning || installTutorialStep != step)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        RectTransform rootRect = runtimeRoot != null ? runtimeRoot.GetComponent<RectTransform>() : null;
        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        Canvas.ForceUpdateCanvases();

        if (!TryResolveInstallTutorialLayout(step, out InstallTutorialLayout layout) &&
            !TryResolveAdditionalInstallTutorialLayout(step, out layout))
        {
            yield break;
        }

        ApplyInstallTutorialLayoutFixups(step, ref layout);
        ForceInstallTutorialLayoutRefresh();

        if (ShouldRetryInstallTutorialPanelFocus(step, layout))
        {
            ScheduleInstallTutorialPanelFocusRetry(step, layout);
            yield break;
        }

        ResetInstallTutorialPanelFocusRetry(step);
        ShowInstallTutorialDimmer(layout);
    }

    private void ResetInstallTutorialPanelFocusRetry(InstallTutorialStep step)
    {
        if (installTutorialPanelFocusRetryStep != step)
        {
            installTutorialPanelFocusRetryStep = InstallTutorialStep.None;
            installTutorialPanelFocusRetryCount = 0;
            return;
        }

        installTutorialPanelFocusRetryStep = InstallTutorialStep.None;
        installTutorialPanelFocusRetryCount = 0;
    }

    private void LogInstallTutorialRectDebug(InstallTutorialLayout layout)
    {
        int stepIndex = GetInstallTutorialStepIndex(installTutorialStep);
        if (!ShouldLogInstallTutorialProblemPage(stepIndex))
        {
            return;
        }

        Rect top = Rect.zero;
        Rect bottom = Rect.zero;
        Rect left = Rect.zero;
        Rect right = Rect.zero;
        if (installTutorialDimmer != null)
        {
            installTutorialDimmer.TryGetCurrentDimPanelRects(out top, out bottom, out left, out right);
        }

        Canvas canvas = installTutorialRoot != null ? installTutorialRoot.GetComponentInParent<Canvas>() : null;
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        string renderMode = canvas != null ? canvas.renderMode.ToString() : "None";
        float scaleFactor = canvas != null ? canvas.scaleFactor : 0f;
        Vector2 referenceResolution = scaler != null ? scaler.referenceResolution : Vector2.zero;
        string source = GetInstallTutorialFocusSource(layout);

        Debug.Log(
            "[InstallTutorialRectDebug] " +
            $"stepIndex={stepIndex} " +
            $"step={installTutorialStep} " +
            $"message=\"{SanitizeInstallTutorialLogText(layout.message)}\" " +
            $"mode={layout.focusMode} " +
            $"target=\"{layout.focusTargetName}\" " +
            $"source={source} " +
            $"allowFocusInput={layout.allowFocusInteraction} " +
            $"focus={FormatInstallTutorialRect(layout.hasFocus ? layout.focusRect : Rect.zero)} " +
            $"dimTop={FormatInstallTutorialRect(top)} " +
            $"dimBottom={FormatInstallTutorialRect(bottom)} " +
            $"dimLeft={FormatInstallTutorialRect(left)} " +
            $"dimRight={FormatInstallTutorialRect(right)} " +
            $"canvasRenderMode={renderMode} " +
            $"canvasScaleFactor={scaleFactor:F3} " +
            $"referenceResolution=({referenceResolution.x:F1},{referenceResolution.y:F1})",
            this);
    }

    private static bool ShouldLogInstallTutorialProblemPage(int stepIndex)
    {
        return stepIndex == 2 ||
               stepIndex == 4 ||
               stepIndex == 6 ||
               stepIndex == 7 ||
               stepIndex == 8 ||
               stepIndex == 10 ||
               stepIndex == 12 ||
               stepIndex == 14;
    }

    private int GetInstallTutorialStepIndex(InstallTutorialStep step)
    {
        InstallTutorialStep[] orderedSteps = GetInstallTutorialOrderedSteps();
        for (int i = 0; i < orderedSteps.Length; i++)
        {
            if (orderedSteps[i] == step)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private static string GetInstallTutorialFocusSource(InstallTutorialLayout layout)
    {
        if (!string.IsNullOrEmpty(layout.focusSource))
        {
            return layout.focusSource;
        }

        switch (layout.focusMode)
        {
            case TutorialFocusMode.ButtonOnly:
            case TutorialFocusMode.PanelOnly:
                return string.IsNullOrEmpty(layout.focusTargetName) ? "RectTransform" : "RectTransform";
            case TutorialFocusMode.TileBoardOnly:
                return "TileBoard";
            case TutorialFocusMode.CustomRect:
                return "CustomRect";
            default:
                return "None";
        }
    }

    private static string SanitizeInstallTutorialLogText(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private static string FormatInstallTutorialRect(Rect rect)
    {
        return $"xMin={rect.xMin:F1},yMin={rect.yMin:F1},xMax={rect.xMax:F1},yMax={rect.yMax:F1},w={rect.width:F1},h={rect.height:F1}";
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
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("InstallTabButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(-270f, -940f, 284f, 146f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(24f, 178f),
                    "1",
                    "설치 탭을 눌러 기구 목록을 열어보세요",
                    650f,
                    142f);
                return true;
            }

            case InstallTutorialStep.SelectTreadmill:
            {
                PrepareInstallTutorialTreadmillList();
                RectTransform targetTransform = FindInstallTutorialTreadmillCard();
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(-500f, -360f, 516f, 132f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(246f, -160f),
                    "2",
                    "러닝머신을 선택해 배치 준비를 해보세요",
                    650f,
                    142f);
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
                layout.targetCenter = gridRect.center + new Vector2(40f, 28f);
                layout.calloutCenter = ClampInstallTutorialCallout(gridRect.center + new Vector2(205f, 265f), 680f, 142f);
                layout.badgeText = "3";
                layout.message = "빈 공간을 누른 뒤 설치 버튼으로 배치하세요";
                layout.showArrow = true;
                return true;
            }

            case InstallTutorialStep.ExplainOperateInfo:
            {
                ConfigureOperatePanelInfoStep(ref layout);
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
                layout.showArrow = false;
                layout.canTapCallout = true;
                return true;
            }

            case InstallTutorialStep.ExplainMenu:
            {
                RectTransform summaryTransform = FindInstallTutorialNamedTargetTransform("MenuSummaryCard");
                Rect targetRect = GetInstallTutorialTargetRect(summaryTransform, Vector2.zero, new Rect(-320f, 210f, 640f, 188f));
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, -720f),
                    "i",
                    "메뉴에서는 지점 이전, 설정, 타이틀 이동 같은 관리 기능을 사용할 수 있어요.",
                    780f,
                    130f);
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
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("OperateTabButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(-520f, -940f, 260f, 146f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(220f, 198f),
                    "4",
                    "운영 탭을 눌러 현재 헬스장 상태를 확인해보세요",
                    700f,
                    142f);
                return true;
            }

            case InstallTutorialStep.PressEconomyTab:
            {
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("EconomyTabButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(-8f, -940f, 260f, 146f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(0f, 198f),
                    "5",
                    "경제 탭을 눌러 수입과 지출 흐름을 살펴보세요",
                    700f,
                    142f);
                return true;
            }

            case InstallTutorialStep.ExplainEconomyInfo:
            {
                Rect targetRect = GetSharedPanelTutorialRect(GetSharedPanelTutorialFocusTransform(InstallTutorialStep.ExplainEconomyInfo));
                float calloutWidth = 760f;
                float calloutHeight = 150f;
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    GetInstallTutorialCalloutAboveRect(targetRect, calloutWidth, calloutHeight),
                    "i",
                    "경제 패널에서는 자금 변화와 수익 흐름을 확인할 수 있어요.",
                    calloutWidth,
                    calloutHeight);
                return true;
            }

            case InstallTutorialStep.PressReviewTab:
            {
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("ReviewTabButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(248f, -940f, 260f, 146f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(-210f, 198f),
                    "6",
                    "리뷰 탭을 눌러 회원 반응과 평가를 확인해보세요",
                    700f,
                    142f);
                return true;
            }

            case InstallTutorialStep.ExplainReviewInfo:
            {
                Rect targetRect = GetSharedPanelTutorialRect(GetSharedPanelTutorialFocusTransform(InstallTutorialStep.ExplainReviewInfo));
                float calloutWidth = 760f;
                float calloutHeight = 150f;
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    GetInstallTutorialCalloutAboveRect(targetRect, calloutWidth, calloutHeight),
                    "i",
                    "리뷰 패널에서는 만족도, 신규 후기, 추천 비율을 볼 수 있어요.",
                    calloutWidth,
                    calloutHeight);
                return true;
            }

            case InstallTutorialStep.PressStaffButton:
            {
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("StaffButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(300f, 805f, 108f, 128f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(-260f, -220f),
                    "7",
                    "직원 버튼을 눌러 직원 채용 화면을 열어보세요",
                    730f,
                    150f);
                return true;
            }

            case InstallTutorialStep.PressStaffHire:
            {
                ConfigureStaffHireStep(ref layout);
                return true;
            }

            case InstallTutorialStep.PressMenuButton:
            {
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("MenuButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(406f, 805f, 108f, 128f));
                ConfigureElevatedActionStep(
                    ref layout,
                    targetTransform,
                    targetRect,
                    targetRect.center + new Vector2(-275f, -220f),
                    "9",
                    "메뉴 버튼을 눌러 관리 기능을 확인해보세요",
                    730f,
                    150f);
                return true;
            }

            case InstallTutorialStep.ExplainRelocation:
            {
                RectTransform targetTransform = FindInstallTutorialNamedTargetTransform("RelocateButton");
                Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, new Rect(-320f, 120f, 640f, 104f));
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
                ConfigureOperatePanelInfoStep(ref layout);
                break;
            }

            case InstallTutorialStep.ExplainMenu:
            {
                RectTransform summaryTransform = FindInstallTutorialNamedTargetTransform("MenuSummaryCard");
                Rect targetRect = GetInstallTutorialTargetRect(summaryTransform, Vector2.zero, new Rect(-320f, 210f, 640f, 188f));
                ConfigureFocusedMessageStep(
                    ref layout,
                    targetRect,
                    new Vector2(0f, -720f),
                    "i",
                    "메뉴에서는 지점 이전, 설정, 타이틀 이동 같은 관리 기능을 사용할 수 있어요.",
                    780f,
                    130f,
                    summaryTransform);
                break;
            }

            default:
                EnsureLayoutHasFocusRects(ref layout);
                break;
        }

        ApplyInstallTutorialFocusPolicy(step, ref layout);
        ApplyInstallTutorialArrowPolicy(step, ref layout);

    }

    private void ApplyInstallTutorialFocusPolicy(InstallTutorialStep step, ref InstallTutorialLayout layout)
    {
        switch (step)
        {
            case InstallTutorialStep.IntroGreeting:
            case InstallTutorialStep.IntroGameGoal:
            case InstallTutorialStep.IntroFirstAction:
            case InstallTutorialStep.FinalMessage:
                ClearInstallTutorialFocusPolicy(ref layout);
                layout.showArrow = false;
                return;

            case InstallTutorialStep.PressInstallTab:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "InstallTabButton",
                    new Rect(-270f, -940f, 284f, 146f),
                    passThrough: true,
                    elevateTarget: true);
                return;

            case InstallTutorialStep.SelectTreadmill:
            {
                RectTransform card = FindInstallTutorialTreadmillCard();
                Rect cardRect = GetInstallTutorialTargetRect(card, Vector2.zero, new Rect(-500f, -360f, 516f, 132f));
                SetInstallTutorialPrimaryFocusRect(ref layout, cardRect, cardRect, passThrough: true);
                layout.focusMode = TutorialFocusMode.ButtonOnly;
                layout.focusTargetName = "TreadmillCard";
                layout.focusSource = card != null ? "RectTransform" : "FallbackRect";
                return;
            }

            case InstallTutorialStep.PlaceTreadmill:
                ConfigurePlaceTreadmillStep(ref layout);
                return;

            case InstallTutorialStep.PressOperateTab:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "OperateTabButton",
                    new Rect(-520f, -940f, 260f, 146f),
                    passThrough: true,
                    elevateTarget: true);
                return;

            case InstallTutorialStep.ExplainOperateInfo:
            case InstallTutorialStep.ExplainEconomyInfo:
            case InstallTutorialStep.ExplainReviewInfo:
            {
                RectTransform panelTransform = GetSharedPanelTutorialFocusTransform(step);
                Rect panelRect = GetSharedPanelTutorialRect(panelTransform);
                SetInstallTutorialPrimaryFocusRect(ref layout, panelRect, Rect.zero, passThrough: false);
                layout.focusMode = TutorialFocusMode.PanelOnly;
                layout.focusTargetName = GetSharedPanelTutorialFocusTargetNameForLastRect(step);
                layout.focusSource = panelTransform != null ? "RectTransform" : "FallbackRect";
                layout.showArrow = false;
                layout.canTapCallout = true;
                return;
            }

            case InstallTutorialStep.PressEconomyTab:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "EconomyTabButton",
                    new Rect(-8f, -940f, 260f, 146f),
                    passThrough: true,
                    elevateTarget: true);
                return;

            case InstallTutorialStep.PressReviewTab:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "ReviewTabButton",
                    new Rect(248f, -940f, 260f, 146f),
                    passThrough: true,
                    elevateTarget: true);
                return;

            case InstallTutorialStep.PressStaffButton:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "StaffButton",
                    new Rect(300f, 805f, 108f, 128f),
                    passThrough: true,
                    elevateTarget: true);
                return;

            case InstallTutorialStep.ExplainStaff:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "StaffButton",
                    new Rect(286f, 790f, 132f, 154f),
                    passThrough: false,
                    elevateTarget: false);
                layout.showArrow = false;
                layout.canTapCallout = true;
                return;

            case InstallTutorialStep.PressStaffHire:
                EnsureLayoutHasFocusRects(ref layout);
                return;

            case InstallTutorialStep.PressMenuButton:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "MenuButton",
                    new Rect(406f, 805f, 108f, 128f),
                    passThrough: true,
                    elevateTarget: true);
                return;

            case InstallTutorialStep.ExplainMenu:
            {
                RectTransform frame = FindInstallTutorialNamedTargetTransform("MenuPopupFrame");
                Rect frameRect = GetInstallTutorialTargetRect(frame, Vector2.zero, new Rect(-385f, -505f, 770f, 1010f));
                SetInstallTutorialPrimaryFocusRect(ref layout, frameRect, Rect.zero, passThrough: false);
                layout.focusMode = TutorialFocusMode.PanelOnly;
                layout.focusTargetName = "MenuPopupFrame";
                layout.focusSource = frame != null ? "RectTransform" : "FallbackRect";
                layout.showArrow = false;
                layout.canTapCallout = true;
                return;
            }

            case InstallTutorialStep.ExplainRelocation:
                SetInstallTutorialNamedButtonFocus(
                    ref layout,
                    "RelocateButton",
                    new Rect(-320f, 120f, 640f, 104f),
                    passThrough: false,
                    elevateTarget: true);
                layout.showArrow = false;
                layout.canTapCallout = true;
                return;
        }
    }

    private void ApplyInstallTutorialArrowPolicy(InstallTutorialStep step, ref InstallTutorialLayout layout)
    {
        int occurrenceIndex = GetInstallTutorialArrowOccurrenceIndex(step);
        layout.showArrow = occurrenceIndex > 0;
        if (!layout.showArrow)
        {
            HideInstallTutorialArrow(ref layout);
            return;
        }

        if (IsInstallTutorialArrowOccurrenceHidden(occurrenceIndex))
        {
            HideInstallTutorialArrow(ref layout);
            return;
        }

        switch (occurrenceIndex)
        {
            case 1:
                SetInstallTutorialManualArrow(ref layout, -128f, -780f, 0f);
                return;
            case 2:
                SetInstallTutorialManualArrow(ref layout, -94f, -373f, 180f);
                return;
            case 3:
                SetInstallTutorialManualArrow(ref layout, -52f, -69f, 0f);
                return;
            case 4:
                SetInstallTutorialManualArrow(ref layout, -368f, -767f, 0f);
                return;
            case 5:
                return;
            case 8:
                SetInstallTutorialManualArrow(ref layout, 381f, -760f, 0f);
                return;
            case 10:
                SetInstallTutorialManualArrow(ref layout, 344f, 734f, 180f);
                return;
            case 11:
                SetInstallTutorialManualArrow(ref layout, 240f, 239f, 0f);
                return;
            case 12:
                SetInstallTutorialManualArrow(ref layout, 450f, 740f, 180f);
                return;
        }
    }

    private static bool ShouldShowInstallTutorialArrow(InstallTutorialStep step)
    {
        int occurrenceIndex = GetInstallTutorialArrowOccurrenceIndex(step);
        return occurrenceIndex > 0 && !IsInstallTutorialArrowOccurrenceHidden(occurrenceIndex);
    }

    private static int GetInstallTutorialArrowOccurrenceIndex(InstallTutorialStep step)
    {
        switch (step)
        {
            case InstallTutorialStep.PressInstallTab:
                return 1;
            case InstallTutorialStep.SelectTreadmill:
                return 2;
            case InstallTutorialStep.PlaceTreadmill:
                return 3;
            case InstallTutorialStep.PressOperateTab:
                return 4;
            case InstallTutorialStep.ExplainOperateInfo:
                return 5;
            case InstallTutorialStep.PressEconomyTab:
                return 6;
            case InstallTutorialStep.ExplainEconomyInfo:
                return 7;
            case InstallTutorialStep.PressReviewTab:
                return 8;
            case InstallTutorialStep.ExplainReviewInfo:
                return 9;
            case InstallTutorialStep.PressStaffButton:
                return 10;
            case InstallTutorialStep.PressStaffHire:
                return 11;
            case InstallTutorialStep.PressMenuButton:
                return 12;
            case InstallTutorialStep.ExplainMenu:
                return 13;
            default:
                return 0;
        }
    }

    private static bool IsInstallTutorialArrowOccurrenceHidden(int occurrenceIndex)
    {
        return occurrenceIndex == 7 ||
               occurrenceIndex == 9 ||
               occurrenceIndex == 13;
    }

    private static void SetInstallTutorialManualArrow(
        ref InstallTutorialLayout layout,
        float x,
        float y,
        float rotationZ)
    {
        layout.showArrow = true;
        layout.hasArrowOverride = true;
        layout.arrowOverrideCenter = new Vector2(x, y);
        layout.arrowOverrideSize = InstallTutorialArrowSize;
        layout.arrowOverrideRotation = rotationZ;
        layout.arrowOffset = Vector2.zero;
        layout.arrowSizeDelta = Vector2.zero;
        layout.arrowRotationOffset = 0f;
    }

    private static void HideInstallTutorialArrow(ref InstallTutorialLayout layout)
    {
        layout.showArrow = false;
        layout.hasArrowTargetRect = false;
        layout.arrowTargetRect = Rect.zero;
        layout.hasArrowOverride = false;
        layout.arrowOverrideCenter = Vector2.zero;
        layout.arrowOverrideSize = Vector2.zero;
        layout.arrowOverrideRotation = 0f;
        layout.arrowOffset = Vector2.zero;
        layout.arrowSizeDelta = Vector2.zero;
        layout.arrowRotationOffset = 0f;
    }

    private void ClearInstallTutorialFocusPolicy(ref InstallTutorialLayout layout)
    {
        ResetInstallTutorialFocus(ref layout);
        layout.focusMode = TutorialFocusMode.None;
        layout.focusTargetName = string.Empty;
        layout.focusSource = "None";
        layout.allowFocusInteraction = false;
    }

    private void SetInstallTutorialNamedButtonFocus(
        ref InstallTutorialLayout layout,
        string targetName,
        Rect fallback,
        bool passThrough,
        bool elevateTarget)
    {
        RectTransform target = FindInstallTutorialNamedTargetTransform(targetName);
        Rect targetRect = GetInstallTutorialTargetRect(target, Vector2.zero, fallback);
        SetInstallTutorialPrimaryFocusRect(
            ref layout,
            targetRect,
            targetRect,
            passThrough);
        layout.focusMode = TutorialFocusMode.ButtonOnly;
        layout.focusTargetName = targetName;
        layout.focusSource = target != null ? "RectTransform" : "FallbackRect";
    }

    private void SetInstallTutorialPrimaryFocusRect(
        ref InstallTutorialLayout layout,
        Rect focusRect,
        Rect passThroughRect,
        bool passThrough)
    {
        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, focusRect);

        layout.targetCenter = layout.hasFocus ? layout.focusRect.center : focusRect.center;
        layout.focusMode = layout.focusMode == TutorialFocusMode.None ? TutorialFocusMode.CustomRect : layout.focusMode;
        layout.allowFocusInteraction = passThrough;
        if (string.IsNullOrEmpty(layout.focusSource))
        {
            layout.focusSource = "CustomRect";
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
        layout.targetCenter = targetRect.center;
        layout.calloutWidth = calloutWidth;
        layout.calloutHeight = calloutHeight;
        layout.calloutCenter = ClampInstallTutorialCallout(calloutCenter, calloutWidth, calloutHeight);
        layout.badgeText = badgeText;
        layout.message = message;
        layout.showArrow = true;
        layout.canTapCallout = false;
        layout.focusMode = TutorialFocusMode.ButtonOnly;
        layout.focusSource = "CustomRect";
        layout.allowFocusInteraction = true;
    }

    private void ConfigureElevatedActionStep(
        ref InstallTutorialLayout layout,
        RectTransform targetTransform,
        Rect fallbackRect,
        Vector2 calloutCenter,
        string badgeText,
        string message,
        float calloutWidth,
        float calloutHeight)
    {
        Rect targetRect = GetInstallTutorialTargetRect(targetTransform, Vector2.zero, fallbackRect);
        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, targetRect);

        layout.focusRect = targetRect;
        layout.targetCenter = targetRect.center;
        layout.calloutWidth = calloutWidth;
        layout.calloutHeight = calloutHeight;
        layout.calloutCenter = ClampInstallTutorialCallout(calloutCenter, calloutWidth, calloutHeight);
        layout.badgeText = badgeText;
        layout.message = message;
        layout.showArrow = true;
        layout.canTapCallout = false;
        layout.focusMode = TutorialFocusMode.ButtonOnly;
        layout.allowFocusInteraction = true;
    }

    private void ConfigureFocusedMessageStep(
        ref InstallTutorialLayout layout,
        Rect targetRect,
        Vector2 calloutCenter,
        string badgeText,
        string message,
        float calloutWidth,
        float calloutHeight,
        bool showArrow = false)
    {
        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, targetRect);

        layout.targetCenter = targetRect.center;
        layout.calloutWidth = calloutWidth;
        layout.calloutHeight = calloutHeight;
        layout.calloutCenter = ClampInstallTutorialCallout(calloutCenter, calloutWidth, calloutHeight);
        layout.badgeText = badgeText;
        layout.message = message;
        layout.showArrow = showArrow;
        layout.canTapCallout = true;
        if (string.IsNullOrEmpty(layout.focusSource))
        {
            layout.focusSource = "CustomRect";
        }
    }

    private void ConfigureOperatePanelInfoStep(ref InstallTutorialLayout layout)
    {
        RectTransform panelTransform = GetSharedPanelTutorialFocusTransform(InstallTutorialStep.ExplainOperateInfo);
        Rect targetRect = GetSharedPanelTutorialRect(panelTransform);
        float calloutWidth = Mathf.Max(520f, installTutorialOperateCalloutSize.x);
        float calloutHeight = Mathf.Max(120f, installTutorialOperateCalloutSize.y);

        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, targetRect);
        layout.targetCenter = targetRect.center;
        layout.calloutWidth = calloutWidth;
        layout.calloutHeight = calloutHeight;
        layout.calloutCenter = ClampInstallTutorialCallout(installTutorialOperateCalloutCenter, calloutWidth, calloutHeight);
        layout.badgeText = "i";
        layout.message = "운영 패널에서는 목표, 상태, 청결,\n직원과 오늘 수익을 확인할 수 있어요.";
        layout.showArrow = false;
        layout.canTapCallout = true;
        layout.focusMode = TutorialFocusMode.PanelOnly;
        layout.focusTargetName = GetSharedPanelTutorialFocusTargetNameForLastRect(InstallTutorialStep.ExplainOperateInfo);
        layout.focusSource = panelTransform != null ? "RectTransform" : "FallbackRect";
    }

    private void ConfigurePlaceTreadmillStep(ref InstallTutorialLayout layout)
    {
        Rect fallbackGridRect = new Rect(-452f, -248f, 904f, 520f);
        GetInstallTutorialGridCellFocusRects(fallbackGridRect, out Rect gridBounds);
        Rect boardFocusRect = IsInstallTutorialGridBoundsUsable(gridBounds) ? gridBounds : fallbackGridRect;
        RectTransform actionTransform = placementActionRoot != null ? placementActionRoot.GetComponent<RectTransform>() : null;
        Rect actionRect = GetInstallTutorialTargetRect(
            actionTransform,
            new Vector2(18f, 18f),
            new Rect(-376f, -720f, 752f, 324f));
        Rect confirmButtonRect = GetInstallTutorialPlacementConfirmButtonRect(actionRect);

        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, boardFocusRect);
        SetInstallTutorialArrowTargetRect(ref layout, confirmButtonRect);

        layout.targetCenter = boardFocusRect.center;
        layout.calloutWidth = 700f;
        layout.calloutHeight = 142f;
        layout.calloutCenter = ClampInstallTutorialCallout(boardFocusRect.center + new Vector2(390f, 96f), layout.calloutWidth, layout.calloutHeight);
        layout.badgeText = "3";
        layout.message = "빈 공간을 누른 뒤 설치 버튼으로 배치하세요";
        layout.showArrow = true;
        layout.focusMode = TutorialFocusMode.TileBoardOnly;
        layout.focusTargetName = "TileBoard";
        layout.focusSource = "TileBoard";
        layout.allowFocusInteraction = true;
        layout.canTapCallout = false;
    }

    private Rect GetInstallTutorialPlacementConfirmButtonRect(Rect actionRect)
    {
        RectTransform buttonTransform = FindInstallTutorialNamedTargetTransform("PlacementActionButton_0");
        return GetInstallTutorialTargetRect(
            buttonTransform,
            Vector2.zero,
            GetInstallTutorialPlacementConfirmButtonFallbackRect(actionRect));
    }

    private static Rect GetInstallTutorialPlacementConfirmButtonFallbackRect(Rect actionRect)
    {
        Rect usableActionRect = actionRect.width > 1f && actionRect.height > 1f
            ? actionRect
            : new Rect(-376f, -720f, 752f, 324f);
        Vector2 center = usableActionRect.center + new Vector2(-95f, -88f);
        return new Rect(center.x - 88f, center.y - 29f, 176f, 58f);
    }

    private static void SetInstallTutorialArrowTargetRect(ref InstallTutorialLayout layout, Rect targetRect)
    {
        layout.hasArrowTargetRect = targetRect.width > 1f && targetRect.height > 1f;
        layout.arrowTargetRect = layout.hasArrowTargetRect ? targetRect : Rect.zero;
    }

    private static bool IsInstallTutorialGridBoundsUsable(Rect rect)
    {
        return rect.width >= 200f &&
               rect.height >= 200f &&
               rect.width <= 980f &&
               rect.height <= 760f;
    }

    private void ConfigureStaffHireStep(ref InstallTutorialLayout layout)
    {
        RectTransform rowTransform = FindInstallTutorialFirstStaffRow();
        RectTransform buttonTransform = FindInstallTutorialStaffHireButton();
        Rect rowRect = GetInstallTutorialTargetRect(rowTransform, Vector2.zero, new Rect(-305f, 98f, 610f, 128f));
        Rect buttonRect = GetInstallTutorialTargetRect(buttonTransform, Vector2.zero, new Rect(182f, 98f, 136f, 64f));

        ResetInstallTutorialFocus(ref layout);
        AddInstallTutorialFocusRect(ref layout, buttonRect);
        layout.targetCenter = buttonRect.center;
        layout.calloutWidth = 700f;
        layout.calloutHeight = 142f;
        layout.calloutCenter = ClampInstallTutorialCallout(rowRect.center + new Vector2(0f, 190f), layout.calloutWidth, layout.calloutHeight);
        layout.badgeText = "8";
        layout.message = "채용 버튼을 눌러 첫 직원을 고용해보세요";
        layout.showArrow = true;
        layout.focusMode = TutorialFocusMode.ButtonOnly;
        layout.focusTargetName = "StaffActionButton";
        layout.focusSource = buttonTransform != null ? "RectTransform" : "FallbackRect";
        layout.allowFocusInteraction = true;
        layout.canTapCallout = false;
    }

    private Rect GetSharedPanelTutorialRect()
    {
        return GetSharedPanelTutorialRect(GetSharedPanelTutorialFocusTransform(installTutorialStep));
    }

    private Rect GetSharedPanelTutorialRect(RectTransform panelTransform)
    {
        if (TryGetSharedPanelTutorialRect(panelTransform, out Rect rect))
        {
            return rect;
        }

        return Rect.zero;
    }

    private bool TryGetSharedPanelTutorialRect(RectTransform panelTransform, out Rect rect)
    {
        installTutorialLastSharedPanelAreaFallbackUsed = false;

        Canvas.ForceUpdateCanvases();
        RectTransform rootRect = runtimeRoot != null ? runtimeRoot.GetComponent<RectTransform>() : null;
        if (rootRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        Canvas.ForceUpdateCanvases();

        if (installTutorialUseManualSharedPanelFocus)
        {
            rect = ApplyInstallTutorialSharedPanelFocusTuning(Rect.zero);
            return IsInstallTutorialPanelFocusRectUsable(rect);
        }

        string targetName = panelTransform != null ? panelTransform.name : "null";
        if (TryGetInstallTutorialTargetRect(panelTransform, Vector2.zero, out rect))
        {
            rect = ApplyInstallTutorialSharedPanelFocusTuning(rect);
            if (IsInstallTutorialPanelFocusRectUsable(rect))
            {
                Debug.Log($"[InstallTutorialTrace] PanelOnly root rect focus resolved step={installTutorialStep} target={targetName} rect={FormatInstallTutorialRect(rect)} source=RootRect", this);
                return true;
            }

            Debug.Log($"[InstallTutorialTrace] PanelOnly root rect invalid step={installTutorialStep} target={targetName} rect={FormatInstallTutorialRect(rect)}. Trying visible child bounds.", this);
        }
        else
        {
            Debug.Log($"[InstallTutorialTrace] PanelOnly root rect missing step={installTutorialStep} target={targetName}. Trying visible child bounds.", this);
        }

        if (TryGetSharedPanelVisibleChildBounds(
                installTutorialStep,
                panelTransform,
                out rect,
                out int childCount,
                out string boundsRootName))
        {
            rect = ApplyInstallTutorialSharedPanelFocusTuning(rect);
            if (IsInstallTutorialPanelFocusRectUsable(rect))
            {
                Debug.Log($"[InstallTutorialTrace] PanelOnly visual bounds focus resolved step={installTutorialStep} target={targetName} boundsRoot={boundsRootName} rect={FormatInstallTutorialRect(rect)} source=VisibleChildBounds childCount={childCount}", this);
                return true;
            }

            Debug.Log($"[InstallTutorialTrace] PanelOnly visual bounds invalid step={installTutorialStep} target={targetName} boundsRoot={boundsRootName} rect={FormatInstallTutorialRect(rect)} childCount={childCount}", this);
        }

        if (TryGetSharedPanelAreaFallbackRect(installTutorialStep, out rect))
        {
            installTutorialLastSharedPanelAreaFallbackUsed = true;
            if (installTutorialStep == InstallTutorialStep.ExplainOperateInfo)
            {
                Debug.Log($"[InstallTutorialTrace] PanelOnly operate visual fallback used step={installTutorialStep} rect={FormatInstallTutorialRect(rect)}", this);
            }
            else
            {
                Debug.Log($"[InstallTutorialTrace] PanelOnly shared panel area fallback used step={installTutorialStep} rect={FormatInstallTutorialRect(rect)}", this);
            }

            return true;
        }

        Debug.LogWarning($"[InstallTutorialTrace] PanelOnly focus failed step={installTutorialStep} candidates={GetSharedPanelTutorialFocusCandidateLog(installTutorialStep)} target={targetName}", this);
        rect = Rect.zero;
        return false;
    }

    private static bool TryGetSharedPanelAreaFallbackRect(InstallTutorialStep step, out Rect rect)
    {
        if (step == InstallTutorialStep.ExplainOperateInfo ||
            step == InstallTutorialStep.ExplainEconomyInfo ||
            step == InstallTutorialStep.ExplainReviewInfo)
        {
            rect = ClampRectToScreen(step == InstallTutorialStep.ExplainOperateInfo
                ? InstallTutorialOperatePanelAreaFallbackRect
                : InstallTutorialSharedPanelAreaFallbackRect);
            return true;
        }

        rect = Rect.zero;
        return false;
    }

    private bool TryGetSharedPanelVisibleChildBounds(
        InstallTutorialStep step,
        RectTransform panelTransform,
        out Rect rect,
        out int childCount,
        out string boundsRootName)
    {
        rect = Rect.zero;
        childCount = 0;

        RectTransform boundsRoot = GetSharedPanelTutorialVisibleBoundsRoot(step, panelTransform);
        boundsRootName = boundsRoot != null ? boundsRoot.name : "null";
        if (boundsRoot == null)
        {
            return false;
        }

        Rect unionRect = Rect.zero;
        bool hasBounds = false;
        RectTransform[] rectTransforms = boundsRoot.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform candidate = rectTransforms[i];
            if (!ShouldUseInstallTutorialVisibleChildForPanelBounds(candidate))
            {
                continue;
            }

            if (!TryGetInstallTutorialTargetRect(candidate, Vector2.zero, out Rect candidateRect))
            {
                continue;
            }

            if (candidateRect.width < 4f || candidateRect.height < 4f)
            {
                continue;
            }

            unionRect = hasBounds ? UnionRects(unionRect, candidateRect) : candidateRect;
            hasBounds = true;
            childCount++;
        }

        if (!hasBounds)
        {
            return false;
        }

        rect = ClampRectToScreen(unionRect);
        return true;
    }

    private RectTransform GetSharedPanelTutorialVisibleBoundsRoot(InstallTutorialStep step, RectTransform panelTransform)
    {
        Transform sharedRoot = sharedPanelRoot != null
            ? sharedPanelRoot
            : FindDeepChild(runtimeRoot, "SharedContentPanelRoot");
        if (sharedRoot != null && sharedRoot.gameObject.activeInHierarchy)
        {
            return sharedRoot.GetComponent<RectTransform>();
        }

        Transform contentRoot = sharedPanelContentRoot != null
            ? sharedPanelContentRoot
            : FindDeepChild(runtimeRoot, "SharedPanelContentRoot");
        if (contentRoot != null && contentRoot.gameObject.activeInHierarchy)
        {
            return contentRoot.GetComponent<RectTransform>();
        }

        return panelTransform;
    }

    private bool ShouldUseInstallTutorialVisibleChildForPanelBounds(RectTransform candidate)
    {
        if (candidate == null ||
            !candidate.gameObject.activeInHierarchy ||
            candidate.rect.width < 4f ||
            candidate.rect.height < 4f ||
            IsInstallTutorialOverlayTransform(candidate))
        {
            return false;
        }

        Graphic graphic = candidate.GetComponent<Graphic>();
        if (graphic != null)
        {
            return graphic.enabled && graphic.color.a > 0.01f;
        }

        Button button = candidate.GetComponent<Button>();
        return button != null && button.enabled;
    }

    private static bool IsInstallTutorialOverlayTransform(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.name;
            if (name == InstallTutorialRootName ||
                name == InstallTutorialDimmerRootName ||
                name == InstallTutorialContentRootName ||
                name.Contains("InstallTutorial") ||
                name.Contains("TutorialDimmer"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static string GetSharedPanelTutorialFocusCandidateLog(InstallTutorialStep step)
    {
        string specific = GetSharedPanelTutorialSpecificRootName(step);
        if (string.IsNullOrEmpty(specific))
        {
            specific = "PanelRoot";
        }

        return $"{specific},SharedPanelContentRoot,SharedContentPanelRoot";
    }

    private RectTransform GetSharedPanelTutorialFocusTransform()
    {
        return GetSharedPanelTutorialFocusTransform(installTutorialStep);
    }

    private RectTransform GetSharedPanelTutorialFocusTransform(InstallTutorialStep step)
    {
        string fallbackName = GetSharedPanelTutorialSpecificRootName(step);
        Transform specificRoot = !string.IsNullOrEmpty(fallbackName)
            ? FindDeepChild(runtimeRoot, fallbackName)
            : null;
        if (specificRoot != null && specificRoot.gameObject.activeInHierarchy)
        {
            return specificRoot.GetComponent<RectTransform>();
        }

        Transform root = sharedPanelContentRoot != null
            ? sharedPanelContentRoot
            : FindDeepChild(runtimeRoot, "SharedPanelContentRoot");
        if (root != null && root.gameObject.activeInHierarchy)
        {
            return root.GetComponent<RectTransform>();
        }

        root = sharedPanelRoot != null ? sharedPanelRoot : FindDeepChild(runtimeRoot, "SharedContentPanelRoot");
        return root != null ? root.GetComponent<RectTransform>() : null;
    }

    private static string GetSharedPanelTutorialSpecificRootName(InstallTutorialStep step)
    {
        switch (step)
        {
            case InstallTutorialStep.ExplainOperateInfo:
                return "OperatePanelRoot";
            case InstallTutorialStep.ExplainEconomyInfo:
                return "EconomyPanelRoot";
            case InstallTutorialStep.ExplainReviewInfo:
                return "ReviewPanelRoot";
            default:
                return string.Empty;
        }
    }

    private static string GetSharedPanelTutorialFocusTargetName(InstallTutorialStep step)
    {
        switch (step)
        {
            case InstallTutorialStep.ExplainOperateInfo:
                return "OperatePanelRoot";
            case InstallTutorialStep.ExplainEconomyInfo:
                return "EconomyPanelRoot";
            case InstallTutorialStep.ExplainReviewInfo:
                return "ReviewPanelRoot";
            default:
                return "SharedPanelContentRoot";
        }
    }

    private string GetSharedPanelTutorialFocusTargetNameForLastRect(InstallTutorialStep step)
    {
        if (installTutorialLastSharedPanelAreaFallbackUsed)
        {
            switch (step)
            {
                case InstallTutorialStep.ExplainOperateInfo:
                    return "SharedPanelAreaFallback/OperateTight";
                case InstallTutorialStep.ExplainEconomyInfo:
                    return "SharedPanelAreaFallback/Economy";
                case InstallTutorialStep.ExplainReviewInfo:
                    return "SharedPanelAreaFallback/Review";
            }
        }

        return GetSharedPanelTutorialFocusTargetName(step);
    }

    private Rect ApplyInstallTutorialSharedPanelFocusTuning(Rect rect)
    {
        if (installTutorialUseManualSharedPanelFocus)
        {
            Vector2 size = new Vector2(
                Mathf.Max(1f, installTutorialSharedPanelFocusSize.x),
                Mathf.Max(1f, installTutorialSharedPanelFocusSize.y));
            rect = new Rect(installTutorialSharedPanelFocusCenter - (size * 0.5f), size);
        }
        else
        {
            Vector2 center = rect.center + installTutorialSharedPanelFocusOffset;
            Vector2 size = new Vector2(
                Mathf.Max(1f, rect.width + installTutorialSharedPanelFocusSizeDelta.x),
                Mathf.Max(1f, rect.height + installTutorialSharedPanelFocusSizeDelta.y));
            rect = new Rect(center - (size * 0.5f), size);
        }

        return ClampRectToScreen(rect);
    }

    private static Rect ApplyInstallTutorialRectTuning(Rect rect, Vector2 offset, Vector2 sizeDelta)
    {
        Vector2 size = new Vector2(
            Mathf.Max(1f, rect.width + sizeDelta.x),
            Mathf.Max(1f, rect.height + sizeDelta.y));
        Vector2 center = rect.center + offset;
        return ClampRectToScreen(new Rect(center - (size * 0.5f), size));
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

    private RectTransform FindInstallTutorialFirstStaffRow()
    {
        Transform row = staffPopupRoot != null ? FindDeepChild(staffPopupRoot, "StaffRow_0") : null;
        return row != null ? row.GetComponent<RectTransform>() : null;
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
        PlacementManager placement = installTutorialBoundPlacementManager != null
            ? installTutorialBoundPlacementManager
            : placementManager;
        int footprintX = 0;
        int footprintY = 0;
        int footprintWidth = 0;
        int footprintHeight = 0;
        bool focusCurrentFootprint = placement != null &&
                                     placement.TryGetCurrentPlacementArea(
                                         out footprintX,
                                         out footprintY,
                                         out footprintWidth,
                                         out footprintHeight,
                                         suggestFirstAvailable: true);

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

                bounds = hasBounds ? UnionRects(bounds, cellRect) : cellRect;
                hasBounds = true;

                bool isFootprintCell = x >= footprintX &&
                                       x < footprintX + footprintWidth &&
                                       y >= footprintY &&
                                       y < footprintY + footprintHeight;
                if (!focusCurrentFootprint || isFootprintCell)
                {
                    rects.Add(cellRect);
                }
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
        return GetInstallTutorialTargetRect(FindInstallTutorialNamedTargetTransform(targetName), padding, fallback);
    }

    private RectTransform FindInstallTutorialNamedTargetTransform(string targetName)
    {
        return FindDeepChild(runtimeRoot, targetName)?.GetComponent<RectTransform>();
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
        return TryGetInstallTutorialTargetRect(target, padding, out Rect rect) ? rect : fallback;
    }

    private bool TryGetInstallTutorialTargetRect(RectTransform target, Vector2 padding, out Rect rect)
    {
        rect = Rect.zero;
        RectTransform overlayRect = installTutorialRoot != null ? installTutorialRoot.GetComponent<RectTransform>() : null;
        if (target == null || overlayRect == null)
        {
            return false;
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
            return false;
        }

        rect = Rect.MinMaxRect(
            min.x - padding.x,
            min.y - padding.y,
            max.x + padding.x,
            max.y + padding.y);
        return rect.width > 1f && rect.height > 1f;
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

    private Vector2 GetInstallTutorialCalloutAboveRect(Rect targetRect, float width, float height, float gap = 34f)
    {
        return ClampInstallTutorialCallout(
            new Vector2(targetRect.center.x, targetRect.yMax + (height * 0.5f) + gap),
            width,
            height);
    }

    private void DrawInstallTutorialArrow(InstallTutorialLayout layout)
    {
        if (!layout.showArrow)
        {
            return;
        }

        Rect targetRect = GetInstallTutorialArrowTargetRect(layout);
        if (targetRect.width <= 1f || targetRect.height <= 1f)
        {
            Debug.LogWarning($"[InstallTutorialTrace] TutorialArrow hidden because target rect is not usable. step={installTutorialStep} target={layout.focusTargetName}", this);
            return;
        }

        Sprite arrowSprite = GeneratedRuntimeSprites.Load(InstallTutorialArrowSprite);
        if (arrowSprite == null)
        {
            Debug.LogWarning($"[InstallTutorialTrace] TutorialArrow hidden because sprite failed to load: Resources/{InstallTutorialArrowSprite}", this);
            return;
        }

        GetInstallTutorialArrowPlacement(
            targetRect,
            layout.calloutCenter,
            out Vector2 arrowCenter,
            out float arrowRotation,
            out string anchorSide);
        arrowCenter += layout.arrowOffset;

        if (layout.hasArrowOverride)
        {
            arrowCenter = layout.arrowOverrideCenter;
            arrowRotation = layout.arrowOverrideRotation;
        }

        Transform parent = GetInstallTutorialContentParent();
        if (parent == null)
        {
            return;
        }

        GameObject arrow = GameUiFactory.CreateNode(parent, "InstallTutorialArrow", typeof(CanvasRenderer), typeof(Image));
        Image image = arrow.GetComponent<Image>();
        image.sprite = arrowSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;

        RectTransform rect = arrow.GetComponent<RectTransform>();
        SetRect(rect, arrowCenter.x, arrowCenter.y, InstallTutorialArrowSize.x, InstallTutorialArrowSize.y, true);
        rect.anchoredPosition3D = new Vector3(arrowCenter.x, arrowCenter.y, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = InstallTutorialArrowSize;
        rect.localEulerAngles = new Vector3(0f, 0f, arrowRotation);
        rect.localScale = Vector3.one;

        Debug.Log(
            $"[InstallTutorialTrace] TutorialArrow pointer layout step={installTutorialStep} target={layout.focusTargetName} size=({InstallTutorialArrowSize.x:F0},{InstallTutorialArrowSize.y:F0}) angle={arrowRotation:F1} pos=({arrowCenter.x:F1},{arrowCenter.y:F1}) bubbleCenter=({layout.calloutCenter.x:F1},{layout.calloutCenter.y:F1}) targetCenter=({targetRect.center.x:F1},{targetRect.center.y:F1}) anchorSide={anchorSide} sprite={InstallTutorialArrowSprite}",
            this);
    }

    private static void GetInstallTutorialArrowPlacement(
        Rect targetRect,
        Vector2 calloutCenter,
        out Vector2 arrowCenter,
        out float rotationZ,
        out string anchorSide)
    {
        Vector2 fromTargetToCallout = calloutCenter - targetRect.center;
        if (Mathf.Abs(fromTargetToCallout.x) > Mathf.Abs(fromTargetToCallout.y))
        {
            if (fromTargetToCallout.x < 0f)
            {
                anchorSide = "Left";
                arrowCenter = new Vector2(targetRect.xMin - InstallTutorialArrowTargetGap, targetRect.center.y);
                rotationZ = 90f;
            }
            else
            {
                anchorSide = "Right";
                arrowCenter = new Vector2(targetRect.xMax + InstallTutorialArrowTargetGap, targetRect.center.y);
                rotationZ = -90f;
            }
        }
        else if (fromTargetToCallout.y < 0f)
        {
            anchorSide = "Bottom";
            arrowCenter = new Vector2(targetRect.center.x, targetRect.yMin - InstallTutorialArrowTargetGap);
            rotationZ = 180f;
        }
        else
        {
            anchorSide = "Top";
            arrowCenter = new Vector2(targetRect.center.x, targetRect.yMax + InstallTutorialArrowTargetGap);
            rotationZ = 0f;
        }

        float halfWidth = InstallTutorialArrowSize.x * 0.5f;
        float halfHeight = InstallTutorialArrowSize.y * 0.5f;
        arrowCenter = new Vector2(
            Mathf.Clamp(arrowCenter.x, InstallTutorialScreenRect.xMin + halfWidth, InstallTutorialScreenRect.xMax - halfWidth),
            Mathf.Clamp(arrowCenter.y, InstallTutorialScreenRect.yMin + halfHeight, InstallTutorialScreenRect.yMax - halfHeight));
    }

    private Rect GetInstallTutorialArrowTargetRect(InstallTutorialLayout layout)
    {
        if (layout.hasArrowTargetRect &&
            layout.arrowTargetRect.width > 1f &&
            layout.arrowTargetRect.height > 1f)
        {
            return layout.arrowTargetRect;
        }

        if (layout.focusRects != null && layout.focusRects.Count > 0)
        {
            Rect bestRect = layout.focusRects[0];
            float bestDistance = GetInstallTutorialPointToRectDistanceSquared(layout.targetCenter, bestRect);
            for (int i = 1; i < layout.focusRects.Count; i++)
            {
                float distance = GetInstallTutorialPointToRectDistanceSquared(layout.targetCenter, layout.focusRects[i]);
                if (distance < bestDistance)
                {
                    bestRect = layout.focusRects[i];
                    bestDistance = distance;
                }
            }

            return bestRect;
        }

        if (layout.hasFocus && layout.focusRect.width > 1f && layout.focusRect.height > 1f)
        {
            return layout.focusRect;
        }

        return new Rect(layout.targetCenter.x - 48f, layout.targetCenter.y - 48f, 96f, 96f);
    }

    private static float GetInstallTutorialPointToRectDistanceSquared(Vector2 point, Rect rect)
    {
        float x = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
        float y = Mathf.Clamp(point.y, rect.yMin, rect.yMax);
        return ((new Vector2(x, y)) - point).sqrMagnitude;
    }

    private static Vector2 GetInstallTutorialArrowStartPoint(Rect calloutRect, Vector2 targetCenter, out string anchorSide)
    {
        Vector2 fromCallout = targetCenter - calloutRect.center;
        float safeX = Mathf.Min(82f, calloutRect.width * 0.28f);
        float safeY = Mathf.Min(46f, calloutRect.height * 0.28f);
        if (Mathf.Abs(fromCallout.x) > Mathf.Abs(fromCallout.y))
        {
            bool right = fromCallout.x > 0f;
            anchorSide = right ? "Right" : "Left";
            return new Vector2(
                right ? calloutRect.xMax : calloutRect.xMin,
                Mathf.Clamp(targetCenter.y, calloutRect.yMin + safeY, calloutRect.yMax - safeY));
        }

        bool top = fromCallout.y > 0f;
        anchorSide = top ? "Top" : "Bottom";
        return new Vector2(
            Mathf.Clamp(targetCenter.x, calloutRect.xMin + safeX, calloutRect.xMax - safeX),
            top ? calloutRect.yMax : calloutRect.yMin);
    }

    private static Vector2 GetInstallTutorialArrowTipPoint(Rect targetRect, Vector2 arrowStart, string anchorSide)
    {
        switch (anchorSide)
        {
            case "Top":
                return new Vector2(Mathf.Clamp(arrowStart.x, targetRect.xMin, targetRect.xMax), targetRect.yMin);
            case "Bottom":
                return new Vector2(Mathf.Clamp(arrowStart.x, targetRect.xMin, targetRect.xMax), targetRect.yMax);
            case "Right":
                return new Vector2(targetRect.xMin, Mathf.Clamp(arrowStart.y, targetRect.yMin, targetRect.yMax));
            case "Left":
                return new Vector2(targetRect.xMax, Mathf.Clamp(arrowStart.y, targetRect.yMin, targetRect.yMax));
            default:
                return targetRect.center;
        }
    }

    private void DrawInstallTutorialCallout(InstallTutorialLayout layout)
    {
        Transform parent = GetInstallTutorialContentParent();
        if (parent == null)
        {
            return;
        }

        GameObject box = CreateGeneratedImage(
            parent,
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

        float messageY = layout.canTapCallout ? 12f : 0f;
        float messageHeight = layout.canTapCallout
            ? Mathf.Max(96f, layout.calloutHeight - 54f)
            : Mathf.Max(86f, layout.calloutHeight - 34f);
        int messageFontSize = layout.calloutHeight <= 150f ? 28 : 31;
        Text messageText = CreateInstallTutorialText(
            box.transform,
            "InstallTutorialMessage",
            layout.message,
            messageFontSize,
            theme.Ink,
            TextAnchor.MiddleLeft,
            58f,
            messageY,
            layout.calloutWidth - 150f,
            messageHeight);
        messageText.lineSpacing = 0.94f;
        messageText.resizeTextForBestFit = true;
        messageText.resizeTextMinSize = 24;
        messageText.resizeTextMaxSize = messageFontSize;

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
                -layout.calloutHeight * 0.5f + 24f,
                170f,
                24f);
        }
    }

    private Transform GetInstallTutorialContentParent()
    {
        if (installTutorialContentRoot == null)
        {
            EnsureInstallTutorialContentRoot();
        }

        return installTutorialContentRoot != null ? installTutorialContentRoot : installTutorialRoot;
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
