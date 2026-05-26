using System.IO;
using UnityEditor;
using UnityEngine;

// Data/CSV 편집본을 Resources로 복사해 플레이 빌드에서 읽을 수 있게 합니다.
public static class PlayerStatsCsvSync
{
    const string SourcePath = "Assets/Data/CSV/player_stats_default.csv";
    const string TargetPath = "Assets/Resources/Config/player_stats_default.csv";

    [MenuItem("Tools/Game/Sync Player Stats CSV To Resources")]
    public static void SyncFromMenu()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogError("[PlayerStatsCsvSync] 소스 없음: " + SourcePath);
            return;
        }

        string targetDirectory = Path.GetDirectoryName(TargetPath);
        if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.Copy(SourcePath, TargetPath, true);
        AssetDatabase.Refresh();
        Debug.Log("[PlayerStatsCsvSync] 복사 완료: " + SourcePath + " → " + TargetPath);
    }
}
