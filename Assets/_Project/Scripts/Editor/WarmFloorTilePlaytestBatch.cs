using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WarmFloorTilePlaytestBatch
{
    private const string ScenePath = "Assets/_Project/Scenes/TestSandbox.unity";
    private const string ScreenshotPath = "Logs/WarmFloorTilePlaytest.png";
    private const string PendingRunKey = "WarmFloorTilePlaytestBatch.PendingRun";

    private static int updateCount;
    private static bool foundConsoleFailure;
    private static string consoleFailureMessage = string.Empty;

    [InitializeOnLoadMethod]
    private static void ResumePendingRun()
    {
        if (!EditorPrefs.GetBool(PendingRunKey, false))
        {
            return;
        }

        Application.logMessageReceived -= OnLogMessageReceived;
        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Debug.Log("[WarmFloorTilePlaytestBatch] Resume pending playtest");
    }

    [MenuItem("_Project/UI/Verify Warm Floor Tile Playtest")]
    public static void VerifyTestSandbox()
    {
        try
        {
            updateCount = 0;
            foundConsoleFailure = false;
            consoleFailureMessage = string.Empty;

            Debug.Log("[WarmFloorTilePlaytestBatch] Start");
            EditorPrefs.SetBool(PendingRunKey, true);
            AssetDatabase.Refresh();
            string baseAssetPath = "Assets/_Project/Resources/" + GymFloorTileResources.BaseWarmPath + ".png";
            string sideAssetPath = "Assets/_Project/Resources/" + GymFloorTileResources.BorderSideWarmPath + ".png";
            string cornerAssetPath = "Assets/_Project/Resources/" + GymFloorTileResources.BorderCornerWarmPath + ".png";

            NormalizeAndVerifyWarmTileImporter(baseAssetPath, false, null);
            float warmPixelsPerUnit = GetWarmTilePixelsPerUnit(baseAssetPath);
            NormalizeAndVerifyWarmTileImporter(sideAssetPath, true, warmPixelsPerUnit);
            NormalizeAndVerifyWarmTileImporter(cornerAssetPath, true, warmPixelsPerUnit);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        updateCount++;
        if (updateCount < 90)
        {
            return;
        }

        EditorApplication.update -= Tick;

        try
        {
            RunPlayModeChecks();
            EditorPrefs.DeleteKey(PendingRunKey);
            Application.logMessageReceived -= OnLogMessageReceived;
            Debug.Log("[WarmFloorTilePlaytestBatch] PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            Fail(exception.Message);
        }
    }

    private static void RunPlayModeChecks()
    {
        Sprite baseSprite = RequireSprite(GymFloorTileResources.BaseWarmPath, "base");
        Sprite sideSprite = RequireSprite(GymFloorTileResources.BorderSideWarmPath, "side");
        Sprite cornerSprite = RequireSprite(GymFloorTileResources.BorderCornerWarmPath, "corner");

        Transform gridRoot = GameObject.Find("GridRoot")?.transform;
        if (gridRoot == null)
        {
            throw new InvalidOperationException("GridRoot was not found in TestSandbox play mode.");
        }

        Transform borderRoot = gridRoot.Find("WarmFloorBorder");
        if (borderRoot == null)
        {
            throw new InvalidOperationException("WarmFloorBorder was not generated.");
        }

        GridManager gridManager = Object.FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            throw new InvalidOperationException("GridManager was not found in TestSandbox play mode.");
        }

        Transform firstCell = gridRoot.Find("Cell_0_0");
        if (firstCell == null)
        {
            throw new InvalidOperationException("Cell_0_0 was not generated.");
        }

        VerifyTileRenderer(firstCell, baseSprite, CalculateWarmFloorTileScale(baseSprite, gridManager.CellSize), "Base");

        int sideCount = 0;
        int cornerCount = 0;
        SpriteRenderer[] renderers = borderRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Sprite sprite = renderers[i].sprite;
            if (sprite == null)
            {
                continue;
            }

            if (sprite.name == sideSprite.name)
            {
                sideCount++;
            }
            else if (sprite.name == cornerSprite.name)
            {
                cornerCount++;
            }
        }

        float sideScale = CalculateWarmFloorTileScale(sideSprite, gridManager.CellSize);
        float cornerScale = CalculateWarmFloorTileScale(cornerSprite, gridManager.CellSize);
        int expectedSideCount = (gridManager.Width * 2) + (gridManager.Height * 2);
        if (sideCount != expectedSideCount || cornerCount != 4)
        {
            throw new InvalidOperationException($"Unexpected warm border sprites. sideCount={sideCount}/{expectedSideCount}, cornerCount={cornerCount}/4");
        }

        if (borderRoot.Find("WarmFloorBackplate") != null)
        {
            throw new InvalidOperationException("WarmFloorBackplate should not be generated for wall mockup floor tiles.");
        }

        VerifySideRun(borderRoot, "WarmFloorBorderTop", sideSprite, 0f, sideScale, gridManager.Width);
        VerifySideRun(borderRoot, "WarmFloorBorderBottom", sideSprite, 180f, sideScale, gridManager.Width);
        VerifySideRun(borderRoot, "WarmFloorBorderLeft", sideSprite, 90f, sideScale, gridManager.Height);
        VerifySideRun(borderRoot, "WarmFloorBorderRight", sideSprite, -90f, sideScale, gridManager.Height);

        VerifyBorderTile(RequireChild(borderRoot, "WarmFloorBorderTopLeft"), cornerSprite, cornerScale, 0f, "Corner");
        VerifyBorderTile(RequireChild(borderRoot, "WarmFloorBorderTopRight"), cornerSprite, cornerScale, 0f, "Corner");
        VerifyBorderTile(RequireChild(borderRoot, "WarmFloorBorderBottomRight"), cornerSprite, cornerScale, 0f, "Corner");
        VerifyBorderTile(RequireChild(borderRoot, "WarmFloorBorderBottomLeft"), cornerSprite, cornerScale, 0f, "Corner");

        VerifyVisualContacts(borderRoot, gridManager);

        CaptureWarmFloorOverviewScreenshot(ScreenshotPath, gridManager);

        if (foundConsoleFailure)
        {
            throw new InvalidOperationException(consoleFailureMessage);
        }
    }

    private static Transform RequireChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            throw new InvalidOperationException("Missing expected warm floor tile: " + childName);
        }

        return child;
    }

    private static void VerifySideRun(
        Transform borderRoot,
        string namePrefix,
        Sprite expectedSprite,
        float expectedRotationZ,
        float expectedScale,
        int expectedCount)
    {
        for (int i = 0; i < expectedCount; i++)
        {
            VerifyBorderTile(RequireChild(borderRoot, namePrefix + "_" + i), expectedSprite, expectedScale, expectedRotationZ, "Side");
        }
    }

    private static void VerifyTileRenderer(Transform tile, Sprite expectedSprite, float expectedScale, string label)
    {
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            throw new InvalidOperationException(label + " tile is missing SpriteRenderer: " + tile.name);
        }

        if (renderer.sprite == null || renderer.sprite.name != expectedSprite.name)
        {
            string actual = renderer.sprite != null ? renderer.sprite.name : "null";
            throw new InvalidOperationException($"{label} tile sprite mismatch: {tile.name}, actual={actual}");
        }

        if (renderer.drawMode != SpriteDrawMode.Simple)
        {
            throw new InvalidOperationException($"{label} tile must use Simple draw mode: {tile.name}, actual={renderer.drawMode}");
        }

        Vector3 localScale = tile.localScale;
        if (Mathf.Abs(localScale.x - localScale.y) > 0.001f)
        {
            throw new InvalidOperationException($"{label} tile scale is not uniform: {tile.name}, scale={localScale}");
        }

        if (Mathf.Abs(localScale.x - expectedScale) > 0.015f)
        {
            throw new InvalidOperationException($"{label} tile scale mismatch: {tile.name}, actual={localScale.x:0.###}, expected={expectedScale:0.###}");
        }
    }

    private static void VerifyBorderTile(Transform tile, Sprite expectedSprite, float expectedScale, float expectedRotationZ, string label)
    {
        VerifyTileRenderer(tile, expectedSprite, expectedScale, label);
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        Vector3 localScale = tile.localScale;

        float rotationDelta = Mathf.Abs(Mathf.DeltaAngle(tile.localEulerAngles.z, expectedRotationZ));
        if (rotationDelta > 0.01f)
        {
            throw new InvalidOperationException($"{label} tile rotation mismatch: {tile.name}, actual={tile.localEulerAngles.z:0.###}, expected={expectedRotationZ:0.###}");
        }

        VisibleWorldBounds visibleBounds = GetVisibleWorldBounds(renderer);
        Vector3 position = tile.localPosition;
        Debug.Log($"[WarmFloorTilePlaytestBatch] {label} {tile.name}: drawMode={renderer.drawMode}, scale={localScale.x:0.###}x{localScale.y:0.###}, visible={visibleBounds}, pos={position.x:0.###},{position.y:0.###}, rotation={tile.localEulerAngles.z:0.###}");
    }

    private static void VerifyVisualContacts(Transform borderRoot, GridManager gridManager)
    {
        VisibleWorldBounds baseBounds = GetBaseVisibleBounds(gridManager);
        VerifyHorizontalSideContacts(borderRoot, gridManager, baseBounds, "WarmFloorBorderTop", true);
        VerifyHorizontalSideContacts(borderRoot, gridManager, baseBounds, "WarmFloorBorderBottom", false);
        VerifyVerticalSideContacts(borderRoot, gridManager, baseBounds, "WarmFloorBorderLeft", true);
        VerifyVerticalSideContacts(borderRoot, gridManager, baseBounds, "WarmFloorBorderRight", false);
        VerifyCornerContacts(borderRoot, gridManager, baseBounds);
    }

    private static void VerifyHorizontalSideContacts(Transform borderRoot, GridManager gridManager, VisibleWorldBounds baseBounds, string prefix, bool top)
    {
        VisibleWorldBounds previous = default;
        float allowedBaseOverlap = GetAllowedHorizontalSideBaseOverlap(borderRoot, prefix, top, gridManager.Width);
        for (int x = 0; x < gridManager.Width; x++)
        {
            VisibleWorldBounds side = GetVisibleWorldBounds(RequireChild(borderRoot, prefix + "_" + x));
            VisibleWorldBounds baseCell = GetVisibleWorldBounds(RequireCell(gridManager, x, top ? gridManager.Height - 1 : 0));

            if (top)
            {
                AssertNoVerticalGapOrExcessOverlap(side.MinY, baseBounds.MaxY, true, allowedBaseOverlap, $"{prefix}_{x} bottom to baseTop");
            }
            else
            {
                AssertNoVerticalGapOrExcessOverlap(side.MaxY, baseBounds.MinY, false, allowedBaseOverlap, $"{prefix}_{x} top to baseBottom");
            }

            AssertEdgeContact(side.MinX, baseCell.MinX, $"{prefix}_{x} left to base cell left");
            AssertEdgeContact(side.MaxX, baseCell.MaxX, $"{prefix}_{x} right to base cell right");

            if (x > 0)
            {
                AssertEdgeContact(side.MinX, previous.MaxX, $"{prefix}_{x - 1}/{x} side-to-side");
            }

            previous = side;
        }
    }

    private static void VerifyVerticalSideContacts(Transform borderRoot, GridManager gridManager, VisibleWorldBounds baseBounds, string prefix, bool left)
    {
        VisibleWorldBounds previous = default;
        float allowedBaseOverlap = GetAllowedVerticalSideBaseOverlap(borderRoot, prefix, left, gridManager.Height);
        for (int y = 0; y < gridManager.Height; y++)
        {
            VisibleWorldBounds side = GetVisibleWorldBounds(RequireChild(borderRoot, prefix + "_" + y));
            VisibleWorldBounds baseCell = GetVisibleWorldBounds(RequireCell(gridManager, left ? 0 : gridManager.Width - 1, y));

            if (left)
            {
                AssertNoHorizontalGapOrExcessOverlap(side.MaxX, baseBounds.MinX, true, allowedBaseOverlap, $"{prefix}_{y} right to baseLeft");
            }
            else
            {
                AssertNoHorizontalGapOrExcessOverlap(side.MinX, baseBounds.MaxX, false, allowedBaseOverlap, $"{prefix}_{y} left to baseRight");
            }

            AssertEdgeContact(side.MinY, baseCell.MinY, $"{prefix}_{y} bottom to base cell bottom");
            AssertEdgeContact(side.MaxY, baseCell.MaxY, $"{prefix}_{y} top to base cell top");

            if (y > 0)
            {
                AssertEdgeContact(side.MinY, previous.MaxY, $"{prefix}_{y - 1}/{y} side-to-side");
            }

            previous = side;
        }
    }

    private static void VerifyCornerContacts(Transform borderRoot, GridManager gridManager, VisibleWorldBounds baseBounds)
    {
        VisibleWorldBounds topFirst = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderTop_0"));
        VisibleWorldBounds topLast = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderTop_" + (gridManager.Width - 1)));
        VisibleWorldBounds bottomFirst = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderBottom_0"));
        VisibleWorldBounds bottomLast = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderBottom_" + (gridManager.Width - 1)));
        VisibleWorldBounds leftBottom = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderLeft_0"));
        VisibleWorldBounds leftTop = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderLeft_" + (gridManager.Height - 1)));
        VisibleWorldBounds rightBottom = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderRight_0"));
        VisibleWorldBounds rightTop = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderRight_" + (gridManager.Height - 1)));

        VisibleWorldBounds topLeft = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderTopLeft"));
        AssertEdgeContact(topLeft.MaxX, topFirst.MinX, "TopLeft corner right to top side left");
        AssertEdgeContact(topLeft.MinY, leftTop.MaxY, "TopLeft corner bottom to left side top");
        AssertNoLeftProtrude(leftTop.MinX, topLeft.MinX, "Left side top outer edge inside TopLeft corner");
        AssertNoTopProtrude(topFirst.MaxY, topLeft.MaxY, "Top side first outer edge inside TopLeft corner");
        AssertCornerOutsideBase(topLeft, baseBounds, "TopLeft", true, true);

        VisibleWorldBounds topRight = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderTopRight"));
        AssertEdgeContact(topRight.MinX, topLast.MaxX, "TopRight corner left to top side right");
        AssertEdgeContact(topRight.MinY, rightTop.MaxY, "TopRight corner bottom to right side top");
        AssertNoRightProtrude(rightTop.MaxX, topRight.MaxX, "Right side top outer edge inside TopRight corner");
        AssertNoTopProtrude(topLast.MaxY, topRight.MaxY, "Top side last outer edge inside TopRight corner");
        AssertCornerOutsideBase(topRight, baseBounds, "TopRight", false, true);

        VisibleWorldBounds bottomRight = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderBottomRight"));
        AssertEdgeContact(bottomRight.MinX, bottomLast.MaxX, "BottomRight corner left to bottom side right");
        AssertEdgeContact(bottomRight.MaxY, rightBottom.MinY, "BottomRight corner top to right side bottom");
        AssertNoRightProtrude(rightBottom.MaxX, bottomRight.MaxX, "Right side bottom outer edge inside BottomRight corner");
        AssertNoBottomProtrude(bottomLast.MinY, bottomRight.MinY, "Bottom side last outer edge inside BottomRight corner");
        AssertCornerOutsideBase(bottomRight, baseBounds, "BottomRight", false, false);

        VisibleWorldBounds bottomLeft = GetVisibleWorldBounds(RequireChild(borderRoot, "WarmFloorBorderBottomLeft"));
        AssertEdgeContact(bottomLeft.MaxX, bottomFirst.MinX, "BottomLeft corner right to bottom side left");
        AssertEdgeContact(bottomLeft.MaxY, leftBottom.MinY, "BottomLeft corner top to left side bottom");
        AssertNoLeftProtrude(leftBottom.MinX, bottomLeft.MinX, "Left side bottom outer edge inside BottomLeft corner");
        AssertNoBottomProtrude(bottomFirst.MinY, bottomLeft.MinY, "Bottom side first outer edge inside BottomLeft corner");
        AssertCornerOutsideBase(bottomLeft, baseBounds, "BottomLeft", true, false);
    }

    private static Transform RequireCell(GridManager gridManager, int x, int y)
    {
        GridCell cell = gridManager.GetCell(x, y);
        if (cell == null)
        {
            throw new InvalidOperationException($"Missing grid cell for visible bounds: Cell_{x}_{y}");
        }

        return cell.transform;
    }

    private static VisibleWorldBounds GetBaseVisibleBounds(GridManager gridManager)
    {
        VisibleWorldBounds first = GetVisibleWorldBounds(RequireCell(gridManager, 0, 0));
        float minX = first.MinX;
        float maxX = first.MaxX;
        float minY = first.MinY;
        float maxY = first.MaxY;

        for (int y = 0; y < gridManager.Height; y++)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                VisibleWorldBounds bounds = GetVisibleWorldBounds(RequireCell(gridManager, x, y));
                minX = Mathf.Min(minX, bounds.MinX);
                maxX = Mathf.Max(maxX, bounds.MaxX);
                minY = Mathf.Min(minY, bounds.MinY);
                maxY = Mathf.Max(maxY, bounds.MaxY);
            }
        }

        VisibleWorldBounds baseBounds = new VisibleWorldBounds(minX, maxX, minY, maxY);
        Debug.Log("[WarmFloorTilePlaytestBatch] base visible bounds: " + baseBounds);
        return baseBounds;
    }

    private static void AssertCornerOutsideBase(VisibleWorldBounds corner, VisibleWorldBounds baseBounds, string name, bool left, bool top)
    {
        if (left)
        {
            AssertEdgeContact(corner.MaxX, baseBounds.MinX, name + " corner base-left tangent");
        }
        else
        {
            AssertEdgeContact(corner.MinX, baseBounds.MaxX, name + " corner base-right tangent");
        }

        if (top)
        {
            AssertEdgeContact(corner.MinY, baseBounds.MaxY, name + " corner base-top tangent");
        }
        else
        {
            AssertEdgeContact(corner.MaxY, baseBounds.MinY, name + " corner base-bottom tangent");
        }
    }

    private static float GetAllowedHorizontalSideBaseOverlap(Transform borderRoot, string prefix, bool top, int width)
    {
        VisibleWorldBounds side = GetVisibleWorldBounds(RequireChild(borderRoot, prefix + "_0"));
        string cornerName = top ? "WarmFloorBorderTopLeft" : "WarmFloorBorderBottomLeft";
        VisibleWorldBounds corner = GetVisibleWorldBounds(RequireChild(borderRoot, cornerName));
        return Mathf.Max(0f, side.Height - corner.Height);
    }

    private static float GetAllowedVerticalSideBaseOverlap(Transform borderRoot, string prefix, bool left, int height)
    {
        VisibleWorldBounds side = GetVisibleWorldBounds(RequireChild(borderRoot, prefix + "_0"));
        string cornerName = left ? "WarmFloorBorderBottomLeft" : "WarmFloorBorderBottomRight";
        VisibleWorldBounds corner = GetVisibleWorldBounds(RequireChild(borderRoot, cornerName));
        return Mathf.Max(0f, side.Width - corner.Width);
    }

    private static void AssertNoVerticalGapOrExcessOverlap(float sideEdge, float baseEdge, bool sideAboveBase, float maxOverlap, string label)
    {
        const float tolerance = 0.003f;
        float overlap = sideAboveBase ? baseEdge - sideEdge : sideEdge - baseEdge;
        float gap = -overlap;
        Debug.Log($"[WarmFloorTilePlaytestBatch] edge {label}: side={sideEdge:0.####}, base={baseEdge:0.####}, gap={Mathf.Max(0f, gap):0.####}, overlap={Mathf.Max(0f, overlap):0.####}, allowedOverlap={maxOverlap:0.####}");

        if (gap > tolerance)
        {
            throw new InvalidOperationException($"{label} visible edge gap: side={sideEdge:0.####}, base={baseEdge:0.####}, gap={gap:0.####}");
        }

        if (overlap - maxOverlap > tolerance)
        {
            throw new InvalidOperationException($"{label} visible edge overlap too large: side={sideEdge:0.####}, base={baseEdge:0.####}, overlap={overlap:0.####}, allowed={maxOverlap:0.####}");
        }
    }

    private static void AssertNoHorizontalGapOrExcessOverlap(float sideEdge, float baseEdge, bool sideLeftOfBase, float maxOverlap, string label)
    {
        const float tolerance = 0.003f;
        float overlap = sideLeftOfBase ? sideEdge - baseEdge : baseEdge - sideEdge;
        float gap = -overlap;
        Debug.Log($"[WarmFloorTilePlaytestBatch] edge {label}: side={sideEdge:0.####}, base={baseEdge:0.####}, gap={Mathf.Max(0f, gap):0.####}, overlap={Mathf.Max(0f, overlap):0.####}, allowedOverlap={maxOverlap:0.####}");

        if (gap > tolerance)
        {
            throw new InvalidOperationException($"{label} visible edge gap: side={sideEdge:0.####}, base={baseEdge:0.####}, gap={gap:0.####}");
        }

        if (overlap - maxOverlap > tolerance)
        {
            throw new InvalidOperationException($"{label} visible edge overlap too large: side={sideEdge:0.####}, base={baseEdge:0.####}, overlap={overlap:0.####}, allowed={maxOverlap:0.####}");
        }
    }

    private static void AssertNoLeftProtrude(float sideMinX, float cornerMinX, string label)
    {
        AssertNoProtrude(sideMinX, cornerMinX, false, label);
    }

    private static void AssertNoRightProtrude(float sideMaxX, float cornerMaxX, string label)
    {
        AssertNoProtrude(sideMaxX, cornerMaxX, true, label);
    }

    private static void AssertNoBottomProtrude(float sideMinY, float cornerMinY, string label)
    {
        AssertNoProtrude(sideMinY, cornerMinY, false, label);
    }

    private static void AssertNoTopProtrude(float sideMaxY, float cornerMaxY, string label)
    {
        AssertNoProtrude(sideMaxY, cornerMaxY, true, label);
    }

    private static void AssertNoProtrude(float sideEdge, float cornerEdge, bool sideMustBeLessOrEqual, string label)
    {
        const float tolerance = 0.003f;
        float protrude = sideMustBeLessOrEqual ? sideEdge - cornerEdge : cornerEdge - sideEdge;
        Debug.Log($"[WarmFloorTilePlaytestBatch] protrude {label}: side={sideEdge:0.####}, corner={cornerEdge:0.####}, protrude={Mathf.Max(0f, protrude):0.####}");

        if (protrude > tolerance)
        {
            throw new InvalidOperationException($"{label} protrudes past corner: side={sideEdge:0.####}, corner={cornerEdge:0.####}, protrude={protrude:0.####}");
        }
    }

    private static void AssertEdgeContact(float actual, float expected, string label)
    {
        const float tolerance = 0.003f;
        float delta = actual - expected;
        Debug.Log($"[WarmFloorTilePlaytestBatch] edge {label}: actual={actual:0.####}, expected={expected:0.####}, gapOverlap={delta:0.####}");

        if (Mathf.Abs(delta) > tolerance)
        {
            throw new InvalidOperationException($"{label} visible edge mismatch: actual={actual:0.####}, expected={expected:0.####}, gapOverlap={delta:0.####}");
        }
    }

    private static VisibleWorldBounds GetVisibleWorldBounds(Transform tile)
    {
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            throw new InvalidOperationException("Missing SpriteRenderer for visible bounds check: " + tile.name);
        }

        return GetVisibleWorldBounds(renderer);
    }

    private static VisibleWorldBounds GetVisibleWorldBounds(SpriteRenderer renderer)
    {
        SpriteVisibleBounds localBounds = GetSpriteVisibleLocalBounds(renderer.sprite);
        Vector3 bottomLeft = renderer.transform.TransformPoint(new Vector3(localBounds.MinX, localBounds.MinY, 0f));
        Vector3 topLeft = renderer.transform.TransformPoint(new Vector3(localBounds.MinX, localBounds.MaxY, 0f));
        Vector3 bottomRight = renderer.transform.TransformPoint(new Vector3(localBounds.MaxX, localBounds.MinY, 0f));
        Vector3 topRight = renderer.transform.TransformPoint(new Vector3(localBounds.MaxX, localBounds.MaxY, 0f));

        return new VisibleWorldBounds(
            Mathf.Min(bottomLeft.x, topLeft.x, bottomRight.x, topRight.x),
            Mathf.Max(bottomLeft.x, topLeft.x, bottomRight.x, topRight.x),
            Mathf.Min(bottomLeft.y, topLeft.y, bottomRight.y, topRight.y),
            Mathf.Max(bottomLeft.y, topLeft.y, bottomRight.y, topRight.y));
    }

    private static SpriteVisibleBounds GetSpriteVisibleLocalBounds(Sprite sprite)
    {
        AlphaPixelBounds alphaBounds = GetSpriteAlphaPixelBounds(sprite);
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;

        return new SpriteVisibleBounds(
            (alphaBounds.MinX - pivot.x) / pixelsPerUnit,
            ((alphaBounds.MaxX + 1f) - pivot.x) / pixelsPerUnit,
            (alphaBounds.MinY - pivot.y) / pixelsPerUnit,
            ((alphaBounds.MaxY + 1f) - pivot.y) / pixelsPerUnit,
            alphaBounds);
    }

    private static AlphaPixelBounds GetSpriteAlphaPixelBounds(Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        int rectX = Mathf.RoundToInt(textureRect.x);
        int rectY = Mathf.RoundToInt(textureRect.y);
        int rectWidth = Mathf.RoundToInt(textureRect.width);
        int rectHeight = Mathf.RoundToInt(textureRect.height);
        Color32[] pixels = ReadTexturePixels(sprite.texture);
        int textureWidth = sprite.texture.width;

        int minX = rectWidth;
        int minY = rectHeight;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < rectHeight; y++)
        {
            int textureY = rectY + y;
            for (int x = 0; x < rectWidth; x++)
            {
                int textureX = rectX + x;
                int index = textureY * textureWidth + textureX;
                if (index < 0 || index >= pixels.Length || pixels[index].a == 0)
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new AlphaPixelBounds(0, rectWidth - 1, 0, rectHeight - 1);
        }

        return new AlphaPixelBounds(minX, maxX, minY, maxY);
    }

    private static Color32[] ReadTexturePixels(Texture2D texture)
    {
        try
        {
            return texture.GetPixels32();
        }
        catch (Exception)
        {
            return ReadTexturePixelsFromRenderTexture(texture);
        }
    }

    private static Color32[] ReadTexturePixelsFromRenderTexture(Texture2D texture)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

        try
        {
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readableTexture.Apply(false, false);
            return readableTexture.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.DestroyImmediate(readableTexture);
        }
    }

    private readonly struct AlphaPixelBounds
    {
        public AlphaPixelBounds(int minX, int maxX, int minY, int maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public int MinX { get; }
        public int MaxX { get; }
        public int MinY { get; }
        public int MaxY { get; }

        public override string ToString()
        {
            return $"({MinX},{MinY})-({MaxX},{MaxY})";
        }
    }

    private readonly struct SpriteVisibleBounds
    {
        public SpriteVisibleBounds(float minX, float maxX, float minY, float maxY, AlphaPixelBounds pixelBounds)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            PixelBounds = pixelBounds;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public AlphaPixelBounds PixelBounds { get; }
    }

    private readonly struct VisibleWorldBounds
    {
        public VisibleWorldBounds(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }

        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;

        public override string ToString()
        {
            return $"L/R/B/T={MinX:0.####}/{MaxX:0.####}/{MinY:0.####}/{MaxY:0.####}";
        }
    }

    private static float CalculateWarmFloorTileScale(Sprite sprite, float cellSize)
    {
        if (sprite == null)
        {
            return 1f;
        }

        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        return spriteSize > 0f ? cellSize / spriteSize : 1f;
    }

    private static void NormalizeAndVerifyWarmTileImporter(string assetPath, bool requireAlphaTransparency, float? requiredPixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Missing TextureImporter: " + assetPath);
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (requireAlphaTransparency && !importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (requiredPixelsPerUnit.HasValue &&
            Mathf.Abs(importer.spritePixelsPerUnit - requiredPixelsPerUnit.Value) > 0.001f)
        {
            importer.spritePixelsPerUnit = requiredPixelsPerUnit.Value;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }

        if (importer.textureType != TextureImporterType.Sprite ||
            importer.filterMode != FilterMode.Point ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            (requiredPixelsPerUnit.HasValue && Mathf.Abs(importer.spritePixelsPerUnit - requiredPixelsPerUnit.Value) > 0.001f) ||
            (requireAlphaTransparency && !importer.alphaIsTransparency))
        {
            throw new InvalidOperationException("Warm tile importer settings are not normalized: " + assetPath);
        }
    }

    private static float GetWarmTilePixelsPerUnit(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Missing TextureImporter: " + assetPath);
        }

        return importer.spritePixelsPerUnit;
    }

    private static Sprite RequireSprite(string resourcePath, string label)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites != null && sprites.Length > 0)
            {
                sprite = sprites[0];
            }
        }

        if (sprite == null)
        {
            throw new InvalidOperationException($"Warm {label} sprite failed to load: {resourcePath}");
        }

        return sprite;
    }

    private static void CaptureWarmFloorOverviewScreenshot(string path, GridManager gridManager)
    {
        Camera camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            throw new InvalidOperationException("No camera found for warm floor screenshot.");
        }

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Vector3 previousPosition = camera.transform.position;
        float previousOrthographicSize = camera.orthographicSize;
        RenderTexture renderTexture = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;

            if (camera.orthographic && gridManager != null)
            {
                float totalWidth = (gridManager.Width + 2) * gridManager.CellSize;
                float totalHeight = (gridManager.Height + 2) * gridManager.CellSize;
                float aspect = (float)renderTexture.width / renderTexture.height;
                float padding = gridManager.CellSize * 0.5f;

                camera.transform.position = new Vector3(0f, 0f, previousPosition.z);
                camera.orthographicSize = Mathf.Max(
                    (totalHeight * 0.5f) + padding,
                    (totalWidth * 0.5f / aspect) + padding
                );
            }

            camera.Render();
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Debug.Log("[WarmFloorTilePlaytestBatch] Screenshot: " + fullPath);
        }
        finally
        {
            camera.transform.position = previousPosition;
            camera.orthographicSize = previousOrthographicSize;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception ||
            condition.Contains("NullReference") ||
            condition.Contains("Missing Sprite") ||
            condition.Contains("Resources.Load"))
        {
            foundConsoleFailure = true;
            consoleFailureMessage = condition;
        }
    }

    private static void Fail(string message)
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        EditorApplication.update -= Tick;
        EditorPrefs.DeleteKey(PendingRunKey);
        Debug.LogError("[WarmFloorTilePlaytestBatch] FAIL: " + message);
        EditorApplication.Exit(1);
    }
}
