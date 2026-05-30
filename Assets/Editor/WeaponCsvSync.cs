using System.IO;

using System.Text;

using UnityEditor;

using UnityEngine;



// Data/CSV 편집본을 Resources로 복사해 플레이 빌드에서 읽을 수 있게 합니다.

public static class WeaponCsvSync

{

    const string SourcePath = "Assets/Data/CSV/weapon_default.csv";

    const string TargetPath = "Assets/Resources/Config/weapon_default.csv";



    static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true);



    [MenuItem("Tools/Game/Sync Weapon CSV To Resources")]

    public static void SyncFromMenu()

    {

        if (!File.Exists(SourcePath))

        {

            Debug.LogError("[WeaponCsvSync] 소스 없음: " + SourcePath);

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

        Debug.Log("[WeaponCsvSync] UTF-8 BOM으로 저장 완료: " + SourcePath + " -> " + TargetPath);

    }



    static void WriteUtf8WithBom(string path, string text)

    {

        File.WriteAllText(path, text, Utf8WithBom);

    }

}


