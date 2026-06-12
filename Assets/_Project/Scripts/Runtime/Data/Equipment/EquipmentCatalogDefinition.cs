using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EquipmentCatalog",
    menuName = "Toss Gym/Equipment Catalog Definition",
    order = 12)]
public sealed class EquipmentCatalogDefinition : ScriptableObject
{
    [SerializeField] private List<EquipmentTypeDefinition> equipmentTypes = new List<EquipmentTypeDefinition>();
    [SerializeField] private List<EquipmentGradeDefinition> gradeDefinitions = new List<EquipmentGradeDefinition>();

    public IReadOnlyList<EquipmentTypeDefinition> EquipmentTypes => equipmentTypes;
    public IReadOnlyList<EquipmentGradeDefinition> GradeDefinitions => gradeDefinitions;

    public void Configure(
        IReadOnlyList<EquipmentTypeDefinition> equipmentTypes,
        IReadOnlyList<EquipmentGradeDefinition> gradeDefinitions)
    {
        this.equipmentTypes = equipmentTypes != null
            ? new List<EquipmentTypeDefinition>(equipmentTypes)
            : new List<EquipmentTypeDefinition>();
        this.gradeDefinitions = gradeDefinitions != null
            ? new List<EquipmentGradeDefinition>(gradeDefinitions)
            : new List<EquipmentGradeDefinition>();
    }
}
