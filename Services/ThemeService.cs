using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace StartMenuMGR.Services;

/// <summary>
/// Windowsのテーマ設定（ダーク/ライト、アクセントカラー）を取得し、UIに反映するサービス
/// </summary>
public class ThemeService
{
    public bool IsDarkMode { get; private set; }
    public Color AccentColor { get; private set; }

    public void DetectTheme()
    {
        IsDarkMode = GetSystemDarkMode();
        AccentColor = GetAccentColor();
    }

    /// <summary>
    /// ウィンドウのタイトルバーをダークモードに対応させる
    /// </summary>
    public void ApplyToWindow(Window window)
    {
        if (!IsDarkMode) return;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int value = 1;
        NativeMethods.DwmSetWindowAttribute(
            hwnd,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref value,
            sizeof(int));
    }

    /// <summary>
    /// テーマに応じたリソースディクショナリを返す
    /// </summary>
    public ResourceDictionary CreateThemeResources()
    {
        var dict = new ResourceDictionary();

        if (IsDarkMode)
        {
            dict["WindowBackground"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
            dict["PanelBackground"] = new SolidColorBrush(Color.FromRgb(43, 43, 43));
            dict["ItemHoverBackground"] = new SolidColorBrush(Color.FromRgb(55, 55, 55));
            dict["ItemSelectedBackground"] = new SolidColorBrush(Color.FromArgb(60, AccentColor.R, AccentColor.G, AccentColor.B));
            dict["TextForeground"] = new SolidColorBrush(Colors.White);
            dict["SecondaryTextForeground"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
            dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            dict["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(55, 55, 55));
            dict["ButtonHoverBackground"] = new SolidColorBrush(Color.FromRgb(70, 70, 70));
            dict["AccentBrush"] = new SolidColorBrush(AccentColor);
            dict["StatusBarBackground"] = new SolidColorBrush(Color.FromRgb(28, 28, 28));
        }
        else
        {
            dict["WindowBackground"] = new SolidColorBrush(Color.FromRgb(243, 243, 243));
            dict["PanelBackground"] = new SolidColorBrush(Colors.White);
            dict["ItemHoverBackground"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            dict["ItemSelectedBackground"] = new SolidColorBrush(Color.FromArgb(60, AccentColor.R, AccentColor.G, AccentColor.B));
            dict["TextForeground"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            dict["SecondaryTextForeground"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            dict["BorderColor"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            dict["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
            dict["ButtonHoverBackground"] = new SolidColorBrush(Color.FromRgb(210, 210, 210));
            dict["AccentBrush"] = new SolidColorBrush(AccentColor);
            dict["StatusBarBackground"] = new SolidColorBrush(Color.FromRgb(235, 235, 235));
        }

        return dict;
    }

    private bool GetSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    private Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\DWM");
            var value = key?.GetValue("AccentColor");
            if (value is int argb)
            {
                var bytes = BitConverter.GetBytes(argb);
                return Color.FromRgb(bytes[0], bytes[1], bytes[2]);
            }
        }
        catch { }

        return Color.FromRgb(0, 120, 215); // Windows 10デフォルトブルー
    }
}
