using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// [프로토타입/MVP]
/// 큰 부지(16x16 / 32x32) 대응용 카메라 팬/줌 컨트롤러.
///
/// 데스크톱
/// - 우클릭 드래그: 팬
/// - 마우스 휠: 줌
///
/// 모바일
/// - 두 손가락 드래그: 팬
/// - 핀치: 줌
///
/// 아직 완성형 카메라 시스템은 아님.
/// - 관성 없음
/// - 미니맵 없음
/// - 카메라 위치/줌 저장 없음
/// </summary>
[DisallowMultipleComponent]
public sealed class GridCameraController : MonoBehaviour
{
    [Header("References (비워두면 자동 탐색)")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GridManager gridManager;

    [Header("Overview Fit (Prototype)")]
    [SerializeField] private bool resetViewWhenGridChanges = true;
    [SerializeField] private float fitPadding = 1.5f;
    [SerializeField] private float minimumOverviewOrthographicSize = 5f;

    [Tooltip("세로형 화면에서 하단 HUD에 덜 가리도록 초기 시야를 위로 조금 올리는 값(스크린 높이 비율)")]
    [SerializeField, Range(0f, 0.25f)] private float portraitOverviewVerticalBias = 0.08f;

    [Header("Pan Bounds (Prototype)")]
    [SerializeField] private float panBoundsPadding = 0.9f;

    [Tooltip("현재 화면이 그리드 전체보다 커서 원래는 상하/좌우 이동이 막히는 경우, 약간 움직일 수 있게 해주는 여유값")]
    [SerializeField] private float freePanSlackXWhenOverviewFits = 3.5f;
    [SerializeField] private float freePanSlackYWhenOverviewFits = 4.5f;

    [Header("Zoom (Prototype)")]
    [SerializeField, Range(0.1f, 1f)] private float zoomInFactor = 0.18f;
    [SerializeField] private float zoomOutFactor = 1.08f;
    [SerializeField] private float absoluteMinimumOrthographicSize = 2.5f;

    [Tooltip("마우스 휠 줌 감도")]
    [SerializeField] private float mouseWheelZoomSensitivity = 0.085f;

    [Header("Input (Prototype)")]
    [SerializeField] private bool enableMousePan = true;
    [SerializeField] private bool enableMouseWheelZoom = true;
    [SerializeField] private bool enableTouchPanAndPinch = true;
    [SerializeField] private bool logAutoReset = false;

    private Vector2 lastMouseScreenPosition;
    private bool isMousePanning = false;
    private bool isTouchGestureActive = false;
    private float lastInteractionUnscaledTime = -999f;

    private int lastKnownGridWidth = -1;
    private int lastKnownGridHeight = -1;
    private float lastKnownCellSize = -1f;
    private float lastKnownAspect = -1f;
    private float cachedFitSize = 5f;

    public bool IsCameraGestureActive => isMousePanning || isTouchGestureActive;
    public float LastInteractionUnscaledTime => lastInteractionUnscaledTime;

    private void Awake()
    {
        AutoResolve();
    }

    private void Start()
    {
        ForceResetViewToFit("Start");
    }

    private void LateUpdate()
    {
        AutoResolve();

        if (!CanOperate())
        {
            return;
        }

        RefreshConstraintsIfNeeded();

        if (InGameMenuManager.IsMenuOpen)
        {
            isMousePanning = false;
            isTouchGestureActive = false;
            return;
        }

        HandleMouseInput();
        HandleTouchInput();
        ClampCameraToGrid();
    }

    public bool HadRecentInteraction(float cooldownSeconds)
    {
        return (Time.unscaledTime - lastInteractionUnscaledTime) <= Mathf.Max(0f, cooldownSeconds);
    }

    public void ForceResetViewToFit(string reason = "")
    {
        AutoResolve();

        if (!CanOperate())
        {
            return;
        }

        UpdateConstraintCache();

        targetCamera.orthographicSize = cachedFitSize;

        Vector3 position = targetCamera.transform.position;
        position.x = 0f;
        position.y = GetDefaultCenterYForCurrentView();
        targetCamera.transform.position = position;

        ClampCameraToGrid();

        if (logAutoReset)
        {
            string reasonSuffix = string.IsNullOrWhiteSpace(reason)
                ? string.Empty
                : $" / 사유: {reason}";

            Debug.Log($"[GridCameraController] 시야를 현재 그리드 기준으로 재설정했어.{reasonSuffix}");
        }
    }

    private void AutoResolve()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }

    private bool CanOperate()
    {
        return targetCamera != null &&
               gridManager != null &&
               targetCamera.orthographic;
    }

    private void RefreshConstraintsIfNeeded()
    {
        int currentGridWidth = gridManager.Width;
        int currentGridHeight = gridManager.Height;
        float currentCellSize = gridManager.CellSize;
        float currentAspect = targetCamera.aspect;

        bool gridGeometryChanged =
            currentGridWidth != lastKnownGridWidth ||
            currentGridHeight != lastKnownGridHeight ||
            Mathf.Abs(currentCellSize - lastKnownCellSize) > 0.0001f;

        bool aspectChanged = Mathf.Abs(currentAspect - lastKnownAspect) > 0.001f;

        if (!gridGeometryChanged && !aspectChanged)
        {
            return;
        }

        UpdateConstraintCache();

        if (gridGeometryChanged && resetViewWhenGridChanges)
        {
            ForceResetViewToFit("그리드 크기 변경");
            return;
        }

        targetCamera.orthographicSize = ClampZoom(targetCamera.orthographicSize);
        ClampCameraToGrid();
    }

    private void UpdateConstraintCache()
    {
        GetGridHalfExtents(out float halfGridWidth, out float halfGridHeight);

        float paddedHalfWidth = halfGridWidth + Mathf.Max(0f, fitPadding);
        float paddedHalfHeight = halfGridHeight + Mathf.Max(0f, fitPadding);

        float sizeFromWidth = paddedHalfWidth / Mathf.Max(0.01f, targetCamera.aspect);
        float sizeFromHeight = paddedHalfHeight;

        cachedFitSize = Mathf.Max(minimumOverviewOrthographicSize, sizeFromWidth, sizeFromHeight);

        lastKnownGridWidth = gridManager.Width;
        lastKnownGridHeight = gridManager.Height;
        lastKnownCellSize = gridManager.CellSize;
        lastKnownAspect = targetCamera.aspect;
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            isMousePanning = false;
            return;
        }

        if (enableMouseWheelZoom)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                float nextSize = targetCamera.orthographicSize - (scrollY * mouseWheelZoomSensitivity);
                targetCamera.orthographicSize = ClampZoom(nextSize);
                MarkInteraction();
            }
        }

        if (!enableMousePan)
        {
            isMousePanning = false;
            return;
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            isMousePanning = true;
            lastMouseScreenPosition = mouse.position.ReadValue();
            return;
        }

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            isMousePanning = false;
            return;
        }

        if (!isMousePanning || !mouse.rightButton.isPressed)
        {
            return;
        }

        Vector2 currentMouseScreenPosition = mouse.position.ReadValue();
        if ((currentMouseScreenPosition - lastMouseScreenPosition).sqrMagnitude <= 0.001f)
        {
            return;
        }

        PanFromScreenDelta(lastMouseScreenPosition, currentMouseScreenPosition);
        lastMouseScreenPosition = currentMouseScreenPosition;
        MarkInteraction();
    }

    private void HandleTouchInput()
    {
        isTouchGestureActive = false;

        if (!enableTouchPanAndPinch)
        {
            return;
        }

        if (!TryGetTwoActiveTouches(out TouchControl firstTouch, out TouchControl secondTouch))
        {
            return;
        }

        isTouchGestureActive = true;

        if (firstTouch.press.wasPressedThisFrame || secondTouch.press.wasPressedThisFrame)
        {
            return;
        }

        Vector2 firstPosition = firstTouch.position.ReadValue();
        Vector2 secondPosition = secondTouch.position.ReadValue();
        Vector2 firstDelta = firstTouch.delta.ReadValue();
        Vector2 secondDelta = secondTouch.delta.ReadValue();

        Vector2 currentMidpoint = (firstPosition + secondPosition) * 0.5f;
        Vector2 previousMidpoint = ((firstPosition - firstDelta) + (secondPosition - secondDelta)) * 0.5f;

        if ((currentMidpoint - previousMidpoint).sqrMagnitude > 0.001f)
        {
            PanFromScreenDelta(previousMidpoint, currentMidpoint);
            MarkInteraction();
        }

        float currentDistance = Vector2.Distance(firstPosition, secondPosition);
        float previousDistance = Vector2.Distance(firstPosition - firstDelta, secondPosition - secondDelta);

        if (currentDistance <= 0.01f || previousDistance <= 0.01f)
        {
            return;
        }

        float scaleRatio = previousDistance / currentDistance;
        if (Mathf.Abs(scaleRatio - 1f) > 0.0001f)
        {
            float nextSize = targetCamera.orthographicSize * scaleRatio;
            targetCamera.orthographicSize = ClampZoom(nextSize);
            MarkInteraction();
        }
    }

    private void PanFromScreenDelta(Vector2 previousScreenPosition, Vector2 currentScreenPosition)
    {
        if (!TryScreenToWorld(previousScreenPosition, out Vector3 previousWorld))
        {
            return;
        }

        if (!TryScreenToWorld(currentScreenPosition, out Vector3 currentWorld))
        {
            return;
        }

        Vector3 delta = previousWorld - currentWorld;

        Vector3 position = targetCamera.transform.position;
        position.x += delta.x;
        position.y += delta.y;
        targetCamera.transform.position = position;
    }

    private bool TryScreenToWorld(Vector2 screenPosition, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (targetCamera == null)
        {
            return false;
        }

        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera)
        );

        return true;
    }

    private void GetGridHalfExtents(out float halfGridWidth, out float halfGridHeight)
    {
        float safeCellSize = Mathf.Max(0.01f, gridManager.CellSize);

        halfGridWidth = gridManager.Width * safeCellSize * 0.5f;
        halfGridHeight = gridManager.Height * safeCellSize * 0.5f;
    }

    private void ClampCameraToGrid()
    {
        GetGridHalfExtents(out float halfGridWidth, out float halfGridHeight);

        float padding = Mathf.Max(0f, panBoundsPadding);
        float halfCameraHeight = targetCamera.orthographicSize;
        float halfCameraWidth = halfCameraHeight * Mathf.Max(0.01f, targetCamera.aspect);

        float minX = (-halfGridWidth - padding) + halfCameraWidth;
        float maxX = (halfGridWidth + padding) - halfCameraWidth;
        float minY = (-halfGridHeight - padding) + halfCameraHeight;
        float maxY = (halfGridHeight + padding) - halfCameraHeight;

        float defaultCenterX = 0f;
        float defaultCenterY = GetDefaultCenterYForCurrentView();

        Vector3 position = targetCamera.transform.position;
        position.x = ClampOrAllowSlack(position.x, minX, maxX, defaultCenterX, freePanSlackXWhenOverviewFits);
        position.y = ClampOrAllowSlack(position.y, minY, maxY, defaultCenterY, freePanSlackYWhenOverviewFits);
        targetCamera.transform.position = position;
    }

    private float ClampZoom(float desiredSize)
    {
        float minZoomSize = Mathf.Max(absoluteMinimumOrthographicSize, cachedFitSize * zoomInFactor);
        float maxZoomSize = GetMaxZoomSize();

        if (maxZoomSize < minZoomSize)
        {
            maxZoomSize = minZoomSize;
        }

        return Mathf.Clamp(desiredSize, minZoomSize, maxZoomSize);
    }

    private float GetMaxZoomSize()
    {
        return cachedFitSize * Mathf.Max(1f, zoomOutFactor);
    }

    private float GetDefaultCenterYForCurrentView()
    {
        if (targetCamera == null)
        {
            return 0f;
        }

        if (targetCamera.aspect >= 1f)
        {
            return 0f;
        }

        return targetCamera.orthographicSize * Mathf.Max(0f, portraitOverviewVerticalBias);
    }

    private void MarkInteraction()
    {
        lastInteractionUnscaledTime = Time.unscaledTime;
    }

    private static float ClampOrAllowSlack(float value, float min, float max, float centerValue, float slackWhenOverviewFits)
    {
        if (min <= max)
        {
            return Mathf.Clamp(value, min, max);
        }

        float safeSlack = Mathf.Max(0f, slackWhenOverviewFits);
        return Mathf.Clamp(value, centerValue - safeSlack, centerValue + safeSlack);
    }

    private static bool TryGetTwoActiveTouches(out TouchControl firstTouch, out TouchControl secondTouch)
    {
        firstTouch = default;
        secondTouch = default;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return false;
        }

        int foundCount = 0;

        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            TouchControl candidate = touchscreen.touches[i];
            if (candidate == null || !candidate.press.isPressed)
            {
                continue;
            }

            if (foundCount == 0)
            {
                firstTouch = candidate;
                foundCount = 1;
                continue;
            }

            secondTouch = candidate;
            return true;
        }

        firstTouch = default;
        secondTouch = default;
        return false;
    }
}