using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using StartMenuMGR.Models;
using StartMenuMGR.Services;

namespace StartMenuMGR.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly StartMenuReader _reader = new();
    private readonly SnapshotService _snapshot = new();
    private readonly ApplyService _applyService = new();

    private StartMenuEntry? _selectedEntry;
    private string _statusMessage = string.Empty;
    private int _totalItems;

    public ObservableCollection<StartMenuEntry> Entries { get; } = new();

    public StartMenuEntry? SelectedEntry
    {
        get => _selectedEntry;
        set { _selectedEntry = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedEntryInfo)); }
    }

    public string SelectedEntryInfo
    {
        get
        {
            if (_selectedEntry == null) return string.Empty;

            var scope = _selectedEntry.IsUserScope ? "ユーザー" : "全ユーザー";
            if (_selectedEntry.IsFolder)
                return $"[{scope}] フォルダ: {_selectedEntry.FullPath}";

            var info = $"[{scope}] {_selectedEntry.FullPath}";
            if (!string.IsNullOrEmpty(_selectedEntry.TargetPath))
                info += $"\nリンク先: {_selectedEntry.TargetPath}";
            return info;
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public int TotalItems
    {
        get => _totalItems;
        private set { _totalItems = value; OnPropertyChanged(); }
    }

    public bool HasSnapshot => _snapshot.HasSnapshot;

    public ICommand LoadCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand RefreshCommand { get; }

    public MainViewModel()
    {
        LoadCommand = new RelayCommand(Load);
        ApplyCommand = new RelayCommand(Apply, () => _snapshot.HasSnapshot);
        RestoreCommand = new RelayCommand(Restore, () => _snapshot.HasSnapshot);
        RefreshCommand = new RelayCommand(Load);
    }

    public void Load()
    {
        StatusMessage = "スタートメニューを読み取り中...";

        try
        {
            // スナップショット取得（初回のみ）
            if (!_snapshot.HasSnapshot)
            {
                _snapshot.CaptureSnapshot();
                StatusMessage = "起動時スナップショットを取得しました。";
            }

            Entries.Clear();
            var entries = _reader.ReadAll();
            foreach (var entry in entries)
                Entries.Add(entry);

            TotalItems = CountItems(Entries);
            StatusMessage = $"読み取り完了: {TotalItems}項目";
        }
        catch (Exception ex)
        {
            StatusMessage = $"読み取りエラー: {ex.Message}";
        }
    }

    private void Apply()
    {
        var result = MessageBox.Show(
            "疑似スタートメニューの変更を実際のスタートメニューに適用しますか？",
            "適用の確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        var applyResult = _applyService.Apply(Entries);
        StatusMessage = applyResult.Message;

        if (!applyResult.Success)
        {
            MessageBox.Show(applyResult.Message, "適用結果", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Restore()
    {
        var result = MessageBox.Show(
            "起動時の状態に戻しますか？\n現在のスタートメニューの変更は失われます。",
            "元に戻す",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        var restoreResult = _snapshot.Restore();
        StatusMessage = restoreResult.Message;

        if (restoreResult.Success)
        {
            // ツリーをリロード
            Entries.Clear();
            var entries = _reader.ReadAll();
            foreach (var entry in entries)
                Entries.Add(entry);
            TotalItems = CountItems(Entries);
        }
        else
        {
            MessageBox.Show(restoreResult.Message, "復元結果", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private int CountItems(ObservableCollection<StartMenuEntry> entries)
    {
        int count = 0;
        foreach (var e in entries)
        {
            count++;
            if (e.IsFolder)
                count += CountItems(e.Children);
        }
        return count;
    }

    public void Cleanup() => _snapshot.Cleanup();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
