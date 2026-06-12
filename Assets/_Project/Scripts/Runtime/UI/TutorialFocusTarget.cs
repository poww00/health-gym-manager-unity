using UnityEngine;

public readonly struct TutorialFocusTarget
{
    public TutorialFocusTarget(
        TutorialFocusMode mode,
        Rect rect,
        bool allowFocusInteraction,
        string targetName,
        float padding = 0f)
    {
        Mode = mode;
        Rect = rect;
        AllowFocusInteraction = allowFocusInteraction;
        TargetName = targetName ?? string.Empty;
        Padding = padding;
    }

    public TutorialFocusMode Mode { get; }
    public Rect Rect { get; }
    public bool AllowFocusInteraction { get; }
    public string TargetName { get; }
    public float Padding { get; }

    public bool HasHole => Mode != TutorialFocusMode.None && Rect.width > 1f && Rect.height > 1f;

    public static TutorialFocusTarget None(string targetName = "")
    {
        return new TutorialFocusTarget(TutorialFocusMode.None, Rect.zero, false, targetName);
    }
}
