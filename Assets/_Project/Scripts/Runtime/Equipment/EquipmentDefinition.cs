using UnityEngine;
using UnityEngine.AddressableAssets;

public enum EquipmentCategory
{
    [InspectorName("카디오")]
    Cardio,
    [InspectorName("푸쉬")]
    Push,
    [InspectorName("풀")]
    Pull,
    [InspectorName("하체")]
    Legs,
    [InspectorName("회복")]
    Recovery,
    [InspectorName("기타")]
    Other
}

public enum EquipmentBrandTier
{
    B,
    A,
    S,
    SS
}

public static class EquipmentBrandTierRules
{
    public static string GetShortLabel(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return "A";
            case EquipmentBrandTier.S: return "S";
            case EquipmentBrandTier.SS: return "SS";
            default: return "B";
        }
    }

    public static float GetPurchaseMultiplier(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 1.45f;
            case EquipmentBrandTier.S: return 2.10f;
            case EquipmentBrandTier.SS: return 3.10f;
            default: return 1.00f;
        }
    }

    public static float GetPrestigeMultiplier(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 1.20f;
            case EquipmentBrandTier.S: return 1.55f;
            case EquipmentBrandTier.SS: return 2.00f;
            default: return 1.00f;
        }
    }

    public static float GetRunningCostMultiplier(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 1.15f;
            case EquipmentBrandTier.S: return 1.35f;
            case EquipmentBrandTier.SS: return 1.60f;
            default: return 1.00f;
        }
    }

    public static float GetPtDemandMultiplier(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 1.10f;
            case EquipmentBrandTier.S: return 1.25f;
            case EquipmentBrandTier.SS: return 1.45f;
            default: return 1.00f;
        }
    }

    public static int GetBaseInstallationMinutes(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 3;
            case EquipmentBrandTier.S: return 10;
            case EquipmentBrandTier.SS: return 30;
            default: return 0;
        }
    }

    /// <summary>
    /// [프로토타입 1차]
    /// 브랜드 등급이 직접 회원 계층을 구현하진 않지만,
    /// 이후 GymEconomyManager에서 유입 품질/부가매출 품질 보정으로 쓸 수 있는 0~1 점수.
    /// B는 기본값, A/S/SS가 높을수록 "매너/객단가" 잠재력이 높다고 본다.
    /// </summary>
    public static float GetQualityScore01(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 0.35f;
            case EquipmentBrandTier.S: return 0.70f;
            case EquipmentBrandTier.SS: return 1.00f;
            default: return 0.00f;
        }
    }

    public static float GetBreakdownChancePerUse(EquipmentBrandTier tier)
    {
        switch (tier)
        {
            case EquipmentBrandTier.A: return 0.035f;
            case EquipmentBrandTier.S: return 0.02f;
            case EquipmentBrandTier.SS: return 0.01f;
            default: return 0.05f; // B tier (5%)
        }
    }
}

[CreateAssetMenu(
    fileName = "EquipmentDefinition",
    menuName = "Toss Gym/Equipment Definition",
    order = 0)]
public sealed class EquipmentDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string equipmentId = "equipment_001";
    [SerializeField] private string displayName = "기본 기구";
    [SerializeField] private EquipmentCategory category = EquipmentCategory.Other;
    [SerializeField] private EquipmentBrandTier brandTier = EquipmentBrandTier.B;

    [Header("Placement")]
    [SerializeField] private int width = 2;
    [SerializeField] private int height = 2;
    [SerializeField] private int installCost = 3000;

    [Header("Economy / Growth (다음 단계용 기초 데이터)")]
    [SerializeField] private int prestigeBonus = 8;
    [SerializeField] private int memberCapacityBonus = 8;
    [SerializeField] private int electricityCostPerDay = 250;
    [SerializeField] private int maintenanceCostPerDay = 180;
    [SerializeField] private int ptDemandBonus = 0;

    [Header("Visual / UI (비주얼 설정)")]
    [SerializeField] private AssetReferenceT<Sprite> iconReference;
    [SerializeField] private Color debugColor = new Color(1f, 0.65f, 0.2f, 1f);

    [Header("Unlock")]
    [SerializeField] private bool unlockedByDefault = true;

    public string EquipmentId => equipmentId;
    public string DisplayName => displayName;
    public EquipmentCategory Category => category;
    public EquipmentBrandTier BrandTier => brandTier;
    public string BrandTierLabel => EquipmentBrandTierRules.GetShortLabel(brandTier);
    public float BrandQualityScore01 => EquipmentBrandTierRules.GetQualityScore01(brandTier);
    public int BaseInstallationMinutes => EquipmentBrandTierRules.GetBaseInstallationMinutes(brandTier);

    public int Width => Mathf.Max(1, width);
    public int Height => Mathf.Max(1, height);

    /// <summary>
    /// [프로토타입 1차]
    /// 인스펙터의 installCost는 B등급 기준 원가로 간주하고,
    /// A/S/SS는 코드 규칙으로만 가격을 올린다.
    /// 현재 기존 SO 자산은 전부 기본값 B라서 기존 밸런스를 깨지 않는다.
    /// </summary>
    public int InstallCost => ScaleRounded(installCost, EquipmentBrandTierRules.GetPurchaseMultiplier(brandTier));

    /// <summary>
    /// 브랜드가 높을수록 같은 기구라도 헬스장 품격에 더 크게 기여.
    /// </summary>
    public int PrestigeBonus => ScaleRounded(prestigeBonus, EquipmentBrandTierRules.GetPrestigeMultiplier(brandTier));

    /// <summary>
    /// 1차 단계에선 수용력 자체는 브랜드보다 "종류/개수" 영향이 더 크다고 보고 그대로 둔다.
    /// </summary>
    public int MemberCapacityBonus => Mathf.Max(0, memberCapacityBonus);

    public int ElectricityCostPerDay => ScaleRounded(electricityCostPerDay, EquipmentBrandTierRules.GetRunningCostMultiplier(brandTier));
    public int MaintenanceCostPerDay => ScaleRounded(maintenanceCostPerDay, EquipmentBrandTierRules.GetRunningCostMultiplier(brandTier));
    public int PtDemandBonus => ScaleRounded(ptDemandBonus, EquipmentBrandTierRules.GetPtDemandMultiplier(brandTier));

    public AssetReferenceT<Sprite> IconReference => iconReference;
    public Color DebugColor => debugColor;
    public bool UnlockedByDefault => unlockedByDefault;

    private static int ScaleRounded(int baseValue, float multiplier)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseValue) * Mathf.Max(0f, multiplier)));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(equipmentId))
        {
            equipmentId = name;
        }

        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        installCost = Mathf.Max(0, installCost);
        prestigeBonus = Mathf.Max(0, prestigeBonus);
        memberCapacityBonus = Mathf.Max(0, memberCapacityBonus);
        electricityCostPerDay = Mathf.Max(0, electricityCostPerDay);
        maintenanceCostPerDay = Mathf.Max(0, maintenanceCostPerDay);
        ptDemandBonus = Mathf.Max(0, ptDemandBonus);
    }
#endif
}
