using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace StartMenuMGR.Services;

/// <summary>
/// Windows 10 スタートメニューのピン留めタイル情報をレジストリから読み取る
/// </summary>
public class PinnedTileReader
{
    private static readonly string TileGridKeyPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\" +
        @"$de${8bb38f94-f5e1-44cd-abf7-6d995b9e7fd8}$start.tilegrid$windows.data.curatedtilecollection.tilecollection\Current";

    // KnownFolder GUIDs → 実パス
    private static readonly Dictionary<string, string> KnownFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        { "{6D809377-6AF0-444B-8957-A3773F02200E}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) },
        { "{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) },
        { "{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32") },
        { "{F38BF404-1D43-42F2-9305-67DE0B28FC23}", Environment.GetFolderPath(Environment.SpecialFolder.Windows) },
        { "{D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27}", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "system32") },
    };

    /// <summary>
    /// ピン留めされたアプリの識別子リストを返す
    /// </summary>
    public List<PinnedTileInfo> ReadPinnedTiles()
    {
        var result = new List<PinnedTileInfo>();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(TileGridKeyPath);
            if (key == null) return result;

            var data = key.GetValue("Data") as byte[];
            if (data == null) return result;

            var text = Encoding.Unicode.GetString(data);
            var matches = Regex.Matches(text, @"[WP]~[^\x00-\x1F]+");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in matches)
            {
                var raw = m.Value.TrimEnd('\uFFFD', '\uFFFE', '\uFFFF');
                // 末尾のゴミバイトを除去
                raw = CleanEntry(raw);

                if (string.IsNullOrWhiteSpace(raw) || !seen.Add(raw))
                    continue;

                var info = new PinnedTileInfo { RawId = raw };

                if (raw.StartsWith("W~"))
                {
                    info.Type = TileType.Desktop;
                    info.ResolvedPath = ResolveDesktopPath(raw[2..]);
                }
                else if (raw.StartsWith("P~"))
                {
                    info.Type = TileType.UWP;
                    info.AppUserModelId = raw[2..];
                }

                result.Add(info);
            }
        }
        catch
        {
            // レジストリ読み取り失敗: 空リスト
        }

        return result;
    }

    private static string CleanEntry(string raw)
    {
        // 制御文字以外の末尾ゴミを除去
        var sb = new StringBuilder();
        foreach (var c in raw)
        {
            if (c < 0x20 && c != '\t') break;
            sb.Append(c);
        }
        return sb.ToString().TrimEnd();
    }

    private static string? ResolveDesktopPath(string id)
    {
        // {GUID}\relative\path 形式
        if (id.StartsWith("{"))
        {
            var closeBrace = id.IndexOf('}');
            if (closeBrace > 0 && closeBrace + 1 < id.Length)
            {
                var guid = id[..(closeBrace + 1)];
                var relativePath = id[(closeBrace + 1)..].TrimStart('\\');

                if (KnownFolders.TryGetValue(guid, out var basePath))
                    return Path.Combine(basePath, relativePath);
            }
        }

        // フルパス
        if (Path.IsPathRooted(id))
            return id;

        // 名前のみ（Chrome, Microsoft.Windows.ControlPanel 等）
        return null;
    }
}

public class PinnedTileInfo
{
    public string RawId { get; set; } = string.Empty;
    public TileType Type { get; set; }
    public string? ResolvedPath { get; set; }
    public string? AppUserModelId { get; set; }
}

public enum TileType
{
    Desktop,
    UWP
}
