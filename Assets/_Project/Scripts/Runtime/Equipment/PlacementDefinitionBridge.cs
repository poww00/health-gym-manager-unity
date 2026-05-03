using UnityEngine;

/// <summary>
/// [프로토타입 -> 1차 정식화]
/// 현재 선택된 EquipmentDefinition을 PlacementManager에 직접 전달하는 브리지.
///
/// 이전 reflection 기반 진단 버전보다 단순화했지만,
/// 아직 정식 완성본은 아니다.
/// - Canvas 상점 UI 없음
/// - 프리팹/아트 교체 없음
/// - 선택 상태 -> PlacementManager 동기화만 담당
/// </summary>
[DefaultExecutionOrder(1150)]
public sealed class PlacementDefinitionBridge : MonoBehaviour
{
    [Header("External References (비워두면 자동 탐색)")]
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private EquipmentCatalog equipmentCatalog;

    [Header("Debug")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool logApplySuccess = true;
    [SerializeField] private bool logApplyFailure = true;

    private EquipmentDefinition lastAppliedDefinition;

    private void OnEnable()
    {
        EquipmentSelectionState.OnDefinitionChanged += HandleDefinitionChanged;
    }

    private void OnDisable()
    {
        EquipmentSelectionState.OnDefinitionChanged -= HandleDefinitionChanged;
    }

    private void Awake()
    {
        AutoResolve();

        if (!applyOnAwake)
        {
            return;
        }

        EquipmentDefinition definition = EquipmentSelectionState.CurrentDefinition;

        if (definition == null && equipmentCatalog != null)
        {
            definition = equipmentCatalog.GetFirstValidDefinition();
            if (definition != null)
            {
                EquipmentSelectionState.Select(definition);
            }
        }

        ApplyDefinition(definition);
    }

    private void Update()
    {
        AutoResolve();

        EquipmentDefinition current = EquipmentSelectionState.CurrentDefinition;
        if (current == null || current == lastAppliedDefinition)
        {
            return;
        }

        ApplyDefinition(current);
    }

    private void HandleDefinitionChanged(EquipmentDefinition definition)
    {
        ApplyDefinition(definition);
    }

    private void AutoResolve()
    {
        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (equipmentCatalog == null)
        {
            equipmentCatalog = FindFirstObjectByType<EquipmentCatalog>();
        }
    }

    private void ApplyDefinition(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (placementManager == null)
        {
            if (logApplyFailure)
            {
                Debug.LogWarning("[PlacementDefinitionBridge] PlacementManager를 찾지 못해서 선택 기구를 반영하지 못했어.");
            }

            return;
        }

        placementManager.SetPlacementDefinition(definition);
        lastAppliedDefinition = definition;

        if (logApplySuccess)
        {
            Debug.Log(
                $"[PlacementDefinitionBridge] 적용 완료: {definition.DisplayName} / " +
                $"{definition.Width}x{definition.Height} / {definition.InstallCost:N0} / id={definition.EquipmentId}"
            );
        }
    }
}