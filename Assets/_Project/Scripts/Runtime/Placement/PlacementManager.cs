using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [프로토타입/MVP]
/// 기구 설치 + 설치된 기구 선택/이동/철거까지 포함한 배치 매니저.
/// </summary>
public class PlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WalletManager walletManager;
    [SerializeField] private Transform placedObjectsRoot;
    [SerializeField] private EquipmentCatalog equipmentCatalog;

    [Header("Current Build")]
    [SerializeField] private int buildWidth = 2;
    [SerializeField] private int buildHeight = 2;
    [SerializeField] private int buildCost = 3000;

    [Header("Colors")]
    [SerializeField] private Color previewValidColor = new Color(0.46f, 1.00f, 0.20f, 0.06f);
    [SerializeField] private Color previewInvalidColor = new Color(0.95f, 0.34f, 0.30f, 0.10f);
    [SerializeField] private Color previewValidBorderColor = new Color(0.62f, 1.00f, 0.25f, 0.94f);
    [SerializeField] private Color previewInvalidBorderColor = new Color(1.00f, 0.30f, 0.18f, 0.94f);
    [SerializeField] private Color placedColor = new Color(0.88f, 0.58f, 0.22f, 0.95f);
    [SerializeField] private Color selectedPlacedColorTint = new Color(0.98f, 0.86f, 0.38f, 1f);
    [SerializeField] private Color movingGhostColor = new Color(0.94f, 0.98f, 1f, 0.18f);

    [Header("Edit Prototype")]
    [SerializeField, Range(0f, 1f)] private float sellRefundRate = 0.50f;
    [SerializeField] private bool showSelectedObjectPanel = true;
    [SerializeField] private bool useBottomHudHost = true;
    [SerializeField] private float portraitActionPanelBottomOffset = 212f;
    [SerializeField] private float landscapeActionPanelBottomOffset = 24f;

    private static Sprite cachedWhiteSprite;
    private static Material outlineMaterial;

    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private SpriteRenderer previewGlowRenderer;
    private SpriteRenderer customPreviewRenderer;
    private SpriteRenderer[] customPreviewOutlineRenderers;

    private int currentAnchorX = -1;
    private int currentAnchorY = -1;

    private bool isAreaAvailableCurrent = false;
    private bool canAffordCurrent = false;
    private bool canPlaceCurrent = false;
    private bool isInitialized = false;

    private EquipmentDefinition currentDefinition;
    private string currentEquipmentId = string.Empty;

    private readonly List<PlacedObjectSaveData> placedObjectDataList = new List<PlacedObjectSaveData>();
    private readonly List<GameObject> placedObjectVisuals = new List<GameObject>();

    private int selectedPlacedObjectIndex = -1;
    private bool isPlacementPreviewActive = false;
    private bool isRelocatingSelectedObject = false;
    private PlacedObjectSaveData relocatingSnapshot;
    private EquipmentDefinition relocatingDefinition;
    private bool wasBuildModeActive;

    private GUIStyle actionPanelBoxStyle;
    private GUIStyle actionTitleStyle;
    private GUIStyle actionInfoStyle;
    private GUIStyle actionButtonStyle;

    public enum HudActionId
    {
        None,
        ConfirmPlacement,
        CancelPlacement,
        BeginMove,
        CancelMove,
        Sell,
        CancelConstruction,
        ClearSelection,
        Repair,
        SkipConstruction
    }

    public struct HudActionDescriptor
    {
        public HudActionId actionId;
        public string label;
        public bool isEnabled;
    }

    public struct SelectedObjectHudState
    {
        public string eyebrow;
        public string title;
        public string status;
        public string detail;
        public HudActionDescriptor primaryAction;
        public HudActionDescriptor secondaryAction;
        public HudActionDescriptor tertiaryAction;
        public HudActionDescriptor quaternaryAction;
    }

    public event Action PlayerPlacedObject;
    public event Action<EquipmentDefinition> ObjectPlaced;

    public int PlacedObjectCount => placedObjectDataList.Count;
    public EquipmentDefinition CurrentDefinition => currentDefinition;
    public string CurrentEquipmentId => currentEquipmentId;
    public bool HasSelectedPlacedObject => selectedPlacedObjectIndex >= 0 && selectedPlacedObjectIndex < placedObjectDataList.Count;
    public bool IsRelocatingSelectedObject => isRelocatingSelectedObject;
    public bool IsPlacementPreviewActive => BuildPlayModeManager.IsBuildMode && isPlacementPreviewActive && currentDefinition != null && !HasSelectedPlacedObject;
    public bool HasCurrentPlacementAnchor => currentAnchorX >= 0 && currentAnchorY >= 0;
    public bool CanConfirmCurrentPlacement => canPlaceCurrent;

    public bool TryGetCurrentPlacementArea(
        out int anchorX,
        out int anchorY,
        out int width,
        out int height,
        bool suggestFirstAvailable = false)
    {
        anchorX = -1;
        anchorY = -1;
        width = Mathf.Max(1, buildWidth);
        height = Mathf.Max(1, buildHeight);

        if (!BuildPlayModeManager.IsBuildMode ||
            (!isPlacementPreviewActive && !isRelocatingSelectedObject) ||
            (HasSelectedPlacedObject && !isRelocatingSelectedObject))
        {
            return false;
        }

        if (currentAnchorX >= 0 && currentAnchorY >= 0)
        {
            anchorX = currentAnchorX;
            anchorY = currentAnchorY;
            return true;
        }

        if (!suggestFirstAvailable || gridManager == null)
        {
            return false;
        }

        for (int y = 0; y <= gridManager.Height - height; y++)
        {
            for (int x = 0; x <= gridManager.Width - width; x++)
            {
                if (!gridManager.IsAreaAvailable(x, y, width, height))
                {
                    continue;
                }

                anchorX = x;
                anchorY = y;
                return true;
            }
        }

        return false;
    }

    public bool TryGetPlacementHudState(out SelectedObjectHudState state)
    {
        if (TryGetPlacementPreviewHudState(out state))
        {
            return true;
        }

        return TryGetSelectedObjectHudState(out state);
    }

    public bool TryGetPlacementPreviewHudState(out SelectedObjectHudState state)
    {
        state = default;

        if (!BuildPlayModeManager.IsBuildMode || (!isPlacementPreviewActive && !isRelocatingSelectedObject))
        {
            return false;
        }

        EquipmentDefinition definition = isRelocatingSelectedObject ? relocatingDefinition : currentDefinition;
        PlacedObjectSaveData selectedData = isRelocatingSelectedObject ? GetSelectedPlacedObjectData() : null;
        string displayName = selectedData != null ? GetPlacedObjectDisplayName(selectedData) : GetDefinitionDisplayName(definition);

        state.eyebrow = isRelocatingSelectedObject ? "기구 이동" : "배치 미리보기";
        state.title = displayName;

        if (!HasCurrentPlacementAnchor)
        {
            state.status = "타일을 눌러 위치를 정해 주세요.";
        }
        else if (!isAreaAvailableCurrent)
        {
            state.status = "이 위치에는 설치할 수 없습니다.";
        }
        else if (!canAffordCurrent)
        {
            state.status = "자금이 부족합니다.";
        }
        else
        {
            state.status = isRelocatingSelectedObject ? "이 위치로 이동할 수 있습니다." : "설치 가능한 위치입니다.";
        }

        if (isRelocatingSelectedObject)
        {
            state.detail = "설치를 누르면 새 위치로 확정됩니다.";
        }
        else
        {
            int minutes = definition != null ? definition.BaseInstallationMinutes : 0;
            string installTime = minutes <= 0 ? "즉시 설치" : $"설치 시간 {minutes}분";
            state.detail = $"비용 {buildCost:N0} G  |  {buildWidth}x{buildHeight}칸  |  {installTime}";
        }

        state.primaryAction = CreateHudAction(HudActionId.ConfirmPlacement, "설치", canPlaceCurrent);
        state.secondaryAction = CreateHudAction(HudActionId.CancelPlacement, "취소", true);
        return true;
    }

    public bool TryGetSelectedObjectHudState(out SelectedObjectHudState state)
    {
        state = default;

        if (!HasSelectedPlacedObject)
        {
            return false;
        }

        PlacedObjectSaveData selectedData = GetSelectedPlacedObjectData();
        if (selectedData == null)
        {
            return false;
        }

        string displayName = GetPlacedObjectDisplayName(selectedData);
        int refundAmount = GetSellRefundAmount(selectedData);
        int durabilityPercent = GetDurabilityPercent(selectedData);

        state.title = displayName;
        state.detail = $"내구도 {durabilityPercent}%  |  철거 환급 {refundAmount:N0} G";

        if (isRelocatingSelectedObject)
        {
            state.eyebrow = "이동 배치";
            state.status = $"{displayName}을 옮길 새 칸을 눌러 주세요.";
            state.primaryAction = CreateHudAction(HudActionId.CancelPlacement, "취소", true);
            return true;
        }

        if (selectedData.isUnderConstruction)
        {
            TimeSpan remaining = GetConstructionRemaining(selectedData);
            int skipCost = GetConstructionSkipCost(selectedData);
            bool canSkip = walletManager == null || walletManager.CurrentStarCoin >= skipCost;

            state.eyebrow = "설치 진행 중";
            state.status = $"{remaining.Minutes:D2}:{remaining.Seconds:D2} 뒤 완료  |  별코인 {skipCost}개로 즉시 완료";
            state.primaryAction = CreateHudAction(HudActionId.SkipConstruction, $"즉시 완료 ({skipCost} SC)", canSkip);
            state.secondaryAction = CreateHudAction(HudActionId.CancelConstruction, "취소", true);
            return true;
        }

        if (selectedData.isBroken)
        {
            int repairCost = GetRepairCost(selectedData);
            bool canRepair = walletManager == null || walletManager.CurrentCash >= repairCost;

            state.eyebrow = "고장 상태";
            state.status = "수리하거나 위치를 옮기고, 필요하면 철거할 수 있습니다.";
            state.primaryAction = CreateHudAction(HudActionId.BeginMove, "이동", true);
            state.secondaryAction = CreateHudAction(HudActionId.Sell, "철거", true);
            state.tertiaryAction = CreateHudAction(HudActionId.Repair, $"수리 ({repairCost:N0} G)", canRepair);
            return true;
        }

        state.eyebrow = "선택된 기구";
        state.status = $"상태: 정상  |  내구도 {durabilityPercent}%";
        state.primaryAction = CreateHudAction(HudActionId.BeginMove, "이동", true);
        state.secondaryAction = CreateHudAction(HudActionId.Sell, "철거", true);
        int normalRepairCost = GetRepairCost(selectedData);
        state.tertiaryAction = CreateHudAction(HudActionId.Repair, normalRepairCost > 0 ? $"수리 ({normalRepairCost:N0} G)" : "수리", durabilityPercent < 100);
        return true;
    }

    public bool ExecuteHudAction(HudActionId actionId)
    {
        switch (actionId)
        {
            case HudActionId.ConfirmPlacement:
                return ConfirmCurrentPlacement();
            case HudActionId.CancelPlacement:
                CancelCurrentPlacement();
                return true;
            case HudActionId.BeginMove:
                BeginRelocation();
                return true;
            case HudActionId.CancelMove:
                CancelRelocation();
                return true;
            case HudActionId.Sell:
                SellSelectedPlacedObject();
                return true;
            case HudActionId.CancelConstruction:
                return CancelSelectedConstruction();
            case HudActionId.ClearSelection:
                ClearSelectedPlacedObject();
                return true;
            case HudActionId.Repair:
                return TryRepairSelectedPlacedObject();
            case HudActionId.SkipConstruction:
                return TrySkipSelectedConstructionWithStarCoin();
            default:
                return false;
        }
    }

    public void SetBottomHudHostEnabled(bool enabled)
    {
        useBottomHudHost = enabled;
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (walletManager == null) walletManager = FindFirstObjectByType<WalletManager>();

        EnsureEquipmentCatalog();

        if (gridManager == null)
        {
            Debug.LogError("[PlacementManager] GridManager를 찾지 못했어.");
            return;
        }

        if (currentDefinition == null && EquipmentSelectionState.CurrentDefinition != null)
        {
            ApplyBuildDefinition(EquipmentSelectionState.CurrentDefinition);
        }

        EnsurePlacedObjectsRoot();
        CreatePreviewObject();

        gridManager.HoveredCellChanged += HandleHoveredCellChanged;
        gridManager.CellClicked += HandleCellClicked;

        isInitialized = true;
        wasBuildModeActive = BuildPlayModeManager.IsBuildMode;
        HidePreviewCompletely();
        RefreshAllPlacedVisualStates();

        if (wasBuildModeActive)
        {
            RefreshPlacementState();
            UpdatePreview();
        }
    }

    public void SetPlacementDefinition(EquipmentDefinition definition)
    {
        if (definition == null) return;
        if (isRelocatingSelectedObject) return;

        ApplyBuildDefinition(definition);

        if (!isInitialized) return;

        RefreshPlacementState();
        UpdatePreview();
    }

    public void BeginPlacement(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (isRelocatingSelectedObject)
        {
            CancelRelocation();
        }

        ApplyBuildDefinition(definition);
        selectedPlacedObjectIndex = -1;
        isPlacementPreviewActive = true;
        currentAnchorX = -1;
        currentAnchorY = -1;

        if (Application.isPlaying)
        {
            BuildPlayModeManager.EnterBuildMode();
        }

        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
    }

    public List<PlacedObjectSaveData> GetPlacedObjectSaveDataList()
    {
        List<PlacedObjectSaveData> copy = new List<PlacedObjectSaveData>(placedObjectDataList.Count);
        foreach (PlacedObjectSaveData data in placedObjectDataList)
        {
            PlacedObjectSaveData cloned = PlacedObjectSaveData.Clone(data);
            if (cloned != null)
            {
                cloned.runtimeDefinition = null;
                copy.Add(cloned);
            }
        }
        return copy;
    }

    public IReadOnlyList<PlacedObjectSaveData> GetPlacedObjectRuntimeData()
    {
        return placedObjectDataList;
    }

    public void LoadPlacedObjects(List<PlacedObjectSaveData> saveDataList)
    {
        ClearAllPlacedObjects();
        EnsureEquipmentCatalog();

        if (saveDataList == null || saveDataList.Count == 0)
        {
            RefreshPlacementState();
            UpdatePreview();
            return;
        }

        foreach (PlacedObjectSaveData data in saveDataList)
        {
            if (data == null) continue;

            EquipmentDefinition resolvedDefinition = ResolveDefinitionFromSaveData(data);
            int resolvedWidth = ResolveWidth(data, resolvedDefinition);
            int resolvedHeight = ResolveHeight(data, resolvedDefinition);

            if (!gridManager.IsAreaAvailable(data.anchorX, data.anchorY, resolvedWidth, resolvedHeight)) continue;

            PlaceObjectAt(data.anchorX, data.anchorY, resolvedWidth, resolvedHeight, resolvedDefinition, data, false);
        }

        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
    }

    public void ClearAllPlacedObjects()
    {
        foreach (GameObject placedObject in placedObjectVisuals)
        {
            if (placedObject != null) Destroy(placedObject);
        }

        placedObjectVisuals.Clear();
        placedObjectDataList.Clear();
        selectedPlacedObjectIndex = -1;
        isPlacementPreviewActive = false;
        isRelocatingSelectedObject = false;
        relocatingSnapshot = null;
        relocatingDefinition = null;

        if (gridManager != null)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                for (int x = 0; x < gridManager.Width; x++)
                {
                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell != null) cell.SetOccupied(false);
                }
            }
        }

        RestoreBuildDefinitionFromSelection();
        RefreshPlacementState();
        UpdatePreview();
    }

    private void Update()
    {
        if (!isInitialized) return;

        bool isBuildModeActive = BuildPlayModeManager.IsBuildMode;
        if (isBuildModeActive != wasBuildModeActive)
        {
            wasBuildModeActive = isBuildModeActive;
            HandleBuildModeChanged(isBuildModeActive);
        }

        bool anyCompleted = false;
        long nowTicks = System.DateTime.UtcNow.Ticks;

        for (int i = 0; i < placedObjectDataList.Count; i++)
        {
            var data = placedObjectDataList[i];
            if (data != null && data.isUnderConstruction)
            {
                if (nowTicks >= data.constructionEndTimeTicks)
                {
                    data.isUnderConstruction = false;
                    anyCompleted = true;
                    Debug.Log($"[PlacementManager] {data.displayName} 설치가 완료되었습니다.");
                }
            }
        }

        if (anyCompleted)
        {
            RefreshAllPlacedVisualStates();
            PlayerPlacedObject?.Invoke();
        }
    }

    private void OnGUI()
    {
        // The runtime HUD now owns selected-object presentation.
        return;
    }

    public void DrawBottomHudContent(Rect contentRect)
    {
        EnsureActionPanelStyles();

        if (!BuildPlayModeManager.IsBuildMode)
        {
            GUI.Box(contentRect, GUIContent.none, actionPanelBoxStyle);
            GUI.Label(new Rect(contentRect.x + 10f, contentRect.y + 10f, contentRect.width - 20f, contentRect.height - 20f), "플레이 모드야.", actionInfoStyle);
            return;
        }

        if (!HasSelectedPlacedObject)
        {
            GUI.Box(contentRect, GUIContent.none, actionPanelBoxStyle);
            GUI.Label(new Rect(contentRect.x + 10f, contentRect.y + 10f, contentRect.width - 20f, contentRect.height - 20f), "선택된 기구가 없어.", actionInfoStyle);
            return;
        }

        DrawSelectedObjectPanel(contentRect, false);
    }

    private void DrawSelectedObjectPanel(Rect panelRect, bool registerUiBlocker)
    {
        if (registerUiBlocker) ScreenUiBlocker.RegisterRect(GetInstanceID(), panelRect);
        GUI.Box(panelRect, GUIContent.none, actionPanelBoxStyle);

        PlacedObjectSaveData selectedData = GetSelectedPlacedObjectData();
        if (selectedData == null) return;

        string displayName = GetPlacedObjectDisplayName(selectedData);
        int refundAmount = GetSellRefundAmount(selectedData);

        GUI.Label(
            new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, 20f),
            isRelocatingSelectedObject ? "기구 이동 중 [프로토타입]" : 
            (selectedData.isUnderConstruction ? "기구 설치 대기중 [프로토타입]" : "설치된 기구 편집 [프로토타입]"),
            actionTitleStyle
        );

        if (isRelocatingSelectedObject)
        {
            string moveInfo = $"{displayName}\n원래 위치: ({relocatingSnapshot.anchorX}, {relocatingSnapshot.anchorY}) / 새 위치 터치해";
            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 30f, panelRect.width - 20f, 34f), moveInfo, actionInfoStyle);
            Rect cancelRect = new Rect(panelRect.x + 10f, panelRect.yMax - 34f, panelRect.width - 20f, 24f);
            if (registerUiBlocker) ScreenUiBlocker.RegisterRect(GetInstanceID(), cancelRect);
            if (GUI.Button(cancelRect, "이동 취소", actionButtonStyle)) CancelRelocation();
            return;
        }

        if (selectedData.isUnderConstruction)
        {
            System.TimeSpan remaining = new System.DateTime(selectedData.constructionEndTimeTicks) - System.DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0) remaining = System.TimeSpan.Zero;
            int remainingMinutes = Mathf.Max(1, Mathf.CeilToInt((float)remaining.TotalMinutes));
            int skipCost = Mathf.Max(1, Mathf.CeilToInt(remainingMinutes / 5.0f));

            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 30f, panelRect.width - 20f, 18f), $"{displayName} / 배송 대기중", actionInfoStyle);
            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 48f, panelRect.width - 20f, 18f), $"남은 시간: {remaining.Minutes:D2}분 {remaining.Seconds:D2}초", actionInfoStyle);

            float skipY = panelRect.yMax - 34f;
            float gapSkip = 6f;
            float ww = (panelRect.width - 20f - gapSkip) / 2f;
            Rect skipRect = new Rect(panelRect.x + 10f, skipY, ww, 24f);
            Rect closeRect = new Rect(skipRect.xMax + gapSkip, skipY, ww, 24f);

            if (registerUiBlocker) { ScreenUiBlocker.RegisterRect(GetInstanceID(), skipRect); ScreenUiBlocker.RegisterRect(GetInstanceID(), closeRect); }

            if (GUI.Button(closeRect, "닫기", actionButtonStyle)) ClearSelectedPlacedObject();

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.yellow;
            if (GUI.Button(skipRect, $"즉시 완료 ({skipCost} 코인)", actionButtonStyle))
            {
                if (walletManager != null && walletManager.TrySpendStarCoin(skipCost, "타이머 스킵"))
                {
                    selectedData.isUnderConstruction = false;
                    RefreshAllPlacedVisualStates();
                    ClearSelectedPlacedObject();
                }
            }
            GUI.backgroundColor = prevColor;
            return;
        }

        GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 30f, panelRect.width - 20f, 18f), $"{displayName} / 크기 {selectedData.width}x{selectedData.height}", actionInfoStyle);
        GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 48f, panelRect.width - 20f, 18f), $"철거 환불 예상: {refundAmount:N0}", actionInfoStyle);

        float buttonY = panelRect.yMax - 34f;
        float gap = 6f;
        int btnCount = selectedData != null && selectedData.isBroken ? 4 : 3; float buttonWidth = (panelRect.width - 20f - (gap * (btnCount - 1))) / btnCount;

        Rect moveRect = new Rect(panelRect.x + 10f, buttonY, buttonWidth, 24f);
        Rect removeRect = new Rect(moveRect.xMax + gap, buttonY, buttonWidth, 24f);
        Rect clearRect = new Rect(removeRect.xMax + gap, buttonY, buttonWidth, 24f);
        Rect repairRect = new Rect(clearRect.xMax + gap, buttonY, buttonWidth, 24f);

        if (registerUiBlocker)
        {
            ScreenUiBlocker.RegisterRect(GetInstanceID(), moveRect);
            ScreenUiBlocker.RegisterRect(GetInstanceID(), removeRect);
            ScreenUiBlocker.RegisterRect(GetInstanceID(), clearRect);
        }

        if (GUI.Button(moveRect, "이동", actionButtonStyle)) BeginRelocation();
        if (GUI.Button(removeRect, "철거", actionButtonStyle)) SellSelectedPlacedObject();
        if (GUI.Button(clearRect, "해제", actionButtonStyle)) ClearSelectedPlacedObject();

        if (selectedData.isBroken)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (registerUiBlocker) ScreenUiBlocker.RegisterRect(GetInstanceID(), repairRect);
            if (GUI.Button(repairRect, "기구 수리 (1,500)", actionButtonStyle))
            {
                if (walletManager.TrySpend(1500, "기구 수리 서비스"))
                {
                    selectedData.isBroken = false;
                    selectedData.usageCount = 0;
                    ClearSelectedPlacedObject();
                }
            }
            GUI.backgroundColor = prev;
        }
    }

    private void HandleBuildModeChanged(bool isBuildModeActive)
    {
        if (!isBuildModeActive)
        {
            if (isRelocatingSelectedObject) CancelRelocation();
            isPlacementPreviewActive = false;
            ClearSelectedPlacedObject();
            HidePreviewCompletely();
            RefreshAllPlacedVisualStates();
            return;
        }

        if (!isPlacementPreviewActive)
        {
            RestoreBuildDefinitionFromSelection();
        }
        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
    }

    private void OnDestroy() { Unsubscribe(); }
    private void OnDisable() { Unsubscribe(); }
    private void Unsubscribe()
    {
        if (!isInitialized || gridManager == null) return;
        gridManager.HoveredCellChanged -= HandleHoveredCellChanged;
        gridManager.CellClicked -= HandleCellClicked;
        isInitialized = false;
    }

    private void HandleHoveredCellChanged(GridCell cell)
    {
        string cellText = cell != null ? $"({cell.X},{cell.Y})" : "null";
        Debug.Log($"[InstallTutorialTrace] PlacementManager.HandleHoveredCellChanged cell={cellText} buildMode={BuildPlayModeManager.IsBuildMode} previewActive={isPlacementPreviewActive} relocating={isRelocatingSelectedObject} selected={HasSelectedPlacedObject} anchorBefore=({currentAnchorX},{currentAnchorY}) definition=\"{(currentDefinition != null ? currentDefinition.DisplayName : "null")}\"");
        if (!IsPlacementVisualModeActive())
        {
            if (!BuildPlayModeManager.IsBuildMode)
            {
                HidePreviewCompletely();
            }
            else
            {
                HidePreviewVisualOnly();
            }

            return;
        }

        if (cell == null)
        {
            RefreshPlacementState();
            UpdatePreview();
            return;
        }

        currentAnchorX = cell.X;
        currentAnchorY = cell.Y;
        Debug.Log($"[InstallTutorialTrace] PlacementManager.HandleHoveredCellChanged anchorAfter=({currentAnchorX},{currentAnchorY}) cell={cellText}");

        RefreshPlacementState();
        UpdatePreview();
    }


    private void HandleCellClicked(GridCell cell)
    {
        if (cell == null)
        {
            if (!isRelocatingSelectedObject && !isPlacementPreviewActive)
            {
                ClearSelectedPlacedObject();
            }

            return;
        }

        currentAnchorX = cell.X;
        currentAnchorY = cell.Y;

        if (!BuildPlayModeManager.IsBuildMode)
        {
            if (TryGetPlacedObjectIndexAtCell(cell.X, cell.Y, out int playModeObjectIndex))
            {
                SelectPlacedObject(playModeObjectIndex);
                HidePreviewVisualOnly();
                return;
            }

            ClearSelectedPlacedObject();
            return;
        }

        if (isRelocatingSelectedObject || isPlacementPreviewActive)
        {
            RefreshPlacementState();
            UpdatePreview();
            return;
        }

        if (TryGetPlacedObjectIndexAtCell(cell.X, cell.Y, out int occupiedObjectIndex))
        {
            SelectPlacedObject(occupiedObjectIndex);
            HidePreviewVisualOnly();
            return;
        }

        ClearSelectedPlacedObject();
        RefreshPlacementState();
        UpdatePreview();
    }

    private void RefreshPlacementState()
    {
        if (gridManager == null ||
            currentAnchorX < 0 ||
            currentAnchorY < 0 ||
            (!isPlacementPreviewActive && !isRelocatingSelectedObject) ||
            (HasSelectedPlacedObject && !isRelocatingSelectedObject))
        {
            isAreaAvailableCurrent = false;
            canAffordCurrent = false;
            canPlaceCurrent = false;
            return;
        }

        isAreaAvailableCurrent = gridManager.IsAreaAvailable(currentAnchorX, currentAnchorY, buildWidth, buildHeight);
        canAffordCurrent = isRelocatingSelectedObject || walletManager == null || walletManager.CanSpend(buildCost);
        canPlaceCurrent = isAreaAvailableCurrent && canAffordCurrent;
    }

    private void UpdatePreview()
    {
        if (previewObject == null) CreatePreviewObject();
        if (!isRelocatingSelectedObject && currentDefinition == null && EquipmentSelectionState.CurrentDefinition != null)
        {
            ApplyBuildDefinition(EquipmentSelectionState.CurrentDefinition);
        }

        if (!IsPlacementVisualModeActive() ||
            currentAnchorX < 0 ||
            currentAnchorY < 0 ||
            (!isPlacementPreviewActive && !isRelocatingSelectedObject) ||
            (HasSelectedPlacedObject && !isRelocatingSelectedObject))
        {
            HidePreviewCompletely();
            return;
        }

        previewObject.SetActive(true);
        Vector3 targetPos = gridManager.GetAreaCenterWorldPosition(currentAnchorX, currentAnchorY, buildWidth, buildHeight);
        targetPos.z = targetPos.y * 0.001f;
        previewObject.transform.position = targetPos;
        Vector2 footprintSize = GetFootprintSize(buildWidth, buildHeight);
        int previewDepthOffset = GymPlacedObjectVisual.GetSortingDepthOffsetForAnchorY(currentAnchorY);
        
        if (customPreviewRenderer == null || customPreviewOutlineRenderers == null || customPreviewOutlineRenderers.Length < 4)
        {
            customPreviewRenderer = EnsureCustomPreviewRenderer("CustomPreview", 8);
            customPreviewOutlineRenderers = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++) {
                customPreviewOutlineRenderers[i] = EnsureCustomPreviewRenderer("CustomOutline" + i, 7);
            }
        }

        bool hasCustomSprite = false;
        string previewEquipmentId = currentDefinition != null ? currentDefinition.EquipmentId : currentEquipmentId;
        if (!string.IsNullOrEmpty(previewEquipmentId))
        {
            string id = GetRuntimeObjectSpriteId(previewEquipmentId);
            Sprite loadedSprite = LoadPreviewSprite(id);
            if (loadedSprite != null)
            {
                float targetWidth = Mathf.Max(0.55f, footprintSize.x);
                float currentWidth = loadedSprite != null ? loadedSprite.bounds.size.x : 1f;
                float scale = targetWidth / currentWidth;
                scale = Mathf.Clamp(scale, 0.2f, 2.5f);

                customPreviewRenderer.sprite = loadedSprite;
                customPreviewRenderer.drawMode = SpriteDrawMode.Simple;
                customPreviewRenderer.sortingOrder = previewDepthOffset + 8;
                customPreviewRenderer.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                customPreviewRenderer.transform.localScale = new Vector3(scale, scale, 1f);
                customPreviewRenderer.color = canPlaceCurrent ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 0.4f, 0.4f, 0.6f);
                customPreviewRenderer.enabled = true;
                
                float offset = 0.035f;
                Vector3[] offsets = { new Vector3(offset, 0, 0), new Vector3(-offset, 0, 0), new Vector3(0, offset, 0), new Vector3(0, -offset, 0) };
                for (int i = 0; i < 4; i++) {
                    customPreviewOutlineRenderers[i].sprite = loadedSprite;
                    customPreviewOutlineRenderers[i].drawMode = SpriteDrawMode.Simple;
                    customPreviewOutlineRenderers[i].sortingOrder = previewDepthOffset + 7;
                    customPreviewOutlineRenderers[i].transform.localPosition = customPreviewRenderer.transform.localPosition + offsets[i];
                    customPreviewOutlineRenderers[i].transform.localScale = customPreviewRenderer.transform.localScale;
                    customPreviewOutlineRenderers[i].color = canPlaceCurrent ? previewValidBorderColor : previewInvalidBorderColor;
                    customPreviewOutlineRenderers[i].enabled = true;
                }
                
                hasCustomSprite = true;
            }
        }

        if (hasCustomSprite)
        {
            SetBuildHoverSuppressed(true);
            customPreviewRenderer.gameObject.SetActive(true);
            for (int i = 0; i < 4; i++) customPreviewOutlineRenderers[i].gameObject.SetActive(true);
            previewRenderer.enabled = false;
            previewGlowRenderer.gameObject.SetActive(false);
        }
        else
        {
            SetBuildHoverSuppressed(false);
            if (customPreviewRenderer != null) customPreviewRenderer.gameObject.SetActive(false);
            if (customPreviewOutlineRenderers != null) {
                for (int i = 0; i < 4; i++) if (customPreviewOutlineRenderers[i] != null) customPreviewOutlineRenderers[i].gameObject.SetActive(false);
            }
            previewRenderer.enabled = true;
            previewRenderer.size = footprintSize;
            previewRenderer.color = canPlaceCurrent ? previewValidColor : previewInvalidColor;
            
            previewGlowRenderer.gameObject.SetActive(true);
            previewGlowRenderer.sprite = RuntimeHighlightSpriteFactory.GetSoftRoundedOutlineSprite();
            previewGlowRenderer.drawMode = SpriteDrawMode.Sliced;
            UpdatePreviewGlow(footprintSize, canPlaceCurrent ? previewValidBorderColor : previewInvalidBorderColor);
        }
    }

    private bool IsPlacementVisualModeActive()
    {
        return BuildPlayModeManager.IsBuildMode &&
               (isPlacementPreviewActive || isRelocatingSelectedObject) &&
               (!HasSelectedPlacedObject || isRelocatingSelectedObject);
    }

    private static string GetRuntimeObjectSpriteId(string equipmentId)
    {
        return string.IsNullOrWhiteSpace(equipmentId)
            ? string.Empty
            : equipmentId.ToLowerInvariant().Trim();
    }

    private static string StripEquipmentGradeSuffix(string spriteId)
    {
        string normalized = string.IsNullOrWhiteSpace(spriteId)
            ? string.Empty
            : spriteId.ToLowerInvariant().Trim();

        if (normalized.EndsWith("_basic", System.StringComparison.Ordinal)) return normalized.Substring(0, normalized.Length - 6);
        if (normalized.EndsWith("_ss", System.StringComparison.Ordinal)) return normalized.Substring(0, normalized.Length - 3);
        if (normalized.EndsWith("_a", System.StringComparison.Ordinal) ||
            normalized.EndsWith("_b", System.StringComparison.Ordinal) ||
            normalized.EndsWith("_s", System.StringComparison.Ordinal))
        {
            return normalized.Substring(0, normalized.Length - 2);
        }

        return normalized;
    }

    private static Sprite LoadPreviewSprite(string spriteId)
    {
        Sprite sprite = LoadPreviewSpriteExact(spriteId);
        if (sprite != null)
        {
            return sprite;
        }

        string fallbackSpriteId = StripEquipmentGradeSuffix(spriteId);
        return string.Equals(fallbackSpriteId, spriteId, System.StringComparison.Ordinal)
            ? null
            : LoadPreviewSpriteExact(fallbackSpriteId);
    }

    private static Sprite LoadPreviewSpriteExact(string spriteId)
    {
        if (string.IsNullOrWhiteSpace(spriteId))
        {
            return null;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>($"GeneratedRuntimeUI/objects/{spriteId}");
        if (sprites != null && sprites.Length > 0)
        {
            System.Array.Sort(sprites, CompareSpritesByName);
            return sprites[0];
        }

        return Resources.Load<Sprite>($"GeneratedRuntimeUI/objects/{spriteId}");
    }

    private static int CompareSpritesByName(Sprite left, Sprite right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private void SetBuildHoverSuppressed(bool suppressed)
    {
        GridCell.SuppressBuildHoverVisual = suppressed;

        if (gridManager == null || currentAnchorX < 0 || currentAnchorY < 0)
        {
            return;
        }

        GridCell currentCell = gridManager.GetCell(currentAnchorX, currentAnchorY);
        if (currentCell != null)
        {
            currentCell.SetHovered(true);
        }
    }

    public bool ConfirmCurrentPlacement()
    {
        if (!BuildPlayModeManager.IsBuildMode)
        {
            return false;
        }

        RefreshPlacementState();
        UpdatePreview();

        if (!canPlaceCurrent)
        {
            return false;
        }

        if (isRelocatingSelectedObject)
        {
            CommitRelocationAtCurrentAnchor();
            return true;
        }

        if (!isPlacementPreviewActive || currentDefinition == null || HasSelectedPlacedObject)
        {
            return false;
        }

        if (walletManager != null && !walletManager.TrySpend(buildCost, GetPlacementSpendReason()))
        {
            RefreshPlacementState();
            UpdatePreview();
            return false;
        }

        EquipmentDefinition placedDefinition = currentDefinition;
        int placedIndex = PlaceObjectAt(currentAnchorX, currentAnchorY, buildWidth, buildHeight, placedDefinition, null, true);
        selectedPlacedObjectIndex = placedIndex;
        isPlacementPreviewActive = false;
        HidePreviewCompletely();
        RefreshAllPlacedVisualStates();
        ObjectPlaced?.Invoke(placedDefinition);
        PlayerPlacedObject?.Invoke();
        if (Application.isPlaying)
        {
            BuildPlayModeManager.EnterPlayMode();
        }
        return true;
    }

    public void CancelCurrentPlacement()
    {
        if (isRelocatingSelectedObject)
        {
            CancelRelocation();
            return;
        }

        isPlacementPreviewActive = false;
        HidePreviewCompletely();
        RefreshAllPlacedVisualStates();

        if (Application.isPlaying)
        {
            BuildPlayModeManager.EnterPlayMode();
        }
    }

    private void BeginRelocation()
    {
        if (!HasSelectedPlacedObject || isRelocatingSelectedObject) return;
        PlacedObjectSaveData selectedData = GetSelectedPlacedObjectData();
        if (selectedData == null) return;

        relocatingSnapshot = PlacedObjectSaveData.Clone(selectedData);
        relocatingDefinition = ResolveDefinitionFromSaveData(selectedData);
        isRelocatingSelectedObject = true;
        isPlacementPreviewActive = false;
        currentAnchorX = selectedData.anchorX;
        currentAnchorY = selectedData.anchorY;

        if (Application.isPlaying)
        {
            BuildPlayModeManager.EnterBuildMode();
        }

        MarkOccupiedForData(selectedData, false);
        ApplyBuildDefinition(relocatingDefinition, selectedData.width, selectedData.height, selectedData.installCost, selectedData.equipmentId);

        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
        MonthlySettlementManager.RequestOpenEquipmentTab();
    }

    private void CommitRelocationAtCurrentAnchor()
    {
        if (!HasSelectedPlacedObject || !isRelocatingSelectedObject) return;

        int index = selectedPlacedObjectIndex;
        if (!IsValidPlacedObjectIndex(index))
        {
            CancelRelocation();
            return;
        }

        PlacedObjectSaveData selectedData = placedObjectDataList[index];
        selectedData.anchorX = currentAnchorX;
        selectedData.anchorY = currentAnchorY;
        selectedData.runtimeDefinition = relocatingDefinition != null ? relocatingDefinition : selectedData.runtimeDefinition;

        GameObject visual = placedObjectVisuals[index];
        if (visual != null)
        {
            visual.transform.position = gridManager.GetAreaCenterWorldPosition(currentAnchorX, currentAnchorY, selectedData.width, selectedData.height);
            visual.name = BuildPlacedObjectName(selectedData, currentAnchorX, currentAnchorY);
        }

        MarkOccupiedForData(selectedData, true);

        isRelocatingSelectedObject = false;
        isPlacementPreviewActive = false;
        relocatingSnapshot = null;
        relocatingDefinition = null;

        RestoreBuildDefinitionFromSelection();
        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
        PlayerPlacedObject?.Invoke();
        if (Application.isPlaying)
        {
            BuildPlayModeManager.EnterPlayMode();
        }
    }

    private void CancelRelocation()
    {
        if (!HasSelectedPlacedObject || !isRelocatingSelectedObject) return;

        isPlacementPreviewActive = false;
        int index = selectedPlacedObjectIndex;
        if (!IsValidPlacedObjectIndex(index) || relocatingSnapshot == null)
        {
            isRelocatingSelectedObject = false;
            relocatingSnapshot = null;
            relocatingDefinition = null;
            RestoreBuildDefinitionFromSelection();
            RefreshPlacementState();
            UpdatePreview();
            RefreshAllPlacedVisualStates();
            return;
        }

        PlacedObjectSaveData selectedData = placedObjectDataList[index];
        selectedData.anchorX = relocatingSnapshot.anchorX;
        selectedData.anchorY = relocatingSnapshot.anchorY;
        selectedData.width = relocatingSnapshot.width;
        selectedData.height = relocatingSnapshot.height;
        selectedData.equipmentId = relocatingSnapshot.equipmentId;
        selectedData.displayName = relocatingSnapshot.displayName;
        selectedData.installCost = relocatingSnapshot.installCost;
        selectedData.runtimeDefinition = relocatingDefinition != null ? relocatingDefinition : selectedData.runtimeDefinition;

        GameObject visual = placedObjectVisuals[index];
        if (visual != null)
        {
            visual.transform.position = gridManager.GetAreaCenterWorldPosition(selectedData.anchorX, selectedData.anchorY, selectedData.width, selectedData.height);
            visual.name = BuildPlacedObjectName(selectedData, selectedData.anchorX, selectedData.anchorY);
        }

        MarkOccupiedForData(selectedData, true);

        isRelocatingSelectedObject = false;
        relocatingSnapshot = null;
        relocatingDefinition = null;

        RestoreBuildDefinitionFromSelection();
        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
    }

    private void SellSelectedPlacedObject()
    {
        if (!HasSelectedPlacedObject) return;

        int index = selectedPlacedObjectIndex;
        if (!IsValidPlacedObjectIndex(index))
        {
            ClearSelectedPlacedObject();
            return;
        }

        PlacedObjectSaveData selectedData = placedObjectDataList[index];
        string displayName = GetPlacedObjectDisplayName(selectedData);
        int refundAmount = GetSellRefundAmount(selectedData);

        RemovePlacedObjectAt(index);

        if (walletManager != null && refundAmount > 0)
        {
            walletManager.AddCash(refundAmount, $"{displayName} 철거 환불");
        }
        PlayerPlacedObject?.Invoke();
    }

    private void RemovePlacedObjectAt(int index)
    {
        if (!IsValidPlacedObjectIndex(index)) return;

        PlacedObjectSaveData data = placedObjectDataList[index];
        GameObject visual = placedObjectVisuals[index];

        if (data != null) MarkOccupiedForData(data, false);
        if (visual != null) Destroy(visual);

        placedObjectDataList.RemoveAt(index);
        placedObjectVisuals.RemoveAt(index);

        selectedPlacedObjectIndex = -1;
        isPlacementPreviewActive = false;
        isRelocatingSelectedObject = false;
        relocatingSnapshot = null;
        relocatingDefinition = null;

        RestoreBuildDefinitionFromSelection();
        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
    }

    private void SelectPlacedObject(int index)
    {
        if (!IsValidPlacedObjectIndex(index)) return;

        selectedPlacedObjectIndex = index;
        isPlacementPreviewActive = false;
        isRelocatingSelectedObject = false;
        relocatingSnapshot = null;
        relocatingDefinition = null;

        RefreshAllPlacedVisualStates();
        HidePreviewVisualOnly();
        MonthlySettlementManager.RequestOpenEquipmentTab();
    }

    private void ClearSelectedPlacedObject()
    {
        selectedPlacedObjectIndex = -1;
        isPlacementPreviewActive = false;
        isRelocatingSelectedObject = false;
        relocatingSnapshot = null;
        relocatingDefinition = null;

        RestoreBuildDefinitionFromSelection();
        RefreshPlacementState();
        UpdatePreview();
        RefreshAllPlacedVisualStates();
    }

    private bool TryGetPlacedObjectIndexAtCell(int x, int y, out int foundIndex)
    {
        for (int i = 0; i < placedObjectDataList.Count; i++)
        {
            PlacedObjectSaveData data = placedObjectDataList[i];
            if (data == null) continue;
            if (x >= data.anchorX && x < data.anchorX + data.width && y >= data.anchorY && y < data.anchorY + data.height)
            {
                foundIndex = i;
                return true;
            }
        }
        foundIndex = -1;
        return false;
    }

    private bool IsValidPlacedObjectIndex(int index)
    {
        return index >= 0 && index < placedObjectDataList.Count && index < placedObjectVisuals.Count;
    }

    private PlacedObjectSaveData GetSelectedPlacedObjectData()
    {
        return IsValidPlacedObjectIndex(selectedPlacedObjectIndex) ? placedObjectDataList[selectedPlacedObjectIndex] : null;
    }

    private int GetSellRefundAmount(PlacedObjectSaveData data)
    {
        if (data == null) return 0;
        return Mathf.Max(0, Mathf.RoundToInt(data.installCost * Mathf.Clamp01(sellRefundRate)));
    }

    private static int GetDurabilityPercent(PlacedObjectSaveData data)
    {
        if (data == null)
        {
            return 0;
        }

        if (data.isBroken)
        {
            return 0;
        }

        return Mathf.Clamp(100 - (data.usageCount * 4), 1, 100);
    }

    private static int GetRepairCost(PlacedObjectSaveData data)
    {
        int missingDurability = 100 - GetDurabilityPercent(data);
        return Mathf.Max(0, Mathf.CeilToInt(missingDurability / 25f) * 500);
    }

    private static HudActionDescriptor CreateHudAction(HudActionId actionId, string label, bool isEnabled)
    {
        return new HudActionDescriptor
        {
            actionId = actionId,
            label = label,
            isEnabled = isEnabled
        };
    }

    private static TimeSpan GetConstructionRemaining(PlacedObjectSaveData data)
    {
        if (data == null || !data.isUnderConstruction)
        {
            return TimeSpan.Zero;
        }

        TimeSpan remaining = new DateTime(data.constructionEndTimeTicks, DateTimeKind.Utc) - DateTime.UtcNow;
        return remaining.TotalSeconds <= 0 ? TimeSpan.Zero : remaining;
    }

    private static int GetConstructionSkipCost(PlacedObjectSaveData data)
    {
        TimeSpan remaining = GetConstructionRemaining(data);
        int remainingMinutes = Mathf.Max(1, Mathf.CeilToInt((float)remaining.TotalMinutes));
        return Mathf.Max(1, Mathf.CeilToInt(remainingMinutes / 5.0f));
    }

    private bool TrySkipSelectedConstructionWithStarCoin()
    {
        if (!HasSelectedPlacedObject)
        {
            return false;
        }

        PlacedObjectSaveData selectedData = GetSelectedPlacedObjectData();
        if (selectedData == null || !selectedData.isUnderConstruction)
        {
            return false;
        }

        int skipCost = GetConstructionSkipCost(selectedData);
        if (walletManager != null && !walletManager.TrySpendStarCoin(skipCost, "타이머 스킵"))
        {
            return false;
        }

        selectedData.isUnderConstruction = false;
        selectedData.constructionEndTimeTicks = 0;
        RefreshAllPlacedVisualStates();
        PlayerPlacedObject?.Invoke();
        return true;
    }

    private bool CancelSelectedConstruction()
    {
        if (!HasSelectedPlacedObject)
        {
            return false;
        }

        int index = selectedPlacedObjectIndex;
        if (!IsValidPlacedObjectIndex(index))
        {
            ClearSelectedPlacedObject();
            return false;
        }

        PlacedObjectSaveData selectedData = placedObjectDataList[index];
        if (selectedData == null || !selectedData.isUnderConstruction)
        {
            return false;
        }

        if (walletManager != null && selectedData.installCost > 0)
        {
            walletManager.AddCash(selectedData.installCost, $"{GetPlacedObjectDisplayName(selectedData)} 설치 취소 환불");
        }

        RemovePlacedObjectAt(index);
        PlayerPlacedObject?.Invoke();
        return true;
    }

    private bool TryRepairSelectedPlacedObject()
    {
        if (!HasSelectedPlacedObject)
        {
            return false;
        }

        PlacedObjectSaveData selectedData = GetSelectedPlacedObjectData();
        if (selectedData == null || selectedData.isUnderConstruction || GetDurabilityPercent(selectedData) >= 100)
        {
            return false;
        }

        int repairCost = GetRepairCost(selectedData);
        if (walletManager != null && repairCost > 0 && !walletManager.TrySpend(repairCost, "기구 수리 서비스"))
        {
            return false;
        }

        selectedData.isBroken = false;
        selectedData.usageCount = 0;
        RefreshAllPlacedVisualStates();
        PlayerPlacedObject?.Invoke();
        return true;
    }

    public void RefreshAllPlacedVisualStates()
    {
        for (int i = 0; i < placedObjectVisuals.Count && i < placedObjectDataList.Count; i++)
        {
            GameObject visual = placedObjectVisuals[i];
            PlacedObjectSaveData data = placedObjectDataList[i];

            if (visual == null || data == null) continue;

            GymPlacedObjectVisual layeredVisual = visual.GetComponent<GymPlacedObjectVisual>();
            if (layeredVisual != null)
            {
                bool isGhost = isRelocatingSelectedObject && i == selectedPlacedObjectIndex;
                bool isSelected = i == selectedPlacedObjectIndex && !isGhost;
                layeredVisual.ApplyState(data, isSelected, isGhost);
                continue;
            }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null) continue;

            Color baseColor = GetPlacedObjectBaseColor(data);

            if (isRelocatingSelectedObject && i == selectedPlacedObjectIndex)
            {
                renderer.color = movingGhostColor;
                continue;
            }

            if (i == selectedPlacedObjectIndex)
            {
                renderer.color = Color.Lerp(baseColor, selectedPlacedColorTint, 0.55f);
                continue;
            }

            if (data.isUnderConstruction)
            {
                renderer.color = Color.gray;
                continue;
            }

            renderer.color = baseColor;
        }
    }

    private Color GetPlacedObjectBaseColor(PlacedObjectSaveData data)
    {
        if (data == null) return placedColor;
        EquipmentDefinition definition = ResolveDefinitionFromSaveData(data);
        if (definition != null) return definition.DebugColor;
        return placedColor;
    }

    private void MarkOccupiedForData(PlacedObjectSaveData data, bool occupied)
    {
        if (gridManager == null || data == null) return;
        for (int y = data.anchorY; y < data.anchorY + data.height; y++)
        {
            for (int x = data.anchorX; x < data.anchorX + data.width; x++)
            {
                GridCell targetCell = gridManager.GetCell(x, y);
                if (targetCell != null) targetCell.SetOccupied(occupied);
            }
        }
    }

    private void ApplyBuildDefinition(EquipmentDefinition definition)
    {
        if (definition == null) return;
        ApplyBuildDefinition(definition, definition.Width, definition.Height, definition.InstallCost, definition.EquipmentId);
    }

    private void ApplyBuildDefinition(EquipmentDefinition definition, int width, int height, int cost, string equipmentId)
    {
        currentDefinition = definition;
        currentEquipmentId = definition != null ? definition.EquipmentId : equipmentId;
        buildWidth = Mathf.Max(1, width);
        buildHeight = Mathf.Max(1, height);
        buildCost = Mathf.Max(0, cost);
    }

    private void RestoreBuildDefinitionFromSelection()
    {
        EquipmentDefinition selectedDefinition = EquipmentSelectionState.CurrentDefinition;
        if (selectedDefinition != null) ApplyBuildDefinition(selectedDefinition);
    }

    private int PlaceObjectAt(int anchorX, int anchorY, int width, int height, EquipmentDefinition definition, PlacedObjectSaveData sourceData, bool logPlacement)
    {
        PlacedObjectSaveData placedData = sourceData != null ? PlacedObjectSaveData.Clone(sourceData) : new PlacedObjectSaveData();

        placedData.anchorX = anchorX;
        placedData.anchorY = anchorY;
        placedData.width = Mathf.Max(1, width);
        placedData.height = Mathf.Max(1, height);

        if (definition != null)
        {
            placedData.ApplyDefinitionSnapshot(definition);
            if (sourceData == null)
            {
                int minutes = definition.BaseInstallationMinutes;
                if (minutes > 0)
                {
                    placedData.isUnderConstruction = true;
                    placedData.constructionEndTimeTicks = System.DateTime.UtcNow.AddMinutes(minutes).Ticks;
                }
            }
        }
        else
        {
            placedData.runtimeDefinition = null;
            placedData.installCost = sourceData != null ? sourceData.installCost : buildCost;
            if (string.IsNullOrWhiteSpace(placedData.displayName)) placedData.displayName = $"기구 {placedData.width}x{placedData.height}";
        }

        GameObject placedObject = new GameObject(BuildPlacedObjectName(placedData, anchorX, anchorY));
        placedObject.transform.SetParent(EnsurePlacedObjectsRoot(), false);
        placedObject.transform.position = gridManager.GetAreaCenterWorldPosition(anchorX, anchorY, placedData.width, placedData.height);

        GymPlacedObjectVisual layeredVisual = placedObject.AddComponent<GymPlacedObjectVisual>();
        layeredVisual.Initialize(placedData, definition, GetFootprintSize(placedData.width, placedData.height));

        MarkOccupiedForData(placedData, true);

        placedObjectVisuals.Add(placedObject);
        placedObjectDataList.Add(placedData);
        return placedObjectDataList.Count - 1;
    }

    private Vector2 GetFootprintSize(int width, int height)
    {
        return new Vector2(width * gridManager.CellSize * 0.95f, height * gridManager.CellSize * 0.95f);
    }

    private void HidePreviewCompletely()
    {
        currentAnchorX = -1;
        currentAnchorY = -1;
        isAreaAvailableCurrent = false;
        canAffordCurrent = false;
        canPlaceCurrent = false;
        HidePreviewVisualOnly();
    }

    private void HidePreviewVisualOnly()
    {
        SetBuildHoverSuppressed(false);
        if (previewObject != null) previewObject.SetActive(false);
    }

    private Transform EnsurePlacedObjectsRoot()
    {
        if (placedObjectsRoot != null) return placedObjectsRoot;

        GameObject rootObject = new GameObject("PlacedObjectsRoot");
        Transform parent = null;

        if (gridManager != null && gridManager.transform.parent != null) parent = gridManager.transform.parent;
        else if (transform.parent != null) parent = transform.parent;
        else parent = transform;

        rootObject.transform.SetParent(parent, false);
        placedObjectsRoot = rootObject.transform;
        return placedObjectsRoot;
    }

    private void CreatePreviewObject()
    {
        if (previewObject == null)
        {
            previewObject = new GameObject("PlacementPreview");
            previewObject.transform.SetParent(EnsurePlacedObjectsRoot(), false);
        }

        previewRenderer ??= previewObject.GetComponent<SpriteRenderer>();
        if (previewRenderer == null)
        {
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
        }

        previewRenderer.sprite = GetWhiteSprite();
        previewRenderer.drawMode = SpriteDrawMode.Sliced;
        previewRenderer.sortingOrder = 6;

        previewGlowRenderer = EnsurePreviewGlowRenderer("PreviewGlow");
        customPreviewRenderer = EnsureCustomPreviewRenderer("CustomPreview", 8);
        customPreviewOutlineRenderers = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++) {
            customPreviewOutlineRenderers[i] = EnsureCustomPreviewRenderer("CustomOutline" + i, 7);
        }

        previewObject.SetActive(false);
    }

    private SpriteRenderer EnsurePreviewGlowRenderer(string childName)
    {
        Transform child = previewObject.transform.Find(childName);
        GameObject node = child != null ? child.gameObject : new GameObject(childName);
        node.transform.SetParent(previewObject.transform, false);

        SpriteRenderer renderer = node.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = node.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = RuntimeHighlightSpriteFactory.GetSoftRoundedOutlineSprite();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.sortingOrder = 16;
        return renderer;
    }

    private SpriteRenderer EnsureCustomPreviewRenderer(string childName, int sortingOrder)
    {
        Transform child = previewObject.transform.Find(childName);
        GameObject node = child != null ? child.gameObject : new GameObject(childName);
        node.transform.SetParent(previewObject.transform, false);

        SpriteRenderer renderer = node.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = node.AddComponent<SpriteRenderer>();
        }

        if (outlineMaterial == null)
        {
            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null) outlineMaterial = new Material(shader);
        }

        if (childName.StartsWith("CustomOutline") && outlineMaterial != null)
        {
            renderer.material = outlineMaterial;
        }

        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void UpdatePreviewGlow(Vector2 footprintSize, Color borderColor)
    {
        if (previewGlowRenderer == null)
        {
            CreatePreviewObject();
        }

        float cellSize = gridManager != null ? gridManager.CellSize : 1f;
        Vector2 glowSize = new Vector2(
            footprintSize.x + cellSize * 0.68f,
            footprintSize.y + cellSize * 0.68f);
        ConfigurePreviewGlow(previewGlowRenderer, glowSize, borderColor);
    }

    private static void ConfigurePreviewGlow(SpriteRenderer renderer, Vector2 size, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.transform.localPosition = Vector3.zero;
        renderer.size = size;
        renderer.color = color;
    }

    private void EnsureEquipmentCatalog()
    {
        if (equipmentCatalog == null) equipmentCatalog = FindFirstObjectByType<EquipmentCatalog>();
    }

    private EquipmentDefinition ResolveDefinitionFromSaveData(PlacedObjectSaveData data)
    {
        if (data == null) return null;
        if (data.runtimeDefinition != null) return data.runtimeDefinition;

        EnsureEquipmentCatalog();
        EquipmentDefinition definition = equipmentCatalog != null ? equipmentCatalog.GetDefinitionById(data.equipmentId) : null;
        data.runtimeDefinition = definition;
        return definition;
    }

    private int ResolveWidth(PlacedObjectSaveData data, EquipmentDefinition definition)
    {
        if (definition != null) return definition.Width;
        return Mathf.Max(1, data != null ? data.width : 1);
    }

    public void UpdateMachineInUseVisuals(HashSet<string> inUseKeys)
    {
        for (int i = 0; i < placedObjectVisuals.Count && i < placedObjectDataList.Count; i++)
        {
            if (placedObjectVisuals[i] == null || placedObjectDataList[i] == null) continue;
            var layeredVisual = placedObjectVisuals[i].GetComponent<GymPlacedObjectVisual>();
            if (layeredVisual != null)
            {
                string key = CustomerFlowManager.BuildMachineKey(placedObjectDataList[i]);
                layeredVisual.SetForegroundActive(inUseKeys != null && inUseKeys.Contains(key));
            }
        }
    }

    private int ResolveHeight(PlacedObjectSaveData data, EquipmentDefinition definition)
    {
        if (definition != null) return definition.Height;
        return Mathf.Max(1, data != null ? data.height : 1);
    }

    private string GetPlacementSpendReason()
    {
        if (currentDefinition != null) return $"{currentDefinition.DisplayName} 설치";
        return $"{buildWidth}x{buildHeight} 기구 설치";
    }

    private string GetCurrentBuildLabel()
    {
        if (currentDefinition != null) return currentDefinition.DisplayName;
        return $"{buildWidth}x{buildHeight} 기구";
    }

    private static string GetDefinitionDisplayName(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return "기구";
        }

        return !string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.DisplayName : definition.EquipmentId;
    }

    private static string GetPlacedObjectDisplayName(PlacedObjectSaveData data)
    {
        if (data == null) return "기구";
        if (!string.IsNullOrWhiteSpace(data.displayName)) return data.displayName;
        if (!string.IsNullOrWhiteSpace(data.equipmentId)) return data.equipmentId;
        return $"기구 {data.width}x{data.height}";
    }

    private static string BuildPlacedObjectName(PlacedObjectSaveData data, int anchorX, int anchorY)
    {
        string label = !string.IsNullOrWhiteSpace(data.equipmentId) ? data.equipmentId : $"{data.width}x{data.height}";
        return $"Placed_{label}_{anchorX}_{anchorY}";
    }

    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;
        Texture2D texture = Texture2D.whiteTexture;
        cachedWhiteSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        return cachedWhiteSprite;
    }

    private void EnsureActionPanelStyles()
    {
        if (actionPanelBoxStyle == null)
        {
            actionPanelBoxStyle = new GUIStyle(GUI.skin.box);
            actionPanelBoxStyle.padding = new RectOffset(10, 10, 10, 10);
        }
        if (actionTitleStyle == null)
        {
            actionTitleStyle = new GUIStyle(GUI.skin.label);
            actionTitleStyle.fontStyle = FontStyle.Bold;
            actionTitleStyle.normal.textColor = Color.white;
        }
        if (actionInfoStyle == null)
        {
            actionInfoStyle = new GUIStyle(GUI.skin.label);
            actionInfoStyle.wordWrap = true;
            actionInfoStyle.normal.textColor = Color.white;
        }
        if (actionButtonStyle == null)
        {
            actionButtonStyle = new GUIStyle(GUI.skin.button);
            actionButtonStyle.alignment = TextAnchor.MiddleCenter;
            actionButtonStyle.wordWrap = false;
        }

        bool isPortrait = Screen.height > Screen.width;
        actionTitleStyle.fontSize = isPortrait ? 13 : 14;
        actionInfoStyle.fontSize = isPortrait ? 12 : 13;
        actionButtonStyle.fontSize = isPortrait ? 12 : 13;
    }
}
