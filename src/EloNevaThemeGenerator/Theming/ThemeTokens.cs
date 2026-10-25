using System.Windows;
using System.Windows.Media;

namespace EloNevaThemeGenerator.Theming;

/// <summary>
/// Ensemble des ressources produites par un thème pour un mode (clair ou sombre) :
/// rôles MD3 (Md3.*), pinceaux du toolkit (MaterialDesign.Brush.*), formes et typographie.
/// Sert à la fois à l'application en direct et à l'export XAML.
/// </summary>
public sealed class ThemeTokens
{
    public required bool IsDark { get; init; }

    /// <summary>Rôles calculés depuis les couleurs sources, avant surcharge.</summary>
    public required IReadOnlyDictionary<string, Color> GeneratedRoles { get; init; }

    /// <summary>Rôles effectifs (surcharges manuelles appliquées).</summary>
    public required IReadOnlyDictionary<string, Color> Roles { get; init; }

    public required IReadOnlyList<KeyValuePair<string, Color>> ToolkitBrushes { get; init; }
    public required IReadOnlyList<KeyValuePair<string, CornerRadius>> CornerRadii { get; init; }
    public required IReadOnlyList<KeyValuePair<string, double>> Numbers { get; init; }
    public required string FontFamily { get; init; }

    /// <summary>Police Roboto embarquée par MaterialDesignThemes (inutile de l'installer).</summary>
    public const string RobotoSource = "pack://application:,,,/MaterialDesignThemes.Wpf;component/Resources/Roboto/#Roboto";

    public Color this[string role] => Roles[role];

    public static ThemeTokens Build(ThemeDefinition def, bool isDark)
    {
        var palettes = new Dictionary<PaletteKind, TonalPalette>
        {
            [PaletteKind.Primary] = TonalPalette.FromColor(ColorMath.ParseHex(def.PrimarySeed, Color.FromRgb(0x67, 0x50, 0xA4))),
            [PaletteKind.Secondary] = TonalPalette.FromColor(ColorMath.ParseHex(def.SecondarySeed, Color.FromRgb(0x62, 0x5B, 0x71))),
            [PaletteKind.Tertiary] = TonalPalette.FromColor(ColorMath.ParseHex(def.TertiarySeed, Color.FromRgb(0x7D, 0x52, 0x60))),
            [PaletteKind.Error] = TonalPalette.FromColor(ColorMath.ParseHex(def.ErrorSeed, Color.FromRgb(0xB3, 0x26, 0x1E))),
            [PaletteKind.Neutral] = TonalPalette.FromColor(ColorMath.ParseHex(def.NeutralSeed, Color.FromRgb(0x60, 0x5D, 0x62))),
            [PaletteKind.NeutralVariant] = TonalPalette.FromColor(ColorMath.ParseHex(def.NeutralVariantSeed, Color.FromRgb(0x60, 0x5D, 0x66))),
        };

        var generated = Md3Roles.All.ToDictionary(r => r.Name, r => palettes[r.Palette].Tone(r.Tone(isDark)));

        if (def.ExactPrimary && !isDark)
        {
            var seed = ColorMath.ParseHex(def.PrimarySeed, generated["Primary"]);
            generated["Primary"] = seed;
            generated["OnPrimary"] = ColorMath.Contrast(seed, Colors.White) >= ColorMath.Contrast(seed, palettes[PaletteKind.Primary].Tone(10))
                ? Colors.White
                : palettes[PaletteKind.Primary].Tone(10);
        }

        var roles = new Dictionary<string, Color>(generated);
        foreach (var (name, hex) in def.Overrides(isDark))
            if (roles.ContainsKey(name) && ColorMath.TryParseHex(hex, out var c))
                roles[name] = c;

        var p = palettes[PaletteKind.Primary];
        var s = palettes[PaletteKind.Secondary];

        return new ThemeTokens
        {
            IsDark = isDark,
            GeneratedRoles = generated,
            Roles = roles,
            ToolkitBrushes = BuildToolkitBrushes(roles,
                primaryDark: isDark ? p.Tone(70) : p.Tone(30),
                onPrimaryDark: isDark ? p.Tone(10) : p.Tone(100),
                secondaryDark: isDark ? s.Tone(70) : s.Tone(30),
                onSecondaryDark: isDark ? s.Tone(10) : s.Tone(100)),
            CornerRadii =
            [
                new("Md3.Shape.Button", new CornerRadius(def.ButtonCornerRadius)),
                new("Md3.Shape.Field", new CornerRadius(def.FieldCornerRadius)),
                new("Md3.Shape.FieldFilled", new CornerRadius(def.FieldCornerRadius, def.FieldCornerRadius, 0, 0)),
                new("Md3.Shape.Small", new CornerRadius(def.SmallCornerRadius)),
                new("Md3.Shape.CardRadius", new CornerRadius(def.CardCornerRadius)),
            ],
            Numbers =
            [
                new("Md3.Shape.Card", def.CardCornerRadius),
                new("Md3.FontSize", def.FontSize),
            ],
            FontFamily = def.FontFamily == "Roboto" ? RobotoSource : def.FontFamily,
        };
    }

    /// <summary>Correspondance rôles MD3 → pinceaux attendus par les styles MaterialDesignInXamlToolkit.</summary>
    private static List<KeyValuePair<string, Color>> BuildToolkitBrushes(
        IReadOnlyDictionary<string, Color> r,
        Color primaryDark, Color onPrimaryDark, Color secondaryDark, Color onSecondaryDark)
    {
        var onSurface = r["OnSurface"];
        Color Disabled() => ColorMath.WithAlpha(onSurface, 0.38);
        Color Hover(string surface) => ColorMath.Mix(r[surface], onSurface, 0.08);

        var map = new List<(string Key, Color Color)>
        {
            ("Primary", r["Primary"]),
            ("Primary.Foreground", r["OnPrimary"]),
            ("Primary.Light", r["PrimaryContainer"]),
            ("Primary.Light.Foreground", r["OnPrimaryContainer"]),
            ("Primary.Dark", primaryDark),
            ("Primary.Dark.Foreground", onPrimaryDark),
            ("Secondary", r["Secondary"]),
            ("Secondary.Foreground", r["OnSecondary"]),
            ("Secondary.Light", r["SecondaryContainer"]),
            ("Secondary.Light.Foreground", r["OnSecondaryContainer"]),
            ("Secondary.Dark", secondaryDark),
            ("Secondary.Dark.Foreground", onSecondaryDark),

            ("Background", r["Surface"]),
            ("Foreground", onSurface),
            ("ForegroundLight", r["OnSurfaceVariant"]),
            ("ValidationError", r["Error"]),

            ("Badged.DarkBackground", r["Error"]),
            ("Badged.DarkForeground", r["OnError"]),
            ("Badged.LightBackground", r["ErrorContainer"]),
            ("Badged.LightForeground", r["OnErrorContainer"]),

            ("Button.FlatClick", ColorMath.WithAlpha(r["Primary"], 0.12)),
            ("Button.Ripple", ColorMath.WithAlpha(r["OnPrimary"], 0.6)),
            ("Button.FlatRipple", ColorMath.WithAlpha(r["Primary"], 0.4)),

            ("Card.Background", r["SurfaceContainerLow"]),
            ("Card.Border", r["OutlineVariant"]),

            ("CheckBox.Disabled", Disabled()),
            ("CheckBox.Off", r["OnSurfaceVariant"]),
            ("CheckBox.UncheckedBorder", r["OnSurfaceVariant"]),

            ("Chip.Background", r["SurfaceContainerHigh"]),
            ("Chip.OutlineBorder", r["Outline"]),

            ("ColorZone.DarkBackground", r["InverseSurface"]),
            ("ColorZone.DarkForeground", r["InverseOnSurface"]),
            ("ColorZone.LightBackground", r["SurfaceContainer"]),
            ("ColorZone.LightForeground", onSurface),

            ("ComboBox.Border", r["OnSurfaceVariant"]),
            ("ComboBox.Disabled", Disabled()),
            ("ComboBox.DropDown.Background", r["SurfaceContainer"]),
            ("ComboBox.FilledBackground", r["SurfaceContainerHighest"]),
            ("ComboBox.HoverBackground", Hover("SurfaceContainerHighest")),
            ("ComboBox.HoverBorder", onSurface),
            ("ComboBox.OutlineBorder", r["Outline"]),
            ("ComboBox.OutlineInactiveBorder", r["Outline"]),
            ("ComboBox.Popup.DarkBackground", r["SurfaceContainer"]),
            ("ComboBox.Popup.DarkForeground", onSurface),
            ("ComboBox.Popup.LightBackground", r["SurfaceContainer"]),
            ("ComboBox.Popup.LightForeground", onSurface),

            ("DataGrid.Border", r["OutlineVariant"]),
            ("DataGrid.ButtonPressed", r["SurfaceContainerHighest"]),
            ("DataGrid.ColumnHeaderForeground", r["OnSurfaceVariant"]),
            ("DataGrid.ComboBoxHover", r["SurfaceContainerHigh"]),
            ("DataGrid.ComboBoxSelected", r["SecondaryContainer"]),
            ("DataGrid.PopupBorder", r["OutlineVariant"]),
            ("DataGrid.RowHoverBackground", Hover("Surface")),
            ("DataGrid.Selected", r["SecondaryContainer"]),

            ("GridSplitter.Background", r["OutlineVariant"]),
            ("GridSplitter.PreviewBackground", r["Outline"]),

            ("Header.Foreground", onSurface),

            ("ListBox.SegmentedBackground", r["SurfaceContainerHigh"]),
            ("ListBoxItem.Border", r["OutlineVariant"]),
            ("ListBoxItem.Selected", r["SecondaryContainer"]),
            ("ListView.Hover", Hover("Surface")),
            ("ListView.Selected", r["SecondaryContainer"]),
            ("ListView.Separator", r["OutlineVariant"]),

            ("PasswordBox.Border", r["OnSurfaceVariant"]),
            ("PasswordBox.FilledBackground", r["SurfaceContainerHighest"]),
            ("PasswordBox.HoverBackground", Hover("SurfaceContainerHighest")),
            ("PasswordBox.HoverBorder", onSurface),
            ("PasswordBox.OutlineBorder", r["Outline"]),
            ("PasswordBox.OutlineInactiveBorder", r["Outline"]),

            ("RadioButton.Border", r["OnSurfaceVariant"]),
            ("RadioButton.Checked", r["Primary"]),
            ("RadioButton.Chip.CheckedBackground", r["SecondaryContainer"]),
            ("RadioButton.Disabled", Disabled()),
            ("RadioButton.Outline", r["Outline"]),

            ("ScrollBar.ActiveBackground", r["Outline"]),
            ("ScrollBar.Foreground", r["OutlineVariant"]),
            ("ScrollBar.RepeatButtonBackground", r["SurfaceContainerHighest"]),

            ("Separator.Background", r["OutlineVariant"]),

            ("SnackBar.Background", r["InverseSurface"]),
            ("SnackBar.MouseOver", ColorMath.Mix(r["InverseSurface"], r["InverseOnSurface"], 0.08)),
            ("SnackBar.Ripple", r["InverseOnSurface"]),

            ("StatusBar.Background", r["SurfaceContainer"]),
            ("StatusBar.Foreground", onSurface),

            ("TabControl.Divider", r["OutlineVariant"]),

            ("TextBox.Border", r["OnSurfaceVariant"]),
            ("TextBox.DisabledBackground", ColorMath.WithAlpha(onSurface, 0.04)),
            ("TextBox.FilledBackground", r["SurfaceContainerHighest"]),
            ("TextBox.HoverBackground", Hover("SurfaceContainerHighest")),
            ("TextBox.HoverBorder", onSurface),
            ("TextBox.OutlineBorder", r["Outline"]),
            ("TextBox.OutlineInactiveBorder", r["Outline"]),

            ("ToggleButton.Background", r["SurfaceContainerHighest"]),
            ("ToggleButton.Foreground", onSurface),
            ("ToggleButton.Switch.TrackOffBackground", r["SurfaceContainerHighest"]),

            ("ToolBar.Background", r["SurfaceContainer"]),
            ("ToolBar.Item.Background", r["SurfaceContainer"]),
            ("ToolBar.Item.Foreground", r["OnSurfaceVariant"]),
            ("ToolBar.Overflow.Border", r["OutlineVariant"]),
            ("ToolBar.Separator", r["OutlineVariant"]),
            ("ToolBar.Thumb.Foreground", r["Outline"]),

            ("ToolTip.Background", r["InverseSurface"]),
        };

        return map.Select(m => new KeyValuePair<string, Color>("MaterialDesign.Brush." + m.Key, m.Color)).ToList();
    }
}
