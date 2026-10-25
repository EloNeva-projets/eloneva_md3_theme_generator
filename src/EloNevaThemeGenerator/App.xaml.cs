using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using EloNevaThemeGenerator.ViewModels;

namespace EloNevaThemeGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            var log = Path.Combine(Path.GetTempPath(), "EloNevaThemeGenerator.log");
            File.AppendAllText(log, $"[{DateTime.Now:O}] {args.Exception}{Environment.NewLine}");
            MessageBox.Show(args.Exception.Message, "Erreur inattendue", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Formats de date des contrôles WPF alignés sur la culture de l'utilisateur.
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        // Le view-model applique le thème avant l'affichage de la fenêtre.
        var viewModel = new MainViewModel();
        var window = new MainWindow { DataContext = viewModel };
        MainWindow = window;
        window.Show();
    }
}
