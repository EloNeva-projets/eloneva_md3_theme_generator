# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Projet

Générateur de thèmes **Material Design 3** pour WPF (.NET 10, MaterialDesignInXamlToolkit **5.4.0**, CommunityToolkit.Mvvm).
Code, commentaires, interface et documentation sont **en français**.

| Projet | Rôle |
|---|---|
| `src/EloNevaThemeGenerator` | Éditeur de thème avec aperçu en direct, sauvegarde `.md3theme.json`, export XAML clair/sombre |
| `src/EloNeva.Md3.Controls` | Bibliothèque réutilisable : `Themes/Components.xaml` (styles `Md3.*`), `FilterDataGrid`, `DateRangePicker` |
| `samples/EloNeva.Md3.Demo` | Application consommatrice d'un thème exporté ; doit rester alignée sur `guide.md` |
| `tests/EloNevaThemeGenerator.Tests` | xUnit (thème, export, contrôles, page principale, data binding, 1 000 lignes) |

`guide.md` = documentation d'intégration d'un thème exporté pour les autres applications (à mettre à jour si l'API des contrôles ou les jetons changent).

## Commandes

```powershell
dotnet build EloNevaThemeGenerator.slnx
dotnet run --project src/EloNevaThemeGenerator
dotnet run --project samples/EloNeva.Md3.Demo
dotnet test tests/EloNevaThemeGenerator.Tests
dotnet test tests/EloNevaThemeGenerator.Tests --filter "FullyQualifiedName~FilterDataGridTests"          # une classe
dotnet test tests/EloNevaThemeGenerator.Tests --filter "FullyQualifiedName~Two_clicks_select_a_range"    # un test
dotnet test tests/EloNevaThemeGenerator.Tests -c Release --filter "FullyQualifiedName~ThousandRows" --logger "console;verbosity=detailed"   # mesures de perf affichées
```

Release : pousser un tag `vX.Y.Z` déclenche `.github/workflows/release.yml` (build, tests, exe autonome compressé,
`dotnet pack` de `EloNeva.Md3.Controls`, `gh release create`). La version vient du tag (`-p:Version`), sinon de
`Directory.Build.props`. `gh` n'est pas installé sur le poste de développement.

Si le générateur est ouvert, ses DLL sont verrouillées et le build échoue (MSB3027) : ne pas tuer le processus
(thème non enregistré possible), compiler ailleurs avec `--artifacts-path <dossier temporaire>`.

## Architecture du thème

Une seule source de vérité : **`ThemeTokens.Build(ThemeDefinition, isDark)`** (`src/EloNevaThemeGenerator/Theming`).

1. `ThemeDefinition` (sérialisée en JSON) : couleurs sources, surcharges de rôles **par mode** (`LightOverrides` / `DarkOverrides`), rayons, police.
2. `ColorMath` construit des palettes tonales en CIELAB/LCh (tonalité = L*, chroma réduit jusqu'au gamut sRGB) ; `Md3Roles` donne la tonalité clair/sombre de chaque rôle MD3.
3. `ThemeTokens` produit les rôles (`Md3.Color.*` / `Md3.Brush.*`), les formes (`Md3.Shape.*`), la police (`Md3.FontFamily`, `Md3.FontSize`) et la correspondance rôles → pinceaux du toolkit (`MaterialDesign.Brush.*`, dans `BuildToolkitBrushes`).
4. Deux consommateurs des mêmes jetons :
   - `ThemeApplier` écrit chaque jeton **au premier niveau** de `Application.Resources` (prime sur les dictionnaires fusionnés) → tout ce qui est en `DynamicResource` se met à jour en direct.
   - `XamlThemeExporter` écrit le même jeu de jetons en `ResourceDictionary` (+ `md:CustomColorTheme`).

Ajouter un jeton = l'ajouter dans `ThemeTokens` ; l'aperçu et l'export suivent. Le test `Exported_xaml_parses_and_contains_all_tokens` recharge le XAML exporté et compare tous les jetons.

`MainViewModel` : `OnPropertyChanged` recopie chaque propriété dans `_def` puis `ScheduleRefresh()` regroupe les changements (priorité Background) avant `Refresh()` ; `_loading` neutralise ce mécanisme pendant un chargement. Modifier un `ColorRoleViewModel` crée une surcharge pour le mode courant.

`Components.xaml` fusionne lui-même `MaterialDesign3.Defaults.xaml`, n'utilise que des `DynamicResource` sur les jetons `Md3.*` et **ne définit aucun jeton** : un thème exporté doit être fusionné **après** lui. Les noms de clés `MaterialDesign.Brush.*` dépendent de la version du toolkit (5.4.0).

## Pièges WPF / toolkit déjà rencontrés

- `FilterDataGrid` et `DateRangePicker` appliquent leur style par `SetResourceReference(StyleProperty, "Md3.…")` dans le constructeur : les styles implicites par type n'étaient pas appliqués de façon fiable.
- Le style implicite global `DataGridColumnHeader` du toolkit l'emporte sur `ColumnHeaderStyle` : le style d'en-tête avec icône filtre est donc injecté comme style implicite dans les `Style.Resources` de `Md3.FilterDataGrid`.
- Dans `ControlTemplate.Triggers`, les `DataTrigger` utilisent `RelativeSource Self` (= le contrôle modèle) ; `TemplatedParent` y renvoie `null`. À l'inverse, à l'intérieur du modèle (y compris dans un `Popup`), utiliser `RelativeSource TemplatedParent`.
- `FilterDataGrid` (filtre = propriété attachée `FilterText` sur chaque colonne) :
  - le filtrage part du `TextChanged` de `PART_FilterBox` (regroupé) ou de `ApplyFilters()` ;
  - une signature des critères évite un second rafraîchissement complet quand rien n'a changé ;
  - le filtre d'un `ICollectionView` fourni par le ViewModel est conservé et combiné (`_sourceFilter`) ;
  - le filtrage en direct passe par `ICollectionViewLiveShaping` ;
  - un clic sur l'icône filtre annule le tri de la colonne (`OnSorting`).
- `DateRangePicker` intercepte `PreviewMouseLeftButtonDown` sur les `CalendarDayButton` (`DataContext` = `DateTime`) et ne s'appuie pas sur la sélection native du `Calendar`.

## Tests

- `WpfHost` : un unique thread STA porte l'`App` du générateur (`InitializeComponent()`, ressources chargées) ; chaque test passe par `WpfHost.Run(...)`, les fenêtres sont **hors écran et `ShowActivated=false`**, `DoEvents()` laisse passer layout et bindings. Parallélisation xUnit désactivée.
- Ne jamais piloter l'interface avec la vraie souris ou le vrai clavier (envoi de clics/touches au bureau) : cela vole le focus de l'utilisateur et a déjà tapé du texte dans son terminal. Tout se teste dans le processus.
- Simuler un clic : lever `Mouse.PreviewMouseDownEvent` (et non `PreviewMouseLeftButtonDownEvent`, qui est un événement *direct* dérivé par WPF élément par élément). Clic d'en-tête de colonne : `ButtonBase.OnClick` par réflexion. Boutons : `ButtonAutomationPeer` + `IInvokeProvider`.
- `WpfHost` arrête le dispatcher sur `ProcessExit` ; sinon `WeakEventTable.OnShutDown` lève une exception de thread en fin de processus.
- Les temps mesurés sur fenêtre hors écran sont bruités : n'asserter que des médianes avec une marge large.

## Application de démo

`samples/EloNeva.Md3.Demo/Themes/MonTheme.*.xaml` sont des fichiers **générés** par l'export du générateur : les régénérer plutôt que les éditer à la main.
