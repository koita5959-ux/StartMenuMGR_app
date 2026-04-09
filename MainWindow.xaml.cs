using System.Windows;
using System.Windows.Controls;
using StartMenuMGR.Models;
using StartMenuMGR.Services;
using StartMenuMGR.ViewModels;

namespace StartMenuMGR;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ThemeService _themeService;

    public MainWindow()
    {
        _themeService = new ThemeService();
        _themeService.DetectTheme();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        InitializeComponent();

        // テーマリソースを適用
        Resources.MergedDictionaries.Add(_themeService.CreateThemeResources());

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // タイトルバーのダークモード対応
        _themeService.ApplyToWindow(this);

        // スタートメニュー読み取り
        _viewModel.Load();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void MenuTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is StartMenuEntry entry)
        {
            _viewModel.SelectedEntry = entry;
            NoSelectionText.Visibility = Visibility.Collapsed;
            DetailContent.Visibility = Visibility.Visible;

            TypeText.Text = entry.IsFolder ? "フォルダ" : "ショートカット";
            ScopeText.Text = entry.IsUserScope ? "現在のユーザー (AppData)" : "全ユーザー (ProgramData)";
            TargetPathPanel.Visibility = string.IsNullOrEmpty(entry.TargetPath)
                ? Visibility.Collapsed : Visibility.Visible;
            DescriptionPanel.Visibility = string.IsNullOrEmpty(entry.Description)
                ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            _viewModel.SelectedEntry = null;
            NoSelectionText.Visibility = Visibility.Visible;
            DetailContent.Visibility = Visibility.Collapsed;
        }
    }
}
