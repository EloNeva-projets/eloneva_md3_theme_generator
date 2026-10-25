using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using EloNeva.Md3.Controls;
using static EloNevaThemeGenerator.Tests.WpfHost;
using Calendar = System.Windows.Controls.Calendar;

namespace EloNevaThemeGenerator.Tests;

public class DateRangePickerTests
{
    private static (DateRangePicker Picker, Calendar Calendar, Window Window) Open()
    {
        var picker = new DateRangePicker { Width = 320 };
        var window = ShowOffscreen(picker);
        picker.IsDropDownOpen = true;
        DoEvents();
        var calendar = (Calendar)picker.Template.FindName("PART_Calendar", picker);
        calendar.DisplayDate = new DateTime(2026, 3, 1);
        DoEvents();
        return (picker, calendar, window);
    }

    private static void ClickDay(Calendar calendar, DateTime date)
    {
        var button = Descendants<CalendarDayButton>(calendar).First(b => b.DataContext is DateTime d && d == date);
        // Le clic part d'un élément interne du bouton, comme avec la souris.
        var source = Descendants<FrameworkElement>(button).FirstOrDefault() ?? (FrameworkElement)button;
        // PreviewMouseDown (tunnel) : WPF en dérive PreviewMouseLeftButtonDown sur chaque élément, comme une vraie souris.
        source.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = Mouse.PreviewMouseDownEvent,
            Source = source,
        });
        DoEvents();
    }

    [Fact]
    public void Two_clicks_select_a_range_in_either_order() => Run(() =>
    {
        var (picker, calendar, window) = Open();

        ClickDay(calendar, new DateTime(2026, 3, 10));
        Assert.Equal(new DateTime(2026, 3, 10), picker.StartDate);
        Assert.Null(picker.EndDate);

        ClickDay(calendar, new DateTime(2026, 3, 15));
        Assert.Equal(new DateTime(2026, 3, 10), picker.StartDate);
        Assert.Equal(new DateTime(2026, 3, 15), picker.EndDate);
        Assert.Equal(6, calendar.SelectedDates.Count);

        var expected = $"{new DateTime(2026, 3, 10).ToString("d", CultureInfo.CurrentCulture)} – {new DateTime(2026, 3, 15).ToString("d", CultureInfo.CurrentCulture)}";
        Assert.Equal(expected, picker.DisplayText);
        var field = (TextBox)picker.Template.FindName("PART_Field", picker);
        Assert.Equal(expected, field.Text);

        // Nouvelle plage, fin cliquée avant le début.
        ClickDay(calendar, new DateTime(2026, 3, 20));
        ClickDay(calendar, new DateTime(2026, 3, 5));
        Assert.Equal(new DateTime(2026, 3, 5), picker.StartDate);
        Assert.Equal(new DateTime(2026, 3, 20), picker.EndDate);

        window.Close();
    });

    [Fact]
    public void Presets_clear_and_close_commands_work() => Run(() =>
    {
        var (picker, _, window) = Open();
        var today = DateTime.Today;

        DateRangePicker.ApplyPresetCommand.Execute("Last7Days", picker);
        Assert.Equal(today.AddDays(-6), picker.StartDate);
        Assert.Equal(today, picker.EndDate);

        DateRangePicker.ApplyPresetCommand.Execute("ThisMonth", picker);
        Assert.Equal(new DateTime(today.Year, today.Month, 1), picker.StartDate);
        Assert.Equal(new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)), picker.EndDate);

        DateRangePicker.ClearCommand.Execute(null, picker);
        Assert.Null(picker.StartDate);
        Assert.Null(picker.EndDate);
        Assert.Equal(string.Empty, picker.DisplayText);

        DateRangePicker.CloseCommand.Execute(null, picker);
        Assert.False(picker.IsDropDownOpen);

        window.Close();
    });

    [Fact]
    public void Preset_buttons_in_the_popup_are_wired_to_the_picker() => Run(() =>
    {
        var (picker, calendar, window) = Open();
        var popupContent = (DependencyObject)LogicalTreeHelper.GetParent(LogicalTreeHelper.GetParent(calendar));
        Button ButtonNamed(string text) => Descendants<Button>(popupContent).Single(b => b.Content as string == text);
        void Click(Button b) => ((System.Windows.Automation.Provider.IInvokeProvider)
            new System.Windows.Automation.Peers.ButtonAutomationPeer(b).GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();

        Click(ButtonNamed("7 derniers jours"));
        DoEvents();
        Assert.Equal(DateTime.Today.AddDays(-6), picker.StartDate);
        Assert.Equal(DateTime.Today, picker.EndDate);

        Click(ButtonNamed("Effacer"));
        DoEvents();
        Assert.Null(picker.StartDate);

        Click(ButtonNamed("OK"));
        DoEvents();
        Assert.False(picker.IsDropDownOpen);

        window.Close();
    });
}
