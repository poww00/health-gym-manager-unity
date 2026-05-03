using System;
using UnityEngine;

[Serializable]
public class PlacedObjectSaveData
{
    public int anchorX;
    public int anchorY;
    public int width;
    public int height;
    public string equipmentId;
    public string displayName;
    public int installCost;

    public bool isBroken;
    public int usageCount;

    public bool isUnderConstruction;
    public long constructionEndTimeTicks;

    [NonSerialized] public EquipmentDefinition runtimeDefinition;

    public void ApplyDefinitionSnapshot(EquipmentDefinition definition)
    {
        runtimeDefinition = definition;

        if (definition == null)
        {
            return;
        }

        equipmentId = definition.EquipmentId;
        displayName = definition.DisplayName;
        width = definition.Width;
        height = definition.Height;
        installCost = definition.InstallCost;
    }

    public static PlacedObjectSaveData Clone(PlacedObjectSaveData source)
    {
        if (source == null)
        {
            return null;
        }

        return new PlacedObjectSaveData
        {
            anchorX = source.anchorX,
            anchorY = source.anchorY,
            width = source.width,
            height = source.height,
            equipmentId = source.equipmentId,
            displayName = source.displayName,
            installCost = source.installCost,
            isBroken = source.isBroken,
            usageCount = source.usageCount,
            isUnderConstruction = source.isUnderConstruction,
            constructionEndTimeTicks = source.constructionEndTimeTicks,
            runtimeDefinition = source.runtimeDefinition,
        };
    }
}