using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EloNevaThemeGenerator.Theming;

namespace EloNevaThemeGenerator.ViewModels;

/// <summary>Un rôle de couleur MD3 affiché dans l'éditeur (modifiable = surcharge manuelle).</summary>
public sealed partial class ColorRoleViewModel : ObservableObject
{
    private readonly Action<ColorRoleViewModel, Color?> _onUserChange;
    private bool _syncing;

    public ColorRoleViewModel(Md3Role role, Action<ColorRoleViewModel, Color?> onUserChange)
    {
        Role = role;
        _onUserChange = onUserChange;
    }

    public Md3Role Role { get; }
    public string Name => Role.Name;
    public string Group => Role.Group;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush), nameof(Hex), nameof(ContentBrush))]
    private Color _color;

    [ObservableProperty]
    private bool _isOverridden;

    public SolidColorBrush Brush => new(Color);

    public string Hex => ColorMath.ToHex(Color);

    /// <summary>Couleur de texte lisible sur la pastille.</summary>
    public SolidColorBrush ContentBrush =>
        ColorMath.Contrast(Color, Colors.White) >= ColorMath.Contrast(Color, Colors.Black) ? Brushes.White : Brushes.Black;

    /// <summary>Met à jour la couleur depuis le moteur de thème, sans la considérer comme une saisie.</summary>
    public void Sync(Color color, bool isOverridden)
    {
        _syncing = true;
        Color = color;
        IsOverridden = isOverridden;
        _syncing = false;
    }

    partial void OnColorChanged(Color value)
    {
        if (!_syncing) _onUserChange(this, value);
    }

    [RelayCommand]
    private void Reset() => _onUserChange(this, null);
}
