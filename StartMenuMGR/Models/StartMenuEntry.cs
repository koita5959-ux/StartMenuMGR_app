using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StartMenuMGR.Models;

/// <summary>
/// スタートメニュー内の1項目（フォルダまたはショートカット）を表すモデル
/// </summary>
public class StartMenuEntry : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _fullPath = string.Empty;
    private bool _isFolder;
    private bool _isExpanded;
    private bool _isUserScope;
    private bool _isGroupHeader;
    private string? _targetPath;
    private string? _description;
    private System.Windows.Media.ImageSource? _icon;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>実ファイルシステム上のパス</summary>
    public string FullPath
    {
        get => _fullPath;
        set { _fullPath = value; OnPropertyChanged(); }
    }

    public bool IsFolder
    {
        get => _isFolder;
        set { _isFolder = value; OnPropertyChanged(); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    /// <summary>アルファベットグループヘッダー（#, A, B, C...）</summary>
    public bool IsGroupHeader
    {
        get => _isGroupHeader;
        set { _isGroupHeader = value; OnPropertyChanged(); }
    }

    /// <summary>true=ユーザースコープ, false=全ユーザースコープ</summary>
    public bool IsUserScope
    {
        get => _isUserScope;
        set { _isUserScope = value; OnPropertyChanged(); }
    }

    /// <summary>.lnkのリンク先パス（フォルダの場合はnull）</summary>
    public string? TargetPath
    {
        get => _targetPath;
        set { _targetPath = value; OnPropertyChanged(); }
    }

    /// <summary>.lnkの説明欄</summary>
    public string? Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public System.Windows.Media.ImageSource? Icon
    {
        get => _icon;
        set { _icon = value; OnPropertyChanged(); }
    }

    /// <summary>ファイルの更新日時</summary>
    public DateTime? LastModified { get; set; }

    /// <summary>ファイルサイズ（バイト）</summary>
    public long? FileSize { get; set; }

    /// <summary>.lnkの引数</summary>
    public string? Arguments { get; set; }

    /// <summary>.lnkの作業ディレクトリ</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>リンク先が存在するか</summary>
    public bool? TargetExists { get; set; }

    /// <summary>フォルダ内の子項目数</summary>
    public int ChildCount => Children.Count;

    public ObservableCollection<StartMenuEntry> Children { get; } = new();

    /// <summary>親エントリへの参照（ルートの場合はnull）</summary>
    public StartMenuEntry? Parent { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
