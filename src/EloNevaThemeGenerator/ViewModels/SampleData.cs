using MaterialDesignThemes.Wpf;

namespace EloNevaThemeGenerator.ViewModels;

public sealed record Employee(string Matricule, string Nom, string Prenom, string Service, string Ville, DateTime DateEmbauche, decimal Salaire, string Statut);

public sealed record ListEntry(PackIconKind Icon, string Title, string Subtitle, string Trailing);

/// <summary>Données de démonstration pour l'aperçu des composants.</summary>
public static class SampleData
{
    public static readonly string[] Services = ["Comptabilité", "Ressources humaines", "Informatique", "Commercial", "Logistique", "Direction"];

    public static readonly string[] Statuts = ["Actif", "En congé", "Période d'essai", "Sorti"];

    public static IReadOnlyList<Employee> Employees()
    {
        string[] noms = ["Martin", "Bernard", "Thomas", "Petit", "Robert", "Richard", "Durand", "Dubois", "Moreau", "Laurent", "Simon", "Michel", "Lefebvre", "Leroy", "Roux", "David", "Bertrand", "Morel", "Fournier", "Girard"];
        string[] prenoms = ["Camille", "Léa", "Hugo", "Louis", "Chloé", "Lucas", "Manon", "Jules", "Inès", "Arthur", "Emma", "Nathan", "Sarah", "Gabriel", "Jade", "Paul", "Alice", "Tom", "Zoé", "Adam"];
        string[] villes = ["Paris", "Lyon", "Marseille", "Toulouse", "Nantes", "Lille", "Bordeaux", "Rennes"];

        var random = new Random(42);
        return Enumerable.Range(1, 40)
            .Select(i => new Employee(
                $"EMP-{i:000}",
                noms[random.Next(noms.Length)],
                prenoms[random.Next(prenoms.Length)],
                Services[random.Next(Services.Length)],
                villes[random.Next(villes.Length)],
                new DateTime(2012, 1, 1).AddDays(random.Next(0, 5000)),
                Math.Round(28000m + random.Next(0, 42000), 0),
                Statuts[random.Next(Statuts.Length)]))
            .ToList();
    }

    public static IReadOnlyList<ListEntry> Messages() =>
    [
        new(PackIconKind.EmailOutline, "Validation des congés", "Votre demande du 12/11 a été acceptée", "09:24"),
        new(PackIconKind.FileDocumentOutline, "Bulletin de paie", "Le bulletin d'octobre est disponible", "Hier"),
        new(PackIconKind.CalendarClock, "Entretien annuel", "Planifié le 18/11 à 14h00", "Lun."),
        new(PackIconKind.AccountGroupOutline, "Nouvel arrivant", "Bienvenue à Camille au service RH", "22/10"),
        new(PackIconKind.AlertCircleOutline, "Document manquant", "Merci de fournir votre RIB", "20/10"),
    ];
}
