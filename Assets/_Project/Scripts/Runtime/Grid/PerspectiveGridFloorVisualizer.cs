using UnityEngine;

/// <summary>
/// [DEPRECATED - 2026-04]
/// 
/// 이 컴포넌트는 정면형 쿼터뷰(Perspective Quarter-View) 전환 실험을 위해 만들어졌다.
/// 
/// 사용자 결정: quarter-view 포기 → 순수 탑뷰(90도 직하 Top-Down) 공식 복귀.
/// 
/// 모든 mesh/quad/perspective floor 로직, UV jitter, row tint, 
/// gym_floor_tileset_quarterview_v1.png 관련 코드가 완전히 제거되었다.
/// 
/// 현재 공식 바닥 렌더링:
/// - GridCell 프리팹의 SpriteRenderer
/// - Warm Theme 타일셋 (gym_floor_tile_base_warm_final + border)
/// - BuildWarmFloorBorderTiles + Entrance/Reception (안정화 영역, 절대 건드리지 않음)
/// 
/// 이 파일은 하위 호환성을 위해 최소 stub으로 남겨두었으며,
/// GridManager 등에서 GetComponent / Configure 호출이 있어도 안전하게 no-op 처리된다.
/// 
/// 향후 삭제 예정.
/// </summary>
[DisallowMultipleComponent]
public class PerspectiveGridFloorVisualizer : MonoBehaviour
{
    public const string VisualRootName = "PerspectiveQuarterViewFloor_VisualOnly_DEPRECATED";

    private bool hasConfiguration = false;

    // --- 하위 호환 프로퍼티 (호출 측이 깨지지 않도록 최소 유지) ---
    public string CurrentFloorSpriteName => "DEPRECATED (pure top-down returned)";
    public string CurrentFloorTextureName => string.Empty;
    public float CurrentSpriteUvInsetPixels => 0f;
    public string CurrentSpriteUvLog => "Quarter-view abandoned. Pure top-down (GridCell SpriteRenderer + Warm Floor) is official.";
    public string CurrentTextureSettingsLog => "N/A";
    public float CurrentNearSideInset => 0f;
    public float CurrentFarSideInset => 0f;
    public bool RandomizeTileUvOffset => false;
    public float TileUvJitterPixels => 0f;

    /// <summary>
    /// [DEPRECATED] quarter-view Configure.
    /// 호출되어도 무시된다. 순수 탑뷰 복귀 완료.
    /// </summary>
    public void Configure(
        Transform sourceGridRoot,
        GridCell[,] sourceGridCells,
        int sourceColumns,
        int sourceRows,
        float sourceCellSize,
        Sprite sourceFloorSprite,
        float sourcePerspectiveStrength,
        float sourceFarWidthRatio,
        float sourceFarHeightRatio,
        float sourceOverlapPadding,
        float sourceSpriteUvInsetPixels,
        float sourceNearSideInset,
        float sourceFarSideInset,
        bool sourceRandomizeTileUvOffset,
        float sourceTileUvJitterPixels,
        float sourceRowTintStrength,
        Color sourceGlobalFloorTint,
        bool sourceUseMeshPerspectiveFloor,
        bool sourceDebugTintPerspectiveFloor)
    {
        hasConfiguration = true;
        Debug.Log("[PerspectiveGridFloorVisualizer] DEPRECATED: Quarter-view / perspective mesh floor completely removed (2026-04). Pure top-down + GridCell SpriteRenderer + Warm Floor is now the only official system. This Configure call is ignored.");
    }

    public void RefreshVisualFloor(bool enableVisualFloor)
    {
        if (enableVisualFloor)
        {
            Debug.LogWarning("[PerspectiveGridFloorVisualizer] RefreshVisualFloor called on deprecated component. Quarter-view support has been fully removed. No visual action performed. Use GridCell SpriteRenderer + Warm Floor (GymFloorTileResources).");
        }
    }

    public void ClearVisualsAndRestore()
    {
        Debug.Log("[PerspectiveGridFloorVisualizer] ClearVisualsAndRestore (deprecated) - no action taken. The active floor system is GridCell + Warm Floor Border + Entrance.");
    }

    /// <summary>
    /// [DEPRECATED] Old seam-fix test helper. Does nothing now.
    /// </summary>
    [ContextMenu("Rebuild Floor With Current Inspector Values (DEPRECATED - TopDown Returned)")]
    public void RebuildWithCurrentValuesForTest()
    {
        Debug.LogWarning("[PerspectiveGridFloorVisualizer] RebuildWithCurrentValuesForTest is deprecated and non-functional. Quarter-view experiment has been abandoned in favor of pure top-down.");
    }

    private void OnDisable()
    {
        // Deprecated component - nothing to clean up.
    }

    // ============================================================
    // [2026-04 완전 제거 목록]
    // - 모든 Mesh/Quad 생성 (BuildMeshFloorTiles, BuildSpriteFloorTiles)
    // - Perspective 레이아웃 계산 전체 (RecalculateLayoutIfNeeded, GetVisualCellQuadVertices 등)
    // - UV inset / jitter / row tint / noise 관련 모든 코드
    // - SpriteUvRect struct 및 관련 헬퍼
    // - Warm floor fallback 판별 로직 (책임은 GymFloorTileResources로 이관)
    // 
    // 결과: 더 이상 quarter-view 관련 코드가 이 파일에 남아있지 않다.
    // ============================================================
}
