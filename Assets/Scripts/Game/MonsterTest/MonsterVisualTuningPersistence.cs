using System.IO;
using UnityEngine;

public static class MonsterVisualTuningPersistence
{
    const string FolderName = "MonsterTuning";

    public static int GetDefaultMonId(MonsterKind kind)
    {
        switch (kind)
        {
            case MonsterKind.Ranged:
                return 2007;
            case MonsterKind.Mage:
                return 2009;
            default:
                return 2001;
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

    public static string GetSavePath(int monId)
    {
        return Path.Combine(GetSaveDirectory(), $"Monster_{monId}.json");
    }

    public static bool TryLoad(int monId, out MonsterVisualTuningSnapshot snapshot)
    {
        snapshot = null;
        string path = GetSavePath(monId);
        if (!File.Exists(path))
        {
            return false;
        }

        string json = File.ReadAllText(path);
        snapshot = JsonUtility.FromJson<MonsterVisualTuningSnapshot>(json);
        return snapshot != null;
    }

    public static void Save(MonsterVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        string path = GetSavePath(snapshot.monId);
        File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
        Debug.Log("[MonsterTuning] 저장 완료: " + path);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
