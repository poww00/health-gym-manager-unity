using UnityEngine;

[DisallowMultipleComponent]
public sealed class GymPlacedObjectVisual : MonoBehaviour
{
    private static Sprite cachedWhiteSprite;
    private static Font cachedFont;

    private SpriteRenderer shadowRenderer;
    private SpriteRenderer baseRenderer;
    private SpriteRenderer plateRenderer;
    private SpriteRenderer accentRenderer;
    private SpriteRenderer frameRenderer;
    private TextMesh tokenText;
    private TextMesh tierText;
    private MeshRenderer tokenRenderer;
    private MeshRenderer tierRenderer;

    private Vector2 footprintSize = Vector2.one;
    private Color accentColor = new Color(0.91f, 0.63f, 0.22f, 1f);
    private string token = "헬";
    private string tier = "B";

    public void Initialize(PlacedObjectSaveData data, EquipmentDefinition definition, Vector2 size)
    {
        footprintSize = size;
        accentColor = ResolveAccentColor(definition, data);
        token = ResolveToken(definition, data);
        tier = definition != null ? definition.BrandTierLabel : "B";

        EnsureAssets();
        EnsureChildren();
        ApplyLayout();
        ApplyState(data, false, false);
    }

    public void ApplyState(PlacedObjectSaveData data, bool selected, bool ghost)
    {
        bool isUnderConstruction = data != null && data.isUnderConstruction;
        bool isBroken = data != null && data.isBroken;

        Color activeAccent = accentColor;
        if (isBroken)
        {
            activeAccent = new Color(0.84f, 0.32f, 0.28f, 1f);
        }
        else if (isUnderConstruction)
        {
            activeAccent = Color.Lerp(accentColor, new Color(0.60f, 0.67f, 0.76f, 1f), 0.55f);
        }

        Color shellColor = Color.Lerp(new Color(0.12f, 0.16f, 0.23f, 1f), activeAccent, 0.20f);
        Color plateColor = Color.Lerp(new Color(0.22f, 0.27f, 0.36f, 1f), activeAccent, 0.14f);
        Color selectedFrame = isUnderConstruction
            ? new Color(0.72f, 0.77f, 0.82f, 0.86f)
            : new Color(0.62f, 1.00f, 0.25f, 0.88f);
        Color frameColor = selected
            ? selectedFrame
            : new Color(0.98f, 0.92f, 0.82f, 0f);

        if (ghost)
        {
            Color ghostFrame = isUnderConstruction
                ? new Color(0.72f, 0.77f, 0.82f, 0.36f)
                : new Color(0.62f, 1.00f, 0.25f, 0.40f);
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.10f);
            baseRenderer.color = new Color(shellColor.r, shellColor.g, shellColor.b, 0.20f);
            plateRenderer.color = new Color(plateColor.r, plateColor.g, plateColor.b, 0.14f);
            accentRenderer.color = new Color(activeAccent.r, activeAccent.g, activeAccent.b, 0.22f);
            frameRenderer.color = ghostFrame;
            tokenText.color = new Color(0.97f, 0.97f, 0.95f, 0.45f);
            tierText.color = new Color(0.97f, 0.97f, 0.95f, 0.45f);
            return;
        }

        if (selected)
        {
            Color selectedTint = isUnderConstruction
                ? new Color(0.74f, 0.78f, 0.82f, 1f)
                : new Color(0.58f, 1.00f, 0.26f, 1f);
            shellColor = Color.Lerp(shellColor, selectedTint, 0.08f);
            plateColor = Color.Lerp(plateColor, new Color(0.97f, 0.98f, 0.92f, 1f), 0.08f);
        }

        shadowRenderer.color = new Color(0f, 0f, 0f, selected ? 0.34f : 0.24f);
        baseRenderer.color = shellColor;
        plateRenderer.color = plateColor;
        accentRenderer.color = activeAccent;
        frameRenderer.color = frameColor;
        tokenText.color = isUnderConstruction
            ? new Color(0.90f, 0.92f, 0.96f, 1f)
            : new Color(0.98f, 0.98f, 0.94f, 1f);
        tierText.color = new Color(0.98f, 0.98f, 0.94f, 0.92f);

        tokenText.text = isUnderConstruction ? "BUILD" : token;
        tierText.text = isBroken ? "FIX" : tier;
    }

    private void EnsureAssets()
    {
        if (cachedWhiteSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            cachedWhiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    private void EnsureChildren()
    {
        shadowRenderer = EnsureSpriteRenderer("Shadow", 6);
        frameRenderer = EnsureSpriteRenderer("Frame", 7);
        frameRenderer.sprite = RuntimeHighlightSpriteFactory.GetSoftRoundedOutlineSprite();
        baseRenderer = EnsureSpriteRenderer("Base", 8);
        plateRenderer = EnsureSpriteRenderer("Plate", 9);
        accentRenderer = EnsureSpriteRenderer("Accent", 10);

        tokenText = EnsureText("TokenText", out tokenRenderer, 12);
        tierText = EnsureText("TierText", out tierRenderer, 13);
    }

    private void ApplyLayout()
    {
        float width = Mathf.Max(0.55f, footprintSize.x);
        float height = Mathf.Max(0.55f, footprintSize.y);

        ConfigureRenderer(shadowRenderer, new Vector3(0f, -0.06f, 0f), new Vector2(width * 1.02f, height * 0.98f));
        ConfigureRenderer(frameRenderer, Vector3.zero, new Vector2(width * 1.24f, height * 1.20f));
        ConfigureRenderer(baseRenderer, Vector3.zero, new Vector2(width * 0.96f, height * 0.92f));
        ConfigureRenderer(plateRenderer, new Vector3(0f, -0.02f, 0f), new Vector2(width * 0.72f, height * 0.52f));
        ConfigureRenderer(accentRenderer, new Vector3(0f, height * 0.28f, 0f), new Vector2(width * 0.96f, Mathf.Max(0.18f, height * 0.24f)));

        float tokenSize = Mathf.Clamp(Mathf.Min(width, height) * 0.08f, 0.05f, 0.11f);
        tokenText.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        tokenText.characterSize = tokenSize;
        tokenText.fontSize = 72;
        tokenText.text = token;

        float tierSize = Mathf.Clamp(tokenSize * 0.72f, 0.04f, 0.08f);
        tierText.transform.localPosition = new Vector3(width * 0.24f, height * 0.27f, 0f);
        tierText.characterSize = tierSize;
        tierText.fontSize = 52;
        tierText.anchor = TextAnchor.MiddleCenter;
        tierText.alignment = TextAlignment.Center;
        tierText.text = tier;
    }

    private SpriteRenderer EnsureSpriteRenderer(string childName, int sortingOrder)
    {
        Transform child = transform.Find(childName);
        GameObject node = child != null ? child.gameObject : new GameObject(childName);
        node.transform.SetParent(transform, false);

        SpriteRenderer renderer = node.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = node.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = cachedWhiteSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private TextMesh EnsureText(string childName, out MeshRenderer meshRenderer, int sortingOrder)
    {
        Transform child = transform.Find(childName);
        GameObject node = child != null ? child.gameObject : new GameObject(childName);
        node.transform.SetParent(transform, false);

        TextMesh textMesh = node.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = node.AddComponent<TextMesh>();
        }

        textMesh.font = cachedFont;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;

        meshRenderer = node.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = sortingOrder;
        }

        return textMesh;
    }

    private static void ConfigureRenderer(SpriteRenderer renderer, Vector3 localPosition, Vector2 size)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.transform.localPosition = localPosition;
        renderer.size = size;
    }

    private static string ResolveToken(EquipmentDefinition definition, PlacedObjectSaveData data)
    {
        if (definition != null)
        {
            switch (definition.Category)
            {
                case EquipmentCategory.Cardio:
                    return "유";
                case EquipmentCategory.Push:
                    return "푸";
                case EquipmentCategory.Pull:
                    return "풀";
                case EquipmentCategory.Legs:
                    return "하";
                case EquipmentCategory.Recovery:
                    return "회";
                default:
                    return "헬";
            }
        }

        string name = data != null ? data.displayName : string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return "헬";
        }

        string compact = name.Replace(" ", string.Empty).ToUpperInvariant();
        return compact.Length <= 4 ? compact : compact.Substring(0, 4);
    }

    private static Color ResolveAccentColor(EquipmentDefinition definition, PlacedObjectSaveData data)
    {
        if (definition != null)
        {
            switch (definition.Category)
            {
                case EquipmentCategory.Cardio:
                    return new Color(0.22f, 0.67f, 0.86f, 1f);
                case EquipmentCategory.Push:
                    return new Color(0.92f, 0.48f, 0.22f, 1f);
                case EquipmentCategory.Pull:
                    return new Color(0.58f, 0.48f, 0.92f, 1f);
                case EquipmentCategory.Legs:
                    return new Color(0.92f, 0.72f, 0.22f, 1f);
                case EquipmentCategory.Recovery:
                    return new Color(0.32f, 0.76f, 0.48f, 1f);
                default:
                    return definition.DebugColor;
            }
        }

        return data != null && data.isBroken
            ? new Color(0.84f, 0.32f, 0.28f, 1f)
            : new Color(0.91f, 0.63f, 0.22f, 1f);
    }
}
