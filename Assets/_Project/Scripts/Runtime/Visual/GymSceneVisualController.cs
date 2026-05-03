using UnityEngine;

[DefaultExecutionOrder(-8500)]
public sealed class GymSceneVisualController : MonoBehaviour
{
    private const string VisualRootName = "GymWorldVisuals";

    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private bool showLegacyMockupRoom = false;

    private static Sprite cachedWhiteSprite;
    private static Font cachedFont;

    private string lastSignature = string.Empty;

    private void OnEnable()
    {
        RefreshIfNeeded(true);
    }

    private void Update()
    {
        RefreshIfNeeded(false);
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        Transform root = FindVisualRoot();
        if (root != null)
        {
            DestroyImmediate(root.gameObject);
        }

        lastSignature = string.Empty;
    }

    private void RefreshIfNeeded(bool force)
    {
        ResolveReferences();
        if (gridManager == null || gridRoot == null)
        {
            return;
        }

        string signature = string.Format(
            "{0}|{1}|{2:F3}|{3}",
            gridManager.Width,
            gridManager.Height,
            gridManager.CellSize,
            BuildPlayModeManager.IsBuildMode);

        if (!force && signature == lastSignature)
        {
            ApplyCamera();
            return;
        }

        EnsureAssets();
        BuildVisuals();
        ApplyCamera();
        lastSignature = signature;
    }

    private void ResolveReferences()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (placementManager == null)
        {
            placementManager = FindFirstObjectByType<PlacementManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (gridRoot == null)
        {
            GameObject found = GameObject.Find("GridRoot");
            if (found != null)
            {
                gridRoot = found.transform;
            }
        }
    }

    private void ApplyCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.backgroundColor = BuildPlayModeManager.IsBuildMode
            ? new Color(0.18f, 0.22f, 0.16f, 1f)
            : new Color(0.15f, 0.20f, 0.15f, 1f);
    }

    private void BuildVisuals()
    {
        Transform root = EnsureVisualRoot();
        ClearChildren(root);

        if (!showLegacyMockupRoom)
        {
            return;
        }

        float gridWidth = gridManager.Width * gridManager.CellSize;
        float gridHeight = gridManager.Height * gridManager.CellSize;
        float cell = gridManager.CellSize;

        CreateBlock(root, "RoomBackdrop", Vector3.zero, new Vector2(gridWidth + 6.8f, gridHeight + 10.0f), new Color(0.17f, 0.23f, 0.15f, 1f), -40);
        CreateBlock(root, "RoomGlow", new Vector3(0f, 0.35f, 0f), new Vector2(gridWidth + 5.4f, gridHeight + 8.4f), new Color(0.62f, 0.52f, 0.36f, 1f), -38);

        CreateBlock(root, "UpperWall", new Vector3(0f, (gridHeight * 0.5f) + 1.55f, 0f), new Vector2(gridWidth + 4.8f, 2.7f), new Color(0.69f, 0.58f, 0.42f, 1f), -36);
        CreateBlock(root, "UpperGlow", new Vector3(0f, (gridHeight * 0.5f) + 1.86f, 0f), new Vector2(gridWidth + 3.2f, 0.20f), new Color(0.97f, 0.76f, 0.34f, 0.28f), -35);
        CreateBlock(root, "LowerDeck", new Vector3(0f, (-gridHeight * 0.5f) - 1.25f, 0f), new Vector2(gridWidth + 5.2f, 1.7f), new Color(0.34f, 0.31f, 0.25f, 1f), -36);

        CreateBlock(root, "FloorShell", Vector3.zero, new Vector2(gridWidth + 1.8f, gridHeight + 1.6f), new Color(0.24f, 0.17f, 0.11f, 1f), -28);
        CreateBlock(root, "FloorBorder", Vector3.zero, new Vector2(gridWidth + 1.22f, gridHeight + 1.04f), new Color(0.60f, 0.43f, 0.24f, 1f), -26);
        CreateBlock(root, "FloorInset", Vector3.zero, new Vector2(gridWidth + 0.42f, gridHeight + 0.32f), new Color(0.70f, 0.54f, 0.34f, 1f), -24);

        for (int y = 0; y < gridManager.Height; y++)
        {
            float yPosition = (-gridHeight * 0.5f) + (cell * 0.5f) + (y * cell);
            bool laneRow = (y % 4) == 0 || (y % 4) == 3;
            Color rowColor = laneRow
                ? new Color(0.74f, 0.58f, 0.35f, 1f)
                : new Color(0.67f, 0.50f, 0.29f, 1f);

            CreateBlock(root, $"Row_{y}", new Vector3(0f, yPosition, 0f), new Vector2(gridWidth + 0.08f, cell * 0.98f), rowColor, -18);

            if (laneRow)
            {
                CreateBlock(root, $"LaneStripe_{y}", new Vector3(0f, yPosition + (cell * 0.27f), 0f), new Vector2(gridWidth - 0.25f, Mathf.Max(0.04f, cell * 0.08f)), new Color(0.98f, 0.84f, 0.46f, 0.22f), -17);
            }
        }

        BuildRuntimeGridTiles(root, gridWidth, gridHeight, cell);

        CreateBlock(root, "LeftRail", new Vector3((-gridWidth * 0.5f) - 0.76f, 0f, 0f), new Vector2(0.20f, gridHeight + 0.92f), new Color(0.24f, 0.23f, 0.19f, 1f), -14);
        CreateBlock(root, "RightRail", new Vector3((gridWidth * 0.5f) + 0.76f, 0f, 0f), new Vector2(0.20f, gridHeight + 0.92f), new Color(0.24f, 0.23f, 0.19f, 1f), -14);
        CreateBlock(root, "TopMarker", new Vector3(0f, (gridHeight * 0.5f) + 1.52f, 0f), new Vector2(2.6f, 0.48f), new Color(0.93f, 0.76f, 0.31f, 0.95f), -15);

        CreateBlock(root, "TopCap", new Vector3(0f, (gridHeight * 0.5f) + 1.46f, 0f), new Vector2(3.2f, 0.18f), new Color(0.25f, 0.17f, 0.10f, 0.92f), -13);
        CreateBlock(root, "BottomCap", new Vector3(0f, (-gridHeight * 0.5f) - 1.18f, 0f), new Vector2(4.6f, 0.16f), new Color(0.93f, 0.87f, 0.68f, 0.32f), -13);
    }

    private void BuildRuntimeGridTiles(Transform root, float gridWidth, float gridHeight, float cell)
    {
        int maxColumns = Mathf.Min(gridManager.Width, 12);
        int maxRows = Mathf.Min(gridManager.Height, 12);
        for (int y = 0; y < maxRows; y++)
        {
            for (int x = 0; x < maxColumns; x++)
            {
                float xPosition = (-gridWidth * 0.5f) + (cell * 0.5f) + (x * cell);
                float yPosition = (-gridHeight * 0.5f) + (cell * 0.5f) + (y * cell);
                bool alternate = ((x + y) % 2) == 0;
                Color tileColor = alternate
                    ? new Color(0.76f, 0.57f, 0.33f, 1f)
                    : new Color(0.68f, 0.49f, 0.29f, 1f);
                CreateBlock(root, $"RuntimeGridTile_{x}_{y}", new Vector3(xPosition, yPosition, 0f), new Vector2(cell * 0.92f, cell * 0.92f), tileColor, -16);
                CreateBlock(root, $"RuntimeGridTileEdge_{x}_{y}", new Vector3(xPosition, yPosition - (cell * 0.44f), 0f), new Vector2(cell * 0.92f, Mathf.Max(0.02f, cell * 0.035f)), new Color(0.34f, 0.23f, 0.14f, 0.38f), -15);
            }
        }
    }

    private Transform EnsureVisualRoot()
    {
        Transform existing = FindVisualRoot();
        if (existing != null)
        {
            existing.gameObject.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;
            return existing;
        }

        Transform parent = gridRoot != null && gridRoot.parent != null
            ? gridRoot.parent
            : transform;

        GameObject rootObject = new GameObject(VisualRootName);
        rootObject.transform.SetParent(parent, false);
        rootObject.transform.SetAsFirstSibling();
        rootObject.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;
        return rootObject.transform;
    }

    private Transform FindVisualRoot()
    {
        Transform parent = gridRoot != null && gridRoot.parent != null
            ? gridRoot.parent
            : transform;

        return parent.Find(VisualRootName);
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
            else
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }
    }

    private static void EnsureAssets()
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

    private static SpriteRenderer CreateBlock(Transform parent, string name, Vector3 localPosition, Vector2 size, Color color, int sortingOrder)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;

        SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
        renderer.sprite = cachedWhiteSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void CreateWorldLabel(Transform parent, string name, Vector3 localPosition, string text, Color color, int sortingOrder, float characterSize, FontStyle style)
    {
        GameObject node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;

        TextMesh textMesh = node.AddComponent<TextMesh>();
        textMesh.font = cachedFont;
        textMesh.text = text;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = style;
        textMesh.fontSize = 72;
        textMesh.characterSize = characterSize;

        MeshRenderer renderer = node.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
        }
    }
}
