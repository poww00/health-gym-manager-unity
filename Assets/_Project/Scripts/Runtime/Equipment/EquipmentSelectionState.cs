using System;

public static class EquipmentSelectionState
{
    public static EquipmentDefinition CurrentDefinition { get; private set; }

    public static event Action<EquipmentDefinition> OnDefinitionChanged;

    public static void Select(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (CurrentDefinition == definition)
        {
            return;
        }

        CurrentDefinition = definition;
        OnDefinitionChanged?.Invoke(CurrentDefinition);
    }

    public static bool HasSelection()
    {
        return CurrentDefinition != null;
    }

    public static string GetCurrentName()
    {
        return CurrentDefinition != null ? CurrentDefinition.DisplayName : "(선택 없음)";
    }
}