using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GeneratedRuntimeSprites
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    private static readonly HashSet<string> MissingPaths = new HashSet<string>();

    public static void ClearCache()
    {
        Cache.Clear();
        MissingPaths.Clear();
    }

    public static void ClearCache(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            ClearCache();
            return;
        }

        Cache.Remove(resourcePath);
        MissingPaths.Remove(resourcePath);
    }

    public static Sprite Load(string resourcePath, float pixelsPerUnit = 100f)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (Cache.TryGetValue(resourcePath, out Sprite cached))
        {
            if (IsUsableSprite(cached))
            {
                return cached;
            }

            Cache.Remove(resourcePath);
        }

        // 먼저 Sprite 에셋을 직접 로드한다.
        // 이전 구현처럼 Texture2D에서 Sprite.Create로 만든 런타임 Sprite는 씬에 안정적으로 직렬화되지 않아
        // Play 전 편집 모드에서 Image.sprite가 비는 문제가 생길 수 있다.
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                sprite = sprites[0];
            }
        }

#if UNITY_EDITOR
        if (sprite == null && !Application.isPlaying)
        {
            sprite = LoadEditorSprite(resourcePath);
        }
#endif

        if (sprite != null && IsUsableSprite(sprite))
        {
            ConfigureTexture(sprite.texture);
            Cache[resourcePath] = sprite;
            return sprite;
        }

        // 예외적으로 Sprite importer가 아닌 Texture만 있는 경우를 위한 런타임 fallback.
        // 이 fallback은 Play 중 표시용이며, 편집 모드 저장 안정성 기준은 위의 Sprite 에셋 로드다.
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            if (MissingPaths.Add(resourcePath))
            {
                Debug.LogWarning($"[GeneratedRuntimeSprites] Missing generated sprite resource: Resources/{resourcePath}");
            }

            return null;
        }

        ConfigureTexture(texture);
        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            GetBorder(resourcePath, texture));

        Cache[resourcePath] = sprite;
        return sprite;
    }

    public static bool Assign(Image image, string resourcePath, bool preserveAspect = true)
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

        Image.Type targetType = IsUiResource(resourcePath) && !preserveAspect ? Image.Type.Sliced : Image.Type.Simple;
        bool changed = image.sprite != sprite ||
                       image.type != targetType ||
                       image.preserveAspect != preserveAspect ||
                       image.color != Color.white;

        image.sprite = sprite;
        image.type = targetType;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;

#if UNITY_EDITOR
        if (changed && !Application.isPlaying)
        {
            EditorUtility.SetDirty(image);
        }
#endif

        return true;
    }

    public static bool Assign(SpriteRenderer renderer, string resourcePath, bool preserveAspect = true)
    {
        if (renderer == null)
        {
            return false;
        }

        Sprite sprite = Load(resourcePath);
        if (sprite == null)
        {
            return false;
        }

        SpriteDrawMode targetDrawMode = preserveAspect ? SpriteDrawMode.Simple : SpriteDrawMode.Sliced;
        bool changed = renderer.sprite != sprite ||
                       renderer.drawMode != targetDrawMode ||
                       renderer.color != Color.white;

        renderer.sprite = sprite;
        renderer.drawMode = targetDrawMode;
        renderer.color = Color.white;

#if UNITY_EDITOR
        if (changed && !Application.isPlaying)
        {
            EditorUtility.SetDirty(renderer);
        }
#endif

        return true;
    }


#if UNITY_EDITOR
    private static Sprite LoadEditorSprite(string resourcePath)
    {
        string[] candidates =
        {
            $"Assets/_Project/Resources/{resourcePath}.png",
            $"Assets/_Project/Resources/{resourcePath}.jpg",
            $"Assets/_Project/Resources/{resourcePath}.jpeg"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string assetPath = candidates[i];
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int j = 0; j < assets.Length; j++)
            {
                if (assets[j] is Sprite nestedSprite)
                {
                    return nestedSprite;
                }
            }
        }

        return null;
    }

#endif

    private static void ConfigureTexture(Texture texture)
    {
        if (texture == null)
        {
            return;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    private static Vector4 GetBorder(string resourcePath, Texture2D texture)
    {
        if (!IsUiResource(resourcePath) || texture == null)
        {
            return Vector4.zero;
        }

        if (TryGetFixedBorder(resourcePath, out float fixedBorder))
        {
            return new Vector4(fixedBorder, fixedBorder, fixedBorder, fixedBorder);
        }

        float border = Mathf.Clamp(Mathf.Min(texture.width, texture.height) * 0.18f, 8f, 44f);
        return new Vector4(border, border, border, border);
    }

    private static bool TryGetFixedBorder(string resourcePath, out float border)
    {
        border = 0f;
        if (string.IsNullOrEmpty(resourcePath))
        {
            return false;
        }

        string normalized = resourcePath.Replace('\\', '/').ToLowerInvariant();
        if (!normalized.Contains("/ui_v2/review/"))
        {
            return false;
        }

        if (normalized.EndsWith("/review_list_item_base") ||
            normalized.EndsWith("/review_summary_card_base"))
        {
            border = 18f;
            return true;
        }

        if (normalized.EndsWith("/review_filter_button_active") ||
            normalized.EndsWith("/review_filter_button_inactive"))
        {
            border = 12f;
            return true;
        }

        return false;
    }

    private static bool IsUsableSprite(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return false;
        }

        Rect rect = sprite.rect;
        return rect.width > 0f &&
               rect.height > 0f &&
               rect.xMin >= -0.5f &&
               rect.yMin >= -0.5f &&
               rect.xMax <= sprite.texture.width + 0.5f &&
               rect.yMax <= sprite.texture.height + 0.5f;
    }

    private static bool IsUiResource(string resourcePath)
    {
        return !string.IsNullOrEmpty(resourcePath) &&
               (resourcePath.Contains("/ui/") || resourcePath.Contains("/ui_v2/"));
    }
}
