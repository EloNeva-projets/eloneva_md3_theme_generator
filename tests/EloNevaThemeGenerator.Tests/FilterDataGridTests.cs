using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using EloNeva.Md3.Controls;
using EloNevaThemeGenerator.ViewModels;
using MaterialDesignThemes.Wpf;
using DataGridTextColumn = System.Windows.Controls.DataGridTextColumn;
using static EloNevaThemeGenerator.Tests.WpfHost;

namespace EloNevaThemeGenerator.Tests;

public class FilterDataGridTests
{
    private static (FilterDataGrid Grid, Window Window, IReadOnlyList<Employee> Data) CreateGrid()
    {
        var data = SampleData.Employees();
        var grid = new FilterDataGrid { ItemsSource = data, Height = 600 };
        grid.Columns.Add(new DataGridTextColumn { Header = "Nom", Binding = new Binding(nameof(Employee.Nom)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Service", Binding = new Binding(nameof(Employee.Service)) });
        var salaire = new DataGridTextColumn { Header = "Salaire", Binding = new Binding(nameof(Employee.Salaire)) };
        FilterDataGrid.SetIsFilterable(salaire, false);
        grid.Columns.Add(salaire);
        var window = ShowOffscreen(grid);
        return (grid, window, data);
    }

    private static DataGridColumnHeader Header(FilterDataGrid grid, string text) =>
        Descendants<DataGridColumnHeader>(grid).Single(h => h.Column?.Header as string == text);

    private static T Part<T>(DataGridColumnHeader header, string name) where T : class =>
        (T)header.Template.FindName(name, header);

    private static void PreviewMouseDown(UIElement element) =>
        element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = Mouse.PreviewMouseDownEvent,
            Source = element,
        });

    // Équivaut au relâchement de la souris sur l'en-tête : c'est ce qui déclenche le tri.
    private static void InvokeHeader(DataGridColumnHeader header) =>
        typeof(ButtonBase).GetMethod("OnClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(header, null);

    [Fact]
    public void Headers_show_filter_icon_except_on_non_filterable_columns() => Run(() =>
    {
        var (grid, window, _) = CreateGrid();

        Assert.Equal(Visibility.Visible, Part<ToggleButton>(Header(grid, "Nom"), "FilterToggle").Visibility);
        Assert.Equal(Visibility.Visible, Part<ToggleButton>(Header(grid, "Service"), "FilterToggle").Visibility);
        Assert.Equal(Visibility.Collapsed, Part<ToggleButton>(Header(grid, "Salaire"), "FilterToggle").Visibility);
        Assert.Equal(PackIconKind.FilterOutline, Part<PackIcon>(Header(grid, "Service"), "FilterIcon").Kind);

        window.Close();
    });

    [Fact]
    public void Typing_in_the_filter_popup_filters_rows_and_clear_restores_them() => Run(() =>
    {
        var (grid, window, data) = CreateGrid();
        var header = Header(grid, "Service");
        var toggle = Part<ToggleButton>(header, "FilterToggle");

        toggle.IsChecked = true;
        DoEvents();
        Assert.True(Part<Popup>(header, "FilterPopup").IsOpen);

        var box = Part<TextBox>(header, "PART_FilterBox");
        box.Text = "logi";
        DoEvents();

        var expected = data.Count(e => e.Service.Contains("logi", StringComparison.CurrentCultureIgnoreCase));
        Assert.True(expected > 0);
        Assert.Equal(expected, grid.FilteredCount);
        Assert.Equal(expected, grid.Items.Count);
        Assert.Equal("logi", FilterDataGrid.GetFilterText(header.Column));
        Assert.Equal(PackIconKind.Filter, Part<PackIcon>(header, "FilterIcon").Kind);

        // Deuxième filtre sur une autre colonne : les deux se combinent.
        var nameHeader = Header(grid, "Nom");
        var firstName = grid.Items.Cast<Employee>().First().Nom;
        Part<TextBox>(nameHeader, "PART_FilterBox").Text = firstName;
        DoEvents();
        var combined = data.Count(e => e.Service.Contains("logi", StringComparison.CurrentCultureIgnoreCase)
                                       && e.Nom.Contains(firstName, StringComparison.CurrentCultureIgnoreCase));
        Assert.Equal(combined, grid.FilteredCount);

        grid.ClearFilters();
        DoEvents();
        Assert.Equal(data.Count, grid.FilteredCount);
        Assert.Equal(string.Empty, box.Text ?? string.Empty);
        Assert.Equal(PackIconKind.FilterOutline, Part<PackIcon>(header, "FilterIcon").Kind);

        window.Close();
    });

    [Fact]
    public void Click_on_filter_icon_does_not_sort_but_click_on_header_does() => Run(() =>
    {
        var (grid, window, _) = CreateGrid();
        var header = Header(grid, "Service");

        // Clic sur l'icône : la DataGrid voit le PreviewMouseDown puis la demande de tri de l'en-tête.
        PreviewMouseDown(Part<ToggleButton>(header, "FilterToggle"));
        InvokeHeader(header);
        DoEvents();
        Assert.Null(header.Column.SortDirection);

        // Clic sur le libellé de l'en-tête : tri normal.
        PreviewMouseDown(header);
        InvokeHeader(header);
        DoEvents();
        Assert.Equal(System.ComponentModel.ListSortDirection.Ascending, header.Column.SortDirection);

        window.Close();
    });
}
