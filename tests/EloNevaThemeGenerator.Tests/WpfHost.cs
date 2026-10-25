using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using EloNevaThemeGenerator.Theming;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace EloNevaThemeGenerator.Tests;

/// <summary>
/// Thread STA unique portant l'Application WPF (ressources de App.xaml chargées).
/// Les fenêtres de test sont placées hors écran et ne prennent jamais le focus.
/// </summary>
public static class WpfHost
{
    private static readonly Lazy<Dispatcher> Ui = new(Start);

    private static Dispatcher Start()
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();

        // Arrêt propre du thread WPF en fin de processus, AVANT le nettoyage interne de WPF
        // (sinon WeakEventTable.OnShutDown touche des objets WPF depuis un autre thread).
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (dispatcher is { HasShutdownStarted: false } d)
                d.InvokeShutdown();
        };

        var thread = new Thread(() =>
        {
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            ThemeApplier.Apply(ThemeTokens.Build(new ThemeDefinition(), isDark: false));
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        ready.Wait();
        return dispatcher!;
    }

    public static void Run(Action action)
    {
        ExceptionDispatchInfo? error = null;
        Ui.Value.Invoke(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
        });
        error?.Throw();
    }

    /// <summary>Laisse s'exécuter le travail en attente (layout, bindings, BeginInvoke).</summary>
    public static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
    }

    public static Window ShowOffscreen(object content, object? dataContext = null)
    {
        var window = new Window
        {
            Content = content,
            DataContext = dataContext,
            Left = -20000,
            Top = -20000,
            Width = 1400,
            Height = 900,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
        };
        window.Show();
        DoEvents();
        return window;
    }

    public static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    public static Color BrushColor(string key) => ((System.Windows.Media.SolidColorBrush)Application.Current.Resources[key]).Color;
}
