using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EloNevaThemeGenerator.Theming;

namespace EloNevaThemeGenerator.Views;

/// <summary>Pastille de couleur + code hexadécimal ; un clic sur la pastille ouvre le sélecteur Material Design.</summary>
public partial class ColorEditor : UserControl
{
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(Color), typeof(ColorEditor),
        new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(ColorEditor), new PropertyMetadata(string.Empty));

    public ColorEditor()
    {
        InitializeComponent();
        HexBox.Text = ColorMath.ToHex(Color);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ColorEditor)d;
        if (!editor.HexBox.IsKeyboardFocused)
            editor.HexBox.Text = ColorMath.ToHex((Color)e.NewValue);
    }

    private void OnHexKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CommitHex();
    }

    private void OnHexLostFocus(object sender, KeyboardFocusChangedEventArgs e) => CommitHex();

    private void CommitHex()
    {
        if (ColorMath.TryParseHex(HexBox.Text, out var color))
            Color = Color.FromRgb(color.R, color.G, color.B);
        HexBox.Text = ColorMath.ToHex(Color);
    }
}
