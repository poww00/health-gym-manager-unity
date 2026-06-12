using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    // [REMOVED 2026-04] Quarter-view / perspective floor prototype constants
    // (gym_floor_tileset_quarterview_v1, floor_wood_plank_a 등 관련 모든 실험 코드 폐기)
    // 현재 공식: Warm Floor + GridCell SpriteRenderer (순수 탑뷰)

    private const string WarmFloorBorderRootName = "WarmFloorBorder";
    private const int WarmFloorBorderSortingOrder = -5;
    private const string DefaultEntranceReceptionRootName = "DefaultEntranceReception";
    private const string EntranceBackResourcePath = "GeneratedRuntimeUI/ui_v2/props/entrance/gym_entrance_topdown_2x1";
    private const string EntranceFrontResourcePath = "GeneratedRuntimeUI/ui_v2/props/entrance/gym_entrance_front_occluder_2x1";
    private const string ReceptionDeskResourcePath = "GeneratedRuntimeUI/ui_v2/props/reception/gym_reception_desk_2x1";
    private const int EntranceBackSortingOrder = -2;
    private const int EntranceGapFillerSortingOrder = EntranceBackSortingOrder - 1;
    private const int EntranceFrontSortingOrder = 40;
    private const int ReceptionDeskSortingOrder = 14;
    private const float EntranceLowerGapFillerWidthRatio = 0.52f;
    private const float EntranceLowerGapFillerHeightRatio = 0.86f;
    private const float EntranceFloorSourceCropWidthRatio = 0.50f;
    private const float EntranceFloorSourceCropHeightRatio = 0.75f;
    private const float EntranceWalkLaneMinOffsetCells = -0.80f;
    private const float EntranceWalkLaneMaxOffsetCells = 1.20f;
    private const float EntranceWalkLaneHalfHeightCells = 0.42f;
    private const float EntranceOutsideWaypointOffsetCells = -0.55f;
    private const float EntranceWalkWaypointOffsetCells = 0.38f;
    private const float EntranceInsideSafeWaypointOffsetCells = 0.92f;
    private const int ReceptionDeskFixedWidth = 2;
    private const int ReceptionDeskFixedHeight = 1;
    private const float ReceptionDeskWorldOffsetYCells = 0.50f;
    [Header("Grid Settings")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float cellSize = 1f;

    [Header("References")]
    [SerializeField] private Transform gridRoot;
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private Camera targetCamera;

    [Header("Camera Fit (Prototype)")]
    [SerializeField] private bool autoFitCameraToGrid = true;
    [SerializeField] private float cameraFitPadding = 1.5f;
    [SerializeField] private float minimumOrthographicSize = 5f;

    [Header("Touch Tap (Prototype)")]
    [SerializeField] private float touchTapMaxDuration = 0.25f;
    [SerializeField] private float touchTapMaxMovementPixels = 24f;

    [Header("Camera Interaction Input Block (Prototype)")]
    [SerializeField] private float blockGridInputAfterCameraInteractionSeconds = 0.08f;

    // [REMOVED 2026-04] All Quarter View / Perspective Floor prototype serialized fields removed.
    // Pure top-down (GridCell SpriteRenderer + Warm Floor) is the official and only supported mode.
    // Stabilized Warm Floor Border + Entrance/Reception visuals remain untouched.

    [Header("Top-View Floor Tiles (New Tileset)")]
    [SerializeField] private string defaultFloorTilesetPath = "GeneratedRuntimeUI/building/floor/gym_floor_tileset_quarterview_v1";
    [Tooltip("herringbone wood pattern (floor_wood_herringbone_a). 이름으로 검색하여 선택됨.")]
    [SerializeField] private string defaultFloorTileName = "floor_wood_herringbone_a";

    private GridCell[,] gridCells;
    private bool isGenerated = false;
    private GridCell currentHoveredCell;
    private GridCameraController gridCameraController;
    // [REMOVED] lastAppliedPerspective* fields removed with the prototype.

    private bool isTouchTapCandidate = false;
    private Vector2 touchTapStartScreenPosition;
    private float touchTapStartTime = 0f;

    public event Action<GridCell> HoveredCellChanged;
    public event Action<GridCell> CellClicked;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    // [REMOVED] Quarter-view / Perspective properties removed (top-down return).
    // These properties no longer exist. Code that previously referenced them has been cleaned.

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!isGenerated)
        {
            return;
        }

        ResolveReferences();

        // [REMOVED 2026-04] All perspective/quarter-view update checks removed.
        // Pure top-down GridCell floor is always active.

        if (InGameMenuManager.IsMenuOpen)
        {
            CancelTouchTapCandidate();
            ClearHoverIfNeeded();
            return;
        }

        if (IsActivePointerOverBlockedUi())
        {
            CancelTouchTapCandidate();
            ClearHoverIfNeeded();
            return;
        }

        UpdateTouchTapCandidateState();

        if (ShouldTemporarilyBlockGridInput())
        {
            CancelTouchTapCandidate();
            ClearHoverIfNeeded();
            return;
        }

        UpdateHover();
        HandlePointerPress();
    }

    public void SetGridSize(int newWidth, int newHeight, string reason = "")
    {
        int safeWidth = Mathf.Max(1, newWidth);
        int safeHeight = Mathf.Max(1, newHeight);

        bool changed = width != safeWidth || height != safeHeight;

        width = safeWidth;
        height = safeHeight;

        if (changed)
        {
            string reasonSuffix = string.IsNullOrWhiteSpace(reason)
                ? string.Empty
                : $" / 사유: {reason}";

            Debug.Log($"[GridManager] 그리드 크기 설정: {width}x{height}{reasonSuffix}");
        }
    }

    public void GenerateGrid()
    {
        if (isGenerated)
        {
            Debug.LogWarning("[GridManager] 이미 그리드가 생성되어 있어.");
            return;
        }

        if (gridRoot == null)
        {
            Debug.LogError("[GridManager] Grid Root가 연결되지 않았어.");
            return;
        }

        if (gridCellPrefab == null)
        {
            Debug.LogError("[GridManager] Grid Cell Prefab이 연결되지 않았어.");
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("[GridManager] Target Camera를 찾지 못했어.");
            return;
        }

        gridCells = new GridCell[width, height];

        Vector2 originOffset = new Vector2(
            -(width * cellSize) / 2f + cellSize / 2f,
            -(height * cellSize) / 2f + cellSize / 2f
        );

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 worldPosition = new Vector3(
                    originOffset.x + (x * cellSize),
                    originOffset.y + (y * cellSize),
                    0f
                );

                GameObject cellObject = Instantiate(gridCellPrefab, worldPosition, Quaternion.identity, gridRoot);
                cellObject.name = $"Cell_{x}_{y}";

                GridCell gridCell = cellObject.GetComponent<GridCell>();
                if (gridCell == null)
                {
                    Debug.LogError("[GridManager] GridCell 컴포넌트가 프리팹에 없어.");
                    return;
                }

                gridCell.Initialize(x, y, cellSize);
                gridCells[x, y] = gridCell;
            }
        }

        // === Apply new top-view floor tileset (gym_floor_tileset_quarterview_v1.png) ===
        // ApplyNewTopViewFloorTileset();

        BuildWarmFloorBorderTiles();
        // [REMOVED 2026-04] ApplyQuarterView... and ApplyPerspective... calls removed.
        // Warm Floor Border (stabilized) + Entrance/Reception remain the only visual additions.

        isGenerated = true;
        FitCameraToCurrentGrid();

        Debug.Log($"[GridManager] {width}x{height} 그리드 생성 완료");
    }

    // [REMOVED 2026-04 - Pure Top-Down Return]
    // ApplyQuarterViewFloorReplacementIfEnabled, ApplyPerspectiveFloorVisualizerIfEnabled,
    // GetPerspectiveFloorSettingsHash, RemoveExistingQuarterViewFloorReplacement,
    // SetGeneratedCellFloorRenderersVisible (prototype version) 등
    // 모든 quarter-view prototype 메서드가 완전히 제거되었다.
    //
    // WarmFloorBorder + Entrance/Reception 관련 메서드 (BuildWarmFloorBorderTiles 등)는
    // 안정화 영역이므로 절대 건드리지 않고 그대로 유지된다.

    // [REMOVED 2026-04] SetGeneratedCellFloorRenderersVisible (prototype version) removed.
    // The stabilized warm floor border + GridCell floor renderers are controlled directly.

    private void ApplyNewTopViewFloorTileset()
    {
        // === 필수 실행 단계 1: Resources.LoadAll<Sprite>로 atlas 전체 로드 ===
        Sprite[] allFloorSprites = Resources.LoadAll<Sprite>(defaultFloorTilesetPath);

        // === 필수 실행 단계 2: 모든 sprite의 index, name, rect.size를 먼저 출력 (디버그 필수) ===
        Debug.Log($"[FloorTileset Debug] Loaded {allFloorSprites.Length} sprites from {defaultFloorTilesetPath}");
        for (int i = 0; i < allFloorSprites.Length; i++)
        {
            Sprite s = allFloorSprites[i];
            if (s != null)
            {
                Debug.Log($"[FloorTileset Debug] [{i}] name='{s.name}' | rect={s.rect.width}x{s.rect.height} | textureRect={s.textureRect.width}x{s.textureRect.height}");
            }
            else
            {
                Debug.Log($"[FloorTileset Debug] [{i}] <null>");
            }
        }

        // === 필수 실행 단계 3: 이름으로 herringbone sprite 명시적 검색 (index 3 절대 사용 금지) ===
        Sprite newFloorSprite = null;

        // 1순위: 정확한 이름 "floor_wood_herringbone_a" 검색
        for (int i = 0; i < allFloorSprites.Length; i++)
        {
            Sprite s = allFloorSprites[i];
            if (s != null && s.name == "floor_wood_herringbone_a")
            {
                newFloorSprite = s;
                Debug.Log($"[FloorTileset Debug] Selected exact name match at index {i}: {s.name}");
                break;
            }
        }

        // 2순위: 이름에 "herringbone"이 포함된 sprite 검색
        if (newFloorSprite == null)
        {
            for (int i = 0; i < allFloorSprites.Length; i++)
            {
                Sprite s = allFloorSprites[i];
                if (s != null && s.name.ToLower().Contains("herringbone"))
                {
                    newFloorSprite = s;
                    Debug.Log($"[FloorTileset Debug] Selected herringbone match at index {i}: {s.name}");
                    break;
                }
            }
        }

        if (newFloorSprite == null)
        {
            newFloorSprite = GymFloorTileResources.LoadBaseWarmSprite();
            Debug.LogWarning($"[GridManager] floor_wood_herringbone_a (herringbone)를 찾을 수 없습니다. Warm floor로 fallback.");
        }

        if (newFloorSprite != null)
        {
            // === 필수 실행 단계 4 & 5: GridCell에 Simple + 정확한 1:1 scale 적용 ===
            GridCell.SetDefaultFloorSprite(newFloorSprite);
            GridCell.UsingNewTopViewFloorTiles = true;

            // 사용자가 지시한 정확한 공식 + scale=0 방지
            float w = newFloorSprite.rect.width;
            float h = newFloorSprite.rect.height;

            if (w < 0.1f) w = cellSize;
            if (h < 0.1f) h = cellSize;

            float scaleX = cellSize / w;
            float scaleY = cellSize / h;

            // 기존 생성된 모든 셀에 즉시 적용
            if (gridCells != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        GridCell cell = gridCells[x, y];
                        if (cell != null)
                        {
                            var sr = cell.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.sprite = newFloorSprite;
                                sr.drawMode = SpriteDrawMode.Simple;
                            }

                            cell.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                            var col = cell.GetComponent<BoxCollider2D>();
                            if (col != null)
                            {
                                col.size = new Vector2(cellSize * 0.92f, cellSize * 0.92f);
                            }
                        }
                    }
                }
            }

            Debug.Log($"Top-view floor tileset updated - sprite: {newFloorSprite.name} | rect: {w}x{h} | scale: [{scaleX:0.0000},{scaleY:0.0000}] | gap 제거 완료");
        }
    }

    private void BuildWarmFloorBorderTiles()
    {
        Sprite baseSprite = GymFloorTileResources.LoadBaseWarmSprite();
        Sprite sideSprite = GymFloorTileResources.LoadBorderSideWarmSprite();
        Sprite cornerSprite = GymFloorTileResources.LoadBorderCornerWarmSprite();
        if (baseSprite == null || sideSprite == null || cornerSprite == null || gridRoot == null)
        {
            return;
        }

        GameObject borderRootObject = new GameObject(WarmFloorBorderRootName);
        borderRootObject.transform.SetParent(gridRoot, false);
        borderRootObject.transform.localPosition = Vector3.zero;

        Transform borderRoot = borderRootObject.transform;

        float baseScale = CalculateWarmFloorTileScale(baseSprite, cellSize);
        float sideScale = CalculateWarmFloorTileScale(sideSprite, cellSize);
        float cornerScale = CalculateWarmFloorTileScale(cornerSprite, cellSize);

        WarmFloorVisibleBounds baseLocal = GetSpriteVisibleLocalBounds(baseSprite);
        WarmFloorVisibleBounds sideLocal = GetSpriteVisibleLocalBounds(sideSprite);
        WarmFloorVisibleBounds cornerLocal = GetSpriteVisibleLocalBounds(cornerSprite);

        WarmFloorTransformedBounds baseBounds = TransformVisibleBounds(baseLocal, 0f, baseScale);
        WarmFloorTransformedBounds topSideBounds = TransformVisibleBounds(sideLocal, 0f, sideScale);
        WarmFloorTransformedBounds bottomSideBounds = TransformVisibleBounds(sideLocal, 180f, sideScale);
        WarmFloorTransformedBounds leftSideBounds = TransformVisibleBounds(sideLocal, 90f, sideScale);
        WarmFloorTransformedBounds rightSideBounds = TransformVisibleBounds(sideLocal, -90f, sideScale);
        WarmFloorTransformedBounds cornerBounds = TransformVisibleBounds(cornerLocal, 0f, cornerScale);
        WarmFloorSideClamp sideClamp = CalculateWarmFloorSideClamp(
            topSideBounds,
            bottomSideBounds,
            leftSideBounds,
            rightSideBounds,
            cornerBounds);

        int defaultEntranceStartY = GetDefaultEntranceStartY();

        float baseLeft = GetGeneratedCellLocalPosition(0, 0).x + baseBounds.MinX;
        float baseRight = GetGeneratedCellLocalPosition(width - 1, 0).x + baseBounds.MaxX;
        float baseBottom = GetGeneratedCellLocalPosition(0, 0).y + baseBounds.MinY;
        float baseTop = GetGeneratedCellLocalPosition(0, height - 1).y + baseBounds.MaxY;

        for (int x = 0; x < width; x++)
        {
            WarmFloorTransformedBounds baseCellBounds = OffsetBounds(baseBounds, GetGeneratedCellLocalPosition(x, 0));

            Vector3 topPosition = new Vector3(
                baseCellBounds.MinX - topSideBounds.MinX,
                baseTop - topSideBounds.MinY - sideClamp.TopInset,
                0f);
            Vector3 bottomPosition = new Vector3(
                baseCellBounds.MinX - bottomSideBounds.MinX,
                baseBottom - bottomSideBounds.MaxY + sideClamp.BottomInset,
                0f);

            CreateWarmFloorTile(borderRoot, $"WarmFloorBorderTop_{x}", sideSprite, topPosition, 0f, sideScale);
            CreateWarmFloorTile(borderRoot, $"WarmFloorBorderBottom_{x}", sideSprite, bottomPosition, 180f, sideScale);
        }

        for (int y = 0; y < height; y++)
        {
            WarmFloorTransformedBounds baseCellBounds = OffsetBounds(baseBounds, GetGeneratedCellLocalPosition(0, y));

            Vector3 leftPosition = new Vector3(
                baseLeft - leftSideBounds.MaxX + sideClamp.LeftInset,
                baseCellBounds.MinY - leftSideBounds.MinY,
                0f);
            Vector3 rightPosition = new Vector3(
                baseRight - rightSideBounds.MinX - sideClamp.RightInset,
                baseCellBounds.MinY - rightSideBounds.MinY,
                0f);

            if (!IsDefaultEntranceWallRow(y, defaultEntranceStartY))
            {
                CreateWarmFloorTile(borderRoot, $"WarmFloorBorderLeft_{y}", sideSprite, leftPosition, 90f, sideScale);
            }

            CreateWarmFloorTile(borderRoot, $"WarmFloorBorderRight_{y}", sideSprite, rightPosition, -90f, sideScale);
        }

        CreateWarmFloorTile(borderRoot, "WarmFloorBorderTopLeft", cornerSprite, new Vector3(baseLeft - cornerBounds.MaxX, baseTop - cornerBounds.MinY, 0f), 0f, cornerScale);
        CreateWarmFloorTile(borderRoot, "WarmFloorBorderTopRight", cornerSprite, new Vector3(baseRight - cornerBounds.MinX, baseTop - cornerBounds.MinY, 0f), 0f, cornerScale);
        CreateWarmFloorTile(borderRoot, "WarmFloorBorderBottomRight", cornerSprite, new Vector3(baseRight - cornerBounds.MinX, baseBottom - cornerBounds.MaxY, 0f), 0f, cornerScale);
        CreateWarmFloorTile(borderRoot, "WarmFloorBorderBottomLeft", cornerSprite, new Vector3(baseLeft - cornerBounds.MaxX, baseBottom - cornerBounds.MaxY, 0f), 0f, cornerScale);

        BuildDefaultEntranceAndReception(
            borderRoot,
            defaultEntranceStartY,
            sideScale,
            baseSprite,
            sideLocal,
            leftSideBounds,
            sideClamp,
            baseBounds,
            baseLeft);

        Debug.Log(
            "[GridManager] Warm floor alpha layout " +
            $"base={baseLocal.PixelBounds}, side={sideLocal.PixelBounds}, corner={cornerLocal.PixelBounds}, " +
            $"baseEdges L/R/B/T={baseLeft:0.###}/{baseRight:0.###}/{baseBottom:0.###}/{baseTop:0.###}, " +
            $"sideClamp L/R/B/T={sideClamp.LeftInset:0.####}/{sideClamp.RightInset:0.####}/{sideClamp.BottomInset:0.####}/{sideClamp.TopInset:0.####}");
    }

    private void BuildDefaultEntranceAndReception(
        Transform borderRoot,
        int entranceStartY,
        float sideScale,
        Sprite floorBaseSprite,
        WarmFloorVisibleBounds sideLocal,
        WarmFloorTransformedBounds leftSideBounds,
        WarmFloorSideClamp sideClamp,
        WarmFloorTransformedBounds baseBounds,
        float baseLeft)
    {
        if (borderRoot == null || height < 2)
        {
            return;
        }

        Sprite entranceBackSprite = LoadRuntimeSprite(EntranceBackResourcePath);
        Sprite entranceFrontSprite = null; // LoadRuntimeSprite(EntranceFrontResourcePath);
        Sprite receptionDeskSprite = LoadRuntimeSprite(ReceptionDeskResourcePath);

        Transform featureRoot = CreateChildRoot(borderRoot, DefaultEntranceReceptionRootName);

        // v9에서 형태가 맞았던 back/front 조합을 그대로 유지한 뒤, 조합 전체만 좌측 벽 방향으로 회전한다.
        // 개별 sprite를 반대로 회전시키거나 뒤집지 않는다. 비균일 스케일도 사용하지 않는다.
        float entranceRotationZ = 90f;
        float entranceScale = CalculateModuleScaleByRotatedHeight(entranceBackSprite, entranceRotationZ, cellSize * 2f);
        if (entranceScale <= 0f)
        {
            entranceScale = sideScale;
        }

        WarmFloorTransformedBounds rotatedBackBounds = entranceBackSprite != null
            ? TransformFullSpriteBounds(entranceBackSprite, entranceRotationZ, entranceScale)
            : default;

        float entranceInnerEdgeX = baseLeft + sideClamp.LeftInset;
        float entranceX = entranceBackSprite != null
            ? entranceInnerEdgeX - rotatedBackBounds.MaxX
            : GetLeftBorderLocalPosition(entranceStartY, baseBounds, leftSideBounds, sideClamp, baseLeft).x;

        float entranceTargetCenterY = (GetGeneratedCellLocalPosition(0, entranceStartY).y +
            GetGeneratedCellLocalPosition(0, Mathf.Min(height - 1, entranceStartY + 1)).y) * 0.5f;
        float rotatedBackCenterY = entranceBackSprite != null
            ? (rotatedBackBounds.MinY + rotatedBackBounds.MaxY) * 0.5f
            : 0f;

        // v14에서 거의 맞았던 형태/회전은 유지한다.
        // v17의 자동 bounds 정렬은 폐기하고, 인게임 월드 X축 기준으로만 살짝 더 왼쪽으로 이동한다.
        float entranceWorldOffsetX = -1.04f * cellSize;
        float entranceWorldOffsetY = 0f * cellSize;
        Vector3 entrancePosition = new Vector3(
            entranceX + entranceWorldOffsetX,
            entranceTargetCenterY - rotatedBackCenterY + entranceWorldOffsetY,
            0f);

        // v15처럼 과하게 키우지 않고, 벽 두께에 맞추기 위한 최소 월드 X축 보정만 적용한다.
        // 회전된 이미지의 로컬 축이 아니라 인게임 기준 X축 스케일이다.
        float entranceWorldScaleX = 1.24f;
        float entranceWorldScaleY = 1.03f;
        Transform entranceWorldRoot = CreateChildRoot(featureRoot, "GymEntrance_2x1_LeftWall_WorldXScaledRoot");
        entranceWorldRoot.localPosition = entrancePosition;
        entranceWorldRoot.localScale = new Vector3(entranceWorldScaleX, entranceWorldScaleY, 1f);

        Transform entranceRoot = CreateChildRoot(entranceWorldRoot, "GymEntrance_2x1_LeftWall_RotatedComposite");
        entranceRoot.localPosition = Vector3.zero;
        // v11에서 형태 자체는 맞았으므로, 개별 back/front는 건드리지 않고 묶음 전체만 180도 추가 회전한다.
        entranceRoot.localRotation = Quaternion.Euler(0f, 0f, entranceRotationZ + 180f);

        WarmFloorTransformedBounds backBounds = entranceBackSprite != null
            ? TransformFullSpriteBounds(entranceBackSprite, 0f, entranceScale)
            : default;
        WarmFloorTransformedBounds frontBounds = entranceFrontSprite != null
            ? TransformFullSpriteBounds(entranceFrontSprite, 0f, entranceScale)
            : default;

        // back은 v9에서 보이던 원본 형태 그대로 둔다. 회전은 부모 루트에만 적용된다.
        CreateEntranceLowerGapFiller(
            entranceRoot,
            floorBaseSprite,
            backBounds);

        CreateRuntimeSprite(
            entranceRoot,
            "GymEntranceBack_2x1_LeftWall",
            entranceBackSprite,
            Vector3.zero,
            0f,
            entranceScale,
            EntranceBackSortingOrder);

        // front_occluder도 v9처럼 back의 상단 보 쪽에 붙인 뒤, 부모 루트 회전만 따라가게 한다.
        Vector3 frontLocalPosition = Vector3.zero;
        if (entranceFrontSprite != null && entranceBackSprite != null)
        {
            frontLocalPosition.y += backBounds.MaxY - frontBounds.MaxY;
        }

        CreateRuntimeSprite(
            entranceRoot,
            "GymEntranceFrontOccluder_2x1_LeftWall",
            entranceFrontSprite,
            frontLocalPosition,
            0f,
            entranceScale,
            EntranceFrontSortingOrder);

        if (receptionDeskSprite == null || width < 3)
        {
            return;
        }

        // 데스크는 입구 바로 안쪽 벽에 붙여 보이도록 좌측 첫 2칸 기준으로 배치한다.
        // y는 입구 2칸의 중앙에 맞춰서 시각적으로 입구와 붙어 보이게 한다.
        Vector3 leftInnerCellPosition = GetGeneratedCellLocalPosition(0, Mathf.Clamp(entranceStartY, 0, height - 1));
        float deskWorldOffsetY = ReceptionDeskWorldOffsetYCells * cellSize;
        Vector3 deskLocalPosition = new Vector3(
            leftInnerCellPosition.x + (cellSize * 0.5f),
            entranceTargetCenterY + deskWorldOffsetY,
            0f);

        float deskScale = CalculateModuleFitScale(receptionDeskSprite, 0f, cellSize * 2f, cellSize);

        CreateRuntimeSprite(
            featureRoot,
            "GymReceptionDesk_2x1_Default",
            receptionDeskSprite,
            deskLocalPosition,
            0f,
            deskScale,
            ReceptionDeskSortingOrder);

        RegisterDefaultReceptionDeskOccupancy(entranceStartY);
    }

    private static Transform CreateChildRoot(Transform parent, string name)
    {
        GameObject rootObject = new GameObject(name);
        rootObject.transform.SetParent(parent, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private Vector3 GetLeftBorderLocalPosition(
        int y,
        WarmFloorTransformedBounds baseBounds,
        WarmFloorTransformedBounds leftSideBounds,
        WarmFloorSideClamp sideClamp,
        float baseLeft)
    {
        WarmFloorTransformedBounds baseCellBounds = OffsetBounds(baseBounds, GetGeneratedCellLocalPosition(0, y));
        return new Vector3(
            baseLeft - leftSideBounds.MaxX + sideClamp.LeftInset,
            baseCellBounds.MinY - leftSideBounds.MinY,
            0f);
    }

    private int GetDefaultEntranceStartY()
    {
        if (height <= 2)
        {
            return 0;
        }

        return Mathf.Clamp((height / 2) - 1, 1, height - 2);
    }

    private static bool IsDefaultEntranceWallRow(int y, int entranceStartY)
    {
        return y == entranceStartY || y == entranceStartY + 1;
    }

    public bool TryGetDefaultEntrancePassCell(out int x, out int y)
    {
        x = 0;
        y = Mathf.Clamp(GetDefaultEntranceStartY(), 0, Mathf.Max(0, height - 1));
        return width > 0 && height > 0 && GetCell(x, y) != null;
    }

    public bool TryGetDefaultEntrancePassWorldPosition(out Vector3 position)
    {
        if (TryGetDefaultEntrancePassCell(out int x, out int y))
        {
            position = GetAreaCenterWorldPosition(x, y, 1, 1);
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    public bool TryGetEntranceOutsideWorldPosition(out Vector3 position)
    {
        return TryGetEntranceOutsideWorldPosition(0f, out position);
    }

    public bool TryGetEntranceOutsideWorldPosition(float customerRadius, out Vector3 position)
    {
        return TryGetEntranceWalkLaneWorldPosition(EntranceOutsideWaypointOffsetCells, customerRadius, out position);
    }

    public bool TryGetEntranceWalkWorldPosition(out Vector3 position)
    {
        return TryGetEntranceWalkWorldPosition(0f, out position);
    }

    public bool TryGetEntranceWalkWorldPosition(float customerRadius, out Vector3 position)
    {
        return TryGetEntranceWalkLaneWorldPosition(EntranceWalkWaypointOffsetCells, customerRadius, out position);
    }

    public bool TryGetEntranceInsideSafeWorldPosition(out Vector3 position)
    {
        return TryGetEntranceInsideSafeWorldPosition(0f, out position);
    }

    public bool TryGetEntranceInsideSafeWorldPosition(float customerRadius, out Vector3 position)
    {
        return TryGetEntranceWalkLaneWorldPosition(EntranceInsideSafeWaypointOffsetCells, customerRadius, out position);
    }

    public bool TryGetEntranceWalkLaneCenterWorldPosition(float customerRadius, out Vector3 position)
    {
        return TryGetEntranceWalkWorldPosition(customerRadius, out position);
    }

    public bool TryGetEntranceWalkLaneBounds(out Rect bounds)
    {
        return TryGetEntranceWalkLaneBounds(0f, out bounds);
    }

    public bool TryGetEntranceWalkLaneBounds(float marginWorld, out Rect bounds)
    {
        if (!TryGetEntranceWalkLaneReference(out Vector3 passCellCenter, out float laneCenterY))
        {
            bounds = default;
            return false;
        }

        float minX = passCellCenter.x + (cellSize * EntranceWalkLaneMinOffsetCells);
        float maxX = passCellCenter.x + (cellSize * EntranceWalkLaneMaxOffsetCells);
        float minY = laneCenterY - (cellSize * EntranceWalkLaneHalfHeightCells);
        float maxY = laneCenterY + (cellSize * EntranceWalkLaneHalfHeightCells);

        float margin = Mathf.Max(0f, marginWorld);
        float marginX = Mathf.Min(margin, Mathf.Max(0f, ((maxX - minX) * 0.5f) - 0.001f));
        float marginY = Mathf.Min(margin, Mathf.Max(0f, ((maxY - minY) * 0.5f) - 0.001f));

        bounds = Rect.MinMaxRect(
            minX + marginX,
            minY + marginY,
            maxX - marginX,
            maxY - marginY);
        return true;
    }

    private bool TryGetEntranceWalkLaneWorldPosition(float offsetCellsFromPassCellCenter, float customerRadius, out Vector3 position)
    {
        if (TryGetEntranceWalkLaneReference(out Vector3 passCellCenter, out float laneCenterY) &&
            TryGetEntranceWalkLaneBounds(customerRadius, out Rect safeLaneBounds))
        {
            float desiredX = passCellCenter.x + (cellSize * offsetCellsFromPassCellCenter);
            position = new Vector3(
                Mathf.Clamp(desiredX, safeLaneBounds.xMin, safeLaneBounds.xMax),
                Mathf.Clamp(laneCenterY, safeLaneBounds.yMin, safeLaneBounds.yMax),
                0f);
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool TryGetEntranceWalkLaneReference(out Vector3 passCellCenter, out float laneCenterY)
    {
        passCellCenter = Vector3.zero;
        laneCenterY = 0f;

        if (!TryGetDefaultEntrancePassCell(out int x, out int y))
        {
            return false;
        }

        passCellCenter = GetAreaCenterWorldPosition(x, y, 1, 1);
        int upperY = Mathf.Clamp(y + 1, 0, Mathf.Max(0, height - 1));
        Vector3 upperCellCenter = GetAreaCenterWorldPosition(x, upperY, 1, 1);
        laneCenterY = upperY != y
            ? (passCellCenter.y + upperCellCenter.y) * 0.5f
            : passCellCenter.y;
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!TryGetEntranceWalkLaneBounds(0f, out Rect rawLaneBounds))
        {
            return;
        }

        Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.45f);
        Gizmos.DrawWireCube(
            rawLaneBounds.center,
            new Vector3(rawLaneBounds.width, rawLaneBounds.height, 0.02f));

        float previewRadius = Mathf.Max(0.01f, cellSize * 0.20f);
        if (TryGetEntranceWalkLaneBounds(previewRadius, out Rect safeLaneBounds))
        {
            Gizmos.color = new Color(0.1f, 1f, 0.45f, 0.65f);
            Gizmos.DrawWireCube(
                safeLaneBounds.center,
                new Vector3(safeLaneBounds.width, safeLaneBounds.height, 0.02f));
        }

        if (TryGetEntranceOutsideWorldPosition(previewRadius, out Vector3 outside))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(outside, cellSize * 0.06f);
        }

        if (TryGetEntranceWalkWorldPosition(previewRadius, out Vector3 walk))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(walk, cellSize * 0.06f);
        }

        if (TryGetEntranceInsideSafeWorldPosition(previewRadius, out Vector3 inside))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(inside, cellSize * 0.06f);
        }
    }
#endif

    public bool TryGetDefaultStaffSpawnWorldPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        int centerX = Mathf.Clamp(width / 2, 0, width - 1);
        int centerY = Mathf.Clamp(height / 2, 0, height - 1);
        if (!TryFindNearestAvailableCell(centerX, centerY, out int spawnX, out int spawnY))
        {
            return false;
        }

        position = GetAreaCenterWorldPosition(spawnX, spawnY, 1, 1);
        return true;
    }

    private bool TryFindNearestAvailableCell(int originX, int originY, out int resultX, out int resultY)
    {
        resultX = 0;
        resultY = 0;

        float bestDistance = float.PositiveInfinity;
        bool found = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!IsAreaAvailable(x, y, 1, 1))
                {
                    continue;
                }

                float dx = x - originX;
                float dy = y - originY;
                float distance = (dx * dx) + (dy * dy);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                resultX = x;
                resultY = y;
                found = true;
            }
        }

        return found;
    }

    private int GetDefaultReceptionDeskAnchorY(int entranceStartY)
    {
        return Mathf.Clamp(entranceStartY + 1, 0, Mathf.Max(0, height - ReceptionDeskFixedHeight));
    }

    private void RegisterDefaultReceptionDeskOccupancy(int entranceStartY)
    {
        int deskWidth = Mathf.Min(ReceptionDeskFixedWidth, width);
        if (deskWidth <= 0 || height <= 0)
        {
            return;
        }

        SetFixedAreaOccupied(0, GetDefaultReceptionDeskAnchorY(entranceStartY), deskWidth, ReceptionDeskFixedHeight, true);
    }

    private void SetFixedAreaOccupied(int anchorX, int anchorY, int areaWidth, int areaHeight, bool occupied)
    {
        for (int y = anchorY; y < anchorY + areaHeight; y++)
        {
            for (int x = anchorX; x < anchorX + areaWidth; x++)
            {
                GridCell cell = GetCell(x, y);
                if (cell != null)
                {
                    cell.SetFixedOccupied(occupied);
                }
            }
        }
    }

    private static Sprite LoadRuntimeSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0];
        }

        Debug.LogWarning($"[GridManager] Sprite resource not found: {resourcePath}");
        return null;
    }

    private static void CreateRuntimeSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPosition,
        float rotationZ,
        float uniformScale,
        int sortingOrder)
    {
        if (parent == null || sprite == null)
        {
            return;
        }

        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        node.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

        SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;
    }

    private static void CreateEntranceLowerGapFiller(
        Transform parent,
        Sprite floorBaseSprite,
        WarmFloorTransformedBounds entranceBackBounds)
    {
        if (parent == null ||
            floorBaseSprite == null ||
            entranceBackBounds.Width <= 0f ||
            entranceBackBounds.Height <= 0f)
        {
            return;
        }

        Sprite fillerSprite = CreateCenteredSpriteCrop(
            floorBaseSprite,
            "GymEntranceLowerGapFiller_FloorBaseCrop",
            EntranceFloorSourceCropWidthRatio,
            EntranceFloorSourceCropHeightRatio);
        if (fillerSprite == null)
        {
            return;
        }

        Vector2 fillerSize = new Vector2(
            entranceBackBounds.Width * EntranceLowerGapFillerWidthRatio,
            entranceBackBounds.Height * EntranceLowerGapFillerHeightRatio);
        Vector3 fillerPosition = new Vector3(
            (entranceBackBounds.MinX + entranceBackBounds.MaxX) * 0.5f,
            (entranceBackBounds.MinY + entranceBackBounds.MaxY) * 0.5f,
            0f);

        CreateRuntimeTiledSprite(
            parent,
            "GymEntranceLowerGapFiller_CroppedFloorBase",
            fillerSprite,
            fillerPosition,
            0f,
            fillerSize,
            EntranceGapFillerSortingOrder);
    }

    private static Sprite CreateCenteredSpriteCrop(
        Sprite sourceSprite,
        string name,
        float normalizedWidth,
        float normalizedHeight)
    {
        if (sourceSprite == null || sourceSprite.texture == null)
        {
            return null;
        }

        Rect sourceRect = sourceSprite.textureRect;
        float cropWidth = Mathf.Max(1f, Mathf.Round(sourceRect.width * Mathf.Clamp01(normalizedWidth)));
        float cropHeight = Mathf.Max(1f, Mathf.Round(sourceRect.height * Mathf.Clamp01(normalizedHeight)));
        float cropX = sourceRect.x + Mathf.Round((sourceRect.width - cropWidth) * 0.5f);
        float cropY = sourceRect.y + Mathf.Round((sourceRect.height - cropHeight) * 0.5f);

        Sprite crop = Sprite.Create(
            sourceSprite.texture,
            new Rect(cropX, cropY, cropWidth, cropHeight),
            new Vector2(0.5f, 0.5f),
            sourceSprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        crop.name = name;
        return crop;
    }

    private static void CreateRuntimeTiledSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPosition,
        float rotationZ,
        Vector2 size,
        int sortingOrder)
    {
        if (parent == null || sprite == null || size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        node.transform.localScale = Vector3.one;

        SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;
    }


    private static float CalculateModuleScaleByRotatedHeight(Sprite sprite, float rotationZ, float targetHeight)
    {
        if (sprite == null)
        {
            return 0f;
        }

        WarmFloorTransformedBounds rotatedBounds = TransformFullSpriteBounds(sprite, rotationZ, 1f);
        return rotatedBounds.Height > 0f ? targetHeight / rotatedBounds.Height : 0f;
    }

    private static float CalculateModuleFitScale(Sprite sprite, float rotationZ, float targetWidth, float targetHeight)
    {
        if (sprite == null)
        {
            return 1f;
        }

        WarmFloorTransformedBounds rotatedBounds = TransformFullSpriteBounds(sprite, rotationZ, 1f);
        float scaleX = rotatedBounds.Width > 0f ? targetWidth / rotatedBounds.Width : 1f;
        float scaleY = rotatedBounds.Height > 0f ? targetHeight / rotatedBounds.Height : 1f;
        return Mathf.Min(scaleX, scaleY);
    }

    private static WarmFloorTransformedBounds TransformFullSpriteBounds(Sprite sprite, float rotationZ, float scale)
    {
        Bounds bounds = sprite.bounds;
        WarmFloorVisibleBounds fullBounds = new WarmFloorVisibleBounds(
            bounds.min.x,
            bounds.max.x,
            bounds.min.y,
            bounds.max.y,
            default);

        return TransformVisibleBounds(fullBounds, rotationZ, scale);
    }

    private Vector3 GetGridLocalPosition(int tileX, int tileY)
    {
        Vector2 originOffset = new Vector2(
            -(width * cellSize) / 2f + cellSize / 2f,
            -(height * cellSize) / 2f + cellSize / 2f
        );

        return new Vector3(
            originOffset.x + (tileX * cellSize),
            originOffset.y + (tileY * cellSize),
            0f
        );
    }

    private Vector3 GetGeneratedCellLocalPosition(int tileX, int tileY)
    {
        if (gridCells != null &&
            tileX >= 0 &&
            tileX < width &&
            tileY >= 0 &&
            tileY < height &&
            gridCells[tileX, tileY] != null)
        {
            return gridCells[tileX, tileY].transform.localPosition;
        }

        return GetGridLocalPosition(tileX, tileY);
    }

    private static void CreateWarmFloorTile(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPosition,
        float rotationZ,
        float uniformScale,
        int sortingOrder = WarmFloorBorderSortingOrder)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;
        node.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;

        node.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    private static float CalculateWarmFloorTileScale(Sprite sprite, float cellSize)
    {
        if (sprite == null)
        {
            return 1f;
        }

        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        return spriteSize > 0f ? cellSize / spriteSize : 1f;
    }

    private static WarmFloorVisibleBounds GetSpriteVisibleLocalBounds(Sprite sprite)
    {
        WarmFloorAlphaPixelBounds alphaBounds = GetSpriteAlphaPixelBounds(sprite);
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;

        return new WarmFloorVisibleBounds(
            (alphaBounds.MinX - pivot.x) / pixelsPerUnit,
            ((alphaBounds.MaxX + 1f) - pivot.x) / pixelsPerUnit,
            (alphaBounds.MinY - pivot.y) / pixelsPerUnit,
            ((alphaBounds.MaxY + 1f) - pivot.y) / pixelsPerUnit,
            alphaBounds);
    }

    private static WarmFloorAlphaPixelBounds GetSpriteAlphaPixelBounds(Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        int rectX = Mathf.RoundToInt(textureRect.x);
        int rectY = Mathf.RoundToInt(textureRect.y);
        int rectWidth = Mathf.RoundToInt(textureRect.width);
        int rectHeight = Mathf.RoundToInt(textureRect.height);
        Color32[] pixels = ReadTexturePixels(sprite.texture);
        int textureWidth = sprite.texture.width;

        int minX = rectWidth;
        int minY = rectHeight;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < rectHeight; y++)
        {
            int textureY = rectY + y;
            for (int x = 0; x < rectWidth; x++)
            {
                int textureX = rectX + x;
                int index = textureY * textureWidth + textureX;
                if (index < 0 || index >= pixels.Length || pixels[index].a == 0)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new WarmFloorAlphaPixelBounds(0, rectWidth - 1, 0, rectHeight - 1);
        }

        return new WarmFloorAlphaPixelBounds(minX, maxX, minY, maxY);
    }

    private static Color32[] ReadTexturePixels(Texture2D texture)
    {
        try
        {
            return texture.GetPixels32();
        }
        catch (Exception)
        {
            return ReadTexturePixelsFromRenderTexture(texture);
        }
    }

    private static Color32[] ReadTexturePixelsFromRenderTexture(Texture2D texture)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

        try
        {
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readableTexture.Apply(false, false);
            return readableTexture.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);

            if (Application.isPlaying)
            {
                Destroy(readableTexture);
            }
            else
            {
                DestroyImmediate(readableTexture);
            }
        }
    }

    private static WarmFloorTransformedBounds TransformVisibleBounds(WarmFloorVisibleBounds bounds, float rotationZ, float scale)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, rotationZ);
        Vector3 minMin = rotation * new Vector3(bounds.MinX * scale, bounds.MinY * scale, 0f);
        Vector3 minMax = rotation * new Vector3(bounds.MinX * scale, bounds.MaxY * scale, 0f);
        Vector3 maxMin = rotation * new Vector3(bounds.MaxX * scale, bounds.MinY * scale, 0f);
        Vector3 maxMax = rotation * new Vector3(bounds.MaxX * scale, bounds.MaxY * scale, 0f);

        return new WarmFloorTransformedBounds(
            Mathf.Min(minMin.x, minMax.x, maxMin.x, maxMax.x),
            Mathf.Max(minMin.x, minMax.x, maxMin.x, maxMax.x),
            Mathf.Min(minMin.y, minMax.y, maxMin.y, maxMax.y),
            Mathf.Max(minMin.y, minMax.y, maxMin.y, maxMax.y));
    }

    private static WarmFloorTransformedBounds OffsetBounds(WarmFloorTransformedBounds bounds, Vector3 offset)
    {
        return new WarmFloorTransformedBounds(
            bounds.MinX + offset.x,
            bounds.MaxX + offset.x,
            bounds.MinY + offset.y,
            bounds.MaxY + offset.y);
    }

    private static WarmFloorSideClamp CalculateWarmFloorSideClamp(
        WarmFloorTransformedBounds topSide,
        WarmFloorTransformedBounds bottomSide,
        WarmFloorTransformedBounds leftSide,
        WarmFloorTransformedBounds rightSide,
        WarmFloorTransformedBounds corner)
    {
        float cornerWidth = corner.Width;
        float cornerHeight = corner.Height;

        return new WarmFloorSideClamp(
            Mathf.Max(0f, leftSide.Width - cornerWidth),
            Mathf.Max(0f, rightSide.Width - cornerWidth),
            Mathf.Max(0f, bottomSide.Height - cornerHeight),
            Mathf.Max(0f, topSide.Height - cornerHeight));
    }

    private readonly struct WarmFloorAlphaPixelBounds
    {
        public WarmFloorAlphaPixelBounds(int minX, int maxX, int minY, int maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public int MinX { get; }
        public int MaxX { get; }
        public int MinY { get; }
        public int MaxY { get; }

        public override string ToString()
        {
            return $"({MinX},{MinY})-({MaxX},{MaxY})";
        }
    }

    private readonly struct WarmFloorVisibleBounds
    {
        public WarmFloorVisibleBounds(float minX, float maxX, float minY, float maxY, WarmFloorAlphaPixelBounds pixelBounds)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            PixelBounds = pixelBounds;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public WarmFloorAlphaPixelBounds PixelBounds { get; }
    }

    private readonly struct WarmFloorTransformedBounds
    {
        public WarmFloorTransformedBounds(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }

        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
    }

    private readonly struct WarmFloorSideClamp
    {
        public WarmFloorSideClamp(float leftInset, float rightInset, float bottomInset, float topInset)
        {
            LeftInset = leftInset;
            RightInset = rightInset;
            BottomInset = bottomInset;
            TopInset = topInset;
        }

        public float LeftInset { get; }
        public float RightInset { get; }
        public float BottomInset { get; }
        public float TopInset { get; }
    }
    public bool RebuildGrid(string reason = "")
    {
        if (gridRoot == null || gridCellPrefab == null)
        {
            Debug.LogError("[GridManager] RebuildGrid 전에 필요한 참조가 비어 있어.");
            return false;
        }

        ClearHoverIfNeeded();
        CancelTouchTapCandidate();
        ClearGeneratedGrid();
        GenerateGrid();

        if (isGenerated)
        {
            string reasonSuffix = string.IsNullOrWhiteSpace(reason)
                ? string.Empty
                : $" / 사유: {reason}";

            Debug.Log($"[GridManager] 그리드 재생성 완료: {width}x{height}{reasonSuffix}");
        }

        return isGenerated;
    }

    public GridCell GetCell(int x, int y)
    {
        if (gridCells == null)
        {
            return null;
        }

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return null;
        }

        return gridCells[x, y];
    }

    public bool IsAreaAvailable(int anchorX, int anchorY, int areaWidth, int areaHeight)
    {
        for (int y = anchorY; y < anchorY + areaHeight; y++)
        {
            for (int x = anchorX; x < anchorX + areaWidth; x++)
            {
                GridCell cell = GetCell(x, y);
                if (cell == null || cell.IsOccupied)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public Vector3 GetAreaCenterWorldPosition(int anchorX, int anchorY, int areaWidth, int areaHeight)
    {
        GridCell anchorCell = GetCell(anchorX, anchorY);
        if (anchorCell == null)
        {
            return Vector3.zero;
        }

        Vector3 anchorPosition = anchorCell.transform.position;

        return new Vector3(
            anchorPosition.x + ((areaWidth - 1) * cellSize * 0.5f),
            anchorPosition.y + ((areaHeight - 1) * cellSize * 0.5f),
            0f
        );
    }

    public bool TryGetCellIndexFromWorldPosition(Vector3 worldPos, out int x, out int y)
    {
        Vector2 originOffset = new Vector2(
            -(width * cellSize) / 2f + cellSize / 2f,
            -(height * cellSize) / 2f + cellSize / 2f
        );

        x = Mathf.RoundToInt((worldPos.x - originOffset.x) / cellSize);
        y = Mathf.RoundToInt((worldPos.y - originOffset.y) / cellSize);

        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (gridCameraController == null && targetCamera != null)
        {
            gridCameraController = targetCamera.GetComponent<GridCameraController>();
        }
    }

    private void UpdateHover()
    {
        GridCell hitCell = GetCellUnderActivePointer();

        if (currentHoveredCell == hitCell)
        {
            return;
        }

        if (currentHoveredCell != null)
        {
            currentHoveredCell.SetHovered(false);
        }

        currentHoveredCell = hitCell;

        if (currentHoveredCell != null)
        {
            currentHoveredCell.SetHovered(true);
        }

        HoveredCellChanged?.Invoke(currentHoveredCell);
    }

    private void HandlePointerPress()
    {
        if (TryConsumeTouchTap(out GridCell tappedCell))
        {
            CellClicked?.Invoke(tappedCell);
            return;
        }

        if (!WasMousePointerPressedThisFrame())
        {
            return;
        }

        GridCell hitCell = GetCellUnderMousePointer();
        CellClicked?.Invoke(hitCell);
    }

    private GridCell GetCellUnderActivePointer()
    {
        if (!TryGetActivePointerScreenPosition(out Vector2 screenPosition))
        {
            return null;
        }

        return GetCellFromScreenPosition(screenPosition);
    }

    private GridCell GetCellUnderMousePointer()
    {
        if (Mouse.current == null)
        {
            return null;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        if (IsScreenPositionBlockedByUi(mouseScreenPosition))
        {
            return null;
        }

        return GetCellFromScreenPosition(mouseScreenPosition);
    }

    private GridCell GetCellFromScreenPosition(Vector2 screenPosition)
    {
        if (targetCamera == null)
        {
            return null;
        }

        if (!TryGetWorldPositionFromScreenPosition(screenPosition, out Vector2 worldPosition))
        {
            return null;
        }

        Collider2D hitCollider = Physics2D.OverlapPoint(worldPosition);
        if (hitCollider == null)
        {
            return null;
        }

        return hitCollider.GetComponent<GridCell>();
    }

    private bool TryGetActivePointerScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        if (Touchscreen.current != null &&
            GetActiveTouchCount() == 1 &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();

            if (IsScreenPositionBlockedByUi(screenPosition))
            {
                return false;
            }

            return true;
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();

            if (IsScreenPositionBlockedByUi(screenPosition))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private bool TryGetWorldPositionFromScreenPosition(Vector2 screenPosition, out Vector2 worldPosition)
    {
        worldPosition = Vector2.zero;

        if (targetCamera == null)
        {
            return false;
        }

        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 converted = targetCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera)
        );

        worldPosition = new Vector2(converted.x, converted.y);
        return true;
    }

    private bool WasMousePointerPressedThisFrame()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void UpdateTouchTapCandidateState()
    {
        if (Touchscreen.current == null)
        {
            CancelTouchTapCandidate();
            return;
        }

        int activeTouchCount = GetActiveTouchCount();
        var primaryTouch = Touchscreen.current.primaryTouch;

        if (activeTouchCount >= 2)
        {
            CancelTouchTapCandidate();
            return;
        }

        if (primaryTouch.press.wasPressedThisFrame && activeTouchCount == 1)
        {
            Vector2 startPosition = primaryTouch.position.ReadValue();

            if (IsScreenPositionBlockedByUi(startPosition))
            {
                CancelTouchTapCandidate();
                return;
            }

            isTouchTapCandidate = true;
            touchTapStartScreenPosition = startPosition;
            touchTapStartTime = Time.unscaledTime;
            return;
        }

        if (!isTouchTapCandidate)
        {
            return;
        }

        if (!primaryTouch.press.isPressed)
        {
            return;
        }

        float elapsed = Time.unscaledTime - touchTapStartTime;
        float movedSqr = (primaryTouch.position.ReadValue() - touchTapStartScreenPosition).sqrMagnitude;
        float maxMoveSqr = touchTapMaxMovementPixels * touchTapMaxMovementPixels;

        if (elapsed > touchTapMaxDuration || movedSqr > maxMoveSqr)
        {
            CancelTouchTapCandidate();
        }
    }

    private bool TryConsumeTouchTap(out GridCell tappedCell)
    {
        tappedCell = null;

        if (Touchscreen.current == null)
        {
            return false;
        }

        var primaryTouch = Touchscreen.current.primaryTouch;
        if (!primaryTouch.press.wasReleasedThisFrame)
        {
            return false;
        }

        bool wasTapCandidate = isTouchTapCandidate;
        Vector2 startScreenPosition = touchTapStartScreenPosition;
        float startedAt = touchTapStartTime;
        Vector2 releaseScreenPosition = primaryTouch.position.ReadValue();

        CancelTouchTapCandidate();

        if (!wasTapCandidate)
        {
            return false;
        }

        if (IsScreenPositionBlockedByUi(releaseScreenPosition))
        {
            return false;
        }

        float elapsed = Time.unscaledTime - startedAt;
        float movedSqr = (releaseScreenPosition - startScreenPosition).sqrMagnitude;
        float maxMoveSqr = touchTapMaxMovementPixels * touchTapMaxMovementPixels;

        if (elapsed > touchTapMaxDuration || movedSqr > maxMoveSqr)
        {
            return false;
        }

        tappedCell = GetCellFromScreenPosition(releaseScreenPosition);
        return true;
    }

    private bool ShouldTemporarilyBlockGridInput()
    {
        if (ShouldBlockGridInputForCameraGesture())
        {
            return true;
        }

        if (gridCameraController != null &&
            gridCameraController.HadRecentInteraction(blockGridInputAfterCameraInteractionSeconds))
        {
            return true;
        }

        return false;
    }

    private bool ShouldBlockGridInputForCameraGesture()
    {
        if (GetActiveTouchCount() >= 2)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            return true;
        }

        if (gridCameraController != null && gridCameraController.IsCameraGestureActive)
        {
            return true;
        }

        return false;
    }

    private bool IsActivePointerOverBlockedUi()
    {
        if (Touchscreen.current != null &&
            GetActiveTouchCount() == 1 &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            return IsScreenPositionBlockedByUi(Touchscreen.current.primaryTouch.position.ReadValue());
        }

        if (Mouse.current != null)
        {
            return IsScreenPositionBlockedByUi(Mouse.current.position.ReadValue());
        }

        return false;
    }

    private static int GetActiveTouchCount()
    {
        if (Touchscreen.current == null)
        {
            return 0;
        }

        int activeTouchCount = 0;

        for (int i = 0; i < Touchscreen.current.touches.Count; i++)
        {
            if (Touchscreen.current.touches[i].press.isPressed)
            {
                activeTouchCount++;
            }
        }

        return activeTouchCount;
    }

    private static bool IsScreenPositionBlockedByUi(Vector2 screenPosition)
    {
        return ScreenUiBlocker.IsScreenPositionBlocked(screenPosition);
    }

    private void CancelTouchTapCandidate()
    {
        isTouchTapCandidate = false;
        touchTapStartScreenPosition = Vector2.zero;
        touchTapStartTime = 0f;
    }

    private void ClearHoverIfNeeded()
    {
        if (currentHoveredCell != null)
        {
            currentHoveredCell.SetHovered(false);
            currentHoveredCell = null;
            HoveredCellChanged?.Invoke(null);
        }
    }

    private void ClearGeneratedGrid()
    {
        isGenerated = false;
        gridCells = null;

        if (gridRoot == null)
        {
            return;
        }

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gridRoot.GetChild(i);
            if (child == null)
            {
                continue;
            }

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void FitCameraToCurrentGrid()
    {
        if (!autoFitCameraToGrid)
        {
            return;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (!targetCamera.orthographic)
        {
            return;
        }

        float paddedHalfWidth = (width * cellSize * 0.5f) + Mathf.Max(0f, cameraFitPadding);
        float paddedHalfHeight = (height * cellSize * 0.5f) + Mathf.Max(0f, cameraFitPadding);

        float sizeFromWidth = paddedHalfWidth / Mathf.Max(0.01f, targetCamera.aspect);
        float sizeFromHeight = paddedHalfHeight;

        targetCamera.orthographicSize = Mathf.Max(minimumOrthographicSize, sizeFromWidth, sizeFromHeight);

        Vector3 cameraPosition = targetCamera.transform.position;
        cameraPosition.x = 0f;
        cameraPosition.y = 0f;
        targetCamera.transform.position = cameraPosition;
    }
}
