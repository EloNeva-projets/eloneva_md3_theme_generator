# Guide : utiliser un thème généré dans une nouvelle application WPF

Ce guide explique comment intégrer les fichiers produits par **EloNeva MD3 Theme Generator**
(`MonTheme.Light.xaml` et `MonTheme.Dark.xaml`) dans une application WPF .NET.

---

## 1. Ce que contient un fichier de thème

Chaque fichier exporté est un `ResourceDictionary` qui définit :

| Contenu | Clés | Type |
|---|---|---|
| Rôles de couleur Material 3 | `Md3.Color.Primary`, `Md3.Color.OnPrimary`, `Md3.Color.SurfaceContainerHigh`… | `Color` |
| Pinceaux des rôles | `Md3.Brush.Primary`, `Md3.Brush.OnSurface`, `Md3.Brush.Outline`… | `SolidColorBrush` |
| Formes | `Md3.Shape.Button`, `Md3.Shape.Field`, `Md3.Shape.FieldFilled`, `Md3.Shape.Small`, `Md3.Shape.CardRadius` | `CornerRadius` |
| Rayon des cartes | `Md3.Shape.Card` | `Double` |
| Typographie | `Md3.FontFamily`, `Md3.FontSize` | `FontFamily`, `Double` |
| Pinceaux du toolkit | `MaterialDesign.Brush.Primary`, `MaterialDesign.Brush.Background`, `MaterialDesign.Brush.TextBox.Border`… | `SolidColorBrush` |

Il inclut aussi un `md:CustomColorTheme` (mode clair ou sombre, couleurs primaire et secondaire) pour que
`PaletteHelper` et les styles natifs de MaterialDesignInXamlToolkit restent cohérents.

> Un fichier = un mode. `MonTheme.Light.xaml` pour le clair, `MonTheme.Dark.xaml` pour le sombre.

---

## 2. Préparer le projet

### 2.1 Paquet NuGet

```powershell
dotnet add package MaterialDesignThemes --version 5.4.0
```

Utilisez la même version que le générateur (5.4.0) : les noms des pinceaux `MaterialDesign.Brush.*` peuvent changer
d'une version majeure à l'autre.

### 2.2 Bibliothèque de composants EloNeva

Les styles `Md3.*` (boutons, champs, cartes…) et les contrôles `FilterDataGrid` et `DateRangePicker` sont dans
**EloNeva.Md3.Controls**. Référencez-la :

```powershell
dotnet add reference ..\chemin\vers\src\EloNeva.Md3.Controls\EloNeva.Md3.Controls.csproj
```

(ou la DLL `EloNeva.Md3.Controls.dll` compilée). Le projet doit cibler `net10.0-windows` avec `<UseWPF>true</UseWPF>`.

> Vous pouvez vous passer de la bibliothèque, voir la [section 7](#7-sans-la-bibliothèque-eloneva).

### 2.3 Copier les fichiers du thème

Copiez les fichiers exportés dans un dossier `Themes` du projet :

```
MonApplication/
├── App.xaml
├── MainWindow.xaml
└── Themes/
    ├── MonTheme.Light.xaml
    └── MonTheme.Dark.xaml
```

Aucune configuration n'est nécessaire : dans un projet WPF SDK, les fichiers `.xaml` sont compilés automatiquement
(action de génération **Page**).

---

## 3. Charger le thème dans `App.xaml`

```xml
<Application x:Class="MonApplication.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- 1. Styles et composants MD3 (inclut MaterialDesign3.Defaults.xaml) -->
                <ResourceDictionary Source="pack://application:,,,/EloNeva.Md3.Controls;component/Themes/Components.xaml" />

                <!-- 2. Thème généré : toujours EN DERNIER -->
                <ResourceDictionary Source="pack://application:,,,/Themes/MonTheme.Light.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**L'ordre est important.** Quand deux dictionnaires fusionnés définissent la même clé, c'est le dernier qui gagne :
le thème doit donc être après tout autre dictionnaire MaterialDesign.

> N'ajoutez pas de `<md:BundledTheme>` en plus. Si vous en avez déjà un, placez-le **avant** le thème généré,
> sinon il écrase vos couleurs.

---

## 4. Configurer les fenêtres

Le thème ne change pas tout seul la police et le fond d'une fenêtre : liez-les aux jetons.

```xml
<Window x:Class="MonApplication.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:md3="clr-namespace:EloNeva.Md3.Controls;assembly=EloNeva.Md3.Controls"
        Style="{StaticResource MaterialDesignWindow}"
        Background="{DynamicResource Md3.Brush.Surface}"
        Foreground="{DynamicResource Md3.Brush.OnSurface}"
        TextElement.Foreground="{DynamicResource Md3.Brush.OnSurface}"
        FontFamily="{DynamicResource Md3.FontFamily}"
        FontSize="{DynamicResource Md3.FontSize}">
```

Pour éviter de le répéter dans chaque fenêtre, vous pouvez créer un style de fenêtre commun dans `App.xaml`.

### Règle d'or : `DynamicResource` pour les jetons

Utilisez toujours **`DynamicResource`** pour les jetons `Md3.*` et `MaterialDesign.Brush.*`. C'est ce qui permet
de changer de thème (clair/sombre) pendant l'exécution. `StaticResource` reste correct pour les **styles**
(`Style="{StaticResource Md3.Button.Filled}"`).

---

## 5. Utiliser les composants

### 5.1 Styles disponibles

| Composant | Styles |
|---|---|
| Boutons | `Md3.Button.Filled`, `Md3.Button.Tonal`, `Md3.Button.Elevated`, `Md3.Button.Outlined`, `Md3.Button.Text`, `Md3.Button.Error`, `Md3.Button.Fab`, `Md3.Button.Icon` |
| Champs texte | `Md3.TextBox.Outlined`, `Md3.TextBox.Filled`, `Md3.PasswordBox.Outlined` |
| Listes déroulantes | `Md3.ComboBox.Outlined`, `Md3.ComboBox.Filled` |
| Dates | `Md3.DatePicker.Outlined`, `Md3.DatePicker.Filled` |
| Cartes | `Md3.Card.Elevated`, `Md3.Card.Filled`, `Md3.Card.Outlined` |
| Chips | `Md3.ChipListBox` (choix unique ou filtre selon `SelectionMode`) |
| Table filtrable | `Md3.FilterDataGrid` (appliqué automatiquement) |
| Plage de dates | `Md3.DateRangePicker` (appliqué automatiquement) |

```xml
<StackPanel>
    <Button Style="{StaticResource Md3.Button.Filled}" Content="Enregistrer" />
    <Button Style="{StaticResource Md3.Button.Outlined}" Content="Annuler" />

    <TextBox Style="{StaticResource Md3.TextBox.Outlined}"
             md:HintAssist.Hint="Nom"
             md:HintAssist.HelperText="Obligatoire" />

    <ComboBox Style="{StaticResource Md3.ComboBox.Outlined}"
              md:HintAssist.Hint="Service"
              ItemsSource="{Binding Services}" SelectedItem="{Binding Service}" />

    <DatePicker Style="{StaticResource Md3.DatePicker.Outlined}"
                md:HintAssist.Hint="Date d'effet" SelectedDate="{Binding DateEffet}" />

    <md:Card Style="{StaticResource Md3.Card.Outlined}" Padding="16">
        <TextBlock Text="Contenu de la carte" />
    </md:Card>

    <!-- Chips de filtre (sélection multiple) -->
    <ListBox Style="{StaticResource Md3.ChipListBox}" SelectionMode="Multiple"
             ItemsSource="{Binding Categories}" />
</StackPanel>
```

### 5.2 Table avec filtre par colonne (`FilterDataGrid`)

Chaque en-tête affiche une icône **Filtre** : un clic ouvre un champ « Contient… » qui filtre les lignes
(sans tenir compte des majuscules). Les filtres de plusieurs colonnes se combinent.

```xml
<md3:FilterDataGrid x:Name="ClientsGrid" ItemsSource="{Binding Clients}" IsReadOnly="True">
    <md3:FilterDataGrid.Columns>
        <DataGridTextColumn Header="Nom" Binding="{Binding Nom}" />
        <DataGridTextColumn Header="Ville" Binding="{Binding Ville}" />
        <DataGridTextColumn Header="Créé le" Binding="{Binding DateCreation, StringFormat=d}" />

        <!-- Colonne sans icône de filtre -->
        <DataGridTextColumn Header="Montant" Binding="{Binding Montant, StringFormat=C}"
                            md3:FilterDataGrid.IsFilterable="False" />

        <!-- Colonne modèle : indiquer la propriété à filtrer avec SortMemberPath -->
        <DataGridTemplateColumn Header="Statut" SortMemberPath="Statut">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Statut}" />
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </md3:FilterDataGrid.Columns>
</md3:FilterDataGrid>

<TextBlock Text="{Binding FilteredCount, ElementName=ClientsGrid, StringFormat='{}{0} ligne(s)'}" />
```

| Membre | Rôle |
|---|---|
| `FilterDataGrid.IsFilterable` (attachée à la colonne) | `False` masque l'icône de filtre |
| `FilterDataGrid.FilterText` (attachée à la colonne) | Texte du filtre ; lisible et modifiable par code |
| `FilteredCount` | Nombre de lignes affichées après filtrage |
| `ClearFilters()` | Efface tous les filtres |
| `ApplyFilters()` | Réapplique les filtres, à appeler après avoir modifié `FilterText` par code |

La colonne est filtrée sur sa propriété `SortMemberPath`, ou à défaut sur le chemin de son `Binding`.
Les dates sont comparées au format court de la culture (`25/10/2026`).

#### Avec le data binding (MVVM)

`ItemsSource` se lie normalement au ViewModel. Les cas suivants sont couverts par les tests automatisés :

| Situation | Comportement |
|---|---|
| `ItemsSource="{Binding Clients}"` sur une `ObservableCollection` | Filtrée normalement |
| Lignes ajoutées ou supprimées dans la collection | Les nouvelles lignes suivent les filtres actifs, `FilteredCount` est mis à jour |
| Propriété d'une ligne modifiée (ex. `client.Ville = "Paris"`) | La ligne est réévaluée immédiatement, **si la classe implémente `INotifyPropertyChanged`** |
| Collection remplacée par le ViewModel (`Clients = new …`) | Les filtres saisis restent actifs sur la nouvelle collection |
| Chargement asynchrone (`null` puis la collection) | Filtres appliqués à l'arrivée des données |
| Le ViewModel expose un `ICollectionView` avec son propre `Filter` | Le filtre du ViewModel est **conservé** et combiné avec ceux des colonnes |

Points à connaître :

- **Collection partagée** : si `ItemsSource` est une simple collection, WPF filtre et trie sa *vue par défaut*.
  Un autre contrôle lié **à la même collection** (une `ComboBox`, une `ListBox`…) voit alors les mêmes
  filtres et le même tri. C'est le fonctionnement normal de WPF, pas propre à `FilterDataGrid`. Pour l'éviter,
  exposez une vue dédiée à la table dans le ViewModel :

  ```csharp
  public ObservableCollection<Client> Clients { get; } = new();
  public ICollectionView ClientsGrille { get; }

  public ClientsViewModel()
  {
      ClientsGrille = new ListCollectionView(Clients);   // vue propre à la table
  }
  ```

  ```xml
  <md3:FilterDataGrid ItemsSource="{Binding ClientsGrille}" ... />
  ```

  Ne modifiez pas le `Filter` de cette vue **après** l'avoir liée : la table ne lit ce filtre qu'au moment où
  `ItemsSource` change. Pour changer le filtre du ViewModel, créez une **nouvelle** vue avec le nouveau filtre et
  réaffectez la propriété liée (réaffecter la même instance n'a aucun effet).
- **Valeur filtrée ≠ texte affiché** : le filtre compare la valeur de la propriété, pas le texte formaté de la cellule.
  Avec `StringFormat=C0` (« 39 028 € »), il faut taper `39028`. Un `Converter` n'est pas appliqué non plus.
  Pour ces colonnes, mettez `IsFilterable="False"`, ou exposez une propriété texte dédiée et indiquez-la
  dans `SortMemberPath`.
- **`DataGridComboBoxColumn`** : le filtre porte sur la valeur liée (souvent un identifiant), pas sur le libellé affiché.
- **Booléens** : comparés à `True` / `False`.
- **Volumes** : le filtrage se fait en mémoire et se refait à chaque caractère tapé. Mesures du test automatisé
  `FilterDataGridThousandRowsTests` (1 000 lignes liées, 6 colonnes, build Release, fenêtre de test hors écran) :
  - calcul du filtre : environ 10 ms ;
  - une frappe complète, filtrage et redessin des lignes visibles compris : 150 à 200 ms en médiane ;
  - affichage initial : environ 200 ms ;
  - seules les lignes visibles sont créées (virtualisation) ;
  - l'essentiel du temps d'une frappe est le redessin des lignes visibles par la `DataGrid` : une frappe qui ne laisse
    aucune ligne prend environ 40 à 50 ms. Le calcul du filtre, lui, croît avec le nombre total de lignes.

  Au-delà de quelques dizaines de milliers de lignes (non mesuré), testez, et préférez si besoin un filtrage côté serveur.
- **Threads** : comme pour toute `DataGrid`, ne modifiez la collection que depuis le thread de l'interface, ou activez
  `BindingOperations.EnableCollectionSynchronization`.

### 5.3 Sélecteur de plage de dates (`DateRangePicker`)

```xml
<md3:DateRangePicker Hint="Période"
                     StartDate="{Binding Debut}"
                     EndDate="{Binding Fin}" />
```

Premier clic dans le calendrier = date de début, second clic = date de fin (dans n'importe quel ordre).
Le panneau propose des raccourcis : *Aujourd'hui*, *7 derniers jours*, *30 derniers jours*, *Ce mois-ci*,
*Cette année*, ainsi que *Effacer* et *OK*.

| Propriété | Type | Rôle |
|---|---|---|
| `StartDate`, `EndDate` | `DateTime?` | Plage sélectionnée (liaison bidirectionnelle par défaut) |
| `Hint` | `string` | Libellé du champ |
| `DateFormat` | `string` | Format d'affichage (par défaut `d`, date courte) |
| `IsDropDownOpen` | `bool` | Ouvre ou ferme le calendrier |
| `DisplayText` | `string` (lecture seule) | Texte affiché, ex. `01/10/2026 – 15/10/2026` |

### 5.4 Utiliser directement les couleurs du thème

Pour vos propres éléments, utilisez les rôles MD3 plutôt que des couleurs en dur :

```xml
<Border Background="{DynamicResource Md3.Brush.PrimaryContainer}"
        CornerRadius="{DynamicResource Md3.Shape.Small}" Padding="12,4">
    <TextBlock Text="Nouveau" Foreground="{DynamicResource Md3.Brush.OnPrimaryContainer}" />
</Border>
```

Toujours associer un fond à son rôle « On » : `Primary` / `OnPrimary`, `SecondaryContainer` / `OnSecondaryContainer`,
`Surface` / `OnSurface`… Le générateur garantit un contraste lisible pour ces couples.

---

## 6. Basculer entre clair et sombre

Pour proposer les deux modes, remplacez le dictionnaire du thème pendant l'exécution :

```csharp
using System.Windows;

public static class ThemeSwitcher
{
    /// <summary>Charge Themes/{themeName}.Light.xaml ou .Dark.xaml à la place du thème actuel.</summary>
    public static void Use(string themeName, bool dark)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        var current = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains($"/Themes/{themeName}.") == true);

        var next = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{themeName}.{(dark ? "Dark" : "Light")}.xaml")
        };

        if (current is null) merged.Add(next);
        else merged[merged.IndexOf(current)] = next;   // garde la même position (en dernier)
    }
}
```

```csharp
ThemeSwitcher.Use("MonTheme", dark: true);
```

Tous les éléments liés en `DynamicResource` se mettent à jour immédiatement.

Pour suivre le thème de Windows au démarrage :

```csharp
var dark = MaterialDesignThemes.Wpf.Theme.GetSystemTheme() == MaterialDesignThemes.Wpf.BaseTheme.Dark;
ThemeSwitcher.Use("MonTheme", dark);
```

---

## 7. Sans la bibliothèque EloNeva

Le thème fonctionne aussi seul, avec les styles natifs de MaterialDesignInXamlToolkit : les pinceaux
`MaterialDesign.Brush.*` qu'il redéfinit recolorent tous les contrôles du toolkit. Remplacez simplement
`Components.xaml` par le dictionnaire MD3 du toolkit :

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
    <ResourceDictionary Source="pack://application:,,,/Themes/MonTheme.Light.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Vous perdez alors les styles `Md3.*` (rayons configurables, bouton Tonal…) et les contrôles `FilterDataGrid` et
`DateRangePicker`, mais les jetons `Md3.Brush.*`, `Md3.Shape.*` et `Md3.FontFamily` restent utilisables.

---

## 8. Performances : cache des ombres (facultatif)

> Réglage de performance propre à chaque application : il ne fait **pas** partie du thème exporté.
> Ne l'activez que si vous constatez un ralentissement.
> Référence : [Rendering performance](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/master/docs/rendering-performance.md#advanced-setting-shadowassistcachemode) (documentation MaterialDesignInXamlToolkit).

### Principe

Les ombres (élévation) sont coûteuses à dessiner, à cause du flou. Avec un `BitmapCache`, WPF garde une image
déjà dessinée de l'élément au lieu de le redessiner à chaque affichage.

`UIElement.CacheMode` ne se transmet pas aux éléments enfants : il faudrait le poser sur chaque contrôle.
L'attachée **`md:ShadowAssist.CacheMode`** du toolkit, elle, se transmet aux enfants (vérifié en
MaterialDesignThemes 5.4.0). Les modèles du toolkit la lisent sur les parties qui portent une ombre. Il suffit donc de
la poser **une fois** sur une fenêtre ou un conteneur pour que tous les composants ombrés à l'intérieur en profitent.

### Activation

```xml
<Window ...
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">
    <md:ShadowAssist.CacheMode>
        <BitmapCache EnableClearType="True" SnapsToDevicePixels="True" RenderAtScale="1" />
    </md:ShadowAssist.CacheMode>

    <!-- contenu de la fenêtre -->
</Window>
```

Utilisez la syntaxe ci-dessus (élément de propriété). Si le `BitmapCache` est déclaré dans les `Window.Resources`,
`md:ShadowAssist.CacheMode="{StaticResource …}"` **sur la fenêtre elle-même** ne le trouve pas : la ressource n'est
pas encore chargée quand l'attribut est lu. Pour partager un même cache entre plusieurs fenêtres, déclarez-le dans
`App.xaml` :

```xml
<!-- App.xaml, dans le ResourceDictionary de l'application -->
<BitmapCache x:Key="Md3ShadowCache" EnableClearType="True" SnapsToDevicePixels="True" RenderAtScale="1" />
```

```xml
<Window ... md:ShadowAssist.CacheMode="{StaticResource Md3ShadowCache}">
```

Vous pouvez aussi limiter le cache à une zone : posez-le sur le conteneur concerné (par exemple la liste de cartes)
plutôt que sur la fenêtre, ou désactivez-le pour une partie avec `md:ShadowAssist.CacheMode="{x:Null}"`.

| Paramètre du `BitmapCache` | Effet |
|---|---|
| `RenderAtScale` | Résolution de l'image en cache (`1` = taille réelle). Plus élevé = plus net si l'élément est agrandi, mais plus de mémoire. |
| `EnableClearType` | Garde le lissage ClearType du texte, si le fond de l'élément est opaque. |
| `SnapsToDevicePixels` | Aligne l'image sur les pixels de l'écran, pour éviter un léger flou. |

### Quand l'activer

| Utile | Inutile ou contre-productif |
|---|---|
| Écrans avec beaucoup d'éléments ombrés : listes de cartes élevées, tableaux de bord | Écrans avec peu d'ombres. Les styles `Md3.Button.Filled`, `Tonal` et `Error` n'ont pas d'ombre (élévation `Dp0`) |
| Défilement, survol, effets au clic : l'élément ne change pas, l'image en cache est réutilisée | Contenu qui change en permanence (animation continue, couleurs modifiées en direct) : chaque changement oblige à redessiner l'image, avec un surcoût |
| Bureau à distance (RDP, Citrix) ou machines virtuelles, où WPF dessine souvent sans la carte graphique et où les ombres floues coûtent cher | Machine récente avec carte graphique, sans lenteur constatée |

Composants Md3 qui portent une ombre : `Md3.Card.Elevated`, `Md3.Button.Elevated`, `Md3.Button.Fab`,
et les popups (filtre de `FilterDataGrid`, calendrier de `DateRangePicker`).

### Points de vigilance

- **Netteté** : vérifiez à l'œil, surtout avec une mise à l'échelle Windows de 125 % ou 150 %, ou si l'élément est
  agrandi (zoom, `ScaleTransform`). En cas de flou, augmentez `RenderAtScale` (ex. `1.5` ou `2`), au prix de plus de mémoire.
- **Mémoire** : chaque élément mis en cache occupe une image, souvent en mémoire de la carte graphique. Le coût reste
  limité ici, car seules les parties ombrées des modèles sont concernées, et non des écrans entiers.
- **Mesurez avant et après** : sur le poste cible, par exemple en ouvrant l'écran et en le faisant défiler, avec
  et sans le réglage. Gardez-le uniquement si le gain est visible.

---

## 9. Mettre à jour le thème

1. Rouvrir le fichier `.md3theme.json` dans le générateur (**Ouvrir**, Ctrl+O).
2. Modifier les couleurs, formes ou polices.
3. **Exporter XAML** (Ctrl+E) dans le dossier `Themes` de l'application, en écrasant les anciens fichiers.
4. Recompiler l'application.

Gardez le `.md3theme.json` avec le projet (dans le contrôle de source) : c'est la source du thème ;
les fichiers `.xaml` en sont dérivés.

---

## 10. Dépannage

| Symptôme | Cause probable | Correction |
|---|---|---|
| Les couleurs restent violettes (thème par défaut) | Le thème est fusionné avant un autre dictionnaire MaterialDesign (`BundledTheme`…) | Mettre le thème **en dernier** dans `MergedDictionaries` |
| `Cannot find resource named 'Md3.Button.Filled'` | `Components.xaml` absent | Ajouter `Components.xaml` et la référence à `EloNeva.Md3.Controls` |
| `Cannot locate resource 'themes/montheme.light.xaml'` | Fichier mal placé, ou action de génération différente de **Page** | Vérifier le chemin et les propriétés du fichier |
| Le changement clair/sombre n'a pas d'effet sur un élément | Jeton lu avec `StaticResource` | Passer en `DynamicResource` |
| La police n'est pas appliquée | Fenêtre non liée à `Md3.FontFamily` | Voir la [section 4](#4-configurer-les-fenêtres) ; la police doit être installée sur le poste (Roboto est fournie par le toolkit) |
| Dates affichées au format américain | Culture WPF par défaut (`en-US`) | Au démarrage : `FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));` |
| Une colonne modèle ne se filtre pas | Pas de `SortMemberPath` | Renseigner `SortMemberPath` sur la colonne |
| Composants ombrés légèrement flous | Cache des ombres activé ([section 8](#8-performances--cache-des-ombres-facultatif)) | Augmenter `RenderAtScale`, ou retirer `md:ShadowAssist.CacheMode` |
| `Cannot find resource` sur `md:ShadowAssist.CacheMode` | `BitmapCache` déclaré dans les ressources de la fenêtre qui l'utilise | Syntaxe en élément de propriété, ou ressource déclarée dans `App.xaml` |
