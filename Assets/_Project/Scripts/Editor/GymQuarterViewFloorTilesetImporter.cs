// Quarter-view support has been removed. Pure top-view only.
// 
// IMPORTANT: This entire file's quarter-view integration (QuarterViewFloorReplacementBatch,
// all MenuItems for perspective verification, references to removed GridManager quarter-view
// properties and PerspectiveGridFloorVisualizer) has been completely deleted to eliminate
// compile errors.
// 
// Only the general atlas slicing engine remains (now used to prepare sprites that are
// consumed as regular top-view repeating floor tiles via GridManager + GridCell).
// 
// The class name is kept for minimal diff, but the component is legacy.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// [LEGACY / DEPRECATED - Quarter-view support removed 2026-04]
/// Pure top-view only.
///
/// This class contains a reusable 4x5 grid alpha-based sprite atlas slicer.
/// It was originally created to slice gym_floor_tileset_quarterview_v1.png.
///
/// All quarter-view runtime integration, MenuItems, batch tools, and references
/// to removed APIs (GridManager quarter-view fields, PerspectiveGridFloorVisualizer, etc.)
/// have been completely removed from this file to eliminate compile errors.
///
/// The slicing engine itself is kept as a general utility.
/// The new tileset sprites (e.g. floor_wood_plank_a) are now used directly
/// as regular top-view floor tiles via GridManager / GymFloorTileResources.
/// </summary>
public static class GymQuarterViewFloorTilesetImporter
{
    // [LEGACY] This path points to the tileset that was previously used for quarter-view experiment.
    // As of 2026-04, the sprites inside it (especially floor_wood_plank_a) are now used
    // as regular top-view floor tiles in GridCell via GymFloorTileResources.
    // Quarter-view runtime code has been fully removed from the project.
    private const string TargetTexturePath = "Assets/_Project/Resources/GeneratedRuntimeUI/building/floor/gym_floor_tileset_quarterview_v1.png";

    // [REMOVED] LegacyPreviewRootName was only used by the deleted QuarterViewFloorReplacementBatch.
    // private const string LegacyPreviewRootName = "QuarterViewFloorPrototypePreview_DO_NOT_SHIP";

    private const int ExpectedColumns = 4;
    private const int ExpectedRows = 5;
    private const int ExpectedSpriteCount = ExpectedColumns * ExpectedRows;
    private const int PixelsPerUnit = 64;
    private const byte AlphaThreshold = 8;
    private const int MinComponentPixels = 16;

    private static readonly string[] SpriteNames =
    {
        "floor_beige_ceramic_a",
        "floor_beige_stone_a",
        "floor_wood_plank_a",
        "floor_wood_herringbone_a",
        "floor_rubber_plain_a",
        "floor_rubber_panel_2x2",
        "floor_rubber_dotted",
        "floor_rubber_diamond",
        "floor_transition_beige_top_rubber_bottom",
        "floor_transition_rubber_top_beige_bottom",
        "floor_transition_beige_left_rubber_right",
        "floor_transition_rubber_left_beige_right",
        "floor_transition_corner_rubber_bottom_left",
        "floor_transition_corner_rubber_bottom_right",
        "floor_transition_corner_rubber_top_left",
        "floor_transition_corner_rubber_top_right",
        "floor_border_strip_horizontal",
        "floor_border_strip_vertical",
        "floor_entry_mat_wood",
        "floor_entry_mat_metal",
    };

    // [DISABLED] This MenuItem was for the old quarter-view tileset import workflow.
    // Quarter-view support has been completely removed. Pure top-view only.
    // The sprites from this tileset are now consumed directly as regular floor tiles.
    // [MenuItem("_Project/Art/Import Quarter View Floor Tileset (DISABLED)")]
    public static void ImportTargetTileset()
    {
        Debug.LogWarning("[GymQuarterViewFloorTilesetImporter] This importer is legacy. " +
                         "The tileset gym_floor_tileset_quarterview_v1.png is now used directly " +
                         "for top-view floor tiles (see GridManager defaultFloorTileName).");
        ImportAndSliceTargetTileset(true);
    }

    private static ImportResult ImportAndSliceTargetTileset(bool logWhenMissing)
    {
        AssetDatabase.Refresh();

        string fullPath = Path.GetFullPath(TargetTexturePath);
        if (!File.Exists(fullPath))
        {
            if (logWhenMissing)
            {
                Debug.LogWarning($"[GymQuarterViewFloorTilesetImporter] Target PNG not found. No import changes were made: {TargetTexturePath}");
            }

            return ImportResult.Missing(TargetTexturePath);
        }

        AssetDatabase.ImportAsset(TargetTexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(TargetTexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[GymQuarterViewFloorTilesetImporter] Missing TextureImporter: {TargetTexturePath}");
            return ImportResult.Missing(TargetTexturePath);
        }

        ConfigureImporter(importer);
        importer.SaveAndReimport();

        TextureAlphaData textureData = LoadTextureAlphaData(fullPath);
        List<string> extractionWarnings = new List<string>();
        List<SpriteSlice> slices = ExtractSlices(textureData, extractionWarnings, out int connectedComponentCount, out string extractionMode);

        if (connectedComponentCount != ExpectedSpriteCount)
        {
            Debug.LogWarning($"[GymQuarterViewFloorTilesetImporter] Alpha connected extraction found {connectedComponentCount} parts, expected {ExpectedSpriteCount}. Using fallback/mode: {extractionMode}");
        }

        if (slices.Count != ExpectedSpriteCount)
        {
            Debug.LogWarning($"[GymQuarterViewFloorTilesetImporter] Final extracted sprite count is {slices.Count}, expected {ExpectedSpriteCount}. Sprite names will use generated fallback names.");
        }

        foreach (string warning in extractionWarnings)
        {
            Debug.LogWarning("[GymQuarterViewFloorTilesetImporter] " + warning);
        }

        SortSlicesTopToBottomLeftToRight(slices);
        SpriteRect[] spriteRects = BuildSpriteRects(importer, slices);
        ApplySpriteRects(importer, TargetTexturePath, spriteRects);

        string summary = BuildImportLog(textureData, spriteRects, extractionMode, connectedComponentCount);
        Debug.Log(summary);

        return new ImportResult(TargetTexturePath, textureData.Width, textureData.Height, spriteRects.Length, true);
    }

    private static void ConfigureImporter(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.wrapModeU = TextureWrapMode.Clamp;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.wrapModeW = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.maxTextureSize = 8192;

        TextureImporterSettings textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        textureSettings.spriteAlignment = (int)SpriteAlignment.Center;
        textureSettings.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SetTextureSettings(textureSettings);

        ApplyPlatformOverride(importer, "DefaultTexturePlatform", false);
        ApplyPlatformOverride(importer, "Standalone", true);
        ApplyPlatformOverride(importer, "Android", true);
        ApplyPlatformOverride(importer, "iPhone", true);
        ApplyPlatformOverride(importer, "WebGL", true);
    }

    private static void ApplyPlatformOverride(TextureImporter importer, string platformName, bool overridden)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        settings.name = platformName;
        settings.maxTextureSize = 8192;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        settings.compressionQuality = 100;
        settings.crunchedCompression = false;
        settings.overridden = overridden;
        importer.SetPlatformTextureSettings(settings);
    }

    private static TextureAlphaData LoadTextureAlphaData(string fullPath)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            byte[] pngBytes = File.ReadAllBytes(fullPath);
            if (!ImageConversion.LoadImage(texture, pngBytes, false))
            {
                throw new InvalidOperationException("Failed to decode PNG: " + fullPath);
            }

            return new TextureAlphaData(texture.width, texture.height, texture.GetPixels32());
        }
        finally
        {
            // Use fully qualified name to avoid ambiguity with built-in 'object' type
            // (the previous 'using Object = UnityEngine.Object;' alias was removed during quarter-view cleanup).
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static List<SpriteSlice> ExtractSlices(
        TextureAlphaData textureData,
        List<string> warnings,
        out int connectedComponentCount,
        out string extractionMode)
    {
        List<SpriteSlice> connectedSlices = ExtractConnectedAlphaComponents(textureData);
        connectedComponentCount = connectedSlices.Count;
        if (connectedSlices.Count == ExpectedSpriteCount)
        {
            extractionMode = "alpha-connected-components";
            return connectedSlices;
        }

        List<SpriteSlice> gridCorrectedSlices = ExtractGridCorrectedAlphaRects(textureData, warnings);
        if (gridCorrectedSlices.Count == ExpectedSpriteCount)
        {
            extractionMode = "4x5-grid-alpha-bounds";
            return gridCorrectedSlices;
        }

        extractionMode = connectedSlices.Count > 0 ? "alpha-connected-components-incomplete" : "4x5-grid-alpha-bounds-incomplete";
        return connectedSlices.Count > 0 ? connectedSlices : gridCorrectedSlices;
    }

    private static List<SpriteSlice> ExtractConnectedAlphaComponents(TextureAlphaData textureData)
    {
        List<SpriteSlice> slices = new List<SpriteSlice>();
        bool[] visited = new bool[textureData.Pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int y = 0; y < textureData.Height; y++)
        {
            for (int x = 0; x < textureData.Width; x++)
            {
                int index = textureData.ToIndex(x, y);
                if (visited[index] || !textureData.HasAlphaAt(index, AlphaThreshold))
                {
                    continue;
                }

                int minX = x;
                int minY = y;
                int maxX = x;
                int maxY = y;
                int pixelCount = 0;

                visited[index] = true;
                queue.Enqueue(index);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    int currentX = current % textureData.Width;
                    int currentY = current / textureData.Width;

                    pixelCount++;
                    minX = Mathf.Min(minX, currentX);
                    minY = Mathf.Min(minY, currentY);
                    maxX = Mathf.Max(maxX, currentX);
                    maxY = Mathf.Max(maxY, currentY);

                    EnqueueIfOpaque(textureData, visited, queue, currentX - 1, currentY);
                    EnqueueIfOpaque(textureData, visited, queue, currentX + 1, currentY);
                    EnqueueIfOpaque(textureData, visited, queue, currentX, currentY - 1);
                    EnqueueIfOpaque(textureData, visited, queue, currentX, currentY + 1);
                }

                if (pixelCount >= MinComponentPixels)
                {
                    slices.Add(new SpriteSlice(new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1)));
                }
            }
        }

        return slices;
    }

    private static void EnqueueIfOpaque(TextureAlphaData textureData, bool[] visited, Queue<int> queue, int x, int y)
    {
        if (x < 0 || x >= textureData.Width || y < 0 || y >= textureData.Height)
        {
            return;
        }

        int index = textureData.ToIndex(x, y);
        if (visited[index] || !textureData.HasAlphaAt(index, AlphaThreshold))
        {
            return;
        }

        visited[index] = true;
        queue.Enqueue(index);
    }

    private static List<SpriteSlice> ExtractGridCorrectedAlphaRects(TextureAlphaData textureData, List<string> warnings)
    {
        List<SpriteSlice> slices = new List<SpriteSlice>(ExpectedSpriteCount);
        for (int row = 0; row < ExpectedRows; row++)
        {
            int cellYMin = Mathf.FloorToInt(((ExpectedRows - 1 - row) * textureData.Height) / (float)ExpectedRows);
            int cellYMaxExclusive = Mathf.FloorToInt(((ExpectedRows - row) * textureData.Height) / (float)ExpectedRows);

            for (int column = 0; column < ExpectedColumns; column++)
            {
                int cellXMin = Mathf.FloorToInt((column * textureData.Width) / (float)ExpectedColumns);
                int cellXMaxExclusive = Mathf.FloorToInt(((column + 1) * textureData.Width) / (float)ExpectedColumns);
                RectInt cell = new RectInt(
                    cellXMin,
                    cellYMin,
                    Mathf.Max(1, cellXMaxExclusive - cellXMin),
                    Mathf.Max(1, cellYMaxExclusive - cellYMin));

                if (TryFindAlphaBoundsInCell(textureData, cell, out Rect rect))
                {
                    slices.Add(new SpriteSlice(rect));
                }
                else
                {
                    warnings.Add($"No opaque pixels found in grid cell row={row + 1}, column={column + 1}; using full cell rect {FormatRect(cell)}.");
                    slices.Add(new SpriteSlice(new Rect(cell.x, cell.y, cell.width, cell.height)));
                }
            }
        }

        return slices;
    }

    private static bool TryFindAlphaBoundsInCell(TextureAlphaData textureData, RectInt cell, out Rect rect)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        int xMax = Mathf.Min(textureData.Width, cell.xMax);
        int yMax = Mathf.Min(textureData.Height, cell.yMax);
        for (int y = Mathf.Max(0, cell.yMin); y < yMax; y++)
        {
            for (int x = Mathf.Max(0, cell.xMin); x < xMax; x++)
            {
                if (!textureData.HasAlphaAt(textureData.ToIndex(x, y), AlphaThreshold))
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
            rect = default;
            return false;
        }

        rect = new Rect(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        return true;
    }

    private static void SortSlicesTopToBottomLeftToRight(List<SpriteSlice> slices)
    {
        slices.Sort((left, right) =>
        {
            int yComparison = right.Rect.yMax.CompareTo(left.Rect.yMax);
            if (yComparison != 0)
            {
                return yComparison;
            }

            return left.Rect.xMin.CompareTo(right.Rect.xMin);
        });
    }

    private static SpriteRect[] BuildSpriteRects(TextureImporter importer, IReadOnlyList<SpriteSlice> slices)
    {
        Dictionary<string, GUID> existingIds = GetExistingSpriteIds(importer);
        SpriteRect[] rects = new SpriteRect[slices.Count];
        bool useExpectedNames = slices.Count == ExpectedSpriteCount;

        for (int i = 0; i < slices.Count; i++)
        {
            string spriteName = useExpectedNames ? SpriteNames[i] : $"quarterview_floor_part_{i:00}";
            GUID spriteId = existingIds.TryGetValue(spriteName, out GUID existingId)
                ? existingId
                : GUID.Generate();

            rects[i] = new SpriteRect
            {
                name = spriteName,
                rect = slices[i].Rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = spriteId,
            };
        }

        return rects;
    }

    private static Dictionary<string, GUID> GetExistingSpriteIds(TextureImporter importer)
    {
        Dictionary<string, GUID> existingIds = new Dictionary<string, GUID>();
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            return existingIds;
        }

        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        if (existingRects == null)
        {
            return existingIds;
        }

        for (int i = 0; i < existingRects.Length; i++)
        {
            SpriteRect rect = existingRects[i];
            if (!string.IsNullOrEmpty(rect.name) && !existingIds.ContainsKey(rect.name))
            {
                existingIds.Add(rect.name, rect.spriteID);
            }
        }

        return existingIds;
    }

    private static void ApplySpriteRects(TextureImporter importer, string assetPath, SpriteRect[] rects)
    {
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            throw new InvalidOperationException("Unable to create sprite data provider for " + assetPath);
        }

        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(rects);

        ISpriteNameFileIdDataProvider nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdProvider != null)
        {
            List<SpriteNameFileIdPair> pairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < rects.Length; i++)
            {
                pairs.Add(new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
            }

            nameFileIdProvider.SetNameFileIdPairs(pairs);
        }

        dataProvider.Apply();
        AssetDatabase.ForceReserializeAssets(new[] { assetPath }, ForceReserializeAssetsOptions.ReserializeMetadata);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static string BuildImportLog(TextureAlphaData textureData, SpriteRect[] spriteRects, string extractionMode, int connectedComponentCount)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("[GymQuarterViewFloorTilesetImporter] Import complete");
        builder.AppendLine($"texture path: {TargetTexturePath}");
        builder.AppendLine($"texture size: {textureData.Width}x{textureData.Height}");
        builder.AppendLine($"alpha connected component count: {connectedComponentCount}");
        builder.AppendLine($"extraction mode: {extractionMode}");
        builder.AppendLine($"extracted sprite count: {spriteRects.Length}");
        for (int i = 0; i < spriteRects.Length; i++)
        {
            SpriteRect rect = spriteRects[i];
            builder.AppendLine($"sprite[{i:00}] {rect.name}: {FormatRect(rect.rect)}");
        }

        builder.AppendLine("import settings: TextureType=Sprite, SpriteMode=Multiple, PixelsPerUnit=64, FilterMode=Point, Compression=None, Mipmap=Off, WrapMode=Clamp, AlphaIsTransparency=On, MeshType=FullRect, Pivot=Center, Border=0");
        return builder.ToString();
    }

    private static string FormatRect(Rect rect)
    {
        return $"x={rect.x:0}, y={rect.y:0}, w={rect.width:0}, h={rect.height:0}";
    }

    private static string FormatRect(RectInt rect)
    {
        return $"x={rect.x}, y={rect.y}, w={rect.width}, h={rect.height}";
    }

    private readonly struct TextureAlphaData
    {
        public TextureAlphaData(int width, int height, Color32[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public int Width { get; }
        public int Height { get; }
        public Color32[] Pixels { get; }

        public int ToIndex(int x, int y)
        {
            return (y * Width) + x;
        }

        public bool HasAlphaAt(int index, byte threshold)
        {
            return index >= 0 && index < Pixels.Length && Pixels[index].a > threshold;
        }
    }

    private readonly struct SpriteSlice
    {
        public SpriteSlice(Rect rect)
        {
            Rect = rect;
        }

        public Rect Rect { get; }
    }

    private readonly struct ImportResult
    {
        public ImportResult(string path, int width, int height, int spriteCount, bool success)
        {
            Path = path;
            Width = width;
            Height = height;
            SpriteCount = spriteCount;
            Success = success;
        }

        public string Path { get; }
        public int Width { get; }
        public int Height { get; }
        public int SpriteCount { get; }
        public bool Success { get; }

        public static ImportResult Missing(string path)
        {
            return new ImportResult(path, 0, 0, 0, false);
        }
    }
}

// ============================================================
// [REMOVED 2026-04 - Quarter-view support completely removed]
// 
// The entire QuarterViewFloorReplacementBatch class has been deleted from this file.
// 
// It contained:
// - All [MenuItem] entries for quarter-view verification / perspective visualizer
// - Heavy references to removed GridManager APIs:
//     QuarterViewFloorReplacementRootName, QuarterViewSinglePanelRootName,
//     UseQuarterViewFloorPrototype, UseQuarterViewFloorSinglePanelTest,
//     EnablePerspectiveFloorVisualizer, UseMeshPerspectiveFloor, etc.
// - Heavy references to removed PerspectiveGridFloorVisualizer members:
//     VisualRootName, GetVisualCellSize, CurrentFloorSpriteName, 
//     CurrentSpriteUvLog, GetVisualCellCenter, GetVisualScaleForGridCell, etc.
// - Calls to GymFloorTileResources.QuarterViewFloorTilesetPath
// - Play mode verification logic that no longer compiles after top-view return.
//
// Only the core reusable atlas slicing engine in GymQuarterViewFloorTilesetImporter
// remains (marked as deprecated for quarter-view use).
// Pure top-view (GridCell SpriteRenderer + Warm Floor) is the official direction.
// ============================================================
