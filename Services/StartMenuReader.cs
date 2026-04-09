using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StartMenuMGR.Models;

namespace StartMenuMGR.Services;

/// <summary>
/// スタートメニューフォルダを読み取り、ツリー構造に変換するサービス
/// </summary>
public class StartMenuReader
{
    /// <summary>全ユーザー共通のPrograms</summary>
    public static string CommonProgramsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");

    /// <summary>現在のユーザーのPrograms</summary>
    public static string UserProgramsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

    /// <summary>
    /// 2つのスコープからスタートメニューを読み取り、マージしたツリーを返す
    /// </summary>
    public ObservableCollection<StartMenuEntry> ReadAll()
    {
        var merged = new ObservableCollection<StartMenuEntry>();

        var commonEntries = ReadFolder(CommonProgramsPath, isUserScope: false);
        var userEntries = ReadFolder(UserProgramsPath, isUserScope: true);

        // マージ: 同名フォルダは統合、それ以外は両方表示
        MergeEntries(merged, commonEntries);
        MergeEntries(merged, userEntries);

        SortEntries(merged);
        return merged;
    }

    private List<StartMenuEntry> ReadFolder(string folderPath, bool isUserScope)
    {
        var entries = new List<StartMenuEntry>();
        if (!Directory.Exists(folderPath)) return entries;

        // フォルダ
        foreach (var dir in Directory.GetDirectories(folderPath))
        {
            var entry = new StartMenuEntry
            {
                Name = Path.GetFileName(dir),
                FullPath = dir,
                IsFolder = true,
                IsUserScope = isUserScope,
                Icon = GetFolderIcon()
            };

            var children = ReadFolder(dir, isUserScope);
            foreach (var child in children)
            {
                child.Parent = entry;
                entry.Children.Add(child);
            }
            SortEntries(entry.Children);

            // 空フォルダもツリーに含める
            entries.Add(entry);
        }

        // ショートカット (.lnk) とその他のファイル
        foreach (var file in Directory.GetFiles(folderPath))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var entry = new StartMenuEntry
            {
                Name = Path.GetFileNameWithoutExtension(file),
                FullPath = file,
                IsFolder = false,
                IsUserScope = isUserScope
            };

            if (ext == ".lnk")
            {
                var info = ShellLinkReader.ReadShortcut(file);
                if (info.HasValue)
                {
                    entry.TargetPath = info.Value.targetPath;
                    entry.Description = info.Value.description;
                }
            }

            entry.Icon = GetFileIcon(file);
            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    /// src の各エントリを dest にマージする。同名フォルダは子要素を統合。
    /// </summary>
    private void MergeEntries(ObservableCollection<StartMenuEntry> dest, List<StartMenuEntry> src)
    {
        foreach (var entry in src)
        {
            if (entry.IsFolder)
            {
                var existing = dest.FirstOrDefault(e =>
                    e.IsFolder && e.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // 同名フォルダ: 子を統合
                    MergeEntries(existing.Children, entry.Children.ToList());
                    SortEntries(existing.Children);
                    continue;
                }
            }

            dest.Add(entry);
        }
    }

    private void SortEntries(ObservableCollection<StartMenuEntry> entries)
    {
        var sorted = entries
            .OrderByDescending(e => e.IsFolder) // フォルダ優先
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        entries.Clear();
        foreach (var e in sorted)
            entries.Add(e);
    }

    private ImageSource? GetFileIcon(string path)
    {
        try
        {
            var shInfo = new NativeMethods.SHFILEINFO();
            NativeMethods.SHGetFileInfo(
                path, 0, ref shInfo,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf(shInfo),
                NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON);

            if (shInfo.hIcon == IntPtr.Zero) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                shInfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            NativeMethods.DestroyIcon(shInfo.hIcon);
            return source;
        }
        catch
        {
            return null;
        }
    }

    private ImageSource? GetFolderIcon()
    {
        try
        {
            // シェルからフォルダアイコンを取得
            var shInfo = new NativeMethods.SHFILEINFO();
            NativeMethods.SHGetFileInfo(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                NativeMethods.FILE_ATTRIBUTE_DIRECTORY,
                ref shInfo,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf(shInfo),
                NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_SMALLICON | NativeMethods.SHGFI_USEFILEATTRIBUTES);

            if (shInfo.hIcon == IntPtr.Zero) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                shInfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            NativeMethods.DestroyIcon(shInfo.hIcon);
            return source;
        }
        catch
        {
            return null;
        }
    }
}
