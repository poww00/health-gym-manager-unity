using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class GridCell : MonoBehaviour
{
    private static Sprite cachedWhiteSprite;
    private static Sprite cachedWarmBaseSprite;

    /// <summary>
    /// When true, GridCells will use Tiled draw mode + exact cell size for clean repeating floor tiles
    /// from the new top-view tileset (prevents stretching of non-square atlas sprites).
    /// </summary>
    public static bool UsingNewTopViewFloorTiles { get; set; } = false;
    public static bool SuppressBuildHoverVisual { get; set; } = false;

    /// <summary>
    /// Allows overriding the default floor tile sprite at runtime (used for switching to new tileset).
    /// Sets the flag so Initialize() uses proper Tiled mode for clean top-view repeating tiles.
    /// </summary>
    public static void SetDefaultFloorSprite(Sprite sprite)
    {
        if (sprite != null)
        {
            cachedWarmBaseSprite = sprite;
            UsingNewTopViewFloorTiles = true; // Enable clean tiled mode for new atlas sprites
        }
    }

    private int x;
    private int y;
    private float cellSize;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    private readonly Color hoverColor = new Color(0.22f, 0.86f, 0.96f, 0.82f);
    private readonly Color occupiedColor = new Color(0.20f, 0.25f, 0.34f, 0.68f);

    private bool isHovered = false;
    private bool isOccupied = false;
    private bool isFixedOccupied = false;

    public int X => x;
    public int Y => y;
    public bool IsOccupied => isOccupied || isFixedOccupied;
    public bool IsFixedOccupied => isFixedOccupied;

    public void Initialize(int gridX, int gridY, float size)
    {
        x = gridX;
        y = gridY;
        cellSize = size;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        EnsureSprites();

        spriteRenderer.sprite = cachedWarmBaseSprite != null ? cachedWarmBaseSprite : cachedWhiteSprite;
        spriteRenderer.sortingOrder = -4;

        if (UsingNewTopViewFloorTiles && cachedWarmBaseSprite != null)
        {
            // Pure top-view: Force Simple + 1:1 pixel perfect (사용자 지시)
            spriteRenderer.sprite = cachedWarmBaseSprite;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            spriteRenderer.sortingOrder = -4;

            // 1:1 pixel perfect 정확한 공식 + scale=0 방지 (사용자 지시)
            float w = cachedWarmBaseSprite.rect.width;
            float h = cachedWarmBaseSprite.rect.height;

            if (w < 0.1f) w = cellSize;
            if (h < 0.1f) h = cellSize;

            float scaleX = cellSize / w;
            float scaleY = cellSize / h;

            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Collider는 cell 단위로 유지
            boxCollider.size = new Vector2(cellSize * 0.92f, cellSize * 0.92f);
            boxCollider.offset = Vector2.zero;
        }
        else
        {
            // Legacy warm floor path (preserved for stability)
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            float spriteScale = CalculateCellSpriteScale(spriteRenderer.sprite, cellSize);
            transform.localScale = new Vector3(spriteScale, spriteScale, 1f);

            float inverseSpriteScale = spriteScale > 0f ? 1f / spriteScale : 1f;
            boxCollider.size = new Vector2(cellSize * 0.92f * inverseSpriteScale, cellSize * 0.92f * inverseSpriteScale);
            boxCollider.offset = Vector2.zero;
        }

        boxCollider.isTrigger = false;
        RefreshVisual();
    }

    public void SetHovered(bool hovered)
    {
        if (hovered && !BuildPlayModeManager.IsBuildMode)
        {
            if (!isHovered)
            {
                return;
            }

            isHovered = false;
            RefreshVisual();
            return;
        }

        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;
        RefreshVisual();
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        RefreshVisual();
    }

    public void SetFixedOccupied(bool occupied)
    {
        isFixedOccupied = occupied;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // Placement preview visuals are owned by PlacementManager. A GridCell should
        // never tint itself during normal play/operation hover.
        spriteRenderer.color = GetIdleColor();
    }

    private Color GetIdleColor()
    {
        return Color.white;
    }

    private void EnsureSprites()
    {
        if (cachedWarmBaseSprite == null)
        {
            cachedWarmBaseSprite = GymFloorTileResources.LoadBaseWarmSprite();
        }

        if (cachedWhiteSprite != null)
        {
            return;
        }

        Texture2D texture = Texture2D.whiteTexture;

        cachedWhiteSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private static float CalculateCellSpriteScale(Sprite sprite, float targetSize)
    {
        if (sprite == null)
        {
            return 1f;
        }

        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        return spriteSize > 0f ? targetSize / spriteSize : 1f;
    }
}
