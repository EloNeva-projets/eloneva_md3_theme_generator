using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EloNeva.Md3.Controls;
using EloNevaThemeGenerator.Theming;
using EloNevaThemeGenerator.ViewModels;
using MaterialDesignThemes.Wpf;
using static EloNevaThemeGenerator.Tests.WpfHost;

namespace EloNevaThemeGenerator.Tests;

/// <summary>La page principale se met à jour quand le thème change.</summary>
public class MainWindowTests
{
    private static (MainWindow Window, MainViewModel Vm) Open()
    {
        var vm = new MainViewModel();
        var window = new MainWindow
        {
            DataContext = vm,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -20000,
            Top = -20000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        window.Show();
        DoEvents();
        return (window, vm);
    }

    private static void Close(MainWindow window, MainViewModel vm)
    {
        vm.IsDirty = false; // pas de demande d'enregistrement à la fermeture
        window.Close();
        ThemeApplier.Apply(ThemeTokens.Build(new ThemeDefinition(), false));
    }

    private static Button FirstButtonWithStyle(DependencyObject root, string styleKey)
    {
        var style = (Style)Application.Current.FindResource(styleKey);
        return Descendants<Button>(root).First(b => b.Style == style);
    }

    [Fact]
    public void Changing_seed_updates_resources_and_components() => Run(() =>
    {
        var (window, vm) = Open();
        var filled = FirstButtonWithStyle(window, "Md3.Button.Filled");

        vm.PrimarySeed = Color.FromRgb(0x00, 0x61, 0xA4);
        DoEvents();

        var expected = ThemeTokens.Build(new ThemeDefinition { PrimarySeed = "#0061A4" }, false)["Primary"];
        Assert.Equal(expected, BrushColor("Md3.Brush.Primary"));
        Assert.Equal(expected, BrushColor("MaterialDesign.Brush.Primary"));
        Assert.Equal(expected, ((SolidColorBrush)filled.Background).Color);
        Assert.True(vm.IsDirty);
        Assert.Contains(ColorMath.ToHex(expected), vm.GeneratedXaml);

        Close(window, vm);
    });

    [Fact]
    public void Dark_mode_shapes_and_font_are_applied() => Run(() =>
    {
        var (window, vm) = Open();

        vm.IsDark = true;
        vm.ButtonCornerRadius = 6;
        vm.CardCornerRadius = 24;
        vm.FontFamily = "Segoe UI";
        vm.FontSize = 16;
        DoEvents();

        Assert.True(ColorMath.RelativeLuminance(((SolidColorBrush)window.Background).Color) < 0.05);
        Assert.Equal(new CornerRadius(6), ButtonAssist.GetCornerRadius(FirstButtonWithStyle(window, "Md3.Button.Filled")));
        var card = Descendants<Card>(window).First(c => c.Style == (Style)window.FindResource("SectionCard"));
        Assert.Equal(24, card.UniformCornerRadius);
        Assert.Equal("Segoe UI", window.FontFamily.Source);
        Assert.Equal(16, window.FontSize);

        // La table filtrable et le sélecteur de période suivent le thème.
        var grid = Descendants<FilterDataGrid>(window).Single();
        Assert.Equal(BrushColor("Md3.Brush.Surface"), ((SolidColorBrush)grid.Background).Color);
        Assert.Single(Descendants<DateRangePicker>(window));

        Close(window, vm);
    });

    [Fact]
    public void Role_override_and_reset() => Run(() =>
    {
        var (window, vm) = Open();
        var role = vm.Roles.Single(r => r.Name == "SecondaryContainer");
        var generated = role.Color;

        role.Color = Colors.Orange;
        DoEvents();
        Assert.True(role.IsOverridden);
        Assert.Equal(Colors.Orange, BrushColor("Md3.Brush.SecondaryContainer"));
        Assert.Equal(Colors.Orange, ((SolidColorBrush)FirstButtonWithStyle(window, "Md3.Button.Tonal").Background).Color);

        role.ResetCommand.Execute(null);
        DoEvents();
        Assert.False(role.IsOverridden);
        Assert.Equal(generated, BrushColor("Md3.Brush.SecondaryContainer"));

        Close(window, vm);
    });

    [Fact]
    public void Derive_seeds_updates_secondary_and_tertiary() => Run(() =>
    {
        var (window, vm) = Open();
        vm.PrimarySeed = Color.FromRgb(0x9C, 0xC9, 0x3A);
        vm.DeriveSeedsCommand.Execute(null);
        DoEvents();

        var hue = ColorMath.ToLch(vm.PrimarySeed).H;
        Assert.InRange(Math.Abs(ColorMath.ToLch(vm.SecondarySeed).H - hue), 0, 5);
        Assert.InRange((ColorMath.ToLch(vm.TertiarySeed).H - hue + 360) % 360, 50, 70);

        Close(window, vm);
    });
}
