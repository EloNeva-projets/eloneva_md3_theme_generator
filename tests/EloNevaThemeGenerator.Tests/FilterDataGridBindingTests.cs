using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using EloNeva.Md3.Controls;
using static EloNevaThemeGenerator.Tests.WpfHost;
using DataGridTextColumn = System.Windows.Controls.DataGridTextColumn;

namespace EloNevaThemeGenerator.Tests;

/// <summary>FilterDataGrid alimentée par data binding (MVVM).</summary>
public class FilterDataGridBindingTests
{
    public sealed class Client(string nom, string ville) : INotifyPropertyChanged
    {
        private string _ville = ville;
        public string Nom { get; } = nom;

        public string Ville
        {
            get => _ville;
            set { _ville = value; OnChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    }

    public sealed class ClientsViewModel : INotifyPropertyChanged
    {
        private object? _clients;

        public object? Clients
        {
            get => _clients;
            set { _clients = value; PropertyChanged?.Invoke(this, new(nameof(Clients))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static ObservableCollection<Client> Data() =>
    [
        new("Martin", "Paris"), new("Durand", "Lyon"), new("Petit", "Paris"), new("Robert", "Nantes"),
    ];

    private static (FilterDataGrid Grid, DataGridTextColumn Ville, System.Windows.Window Window) Create(ClientsViewModel vm)
    {
        var grid = new FilterDataGrid { Height = 400 };
        grid.SetBinding(System.Windows.Controls.ItemsControl.ItemsSourceProperty, new Binding(nameof(ClientsViewModel.Clients)));
        grid.Columns.Add(new DataGridTextColumn { Header = "Nom", Binding = new Binding(nameof(Client.Nom)) });
        var ville = new DataGridTextColumn { Header = "Ville", Binding = new Binding(nameof(Client.Ville)) };
        grid.Columns.Add(ville);
        return (grid, ville, ShowOffscreen(grid, vm));
    }

    private static void Filter(FilterDataGrid grid, DataGridTextColumn column, string? text)
    {
        FilterDataGrid.SetFilterText(column, text);
        grid.ApplyFilters();
        DoEvents();
    }

    private static string[] Visible(FilterDataGrid grid) => grid.Items.Cast<Client>().Select(c => c.Nom).ToArray();

    [Fact]
    public void Bound_collection_is_filtered_and_items_added_later_follow_the_filter() => Run(() =>
    {
        var data = Data();
        var vm = new ClientsViewModel { Clients = data };
        var (grid, ville, window) = Create(vm);

        Filter(grid, ville, "paris");
        Assert.Equal(["Martin", "Petit"], Visible(grid));
        Assert.Equal(2, grid.FilteredCount);

        data.Add(new Client("Moreau", "Paris"));
        data.Add(new Client("Simon", "Lille"));
        DoEvents();
        Assert.Equal(["Martin", "Petit", "Moreau"], Visible(grid));
        Assert.Equal(3, grid.FilteredCount);

        data.RemoveAt(0);
        DoEvents();
        Assert.Equal(["Petit", "Moreau"], Visible(grid));
        Assert.Equal(2, grid.FilteredCount);

        window.Close();
    });

    [Fact]
    public void Editing_a_bound_property_refilters_the_row() => Run(() =>
    {
        var data = Data();
        var (grid, ville, window) = Create(new ClientsViewModel { Clients = data });

        Filter(grid, ville, "paris");
        data.Single(c => c.Nom == "Durand").Ville = "Paris";
        data.Single(c => c.Nom == "Martin").Ville = "Marseille";
        DoEvents();

        Assert.Equal(["Durand", "Petit"], Visible(grid));
        Assert.Equal(2, grid.FilteredCount);

        window.Close();
    });

    [Fact]
    public void Replacing_the_bound_collection_keeps_the_column_filters() => Run(() =>
    {
        var vm = new ClientsViewModel { Clients = Data() };
        var (grid, ville, window) = Create(vm);
        Filter(grid, ville, "paris");

        vm.Clients = new ObservableCollection<Client> { new("A", "Paris"), new("B", "Lyon"), new("C", "Paris") };
        DoEvents();

        Assert.Equal(["A", "C"], Visible(grid));
        Assert.Equal(2, grid.FilteredCount);

        window.Close();
    });

    [Fact]
    public void Filter_of_a_view_model_ICollectionView_is_kept_and_combined() => Run(() =>
    {
        var view = new ListCollectionView(Data()) { Filter = o => ((Client)o).Nom != "Petit" };
        var (grid, ville, window) = Create(new ClientsViewModel { Clients = view });
        Assert.Equal(3, grid.FilteredCount);

        Filter(grid, ville, "paris");
        Assert.Equal(["Martin"], Visible(grid));

        Filter(grid, ville, null);
        Assert.Equal(["Martin", "Durand", "Robert"], Visible(grid));

        window.Close();
    });

    [Fact]
    public void Async_loading_null_then_collection_works() => Run(() =>
    {
        var vm = new ClientsViewModel();
        var (grid, ville, window) = Create(vm);
        Filter(grid, ville, "lyon");
        Assert.Equal(0, grid.FilteredCount);

        vm.Clients = Data();
        DoEvents();
        Assert.Equal(["Durand"], Visible(grid));
        Assert.Equal(1, grid.FilteredCount);

        window.Close();
    });
}
