using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private void IconItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is StartMenuEntry entry)
        {
            _viewModel.SelectedEntry = entry;
            NoSelectionText.Visibility = Visibility.Collapsed;
            DetailContent.Visibility = Visibility.Visible;

            TypeText.Text = entry.IsFolder ? $"フォルダ（{entry.ChildCount} 項目）" : "ショートカット";
            ScopeText.Text = entry.IsUserScope ? "現在のユーザー" : "全ユーザー";

            var targetVis = string.IsNullOrEmpty(entry.TargetPath)
                ? Visibility.Collapsed : Visibility.Visible;
            TargetPathText.Visibility = targetVis;
            TargetPathLabel.Visibility = targetVis;

            if (entry.TargetExists.HasValue)
            {
                TargetStatusLabel.Visibility = Visibility.Visible;
                TargetStatusText.Visibility = Visibility.Visible;
                TargetStatusText.Text = entry.TargetExists.Value ? "正常" : "リンク切れ";
                TargetStatusText.Foreground = entry.TargetExists.Value
                    ? (System.Windows.Media.Brush)FindResource("TextForeground")
                    : System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                TargetStatusLabel.Visibility = Visibility.Collapsed;
                TargetStatusText.Visibility = Visibility.Collapsed;
            }

            if (entry.LastModified.HasValue)
            {
                ModifiedLabel.Visibility = Visibility.Visible;
                ModifiedText.Visibility = Visibility.Visible;
                ModifiedText.Text = entry.LastModified.Value.ToString("yyyy/MM/dd HH:mm");
            }
            else
            {
                ModifiedLabel.Visibility = Visibility.Collapsed;
                ModifiedText.Visibility = Visibility.Collapsed;
            }

            SetDetailRow(ArgsLabel, ArgsText, entry.Arguments);
            SetDetailRow(WorkDirLabel, WorkDirText, entry.WorkingDirectory);
        }
    }

    private void TreeItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is StartMenuEntry entry && entry.IsFolder)
        {
            entry.IsExpanded = !entry.IsExpanded;
            e.Handled = true;
        }
    }

    private static void SetDetailRow(System.Windows.Controls.TextBlock label, System.Windows.Controls.TextBlock text, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            label.Visibility = Visibility.Visible;
            text.Visibility = Visibility.Visible;
            text.Text = value;
        }
        else
        {
            label.Visibility = Visibility.Collapsed;
            text.Visibility = Visibility.Collapsed;
        }
    }

    private void MenuTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is StartMenuEntry entry)
        {
            _viewModel.SelectedEntry = entry;
            NoSelectionText.Visibility = Visibility.Collapsed;
            DetailContent.Visibility = Visibility.Visible;

            TypeText.Text = entry.IsFolder ? $"フォルダ（{entry.ChildCount} 項目）" : "ショートカット";
            ScopeText.Text = entry.IsUserScope ? "現在のユーザー" : "全ユーザー";

            var targetVis = string.IsNullOrEmpty(entry.TargetPath)
                ? Visibility.Collapsed : Visibility.Visible;
            TargetPathText.Visibility = targetVis;
            TargetPathLabel.Visibility = targetVis;

            // リンク先状態
            if (entry.TargetExists.HasValue)
            {
                TargetStatusLabel.Visibility = Visibility.Visible;
                TargetStatusText.Visibility = Visibility.Visible;
                TargetStatusText.Text = entry.TargetExists.Value ? "正常" : "リンク切れ";
                TargetStatusText.Foreground = entry.TargetExists.Value
                    ? (System.Windows.Media.Brush)FindResource("TextForeground")
                    : System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                TargetStatusLabel.Visibility = Visibility.Collapsed;
                TargetStatusText.Visibility = Visibility.Collapsed;
            }

            // 更新日時
            if (entry.LastModified.HasValue)
            {
                ModifiedLabel.Visibility = Visibility.Visible;
                ModifiedText.Visibility = Visibility.Visible;
                ModifiedText.Text = entry.LastModified.Value.ToString("yyyy/MM/dd HH:mm");
            }
            else
            {
                ModifiedLabel.Visibility = Visibility.Collapsed;
                ModifiedText.Visibility = Visibility.Collapsed;
            }

            // 引数
            SetDetailRow(ArgsLabel, ArgsText, entry.Arguments);
            // 作業フォルダ
            SetDetailRow(WorkDirLabel, WorkDirText, entry.WorkingDirectory);
        }
        else
        {
            _viewModel.SelectedEntry = null;
            NoSelectionText.Visibility = Visibility.Visible;
            DetailContent.Visibility = Visibility.Collapsed;
        }
    }
}
