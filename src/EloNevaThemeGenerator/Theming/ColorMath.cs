using System.Globalization;
using System.Windows.Media;

namespace EloNevaThemeGenerator.Theming;

/// <summary>Couleur exprimée en CIE LCh (luminance perçue L* 0-100, chroma, teinte en degrés).</summary>
public readonly record struct Lch(double L, double C, double H);

/// <summary>
/// Conversions sRGB ⇄ CIELAB/LCh. La « tonalité » Material 3 correspond à L* :
/// une palette tonale garde teinte et chroma et fait varier L* de 0 à 100.
/// </summary>
public static class ColorMath
{
    private const double Xn = 0.95047, Yn = 1.0, Zn = 1.08883;

    public static Lch ToLch(Color color)
    {
        var (l, a, b) = ToLab(color);
        var c = Math.Sqrt(a * a + b * b);
        var h = Math.Atan2(b, a) * 180 / Math.PI;
        if (h < 0) h += 360;
        return new Lch(l, c, h);
    }

    /// <summary>Couleur de tonalité <paramref name="tone"/> ; le chroma est réduit jusqu'à rentrer dans le gamut sRGB.</summary>
    public static Color FromTone(double hue, double chroma, double tone)
    {
        if (tone <= 0) return Colors.Black;
        if (tone >= 100) return Colors.White;

        if (TryFromLch(tone, chroma, hue, out var exact)) return exact;

        double low = 0, high = chroma;
        var best = FromLchClamped(tone, 0, hue);
        for (var i = 0; i < 20; i++)
        {
            var mid = (low + high) / 2;
            if (TryFromLch(tone, mid, hue, out var candidate))
            {
                best = candidate;
                low = mid;
            }
            else
            {
                high = mid;
            }
        }
        return best;
    }

    public static double RelativeLuminance(Color c)
    {
        static double Lin(byte v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);
    }

    /// <summary>Rapport de contraste WCAG entre deux couleurs (1 à 21).</summary>
    public static double Contrast(Color a, Color b)
    {
        var la = RelativeLuminance(a) + 0.05;
        var lb = RelativeLuminance(b) + 0.05;
        return la > lb ? la / lb : lb / la;
    }

    /// <summary>Mélange <paramref name="overlay"/> sur <paramref name="baseColor"/> (couche d'état Material).</summary>
    public static Color Mix(Color baseColor, Color overlay, double amount)
    {
        byte Blend(byte b, byte o) => (byte)Math.Round(b + (o - b) * amount);
        return Color.FromRgb(Blend(baseColor.R, overlay.R), Blend(baseColor.G, overlay.G), Blend(baseColor.B, overlay.B));
    }

    public static Color WithAlpha(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Round(255 * opacity), color.R, color.G, color.B);

    public static string ToHex(Color c) =>
        c.A == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    public static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Trim().TrimStart('#');
        if (s.Length == 3) s = string.Concat(s.Select(ch => $"{ch}{ch}"));
        if (s.Length is not (6 or 8) || !uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            return false;
        color = s.Length == 6
            ? Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v)
            : Color.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
        return true;
    }

    public static Color ParseHex(string? text, Color fallback) => TryParseHex(text, out var c) ? c : fallback;

    private static (double L, double A, double B) ToLab(Color color)
    {
        static double Lin(byte v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        double r = Lin(color.R), g = Lin(color.G), b = Lin(color.B);
        var x = (0.4124564 * r + 0.3575761 * g + 0.1804375 * b) / Xn;
        var y = (0.2126729 * r + 0.7151522 * g + 0.0721750 * b) / Yn;
        var z = (0.0193339 * r + 0.1191920 * g + 0.9503041 * b) / Zn;

        static double F(double t) => t > 216.0 / 24389 ? Math.Cbrt(t) : (24389.0 / 27 * t + 16) / 116;
        double fx = F(x), fy = F(y), fz = F(z);
        return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));
    }

    private static (double R, double G, double B) LchToLinearRgb(double l, double c, double h)
    {
        var hr = h * Math.PI / 180;
        double a = c * Math.Cos(hr), b = c * Math.Sin(hr);
        var fy = (l + 16) / 116;
        var fx = fy + a / 500;
        var fz = fy - b / 200;

        static double FInv(double t) => t * t * t > 216.0 / 24389 ? t * t * t : (116 * t - 16) / (24389.0 / 27);
        double x = FInv(fx) * Xn, y = FInv(fy) * Yn, z = FInv(fz) * Zn;

        return (3.2404542 * x - 1.5371385 * y - 0.4985314 * z,
                -0.9692660 * x + 1.8760108 * y + 0.0415560 * z,
                0.0556434 * x - 0.2040259 * y + 1.0572252 * z);
    }

    private static bool TryFromLch(double l, double c, double h, out Color color)
    {
        var (r, g, b) = LchToLinearRgb(l, c, h);
        const double eps = 0.0001;
        if (r < -eps || r > 1 + eps || g < -eps || g > 1 + eps || b < -eps || b > 1 + eps)
        {
            color = default;
            return false;
        }
        color = FromLinear(r, g, b);
        return true;
    }

    private static Color FromLchClamped(double l, double c, double h)
    {
        var (r, g, b) = LchToLinearRgb(l, c, h);
        return FromLinear(r, g, b);
    }

    private static Color FromLinear(double r, double g, double b)
    {
        static byte Gamma(double v)
        {
            v = Math.Clamp(v, 0, 1);
            var s = v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055;
            return (byte)Math.Round(Math.Clamp(s, 0, 1) * 255);
        }
        return Color.FromRgb(Gamma(r), Gamma(g), Gamma(b));
    }
}

/// <summary>Palette tonale Material 3 : une teinte et un chroma, déclinés par tonalité (0 = noir, 100 = blanc).</summary>
public sealed class TonalPalette(double hue, double chroma)
{
    public double Hue { get; } = hue;
    public double Chroma { get; } = chroma;

    public static readonly int[] StandardTones = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99, 100];

    public static TonalPalette FromColor(Color color)
    {
        var lch = ColorMath.ToLch(color);
        return new TonalPalette(lch.H, lch.C);
    }

    public Color Tone(double tone) => ColorMath.FromTone(Hue, Chroma, tone);
}
