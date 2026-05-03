using UnityEngine;

public enum GameUiTone
{
    Surface,
    Accent,
    AccentAlt,
    Positive,
    Warning,
    Danger
}

public sealed class GameUiTheme
{
    public Vector2 ReferenceResolution { get; } = new Vector2(1080f, 1920f);
    public Font Font { get; }

    public Color ScreenBack { get; } = new Color(0.23f, 0.33f, 0.50f, 0.98f);
    public Color PanelBack { get; } = new Color(0.31f, 0.22f, 0.13f, 1f);
    public Color PanelFill { get; } = new Color(0.96f, 0.92f, 0.78f, 1f);
    public Color PanelFillAlt { get; } = new Color(0.90f, 0.87f, 0.73f, 1f);
    public Color PanelRaised { get; } = new Color(0.43f, 0.56f, 0.72f, 1f);
    public Color Outline { get; } = new Color(0.19f, 0.15f, 0.10f, 1f);
    public Color Shadow { get; } = new Color(0.10f, 0.08f, 0.06f, 0.46f);
    public Color Overlay { get; } = new Color(0.06f, 0.07f, 0.10f, 0.60f);
    public Color Ink { get; } = new Color(0.15f, 0.11f, 0.07f, 1f);
    public Color MutedInk { get; } = new Color(0.30f, 0.23f, 0.15f, 1f);
    public Color BrightInk { get; } = new Color(1.00f, 0.99f, 0.95f, 1f);
    public Color Accent { get; } = new Color(0.34f, 0.67f, 0.31f, 1f);
    public Color AccentAlt { get; } = new Color(0.31f, 0.49f, 0.82f, 1f);
    public Color Warning { get; } = new Color(0.92f, 0.72f, 0.21f, 1f);
    public Color Danger { get; } = new Color(0.85f, 0.40f, 0.32f, 1f);
    public Color Positive { get; } = new Color(0.39f, 0.73f, 0.36f, 1f);
    public Color TabIdle { get; } = new Color(0.90f, 0.83f, 0.64f, 1f);
    public Color TabIdleAlt { get; } = new Color(0.89f, 0.85f, 0.72f, 1f);
    public Color Divider { get; } = new Color(0.31f, 0.24f, 0.16f, 0.22f);

    public GameUiTheme(Font font)
    {
        Font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ConfigureFontTexture(Font);
    }

    public static GameUiTheme CreateDefault()
    {
        return new GameUiTheme(FindPreferredFont());
    }

    private static Font FindPreferredFont()
    {
        Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            Font font = loadedFonts[i];
            if (font == null || string.IsNullOrEmpty(font.name))
            {
                continue;
            }

            if (font.name.ToLowerInvariant().Contains("neodgm"))
            {
                return font;
            }
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void ConfigureFontTexture(Font font)
    {
        if (font == null || font.material == null || font.material.mainTexture == null)
        {
            return;
        }

        font.material.mainTexture.filterMode = FilterMode.Point;
        font.material.mainTexture.anisoLevel = 0;
    }

    public Color GetToneFill(GameUiTone tone)
    {
        switch (tone)
        {
            case GameUiTone.Accent:
                return Accent;
            case GameUiTone.AccentAlt:
                return AccentAlt;
            case GameUiTone.Positive:
                return Positive;
            case GameUiTone.Warning:
                return Warning;
            case GameUiTone.Danger:
                return Danger;
            default:
                return TabIdle;
        }
    }

    public Color GetToneInk(GameUiTone tone)
    {
        switch (tone)
        {
            case GameUiTone.Accent:
            case GameUiTone.AccentAlt:
            case GameUiTone.Positive:
            case GameUiTone.Danger:
                return BrightInk;
            default:
                return Ink;
        }
    }
}
