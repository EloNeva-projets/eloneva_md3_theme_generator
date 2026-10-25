namespace EloNevaThemeGenerator.Theming;

public enum PaletteKind
{
    Primary,
    Secondary,
    Tertiary,
    Error,
    Neutral,
    NeutralVariant
}

/// <summary>Rôle de couleur Material 3 et tonalités utilisées en clair / sombre.</summary>
public sealed record Md3Role(string Name, string Group, PaletteKind Palette, double LightTone, double DarkTone)
{
    /// <summary>Clé de la couleur : « Md3.Color.Primary ».</summary>
    public string ColorKey => $"Md3.Color.{Name}";

    /// <summary>Clé du pinceau : « Md3.Brush.Primary ».</summary>
    public string BrushKey => $"Md3.Brush.{Name}";

    public double Tone(bool isDark) => isDark ? DarkTone : LightTone;
}

/// <summary>Table des rôles Material 3 (spécification « color roles », schéma Tonal Spot).</summary>
public static class Md3Roles
{
    public static readonly IReadOnlyList<Md3Role> All =
    [
        new("Primary", "Primaire", PaletteKind.Primary, 40, 80),
        new("OnPrimary", "Primaire", PaletteKind.Primary, 100, 20),
        new("PrimaryContainer", "Primaire", PaletteKind.Primary, 90, 30),
        new("OnPrimaryContainer", "Primaire", PaletteKind.Primary, 10, 90),
        new("InversePrimary", "Primaire", PaletteKind.Primary, 80, 40),

        new("Secondary", "Secondaire", PaletteKind.Secondary, 40, 80),
        new("OnSecondary", "Secondaire", PaletteKind.Secondary, 100, 20),
        new("SecondaryContainer", "Secondaire", PaletteKind.Secondary, 90, 30),
        new("OnSecondaryContainer", "Secondaire", PaletteKind.Secondary, 10, 90),

        new("Tertiary", "Tertiaire", PaletteKind.Tertiary, 40, 80),
        new("OnTertiary", "Tertiaire", PaletteKind.Tertiary, 100, 20),
        new("TertiaryContainer", "Tertiaire", PaletteKind.Tertiary, 90, 30),
        new("OnTertiaryContainer", "Tertiaire", PaletteKind.Tertiary, 10, 90),

        new("Error", "Erreur", PaletteKind.Error, 40, 80),
        new("OnError", "Erreur", PaletteKind.Error, 100, 20),
        new("ErrorContainer", "Erreur", PaletteKind.Error, 90, 30),
        new("OnErrorContainer", "Erreur", PaletteKind.Error, 10, 90),

        new("Background", "Surfaces", PaletteKind.Neutral, 98, 6),
        new("OnBackground", "Surfaces", PaletteKind.Neutral, 10, 90),
        new("Surface", "Surfaces", PaletteKind.Neutral, 98, 6),
        new("OnSurface", "Surfaces", PaletteKind.Neutral, 10, 90),
        new("SurfaceDim", "Surfaces", PaletteKind.Neutral, 87, 6),
        new("SurfaceBright", "Surfaces", PaletteKind.Neutral, 98, 24),
        new("SurfaceContainerLowest", "Surfaces", PaletteKind.Neutral, 100, 4),
        new("SurfaceContainerLow", "Surfaces", PaletteKind.Neutral, 96, 10),
        new("SurfaceContainer", "Surfaces", PaletteKind.Neutral, 94, 12),
        new("SurfaceContainerHigh", "Surfaces", PaletteKind.Neutral, 92, 17),
        new("SurfaceContainerHighest", "Surfaces", PaletteKind.Neutral, 90, 22),
        new("SurfaceVariant", "Surfaces", PaletteKind.NeutralVariant, 90, 30),
        new("OnSurfaceVariant", "Surfaces", PaletteKind.NeutralVariant, 30, 80),
        new("InverseSurface", "Surfaces", PaletteKind.Neutral, 20, 90),
        new("InverseOnSurface", "Surfaces", PaletteKind.Neutral, 95, 20),

        new("Outline", "Contours", PaletteKind.NeutralVariant, 50, 60),
        new("OutlineVariant", "Contours", PaletteKind.NeutralVariant, 80, 30),
        new("Shadow", "Contours", PaletteKind.Neutral, 0, 0),
        new("Scrim", "Contours", PaletteKind.Neutral, 0, 0),
    ];

    public static readonly IReadOnlyDictionary<string, Md3Role> ByName = All.ToDictionary(r => r.Name);
}
