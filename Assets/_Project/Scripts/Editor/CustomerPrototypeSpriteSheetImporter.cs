using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class CustomerPrototypeSpriteSheetImporter
{
    private const float PixelsPerUnit = 64f;
    private const int BackgroundWhiteThreshold = 242;
    private const int AlphaThreshold = 12;
    private const int HeadPaddingPixels = 2;

    private const string RawBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_idle_base_32x48_4x2.png";
    private const string ProcessedBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_idle_base_32x48_4x2_processed_transparent.png";
    private const string DumbbellCurlBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_dumbbell_curl_2x2.png";
    private const string DumbbellShoulderPressBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_dumbbell_shoulder_press_2x2.png";
    private const string DumbbellExerciseImportSessionKey = "DumbbellExerciseSpriteImportsApplied_20260627";
    private const string YogaMatSpritePath = "Assets/_Project/Resources/GeneratedRuntimeUI/objects/yoga_mat.png";
    private const string YogaOverheadBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_yoga_overhead_2x2.png";
    private const string YogaSideBendBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_yoga_side_bend_2x2.png";
    private const string YogaToeTouchBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_yoga_toe_touch_2x2.png";
    private const string YogaExerciseImportSessionKey = "YogaExerciseSpriteImportsApplied_20260628_v1";
    private const string LockerSpritePath = "Assets/_Project/Resources/GeneratedRuntimeUI/objects/locker.png";
    private const string LockerDoorSheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/objects/locker_door_2x2.png";
    private const string LockerUseBodySheetPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/body/male_chubby/body_male_chubby_locker_use_2x2.png";
    private const string LockerSpriteImportSessionKey = "LockerSpriteImportsApplied_20260630_v2";
    private const string HeadRootFolder = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer/head";

    private static readonly string[] BodyFrameNames =
    {
        "body_male_chubby_idle_base_00",
        "body_male_chubby_idle_base_01",
        "body_male_chubby_idle_base_02",
        "body_male_chubby_idle_base_03",
        "body_male_chubby_idle_base_04",
        "body_male_chubby_idle_base_05",
        "body_male_chubby_idle_base_06",
        "body_male_chubby_idle_base_07",
    };

    private static readonly string[] HeadFrameNames =
    {
        "head_customer_01_front",
        "head_customer_01_side",
        "head_customer_01_back",
    };

    private static readonly string[] DumbbellCurlFrameNames =
    {
        "body_male_chubby_dumbbell_curl_2x2_0",
        "body_male_chubby_dumbbell_curl_2x2_1",
        "body_male_chubby_dumbbell_curl_2x2_2",
        "body_male_chubby_dumbbell_curl_2x2_3",
    };

    private static readonly string[] DumbbellShoulderPressFrameNames =
    {
        "body_male_chubby_dumbbell_shoulder_press_2x2_0",
        "body_male_chubby_dumbbell_shoulder_press_2x2_1",
        "body_male_chubby_dumbbell_shoulder_press_2x2_2",
        "body_male_chubby_dumbbell_shoulder_press_2x2_3",
    };

    private static readonly string[] YogaOverheadFrameNames =
    {
        "body_male_chubby_yoga_overhead_2x2_0",
        "body_male_chubby_yoga_overhead_2x2_1",
        "body_male_chubby_yoga_overhead_2x2_2",
        "body_male_chubby_yoga_overhead_2x2_3",
    };

    private static readonly string[] YogaSideBendFrameNames =
    {
        "body_male_chubby_yoga_side_bend_2x2_0",
        "body_male_chubby_yoga_side_bend_2x2_1",
        "body_male_chubby_yoga_side_bend_2x2_2",
        "body_male_chubby_yoga_side_bend_2x2_3",
    };

    private static readonly string[] YogaToeTouchFrameNames =
    {
        "body_male_chubby_yoga_toe_touch_2x2_0",
        "body_male_chubby_yoga_toe_touch_2x2_1",
        "body_male_chubby_yoga_toe_touch_2x2_2",
        "body_male_chubby_yoga_toe_touch_2x2_3",
    };

    private static readonly string[] LockerUseFrameNames =
    {
        "body_male_chubby_locker_use_2x2_0",
        "body_male_chubby_locker_use_2x2_1",
        "body_male_chubby_locker_use_2x2_2",
        "body_male_chubby_locker_use_2x2_3",
    };

    private static readonly string[] LockerDoorFrameNames =
    {
        "locker_door_2x2_0",
        "locker_door_2x2_1",
        "locker_door_2x2_2",
        "locker_door_2x2_3",
    };

    [InitializeOnLoadMethod]
    private static void AutoApplyDumbbellExerciseSpriteImportsAfterReload()
    {
        if (SessionState.GetBool(DumbbellExerciseImportSessionKey, false))
        {
            return;
        }

        if (HasExpectedDumbbellSpriteMetadata())
        {
            SessionState.SetBool(DumbbellExerciseImportSessionKey, true);
            return;
        }

        SessionState.SetBool(DumbbellExerciseImportSessionKey, true);
        EditorApplication.delayCall += ApplyDumbbellExerciseSpriteImports;
    }

    [InitializeOnLoadMethod]
    private static void AutoApplyYogaExerciseSpriteImportsAfterReload()
    {
        if (SessionState.GetBool(YogaExerciseImportSessionKey, false))
        {
            return;
        }

        if (HasExpectedYogaSpriteMetadata())
        {
            SessionState.SetBool(YogaExerciseImportSessionKey, true);
            return;
        }

        SessionState.SetBool(YogaExerciseImportSessionKey, true);
        EditorApplication.delayCall += ApplyYogaExerciseSpriteImports;
    }

    [InitializeOnLoadMethod]
    private static void AutoApplyLockerSpriteImportsAfterReload()
    {
        if (SessionState.GetBool(LockerSpriteImportSessionKey, false))
        {
            return;
        }

        if (HasExpectedLockerSpriteMetadata())
        {
            SessionState.SetBool(LockerSpriteImportSessionKey, true);
            return;
        }

        SessionState.SetBool(LockerSpriteImportSessionKey, true);
        EditorApplication.delayCall += ApplyLockerSpriteImports;
    }

    [MenuItem("Tools/Customer Prototype/Apply Layered Customer Sprite Imports")]
    public static void ApplyLayeredCustomerSpriteImports()
    {
        string rawHeadSheetPath = FindRawHeadSheetPath();
        string processedHeadSheetPath = GetProcessedSheetPath(rawHeadSheetPath);

        Vector2Int processedBodySize = BuildProcessedBodySheet(RawBodySheetPath, ProcessedBodySheetPath);
        Vector2Int processedHeadSize = BuildProcessedHeadSheet(rawHeadSheetPath, processedHeadSheetPath);

        AssetDatabase.ImportAsset(RawBodySheetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ProcessedBodySheetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(processedHeadSheetPath, ImportAssetOptions.ForceUpdate);
        SliceSheet(RawBodySheetPath, 4, 2, BodyFrameNames);
        SliceSheet(ProcessedBodySheetPath, 4, 2, BodyFrameNames);
        SliceSheet(processedHeadSheetPath, 3, 1, HeadFrameNames);
        AssetDatabase.Refresh();

        Debug.Log(
            "[CustomerPrototypeSpriteSheetImporter] Applied processed layered customer sprite imports.\n" +
            $"Raw body: {RawBodySheetPath}\n" +
            $"Processed body: {ProcessedBodySheetPath}\n" +
            $"Processed body size: {processedBodySize.x}x{processedBodySize.y}\n" +
            $"Body frame count: {BodyFrameNames.Length}\n" +
            $"Raw head: {rawHeadSheetPath}\n" +
            $"Processed head: {processedHeadSheetPath}\n" +
            $"Processed head size: {processedHeadSize.x}x{processedHeadSize.y}\n" +
            $"Head frame count: {HeadFrameNames.Length}");
    }

    [MenuItem("Tools/Customer Prototype/Apply Dumbbell Exercise Sprite Imports")]
    public static void ApplyDumbbellExerciseSpriteImports()
    {
        SliceSheet(
            DumbbellCurlBodySheetPath,
            2,
            2,
            DumbbellCurlFrameNames,
            96f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        SliceSheet(
            DumbbellShoulderPressBodySheetPath,
            2,
            2,
            DumbbellShoulderPressFrameNames,
            96f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        AssetDatabase.Refresh();

        Debug.Log(
            "[CustomerPrototypeSpriteSheetImporter] Applied dumbbell exercise sprite imports.\n" +
            $"Curl: {DumbbellCurlBodySheetPath}\n" +
            $"Shoulder press: {DumbbellShoulderPressBodySheetPath}");
    }

    [MenuItem("Tools/Customer Prototype/Apply Yoga Exercise Sprite Imports")]
    public static void ApplyYogaExerciseSpriteImports()
    {
        ConfigureSingleSprite(YogaMatSpritePath, 100f, new Vector2(0.5f, 0.5f));

        SliceSheet(
            YogaOverheadBodySheetPath,
            2,
            2,
            YogaOverheadFrameNames,
            96f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        SliceSheet(
            YogaSideBendBodySheetPath,
            2,
            2,
            YogaSideBendFrameNames,
            96f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        SliceSheet(
            YogaToeTouchBodySheetPath,
            2,
            2,
            YogaToeTouchFrameNames,
            96f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        AssetDatabase.Refresh();

        Debug.Log(
            "[CustomerPrototypeSpriteSheetImporter] Applied yoga exercise sprite imports.\n" +
            $"Mat: {YogaMatSpritePath}\n" +
            $"Overhead: {YogaOverheadBodySheetPath}\n" +
            $"Side bend: {YogaSideBendBodySheetPath}\n" +
            $"Toe touch: {YogaToeTouchBodySheetPath}");
    }

    [MenuItem("Tools/Customer Prototype/Apply Locker Sprite Imports")]
    public static void ApplyLockerSpriteImports()
    {
        ConfigureSingleSprite(LockerSpritePath, 100f, new Vector2(0.5f, 0.5f));

        SliceSheet(
            LockerDoorSheetPath,
            2,
            2,
            LockerDoorFrameNames,
            100f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        SliceSheet(
            LockerUseBodySheetPath,
            2,
            2,
            LockerUseFrameNames,
            96f,
            SpriteAlignment.Center,
            new Vector2(0.5f, 0.5f));

        AssetDatabase.Refresh();

        Debug.Log(
            "[CustomerPrototypeSpriteSheetImporter] Applied locker sprite imports.\n" +
            $"Locker: {LockerSpritePath}\n" +
            $"Door: {LockerDoorSheetPath}\n" +
            $"Customer: {LockerUseBodySheetPath}");
    }

    private static bool HasExpectedDumbbellSpriteMetadata()
    {
        return SpriteMetaContainsFrame(DumbbellCurlBodySheetPath, DumbbellCurlFrameNames[DumbbellCurlFrameNames.Length - 1]) &&
            SpriteMetaContainsFrame(DumbbellShoulderPressBodySheetPath, DumbbellShoulderPressFrameNames[DumbbellShoulderPressFrameNames.Length - 1]);
    }

    private static bool HasExpectedYogaSpriteMetadata()
    {
        return IsSingleSpriteImportConfigured(YogaMatSpritePath) &&
            SpriteMetaContainsFrame(YogaOverheadBodySheetPath, YogaOverheadFrameNames[YogaOverheadFrameNames.Length - 1]) &&
            SpriteMetaContainsFrame(YogaSideBendBodySheetPath, YogaSideBendFrameNames[YogaSideBendFrameNames.Length - 1]) &&
            SpriteMetaContainsFrame(YogaToeTouchBodySheetPath, YogaToeTouchFrameNames[YogaToeTouchFrameNames.Length - 1]);
    }

    private static bool HasExpectedLockerSpriteMetadata()
    {
        return IsSingleSpriteImportConfigured(LockerSpritePath) &&
            SpriteMetaContainsFrame(LockerDoorSheetPath, LockerDoorFrameNames[LockerDoorFrameNames.Length - 1]) &&
            SpriteMetaContainsFrame(LockerUseBodySheetPath, LockerUseFrameNames[LockerUseFrameNames.Length - 1]);
    }

    private static bool IsSingleSpriteImportConfigured(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        return importer != null &&
            importer.textureType == TextureImporterType.Sprite &&
            importer.spriteImportMode == SpriteImportMode.Single;
    }

    private static bool SpriteMetaContainsFrame(string assetPath, string frameName)
    {
        string metaPath = assetPath + ".meta";
        if (!File.Exists(metaPath))
        {
            return false;
        }

        return File.ReadAllText(metaPath).Contains(frameName);
    }

    private static string FindRawHeadSheetPath()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { HeadRootFolder });
        string firstPng = "";
        string firstThreeDirectionSheet = "";

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path) ||
                !path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                path.Contains("_processed_transparent"))
            {
                continue;
            }

            if (string.IsNullOrEmpty(firstPng))
            {
                firstPng = path;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains("3dir") || fileName.Contains("3direction") || fileName.Contains("3_direction"))
            {
                firstThreeDirectionSheet = path;
                break;
            }
        }

        string selected = !string.IsNullOrEmpty(firstThreeDirectionSheet) ? firstThreeDirectionSheet : firstPng;
        if (string.IsNullOrEmpty(selected))
        {
            throw new System.InvalidOperationException("No raw head sheet PNG found under " + HeadRootFolder);
        }

        return selected;
    }

    private static string GetProcessedSheetPath(string rawAssetPath)
    {
        string directory = Path.GetDirectoryName(rawAssetPath);
        string fileName = Path.GetFileNameWithoutExtension(rawAssetPath);
        return (directory + "/" + fileName + "_processed_transparent.png").Replace("\\", "/");
    }

    private static Vector2Int BuildProcessedBodySheet(string rawAssetPath, string processedAssetPath)
    {
        Texture2D rawTexture = LoadReadablePng(rawAssetPath);
        try
        {
            Color32[] rawPixels = rawTexture.GetPixels32();
            const int columns = 4;
            const int rows = 2;
            const int outputCellWidth = 32;
            const int outputCellHeight = 48;
            int sourceCellWidth = rawTexture.width / columns;
            int sourceCellHeight = rawTexture.height / rows;
            Color32[] outputPixels = CreateTransparentPixels(columns * outputCellWidth, rows * outputCellHeight);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int sourceCellX = column * sourceCellWidth;
                    int sourceCellY = rawTexture.height - ((row + 1) * sourceCellHeight);
                    Color32[] transparentCell = ExtractFloodFilledCell(
                        rawPixels,
                        rawTexture.width,
                        sourceCellX,
                        sourceCellY,
                        sourceCellWidth,
                        sourceCellHeight);
                    Color32[] resizedCell = ResizeFullCellNearest(
                        transparentCell,
                        sourceCellWidth,
                        sourceCellHeight,
                        outputCellWidth,
                        outputCellHeight);
                    int outputCellX = column * outputCellWidth;
                    int outputCellY = (rows - 1 - row) * outputCellHeight;
                    BlitCell(outputPixels, columns * outputCellWidth, outputCellX, outputCellY, outputCellWidth, outputCellHeight, resizedCell);
                }
            }

            WriteProcessedPng(processedAssetPath, columns * outputCellWidth, rows * outputCellHeight, outputPixels);
            return new Vector2Int(columns * outputCellWidth, rows * outputCellHeight);
        }
        finally
        {
            Object.DestroyImmediate(rawTexture);
        }
    }

    private static Vector2Int BuildProcessedHeadSheet(string rawAssetPath, string processedAssetPath)
    {
        Texture2D rawTexture = LoadReadablePng(rawAssetPath);
        try
        {
            Color32[] rawPixels = rawTexture.GetPixels32();
            const int columns = 3;
            const int rows = 1;
            const int outputCellWidth = 32;
            const int outputCellHeight = 32;
            int sourceCellWidth = rawTexture.width / columns;
            int sourceCellHeight = rawTexture.height;
            Color32[] outputPixels = CreateTransparentPixels(columns * outputCellWidth, rows * outputCellHeight);

            for (int column = 0; column < columns; column++)
            {
                int sourceCellX = column * sourceCellWidth;
                Color32[] transparentCell = ExtractFloodFilledCell(
                    rawPixels,
                    rawTexture.width,
                    sourceCellX,
                    0,
                    sourceCellWidth,
                    sourceCellHeight);
                Color32[] normalizedCell = NormalizeContentBottomCentered(
                    transparentCell,
                    sourceCellWidth,
                    sourceCellHeight,
                    outputCellWidth,
                    outputCellHeight,
                    HeadPaddingPixels);
                BlitCell(outputPixels, columns * outputCellWidth, column * outputCellWidth, 0, outputCellWidth, outputCellHeight, normalizedCell);
            }

            WriteProcessedPng(processedAssetPath, columns * outputCellWidth, rows * outputCellHeight, outputPixels);
            return new Vector2Int(columns * outputCellWidth, rows * outputCellHeight);
        }
        finally
        {
            Object.DestroyImmediate(rawTexture);
        }
    }

    private static Texture2D LoadReadablePng(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Missing source PNG", fullPath);
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
        {
            Object.DestroyImmediate(texture);
            throw new System.InvalidOperationException("Unable to load PNG: " + assetPath);
        }

        return texture;
    }

    private static Color32[] ExtractFloodFilledCell(
        Color32[] sourcePixels,
        int sourceTextureWidth,
        int sourceCellX,
        int sourceCellY,
        int sourceCellWidth,
        int sourceCellHeight)
    {
        Color32[] cellPixels = new Color32[sourceCellWidth * sourceCellHeight];
        for (int y = 0; y < sourceCellHeight; y++)
        {
            for (int x = 0; x < sourceCellWidth; x++)
            {
                cellPixels[(y * sourceCellWidth) + x] =
                    sourcePixels[((sourceCellY + y) * sourceTextureWidth) + sourceCellX + x];
            }
        }

        RemoveEdgeConnectedBackground(cellPixels, sourceCellWidth, sourceCellHeight);
        return cellPixels;
    }

    private static void RemoveEdgeConnectedBackground(Color32[] pixels, int width, int height)
    {
        bool[] visited = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int x = 0; x < width; x++)
        {
            EnqueueBackgroundPixel(pixels, visited, queue, x, 0, width, height);
            EnqueueBackgroundPixel(pixels, visited, queue, x, height - 1, width, height);
        }

        for (int y = 0; y < height; y++)
        {
            EnqueueBackgroundPixel(pixels, visited, queue, 0, y, width, height);
            EnqueueBackgroundPixel(pixels, visited, queue, width - 1, y, width, height);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            Color32 transparent = pixels[index];
            transparent.a = 0;
            pixels[index] = transparent;

            EnqueueBackgroundPixel(pixels, visited, queue, x + 1, y, width, height);
            EnqueueBackgroundPixel(pixels, visited, queue, x - 1, y, width, height);
            EnqueueBackgroundPixel(pixels, visited, queue, x, y + 1, width, height);
            EnqueueBackgroundPixel(pixels, visited, queue, x, y - 1, width, height);
        }
    }

    private static void EnqueueBackgroundPixel(
        Color32[] pixels,
        bool[] visited,
        Queue<int> queue,
        int x,
        int y,
        int width,
        int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = (y * width) + x;
        if (visited[index])
        {
            return;
        }

        visited[index] = true;
        if (IsBackgroundPixel(pixels[index]))
        {
            queue.Enqueue(index);
        }
    }

    private static bool IsBackgroundPixel(Color32 pixel)
    {
        if (pixel.a <= AlphaThreshold)
        {
            return true;
        }

        return pixel.r >= BackgroundWhiteThreshold &&
               pixel.g >= BackgroundWhiteThreshold &&
               pixel.b >= BackgroundWhiteThreshold;
    }

    private static Color32[] ResizeFullCellNearest(
        Color32[] sourcePixels,
        int sourceWidth,
        int sourceHeight,
        int outputWidth,
        int outputHeight)
    {
        Color32[] outputPixels = CreateTransparentPixels(outputWidth, outputHeight);
        for (int y = 0; y < outputHeight; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.FloorToInt(((y + 0.5f) * sourceHeight) / outputHeight), 0, sourceHeight - 1);
            for (int x = 0; x < outputWidth; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt(((x + 0.5f) * sourceWidth) / outputWidth), 0, sourceWidth - 1);
                outputPixels[(y * outputWidth) + x] = sourcePixels[(sourceY * sourceWidth) + sourceX];
            }
        }

        return outputPixels;
    }

    private static Color32[] NormalizeContentBottomCentered(
        Color32[] sourcePixels,
        int sourceWidth,
        int sourceHeight,
        int outputWidth,
        int outputHeight,
        int padding)
    {
        Color32[] outputPixels = CreateTransparentPixels(outputWidth, outputHeight);
        if (!TryGetContentBounds(sourcePixels, sourceWidth, sourceHeight, out RectInt bounds))
        {
            return outputPixels;
        }

        int availableWidth = Mathf.Max(1, outputWidth - (padding * 2));
        int availableHeight = Mathf.Max(1, outputHeight - (padding * 2));
        float scale = Mathf.Min((float)availableWidth / bounds.width, (float)availableHeight / bounds.height);
        int scaledWidth = Mathf.Clamp(Mathf.RoundToInt(bounds.width * scale), 1, availableWidth);
        int scaledHeight = Mathf.Clamp(Mathf.RoundToInt(bounds.height * scale), 1, availableHeight);
        int destinationX = (outputWidth - scaledWidth) / 2;
        int destinationY = padding;

        for (int y = 0; y < scaledHeight; y++)
        {
            int sourceY = bounds.yMin + Mathf.Clamp(Mathf.FloorToInt(((y + 0.5f) * bounds.height) / scaledHeight), 0, bounds.height - 1);
            for (int x = 0; x < scaledWidth; x++)
            {
                int sourceX = bounds.xMin + Mathf.Clamp(Mathf.FloorToInt(((x + 0.5f) * bounds.width) / scaledWidth), 0, bounds.width - 1);
                outputPixels[((destinationY + y) * outputWidth) + destinationX + x] = sourcePixels[(sourceY * sourceWidth) + sourceX];
            }
        }

        return outputPixels;
    }

    private static bool TryGetContentBounds(Color32[] pixels, int width, int height, out RectInt bounds)
    {
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(y * width) + x].a <= AlphaThreshold)
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
            bounds = default;
            return false;
        }

        bounds = new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        return true;
    }

    private static Color32[] CreateTransparentPixels(int width, int height)
    {
        Color32[] pixels = new Color32[width * height];
        Color32 transparent = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparent;
        }

        return pixels;
    }

    private static void BlitCell(
        Color32[] targetPixels,
        int targetWidth,
        int targetX,
        int targetY,
        int cellWidth,
        int cellHeight,
        Color32[] cellPixels)
    {
        for (int y = 0; y < cellHeight; y++)
        {
            for (int x = 0; x < cellWidth; x++)
            {
                targetPixels[((targetY + y) * targetWidth) + targetX + x] = cellPixels[(y * cellWidth) + x];
            }
        }
    }

    private static void WriteProcessedPng(string assetPath, int width, int height, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void SliceSheet(string assetPath, int columns, int rows, IReadOnlyList<string> frameNames)
    {
        SliceSheet(assetPath, columns, rows, frameNames, PixelsPerUnit, SpriteAlignment.BottomCenter, new Vector2(0.5f, 0f));
    }

    private static void SliceSheet(
        string assetPath,
        int columns,
        int rows,
        IReadOnlyList<string> frameNames,
        float pixelsPerUnit,
        SpriteAlignment alignment,
        Vector2 pivot)
    {
        if (frameNames == null || frameNames.Count != columns * rows)
        {
            throw new System.InvalidOperationException($"Invalid sprite name count for {assetPath}");
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException("Missing TextureImporter: " + assetPath);
        }

        ConfigureImporter(importer, pixelsPerUnit, pivot);
        importer.SaveAndReimport();

        importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException("Missing TextureImporter after import: " + assetPath);
        }

        SpriteRect[] rects = BuildSpriteRects(importer, columns, rows, frameNames, alignment, pivot);
        ApplySpriteRects(importer, assetPath, rects);
    }

    private static void ConfigureImporter(TextureImporter importer, float pixelsPerUnit, Vector2 pivot)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = pivot;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        TextureImporterSettings textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);

        ApplyPlatformOverride(importer, "DefaultTexturePlatform", false);
        ApplyPlatformOverride(importer, "Standalone", true);
        ApplyPlatformOverride(importer, "Android", true);
        ApplyPlatformOverride(importer, "iPhone", true);
        ApplyPlatformOverride(importer, "WebGL", true);
    }

    private static void ConfigureSingleSprite(string assetPath, float pixelsPerUnit, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new System.InvalidOperationException("Missing TextureImporter: " + assetPath);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = pivot;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        TextureImporterSettings textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);

        ApplyPlatformOverride(importer, "DefaultTexturePlatform", false);
        ApplyPlatformOverride(importer, "Standalone", true);
        ApplyPlatformOverride(importer, "Android", true);
        ApplyPlatformOverride(importer, "iPhone", true);
        ApplyPlatformOverride(importer, "WebGL", true);
        importer.SaveAndReimport();
    }

    private static void ApplyPlatformOverride(TextureImporter importer, string platformName, bool overridden)
    {
        TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platformName);
        platformSettings.name = platformName;
        platformSettings.maxTextureSize = 4096;
        platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
        platformSettings.compressionQuality = 100;
        platformSettings.crunchedCompression = false;
        platformSettings.overridden = overridden;
        importer.SetPlatformTextureSettings(platformSettings);
    }

    private static SpriteRect[] BuildSpriteRects(
        TextureImporter importer,
        int columns,
        int rows,
        IReadOnlyList<string> frameNames,
        SpriteAlignment alignment,
        Vector2 pivot)
    {
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            throw new System.InvalidOperationException("Unable to create sprite data provider for " + importer.assetPath);
        }

        dataProvider.InitSpriteEditorDataProvider();
        ITextureDataProvider textureDataProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
        if (textureDataProvider == null)
        {
            throw new System.InvalidOperationException("Unable to read texture size for " + importer.assetPath);
        }

        textureDataProvider.GetTextureActualWidthAndHeight(out int textureWidth, out int textureHeight);
        if (textureWidth <= 0 || textureHeight <= 0)
        {
            throw new System.InvalidOperationException("Invalid texture size for " + importer.assetPath);
        }

        int frameWidth = textureWidth / columns;
        int frameHeight = textureHeight / rows;
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new System.InvalidOperationException("Invalid frame size for " + importer.assetPath);
        }

        Dictionary<string, GUID> existingIds = GetExistingSpriteIds(dataProvider);
        SpriteRect[] rects = new SpriteRect[columns * rows];
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = (row * columns) + column;
                string spriteName = frameNames[index];
                float x = column * frameWidth;
                float y = textureHeight - ((row + 1) * frameHeight);

                SpriteRect rect = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(x, y, frameWidth, frameHeight),
                    alignment = alignment,
                    pivot = pivot,
                    border = Vector4.zero,
                    spriteID = existingIds.TryGetValue(spriteName, out GUID existingId)
                        ? existingId
                        : GUID.Generate(),
                };
                rects[index] = rect;
            }
        }

        return rects;
    }

    private static Dictionary<string, GUID> GetExistingSpriteIds(ISpriteEditorDataProvider dataProvider)
    {
        Dictionary<string, GUID> existingIds = new Dictionary<string, GUID>();
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
            throw new System.InvalidOperationException("Unable to create sprite data provider for " + assetPath);
        }

        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(rects);

        ISpriteNameFileIdDataProvider nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdProvider != null)
        {
            List<SpriteNameFileIdPair> nameFileIdPairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < rects.Length; i++)
            {
                nameFileIdPairs.Add(new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
            }

            nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);
        }

        dataProvider.Apply();
        AssetDatabase.ForceReserializeAssets(new[] { assetPath }, ForceReserializeAssetsOptions.ReserializeMetadata);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }
}
