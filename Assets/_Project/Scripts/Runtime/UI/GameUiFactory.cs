using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class GameUiFactory
{
    private static readonly string[] LegacyRuntimeEventSystemNames =
    {
        "GameUIRuntime_EventSystem",
        "TitleUIRuntime_EventSystem"
    };

    public static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }

    public static Canvas EnsureCanvas(Transform root, string name, int sortingOrder)
    {
        Transform canvasTransform = root.Find(name);
        GameObject canvasObject;
        if (canvasTransform != null)
        {
            canvasObject = canvasTransform.gameObject;
        }
        else
        {
            canvasObject = new GameObject(name, typeof(RectTransform));
            canvasObject.transform.SetParent(root, false);
        }

        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        GetOrAdd<GraphicRaycaster>(canvasObject);

        RectTransform rect = GetOrAdd<RectTransform>(canvasObject);
        Stretch(rect);
        return canvas;
    }

    public static EventSystem EnsureEventSystem(Transform parent, string name)
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            return existing;
        }

        GameObject eventSystemObject = new GameObject(name, typeof(EventSystem));
        if (parent != null)
        {
            eventSystemObject.transform.SetParent(parent, false);
        }

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        return eventSystemObject.GetComponent<EventSystem>();
    }

    public static void CleanupLegacyRuntimeEventSystems()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (eventSystem == null)
            {
                continue;
            }

            bool isLegacyGenerated = eventSystem.GetComponent<RectTransform>() != null;
            bool isRuntimeNamed = false;
            for (int nameIndex = 0; nameIndex < LegacyRuntimeEventSystemNames.Length; nameIndex++)
            {
                if (eventSystem.name == LegacyRuntimeEventSystemNames[nameIndex])
                {
                    isRuntimeNamed = true;
                    break;
                }
            }

            if (!isLegacyGenerated && !isRuntimeNamed)
            {
                continue;
            }

            DestroyObject(eventSystem.gameObject);
        }
    }

    public static void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            DestroyObject(root.GetChild(i).gameObject);
        }
    }

    public static GameObject CreateNode(Transform parent, string name, params System.Type[] components)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        node.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++)
        {
            if (node.GetComponent(components[i]) == null)
            {
                node.AddComponent(components[i]);
            }
        }

        return node;
    }

    public static void DestroyObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
            return;
        }

        Object.DestroyImmediate(target);
    }

    public static void Stretch(RectTransform rectTransform, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    public static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    public static GameObject CreatePanel(Transform parent, string name, GameUiTheme theme, Color fillColor, out RectTransform contentRoot, float padding = 18f)
    {
        GameObject panel = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image));
        Image border = panel.GetComponent<Image>();
        border.color = theme.Outline;
        border.type = Image.Type.Simple;
        border.raycastTarget = false;

        GameObject fill = CreateNode(panel.transform, "Fill", typeof(CanvasRenderer), typeof(Image));
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Simple;
        fillImage.raycastTarget = false;
        Stretch(fill.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);

        GameObject topBevel = CreateNode(fill.transform, "TopBevel", typeof(CanvasRenderer), typeof(Image));
        Image topBevelImage = topBevel.GetComponent<Image>();
        topBevelImage.color = Color.Lerp(fillColor, Color.white, 0.26f);
        topBevelImage.raycastTarget = false;
        SetAnchoredRect(topBevel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));

        GameObject sideBevel = CreateNode(fill.transform, "SideBevel", typeof(CanvasRenderer), typeof(Image));
        Image sideBevelImage = sideBevel.GetComponent<Image>();
        sideBevelImage.color = Color.Lerp(fillColor, Color.white, 0.12f);
        sideBevelImage.raycastTarget = false;
        SetAnchoredRect(sideBevel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 0f));

        GameObject bottomLip = CreateNode(fill.transform, "BottomLip", typeof(CanvasRenderer), typeof(Image));
        Image bottomLipImage = bottomLip.GetComponent<Image>();
        bottomLipImage.color = Color.Lerp(fillColor, theme.Outline, 0.28f);
        bottomLipImage.raycastTarget = false;
        SetAnchoredRect(bottomLip.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 10f));

        GameObject content = CreateNode(panel.transform, "Content");
        contentRoot = content.GetComponent<RectTransform>();
        Stretch(contentRoot, padding, padding, padding + 8f, padding + 6f);

        Shadow shadow = GetOrAdd<Shadow>(panel);
        shadow.effectColor = theme.Shadow;
        shadow.effectDistance = new Vector2(3f, -3f);

        return panel;
    }

    public static GameObject CreateDivider(Transform parent, string name, GameUiTheme theme, float height = 2f)
    {
        GameObject divider = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        Image image = divider.GetComponent<Image>();
        image.color = theme.Divider;
        image.raycastTarget = false;

        LayoutElement layout = divider.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
        return divider;
    }

    public static Text CreateText(Transform parent, string name, GameUiTheme theme, int fontSize, Color color, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal)
    {
        GameObject node = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Text));
        Text text = node.GetComponent<Text>();
        text.font = theme.Font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        EnhanceTextReadability(text, theme);
        return text;
    }

    public static void EnhanceTextReadability(Text text, GameUiTheme theme)
    {
        if (text == null)
        {
            return;
        }

        if (theme != null && theme.Font != null)
        {
            text.font = theme.Font;
        }

        if (text.font != null && text.font.material != null && text.font.material.mainTexture != null)
        {
            text.font.material.mainTexture.filterMode = FilterMode.Point;
            text.font.material.mainTexture.anisoLevel = 0;
        }

        text.alignByGeometry = true;
        text.resizeTextForBestFit = false;

        float luma = (text.color.r * 0.299f) + (text.color.g * 0.587f) + (text.color.b * 0.114f);

        Outline outline = GetOrAdd<Outline>(text.gameObject);
        Shadow shadow = GetOrAdd<Shadow>(text.gameObject);

        outline.effectColor = new Color(0f, 0f, 0f, 0f);
        outline.effectDistance = Vector2.zero;

        if (luma >= 0.65f)
        {
            shadow.effectColor = new Color(0.12f, 0.09f, 0.05f, 0.42f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }
        else
        {
            shadow.effectColor = new Color(0.10f, 0.08f, 0.05f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -1f);
        }
    }

    public static void ApplyLightHudText(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyle.Normal;

        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0f, 0f, 0f, 0f);
            outline.effectDistance = Vector2.zero;
        }

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow != null)
        {
            float luma = (text.color.r * 0.299f) + (text.color.g * 0.587f) + (text.color.b * 0.114f);
            if (luma >= 0.65f)
            {
                shadow.effectColor = new Color(0.10f, 0.08f, 0.05f, 0.12f);
                shadow.effectDistance = new Vector2(0f, -1f);
            }
            else
            {
                shadow.effectColor = new Color(0.10f, 0.08f, 0.05f, 0.04f);
                shadow.effectDistance = new Vector2(0f, -1f);
            }
        }
    }

    public static void ApplyMinimalHudText(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.fontStyle = FontStyle.Normal;

        Outline outline = text.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0f, 0f, 0f, 0f);
            outline.effectDistance = Vector2.zero;
        }

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = new Color(0f, 0f, 0f, 0f);
            shadow.effectDistance = Vector2.zero;
        }
    }

    public static void ConfigureSingleLineText(Text text, TextAnchor alignment, bool alignByGeometry = false)
    {
        if (text == null)
        {
            return;
        }

        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false;
        text.alignByGeometry = alignByGeometry;
    }

    public static void ConfigureWrappedText(Text text, TextAnchor alignment, bool alignByGeometry = false)
    {
        if (text == null)
        {
            return;
        }

        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false;
        text.alignByGeometry = alignByGeometry;
        text.lineSpacing = 0.9f;
    }

    public static Button CreateButton(Transform parent, string name, GameUiTheme theme, string label, GameUiTone tone, out Text labelText, float padding = 14f)
    {
        RectTransform contentRoot;
        GameObject buttonObject = CreatePanel(parent, name, theme, theme.GetToneFill(tone), out contentRoot, padding);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.raycastTarget = true;

        Button button = GetOrAdd<Button>(buttonObject);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
        colors.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        button.colors = colors;
        button.targetGraphic = buttonImage;

        labelText = CreateText(contentRoot, "Label", theme, 24, theme.GetToneInk(tone), TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(labelText.rectTransform);
        labelText.text = label;
        return button;
    }

    public static GameObject CreateStateChip(Transform parent, string name, GameUiTheme theme, string label, GameUiTone tone, out Text labelText)
    {
        RectTransform contentRoot;
        GameObject chip = CreatePanel(parent, name, theme, theme.GetToneFill(tone), out contentRoot, 12f);
        LayoutElement layout = GetOrAdd<LayoutElement>(chip);
        layout.preferredHeight = 52f;
        layout.flexibleWidth = 0f;

        labelText = CreateText(contentRoot, "Label", theme, 20, theme.GetToneInk(tone), TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(labelText.rectTransform);
        labelText.text = label;
        return chip;
    }

    public static GameObject CreateBadge(Transform parent, string name, GameUiTheme theme, string label, GameUiTone tone, out Text labelText)
    {
        GameObject badge = CreateStateChip(parent, name, theme, label, tone, out labelText);
        LayoutElement layout = badge.GetComponent<LayoutElement>();
        layout.preferredHeight = 42f;
        return badge;
    }

    public static GameObject CreateListRow(Transform parent, string name, GameUiTheme theme, out RectTransform contentRoot, float height)
    {
        GameObject row = CreatePanel(parent, name, theme, theme.PanelFill, out contentRoot, 16f);
        GetOrAdd<RectMask2D>(row);
        LayoutElement layout = GetOrAdd<LayoutElement>(row);
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
        return row;
    }

    public static GameObject CreateCard(Transform parent, string name, GameUiTheme theme, out RectTransform contentRoot, float height)
    {
        GameObject card = CreatePanel(parent, name, theme, theme.PanelFill, out contentRoot, 18f);
        GetOrAdd<RectMask2D>(card);
        LayoutElement layout = GetOrAdd<LayoutElement>(card);
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
        return card;
    }

    public static GameObject CreateMetricStrip(Transform parent, string name, out RectTransform contentRoot)
    {
        GameObject strip = CreateNode(parent, name, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup layout = strip.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        LayoutElement layoutElement = strip.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 122f;
        layoutElement.flexibleWidth = 1f;

        contentRoot = strip.GetComponent<RectTransform>();
        return strip;
    }

    public static ScrollRect CreateScrollView(Transform parent, string name, GameUiTheme theme, out RectTransform contentRoot)
    {
        GameObject root = CreateNode(parent, name, typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
        Image image = root.GetComponent<Image>();
        image.color = new Color(0.30f, 0.24f, 0.18f, 0.08f);
        image.raycastTarget = true;

        Mask mask = root.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 28f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = root.GetComponent<RectTransform>();

        GameObject content = CreateNode(root.transform, "Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentRoot = content.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(-24f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarObject = CreateNode(root.transform, "Scrollbar", typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        SetAnchoredRect(scrollbarRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(14f, -12f));

        Image scrollbarTrack = scrollbarObject.GetComponent<Image>();
        scrollbarTrack.color = new Color(0.17f, 0.19f, 0.24f, 0.28f);
        scrollbarTrack.raycastTarget = true;

        GameObject slidingArea = CreateNode(scrollbarObject.transform, "SlidingArea");
        RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
        Stretch(slidingAreaRect, 2f, 2f, 2f, 2f);

        GameObject handle = CreateNode(slidingArea.transform, "Handle", typeof(CanvasRenderer), typeof(Image));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        Stretch(handleRect);

        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.37f, 0.62f, 0.91f, 0.92f);
        handleImage.raycastTarget = true;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.25f;

        scrollRect.content = contentRoot;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return scrollRect;
    }
}
