using System.IO;
using System.Text.Json;

namespace StartMenuMGR.Services;

/// <summary>
/// 起動時のスタートメニュー状態をスナップショットとして保存し、復元するサービス
/// </summary>
public class SnapshotService
{
    private readonly string _snapshotDir;
    private SnapshotData? _snapshot;

    public SnapshotService()
    {
        _snapshotDir = Path.Combine(Path.GetTempPath(), "StartMenuMGR_Snapshot");
    }

    public bool HasSnapshot => _snapshot != null;

    /// <summary>
    /// 起動時に呼び出し。スタートメニューの全ファイル構造を記録する。
    /// </summary>
    public void CaptureSnapshot()
    {
        _snapshot = new SnapshotData
        {
            CapturedAt = DateTime.Now,
            Entries = new List<SnapshotEntry>()
        };

        CaptureFolder(StartMenuReader.CommonProgramsPath, isUserScope: false);
        CaptureFolder(StartMenuReader.UserProgramsPath, isUserScope: true);

        // スナップショット用ディレクトリにファイルのバックアップコピーを保存
        if (Directory.Exists(_snapshotDir))
            Directory.Delete(_snapshotDir, true);
        Directory.CreateDirectory(_snapshotDir);

        foreach (var entry in _snapshot.Entries)
        {
            if (entry.IsFolder)
            {
                // フォルダの存在を記録するだけ
                continue;
            }

            // ファイルをバックアップ
            var relativePath = entry.IsUserScope
                ? Path.GetRelativePath(StartMenuReader.UserProgramsPath, entry.FullPath)
                : Path.GetRelativePath(StartMenuReader.CommonProgramsPath, entry.FullPath);
            var scope = entry.IsUserScope ? "User" : "Common";
            var backupPath = Path.Combine(_snapshotDir, scope, relativePath);

            var backupDir = Path.GetDirectoryName(backupPath);
            if (backupDir != null && !Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            try
            {
                File.Copy(entry.FullPath, backupPath, true);
            }
            catch
            {
                // アクセス権等でコピーできない場合はスキップ
            }
        }

        // メタデータ保存
        var metaPath = Path.Combine(_snapshotDir, "snapshot.json");
        var json = JsonSerializer.Serialize(_snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaPath, json);
    }

    /// <summary>
    /// 起動時のスナップショットに復元する
    /// </summary>
    public RestoreResult Restore()
    {
        if (_snapshot == null)
            return new RestoreResult { Success = false, Message = "スナップショットが存在しません。" };

        var errors = new List<string>();

        // 1. 現在の状態で、スナップショットに無いファイル/フォルダを削除
        CleanNewItems(StartMenuReader.CommonProgramsPath, isUserScope: false, errors);
        CleanNewItems(StartMenuReader.UserProgramsPath, isUserScope: true, errors);

        // 2. スナップショットのファイルを復元
        foreach (var entry in _snapshot.Entries)
        {
            if (entry.IsFolder)
            {
                if (!Directory.Exists(entry.FullPath))
                {
                    try { Directory.CreateDirectory(entry.FullPath); }
                    catch (Exception ex) { errors.Add($"フォルダ復元失敗: {entry.FullPath} - {ex.Message}"); }
                }
                continue;
            }

            var relativePath = entry.IsUserScope
                ? Path.GetRelativePath(StartMenuReader.UserProgramsPath, entry.FullPath)
                : Path.GetRelativePath(StartMenuReader.CommonProgramsPath, entry.FullPath);
            var scope = entry.IsUserScope ? "User" : "Common";
            var backupPath = Path.Combine(_snapshotDir, scope, relativePath);

            if (!File.Exists(backupPath)) continue;

            try
            {
                var dir = Path.GetDirectoryName(entry.FullPath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(backupPath, entry.FullPath, true);
            }
            catch (Exception ex)
            {
                errors.Add($"ファイル復元失敗: {entry.FullPath} - {ex.Message}");
            }
        }

        if (errors.Count > 0)
            return new RestoreResult { Success = false, Message = $"一部の復元に失敗しました:\n{string.Join("\n", errors)}" };

        return new RestoreResult { Success = true, Message = "起動時の状態に復元しました。" };
    }

    private void CaptureFolder(string folderPath, bool isUserScope)
    {
        if (!Directory.Exists(folderPath)) return;

        // フォルダ自体を記録
        foreach (var dir in Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
        {
            _snapshot!.Entries.Add(new SnapshotEntry
            {
                FullPath = dir,
                IsFolder = true,
                IsUserScope = isUserScope
            });
        }

        // ファイルを記録
        foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            _snapshot!.Entries.Add(new SnapshotEntry
            {
                FullPath = file,
                IsFolder = false,
                IsUserScope = isUserScope
            });
        }
    }

    /// <summary>
    /// スナップショットに無いアイテムを削除する
    /// </summary>
    private void CleanNewItems(string rootPath, bool isUserScope, List<string> errors)
    {
        if (!Directory.Exists(rootPath)) return;

        var snapshotPaths = _snapshot!.Entries
            .Where(e => e.IsUserScope == isUserScope)
            .Select(e => e.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ファイル
        foreach (var file in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            if (!snapshotPaths.Contains(file))
            {
                try { File.Delete(file); }
                catch (Exception ex) { errors.Add($"削除失敗: {file} - {ex.Message}"); }
            }
        }

        // 空フォルダ（深い順に削除）
        foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length))
        {
            if (!snapshotPaths.Contains(dir) && Directory.Exists(dir))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch (Exception ex) { errors.Add($"フォルダ削除失敗: {dir} - {ex.Message}"); }
            }
        }
    }

    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_snapshotDir))
                Directory.Delete(_snapshotDir, true);
        }
        catch { }
    }
}

public class SnapshotData
{
    public DateTime CapturedAt { get; set; }
    public List<SnapshotEntry> Entries { get; set; } = new();
}

public class SnapshotEntry
{
    public string FullPath { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public bool IsUserScope { get; set; }
}

public class RestoreResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
