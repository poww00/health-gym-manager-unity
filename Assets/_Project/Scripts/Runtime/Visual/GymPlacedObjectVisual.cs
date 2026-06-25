using UnityEngine;

[DisallowMultipleComponent]
public sealed class GymPlacedObjectVisual : MonoBehaviour
{
    private const int MachineDepthSortRange = 19;
    private const int MachineDepthSortStep = 64;
    private const float CustomSpriteBaseLocalY = 0.42f;
    private const float ExerciseBikeVisualReferenceWidth = 2f;
    private const float ExerciseBikeVisualWidthFactor = 0.55f;
    private const float ExerciseBikeVisualYOffset = 0.18f;
    private const float BenchPressVisualWidthFactor = 0.8f;
    private const float BenchPressVisualYOffset = 0.2f;
    private const float LatPulldownVisualWidthFactor = 0.8f;
    private const float LatPulldownVisualYOffset = -0.1f;
    private const float LegPressVisualYOffset = 0.28f;
    private const float ExerciseBikePedalOffsetX = 0.1f;
    private const float ExerciseBikePedalOffsetY = -0.2f;
    private const float ExerciseBikePedalScale = 0.5f;
    private const float MotionAnimationFps = 10f;
    private const float BenchPressMotionAnimationFps = 9f;
    private const float ExerciseBikePedalAnimationFps = 16f;
    private const int DefaultForegroundLayerOffset = 35;
    private const int LatPulldownHandleLayerOffset = CustomerBodyLayerOffset - 1;
    private static readonly int[] LegPressMachineFrameMap = { 0, 1, 2, 6, 7 };
    private static readonly Vector2[] ExerciseBikePedalHubPixels =
    {
        new Vector2(164f, 95f),
        new Vector2(149f, 94f),
        new Vector2(155f, 91f),
        new Vector2(142f, 87f),
        new Vector2(162f, 106f),
        new Vector2(137f, 105f),
        new Vector2(125f, 108f),
        new Vector2(135f, 108f),
    };

    public const int CustomerBodyLayerOffset = 30;
    public const int CustomerHeadLayerOffset = 31;
    public const float LatPulldownAnimationFps = 8f;
    public const float LegPressAnimationFps = 10f;

    public static int GetLatPulldownAnimationFrameIndex(int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0;
        }

        return Mathf.FloorToInt(Time.time * LatPulldownAnimationFps) % frameCount;
    }

    public static int GetLegPressAnimationStepIndex()
    {
        return Mathf.FloorToInt(Time.time * LegPressAnimationFps) % LegPressMachineFrameMap.Length;
    }

    public static int GetLegPressAnimationFrameIndex(int frameCount)
    {
        if (frameCount <= 0)
        {
            return 0;
        }

        int stepIndex = GetLegPressAnimationStepIndex();
        return Mathf.Clamp(LegPressMachineFrameMap[stepIndex], 0, frameCount - 1);
    }

    private static Sprite cachedWhiteSprite;
    private static Font cachedFont;
    private static Material outlineMaterial;

    private SpriteRenderer shadowRenderer;
    private SpriteRenderer baseRenderer;
    private SpriteRenderer plateRenderer;
    private SpriteRenderer accentRenderer;
    private SpriteRenderer frameRenderer;
    private SpriteRenderer beltRenderer;
    private SpriteRenderer rearForegroundRenderer;
    private SpriteRenderer foregroundRenderer;
    private SpriteRenderer[] outlineRenderers;
    private TextMesh tokenText;
    private TextMesh tierText;
    private MeshRenderer tokenRenderer;
    private MeshRenderer tierRenderer;

    private Vector2 footprintSize = Vector2.one;
    private Color accentColor = new Color(0.91f, 0.63f, 0.22f, 1f);
    private string token = "유";
    private string tier = "B";
    private bool hasCustomSprite = false;

    private Sprite[] baseAnimationFrames;
    private Sprite[] beltAnimationFrames;
    private Sprite[] foregroundAnimationFrames;
    private int currentFrameIndex;
    private int currentForegroundFrameIndex;
    private EquipmentDefinition currentDefinition;
    private int sortingDepthOffset;
    private bool reverseMotionAnimation;
    private bool foregroundVisibleWhenIdle;
    private bool useBenchPressMotionAnimation;
    private bool useLatPulldownMotionAnimation;
    private bool useLegPressMotionAnimation;

    private static bool IsExerciseBikeSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
            (spriteName.Equals("exercise_bike", System.StringComparison.OrdinalIgnoreCase) ||
             spriteName.StartsWith("exercise_bike_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBenchPressSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
            (spriteName.Equals("bench_press", System.StringComparison.OrdinalIgnoreCase) ||
             spriteName.StartsWith("bench_press_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLatPulldownSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
            (spriteName.Equals("lat_pulldown", System.StringComparison.OrdinalIgnoreCase) ||
             spriteName.StartsWith("lat_pulldown_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLegPressSpriteName(string spriteName)
    {
        return !string.IsNullOrEmpty(spriteName) &&
            (spriteName.Equals("leg_press", System.StringComparison.OrdinalIgnoreCase) ||
             spriteName.StartsWith("leg_press_", System.StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveMotionLayerResourcePath(string spriteName)
    {
        string layerSuffix = IsExerciseBikeSpriteName(spriteName) ? "_pedal" : "_belt";
        return $"GeneratedRuntimeUI/objects/{spriteName}{layerSuffix}";
    }

    private static string StripEquipmentGradeSuffix(string spriteName)
    {
        string normalized = string.IsNullOrWhiteSpace(spriteName)
            ? string.Empty
            : spriteName.ToLowerInvariant().Trim();

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

    private static Sprite[] LoadObjectSprites(string spriteName, out string resolvedSpriteName)
    {
        resolvedSpriteName = string.IsNullOrWhiteSpace(spriteName)
            ? string.Empty
            : spriteName.ToLowerInvariant().Trim();

        if (string.IsNullOrEmpty(resolvedSpriteName))
        {
            return null;
        }

        if (IsBenchPressSpriteName(resolvedSpriteName))
        {
            Sprite[] benchPressBaseSprites = Resources.LoadAll<Sprite>("GeneratedRuntimeUI/objects/bench_press");
            if (benchPressBaseSprites != null && benchPressBaseSprites.Length > 0)
            {
                resolvedSpriteName = "bench_press";
                return benchPressBaseSprites;
            }
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>($"GeneratedRuntimeUI/objects/{resolvedSpriteName}");
        if (sprites != null && sprites.Length > 0)
        {
            return sprites;
        }

        string fallbackSpriteName = StripEquipmentGradeSuffix(resolvedSpriteName);
        if (!string.Equals(fallbackSpriteName, resolvedSpriteName, System.StringComparison.Ordinal))
        {
            resolvedSpriteName = fallbackSpriteName;
            return Resources.LoadAll<Sprite>($"GeneratedRuntimeUI/objects/{resolvedSpriteName}");
        }

        return sprites;
    }

    private static int CompareSpritesByName(Sprite left, Sprite right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    public void Initialize(PlacedObjectSaveData data, EquipmentDefinition definition, Vector2 size)
    {
        currentDefinition = definition;
        sortingDepthOffset = GetSortingDepthOffset(data);
        footprintSize = size;
        accentColor = ResolveAccentColor(definition, data);
        token = ResolveToken(definition, data);
        tier = definition != null ? definition.BrandTierLabel : "B";

        EnsureAssets();
        EnsureChildren();
        ApplyLayout();
        ApplyState(data, false, false);
    }

    private bool isMachineInUse = false;

    private void Update()
    {
        Sprite[] activeFrames = beltAnimationFrames != null && beltAnimationFrames.Length > 0
            ? beltAnimationFrames
            : baseAnimationFrames;
        SpriteRenderer activeRenderer = beltAnimationFrames != null && beltAnimationFrames.Length > 0
            ? beltRenderer
            : baseRenderer;

        if (isMachineInUse && activeFrames != null && activeFrames.Length > 0 && activeRenderer != null)
        {
            int synchronizedFrameIndex;
            if (useLatPulldownMotionAnimation)
            {
                synchronizedFrameIndex = GetLatPulldownAnimationFrameIndex(activeFrames.Length);
            }
            else if (useLegPressMotionAnimation)
            {
                synchronizedFrameIndex = GetLegPressAnimationFrameIndex(activeFrames.Length);
            }
            else
            {
                float animationFps = reverseMotionAnimation ? ExerciseBikePedalAnimationFps : MotionAnimationFps;
                int rawFrameIndex = Mathf.FloorToInt(Time.time * animationFps) % activeFrames.Length;
                synchronizedFrameIndex = reverseMotionAnimation && activeFrames.Length > 1
                    ? (activeFrames.Length - rawFrameIndex) % activeFrames.Length
                    : rawFrameIndex;
            }

            if (currentFrameIndex != synchronizedFrameIndex)
            {
                currentFrameIndex = synchronizedFrameIndex;
                activeRenderer.sprite = activeFrames[currentFrameIndex];
                if (activeRenderer == beltRenderer)
                {
                    ApplyBeltRendererTransform(activeFrames[currentFrameIndex]);
                }

                // Keep outlines synchronized only when the base sprite itself is animating.
                if ((beltAnimationFrames == null || beltAnimationFrames.Length == 0)
                    && outlineRenderers != null && outlineRenderers.Length == 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (outlineRenderers[i] != null) outlineRenderers[i].sprite = activeFrames[currentFrameIndex];
                    }
                }
            }
        }

        if (isMachineInUse && foregroundAnimationFrames != null && foregroundAnimationFrames.Length > 0 && foregroundRenderer != null)
        {
            int foregroundFrameIndex = useLatPulldownMotionAnimation
                ? GetLatPulldownAnimationFrameIndex(foregroundAnimationFrames.Length)
                : Mathf.FloorToInt(Time.time * (useBenchPressMotionAnimation ? BenchPressMotionAnimationFps : MotionAnimationFps)) % foregroundAnimationFrames.Length;
            if (currentForegroundFrameIndex != foregroundFrameIndex)
            {
                currentForegroundFrameIndex = foregroundFrameIndex;
                foregroundRenderer.sprite = foregroundAnimationFrames[currentForegroundFrameIndex];
            }
        }
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

            if (hasCustomSprite)
            {
                baseRenderer.color = new Color(1f, 1f, 1f, 0.40f);
                if (beltRenderer != null) beltRenderer.color = new Color(1f, 1f, 1f, 0.40f);
                if (rearForegroundRenderer != null) rearForegroundRenderer.color = new Color(1f, 1f, 1f, 0.40f);
                if (foregroundRenderer != null) foregroundRenderer.color = new Color(1f, 1f, 1f, 0.40f);
            }
            else
            {
                baseRenderer.color = new Color(shellColor.r, shellColor.g, shellColor.b, 0.20f);
            }

            plateRenderer.color = new Color(plateColor.r, plateColor.g, plateColor.b, 0.14f);
            accentRenderer.color = new Color(activeAccent.r, activeAccent.g, activeAccent.b, 0.22f);
            frameRenderer.color = ghostFrame;
            if (tokenText != null) tokenText.color = new Color(0.97f, 0.97f, 0.95f, 0.45f);
            if (tierText != null) tierText.color = new Color(0.97f, 0.97f, 0.95f, 0.45f);

            if (outlineRenderers != null) {
                for (int i = 0; i < 4; i++) {
                    if (outlineRenderers[i] != null) outlineRenderers[i].color = new Color(0,0,0,0);
                }
            }
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

        if (hasCustomSprite)
        {
            baseRenderer.color = Color.white;
            if (beltRenderer != null) beltRenderer.color = Color.white;
            if (rearForegroundRenderer != null) rearForegroundRenderer.color = Color.white;
            if (foregroundRenderer != null) foregroundRenderer.color = Color.white;
        }
        else
        {
            baseRenderer.color = shellColor;
        }

        plateRenderer.color = plateColor;
        accentRenderer.color = activeAccent;
        frameRenderer.color = frameColor;

        if (tokenText != null) {
            tokenText.color = isUnderConstruction
                ? new Color(0.90f, 0.92f, 0.96f, 1f)
                : new Color(0.98f, 0.98f, 0.94f, 1f);
            tokenText.text = isUnderConstruction ? "BUILD" : token;
        }
        if (tierText != null) {
            tierText.color = new Color(0.98f, 0.98f, 0.94f, 0.92f);
            tierText.text = isBroken ? "FIX" : tier;
        }

        if (outlineRenderers != null) {
            for (int i = 0; i < 4; i++) {
                if (outlineRenderers[i] != null) {
                    outlineRenderers[i].color = selected ? selectedFrame : new Color(0,0,0,0);
                }
            }
        }
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

        if (outlineMaterial == null)
        {
            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null) outlineMaterial = new Material(shader);
        }
    }

    private void EnsureChildren()
    {
        UnityEngine.Rendering.SortingGroup sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sortingGroup != null)
        {
            sortingGroup.enabled = false;
        }

        shadowRenderer = EnsureSpriteRenderer("Shadow", 6);
        frameRenderer = EnsureSpriteRenderer("Frame", 7);
        // Fallback or missing sprite for frameRenderer is fine
        baseRenderer = EnsureSpriteRenderer("Base", 8);
        beltRenderer = EnsureSpriteRenderer("Belt", 9);
        rearForegroundRenderer = EnsureSpriteRenderer("RearForeground", 10);
        plateRenderer = EnsureSpriteRenderer("Plate", 9);
        accentRenderer = EnsureSpriteRenderer("Accent", 10);
        foregroundRenderer = EnsureSpriteRenderer("Foreground", DefaultForegroundLayerOffset);

        if (outlineRenderers == null || outlineRenderers.Length != 4) {
            outlineRenderers = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++) {
                outlineRenderers[i] = EnsureSpriteRenderer($"Outline_{i}", 7);
                if (outlineMaterial != null) outlineRenderers[i].material = outlineMaterial;
            }
        }

        tokenText = EnsureText("TokenText", out tokenRenderer, 12);
        tierText = EnsureText("TierText", out tierRenderer, 13);
    }

    private void ApplyLayout()
    {
        float targetWidth = Mathf.Max(0.55f, footprintSize.x);

        string spriteName = currentDefinition != null ? currentDefinition.EquipmentId : "";
        Sprite[] loadedSprites = null;

        if (!string.IsNullOrEmpty(spriteName))
        {
            loadedSprites = LoadObjectSprites(spriteName, out spriteName);
            if (loadedSprites != null && loadedSprites.Length > 1)
            {
                System.Array.Sort(loadedSprites, CompareSpritesByName);
            }
        }

        bool useExerciseBikeVisualScale = IsExerciseBikeSpriteName(spriteName);
        bool useBenchPressVisualScale = IsBenchPressSpriteName(spriteName);
        bool useLatPulldownVisualScale = IsLatPulldownSpriteName(spriteName);
        bool useLegPressVisualScale = IsLegPressSpriteName(spriteName);
        hasCustomSprite = loadedSprites != null && loadedSprites.Length > 0;

        if (foregroundRenderer != null)
        {
            foregroundRenderer.sortingOrder = sortingDepthOffset + (useLatPulldownVisualScale
                ? LatPulldownHandleLayerOffset
                : DefaultForegroundLayerOffset);
        }

        if (hasCustomSprite)
        {
            baseAnimationFrames = loadedSprites;
            beltAnimationFrames = null;
            foregroundAnimationFrames = null;
            reverseMotionAnimation = useExerciseBikeVisualScale;
            foregroundVisibleWhenIdle = IsBenchPressSpriteName(spriteName);
            useBenchPressMotionAnimation = IsBenchPressSpriteName(spriteName);
            useLatPulldownMotionAnimation = useLatPulldownVisualScale;
            useLegPressMotionAnimation = useLegPressVisualScale;
            currentFrameIndex = 0;
            currentForegroundFrameIndex = 0;
            baseRenderer.sprite = baseAnimationFrames[0];
            baseRenderer.drawMode = SpriteDrawMode.Simple;

            // BUG FIX: Prevent scale bouncing by ONLY using first frame width!
            float currentWidth = baseAnimationFrames[0] != null ? baseAnimationFrames[0].bounds.size.x : 1f;
            float visualReferenceWidth = useExerciseBikeVisualScale
                ? Mathf.Max(targetWidth, ExerciseBikeVisualReferenceWidth)
                : targetWidth;
            float visualTargetWidth = useExerciseBikeVisualScale
                ? visualReferenceWidth * ExerciseBikeVisualWidthFactor
                : useBenchPressVisualScale
                    ? targetWidth * BenchPressVisualWidthFactor
                    : useLatPulldownVisualScale
                        ? targetWidth * LatPulldownVisualWidthFactor
                        : targetWidth;
            float defaultScale = visualReferenceWidth / currentWidth;
            float scale = visualTargetWidth / currentWidth;
            scale = Mathf.Clamp(scale, 0.1f, 5f); // Prevent ridiculous scale

            Vector3 baseLocalPosition = new Vector3(0f, CustomSpriteBaseLocalY, 0f);
            if ((useExerciseBikeVisualScale || useBenchPressVisualScale || useLatPulldownVisualScale) && baseAnimationFrames[0] != null)
            {
                float heightCompensation = baseAnimationFrames[0].bounds.size.y * (defaultScale - scale) * 0.5f;
                baseLocalPosition.y -= heightCompensation;
            }

            if (useExerciseBikeVisualScale && baseAnimationFrames[0] != null)
            {
                baseLocalPosition.y += ExerciseBikeVisualYOffset;
            }

            if (useBenchPressVisualScale && baseAnimationFrames[0] != null)
            {
                baseLocalPosition.y += BenchPressVisualYOffset;
            }

            if (useLatPulldownVisualScale && baseAnimationFrames[0] != null)
            {
                baseLocalPosition.y += LatPulldownVisualYOffset;
            }

            if (useLegPressVisualScale && baseAnimationFrames[0] != null)
            {
                baseLocalPosition.y += LegPressVisualYOffset;
            }

            baseRenderer.transform.localPosition = baseLocalPosition;
            baseRenderer.transform.localScale = new Vector3(scale, scale, 1f);

            Sprite[] beltSprites = Resources.LoadAll<Sprite>(ResolveMotionLayerResourcePath(spriteName));
            if (beltSprites != null && beltSprites.Length > 0)
            {
                System.Array.Sort(beltSprites, CompareSpritesByName);
                beltAnimationFrames = beltSprites;
                beltRenderer.sprite = beltAnimationFrames[0];
                beltRenderer.drawMode = SpriteDrawMode.Simple;
                ApplyBeltRendererTransform(beltAnimationFrames[0]);
                beltRenderer.gameObject.SetActive(true);
            }
            else if (beltRenderer != null)
            {
                beltRenderer.sprite = null;
                beltRenderer.gameObject.SetActive(false);
            }

            // Hide fallback meshes
            shadowRenderer.gameObject.SetActive(false);
            plateRenderer.gameObject.SetActive(false);
            accentRenderer.gameObject.SetActive(false);
            if (tokenRenderer != null) tokenRenderer.gameObject.SetActive(false);
            if (tierRenderer != null) tierRenderer.gameObject.SetActive(false);

            bool useForegroundSpriteLayer = (currentDefinition != null && currentDefinition.UseForegroundSprite)
                || IsBenchPressSpriteName(spriteName)
                || IsLatPulldownSpriteName(spriteName);

            if (useForegroundSpriteLayer)
            {
                Sprite[] backSprites = Resources.LoadAll<Sprite>($"GeneratedRuntimeUI/objects/{spriteName}_back");
                if (backSprites != null && backSprites.Length > 0)
                {
                    System.Array.Sort(backSprites, CompareSpritesByName);
                    rearForegroundRenderer.sprite = backSprites[0];
                    rearForegroundRenderer.drawMode = SpriteDrawMode.Simple;
                    rearForegroundRenderer.transform.localPosition = baseRenderer.transform.localPosition;
                    rearForegroundRenderer.transform.localScale = baseRenderer.transform.localScale;
                    rearForegroundRenderer.gameObject.SetActive(foregroundVisibleWhenIdle);
                }
                else if (rearForegroundRenderer != null)
                {
                    rearForegroundRenderer.sprite = null;
                    rearForegroundRenderer.gameObject.SetActive(false);
                }

                Sprite[] frontSprites = Resources.LoadAll<Sprite>($"GeneratedRuntimeUI/objects/{spriteName}_front");
                if (frontSprites != null && frontSprites.Length > 0)
                {
                    System.Array.Sort(frontSprites, CompareSpritesByName);
                    foregroundAnimationFrames = frontSprites;
                    foregroundRenderer.sprite = frontSprites[0];
                    foregroundRenderer.drawMode = SpriteDrawMode.Simple;

                    Vector3 foregroundPos = baseRenderer.transform.localPosition;
                    if (currentDefinition != null && currentDefinition.ForegroundOffset != Vector2.zero)
                    {
                        // BUG FIX: Scale the offset visually so it perfectly overlaps the downscaled machine!
                        foregroundPos += new Vector3(currentDefinition.ForegroundOffset.x * scale, currentDefinition.ForegroundOffset.y * scale, 0f);
                    }

                    foregroundRenderer.transform.localPosition = foregroundPos;
                    foregroundRenderer.transform.localScale = baseRenderer.transform.localScale;

                    // Controlled externally by SetForegroundActive; bench press keeps the racked bar visible while idle.
                    foregroundRenderer.gameObject.SetActive(foregroundVisibleWhenIdle);
                }
                else
                {
                    foregroundAnimationFrames = null;
                    if (foregroundRenderer != null) foregroundRenderer.sprite = null;
                    foregroundRenderer.gameObject.SetActive(false);
                }
            }
            else
            {
                if (rearForegroundRenderer != null)
                {
                    rearForegroundRenderer.sprite = null;
                    rearForegroundRenderer.gameObject.SetActive(false);
                }
                if (foregroundRenderer != null)
                {
                    foregroundAnimationFrames = null;
                    foregroundRenderer.sprite = null;
                    foregroundRenderer.gameObject.SetActive(false);
                }
            }

            frameRenderer.gameObject.SetActive(false);

            Vector3[] offsets = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
            for (int i = 0; i < 4; i++) {
                if (outlineRenderers[i] != null) {
                    outlineRenderers[i].sprite = baseRenderer.sprite;
                    outlineRenderers[i].drawMode = SpriteDrawMode.Simple;
                    outlineRenderers[i].transform.localPosition = baseRenderer.transform.localPosition + offsets[i] * 0.05f;
                    outlineRenderers[i].transform.localScale = baseRenderer.transform.localScale;
                    outlineRenderers[i].gameObject.SetActive(true);
                }
            }

            ConfigureRenderer(shadowRenderer, new Vector3(0f, -0.06f, 0f), new Vector2(targetWidth * 1.02f, targetWidth * 0.98f));
        }
        else
        {
            baseAnimationFrames = null;
            beltAnimationFrames = null;
            foregroundAnimationFrames = null;
            foregroundVisibleWhenIdle = false;
            useBenchPressMotionAnimation = false;
            useLatPulldownMotionAnimation = false;
            useLegPressMotionAnimation = false;
            reverseMotionAnimation = false;
            currentFrameIndex = 0;
            currentForegroundFrameIndex = 0;
            float width = targetWidth;
            float height = Mathf.Max(0.55f, footprintSize.y);

            shadowRenderer.gameObject.SetActive(true);
            ConfigureRenderer(shadowRenderer, new Vector3(0f, -0.06f, 0f), new Vector2(width * 1.02f, height * 0.98f));
            ConfigureRenderer(frameRenderer, Vector3.zero, new Vector2(width * 1.24f, height * 1.20f));
            ConfigureRenderer(baseRenderer, Vector3.zero, new Vector2(width * 0.96f, height * 0.92f));
            ConfigureRenderer(plateRenderer, new Vector3(0f, -0.02f, 0f), new Vector2(width * 0.72f, height * 0.52f));
            ConfigureRenderer(accentRenderer, new Vector3(0f, height * 0.28f, 0f), new Vector2(width * 0.96f, Mathf.Max(0.18f, height * 0.24f)));

            float tokenSize = Mathf.Clamp(Mathf.Min(width, height) * 0.08f, 0.05f, 0.11f);
            if (tokenText != null) {
                tokenText.transform.localPosition = new Vector3(0f, -0.02f, 0f);
                tokenText.characterSize = tokenSize;
                tokenText.fontSize = 72;
                tokenText.text = token;
            }

            float tierSize = Mathf.Clamp(tokenSize * 0.72f, 0.04f, 0.08f);
            if (tierText != null) {
                tierText.transform.localPosition = new Vector3(width * 0.24f, height * 0.27f, 0f);
                tierText.characterSize = tierSize;
                tierText.fontSize = 52;
                tierText.anchor = TextAnchor.MiddleCenter;
                tierText.alignment = TextAlignment.Center;
                tierText.text = tier;
            }

            if (foregroundRenderer != null) foregroundRenderer.gameObject.SetActive(false);
            if (beltRenderer != null)
            {
                beltRenderer.sprite = null;
                beltRenderer.gameObject.SetActive(false);
            }
            if (rearForegroundRenderer != null)
            {
                rearForegroundRenderer.sprite = null;
                rearForegroundRenderer.gameObject.SetActive(false);
            }
            if (outlineRenderers != null) {
                for (int i = 0; i < 4; i++) {
                    if (outlineRenderers[i] != null) outlineRenderers[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void SetForegroundActive(bool isActive)
    {
        bool stateChanged = isMachineInUse != isActive;
        isMachineInUse = isActive;

        if (stateChanged)
        {
            currentFrameIndex = 0;
            currentForegroundFrameIndex = 0;
        }

        if (beltRenderer != null && beltAnimationFrames != null && beltAnimationFrames.Length > 0)
        {
            if (stateChanged)
            {
                beltRenderer.sprite = beltAnimationFrames[0];
            }
            ApplyBeltRendererTransform(beltRenderer.sprite);
            beltRenderer.gameObject.SetActive(true);
        }

        bool showForeground = isActive || foregroundVisibleWhenIdle;

        if (rearForegroundRenderer != null && rearForegroundRenderer.sprite != null)
        {
            rearForegroundRenderer.gameObject.SetActive(showForeground);
        }

        if (foregroundRenderer != null && foregroundRenderer.sprite != null)
        {
            if (!isActive && foregroundAnimationFrames != null && foregroundAnimationFrames.Length > 0)
            {
                foregroundRenderer.sprite = foregroundAnimationFrames[0];
            }

            foregroundRenderer.gameObject.SetActive(showForeground);
        }

        if (!isActive && baseAnimationFrames != null && baseAnimationFrames.Length > 0 && baseRenderer != null)
        {
            baseRenderer.sprite = baseAnimationFrames[0];

            if (outlineRenderers != null && outlineRenderers.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (outlineRenderers[i] != null) outlineRenderers[i].sprite = baseAnimationFrames[0];
                }
            }
        }
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
        renderer.sortingOrder = sortingOrder + sortingDepthOffset;
        return renderer;
    }

    private static int GetSortingDepthOffset(PlacedObjectSaveData data)
    {
        if (data == null)
        {
            return 0;
        }

        return GetSortingDepthOffsetForAnchorY(data.anchorY);
    }

    public static int GetSortingDepthOffsetForAnchorY(int anchorY)
    {
        return Mathf.Clamp(MachineDepthSortRange - anchorY, 0, MachineDepthSortRange) * MachineDepthSortStep;
    }

    public static int GetCustomerBodySortingOrder(PlacedObjectSaveData data)
    {
        return GetSortingDepthOffset(data) + CustomerBodyLayerOffset;
    }

    public static int GetCustomerHeadSortingOrder(PlacedObjectSaveData data)
    {
        return GetSortingDepthOffset(data) + CustomerHeadLayerOffset;
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

    private void ApplyBeltRendererTransform(Sprite sprite)
    {
        if (beltRenderer == null || baseRenderer == null)
        {
            return;
        }

        Vector3 scale = baseRenderer.transform.localScale * ExerciseBikePedalScale;
        Vector3 targetHubLocalPosition = baseRenderer.transform.localPosition + new Vector3(ExerciseBikePedalOffsetX, ExerciseBikePedalOffsetY, 0f);
        beltRenderer.transform.localScale = scale;

        if (sprite != null && TryGetExerciseBikePedalFrameIndex(sprite.name, out int frameIndex))
        {
            Vector2 hubPixel = ExerciseBikePedalHubPixels[frameIndex];
            Vector2 hubOffset = (hubPixel - sprite.pivot) / sprite.pixelsPerUnit;
            beltRenderer.transform.localPosition = targetHubLocalPosition - new Vector3(hubOffset.x * scale.x, hubOffset.y * scale.y, 0f);
            return;
        }

        beltRenderer.transform.localPosition = targetHubLocalPosition;
    }

    private static bool TryGetExerciseBikePedalFrameIndex(string spriteName, out int frameIndex)
    {
        frameIndex = -1;
        const string Prefix = "exercise_bike_pedal_";
        if (string.IsNullOrEmpty(spriteName) || !spriteName.StartsWith(Prefix, System.StringComparison.Ordinal))
        {
            return false;
        }

        string suffix = spriteName.Substring(Prefix.Length);
        if (!int.TryParse(suffix, out frameIndex))
        {
            return false;
        }

        return frameIndex >= 0 && frameIndex < ExerciseBikePedalHubPixels.Length;
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
