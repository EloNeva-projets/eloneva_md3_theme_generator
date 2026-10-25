using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EloNeva.Md3.Demo;

public sealed class Client : INotifyPropertyChanged
{
    private string _statut = "";

    public required string Nom { get; init; }
    public required string Ville { get; init; }
    public required DateTime DateCreation { get; init; }
    public required decimal Montant { get; init; }

    // Modifiable : la table refiltre la ligne quand le statut change (INotifyPropertyChanged).
    public string Statut
    {
        get => _statut;
        set { _statut = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

/// <summary>ViewModel de démonstration : toutes les données de la fenêtre passent par le data binding.</summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly string[] Noms = ["Martin", "Bernard", "Durand", "Petit", "Robert", "Moreau", "Simon", "Laurent", "Lefebvre", "Roux"];
    private static readonly string[] Villes = ["Paris", "Lyon", "Marseille", "Toulouse", "Nantes", "Lille", "Bordeaux"];
    private static readonly string[] StatutsPossibles = ["Actif", "En attente", "Suspendu"];

    private readonly Random _random = new(7);
    private bool _isDark;
    private string? _service = "Informatique";
    private DateTime? _dateEffet = DateTime.Today;
    private DateTime? _debut = DateTime.Today.AddDays(-30);
    private DateTime? _fin = DateTime.Today;

    public MainViewModel()
    {
        for (var i = 0; i < 200; i++)
            Clients.Add(NouveauClient());
    }

    public ObservableCollection<Client> Clients { get; } = [];

    public IReadOnlyList<string> Services { get; } = ["Comptabilité", "Ressources humaines", "Informatique", "Commercial", "Logistique"];

    public IReadOnlyList<string> Statuts => StatutsPossibles;

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (Set(ref _isDark, value))
                ThemeSwitcher.Use("MonTheme", value);
        }
    }

    public string? Service { get => _service; set => Set(ref _service, value); }
    public DateTime? DateEffet { get => _dateEffet; set => Set(ref _dateEffet, value); }
    public DateTime? Debut { get => _debut; set => Set(ref _debut, value); }
    public DateTime? Fin { get => _fin; set => Set(ref _fin, value); }

    /// <summary>Ajoute un client : il n'apparaît dans la table que s'il correspond aux filtres saisis.</summary>
    public void AjouterClient() => Clients.Insert(0, NouveauClient());

    /// <summary>Change le statut d'un client au hasard : la table le refiltre aussitôt.</summary>
    public void ChangerUnStatut()
    {
        var client = Clients[_random.Next(Clients.Count)];
        client.Statut = StatutsPossibles[(Array.IndexOf(StatutsPossibles, client.Statut) + 1) % StatutsPossibles.Length];
    }

    private Client NouveauClient() => new()
    {
        Nom = Noms[_random.Next(Noms.Length)],
        Ville = Villes[_random.Next(Villes.Length)],
        DateCreation = DateTime.Today.AddDays(-_random.Next(0, 900)),
        Montant = _random.Next(100, 50000),
        Statut = StatutsPossibles[_random.Next(StatutsPossibles.Length)],
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        return true;
    }
}
