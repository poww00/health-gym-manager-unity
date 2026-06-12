using UnityEngine;

public readonly struct TutorialStepDefinition
{
    public TutorialStepDefinition(
        int stepIndex,
        string stepName,
        TutorialFocusMode focusMode,
        string targetName,
        bool allowFocusInteraction,
        Vector2 padding)
    {
        StepIndex = stepIndex;
        StepName = stepName ?? string.Empty;
        FocusMode = focusMode;
        TargetName = targetName ?? string.Empty;
        AllowFocusInteraction = allowFocusInteraction;
        Padding = padding;
    }

    public int StepIndex { get; }
    public string StepName { get; }
    public TutorialFocusMode FocusMode { get; }
    public string TargetName { get; }
    public bool AllowFocusInteraction { get; }
    public Vector2 Padding { get; }
}
