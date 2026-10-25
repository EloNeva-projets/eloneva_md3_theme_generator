using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace EloNevaThemeGenerator.Theming;

/// <summary>Génère le ResourceDictionary XAML d'un thème (un fichier par mode).</summary>
public static class XamlThemeExporter
{
    public static string Generate(ThemeDefinition def, bool isDark)
    {
        var tokens = ThemeTokens.Build(def, isDark);
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine($"""
            <!--
                Thème Material Design 3 « {Escape(def.Name)} » ({(isDark ? "sombre" : "clair")})
                Généré par EloNeva MD3 Theme Generator le {DateTime.Now:dd/MM/yyyy HH:mm}.

                Utilisation dans App.xaml (l'ordre importe) :
                  <ResourceDictionary.MergedDictionaries>
                      <ResourceDictionary Source="pack://application:,,,/EloNeva.Md3.Controls;component/Themes/Components.xaml" />
                      <ResourceDictionary Source="Themes/{FileName(def, isDark)}" />
                  </ResourceDictionary.MergedDictionaries>
                Puis, dans les fenêtres : FontFamily="{"{"}DynamicResource Md3.FontFamily{"}"}" FontSize="{"{"}DynamicResource Md3.FontSize{"}"}".
            -->
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:sys="clr-namespace:System;assembly=mscorlib"
                                xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

                <ResourceDictionary.MergedDictionaries>
                    <md:CustomColorTheme BaseTheme="{(isDark ? "Dark" : "Light")}" PrimaryColor="{ColorMath.ToHex(tokens["Primary"])}" SecondaryColor="{ColorMath.ToHex(tokens["Secondary"])}" />
                </ResourceDictionary.MergedDictionaries>

                <!-- ===== Rôles de couleur Material 3 ===== -->
            """);

        string? group = null;
        foreach (var role in Md3Roles.All)
        {
            if (role.Group != group)
            {
                group = role.Group;
                sb.AppendLine($"    <!-- {group} -->");
            }
            var hex = ColorMath.ToHex(tokens[role.Name]);
            sb.AppendLine($"    <Color x:Key=\"{role.ColorKey}\">{hex}</Color>");
            sb.AppendLine($"    <SolidColorBrush x:Key=\"{role.BrushKey}\" Color=\"{{StaticResource {role.ColorKey}}}\" />");
        }

        sb.AppendLine();
        sb.AppendLine("    <!-- ===== Formes ===== -->");
        foreach (var (key, r) in tokens.CornerRadii)
            sb.AppendLine($"    <CornerRadius x:Key=\"{key}\">{r.TopLeft.ToString(inv)},{r.TopRight.ToString(inv)},{r.BottomRight.ToString(inv)},{r.BottomLeft.ToString(inv)}</CornerRadius>");

        sb.AppendLine();
        sb.AppendLine("    <!-- ===== Typographie et valeurs ===== -->");
        sb.AppendLine($"    <FontFamily x:Key=\"Md3.FontFamily\">{Escape(tokens.FontFamily)}</FontFamily>");
        foreach (var (key, value) in tokens.Numbers)
            sb.AppendLine($"    <sys:Double x:Key=\"{key}\">{value.ToString(inv)}</sys:Double>");

        sb.AppendLine();
        sb.AppendLine("    <!-- ===== Pinceaux MaterialDesignInXamlToolkit dérivés des rôles MD3 ===== -->");
        foreach (var (key, color) in tokens.ToolkitBrushes)
            sb.AppendLine($"    <SolidColorBrush x:Key=\"{key}\" Color=\"{ColorMath.ToHex(color)}\" />");

        sb.AppendLine("</ResourceDictionary>");
        return sb.ToString();
    }

    public static string FileName(ThemeDefinition def, bool isDark) =>
        $"{SafeName(def.Name)}.{(isDark ? "Dark" : "Light")}.xaml";

    /// <summary>Écrit les variantes claire et sombre dans le dossier ; retourne les chemins créés.</summary>
    public static IReadOnlyList<string> ExportBoth(ThemeDefinition def, string folder)
    {
        var paths = new List<string>();
        foreach (var dark in new[] { false, true })
        {
            var path = Path.Combine(folder, FileName(def, dark));
            File.WriteAllText(path, Generate(def, dark), new UTF8Encoding(false));
            paths.Add(path);
        }
        return paths;
    }

    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray()).Trim('_');
        return string.IsNullOrEmpty(cleaned) ? "Md3Theme" : cleaned;
    }

    private static string Escape(string text) => SecurityElement.Escape(text) ?? string.Empty;
}
