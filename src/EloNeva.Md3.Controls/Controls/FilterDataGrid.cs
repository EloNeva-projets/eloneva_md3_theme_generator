using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EloNeva.Md3.Controls;

/// <summary>
/// DataGrid dont chaque en-tête de colonne affiche une icône "Filtre".
/// Le texte saisi dans le popup de l'en-tête filtre les lignes (contient, insensible à la casse).
/// </summary>
public class FilterDataGrid : DataGrid
{
    /// <summary>Texte du filtre appliqué à une colonne (propriété attachée sur <see cref="DataGridColumn"/>).</summary>
    public static readonly DependencyProperty FilterTextProperty = DependencyProperty.RegisterAttached(
        "FilterText", typeof(string), typeof(FilterDataGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static string? GetFilterText(DependencyObject obj) => (string?)obj.GetValue(FilterTextProperty);
    public static void SetFilterText(DependencyObject obj, string? value) => obj.SetValue(FilterTextProperty, value);

    /// <summary>Indique si une colonne propose l'icône de filtre (vrai par défaut).</summary>
    public static readonly DependencyProperty IsFilterableProperty = DependencyProperty.RegisterAttached(
        "IsFilterable", typeof(bool), typeof(FilterDataGrid), new PropertyMetadata(true));

    public static bool GetIsFilterable(DependencyObject obj) => (bool)obj.GetValue(IsFilterableProperty);
    public static void SetIsFilterable(DependencyObject obj, bool value) => obj.SetValue(IsFilterableProperty, value);

    public static readonly DependencyProperty FilteredCountProperty = DependencyProperty.Register(
        nameof(FilteredCount), typeof(int), typeof(FilterDataGrid), new PropertyMetadata(0));

    /// <summary>Nombre de lignes visibles après filtrage.</summary>
    public int FilteredCount
    {
        get => (int)GetValue(FilteredCountProperty);
        set => SetValue(FilteredCountProperty, value);
    }

    private readonly Dictionary<(Type, string), PropertyInfo?> _properties = new();
    private bool _filterToggleClicked;
    private Predicate<object>? _sourceFilter;
    private Predicate<object>? _appliedFilter;
    private bool _applyPending;
    private string? _appliedSignature;
    private bool _clearing;

    public FilterDataGrid()
    {
        // Style par défaut défini dans Components.xaml (un Style explicite en XAML reste prioritaire).
        SetResourceReference(StyleProperty, "Md3.FilterDataGrid");
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnFilterTextChanged));
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnFilterToggleChecked));
    }

    /// <summary>Efface tous les filtres de colonnes.</summary>
    public void ClearFilters()
    {
        // Chaque effacement met à jour la zone de saisie du popup : on ne filtre qu'une fois, à la fin.
        _clearing = true;
        try
        {
            foreach (var column in Columns)
                SetFilterText(column, null);
        }
        finally
        {
            _clearing = false;
        }
        ApplyFilters();
    }

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);
        // Un ICollectionView fourni par le ViewModel peut déjà avoir son propre filtre : on le conserve.
        _sourceFilter = newValue is ICollectionView { Filter: var f } && f != _appliedFilter ? f : null;
        ApplyFilters(force: true);
    }

    // Ajouts / suppressions dans la collection liée : le filtre s'applique seul, seul le compteur est à mettre à jour.
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        FilteredCount = Items.Count;
    }

    // Un clic sur l'icône filtre ne doit pas trier la colonne.
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _filterToggleClicked = FindParent<ToggleButton>(e.OriginalSource as DependencyObject) is { Name: "FilterToggle" };
        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void OnSorting(DataGridSortingEventArgs eventArgs)
    {
        if (_filterToggleClicked)
        {
            _filterToggleClicked = false;
            eventArgs.Handled = true;
            return;
        }
        base.OnSorting(eventArgs);
    }

    // À l'ouverture du popup, le focus va directement dans le champ de filtre.
    private void OnFilterToggleChecked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not ToggleButton { Name: "FilterToggle", TemplatedParent: DataGridColumnHeader header }) return;
        if (header.Template?.FindName("PART_FilterBox", header) is TextBox box)
            Dispatcher.BeginInvoke(() => { box.Focus(); Keyboard.Focus(box); box.SelectAll(); }, DispatcherPriority.Input);
    }

    private static T? FindParent<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null and not T)
            element = element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
        return element as T;
    }

    private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        // Le binding vers FilterText est mis à jour après l'événement : on filtre juste après.
        // Les frappes rapprochées sont regroupées en un seul filtrage.
        if (_clearing || _applyPending || e.OriginalSource is not TextBox { Name: "PART_FilterBox" }) return;
        _applyPending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _applyPending = false;
            ApplyFilters();
        }, DispatcherPriority.Background);
    }

    /// <summary>Réapplique les filtres (à appeler après avoir modifié <c>FilterText</c> par code).</summary>
    public void ApplyFilters() => ApplyFilters(force: false);

    private void ApplyFilters(bool force)
    {
        var active = Columns
            .Select(c => (Path: GetPath(c), Text: GetFilterText(c)?.Trim()))
            .Where(f => !string.IsNullOrWhiteSpace(f.Path) && !string.IsNullOrEmpty(f.Text))
            .ToList();

        // Critères inchangés (ex. zone de saisie resynchronisée après un FilterText posé par code) :
        // pas de nouveau filtrage, il coûterait un rafraîchissement complet de la table.
        var signature = string.Join('\u001F', active.Select(f => $"{f.Path}\u001E{f.Text}"));
        if (!force && signature == _appliedSignature && Items.Filter == _appliedFilter)
        {
            FilteredCount = Items.Count;
            return;
        }
        _appliedSignature = signature;

        var sourceFilter = _sourceFilter;
        _appliedFilter = active.Count == 0
            ? sourceFilter
            : item => (sourceFilter?.Invoke(item) ?? true) && active.All(f => Matches(item, f.Path!, f.Text!));

        // Filtrage « en direct » : une ligne dont une propriété filtrée change (INotifyPropertyChanged)
        // est réévaluée sans attendre une nouvelle saisie. Configuré avant le filtre, et seulement s'il change,
        // pour qu'un filtrage ne provoque qu'un seul rafraîchissement de la vue.
        if (Items is ICollectionViewLiveShaping { CanChangeLiveFiltering: true } live)
        {
            var paths = active.Select(f => f.Path!).Distinct().ToList();
            if (!paths.SequenceEqual(live.LiveFilteringProperties))
            {
                live.LiveFilteringProperties.Clear();
                foreach (var path in paths)
                    live.LiveFilteringProperties.Add(path);
            }
            if (live.IsLiveFiltering != paths.Count > 0)
                live.IsLiveFiltering = paths.Count > 0;
        }

        Items.Filter = _appliedFilter;

        FilteredCount = Items.Count;
    }

    private static string? GetPath(DataGridColumn column) =>
        !string.IsNullOrEmpty(column.SortMemberPath)
            ? column.SortMemberPath
            : (column as DataGridBoundColumn)?.Binding is Binding b ? b.Path?.Path : null;

    private bool Matches(object item, string path, string filter)
    {
        var value = ResolvePath(item, path);
        var text = value switch
        {
            null => string.Empty,
            DateTime d => d.ToString("d", CultureInfo.CurrentCulture),
            IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
        return text.Contains(filter.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

    private object? ResolvePath(object? item, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (item is null) return null;
            var type = item.GetType();
            if (!_properties.TryGetValue((type, part), out var property))
            {
                property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
                _properties[(type, part)] = property;
            }
            item = property?.GetValue(item);
        }
        return item;
    }
}
