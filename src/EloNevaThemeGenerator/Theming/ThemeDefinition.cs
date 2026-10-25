using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace EloNevaThemeGenerator.Theming;

/// <summary>
/// Définition éditable d'un thème : couleurs sources, surcharges de rôles, formes et typographie.
/// C'est ce qui est enregistré (.md3theme.json) ; le XAML est généré à partir d'elle.
/// </summary>
public sealed class ThemeDefinition
{
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "Mon thème MD3";
    public bool IsDark { get; set; }

    public string PrimarySeed { get; set; } = "#6750A4";
    public string SecondarySeed { get; set; } = "#625B71";
    public string TertiarySeed { get; set; } = "#7D5260";
    public string ErrorSeed { get; set; } = "#B3261E";
    public string NeutralSeed { get; set; } = "#605D62";
    public string NeutralVariantSeed { get; set; } = "#605D66";

    /// <summary>En clair, le rôle Primary reprend exactement la couleur source (couleur de marque).</summary>
    public bool ExactPrimary { get; set; }

    /// <summary>Rôles forcés manuellement (nom du rôle → #RRGGBB), par mode.</summary>
    public Dictionary<string, string> LightOverrides { get; set; } = new();
    public Dictionary<string, string> DarkOverrides { get; set; } = new();

    public double ButtonCornerRadius { get; set; } = 20;
    public double FieldCornerRadius { get; set; } = 4;
    public double CardCornerRadius { get; set; } = 12;
    public double SmallCornerRadius { get; set; } = 8;

    public string FontFamily { get; set; } = "Roboto";
    public double FontSize { get; set; } = 14;

    public Dictionary<string, string> Overrides(bool isDark) => isDark ? DarkOverrides : LightOverrides;

    /// <summary>Recalcule secondaire, tertiaire et neutres à partir de la couleur primaire (règles « Tonal Spot »).</summary>
    public void DeriveSeedsFromPrimary()
    {
        var p = ColorMath.ToLch(ColorMath.ParseHex(PrimarySeed, Colors.MediumPurple));
        SecondarySeed = ColorMath.ToHex(ColorMath.FromTone(p.H, Math.Min(p.C, 16), 40));
        TertiarySeed = ColorMath.ToHex(ColorMath.FromTone((p.H + 60) % 360, Math.Min(Math.Max(p.C, 24), 32), 40));
        NeutralSeed = ColorMath.ToHex(ColorMath.FromTone(p.H, 4, 40));
        NeutralVariantSeed = ColorMath.ToHex(ColorMath.FromTone(p.H, 8, 40));
    }

    public ThemeDefinition Clone() => JsonSerializer.Deserialize<ThemeDefinition>(JsonSerializer.Serialize(this, JsonOptions), JsonOptions)!;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));

    public static ThemeDefinition Load(string path) =>
        JsonSerializer.Deserialize<ThemeDefinition>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidDataException("Fichier de thème vide ou invalide.");
}
