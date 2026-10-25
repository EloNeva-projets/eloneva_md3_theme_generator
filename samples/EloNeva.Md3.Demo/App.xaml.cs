using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using MaterialDesignThemes.Wpf;

namespace EloNeva.Md3.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Dates et nombres au format de l'utilisateur (sinon WPF utilise en-US).
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        // Démarre dans le mode (clair / sombre) de Windows.
        var dark = Theme.GetSystemTheme() == BaseTheme.Dark;
        ThemeSwitcher.Use("MonTheme", dark);

        var window = new MainWindow { DataContext = new MainViewModel { IsDark = dark } };
        MainWindow = window;
        window.Show();
    }
}
