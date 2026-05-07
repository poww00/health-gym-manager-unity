using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
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

    private GridCell[,] gridCells;
    private bool isGenerated = false;
    private GridCell currentHoveredCell;
    private GridCameraController gridCameraController;

    private bool isTouchTapCandidate = false;
    private Vector2 touchTapStartScreenPosition;
    private float touchTapStartTime = 0f;

    public event Action<GridCell> HoveredCellChanged;
    public event Action<GridCell> CellClicked;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

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

        isGenerated = true;
        FitCameraToCurrentGrid();

        Debug.Log($"[GridManager] {width}x{height} 그리드 생성 완료");
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