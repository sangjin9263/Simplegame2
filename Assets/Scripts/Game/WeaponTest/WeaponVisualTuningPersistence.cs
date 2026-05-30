using System.IO;
using UnityEngine;

// Weapon_test 튜닝값을 JSON 파일로 저장·불러옵니다 (에디터: Assets/Data/WeaponTuning).
public static class WeaponVisualTuningPersistence
{
    const string FolderName = "WeaponTuning";

    public static int GetWeaponId(WeaponVisualKind kind)
    {
        switch (kind)
        {
            case WeaponVisualKind.Ranged:
                return 3002;
            case WeaponVisualKind.Magic:
                return 3003;
            default:
                return 3001;
        }
    }

    public static string GetSaveDirectory()
    {
#if UNITY_EDITOR
        string directory = Path.Combine(Application.dataPath, "Data", FolderName);
#else
        string directory = Path.Combine(Application.persistentDataPath, FolderName);
#endif
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string GetSavePath(int weaponId)
    {
        return Path.Combine(GetSaveDirectory(), $"Weapon_{weaponId}.json");
    }

    public static bool TryLoad(int weaponId, out WeaponVisualTuningSnapshot snapshot)
    {
        snapshot = null;
        string path = GetSavePath(weaponId);
        if (!File.Exists(path))
        {
            return false;
        }

        string json = File.ReadAllText(path);
        snapshot = JsonUtility.FromJson<WeaponVisualTuningSnapshot>(json);
        return snapshot != null;
    }

    public static void Save(WeaponVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        string path = GetSavePath(snapshot.weaponId);
        File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
        Debug.Log("[WeaponTuning] 저장 완료: " + path);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
