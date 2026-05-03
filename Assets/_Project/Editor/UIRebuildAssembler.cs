using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIRebuildAssembler
{
    private const string SpriteRoot = "Assets/_Project/Sprites/UI_Rebuild";
    private const string PrefabRoot = "Assets/_Project/Prefabs/UIRebuild";
    private const string FontPath = "Assets/_Project/Fonts/neodgm.ttf";

    private static readonly Color PanelText = new Color32(34, 28, 18, 255);
    private static readonly Color SubtleText = new Color32(77, 63, 43, 255);
    private static readonly Color AccentText = new Color32(32, 77, 44, 255);
    private static readonly Color PreviewBackground = new Color32(244, 236, 218, 255);
    private static readonly Color OutlineColor = new Color32(255, 248, 232, 190);

    private readonly struct BorderSpec
    {
        public BorderSpec(float horizontalRatio, float verticalRatio, int minPixels, int maxPixels)
        {
            HorizontalRatio = horizontalRatio;
            VerticalRatio = verticalRatio;
            MinPixels = minPixels;
            MaxPixels = maxPixels;
        }

        public float HorizontalRatio { get; }
        public float VerticalRatio { get; }
        public int MinPixels { get; }
        public int MaxPixels { get; }
    }

    private static readonly Dictionary<string, BorderSpec> SliceBorderSpecs = new Dictionary<string, BorderSpec>
    {
        ["UI_Common_MainPanel_Base_L"] = new BorderSpec(0.11f, 0.14f, 18, 88),
        ["UI_Common_SectionBox_M"] = new BorderSpec(0.10f, 0.16f, 14, 56),
        ["UI_Common_SummaryBox_S"] = new BorderSpec(0.12f, 0.18f, 12, 44),
        ["UI_Common_Button_Wide_Normal"] = new BorderSpec(0.13f, 0.24f, 12, 40),
        ["UI_Common_Button_Wide_Active"] = new BorderSpec(0.13f, 0.24f, 12, 40),
        ["UI_Common_Button_Wide_Disabled"] = new BorderSpec(0.13f, 0.24f, 12, 40),
        ["UI_Common_Tab_M_Normal"] = new BorderSpec(0.13f, 0.22f, 12, 36),
        ["UI_Common_Tab_M_Active"] = new BorderSpec(0.13f, 0.22f, 12, 36),
        ["UI_Common_Tab_M_Secondary"] = new BorderSpec(0.13f, 0.22f, 12, 36),
        ["UI_HUD_TopBar_Base"] = new BorderSpec(0.09f, 0.24f, 14, 60),
        ["UI_HUD_InfoBox_Small"] = new BorderSpec(0.12f, 0.22f, 10, 32),
        ["UI_BottomNav_Base"] = new BorderSpec(0.09f, 0.26f, 14, 68),
        ["UI_BottomNav_Tab_Normal"] = new BorderSpec(0.12f, 0.24f, 12, 36),
        ["UI_BottomNav_Tab_Active"] = new BorderSpec(0.12f, 0.24f, 12, 36),
        ["UI_Economy_SummaryBox"] = new BorderSpec(0.11f, 0.18f, 12, 44),
        ["UI_Economy_DualInfoBox"] = new BorderSpec(0.10f, 0.16f, 14, 54),
        ["UI_Economy_DetailBox"] = new BorderSpec(0.10f, 0.14f, 16, 64),
        ["UI_Review_SummaryBox"] = new BorderSpec(0.11f, 0.18f, 12, 44),
        ["UI_Review_ListBox"] = new BorderSpec(0.10f, 0.15f, 14, 60),
        ["UI_Review_EventLogBox"] = new BorderSpec(0.10f, 0.15f, 14, 60),
        ["UI_Review_EmptyStateBox"] = new BorderSpec(0.10f, 0.16f, 14, 48),
        ["UI_Popup_Base_Large"] = new BorderSpec(0.10f, 0.14f, 18, 78),
        ["UI_Popup_Header"] = new BorderSpec(0.09f, 0.24f, 12, 42),
        ["UI_Popup_ListRow"] = new BorderSpec(0.10f, 0.22f, 10, 32)
    };

    private static readonly string[] BorderAssetNames = SliceBorderSpecs.Keys.OrderBy(name => name).ToArray();

    [MenuItem("Tools/UI Rebuild/Generate All")]
    public static void GenerateAll()
    {
        EnsureFolderChain(SpriteRoot);
        EnsureFolderChain(PrefabRoot);

        int modifiedSpriteCount = ConfigureAllSpriteImports();

        BuildTitleScreenPrefab();
        BuildTopHudPrefab();
        BuildBottomNavPrefab();
        BuildOperatePanelPrefab();
        BuildInstallPanelPrefab();
        BuildEconomyPanelPrefab();
        BuildReviewPanelPrefab();
        BuildUiRootCanvasPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        WriteReport(modifiedSpriteCount);
        Debug.Log($"UIRebuildAssembler complete. Updated sprites: {modifiedSpriteCount}");
    }

    private static int ConfigureAllSpriteImports()
    {
        int modified = 0;
        string[] pngPaths = Directory
            .GetFiles(SpriteRoot, "*.png", SearchOption.AllDirectories)
            .Select(ToAssetPath)
            .OrderBy(path => path)
            .ToArray();

        foreach (string assetPath in pngPaths)
        {
            if (ConfigureSpriteImport(assetPath))
            {
                modified++;
            }
        }

        return modified;
    }

    private static bool ConfigureSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return false;
        }

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        if (textureSettings.spriteMeshType != SpriteMeshType.FullRect)
        {
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            dirty = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            dirty = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            dirty = true;
        }

        if (importer.spritePixelsPerUnit != 100f)
        {
            importer.spritePixelsPerUnit = 100f;
            dirty = true;
        }

        dirty |= ApplyPlatformOverride(importer, "DefaultTexturePlatform", false);
        dirty |= ApplyPlatformOverride(importer, "Standalone", true);
        dirty |= ApplyPlatformOverride(importer, "Android", true);
        dirty |= ApplyPlatformOverride(importer, "iPhone", true);
        dirty |= ApplyPlatformOverride(importer, "WebGL", true);

        if (dirty)
        {
            importer.SaveAndReimport();
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        Vector4 desiredBorder = Vector4.zero;
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        if (texture != null && SliceBorderSpecs.TryGetValue(assetName, out BorderSpec borderSpec))
        {
            desiredBorder = ComputeBorder(texture.width, texture.height, borderSpec);
        }

        if (importer.spriteBorder != desiredBorder)
        {
            importer.spriteBorder = desiredBorder;
            importer.SaveAndReimport();
            dirty = true;
        }

        return dirty;
    }

    private static bool ApplyPlatformOverride(TextureImporter importer, string platformName, bool overridden)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        bool dirty = false;

        if (settings.name != platformName)
        {
            settings.name = platformName;
            dirty = true;
        }

        if (settings.overridden != overridden)
        {
            settings.overridden = overridden;
            dirty = true;
        }

        if (settings.maxTextureSize != 2048)
        {
            settings.maxTextureSize = 2048;
            dirty = true;
        }

        if (settings.textureCompression != TextureImporterCompression.Uncompressed)
        {
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }

        if (settings.crunchedCompression)
        {
            settings.crunchedCompression = false;
            dirty = true;
        }

        if (dirty)
        {
            importer.SetPlatformTextureSettings(settings);
        }

        return dirty;
    }

    private static Vector4 ComputeBorder(int width, int height, BorderSpec spec)
    {
        int horizontal = Mathf.Clamp(Mathf.RoundToInt(width * spec.HorizontalRatio), spec.MinPixels, Mathf.Max(spec.MinPixels, width / 3));
        int vertical = Mathf.Clamp(Mathf.RoundToInt(height * spec.VerticalRatio), spec.MinPixels, Mathf.Max(spec.MinPixels, height / 3));
        return new Vector4(horizontal, vertical, horizontal, vertical);
    }

    private static void BuildTitleScreenPrefab()
    {
        const string prefabPath = PrefabRoot + "/Title/PF_UI_TitleScreen.prefab";
        var root = CreateRoot("PF_UI_TitleScreen", new Vector2(1080f, 1920f));

        RectTransform background = CreateStretchRect("Background", root.transform, 0f, 0f, 0f, 0f);
        AddSolidImage(background, PreviewBackground);

        RectTransform titleBoard = CreateCenteredRect("TitleBoard", root.transform, 0f, 120f, 860f, 1420f);
        AddSpriteImage(titleBoard, "Common/UI_Common_MainPanel_Base_L", Image.Type.Sliced);

        RectTransform logoFrame = CreateTopLeftRect("LogoFrame", titleBoard, 90f, 90f, 680f, 250f);
        AddSpriteImage(logoFrame, "Common/UI_Common_SectionBox_M", Image.Type.Sliced);
        CreateTextBlock("GameTitle", logoFrame, 0f, 52f, 680f, 86f, "헬스장 운영기", 66, TextAnchor.MiddleCenter, PanelText, 1.3f);
        CreateTextBlock("Subtitle", logoFrame, 0f, 144f, 680f, 42f, "동네 헬스장 경영 타이쿤", 26, TextAnchor.MiddleCenter, SubtleText);

        RectTransform loadButton = CreateTopLeftRect("ContinueButton", titleBoard, 180f, 390f, 500f, 112f);
        AddSpriteButton(loadButton, "Common/UI_Common_Button_Wide_Active", "이어하기", 34);

        RectTransform newButton = CreateTopLeftRect("NewGameButton", titleBoard, 180f, 528f, 500f, 112f);
        AddSpriteButton(newButton, "Common/UI_Common_Button_Wide_Normal", "새 게임", 34);

        RectTransform slotHeader = CreateTopLeftRect("SlotHeader", titleBoard, 110f, 710f, 640f, 70f);
        CreateTextBlock("SaveSlotsLabel", slotHeader, 0f, 0f, 640f, 48f, "저장 슬롯", 30, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("SaveHint", slotHeader, 0f, 34f, 640f, 32f, "월말 결산 직전 자동 저장", 20, TextAnchor.MiddleLeft, SubtleText);

        for (int i = 0; i < 3; i++)
        {
            float y = 810f + (i * 174f);
            RectTransform slot = CreateTopLeftRect($"Slot_{i + 1}", titleBoard, 110f, y, 640f, 144f);
            AddSpriteImage(slot, "Common/UI_Common_SectionBox_M", Image.Type.Sliced);
            CreateTextBlock($"SlotTitle_{i + 1}", slot, 28f, 24f, 400f, 40f, $"슬롯 {i + 1}  |  {GetSlotName(i)}", 24, TextAnchor.MiddleLeft, PanelText);
            CreateTextBlock($"SlotMeta_{i + 1}", slot, 28f, 68f, 360f, 34f, GetSlotMeta(i), 18, TextAnchor.MiddleLeft, SubtleText);

            RectTransform stateBox = CreateTopLeftRect($"SlotState_{i + 1}", slot, 430f, 28f, 168f, 86f);
            AddSpriteImage(stateBox, "Common/UI_Common_SummaryBox_S", Image.Type.Sliced);
            CreateTextBlock($"SlotStateLabel_{i + 1}", stateBox, 0f, 14f, 168f, 24f, "상태", 16, TextAnchor.MiddleCenter, SubtleText);
            CreateTextBlock($"SlotStateValue_{i + 1}", stateBox, 0f, 40f, 168f, 30f, i == 0 ? "운영 중" : "대기 중", 20, TextAnchor.MiddleCenter, AccentText);
        }

        SavePrefab(root, prefabPath);
    }

    private static void BuildTopHudPrefab()
    {
        const string prefabPath = PrefabRoot + "/HUD/PF_UI_TopHUD.prefab";
        var root = CreateRoot("PF_UI_TopHUD", new Vector2(1080f, 168f));

        RectTransform bar = CreateStretchRect("TopBar", root.transform, 0f, 0f, 0f, 0f);
        AddSpriteImage(bar, "HUD/UI_HUD_TopBar_Base", Image.Type.Sliced);

        CreateHudInfoBox(root.transform, "DateBox", 24f, 18f, 208f, 58f, "2026.04", "4주 2일");
        CreateHudInfoBox(root.transform, "CashBox", 248f, 18f, 222f, 58f, "자금", "₩ 128,450");
        CreateHudInfoBox(root.transform, "StarCoinBox", 486f, 18f, 190f, 58f, "스타코인", "34");
        CreateHudInfoBox(root.transform, "SpeedBox", 692f, 18f, 136f, 58f, "속도", "2x");

        CreateHudActionButton(root.transform, "StaffButton", 842f, 18f, "HUD/UI_HUD_Button_Square_Normal", "직원");
        CreateHudActionButton(root.transform, "MenuButton", 914f, 18f, "HUD/UI_HUD_Button_Square_Normal", "메뉴");
        CreateHudActionButton(root.transform, "BuildButton", 986f, 18f, "HUD/UI_HUD_Button_Square_Active", "설치");

        CreateHudSpeedChip(root.transform, "Speed1x", 842f, 92f, "HUD/UI_HUD_InfoBox_Small", "1x");
        CreateHudSpeedChip(root.transform, "Speed2x", 924f, 92f, "HUD/UI_HUD_InfoBox_Small", "2x");
        CreateHudSpeedChip(root.transform, "Speed4x", 1006f, 92f, "HUD/UI_HUD_InfoBox_Small", "4x");

        SavePrefab(root, prefabPath);
    }

    private static void BuildBottomNavPrefab()
    {
        const string prefabPath = PrefabRoot + "/BottomNav/PF_UI_BottomNav.prefab";
        var root = CreateRoot("PF_UI_BottomNav", new Vector2(1080f, 188f));

        RectTransform nav = CreateStretchRect("BottomNav", root.transform, 0f, 0f, 0f, 0f);
        AddSpriteImage(nav, "BottomNav/UI_BottomNav_Base", Image.Type.Sliced);

        string[] labels = { "운영", "설치", "경제", "리뷰" };
        for (int i = 0; i < labels.Length; i++)
        {
            float x = 70f + (i * 240f);
            bool active = i == 0;
            RectTransform tab = CreateTopLeftRect($"Tab_{labels[i]}", nav, x, 40f, 196f, 108f);
            AddSpriteButton(tab, active ? "BottomNav/UI_BottomNav_Tab_Active" : "BottomNav/UI_BottomNav_Tab_Normal", labels[i], 24);

            RectTransform iconFrame = CreateTopLeftRect($"IconFrame_{labels[i]}", tab, 20f, 18f, 42f, 42f);
            AddSpriteImage(iconFrame, "BottomNav/UI_BottomNav_IconFrame", Image.Type.Simple);
            CreateTextBlock($"IconGlyph_{labels[i]}", iconFrame, 0f, 0f, 42f, 42f, GetBottomNavGlyph(i), 20, TextAnchor.MiddleCenter, PanelText);
        }

        SavePrefab(root, prefabPath);
    }

    private static void BuildOperatePanelPrefab()
    {
        const string prefabPath = PrefabRoot + "/Panels/PF_UI_OperatePanel.prefab";
        var root = CreateRoot("PF_UI_OperatePanel", new Vector2(980f, 1220f));

        RectTransform panel = CreateStretchRect("OperatePanel", root.transform, 0f, 0f, 0f, 0f);
        AddSpriteImage(panel, "Common/UI_Common_MainPanel_Base_L", Image.Type.Sliced);

        CreateHeader(panel, "운영 현황", "오늘의 흐름과 회원 반응");
        CreateSubTabs(panel, 56f, 118f, "운영 요약", "일정");

        RectTransform summary = CreateTopLeftRect("SummaryRow", panel, 56f, 208f, 868f, 192f);
        AddSpriteImage(summary, "Panels/Operate/UI_Operate_SummaryRow_4Slot", Image.Type.Simple);
        CreateMetricLabel(summary, "회원", "124명", 18f);
        CreateMetricLabel(summary, "출석", "81명", 232f);
        CreateMetricLabel(summary, "만족도", "94%", 446f);
        CreateMetricLabel(summary, "대기열", "6명", 660f);

        RectTransform dualBox = CreateTopLeftRect("InfoBox", panel, 56f, 434f, 868f, 272f);
        AddSpriteImage(dualBox, "Panels/Operate/UI_Operate_InfoBox_Dual", Image.Type.Simple);
        CreateTextBlock("InfoLeftTitle", dualBox, 40f, 32f, 300f, 30f, "현재 피크 존", 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("InfoLeftBody", dualBox, 40f, 74f, 320f, 100f, "유산소 구역이 가장 붐빕니다.\n러닝머신 대기열 3명", 20, TextAnchor.UpperLeft, SubtleText);
        CreateTextBlock("InfoRightTitle", dualBox, 452f, 32f, 300f, 30f, "직원 알림", 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("InfoRightBody", dualBox, 452f, 74f, 320f, 100f, "카운터 직원이 신규 상담 2건을 처리 중입니다.", 20, TextAnchor.UpperLeft, SubtleText);

        RectTransform memo = CreateTopLeftRect("MemoBox", panel, 56f, 736f, 868f, 390f);
        AddSpriteImage(memo, "Panels/Operate/UI_Operate_MemoBox", Image.Type.Simple);
        CreateTextBlock("MemoTitle", memo, 34f, 28f, 340f, 34f, "공지 / 메모", 26, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("MemoBody", memo, 34f, 80f, 780f, 248f, "오늘의 추천:\n- 러닝머신 존 확장으로 출석 시간 분산\n- 회복 구역 체류 시간 증가로 만족도 상승\n- 월말 결산 전 프리미엄 회원권 판촉 유지", 22, TextAnchor.UpperLeft, SubtleText);

        SavePrefab(root, prefabPath);
    }

    private static void BuildInstallPanelPrefab()
    {
        const string prefabPath = PrefabRoot + "/Panels/PF_UI_InstallPanel.prefab";
        var root = CreateRoot("PF_UI_InstallPanel", new Vector2(980f, 1220f));

        RectTransform panel = CreateStretchRect("InstallPanel", root.transform, 0f, 0f, 0f, 0f);
        AddSpriteImage(panel, "Common/UI_Common_MainPanel_Base_L", Image.Type.Sliced);

        CreateHeader(panel, "설치", "2열 카드형 배치 미리보기");

        string[] categories = { "유산소", "근력", "회복", "편의" };
        for (int i = 0; i < categories.Length; i++)
        {
            RectTransform tab = CreateTopLeftRect($"CategoryTab_{categories[i]}", panel, 56f + (i * 214f), 118f, 190f, 76f);
            AddSpriteButton(tab, i == 0 ? "Panels/Install/UI_Install_CategoryTab_Active" : "Panels/Install/UI_Install_CategoryTab_Normal", categories[i], 24);
        }

        RectTransform descBox = CreateTopLeftRect("CategoryDescription", panel, 56f, 210f, 824f, 64f);
        AddSpriteImage(descBox, "Common/UI_Common_SectionBox_M", Image.Type.Sliced);
        CreateTextBlock("CategoryDescLabel", descBox, 24f, 10f, 180f, 24f, "현재 카테고리", 18, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("CategoryDescValue", descBox, 24f, 30f, 760f, 28f, "유산소 기구는 초반 출석률과 평균 체류 시간을 안정적으로 끌어올립니다.", 20, TextAnchor.MiddleLeft, PanelText);

        RectTransform scrollRoot = CreateTopLeftRect("CardScrollRoot", panel, 56f, 300f, 824f, 664f);
        var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        RectTransform viewport = CreateStretchRect("Viewport", scrollRoot, 0f, 0f, 0f, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport;

        RectTransform content = CreateTopLeftRect("Content", viewport, 0f, 0f, 824f, 760f);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        scrollRect.content = content;

        CreateInstallCard(content, "Card_01", 0f, 0f, "Panels/Install/UI_Install_Card_Selected", "유산소", "러닝머신", "₩ 12,000", "선택");
        CreateInstallCard(content, "Card_02", 416f, 0f, "Panels/Install/UI_Install_Card_Base", "유산소", "프리미엄 러닝머신", "₩ 28,000", "배치");
        CreateInstallCard(content, "Card_03", 0f, 194f, "Panels/Install/UI_Install_Card_Base", "근력", "벤치프레스", "₩ 18,500", "배치");
        CreateInstallCard(content, "Card_04", 416f, 194f, "Panels/Install/UI_Install_Card_Base", "편의", "정수기", "₩ 4,500", "설치");

        RectTransform rail = CreateTopLeftRect("ScrollRail", panel, 894f, 300f, 24f, 664f);
        AddSpriteImage(rail, "Common/UI_Common_ScrollRail_V", Image.Type.Simple);
        RectTransform handle = CreateTopLeftRect("ScrollHandle", rail, 2f, 72f, 20f, 168f);
        AddSpriteImage(handle, "Common/UI_Common_ScrollHandle_V", Image.Type.Simple);

        RectTransform selectionBar = CreateTopLeftRect("SelectionBar", panel, 56f, 986f, 868f, 166f);
        AddSpriteImage(selectionBar, "Panels/Install/UI_Install_SelectionBar", Image.Type.Simple);
        CreateTextBlock("SelectionTitle", selectionBar, 28f, 24f, 280f, 28f, "선택 기구", 20, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("SelectionName", selectionBar, 28f, 48f, 340f, 34f, "러닝머신 B", 26, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("SelectionMeta", selectionBar, 28f, 88f, 420f, 28f, "회원 수요 +8  |  체류 시간 +4", 18, TextAnchor.MiddleLeft, SubtleText);
        RectTransform priceBox = CreateTopLeftRect("SelectionPrice", selectionBar, 532f, 26f, 150f, 80f);
        AddSpriteImage(priceBox, "Common/UI_Common_SummaryBox_S", Image.Type.Sliced);
        CreateTextBlock("SelectionPriceLabel", priceBox, 0f, 14f, 150f, 22f, "가격", 14, TextAnchor.MiddleCenter, SubtleText);
        CreateTextBlock("SelectionPriceValue", priceBox, 0f, 36f, 150f, 28f, "₩ 12,000", 18, TextAnchor.MiddleCenter, AccentText);
        RectTransform confirmButton = CreateTopLeftRect("ConfirmButton", selectionBar, 700f, 30f, 140f, 74f);
        AddSpriteButton(confirmButton, "Common/UI_Common_Button_Wide_Active", "배치", 22);

        SavePrefab(root, prefabPath);
    }

    private static void BuildEconomyPanelPrefab()
    {
        const string prefabPath = PrefabRoot + "/Panels/PF_UI_EconomyPanel.prefab";
        var root = CreateRoot("PF_UI_EconomyPanel", new Vector2(980f, 1220f));

        RectTransform panel = CreateStretchRect("EconomyPanel", root.transform, 0f, 0f, 0f, 0f);
        AddSpriteImage(panel, "Common/UI_Common_MainPanel_Base_L", Image.Type.Sliced);

        CreateHeader(panel, "경제", "운영 패널과 동일한 가독성 스케일");
        CreateSubTabs(panel, 56f, 118f, "일일 현황", "월말 결산");

        CreateEconomySummary(panel, "Summary_01", 56f, 206f, "매출", "₩ 128,450");
        CreateEconomySummary(panel, "Summary_02", 274f, 206f, "지출", "₩ 49,200");
        CreateEconomySummary(panel, "Summary_03", 492f, 206f, "순이익", "₩ 79,250");
        CreateEconomySummary(panel, "Summary_04", 710f, 206f, "회원권", "62건");

        RectTransform middleLeft = CreateTopLeftRect("MemberMixBox", panel, 56f, 398f, 408f, 238f);
        AddSpriteImage(middleLeft, "Panels/Economy/UI_Economy_DualInfoBox", Image.Type.Sliced);
        CreateTextBlock("MemberMixTitle", middleLeft, 28f, 24f, 220f, 28f, "회원 구성", 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("MemberMixBody", middleLeft, 28f, 68f, 320f, 120f, "일반 58%\n프리미엄 28%\n상류층 14%", 22, TextAnchor.UpperLeft, SubtleText);
        CreateTextBlock("MemberMixRight", middleLeft, 228f, 68f, 140f, 120f, "재등록 41건\n추천 가입 9건", 20, TextAnchor.UpperLeft, AccentText);

        RectTransform middleRight = CreateTopLeftRect("CostBox", panel, 516f, 398f, 408f, 238f);
        AddSpriteImage(middleRight, "Panels/Economy/UI_Economy_DualInfoBox", Image.Type.Sliced);
        CreateTextBlock("CostTitle", middleRight, 28f, 24f, 220f, 28f, "비용 분해", 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("CostBody", middleRight, 28f, 68f, 320f, 120f, "인건비 22,000\n전기세 7,600\n유지비 6,100", 22, TextAnchor.UpperLeft, SubtleText);
        CreateTextBlock("CostRight", middleRight, 228f, 68f, 140f, 120f, "광고 5,200\n기타 8,300", 20, TextAnchor.UpperLeft, AccentText);

        RectTransform detailBox = CreateTopLeftRect("DetailBox", panel, 56f, 668f, 868f, 458f);
        AddSpriteImage(detailBox, "Panels/Economy/UI_Economy_DetailBox", Image.Type.Sliced);
        CreateTextBlock("DetailTitle", detailBox, 28f, 24f, 260f, 28f, "결산 상세", 26, TextAnchor.MiddleLeft, PanelText);
        string[] rows =
        {
            "기본 회원권 매출                      ₩ 74,000",
            "프리미엄 회원권 매출                  ₩ 36,500",
            "개인 PT 매출                         ₩ 17,950",
            "인건비                               ₩ -22,000",
            "시설 유지비                          ₩ -13,700",
            "마케팅 비용                          ₩ -5,200",
            "순이익                               ₩ 79,250"
        };
        for (int i = 0; i < rows.Length; i++)
        {
            CreateTextBlock($"DetailRow_{i + 1}", detailBox, 34f, 82f + (i * 42f), 760f, 28f, rows[i], 22, TextAnchor.MiddleLeft, i == rows.Length - 1 ? AccentText : SubtleText);
        }

        SavePrefab(root, prefabPath);
    }

    private static void BuildReviewPanelPrefab()
    {
        const string prefabPath = PrefabRoot + "/Panels/PF_UI_ReviewPanel.prefab";
        var root = CreateRoot("PF_UI_ReviewPanel", new Vector2(980f, 1220f));

        RectTransform panel = CreateStretchRect("ReviewPanel", root.transform, 0f, 0f, 0f, 0f);
        AddSpriteImage(panel, "Common/UI_Common_MainPanel_Base_L", Image.Type.Sliced);

        CreateHeader(panel, "리뷰", "최근 회원 반응과 이벤트 로그");
        CreateSubTabs(panel, 56f, 118f, "최근 반응", "누적 기록");

        CreateReviewSummary(panel, "Summary_01", 56f, 206f, "평점", "4.8");
        CreateReviewSummary(panel, "Summary_02", 274f, 206f, "리뷰 수", "128");
        CreateReviewSummary(panel, "Summary_03", 492f, 206f, "재방문", "82%");
        CreateReviewSummary(panel, "Summary_04", 710f, 206f, "이벤트", "5건");

        RectTransform reviewList = CreateTopLeftRect("ReviewListBox", panel, 56f, 398f, 868f, 278f);
        AddSpriteImage(reviewList, "Panels/Review/UI_Review_ListBox", Image.Type.Sliced);
        CreateTextBlock("ReviewListTitle", reviewList, 28f, 24f, 260f, 28f, "최근 리뷰", 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("ReviewLine_1", reviewList, 30f, 72f, 770f, 28f, "★★★★★  러닝머신 줄이 줄어서 운동이 편해졌어요.", 21, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("ReviewLine_2", reviewList, 30f, 114f, 770f, 28f, "★★★★☆  샤워실 청결이 좋아서 재등록했습니다.", 21, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("ReviewLine_3", reviewList, 30f, 156f, 770f, 28f, "★★★★★  회복 구역 매트가 넓어서 만족도가 높아요.", 21, TextAnchor.MiddleLeft, SubtleText);

        RectTransform eventLog = CreateTopLeftRect("EventLogBox", panel, 56f, 700f, 868f, 222f);
        AddSpriteImage(eventLog, "Panels/Review/UI_Review_EventLogBox", Image.Type.Sliced);
        CreateTextBlock("EventLogTitle", eventLog, 28f, 20f, 260f, 28f, "이벤트 로그", 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("EventLogLine_1", eventLog, 30f, 64f, 780f, 26f, "09:10  신규 회원 3명 등록", 20, TextAnchor.MiddleLeft, AccentText);
        CreateTextBlock("EventLogLine_2", eventLog, 30f, 100f, 780f, 26f, "12:40  프리미엄 러닝머신 관심 급증", 20, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("EventLogLine_3", eventLog, 30f, 136f, 780f, 26f, "18:20  회복 존 체류 시간 최고치 갱신", 20, TextAnchor.MiddleLeft, SubtleText);

        RectTransform emptyState = CreateTopLeftRect("EmptyStateBox", panel, 56f, 950f, 868f, 176f);
        AddSpriteImage(emptyState, "Panels/Review/UI_Review_EmptyStateBox", Image.Type.Sliced);
        CreateTextBlock("EmptyStateLabel", emptyState, 0f, 34f, 868f, 34f, "보류 중인 부정 리뷰가 없습니다.", 24, TextAnchor.MiddleCenter, AccentText);
        CreateTextBlock("EmptyStateSub", emptyState, 0f, 78f, 868f, 28f, "현재 운영 방향을 유지해도 좋습니다.", 18, TextAnchor.MiddleCenter, SubtleText);

        SavePrefab(root, prefabPath);
    }

    private static void BuildUiRootCanvasPrefab()
    {
        const string prefabPath = PrefabRoot + "/PF_UIRoot_Canvas.prefab";
        var rootGo = new GameObject("PF_UIRoot_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var root = rootGo.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(2560f, 3200f);

        var canvas = rootGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = rootGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 3200f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform backdrop = CreateStretchRect("Backdrop", rootGo.transform, 0f, 0f, 0f, 0f);
        AddSolidImage(backdrop, PreviewBackground);

        CreateTextBlock("PreviewTitle", backdrop, 80f, 54f, 1200f, 54f, "UI Rebuild Preview", 40, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("PreviewSub", backdrop, 80f, 108f, 1600f, 34f, "Title / HUD / BottomNav / Operate / Install / Economy / Review", 22, TextAnchor.MiddleLeft, SubtleText);

        RectTransform board = CreateTopLeftRect("PreviewBoard", backdrop, 60f, 170f, 2440f, 2940f);

        PlacePrefabPreview(board, "TitlePreview", PrefabRoot + "/Title/PF_UI_TitleScreen.prefab", 40f, 20f, 0.40f, new Vector2(0f, 1f), "타이틀");
        PlacePrefabPreview(board, "TopHudPreview", PrefabRoot + "/HUD/PF_UI_TopHUD.prefab", 1110f, 20f, 0.95f, new Vector2(0f, 1f), "상단 HUD");
        PlacePrefabPreview(board, "OperatePreview", PrefabRoot + "/Panels/PF_UI_OperatePanel.prefab", 40f, 860f, 0.54f, new Vector2(0f, 1f), "운영");
        PlacePrefabPreview(board, "InstallPreview", PrefabRoot + "/Panels/PF_UI_InstallPanel.prefab", 1320f, 860f, 0.54f, new Vector2(0f, 1f), "설치");
        PlacePrefabPreview(board, "EconomyPreview", PrefabRoot + "/Panels/PF_UI_EconomyPanel.prefab", 40f, 2040f, 0.54f, new Vector2(0f, 1f), "경제");
        PlacePrefabPreview(board, "ReviewPreview", PrefabRoot + "/Panels/PF_UI_ReviewPanel.prefab", 1320f, 2040f, 0.54f, new Vector2(0f, 1f), "리뷰");
        PlacePrefabPreview(board, "BottomNavPreview", PrefabRoot + "/BottomNav/PF_UI_BottomNav.prefab", 1110f, 2700f, 0.95f, new Vector2(0f, 1f), "하단 탭");

        SavePrefab(rootGo, prefabPath);
    }

    private static void PlacePrefabPreview(RectTransform parent, string name, string prefabPath, float x, float y, float scale, Vector2 pivot, string label)
    {
        RectTransform anchor = CreateTopLeftRect(name, parent, x, y, 0f, 0f);
        anchor.pivot = pivot;

        RectTransform frame = CreateTopLeftRect("Frame", anchor, 0f, 24f, 1120f, 1020f);
        AddSpriteImage(frame, "Common/UI_Common_SectionBox_M", Image.Type.Sliced);
        CreateTextBlock("Label", anchor, 0f, -16f, 260f, 28f, label, 22, TextAnchor.MiddleLeft, PanelText);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Missing prefab for preview: {prefabPath}");
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, frame.transform) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException($"Could not instantiate prefab: {prefabPath}");
        }

        RectTransform instanceRect = instance.GetComponent<RectTransform>();
        instanceRect.anchorMin = new Vector2(0f, 1f);
        instanceRect.anchorMax = new Vector2(0f, 1f);
        instanceRect.pivot = new Vector2(0f, 1f);
        instanceRect.anchoredPosition = new Vector2(40f, -56f);
        instanceRect.localScale = new Vector3(scale, scale, 1f);
    }

    private static void CreateInstallCard(RectTransform parent, string name, float x, float y, string spriteKey, string category, string title, string price, string actionLabel)
    {
        RectTransform card = CreateTopLeftRect(name, parent, x, y, 392f, 176f);
        AddSpriteImage(card, spriteKey, Image.Type.Simple);

        RectTransform iconSlot = CreateTopLeftRect("IconSlot", card, 18f, 18f, 88f, 88f);
        AddSpriteImage(iconSlot, "HUD/UI_HUD_IconSlot_Frame", Image.Type.Simple);
        CreateTextBlock("IconGlyph", iconSlot, 0f, 0f, 88f, 88f, category.Substring(0, 1), 34, TextAnchor.MiddleCenter, AccentText);

        CreateTextBlock("Category", card, 122f, 18f, 220f, 22f, category, 16, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("Title", card, 122f, 38f, 220f, 34f, title, 24, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("Price", card, 122f, 78f, 170f, 30f, price, 22, TextAnchor.MiddleLeft, AccentText);

        RectTransform statusButton = CreateTopLeftRect("StatusButton", card, 252f, 114f, 118f, 42f);
        AddSpriteButton(statusButton, "Panels/Install/UI_Install_StatusButton", actionLabel, 18);
    }

    private static void CreateEconomySummary(RectTransform parent, string name, float x, float y, string label, string value)
    {
        RectTransform box = CreateTopLeftRect(name, parent, x, y, 190f, 156f);
        AddSpriteImage(box, "Panels/Economy/UI_Economy_SummaryBox", Image.Type.Sliced);
        CreateTextBlock("Label", box, 0f, 24f, 190f, 24f, label, 18, TextAnchor.MiddleCenter, SubtleText);
        CreateTextBlock("Value", box, 0f, 60f, 190f, 34f, value, 24, TextAnchor.MiddleCenter, label == "지출" ? SubtleText : AccentText);
    }

    private static void CreateReviewSummary(RectTransform parent, string name, float x, float y, string label, string value)
    {
        RectTransform box = CreateTopLeftRect(name, parent, x, y, 190f, 156f);
        AddSpriteImage(box, "Panels/Review/UI_Review_SummaryBox", Image.Type.Sliced);
        CreateTextBlock("Label", box, 0f, 24f, 190f, 24f, label, 18, TextAnchor.MiddleCenter, SubtleText);
        CreateTextBlock("Value", box, 0f, 58f, 190f, 36f, value, 26, TextAnchor.MiddleCenter, AccentText);
    }

    private static void CreateMetricLabel(RectTransform parent, string label, string value, float x)
    {
        RectTransform block = CreateTopLeftRect($"Metric_{label}", parent, x, 34f, 170f, 116f);
        CreateTextBlock($"Label_{label}", block, 0f, 0f, 170f, 24f, label, 18, TextAnchor.MiddleCenter, SubtleText);
        CreateTextBlock($"Value_{label}", block, 0f, 34f, 170f, 42f, value, 28, TextAnchor.MiddleCenter, AccentText);
    }

    private static void CreateHeader(RectTransform parent, string title, string subtitle)
    {
        CreateTextBlock("HeaderTitle", parent, 58f, 46f, 340f, 42f, title, 34, TextAnchor.MiddleLeft, PanelText);
        CreateTextBlock("HeaderSubtitle", parent, 58f, 86f, 480f, 26f, subtitle, 18, TextAnchor.MiddleLeft, SubtleText);
    }

    private static void CreateSubTabs(RectTransform parent, float x, float y, string activeLabel, string secondaryLabel)
    {
        RectTransform active = CreateTopLeftRect("SubTabPrimary", parent, x, y, 170f, 68f);
        AddSpriteButton(active, "Common/UI_Common_Tab_M_Active", activeLabel, 22);

        RectTransform secondary = CreateTopLeftRect("SubTabSecondary", parent, x + 182f, y, 170f, 68f);
        AddSpriteButton(secondary, "Common/UI_Common_Tab_M_Secondary", secondaryLabel, 22);
    }

    private static void CreateHudInfoBox(Transform parent, string name, float x, float y, float width, float height, string label, string value)
    {
        RectTransform box = CreateTopLeftRect(name, parent, x, y, width, height);
        AddSpriteImage(box, "HUD/UI_HUD_InfoBox_Small", Image.Type.Sliced);
        RectTransform icon = CreateTopLeftRect("Icon", box, 8f, 8f, 42f, 42f);
        AddSpriteImage(icon, "HUD/UI_HUD_IconSlot_Frame", Image.Type.Simple);
        CreateTextBlock("IconGlyph", icon, 0f, 0f, 42f, 42f, label.Substring(0, 1), 18, TextAnchor.MiddleCenter, AccentText);
        CreateTextBlock("Label", box, 58f, 6f, width - 68f, 18f, label, 14, TextAnchor.MiddleLeft, SubtleText);
        CreateTextBlock("Value", box, 58f, 24f, width - 68f, 24f, value, 18, TextAnchor.MiddleLeft, PanelText);
    }

    private static void CreateHudActionButton(Transform parent, string name, float x, float y, string spriteKey, string label)
    {
        RectTransform button = CreateTopLeftRect(name, parent, x, y, 56f, 56f);
        AddSpriteImage(button, spriteKey, Image.Type.Simple);
        CreateTextBlock("Label", button, 0f, 58f, 56f, 20f, label, 12, TextAnchor.MiddleCenter, PanelText);
        button.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
    }

    private static void CreateHudSpeedChip(Transform parent, string name, float x, float y, string spriteKey, string label)
    {
        RectTransform chip = CreateTopLeftRect(name, parent, x, y, 70f, 42f);
        AddSpriteImage(chip, spriteKey, Image.Type.Sliced);
        CreateTextBlock("Label", chip, 0f, 0f, 70f, 42f, label, 16, TextAnchor.MiddleCenter, PanelText);
    }

    private static Image AddSpriteButton(RectTransform rect, string spriteKey, string label, int fontSize)
    {
        Image image = AddSpriteImage(rect, spriteKey, Image.Type.Sliced);
        var button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        CreateTextBlock("Label", rect, 0f, 0f, rect.sizeDelta.x, rect.sizeDelta.y, label, fontSize, TextAnchor.MiddleCenter, PanelText);
        return image;
    }

    private static Image AddSpriteImage(RectTransform rect, string relativeSpriteKey, Image.Type type)
    {
        string assetPath = $"{SpriteRoot}/{relativeSpriteKey}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            throw new InvalidOperationException($"Sprite not found: {assetPath}");
        }

        var image = rect.gameObject.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.type = type;
        image.raycastTarget = false;
        image.preserveAspect = type == Image.Type.Simple && rect.sizeDelta.x <= 120f && rect.sizeDelta.y <= 120f;
        return image;
    }

    private static Image AddSolidImage(RectTransform rect, Color color)
    {
        var image = rect.gameObject.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        return image;
    }

    private static Text CreateTextBlock(string name, RectTransform parent, float x, float y, float width, float height, string text, int fontSize, TextAnchor anchor, Color color, float lineSpacing = 1f)
    {
        RectTransform rect = CreateTopLeftRect(name, parent, x, y, width, height);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = LoadFont();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = anchor;
        label.alignByGeometry = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = false;
        label.color = color;
        label.lineSpacing = lineSpacing;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = new Vector2(1f, -1f);
        return label;
    }

    private static Font LoadFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            throw new InvalidOperationException($"Font not found: {FontPath}");
        }

        return font;
    }

    private static GameObject CreateRoot(string name, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        return root;
    }

    private static RectTransform CreateTopLeftRect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static RectTransform CreateCenteredRect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static RectTransform CreateStretchRect(string name, Transform parent, float left, float top, float right, float bottom)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    private static void SavePrefab(GameObject root, string prefabPath)
    {
        EnsureFolderChain(Path.GetDirectoryName(prefabPath)?.Replace("\\", "/") ?? PrefabRoot);

        if (File.Exists(prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
        UnityEngine.Object.DestroyImmediate(root);

        if (!success)
        {
            throw new InvalidOperationException($"Could not save prefab: {prefabPath}");
        }
    }

    private static void EnsureFolderChain(string assetFolderPath)
    {
        string[] parts = assetFolderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string ToAssetPath(string fullPath)
    {
        string normalized = fullPath.Replace("\\", "/");
        int assetsIndex = normalized.IndexOf("Assets/", StringComparison.Ordinal);
        return assetsIndex >= 0 ? normalized.Substring(assetsIndex) : normalized;
    }

    private static string GetSlotName(int index)
    {
        switch (index)
        {
            case 0:
                return "동네 헬스장";
            case 1:
                return "역세권 2호점";
            default:
                return "프리미엄 지점";
        }
    }

    private static string GetSlotMeta(int index)
    {
        switch (index)
        {
            case 0:
                return "부지 16x16  |  회원 124명";
            case 1:
                return "부지 8x8  |  저장 준비";
            default:
                return "부지 32x32  |  확장 후보";
        }
    }

    private static string GetBottomNavGlyph(int index)
    {
        switch (index)
        {
            case 0:
                return "운";
            case 1:
                return "설";
            case 2:
                return "경";
            default:
                return "리";
        }
    }

    private static void WriteReport(int modifiedSpriteCount)
    {
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "UIRebuildAssemblyReport.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? Path.Combine(Directory.GetCurrentDirectory(), "Temp"));

        string[] updatedPrefabs =
        {
            $"{PrefabRoot}/Title/PF_UI_TitleScreen.prefab",
            $"{PrefabRoot}/HUD/PF_UI_TopHUD.prefab",
            $"{PrefabRoot}/BottomNav/PF_UI_BottomNav.prefab",
            $"{PrefabRoot}/Panels/PF_UI_OperatePanel.prefab",
            $"{PrefabRoot}/Panels/PF_UI_InstallPanel.prefab",
            $"{PrefabRoot}/Panels/PF_UI_EconomyPanel.prefab",
            $"{PrefabRoot}/Panels/PF_UI_ReviewPanel.prefab",
            $"{PrefabRoot}/PF_UIRoot_Canvas.prefab"
        };

        var lines = new List<string>
        {
            $"ModifiedSpriteCount={modifiedSpriteCount}",
            $"BorderAssets={string.Join(", ", BorderAssetNames)}",
            $"UpdatedPrefabs={string.Join(", ", updatedPrefabs)}"
        };

        File.WriteAllLines(reportPath, lines);
    }
}
