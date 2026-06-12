using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialDimmerView : MonoBehaviour
{
    private const string TopName = "DimTop";
    private const string BottomName = "DimBottom";
    private const string LeftName = "DimLeft";
    private const string RightName = "DimRight";
    private const string CornerNamePrefix = "DimCorner";
    private const string FocusBlockerName = "FocusInputBlocker";
    private const string DebugOutlineName = "FocusDebugOutline";

    private readonly Image[] dimImages = new Image[4];
    private readonly Image[] cornerImages = new Image[4];
    private readonly Rect[] currentDimRects = new Rect[4];
    private RectTransform rectTransform;
    private RectTransform focusBlocker;
    private Image focusBlockerImage;
    private RectTransform debugOutline;
    private Image debugOutlineImage;
    private Sprite roundedCornerSprite;
    private Rect screenRect;
    private Color dimColor = new Color(0f, 0f, 0f, 0.58f);
    private bool debugOutlineVisible;

    public Rect CurrentFocusRect { get; private set; }
    public TutorialFocusMode CurrentFocusMode { get; private set; }
    public bool HasFocusHole { get; private set; }

    public bool TryGetCurrentDimPanelRects(out Rect top, out Rect bottom, out Rect left, out Rect right)
    {
        top = currentDimRects[0];
        bottom = currentDimRects[1];
        left = currentDimRects[2];
        right = currentDimRects[3];
        return true;
    }

    public void Initialize(Rect screen, Color color)
    {
        rectTransform = GetComponent<RectTransform>();
        screenRect = screen;
        dimColor = color;

        EnsurePanel(0, TopName);
        EnsurePanel(1, BottomName);
        EnsurePanel(2, LeftName);
        EnsurePanel(3, RightName);
        EnsureCornerPanel(0);
        EnsureCornerPanel(1);
        EnsureCornerPanel(2);
        EnsureCornerPanel(3);
        EnsureFocusBlocker();
        EnsureDebugOutline();
        SetDebugOutlineVisible(debugOutlineVisible);
    }

    public void Show(TutorialFocusTarget focusTarget)
    {
        CurrentFocusMode = focusTarget.Mode;
        HasFocusHole = focusTarget.HasHole;
        CurrentFocusRect = focusTarget.HasHole ? ClampToScreen(focusTarget.Rect) : Rect.zero;
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerView.Show called go=\"{gameObject.name}\" scene=\"{gameObject.scene.name}\" mode={focusTarget.Mode} target=\"{focusTarget.TargetName}\" hasHole={focusTarget.HasHole} currentFocusRect={CurrentFocusRect}");

        if (!HasFocusHole)
        {
            SetPanelRect(0, screenRect);
            SetPanelRect(1, Rect.zero);
            SetPanelRect(2, Rect.zero);
            SetPanelRect(3, Rect.zero);
            SetCornerMasks(Rect.zero, 0f);
            SetFocusBlocker(Rect.zero, false);
            UpdateDebugOutline();
            Debug.Log($"[InstallTutorialTrace] TutorialDimmerView.Show panels after layout {GetDebugPanelStateSummary()}");
            return;
        }

        Rect hole = CurrentFocusRect;
        SetPanelRect(0, Rect.MinMaxRect(screenRect.xMin, hole.yMax, screenRect.xMax, screenRect.yMax));
        SetPanelRect(1, Rect.MinMaxRect(screenRect.xMin, screenRect.yMin, screenRect.xMax, hole.yMin));
        SetPanelRect(2, Rect.MinMaxRect(screenRect.xMin, hole.yMin, hole.xMin, hole.yMax));
        SetPanelRect(3, Rect.MinMaxRect(hole.xMax, hole.yMin, screenRect.xMax, hole.yMax));
        SetCornerMasks(hole, GetCornerRadius(focusTarget.Mode, hole));
        SetFocusBlocker(hole, !focusTarget.AllowFocusInteraction);
        UpdateDebugOutline();
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerView.Show panels after layout {GetDebugPanelStateSummary()}");
    }

    public void Hide()
    {
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerView.Hide called before panels {GetDebugPanelStateSummary()}");
        for (int i = 0; i < dimImages.Length; i++)
        {
            SetPanelRect(i, Rect.zero);
        }

        SetCornerMasks(Rect.zero, 0f);
        SetFocusBlocker(Rect.zero, false);
        CurrentFocusMode = TutorialFocusMode.None;
        CurrentFocusRect = Rect.zero;
        HasFocusHole = false;
        UpdateDebugOutline();
        Debug.Log($"[InstallTutorialTrace] TutorialDimmerView.Hide finished panels {GetDebugPanelStateSummary()}");
    }

    public void SetDebugOutlineVisible(bool visible)
    {
        debugOutlineVisible = visible;
        if (debugOutline != null)
        {
            debugOutline.gameObject.SetActive(debugOutlineVisible && HasFocusHole);
        }
    }

    private void EnsurePanel(int index, string panelName)
    {
        if (dimImages[index] != null)
        {
            return;
        }

        Transform existing = transform.Find(panelName);
        GameObject panel = existing != null ? existing.gameObject : new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.transform.SetSiblingIndex(index);

        Image image = panel.GetComponent<Image>();
        image.color = dimColor;
        image.raycastTarget = true;
        dimImages[index] = image;
    }

    private void EnsureCornerPanel(int index)
    {
        if (cornerImages[index] != null)
        {
            return;
        }

        Transform existing = transform.Find($"{CornerNamePrefix}_{index}");
        GameObject panel = existing != null
            ? existing.gameObject
            : new GameObject($"{CornerNamePrefix}_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.transform.SetSiblingIndex(4 + index);

        Image image = panel.GetComponent<Image>();
        image.sprite = GetRoundedCornerSprite();
        image.type = Image.Type.Simple;
        image.color = dimColor;
        image.raycastTarget = true;
        panel.SetActive(false);
        cornerImages[index] = image;
    }

    private void EnsureDebugOutline()
    {
        if (debugOutline != null)
        {
            return;
        }

        GameObject outline = new GameObject(DebugOutlineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        outline.transform.SetParent(transform, false);
        debugOutline = outline.GetComponent<RectTransform>();
        debugOutlineImage = outline.GetComponent<Image>();
        debugOutlineImage.color = new Color(1f, 1f, 1f, 0.06f);
        debugOutlineImage.raycastTarget = false;

        Outline effect = outline.GetComponent<Outline>();
        effect.effectColor = new Color(0.1f, 1f, 0.1f, 0.95f);
        effect.effectDistance = new Vector2(4f, -4f);
    }

    private void EnsureFocusBlocker()
    {
        if (focusBlocker != null)
        {
            return;
        }

        Transform existing = transform.Find(FocusBlockerName);
        GameObject blocker = existing != null
            ? existing.gameObject
            : new GameObject(FocusBlockerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        blocker.transform.SetParent(transform, false);
        blocker.transform.SetSiblingIndex(8);

        focusBlocker = blocker.GetComponent<RectTransform>();
        focusBlockerImage = blocker.GetComponent<Image>();
        focusBlockerImage.color = new Color(0f, 0f, 0f, 0.001f);
        focusBlockerImage.raycastTarget = true;
        blocker.SetActive(false);
    }

    private void SetPanelRect(int index, Rect rect)
    {
        if (dimImages[index] == null)
        {
            return;
        }

        RectTransform panelRect = dimImages[index].rectTransform;
        bool visible = rect.width > 0.5f && rect.height > 0.5f;
        bool wasActive = dimImages[index].gameObject.activeSelf;
        currentDimRects[index] = visible ? rect : Rect.zero;
        dimImages[index].gameObject.SetActive(visible);
        if (wasActive != visible)
        {
            Debug.Log($"[InstallTutorialTrace] TutorialDimmerView.SetPanelActive panel={GetPanelName(index)} from={wasActive} to={visible} requestedRect={rect} storedRect={currentDimRects[index]} mode={CurrentFocusMode} focusRect={CurrentFocusRect}");
        }

        if (!visible)
        {
            return;
        }

        SetRect(panelRect, rect.center.x, rect.center.y, rect.width, rect.height);
    }

    public string GetDebugPanelStateSummary()
    {
        return $"top(active={IsPanelActive(0)}, rect={currentDimRects[0]}) " +
               $"bottom(active={IsPanelActive(1)}, rect={currentDimRects[1]}) " +
               $"left(active={IsPanelActive(2)}, rect={currentDimRects[2]}) " +
               $"right(active={IsPanelActive(3)}, rect={currentDimRects[3]})";
    }

    private bool IsPanelActive(int index)
    {
        return index >= 0 &&
               index < dimImages.Length &&
               dimImages[index] != null &&
               dimImages[index].gameObject.activeSelf;
    }

    private static string GetPanelName(int index)
    {
        switch (index)
        {
            case 0:
                return TopName;
            case 1:
                return BottomName;
            case 2:
                return LeftName;
            case 3:
                return RightName;
            default:
                return $"DimPanel_{index}";
        }
    }

    private void SetFocusBlocker(Rect rect, bool active)
    {
        if (focusBlocker == null)
        {
            return;
        }

        bool visible = active && rect.width > 0.5f && rect.height > 0.5f;
        focusBlocker.gameObject.SetActive(visible);
        if (visible)
        {
            SetRect(focusBlocker, rect.center.x, rect.center.y, rect.width, rect.height);
        }
    }

    private void SetCornerMasks(Rect hole, float radius)
    {
        bool visible = radius > 1f && hole.width > radius * 2f && hole.height > radius * 2f;
        for (int i = 0; i < cornerImages.Length; i++)
        {
            if (cornerImages[i] == null)
            {
                continue;
            }

            cornerImages[i].gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        float size = radius;
        SetCornerRect(0, hole.xMin + size * 0.5f, hole.yMax - size * 0.5f, size, 0f);
        SetCornerRect(1, hole.xMax - size * 0.5f, hole.yMax - size * 0.5f, size, -90f);
        SetCornerRect(2, hole.xMax - size * 0.5f, hole.yMin + size * 0.5f, size, 180f);
        SetCornerRect(3, hole.xMin + size * 0.5f, hole.yMin + size * 0.5f, size, 90f);
    }

    private void SetCornerRect(int index, float x, float y, float size, float rotation)
    {
        if (cornerImages[index] == null)
        {
            return;
        }

        RectTransform cornerRect = cornerImages[index].rectTransform;
        SetRect(cornerRect, x, y, size, size);
        cornerRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void UpdateDebugOutline()
    {
        if (debugOutline == null)
        {
            return;
        }

        debugOutline.gameObject.SetActive(debugOutlineVisible && HasFocusHole);
        if (!debugOutline.gameObject.activeSelf)
        {
            return;
        }

        SetRect(debugOutline, CurrentFocusRect.center.x, CurrentFocusRect.center.y, CurrentFocusRect.width, CurrentFocusRect.height);
    }

    private static float GetCornerRadius(TutorialFocusMode mode, Rect rect)
    {
        if (mode == TutorialFocusMode.TileBoardOnly || mode == TutorialFocusMode.None)
        {
            return 0f;
        }

        float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float desiredRadius = mode == TutorialFocusMode.PanelOnly ? 28f : 22f;
        return Mathf.Clamp(desiredRadius, 0f, maxRadius);
    }

    private Sprite GetRoundedCornerSprite()
    {
        if (roundedCornerSprite != null)
        {
            return roundedCornerSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            name = "TutorialDimRoundedCornerMask",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(size - 0.5f, -0.5f);
        float radius = size - 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance > radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        roundedCornerSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        roundedCornerSprite.name = "TutorialDimRoundedCornerMask";
        roundedCornerSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedCornerSprite;
    }

    private Rect ClampToScreen(Rect rect)
    {
        return Rect.MinMaxRect(
            Mathf.Clamp(rect.xMin, screenRect.xMin, screenRect.xMax),
            Mathf.Clamp(rect.yMin, screenRect.yMin, screenRect.yMax),
            Mathf.Clamp(rect.xMax, screenRect.xMin, screenRect.xMax),
            Mathf.Clamp(rect.yMax, screenRect.yMin, screenRect.yMax));
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
