using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MockupUiSprites
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Load(string resourcePath)
    {
        if (Cache.TryGetValue(resourcePath, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogWarning($"[MockupUiSprites] Missing mockup texture: Resources/{resourcePath}");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        Cache[resourcePath] = sprite;
        return sprite;
    }

    public static bool Assign(Image image, string resourcePath, bool preserveAspect = false)
    {
        if (image == null)
        {
            return false;
        }

        Sprite sprite = Load(resourcePath);
        if (sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
        return true;
    }
}
