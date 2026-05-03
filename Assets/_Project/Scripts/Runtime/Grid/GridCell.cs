using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class GridCell : MonoBehaviour
{
    private static Sprite cachedWhiteSprite;

    private int x;
    private int y;
    private float cellSize;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    private readonly Color hoverColor = new Color(0.22f, 0.86f, 0.96f, 0.82f);
    private readonly Color occupiedColor = new Color(0.20f, 0.25f, 0.34f, 0.68f);

    private bool isHovered = false;
    private bool isOccupied = false;

    public int X => x;
    public int Y => y;
    public bool IsOccupied => isOccupied;

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

        EnsureWhiteSprite();

        spriteRenderer.sprite = cachedWhiteSprite;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(cellSize * 0.92f, cellSize * 0.92f);
        spriteRenderer.sortingOrder = -4;

        transform.localScale = Vector3.one;

        boxCollider.size = new Vector2(cellSize * 0.92f, cellSize * 0.92f);
        boxCollider.offset = Vector2.zero;
        boxCollider.isTrigger = false;

        RefreshVisual();
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        RefreshVisual();
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (isOccupied)
        {
            spriteRenderer.color = occupiedColor;
        }
        else if (isHovered && BuildPlayModeManager.IsBuildMode)
        {
            spriteRenderer.color = hoverColor;
        }
        else
        {
            spriteRenderer.color = GetIdleColor();
        }
    }

    private Color GetIdleColor()
    {
        bool laneRow = (y % 4) == 0 || (y % 4) == 3;
        bool checker = ((x + y) & 1) == 0;

        if (laneRow)
        {
            return checker
                ? new Color(0.28f, 0.33f, 0.42f, 0.78f)
                : new Color(0.24f, 0.29f, 0.37f, 0.78f);
        }

        return checker
            ? new Color(0.18f, 0.22f, 0.30f, 0.84f)
            : new Color(0.15f, 0.19f, 0.27f, 0.84f);
    }

    private void EnsureWhiteSprite()
    {
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
}
