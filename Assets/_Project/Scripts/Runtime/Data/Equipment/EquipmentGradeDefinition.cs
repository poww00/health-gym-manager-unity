using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentGradeDefinition",
    menuName = "Toss Gym/Equipment Grade Definition",
    order = 10)]
public sealed class EquipmentGradeDefinition : ScriptableObject
{
    [SerializeField] private string id = "grade_b";
    [SerializeField] private string displayName = "Grade";
    [SerializeField] private EquipmentGrade grade = EquipmentGrade.B;
    [TextArea]
    [SerializeField] private string description = string.Empty;
    [SerializeField] private float priceMultiplier = 1f;
    [SerializeField] private float incomeMultiplier = 1f;
    [SerializeField] private float satisfactionMultiplier = 1f;
    [SerializeField] private float maintenanceCostMultiplier = 1f;
    [SerializeField] private float repairCostMultiplier = 1f;
    [SerializeField] private float breakdownRateMultiplier = 1f;
    [SerializeField] private float prestigeMultiplier = 1f;
    [SerializeField] private float lowMannerCustomerMultiplier = 1f;
    [SerializeField] private float vipCustomerMultiplier = 1f;

    public string Id => id;
    public string DisplayName => displayName;
    public EquipmentGrade Grade => grade;
    public string Description => description;
    public float PriceMultiplier => priceMultiplier;
    public float IncomeMultiplier => incomeMultiplier;
    public float SatisfactionMultiplier => satisfactionMultiplier;
    public float MaintenanceCostMultiplier => maintenanceCostMultiplier;
    public float RepairCostMultiplier => repairCostMultiplier;
    public float BreakdownRateMultiplier => breakdownRateMultiplier;
    public float PrestigeMultiplier => prestigeMultiplier;
    public float LowMannerCustomerMultiplier => lowMannerCustomerMultiplier;
    public float VipCustomerMultiplier => vipCustomerMultiplier;

    public void Configure(
        string id,
        string displayName,
        EquipmentGrade grade,
        string description,
        float priceMultiplier,
        float incomeMultiplier,
        float satisfactionMultiplier,
        float maintenanceCostMultiplier,
        float repairCostMultiplier,
        float breakdownRateMultiplier,
        float prestigeMultiplier,
        float lowMannerCustomerMultiplier,
        float vipCustomerMultiplier)
    {
        this.id = id;
        this.displayName = displayName;
        this.grade = grade;
        this.description = description;
        this.priceMultiplier = priceMultiplier;
        this.incomeMultiplier = incomeMultiplier;
        this.satisfactionMultiplier = satisfactionMultiplier;
        this.maintenanceCostMultiplier = maintenanceCostMultiplier;
        this.repairCostMultiplier = repairCostMultiplier;
        this.breakdownRateMultiplier = breakdownRateMultiplier;
        this.prestigeMultiplier = prestigeMultiplier;
        this.lowMannerCustomerMultiplier = lowMannerCustomerMultiplier;
        this.vipCustomerMultiplier = vipCustomerMultiplier;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = name;
        }

        priceMultiplier = Mathf.Max(0f, priceMultiplier);
        incomeMultiplier = Mathf.Max(0f, incomeMultiplier);
        satisfactionMultiplier = Mathf.Max(0f, satisfactionMultiplier);
        maintenanceCostMultiplier = Mathf.Max(0f, maintenanceCostMultiplier);
        repairCostMultiplier = Mathf.Max(0f, repairCostMultiplier);
        breakdownRateMultiplier = Mathf.Max(0f, breakdownRateMultiplier);
        prestigeMultiplier = Mathf.Max(0f, prestigeMultiplier);
        lowMannerCustomerMultiplier = Mathf.Max(0f, lowMannerCustomerMultiplier);
        vipCustomerMultiplier = Mathf.Max(0f, vipCustomerMultiplier);
    }
#endif
}
