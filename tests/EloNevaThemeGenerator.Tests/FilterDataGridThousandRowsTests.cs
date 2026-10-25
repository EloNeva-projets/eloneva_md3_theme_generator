using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using EloNeva.Md3.Controls;
using Xunit.Abstractions;
using static EloNevaThemeGenerator.Tests.WpfHost;
using DataGridTextColumn = System.Windows.Controls.DataGridTextColumn;

namespace EloNevaThemeGenerator.Tests;

/// <summary>
/// Scénario complet en data binding : ViewModel avec 1 000 lignes, filtres saisis dans les popups
/// des en-têtes caractère par caractère, modifications de la collection et des lignes, tri, effacement.
/// </summary>
public class FilterDataGridThousandRowsTests(ITestOutputHelper output)
{
    public sealed class Commande : INotifyPropertyChanged
    {
        private string _statut = "";
        public required string Numero { get; init; }
        public required string Client { get; init; }
        public required string Ville { get; init; }
        public required DateTime Date { get; init; }
        public required decimal Montant { get; init; }

        public string Statut
        {
            get => _statut;
            set { _statut = value; OnChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    }

    public sealed class CommandesViewModel
    {
        private static readonly string[] Clients = ["Martin SA", "Durand & Fils", "Petit SARL", "Robert Industrie", "Moreau Conseil", "Simon Transports", "Laurent Bois", "Lefebvre Auto"];
        private static readonly string[] Villes = ["Paris", "Lyon", "Marseille", "Toulouse", "Nantes", "Lille", "Bordeaux", "Rennes"];
        private static readonly string[] Statuts = ["En cours", "Livrée", "Annulée", "En attente"];

        public ObservableCollection<Commande> Commandes { get; }

        public CommandesViewModel(int count)
        {
            var random = new Random(1234);
            Commandes = new(Enumerable.Range(1, count).Select(i => new Commande
            {
                Numero = $"CMD-{i:00000}",
                Client = Clients[random.Next(Clients.Length)],
                Ville = Villes[random.Next(Villes.Length)],
                Date = new DateTime(2025, 1, 1).AddDays(random.Next(0, 600)),
                Montant = random.Next(50, 20000),
                Statut = Statuts[random.Next(Statuts.Length)],
            }));
        }
    }

    private const int RowCount = 1000;

    private static FilterDataGrid CreateGrid()
    {
        var grid = new FilterDataGrid { Height = 600, IsReadOnly = true };
        grid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(CommandesViewModel.Commandes)));
        grid.Columns.Add(new DataGridTextColumn { Header = "Numéro", Binding = new Binding(nameof(Commande.Numero)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Client", Binding = new Binding(nameof(Commande.Client)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Ville", Binding = new Binding(nameof(Commande.Ville)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = new Binding(nameof(Commande.Date)) { StringFormat = "d" } });
        grid.Columns.Add(new DataGridTextColumn { Header = "Montant", Binding = new Binding(nameof(Commande.Montant)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Statut", Binding = new Binding(nameof(Commande.Statut)) });
        return grid;
    }

    private static DataGridColumnHeader Header(FilterDataGrid grid, string text) =>
        Descendants<DataGridColumnHeader>(grid).Single(h => h.Column?.Header as string == text);

    /// <summary>Ouvre le popup de filtre de la colonne et tape le texte caractère par caractère, comme un utilisateur.</summary>
    /// <summary>Durée de chaque frappe : filtrage + rendu de la table.</summary>
    private readonly List<double> _keystrokes = new();

    private void TypeFilter(FilterDataGrid grid, string column, string text)
    {
        var header = Header(grid, column);
        var toggle = (ToggleButton)header.Template.FindName("FilterToggle", header);
        toggle.IsChecked = true;
        DoEvents();
        var box = (TextBox)header.Template.FindName("PART_FilterBox", header);

        var times = new List<double>();
        for (var i = 1; i <= text.Length; i++)
        {
            var watch = Stopwatch.StartNew();
            box.Text = text[..i];
            DoEvents();
            times.Add(watch.Elapsed.TotalMilliseconds);
        }
        _keystrokes.AddRange(times);
        toggle.IsChecked = false; // fermeture du popup, comme un clic ailleurs
        DoEvents();
        output.WriteLine($"Filtre {column} = « {text} » : {grid.FilteredCount} ligne(s), frappes {string.Join(" / ", times.Select(t => $"{t:0}"))} ms");
    }

    private static bool Contains(string value, string filter) =>
        value.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

    private static List<Commande> Visible(FilterDataGrid grid) => grid.Items.Cast<Commande>().ToList();

    [Fact]
    public void Thousand_bound_rows_with_column_filters() => Run(() =>
    {
        var vm = new CommandesViewModel(RowCount);
        var data = vm.Commandes;
        var grid = CreateGrid();

        var load = Stopwatch.StartNew();
        var window = ShowOffscreen(grid, vm);
        load.Stop();
        output.WriteLine($"Affichage de {RowCount} lignes liées : {load.ElapsedMilliseconds} ms");

        // 1. Toutes les lignes sont là ; la virtualisation ne crée que les lignes visibles.
        Assert.Equal(RowCount, grid.Items.Count);
        Assert.Equal(RowCount, grid.FilteredCount);
        var realizedRows = Descendants<DataGridRow>(grid).Count();
        output.WriteLine($"Lignes réellement créées à l'écran : {realizedRows}");
        Assert.InRange(realizedRows, 1, 60);

        // 2. Filtre texte saisi caractère par caractère.
        TypeFilter(grid, "Ville", "Par");
        var expected = data.Where(c => Contains(c.Ville, "Par")).ToList();
        Assert.Equal(expected, Visible(grid));
        Assert.Equal(expected.Count, grid.FilteredCount);

        // 3. Deuxième filtre sur une autre colonne : combinaison ET.
        TypeFilter(grid, "Client", "sa");
        expected = data.Where(c => Contains(c.Ville, "Par") && Contains(c.Client, "sa")).ToList();
        Assert.Equal(expected, Visible(grid));
        Assert.NotEmpty(expected);

        // 4. Filtre sur une date (format court de la culture) : ex. « /2026 » pour l'année 2026.
        var yearFilter = new DateTime(2026, 1, 1).ToString("d", CultureInfo.CurrentCulture)[^4..];
        TypeFilter(grid, "Date", yearFilter);
        expected = expected.Where(c => c.Date.ToString("d", CultureInfo.CurrentCulture).Contains(yearFilter)).ToList();
        Assert.Equal(expected, Visible(grid));

        // 5. Le ViewModel ajoute 100 lignes : seules celles qui correspondent apparaissent.
        var added = Enumerable.Range(1, 100).Select(i => new Commande
        {
            Numero = $"NEW-{i:000}",
            Client = i % 2 == 0 ? "Nouveau SA" : "Autre SARL",
            Ville = i % 3 == 0 ? "Paris" : "Lyon",
            Date = new DateTime(2026, 6, 1),
            Montant = 100,
            Statut = "En cours",
        }).ToList();
        foreach (var c in added) data.Add(c);
        DoEvents();
        expected = data.Where(c => Contains(c.Ville, "Par") && Contains(c.Client, "sa")
                                   && c.Date.ToString("d", CultureInfo.CurrentCulture).Contains(yearFilter)).ToList();
        Assert.Equal(expected, Visible(grid));
        Assert.Equal(expected.Count, grid.FilteredCount);
        Assert.Contains(added.First(c => c.Ville == "Paris"), Visible(grid));

        // 6. Le ViewModel supprime des lignes visibles.
        var removed = Visible(grid).Take(5).ToList();
        foreach (var c in removed) data.Remove(c);
        DoEvents();
        Assert.Equal(expected.Count - 5, grid.FilteredCount);
        Assert.DoesNotContain(removed[0], Visible(grid));

        // 7. Filtre sur le statut, puis le ViewModel modifie le statut de lignes (INotifyPropertyChanged).
        TypeFilter(grid, "Statut", "livr");
        var before = Visible(grid);
        var candidate = data.First(c => !before.Contains(c) && Contains(c.Ville, "Par") && Contains(c.Client, "sa")
                                        && c.Date.ToString("d", CultureInfo.CurrentCulture).Contains(yearFilter));
        candidate.Statut = "Livrée";
        if (before.Count > 0) before[0].Statut = "Annulée";
        DoEvents();
        Assert.Contains(candidate, Visible(grid));
        if (before.Count > 0) Assert.DoesNotContain(before[0], Visible(grid));
        Assert.Equal(grid.Items.Count, grid.FilteredCount);

        // 8. Tri sur une colonne filtrée : l'ordre change, le filtrage est conservé.
        var filteredCount = grid.FilteredCount;
        var montant = grid.Columns.Single(c => c.Header as string == "Montant");
        grid.Items.SortDescriptions.Add(new SortDescription(nameof(Commande.Montant), ListSortDirection.Descending));
        montant.SortDirection = ListSortDirection.Descending;
        DoEvents();
        var montants = Visible(grid).Select(c => c.Montant).ToList();
        Assert.Equal(montants.OrderByDescending(m => m), montants);
        Assert.Equal(filteredCount, grid.FilteredCount);

        // 9. Effacer tous les filtres : on retrouve toutes les lignes de la collection.
        var clear = Stopwatch.StartNew();
        grid.ClearFilters();
        DoEvents();
        clear.Stop();
        output.WriteLine($"Effacement des filtres : {clear.ElapsedMilliseconds} ms");
        Assert.Equal(data.Count, grid.FilteredCount);
        Assert.Equal(RowCount + 100 - 5, grid.Items.Count);

        // 10. Réactivité : médiane des frappes (les pics isolés dépendent de la machine et du ramasse-miettes).
        var median = _keystrokes.Order().ElementAt(_keystrokes.Count / 2);
        output.WriteLine($"Frappes : médiane {median:0} ms, max {_keystrokes.Max():0} ms sur {_keystrokes.Count} frappes");
        Assert.True(median < 250, $"Filtrage trop lent : médiane {median:0} ms par frappe");

        window.Close();
    });
}
