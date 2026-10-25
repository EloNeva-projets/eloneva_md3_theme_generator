using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EloNevaThemeGenerator.Theming;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace EloNevaThemeGenerator.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private const string FileFilter = "Thème MD3 (*.md3theme.json)|*.md3theme.json|JSON (*.json)|*.json";

    private static readonly string SettingsFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EloNeva", "Md3ThemeGenerator");

    private static readonly string SettingsPath = Path.Combine(SettingsFolder, "settings.json");

    private ThemeDefinition _def = new();
    private bool _loading;
    private bool _refreshPending;

    public MainViewModel()
    {
        Roles = new ObservableCollection<ColorRoleViewModel>(Md3Roles.All.Select(r => new ColorRoleViewModel(r, OnRoleEdited)));
        RolesView = CollectionViewSource.GetDefaultView(Roles);
        RolesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ColorRoleViewModel.Group)));

        FontFamilies = new[] { "Roboto" }
            .Concat(Fonts.SystemFontFamilies.Select(f => f.Source).Order())
            .Distinct()
            .ToList();

        Employees = SampleData.Employees();
        Messages = SampleData.Messages();
        Services = SampleData.Services;
        SelectedService = Services[2];

        var last = LoadSettings().LastFile;
        if (last is not null && File.Exists(last))
        {
            try
            {
                Load(ThemeDefinition.Load(last), last);
                return;
            }
            catch
            {
                // Fichier illisible : on repart du thème par défaut.
            }
        }
        Load(new ThemeDefinition(), null);
    }

    public SnackbarMessageQueue MessageQueue { get; } = new(TimeSpan.FromSeconds(3));

    public ObservableCollection<ColorRoleViewModel> Roles { get; }
    public ICollectionView RolesView { get; }
    public IReadOnlyList<string> FontFamilies { get; }

    // ---------- Données de démonstration ----------
    public IReadOnlyList<Employee> Employees { get; }
    public IReadOnlyList<ListEntry> Messages { get; }
    public IReadOnlyList<string> Services { get; }

    [ObservableProperty] private string? _selectedService;
    [ObservableProperty] private DateTime? _selectedDate = DateTime.Today;
    [ObservableProperty] private DateTime? _rangeStart = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime? _rangeEnd = DateTime.Today;
    [ObservableProperty] private double _sliderValue = 40;

    // ---------- Fichier ----------
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle), nameof(FileLabel))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    public string WindowTitle => $"EloNeva MD3 Theme Generator — {Name}{(IsDirty ? " *" : "")}";
    public string FileLabel => FilePath ?? "Thème non enregistré";

    [ObservableProperty] private string _generatedXaml = string.Empty;

    // ---------- Propriétés du thème ----------
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(WindowTitle))] private string _name = string.Empty;
    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private Color _primarySeed;
    [ObservableProperty] private Color _secondarySeed;
    [ObservableProperty] private Color _tertiarySeed;
    [ObservableProperty] private Color _errorSeed;
    [ObservableProperty] private Color _neutralSeed;
    [ObservableProperty] private Color _neutralVariantSeed;
    [ObservableProperty] private bool _exactPrimary;
    [ObservableProperty] private bool _autoDeriveSeeds;
    [ObservableProperty] private double _buttonCornerRadius;
    [ObservableProperty] private double _fieldCornerRadius;
    [ObservableProperty] private double _cardCornerRadius;
    [ObservableProperty] private double _smallCornerRadius;
    [ObservableProperty] private string _fontFamily = "Roboto";
    [ObservableProperty] private double _fontSize;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_loading) return;

        switch (e.PropertyName)
        {
            case nameof(Name): _def.Name = Name; break;
            case nameof(IsDark): _def.IsDark = IsDark; break;
            case nameof(PrimarySeed):
                _def.PrimarySeed = ColorMath.ToHex(PrimarySeed);
                if (AutoDeriveSeeds) DeriveSeeds();
                break;
            case nameof(SecondarySeed): _def.SecondarySeed = ColorMath.ToHex(SecondarySeed); break;
            case nameof(TertiarySeed): _def.TertiarySeed = ColorMath.ToHex(TertiarySeed); break;
            case nameof(ErrorSeed): _def.ErrorSeed = ColorMath.ToHex(ErrorSeed); break;
            case nameof(NeutralSeed): _def.NeutralSeed = ColorMath.ToHex(NeutralSeed); break;
            case nameof(NeutralVariantSeed): _def.NeutralVariantSeed = ColorMath.ToHex(NeutralVariantSeed); break;
            case nameof(ExactPrimary): _def.ExactPrimary = ExactPrimary; break;
            case nameof(ButtonCornerRadius): _def.ButtonCornerRadius = ButtonCornerRadius; break;
            case nameof(FieldCornerRadius): _def.FieldCornerRadius = FieldCornerRadius; break;
            case nameof(CardCornerRadius): _def.CardCornerRadius = CardCornerRadius; break;
            case nameof(SmallCornerRadius): _def.SmallCornerRadius = SmallCornerRadius; break;
            case nameof(FontFamily): _def.FontFamily = FontFamily; break;
            case nameof(FontSize): _def.FontSize = FontSize; break;
            default: return;
        }

        if (e.PropertyName != nameof(IsDark)) IsDirty = true;
        ScheduleRefresh();
    }

    // ---------- Application du thème ----------

    private void ScheduleRefresh()
    {
        // Regroupe les changements rapides (glisser dans le sélecteur de couleur) en une seule application.
        if (_refreshPending) return;
        _refreshPending = true;
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            _refreshPending = false;
            Refresh();
        });
    }

    private void Refresh()
    {
        var tokens = ThemeTokens.Build(_def, IsDark);
        ThemeApplier.Apply(tokens);

        var overrides = _def.Overrides(IsDark);
        foreach (var role in Roles)
            role.Sync(tokens[role.Name], overrides.ContainsKey(role.Name));

        GeneratedXaml = XamlThemeExporter.Generate(_def, IsDark);
    }

    private void OnRoleEdited(ColorRoleViewModel role, Color? color)
    {
        var overrides = _def.Overrides(IsDark);
        if (color is { } c)
            overrides[role.Name] = ColorMath.ToHex(c);
        else
            overrides.Remove(role.Name);
        IsDirty = true;
        ScheduleRefresh();
    }

    private void Load(ThemeDefinition def, string? path)
    {
        _def = def;
        _loading = true;
        try
        {
            Name = def.Name;
            IsDark = def.IsDark;
            PrimarySeed = ColorMath.ParseHex(def.PrimarySeed, Colors.MediumPurple);
            SecondarySeed = ColorMath.ParseHex(def.SecondarySeed, Colors.SlateGray);
            TertiarySeed = ColorMath.ParseHex(def.TertiarySeed, Colors.RosyBrown);
            ErrorSeed = ColorMath.ParseHex(def.ErrorSeed, Colors.Firebrick);
            NeutralSeed = ColorMath.ParseHex(def.NeutralSeed, Colors.Gray);
            NeutralVariantSeed = ColorMath.ParseHex(def.NeutralVariantSeed, Colors.Gray);
            ExactPrimary = def.ExactPrimary;
            ButtonCornerRadius = def.ButtonCornerRadius;
            FieldCornerRadius = def.FieldCornerRadius;
            CardCornerRadius = def.CardCornerRadius;
            SmallCornerRadius = def.SmallCornerRadius;
            FontFamily = def.FontFamily;
            FontSize = def.FontSize;
            FilePath = path;
            IsDirty = false;
        }
        finally
        {
            _loading = false;
        }
        Refresh();
    }

    // ---------- Commandes ----------

    [RelayCommand]
    private void DeriveSeeds()
    {
        _def.PrimarySeed = ColorMath.ToHex(PrimarySeed);
        _def.DeriveSeedsFromPrimary();
        _loading = true;
        SecondarySeed = ColorMath.ParseHex(_def.SecondarySeed, SecondarySeed);
        TertiarySeed = ColorMath.ParseHex(_def.TertiarySeed, TertiarySeed);
        NeutralSeed = ColorMath.ParseHex(_def.NeutralSeed, NeutralSeed);
        NeutralVariantSeed = ColorMath.ParseHex(_def.NeutralVariantSeed, NeutralVariantSeed);
        _loading = false;
        IsDirty = true;
        ScheduleRefresh();
    }

    [RelayCommand]
    private void ResetOverrides()
    {
        _def.Overrides(IsDark).Clear();
        IsDirty = true;
        ScheduleRefresh();
    }

    [RelayCommand]
    private void ApplyPreset(string hex)
    {
        if (ColorMath.TryParseHex(hex, out var c))
        {
            PrimarySeed = c;
            if (!AutoDeriveSeeds) DeriveSeeds();
        }
    }

    [RelayCommand]
    private void New()
    {
        if (!ConfirmDiscard()) return;
        Load(new ThemeDefinition(), null);
        SaveSettings(null);
    }

    [RelayCommand]
    private void Open()
    {
        if (!ConfirmDiscard()) return;
        var dialog = new OpenFileDialog { Filter = FileFilter, Title = "Ouvrir un thème" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            Load(ThemeDefinition.Load(dialog.FileName), dialog.FileName);
            SaveSettings(dialog.FileName);
            MessageQueue.Enqueue($"Thème « {Name} » chargé");
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Impossible d'ouvrir le thème :\n{ex.Message}", "Ouvrir", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (FilePath is null) SaveAs();
        else WriteTo(FilePath);
    }

    [RelayCommand]
    private void SaveAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            Title = "Enregistrer le thème",
            FileName = $"{Name}.md3theme.json",
        };
        if (dialog.ShowDialog() == true) WriteTo(dialog.FileName);
    }

    /// <summary>Exporte les ResourceDictionary XAML clair et sombre dans un dossier.</summary>
    [RelayCommand]
    private void Export()
    {
        var dialog = new OpenFolderDialog { Title = "Dossier d'export des fichiers XAML" };
        if (FilePath is not null) dialog.InitialDirectory = Path.GetDirectoryName(FilePath);
        if (dialog.ShowDialog() != true) return;
        try
        {
            var files = XamlThemeExporter.ExportBoth(_def, dialog.FolderName);
            MessageQueue.Enqueue($"Exporté : {string.Join(", ", files.Select(Path.GetFileName))}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Export impossible :\n{ex.Message}", "Exporter", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CopyXaml()
    {
        Clipboard.SetText(GeneratedXaml);
        MessageQueue.Enqueue("XAML copié dans le presse-papiers");
    }

    [RelayCommand]
    private void ShowSnackbar() => MessageQueue.Enqueue("Ceci est une snackbar Material 3", "ANNULER", () => { });

    private void WriteTo(string path)
    {
        try
        {
            _def.Save(path);
            FilePath = path;
            IsDirty = false;
            SaveSettings(path);
            MessageQueue.Enqueue("Thème enregistré");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Enregistrement impossible :\n{ex.Message}", "Enregistrer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Demande quoi faire des modifications non enregistrées ; faux = annuler l'action.</summary>
    public bool ConfirmDiscard()
    {
        if (!IsDirty) return true;
        var answer = MessageBox.Show("Le thème a été modifié. Enregistrer les modifications ?", "EloNeva MD3",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel) return false;
        if (answer == MessageBoxResult.Yes) Save();
        return !IsDirty || answer == MessageBoxResult.No;
    }

    private sealed record Settings(string? LastFile);

    private static Settings LoadSettings()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath)) ?? new Settings(null)
                : new Settings(null);
        }
        catch
        {
            return new Settings(null);
        }
    }

    private static void SaveSettings(string? lastFile)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Settings(lastFile)));
        }
        catch (IOException)
        {
            // Préférence non essentielle.
        }
    }
}
