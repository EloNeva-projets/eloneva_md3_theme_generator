# EloNeva MD3 Theme Generator

Application WPF (.NET 10, MaterialDesignInXamlToolkit 5.4) pour créer un thème **Material Design 3**,
le prévisualiser en direct sur un catalogue de composants, l'enregistrer et l'exporter en XAML.

```
dotnet run --project src/EloNevaThemeGenerator
```

## Solution

| Projet | Rôle |
|---|---|
| `src/EloNeva.Md3.Controls` | Bibliothèque réutilisable : `FilterDataGrid` (icône filtre par colonne), `DateRangePicker`, styles MD3 (`Themes/Components.xaml`) |
| `src/EloNevaThemeGenerator` | Éditeur de thème + page d'aperçu |
| `tests/EloNevaThemeGenerator.Tests` | Tests xUnit (`dotnet test`) : moteur de thème, export XAML, table filtrable, plage de dates, mise à jour en direct de la page principale. Les fenêtres sont ouvertes hors écran, sans prendre le focus. |

## Fonctionnement

- **Couleurs sources** (primaire, secondaire, tertiaire, erreur, neutres) → palettes tonales (CIELAB/LCh) →
  **rôles MD3** (`Primary`, `OnPrimary`, `PrimaryContainer`, `Surface`, `SurfaceContainer*`, `Outline`…) en clair et en sombre.
- Chaque rôle peut être **surchargé** à la main (par mode), puis réinitialisé.
- Formes (rayons boutons / champs / cartes / petits éléments) et typographie (police, taille de base).
- Toute modification est appliquée **en direct** : les jetons sont écrits dans `Application.Resources` et tous les
  composants les lisent en `DynamicResource`.
- **Enregistrer** : `*.md3theme.json` (le dernier fichier est rouvert au démarrage).
- **Exporter XAML** : `NomDuTheme.Light.xaml` et `NomDuTheme.Dark.xaml`.

## Utiliser un thème exporté dans une autre application

Référencer `EloNeva.Md3.Controls`, puis dans `App.xaml` :

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/EloNeva.Md3.Controls;component/Themes/Components.xaml" />
    <ResourceDictionary Source="Themes/MonTheme.Light.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Jetons disponibles : `Md3.Brush.<Rôle>`, `Md3.Color.<Rôle>`, `Md3.Shape.Button|Field|FieldFilled|Small|CardRadius`,
`Md3.Shape.Card` (double), `Md3.FontFamily`, `Md3.FontSize`, ainsi que les pinceaux `MaterialDesign.Brush.*` du toolkit.

Styles : `Md3.Button.Filled|Tonal|Elevated|Outlined|Text|Error|Fab|Icon`, `Md3.TextBox.Outlined|Filled`,
`Md3.PasswordBox.Outlined`, `Md3.ComboBox.Outlined|Filled`, `Md3.DatePicker.Outlined|Filled`,
`Md3.Card.Elevated|Filled|Outlined`, `Md3.ChipListBox`, `Md3.FilterDataGrid`, `Md3.DateRangePicker`.
