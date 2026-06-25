using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EquipmentSoAssetGenerator
{
    private const string RootPath = "Assets/_Project/ScriptableObjects/Equipment";
    private const string GradesPath = RootPath + "/Grades";
    private const string TypesPath = RootPath + "/Types";
    private const string CatalogPath = RootPath + "/EquipmentCatalog.asset";
    private const string LegacyDefinitionsPath = "Assets/_Project/Data/SO/GeneratedEquipment";
    private const string TestSandboxScenePath = "Assets/_Project/Scenes/TestSandbox.unity";

    [MenuItem("Tools/헬스장 운영기/Generate Equipment SO Assets")]
    public static void GenerateAll()
    {
        EnsureFolders();

        List<GradeSeed> gradeSeeds = CreateGradeSeeds();
        List<TypeSeed> typeSeeds = CreateTypeSeeds();

        List<EquipmentGradeDefinition> grades = new List<EquipmentGradeDefinition>();
        foreach (GradeSeed seed in gradeSeeds)
        {
            grades.Add(UpsertGrade(seed));
        }

        List<EquipmentTypeDefinition> types = new List<EquipmentTypeDefinition>();
        foreach (TypeSeed seed in typeSeeds)
        {
            types.Add(UpsertType(seed));
        }

        List<EquipmentDefinition> legacyDefinitions = new List<EquipmentDefinition>();
        foreach (TypeSeed type in typeSeeds.OrderBy(seed => seed.UnlockStage).ThenBy(seed => seed.SortOrder))
        {
            foreach (GradeSeed grade in gradeSeeds.OrderBy(seed => seed.SortOrder))
            {
                legacyDefinitions.Add(UpsertLegacyDefinition(type, grade));
            }
        }

        EquipmentCatalogDefinition catalog = LoadOrCreate<EquipmentCatalogDefinition>(CatalogPath);
        catalog.Configure(types, grades);
        EditorUtility.SetDirty(catalog);

        AssignTestSandboxCatalog(legacyDefinitions);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static EquipmentGradeDefinition UpsertGrade(GradeSeed seed)
    {
        EquipmentGradeDefinition definition = LoadOrCreate<EquipmentGradeDefinition>($"{GradesPath}/{seed.AssetName}.asset");
        definition.Configure(
            seed.Id,
            seed.DisplayName,
            seed.Grade,
            seed.Description,
            seed.PriceMultiplier,
            seed.IncomeMultiplier,
            seed.SatisfactionMultiplier,
            seed.MaintenanceCostMultiplier,
            seed.RepairCostMultiplier,
            seed.BreakdownRateMultiplier,
            seed.PrestigeMultiplier,
            seed.LowMannerCustomerMultiplier,
            seed.VipCustomerMultiplier);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static EquipmentTypeDefinition UpsertType(TypeSeed seed)
    {
        EquipmentTypeDefinition definition = LoadOrCreate<EquipmentTypeDefinition>($"{TypesPath}/{seed.AssetName}.asset");
        definition.Configure(
            seed.Id,
            seed.DisplayName,
            seed.SourceCategory,
            seed.UnlockStage,
            seed.SizeX,
            seed.SizeY,
            seed.BasePrice,
            seed.BaseMonthlyMaintenanceCost,
            seed.BaseIncomePerUse,
            seed.BaseUseDurationSeconds,
            seed.BaseSatisfaction,
            seed.BaseCleanlinessCost,
            seed.BaseDurability,
            seed.BaseBreakdownRate,
            seed.RequiredGymLevel,
            seed.SpriteKey,
            seed.AnimationKey,
            string.Empty);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static EquipmentDefinition UpsertLegacyDefinition(TypeSeed type, GradeSeed grade)
    {
        string assetName = $"{type.AssetName}_{grade.AssetSuffix}";
        EquipmentDefinition definition = LoadOrCreate<EquipmentDefinition>($"{LegacyDefinitionsPath}/{assetName}.asset");
        SerializedObject serialized = new SerializedObject(definition);

        SetString(serialized, "equipmentId", $"{type.Id}_{grade.IdSuffix}");
        SetString(serialized, "displayName", $"{grade.DisplayPrefix} {type.DisplayName}");
        SetEnum(serialized, "category", (int)type.LegacyCategory);
        SetEnum(serialized, "brandTier", (int)grade.BrandTier);
        SetInt(serialized, "width", type.SizeX);
        SetInt(serialized, "height", type.SizeY);

        SetInt(serialized, "installCost", ToRawValue(type.BasePrice, grade.PriceMultiplier, EquipmentBrandTierRules.GetPurchaseMultiplier(grade.BrandTier)));
        SetInt(serialized, "prestigeBonus", ToRawValue(Mathf.Max(1, type.BaseSatisfaction), grade.PrestigeMultiplier, EquipmentBrandTierRules.GetPrestigeMultiplier(grade.BrandTier)));
        SetInt(serialized, "memberCapacityBonus", Mathf.Max(1, Mathf.RoundToInt(type.BaseSatisfaction * grade.SatisfactionMultiplier)));
        SetInt(serialized, "electricityCostPerDay", ToRawValue(type.BaseCleanlinessCost * 10, grade.MaintenanceCostMultiplier, EquipmentBrandTierRules.GetRunningCostMultiplier(grade.BrandTier)));
        SetInt(serialized, "maintenanceCostPerDay", ToRawValue(type.BaseMonthlyMaintenanceCost, grade.MaintenanceCostMultiplier, EquipmentBrandTierRules.GetRunningCostMultiplier(grade.BrandTier)));
        SetInt(serialized, "ptDemandBonus", ToRawValue(type.BaseIncomePerUse, grade.IncomeMultiplier, EquipmentBrandTierRules.GetPtDemandMultiplier(grade.BrandTier)));
        ClearIconReference(serialized);
        SetColor(serialized, "debugColor", grade.DebugColor);
        SetVector2(serialized, "customerUseOffset", GetLegacyCustomerUseOffset(type.Id));
        SetBool(serialized, "useForegroundSprite", UsesLegacyForegroundSprite(type.Id));
        SetVector2(serialized, "foregroundOffset", Vector2.zero);
        SetBool(serialized, "unlockedByDefault", true);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void AssignTestSandboxCatalog(IReadOnlyList<EquipmentDefinition> definitions)
    {
        if (definitions == null || definitions.Count <= 0)
        {
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(TestSandboxScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(TestSandboxScenePath, OpenSceneMode.Single);
        }

        EquipmentCatalog catalog = Object.FindFirstObjectByType<EquipmentCatalog>();
        if (catalog == null)
        {
            Debug.LogWarning($"EquipmentCatalog not found in {TestSandboxScenePath}. Generated assets were created, but the scene catalog was not updated.");
            return;
        }

        SerializedObject serialized = new SerializedObject(catalog);
        SerializedProperty definitionList = serialized.FindProperty("definitions");
        if (definitionList == null)
        {
            Debug.LogWarning("EquipmentCatalog.definitions serialized field was not found.");
            return;
        }

        definitionList.arraySize = definitions.Count;
        for (int i = 0; i < definitions.Count; i++)
        {
            definitionList.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
        }

        SerializedProperty defaultDefinition = serialized.FindProperty("defaultDefinition");
        if (defaultDefinition != null)
        {
            defaultDefinition.objectReferenceValue = definitions[0];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static int ToRawValue(int baseValue, float targetMultiplier, float existingTierMultiplier)
    {
        if (existingTierMultiplier <= 0f)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseValue * targetMultiplier));
        }

        return Mathf.Max(0, Mathf.RoundToInt(baseValue * targetMultiplier / existingTierMultiplier));
    }

    private static void SetString(SerializedObject serialized, string propertyName, string value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetEnum(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = value;
        }
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetVector2(SerializedObject serialized, string propertyName, Vector2 value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.vector2Value = value;
        }
    }

    private static void SetColor(SerializedObject serialized, string propertyName, Color value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.colorValue = value;
        }
    }

    private static Vector2 GetLegacyCustomerUseOffset(string typeId)
    {
        switch (typeId)
        {
            case "treadmill":
            case "exercise_bike":
                return new Vector2(0.35f, 0f);
            case "leg_press":
                return new Vector2(0.55f, 0.55f);
            default:
                return Vector2.zero;
        }
    }

    private static bool UsesLegacyForegroundSprite(string typeId)
    {
        switch (typeId)
        {
            case "treadmill":
            case "bench_press":
                return true;
            default:
                return false;
        }
    }

    private static void ClearIconReference(SerializedObject serialized)
    {
        SerializedProperty iconReference = serialized.FindProperty("iconReference");
        if (iconReference == null)
        {
            return;
        }

        ClearRelativeString(iconReference, "m_AssetGUID");
        ClearRelativeString(iconReference, "m_SubObjectName");
        ClearRelativeString(iconReference, "m_SubObjectGUID");

        SerializedProperty subObjectType = iconReference.FindPropertyRelative("m_SubObjectType");
        if (subObjectType != null)
        {
            subObjectType.stringValue = string.Empty;
        }

        SerializedProperty editorAssetChanged = iconReference.FindPropertyRelative("m_EditorAssetChanged");
        if (editorAssetChanged != null)
        {
            editorAssetChanged.intValue = 0;
        }
    }

    private static void ClearRelativeString(SerializedProperty property, string relativeName)
    {
        SerializedProperty relative = property.FindPropertyRelative(relativeName);
        if (relative != null)
        {
            relative.stringValue = string.Empty;
        }
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/_Project", "ScriptableObjects");
        EnsureFolder("Assets/_Project/ScriptableObjects", "Equipment");
        EnsureFolder(RootPath, "Grades");
        EnsureFolder(RootPath, "Types");
        EnsureFolder("Assets/_Project/Data/SO", "GeneratedEquipment");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static List<GradeSeed> CreateGradeSeeds()
    {
        return new List<GradeSeed>
        {
            new GradeSeed("Grade_B", "B", "b", "grade_b", "중고", "중고", EquipmentGrade.B, EquipmentBrandTier.B, "저렴하지만 만족도가 낮고 고장이 잦은 중고 기구", 0.6f, 0.85f, 0.8f, 0.85f, 0.7f, 1.6f, 0.75f, 1.25f, 0.75f, new Color(0.62f, 0.45f, 0.28f, 1f), 0),
            new GradeSeed("Grade_A", "A", "a", "grade_a", "새상품", "새상품", EquipmentGrade.A, EquipmentBrandTier.A, "표준적인 가격과 성능의 일반 새상품 기구", 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, new Color(0.95f, 0.72f, 0.35f, 1f), 1),
            new GradeSeed("Grade_S", "S", "s", "grade_s", "고급", "고급", EquipmentGrade.S, EquipmentBrandTier.S, "만족도와 품격이 높은 고급 브랜드 기구", 1.8f, 1.2f, 1.25f, 1.2f, 1.25f, 0.75f, 1.35f, 0.8f, 1.35f, new Color(0.45f, 0.72f, 0.92f, 1f), 2),
            new GradeSeed("Grade_SS", "SS", "ss", "grade_ss", "외제", "외제", EquipmentGrade.SS, EquipmentBrandTier.SS, "비싸지만 상류층 유입과 만족도가 크게 오르는 외제 명품 기구", 3.2f, 1.45f, 1.6f, 1.5f, 1.8f, 0.55f, 1.8f, 0.6f, 1.8f, new Color(0.95f, 0.55f, 0.88f, 1f), 3)
        };
    }

    private static List<TypeSeed> CreateTypeSeeds()
    {
        return new List<TypeSeed>
        {
            new TypeSeed("Treadmill", "treadmill", "러닝머신", EquipmentCategory.Cardio, EquipmentCategory.Cardio, EquipmentUnlockStage.Start, 2, 1, 1200, 120, 80, 12f, 3, 2, 100, 1f, 1, "treadmill", "treadmill", 0),
            new TypeSeed("ExerciseBike", "exercise_bike", "실내 자전거", EquipmentCategory.Cardio, EquipmentCategory.Cardio, EquipmentUnlockStage.Start, 2, 1, 900, 80, 60, 10f, 2, 1, 100, 0.9f, 1, "exercise_bike", "exercise_bike", 1),
            new TypeSeed("DumbbellRack", "dumbbell_rack", "덤벨 랙", EquipmentCategory.StrengthFreeWeight, EquipmentCategory.Pull, EquipmentUnlockStage.Start, 1, 1, 700, 60, 50, 9f, 2, 1, 120, 0.7f, 1, "dumbbell_rack", "dumbbell_rack", 2),
            new TypeSeed("BenchPress", "bench_press", "벤치 프레스", EquipmentCategory.StrengthFreeWeight, EquipmentCategory.Push, EquipmentUnlockStage.Start, 2, 1, 1400, 130, 90, 14f, 3, 2, 110, 0.85f, 1, "bench_press", "bench_press", 3),
            new TypeSeed("YogaMat", "yoga_mat", "요가 매트", EquipmentCategory.Flexibility, EquipmentCategory.Recovery, EquipmentUnlockStage.Start, 1, 1, 500, 30, 35, 8f, 4, 2, 80, 0.4f, 1, "yoga_mat", "yoga_mat", 4),
            new TypeSeed("Locker", "locker", "락커", EquipmentCategory.Convenience, EquipmentCategory.Other, EquipmentUnlockStage.Start, 1, 1, 600, 40, 10, 4f, 3, 1, 120, 0.4f, 1, "locker", "locker", 5),
            new TypeSeed("WaterDispenser", "water_dispenser", "정수기", EquipmentCategory.Convenience, EquipmentCategory.Other, EquipmentUnlockStage.Start, 1, 1, 400, 50, 5, 3f, 2, 1, 90, 0.5f, 1, "water_dispenser", "water_dispenser", 6),
            new TypeSeed("RowingMachine", "rowing_machine", "로잉머신", EquipmentCategory.Cardio, EquipmentCategory.Cardio, EquipmentUnlockStage.Early, 2, 1, 2200, 200, 130, 15f, 4, 2, 100, 0.9f, 2, "rowing_machine", "rowing_machine", 7),
            new TypeSeed("LatPulldown", "lat_pulldown", "랫풀다운", EquipmentCategory.StrengthMachine, EquipmentCategory.Pull, EquipmentUnlockStage.Early, 2, 1, 2400, 220, 140, 15f, 4, 2, 105, 0.85f, 2, "lat_pulldown", "lat_pulldown", 8),
            new TypeSeed("ShowerBooth", "shower_booth", "샤워 부스", EquipmentCategory.Convenience, EquipmentCategory.Other, EquipmentUnlockStage.Early, 1, 1, 2000, 180, 30, 12f, 10, 5, 90, 0.8f, 2, "shower_booth", "shower_booth", 9),
            new TypeSeed("LegPress", "leg_press", "레그 프레스", EquipmentCategory.StrengthMachine, EquipmentCategory.Legs, EquipmentUnlockStage.Mid, 2, 2, 3000, 280, 170, 18f, 5, 3, 110, 0.9f, 3, "leg_press", "leg_press", 10),
            new TypeSeed("SmithMachine", "smith_machine", "스미스 머신", EquipmentCategory.StrengthMachine, EquipmentCategory.Push, EquipmentUnlockStage.Mid, 2, 2, 2800, 260, 160, 18f, 5, 3, 105, 0.9f, 3, "smith_machine", "smith_machine", 11),
            new TypeSeed("CableMachine", "cable_machine", "케이블 머신", EquipmentCategory.StrengthMachine, EquipmentCategory.Pull, EquipmentUnlockStage.Mid, 2, 2, 3500, 320, 190, 20f, 6, 3, 100, 0.95f, 3, "cable_machine", "cable_machine", 12),
            new TypeSeed("ChestPress", "chest_press", "체스트 프레스", EquipmentCategory.StrengthMachine, EquipmentCategory.Push, EquipmentUnlockStage.Mid, 2, 1, 3000, 290, 165, 16f, 5, 2, 105, 0.85f, 3, "chest_press", "chest_press", 13),
            new TypeSeed("MassageChair", "massage_chair", "마사지 체어", EquipmentCategory.Recovery, EquipmentCategory.Recovery, EquipmentUnlockStage.Mid, 1, 1, 2500, 180, 80, 14f, 9, 2, 90, 0.9f, 3, "massage_chair", "massage_chair", 14),
            new TypeSeed("ProteinBar", "protein_bar", "자판기", EquipmentCategory.Convenience, EquipmentCategory.Other, EquipmentUnlockStage.Mid, 2, 1, 2800, 200, 150, 8f, 5, 3, 100, 0.7f, 3, "protein_bar", "protein_bar", 15),
            new TypeSeed("InBodyMachine", "inbody_machine", "인바디", EquipmentCategory.Service, EquipmentCategory.Other, EquipmentUnlockStage.Mid, 1, 1, 2200, 120, 70, 8f, 6, 1, 95, 0.7f, 3, "inbody_machine", "inbody_machine", 16),
            new TypeSeed("StairMill", "stair_mill", "천국의 계단", EquipmentCategory.Cardio, EquipmentCategory.Cardio, EquipmentUnlockStage.Late, 2, 1, 4200, 420, 230, 20f, 6, 3, 95, 1f, 4, "stair_mill", "stair_mill", 17),
            new TypeSeed("PowerRack", "power_rack", "파워랙", EquipmentCategory.StrengthFreeWeight, EquipmentCategory.Legs, EquipmentUnlockStage.Late, 2, 2, 4500, 450, 250, 22f, 7, 3, 130, 0.75f, 4, "power_rack", "power_rack", 18),
            new TypeSeed("HipAbduction", "hip_abduction", "힙 어브덕션", EquipmentCategory.StrengthMachine, EquipmentCategory.Legs, EquipmentUnlockStage.Late, 2, 1, 3200, 300, 175, 16f, 5, 2, 100, 0.85f, 4, "hip_abduction", "hip_abduction", 19)
        };
    }

    private sealed class GradeSeed
    {
        public GradeSeed(string assetName, string assetSuffix, string idSuffix, string id, string displayName, string displayPrefix, EquipmentGrade grade, EquipmentBrandTier brandTier, string description, float priceMultiplier, float incomeMultiplier, float satisfactionMultiplier, float maintenanceCostMultiplier, float repairCostMultiplier, float breakdownRateMultiplier, float prestigeMultiplier, float lowMannerCustomerMultiplier, float vipCustomerMultiplier, Color debugColor, int sortOrder)
        {
            AssetName = assetName;
            AssetSuffix = assetSuffix;
            IdSuffix = idSuffix;
            Id = id;
            DisplayName = displayName;
            DisplayPrefix = displayPrefix;
            Grade = grade;
            BrandTier = brandTier;
            Description = description;
            PriceMultiplier = priceMultiplier;
            IncomeMultiplier = incomeMultiplier;
            SatisfactionMultiplier = satisfactionMultiplier;
            MaintenanceCostMultiplier = maintenanceCostMultiplier;
            RepairCostMultiplier = repairCostMultiplier;
            BreakdownRateMultiplier = breakdownRateMultiplier;
            PrestigeMultiplier = prestigeMultiplier;
            LowMannerCustomerMultiplier = lowMannerCustomerMultiplier;
            VipCustomerMultiplier = vipCustomerMultiplier;
            DebugColor = debugColor;
            SortOrder = sortOrder;
        }

        public string AssetName { get; }
        public string AssetSuffix { get; }
        public string IdSuffix { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string DisplayPrefix { get; }
        public EquipmentGrade Grade { get; }
        public EquipmentBrandTier BrandTier { get; }
        public string Description { get; }
        public float PriceMultiplier { get; }
        public float IncomeMultiplier { get; }
        public float SatisfactionMultiplier { get; }
        public float MaintenanceCostMultiplier { get; }
        public float RepairCostMultiplier { get; }
        public float BreakdownRateMultiplier { get; }
        public float PrestigeMultiplier { get; }
        public float LowMannerCustomerMultiplier { get; }
        public float VipCustomerMultiplier { get; }
        public Color DebugColor { get; }
        public int SortOrder { get; }
    }

    private sealed class TypeSeed
    {
        public TypeSeed(string assetName, string id, string displayName, EquipmentCategory sourceCategory, EquipmentCategory legacyCategory, EquipmentUnlockStage unlockStage, int sizeX, int sizeY, int basePrice, int baseMonthlyMaintenanceCost, int baseIncomePerUse, float baseUseDurationSeconds, int baseSatisfaction, int baseCleanlinessCost, int baseDurability, float baseBreakdownRate, int requiredGymLevel, string spriteKey, string animationKey, int sortOrder)
        {
            AssetName = assetName;
            Id = id;
            DisplayName = displayName;
            SourceCategory = sourceCategory;
            LegacyCategory = legacyCategory;
            UnlockStage = unlockStage;
            SizeX = sizeX;
            SizeY = sizeY;
            BasePrice = basePrice;
            BaseMonthlyMaintenanceCost = baseMonthlyMaintenanceCost;
            BaseIncomePerUse = baseIncomePerUse;
            BaseUseDurationSeconds = baseUseDurationSeconds;
            BaseSatisfaction = baseSatisfaction;
            BaseCleanlinessCost = baseCleanlinessCost;
            BaseDurability = baseDurability;
            BaseBreakdownRate = baseBreakdownRate;
            RequiredGymLevel = requiredGymLevel;
            SpriteKey = spriteKey;
            AnimationKey = animationKey;
            SortOrder = sortOrder;
        }

        public string AssetName { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public EquipmentCategory SourceCategory { get; }
        public EquipmentCategory LegacyCategory { get; }
        public EquipmentUnlockStage UnlockStage { get; }
        public int SizeX { get; }
        public int SizeY { get; }
        public int BasePrice { get; }
        public int BaseMonthlyMaintenanceCost { get; }
        public int BaseIncomePerUse { get; }
        public float BaseUseDurationSeconds { get; }
        public int BaseSatisfaction { get; }
        public int BaseCleanlinessCost { get; }
        public int BaseDurability { get; }
        public float BaseBreakdownRate { get; }
        public int RequiredGymLevel { get; }
        public string SpriteKey { get; }
        public string AnimationKey { get; }
        public int SortOrder { get; }
    }
}
