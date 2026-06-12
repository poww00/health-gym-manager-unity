using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentTypeDefinition",
    menuName = "Toss Gym/Equipment Type Definition",
    order = 11)]
public sealed class EquipmentTypeDefinition : ScriptableObject
{
    [SerializeField] private string id = "equipment_type";
    [SerializeField] private string displayName = "Equipment Type";
    [SerializeField] private EquipmentCategory category = EquipmentCategory.Cardio;
    [SerializeField] private EquipmentUnlockStage unlockStage = EquipmentUnlockStage.Start;
    [SerializeField] private int sizeX = 1;
    [SerializeField] private int sizeY = 1;
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private int baseMonthlyMaintenanceCost = 100;
    [SerializeField] private int baseIncomePerUse = 50;
    [SerializeField] private float baseUseDurationSeconds = 10f;
    [SerializeField] private int baseSatisfaction = 1;
    [SerializeField] private int baseCleanlinessCost = 1;
    [SerializeField] private int baseDurability = 100;
    [SerializeField] private float baseBreakdownRate = 1f;
    [SerializeField] private int requiredGymLevel = 1;
    [SerializeField] private string spriteKey = string.Empty;
    [SerializeField] private string animationKey = string.Empty;
    [TextArea]
    [SerializeField] private string description = string.Empty;

    public string Id => id;
    public string DisplayName => displayName;
    public EquipmentCategory Category => category;
    public EquipmentUnlockStage UnlockStage => unlockStage;
    public int SizeX => Mathf.Max(1, sizeX);
    public int SizeY => Mathf.Max(1, sizeY);
    public int BasePrice => Mathf.Max(0, basePrice);
    public int BaseMonthlyMaintenanceCost => Mathf.Max(0, baseMonthlyMaintenanceCost);
    public int BaseIncomePerUse => Mathf.Max(0, baseIncomePerUse);
    public float BaseUseDurationSeconds => Mathf.Max(0f, baseUseDurationSeconds);
    public int BaseSatisfaction => Mathf.Max(0, baseSatisfaction);
    public int BaseCleanlinessCost => Mathf.Max(0, baseCleanlinessCost);
    public int BaseDurability => Mathf.Max(0, baseDurability);
    public float BaseBreakdownRate => Mathf.Max(0f, baseBreakdownRate);
    public int RequiredGymLevel => Mathf.Max(1, requiredGymLevel);
    public string SpriteKey => spriteKey;
    public string AnimationKey => animationKey;
    public string Description => description;

    public void Configure(
        string id,
        string displayName,
        EquipmentCategory category,
        EquipmentUnlockStage unlockStage,
        int sizeX,
        int sizeY,
        int basePrice,
        int baseMonthlyMaintenanceCost,
        int baseIncomePerUse,
        float baseUseDurationSeconds,
        int baseSatisfaction,
        int baseCleanlinessCost,
        int baseDurability,
        float baseBreakdownRate,
        int requiredGymLevel,
        string spriteKey,
        string animationKey,
        string description)
    {
        this.id = id;
        this.displayName = displayName;
        this.category = category;
        this.unlockStage = unlockStage;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.basePrice = basePrice;
        this.baseMonthlyMaintenanceCost = baseMonthlyMaintenanceCost;
        this.baseIncomePerUse = baseIncomePerUse;
        this.baseUseDurationSeconds = baseUseDurationSeconds;
        this.baseSatisfaction = baseSatisfaction;
        this.baseCleanlinessCost = baseCleanlinessCost;
        this.baseDurability = baseDurability;
        this.baseBreakdownRate = baseBreakdownRate;
        this.requiredGymLevel = requiredGymLevel;
        this.spriteKey = spriteKey;
        this.animationKey = animationKey;
        this.description = description;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = name;
        }

        sizeX = Mathf.Max(1, sizeX);
        sizeY = Mathf.Max(1, sizeY);
        basePrice = Mathf.Max(0, basePrice);
        baseMonthlyMaintenanceCost = Mathf.Max(0, baseMonthlyMaintenanceCost);
        baseIncomePerUse = Mathf.Max(0, baseIncomePerUse);
        baseUseDurationSeconds = Mathf.Max(0f, baseUseDurationSeconds);
        baseSatisfaction = Mathf.Max(0, baseSatisfaction);
        baseCleanlinessCost = Mathf.Max(0, baseCleanlinessCost);
        baseDurability = Mathf.Max(0, baseDurability);
        baseBreakdownRate = Mathf.Max(0f, baseBreakdownRate);
        requiredGymLevel = Mathf.Max(1, requiredGymLevel);
    }
#endif
}
