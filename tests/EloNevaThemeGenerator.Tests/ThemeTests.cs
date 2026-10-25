using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using EloNevaThemeGenerator.Theming;
using static EloNevaThemeGenerator.Tests.WpfHost;

namespace EloNevaThemeGenerator.Tests;

public class ThemeTests
{
    public static TheoryData<string, bool> Seeds => new()
    {
        { "#6750A4", false }, { "#6750A4", true },
        { "#9CC93A", false }, { "#9CC93A", true },
        { "#0061A4", false }, { "#0061A4", true },
        { "#BA1A1A", false }, { "#212121", true },
    };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generated_roles_are_readable(string seed, bool isDark)
    {
        var def = new ThemeDefinition { PrimarySeed = seed };
        def.DeriveSeedsFromPrimary();
        var t = ThemeTokens.Build(def, isDark);

        foreach (var (bg, fg) in new[]
                 {
                     ("Primary", "OnPrimary"), ("PrimaryContainer", "OnPrimaryContainer"),
                     ("Secondary", "OnSecondary"), ("SecondaryContainer", "OnSecondaryContainer"),
                     ("Tertiary", "OnTertiary"), ("Error", "OnError"),
                     ("Surface", "OnSurface"), ("SurfaceContainerHighest", "OnSurfaceVariant"),
                 })
        {
            var contrast = ColorMath.Contrast(t[bg], t[fg]);
            Assert.True(contrast >= 4.5, $"{bg}/{fg} = {contrast:0.00} ({seed}, sombre={isDark})");
        }

        // Surfaces : plus claires en mode clair, sombres en mode sombre.
        Assert.Equal(isDark, ColorMath.RelativeLuminance(t["Surface"]) < 0.1);
    }

    [Fact]
    public void Exact_primary_keeps_brand_color_in_light_mode()
    {
        var def = new ThemeDefinition { PrimarySeed = "#9CC93A", ExactPrimary = true };
        var t = ThemeTokens.Build(def, isDark: false);
        Assert.Equal("#9CC93A", ColorMath.ToHex(t["Primary"]));
        Assert.True(ColorMath.Contrast(t["Primary"], t["OnPrimary"]) >= 4.5);
    }

    [Fact]
    public void Overrides_apply_only_to_their_mode()
    {
        var def = new ThemeDefinition();
        def.LightOverrides["Surface"] = "#FF0000";
        Assert.Equal(Colors.Red, ThemeTokens.Build(def, false)["Surface"]);
        Assert.NotEqual(Colors.Red, ThemeTokens.Build(def, true)["Surface"]);
        Assert.NotEqual(Colors.Red, ThemeTokens.Build(def, false).GeneratedRoles["Surface"]);
    }

    [Fact]
    public void Theme_round_trips_through_json()
    {
        var def = new ThemeDefinition { Name = "Essai", IsDark = true, PrimarySeed = "#0061A4", ButtonCornerRadius = 6, FontFamily = "Segoe UI" };
        def.DarkOverrides["Outline"] = "#123456";
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.md3theme.json");
        try
        {
            def.Save(path);
            var loaded = ThemeDefinition.Load(path);
            Assert.Equal("Essai", loaded.Name);
            Assert.True(loaded.IsDark);
            Assert.Equal("#0061A4", loaded.PrimarySeed);
            Assert.Equal(6, loaded.ButtonCornerRadius);
            Assert.Equal("Segoe UI", loaded.FontFamily);
            Assert.Equal("#123456", loaded.DarkOverrides["Outline"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Exported_xaml_parses_and_contains_all_tokens(bool isDark) => Run(() =>
    {
        var def = new ThemeDefinition { Name = "Mon thème & co", PrimarySeed = "#9CC93A", FontFamily = "Roboto" };
        def.DeriveSeedsFromPrimary();
        var tokens = ThemeTokens.Build(def, isDark);

        var dictionary = (ResourceDictionary)XamlReader.Parse(XamlThemeExporter.Generate(def, isDark));

        foreach (var role in Md3Roles.All)
        {
            Assert.Equal(tokens[role.Name], (Color)dictionary[role.ColorKey]);
            Assert.Equal(tokens[role.Name], ((SolidColorBrush)dictionary[role.BrushKey]).Color);
        }
        foreach (var (key, color) in tokens.ToolkitBrushes)
            Assert.Equal(color, ((SolidColorBrush)dictionary[key]).Color);

        Assert.Equal(new CornerRadius(def.ButtonCornerRadius), (CornerRadius)dictionary["Md3.Shape.Button"]);
        Assert.Equal(new CornerRadius(4, 4, 0, 0), (CornerRadius)dictionary["Md3.Shape.FieldFilled"]);
        Assert.Equal(def.CardCornerRadius, (double)dictionary["Md3.Shape.Card"]);
        Assert.Equal(def.FontSize, (double)dictionary["Md3.FontSize"]);
        Assert.Contains("Roboto", ((FontFamily)dictionary["Md3.FontFamily"]).Source);
        Assert.Single(dictionary.MergedDictionaries);
    });

    [Fact]
    public void Export_writes_light_and_dark_files()
    {
        var folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var files = XamlThemeExporter.ExportBoth(new ThemeDefinition { Name = "Thème EloNeva" }, folder);
            Assert.Equal(["Thème_EloNeva.Light.xaml", "Thème_EloNeva.Dark.xaml"], files.Select(Path.GetFileName));
            Assert.All(files, f => Assert.True(new FileInfo(f).Length > 5000));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
