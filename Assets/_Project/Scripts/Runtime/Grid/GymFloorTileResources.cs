using UnityEngine;

public static class GymFloorTileResources
{
    public const string BaseWarmPath = "GeneratedRuntimeUI/ui_v2/tiles/gym_floor/warm_theme/gym_floor_tile_base_warm_final";
    public const string BorderSideWarmPath = "GeneratedRuntimeUI/ui_v2/tiles/gym_floor/warm_theme/gym_floor_wall_side_final";
    public const string BorderCornerWarmPath = "GeneratedRuntimeUI/ui_v2/tiles/gym_floor/warm_theme/gym_floor_wall_corner_square_final";

    // New top-view tileset (repurposed from previous quarter-view experiment)
    public const string NewTopViewFloorTilesetPath = "GeneratedRuntimeUI/building/floor/gym_floor_tileset_quarterview_v1";
    public const string DefaultTopViewFloorTileName = "floor_wood_plank_a";

    public static Sprite LoadBaseWarmSprite()
    {
        return LoadSprite(BaseWarmPath);
    }

    public static Sprite LoadBorderSideWarmSprite()
    {
        return LoadSprite(BorderSideWarmPath);
    }

    public static Sprite LoadBorderCornerWarmSprite()
    {
        return LoadSprite(BorderCornerWarmPath);
    }

    /// <summary>
    /// Loads a floor tile from the new top-view tileset (gym_floor_tileset_quarterview_v1.png).
    /// Default is floor_wood_plank_a for a clean natural beige/plank look.
    /// </summary>
    public static Sprite LoadNewTopViewFloorSprite(string spriteName = null)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            spriteName = DefaultTopViewFloorTileName;
        }
        return LoadSpriteByName(NewTopViewFloorTilesetPath, spriteName);
    }

    private static Sprite LoadSprite(string resourcePath)
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

        Debug.LogWarning($"[GymFloorTileResources] Sprite resource not found: {resourcePath}");
        return null;
    }

    public static Sprite LoadSpriteByName(string resourcePath, string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
        }

        Debug.LogWarning($"[GymFloorTileResources] Sprite resource not found: {resourcePath}/{spriteName}");
        return null;
    }

    private static Sprite FindSpriteByName(Sprite[] sprites, string spriteName)
    {
        if (sprites == null)
        {
            return null;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }

    private static string BuildSpriteRectSummary(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return "<none>";
        }

        string[] parts = new string[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            parts[i] = sprite != null ? $"{sprite.name} {FormatSpriteRect(sprite)}" : "<null>";
        }

        return string.Join("; ", parts);
    }

    private static string FormatSpriteRect(Sprite sprite)
    {
        if (sprite == null)
        {
            return "rect=<missing>";
        }

        Rect rect = sprite.textureRect;
        return $"rect=x={rect.x:0.#}, y={rect.y:0.#}, w={rect.width:0.#}, h={rect.height:0.#}";
    }

    private static Sprite FindFirstBasicBeigeSprite(Sprite[] sprites)
    {
        if (sprites == null)
        {
            return null;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            string spriteName = sprite.name;
            if (spriteName.StartsWith("floor_beige_") && !spriteName.Contains("transition"))
            {
                return sprite;
            }
        }

        return null;
    }
}
