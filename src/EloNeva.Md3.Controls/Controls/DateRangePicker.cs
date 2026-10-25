using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Calendar = System.Windows.Controls.Calendar;

namespace EloNeva.Md3.Controls;

/// <summary>
/// Sélecteur de plage de dates : un champ Material Design qui ouvre un calendrier.
/// Premier clic = date de début, second clic = date de fin.
/// </summary>
[TemplatePart(Name = PartCalendar, Type = typeof(Calendar))]
[TemplatePart(Name = PartField, Type = typeof(TextBox))]
public class DateRangePicker : Control
{
    private const string PartCalendar = "PART_Calendar";
    private const string PartField = "PART_Field";

    private Calendar? _calendar;
    private TextBox? _field;
    private DateTime? _anchor;

    public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register(
        nameof(StartDate), typeof(DateTime?), typeof(DateRangePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRangeChanged));

    public static readonly DependencyProperty EndDateProperty = DependencyProperty.Register(
        nameof(EndDate), typeof(DateTime?), typeof(DateRangePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRangeChanged));

    public static readonly DependencyProperty HintProperty = DependencyProperty.Register(
        nameof(Hint), typeof(string), typeof(DateRangePicker), new PropertyMetadata("Période"));

    public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(
        nameof(DateFormat), typeof(string), typeof(DateRangePicker), new PropertyMetadata("d", OnRangeChanged));

    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(DateRangePicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(DateRangePicker), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    public DateTime? StartDate
    {
        get => (DateTime?)GetValue(StartDateProperty);
        set => SetValue(StartDateProperty, value);
    }

    public DateTime? EndDate
    {
        get => (DateTime?)GetValue(EndDateProperty);
        set => SetValue(EndDateProperty, value);
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    /// <summary>Format des dates affichées (par défaut : date courte de la culture).</summary>
    public string DateFormat
    {
        get => (string)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Texte affiché dans le champ, par exemple « 01/10/2026 – 15/10/2026 ».</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    public static readonly RoutedCommand ApplyPresetCommand = new(nameof(ApplyPresetCommand), typeof(DateRangePicker));
    public static readonly RoutedCommand ClearCommand = new(nameof(ClearCommand), typeof(DateRangePicker));
    public static readonly RoutedCommand CloseCommand = new(nameof(CloseCommand), typeof(DateRangePicker));

    public DateRangePicker()
    {
        SetResourceReference(StyleProperty, "Md3.DateRangePicker");
        CommandBindings.Add(new CommandBinding(ApplyPresetCommand, (_, e) => ApplyPreset(e.Parameter as string)));
        CommandBindings.Add(new CommandBinding(ClearCommand, (_, _) => { _anchor = null; StartDate = null; EndDate = null; }));
        CommandBindings.Add(new CommandBinding(CloseCommand, (_, _) => IsDropDownOpen = false));
    }

    public override void OnApplyTemplate()
    {
        if (_calendar is not null)
            _calendar.PreviewMouseLeftButtonDown -= OnCalendarPreviewMouseDown;
        if (_field is not null)
            _field.PreviewMouseLeftButtonUp -= OnFieldClicked;

        base.OnApplyTemplate();

        _calendar = GetTemplateChild(PartCalendar) as Calendar;
        _field = GetTemplateChild(PartField) as TextBox;

        if (_calendar is not null)
        {
            _calendar.SelectionMode = CalendarSelectionMode.SingleRange;
            _calendar.PreviewMouseLeftButtonDown += OnCalendarPreviewMouseDown;
        }
        if (_field is not null)
            _field.PreviewMouseLeftButtonUp += OnFieldClicked;

        SyncCalendar();
        UpdateDisplayText();
    }

    private void OnFieldClicked(object sender, MouseButtonEventArgs e) => IsDropDownOpen = true;

    // Le clic sur un jour est géré ici : la sélection native du Calendar (glisser, Maj+clic)
    // n'est pas intuitive pour une plage et garde la capture souris.
    private void OnCalendarPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var button = FindParent<CalendarDayButton>(e.OriginalSource as DependencyObject);
        if (button?.DataContext is not DateTime date) return;
        e.Handled = true;

        if (_anchor is null)
        {
            _anchor = date;
            StartDate = date;
            EndDate = null;
        }
        else
        {
            var (start, end) = _anchor.Value <= date ? (_anchor.Value, date) : (date, _anchor.Value);
            _anchor = null;
            StartDate = start;
            EndDate = end;
        }
    }

    private void ApplyPreset(string? preset)
    {
        var today = DateTime.Today;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        var (start, end) = preset switch
        {
            "Last7Days" => (today.AddDays(-6), today),
            "Last30Days" => (today.AddDays(-29), today),
            "ThisMonth" => (firstOfMonth, firstOfMonth.AddMonths(1).AddDays(-1)),
            "ThisYear" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),
            _ => (today, today)
        };
        _anchor = null;
        StartDate = start;
        EndDate = end;
        if (_calendar is not null) _calendar.DisplayDate = start;
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (DateRangePicker)d;
        picker.SyncCalendar();
        picker.UpdateDisplayText();
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (DateRangePicker)d;
        picker._anchor = null;
        if ((bool)e.NewValue && picker._calendar is not null && picker.StartDate is { } start)
            picker._calendar.DisplayDate = start;
    }

    private void SyncCalendar()
    {
        if (_calendar is null) return;
        _calendar.SelectedDates.Clear();
        if (StartDate is { } start)
            _calendar.SelectedDates.AddRange(start, EndDate is { } end && end >= start ? end : start);
    }

    private void UpdateDisplayText()
    {
        string Format(DateTime? d) => d?.ToString(DateFormat, CultureInfo.CurrentCulture) ?? "…";
        var text = StartDate is null && EndDate is null ? string.Empty : $"{Format(StartDate)} – {Format(EndDate)}";
        SetValue(DisplayTextPropertyKey, text);
    }

    private static T? FindParent<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null and not T)
            element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
        return element as T;
    }
}
