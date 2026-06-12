using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class SpriteSlicerFixer
{
    [MenuItem("Tools/Fix Customer Sprite Slicing")]
    public static void FixSlicing()
    {
        string folderPath = "Assets/_Project/Resources/GeneratedRuntimeUI/characters/customer";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.EndsWith(".png")) continue;

            Match match = Regex.Match(assetPath, @"_(\d+)x(\d+)\.png$");
            if (!match.Success) continue;

            int cols = int.Parse(match.Groups[1].Value);
            int rows = int.Parse(match.Groups[2].Value);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) continue;

            int cellW = tex.width / cols;
            int cellH = tex.height / rows;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            SpriteMetaData[] metaData = new SpriteMetaData[cols * rows];
            int index = 0;
            // Unity slices from bottom to top, left to right. Or top to bottom?
            // Usually, Sprite Editor slices from top-left, going right, then down.
            // So row 0 is the TOP row, row 1 is the BOTTOM row.
            // In Unity coordinates (bottom-left origin), the top row has y = height - cellH.
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    SpriteMetaData smd = new SpriteMetaData();
                    // Name format: filename_index
                    smd.name = tex.name + "_" + index;
                    // x goes left to right
                    float x = c * cellW;
                    // y goes top to bottom in visual order, so row 0 is top.
                    // Unity Y is bottom-up.
                    float y = tex.height - ((r + 1) * cellH);
                    smd.rect = new Rect(x, y, cellW, cellH);
                    smd.alignment = (int)SpriteAlignment.Center;
                    smd.pivot = new Vector2(0.5f, 0.5f);
                    metaData[index] = smd;
                    index++;
                }
            }

            importer.spritesheet = metaData;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"Sliced {assetPath} into {cols}x{rows} cells of {cellW}x{cellH}");
        }
        Debug.Log("Finished slicing all customer sprites.");
    }
}
