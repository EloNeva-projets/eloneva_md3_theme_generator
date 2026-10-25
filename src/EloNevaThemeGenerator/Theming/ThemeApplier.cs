using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using FontFamily = System.Windows.Media.FontFamily;

namespace EloNevaThemeGenerator.Theming;

/// <summary>
/// Applique un thème en direct : les ressources sont écrites au premier niveau de Application.Resources,
/// ce qui prime sur les dictionnaires fusionnés ; tous les DynamicResource se mettent à jour.
/// </summary>
public static class ThemeApplier
{
    public static void Apply(ThemeTokens tokens)
    {
        var resources = Application.Current.Resources;

        // Mode et couleurs principales du toolkit (pour les éléments qui lisent le thème via PaletteHelper).
        var helper = new PaletteHelper();
        var theme = helper.GetTheme();
        theme.SetBaseTheme(tokens.IsDark ? BaseTheme.Dark : BaseTheme.Light);
        theme.SetPrimaryColor(tokens["Primary"]);
        theme.SetSecondaryColor(tokens["Secondary"]);
        helper.SetTheme(theme);

        foreach (var role in Md3Roles.All)
        {
            var color = tokens[role.Name];
            resources[role.ColorKey] = color;
            resources[role.BrushKey] = Frozen(color);
        }

        foreach (var (key, color) in tokens.ToolkitBrushes)
            resources[key] = Frozen(color);

        foreach (var (key, radius) in tokens.CornerRadii)
            resources[key] = radius;

        foreach (var (key, value) in tokens.Numbers)
            resources[key] = value;

        resources["Md3.FontFamily"] = (FontFamily)new FontFamilyConverter().ConvertFromInvariantString(tokens.FontFamily)!;
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
