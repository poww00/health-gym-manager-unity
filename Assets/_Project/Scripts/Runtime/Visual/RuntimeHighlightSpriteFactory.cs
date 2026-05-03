using UnityEngine;

public static class RuntimeHighlightSpriteFactory
{
    private static Sprite softRoundedOutlineSprite;

    public static Sprite GetSoftRoundedOutlineSprite()
    {
        if (softRoundedOutlineSprite != null)
        {
            return softRoundedOutlineSprite;
        }

        const int size = 96;
        const float margin = 20f;
        const float radius = 22f;
        const float border = 3.5f;
        const float outerGlow = 20f;
        const float innerGlow = 12f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 outerHalf = new Vector2(size * 0.5f - margin, size * 0.5f - margin);
        Vector2 innerHalf = new Vector2(outerHalf.x - border, outerHalf.y - border);
        float innerRadius = Mathf.Max(1f, radius - border);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float outerDistance = RoundedRectDistance(p, outerHalf, radius);
                float innerDistance = RoundedRectDistance(p, innerHalf, innerRadius);

                float edge = outerDistance <= 0f && innerDistance >= 0f
                    ? 0.88f
                    : 0f;
                float outside = outerDistance > 0f && outerDistance < outerGlow
                    ? Mathf.Lerp(0.46f, 0f, Mathf.SmoothStep(0f, 1f, outerDistance / outerGlow))
                    : 0f;
                float inside = innerDistance < 0f && innerDistance > -innerGlow
                    ? Mathf.Lerp(0.24f, 0f, Mathf.SmoothStep(0f, 1f, -innerDistance / innerGlow))
                    : 0f;

                float alpha = Mathf.Clamp01(Mathf.Max(edge, outside, inside));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);

        softRoundedOutlineSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(32f, 32f, 32f, 32f));

        return softRoundedOutlineSprite;
    }

    private static float RoundedRectDistance(Vector2 p, Vector2 halfSize, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halfSize - new Vector2(radius, radius));
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
    }
}
