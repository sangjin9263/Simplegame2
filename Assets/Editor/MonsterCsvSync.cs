#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Data/CSV 편집본을 Resources로 복사해 플레이에서 읽을 수 있게 합니다.
public static class MonsterCsvSync
{
    const string SourcePath = MonsterDefinitionTable.EditorCsvPath;
    const string TargetPath = "Assets/Resources/Config/monster_default.csv";

    static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true);

    [MenuItem("Simplegame2/Sync Monster CSV To Resources")]
    public static void SyncFromMenu()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogError("[MonsterCsvSync] 소스 없음: " + SourcePath);
            return;
        }

        string targetDirectory = Path.GetDirectoryName(TargetPath);
        if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        string csvText = File.ReadAllText(SourcePath, Encoding.UTF8);
        WriteUtf8WithBom(SourcePath, csvText);
        WriteUtf8WithBom(TargetPath, csvText);
        AssetDatabase.Refresh();
        Debug.Log("[MonsterCsvSync] UTF-8 BOM으로 저장 완료: " + SourcePath + " -> " + TargetPath);
    }

    static void WriteUtf8WithBom(string path, string text)
    {
        File.WriteAllText(path, text, Utf8WithBom);
    }
}
#endif
