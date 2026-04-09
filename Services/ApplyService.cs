using System.Collections.ObjectModel;
using System.IO;
using StartMenuMGR.Models;

namespace StartMenuMGR.Services;

/// <summary>
/// 疑似スタートメニュー上の変更を実際のファイルシステムに適用するサービス
/// </summary>
public class ApplyService
{
    /// <summary>
    /// 現在のツリー状態を実際のスタートメニューフォルダに反映する。
    /// 差分を計算し、必要な移動・作成・削除を行う。
    /// </summary>
    public ApplyResult Apply(ObservableCollection<StartMenuEntry> entries)
    {
        var errors = new List<string>();
        int changesApplied = 0;

        // ツリーを走査し、各エントリの FullPath と実際の場所が異なるものを反映
        ApplyEntries(entries, errors, ref changesApplied);

        if (errors.Count > 0)
            return new ApplyResult
            {
                Success = false,
                ChangesApplied = changesApplied,
                Message = $"{changesApplied}件適用、{errors.Count}件のエラー:\n{string.Join("\n", errors)}"
            };

        return new ApplyResult
        {
            Success = true,
            ChangesApplied = changesApplied,
            Message = changesApplied > 0
                ? $"{changesApplied}件の変更を適用しました。"
                : "変更はありません。"
        };
    }

    private void ApplyEntries(ObservableCollection<StartMenuEntry> entries, List<string> errors, ref int changes)
    {
        foreach (var entry in entries)
        {
            // 現時点では、FullPathが実ファイルシステムと1:1対応しているため
            // 移動・リネーム等の操作が実装されたらここで差分を反映する

            if (entry.IsFolder)
            {
                // フォルダが存在しない場合は作成
                if (!Directory.Exists(entry.FullPath))
                {
                    try
                    {
                        Directory.CreateDirectory(entry.FullPath);
                        changes++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"フォルダ作成失敗: {entry.FullPath} - {ex.Message}");
                    }
                }

                ApplyEntries(entry.Children, errors, ref changes);
            }
        }
    }
}

public class ApplyResult
{
    public bool Success { get; set; }
    public int ChangesApplied { get; set; }
    public string Message { get; set; } = string.Empty;
}
