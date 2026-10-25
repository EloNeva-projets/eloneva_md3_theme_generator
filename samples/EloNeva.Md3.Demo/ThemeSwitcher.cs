using System.Windows;

namespace EloNeva.Md3.Demo;

/// <summary>Bascule entre les variantes claire et sombre d'un thème généré (Themes/{nom}.Light|Dark.xaml).</summary>
public static class ThemeSwitcher
{
    public static void Use(string themeName, bool dark)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        var current = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains($"/Themes/{themeName}.") == true);

        var next = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{themeName}.{(dark ? "Dark" : "Light")}.xaml")
        };

        if (current is null) merged.Add(next);
        else merged[merged.IndexOf(current)] = next; // garde la même position (en dernier)
    }
}
