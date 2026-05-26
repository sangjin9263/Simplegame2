using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MonsterDefinitionRow
{
    public int monId;
    public int level;
    public MonsterKind kind;
    public int hp;
    public int mp;
    public int damage;
    public float moveSpeed;
    public int giveExp;
    public string prefabName;
}

// CSV: mon_id,mon_level,mon_type,mon_hp,mon_mp,mon_dmg,mon_speed,give_exp,mon_prefab
public class MonsterDefinitionTable
{
    const string DefaultResourcePath = "Config/monster_default";

    readonly List<MonsterDefinitionRow> rows = new List<MonsterDefinitionRow>();
    readonly Dictionary<int, MonsterDefinitionRow> byId = new Dictionary<int, MonsterDefinitionRow>();
    readonly Dictionary<string, MonsterDefinitionRow> byPrefabName = new Dictionary<string, MonsterDefinitionRow>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MonsterDefinitionRow> Rows => rows;

    public static MonsterDefinitionTable LoadDefault()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>(DefaultResourcePath);
        if (csvAsset == null)
        {
            Debug.LogWarning("[MonsterDefinitionTable] CSV not found at Resources/" + DefaultResourcePath);
            return CreateFallback();
        }

        return LoadFromCsv(csvAsset.text);
    }

    public static MonsterDefinitionTable LoadFromCsv(string csvText)
    {
        var table = new MonsterDefinitionTable();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return table;
        }

        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            return table;
        }

        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            string[] cells = line.Split(',');
            if (cells.Length < 9)
            {
                continue;
            }

            if (!int.TryParse(cells[0].Trim(), out int monId)
                || !int.TryParse(cells[1].Trim(), out int level)
                || !int.TryParse(cells[2].Trim(), out int typeValue)
                || !int.TryParse(cells[3].Trim(), out int hp)
                || !int.TryParse(cells[4].Trim(), out int mp)
                || !int.TryParse(cells[5].Trim(), out int damage)
                || !float.TryParse(cells[6].Trim(), out float moveSpeed)
                || !int.TryParse(cells[7].Trim(), out int giveExp))
            {
                continue;
            }

            string prefabName = cells[8].Trim();
            if (prefabName.Length == 0)
            {
                continue;
            }

            var row = new MonsterDefinitionRow
            {
                monId = monId,
                level = Mathf.Max(1, level),
                kind = ParseKind(typeValue),
                hp = Mathf.Max(1, hp),
                mp = Mathf.Max(0, mp),
                damage = Mathf.Max(0, damage),
                moveSpeed = Mathf.Max(0f, moveSpeed),
                giveExp = Mathf.Max(0, giveExp),
                prefabName = prefabName
            };

            table.rows.Add(row);
            table.byId[monId] = row;
            table.byPrefabName[prefabName] = row;
        }

        return table;
    }

    public bool TryGetById(int monId, out MonsterDefinitionRow row)
    {
        return byId.TryGetValue(monId, out row);
    }

    public bool TryGetByPrefabName(string prefabName, out MonsterDefinitionRow row)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            row = default;
            return false;
        }

        return byPrefabName.TryGetValue(prefabName.Trim(), out row);
    }

    public void CollectSpawnableNormals(List<MonsterDefinitionRow> buffer, HashSet<string> availablePrefabNames)
    {
        buffer.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            MonsterDefinitionRow row = rows[i];
            if (row.kind != MonsterKind.Normal)
            {
                continue;
            }

            if (availablePrefabNames != null && !availablePrefabNames.Contains(row.prefabName))
            {
                continue;
            }

            buffer.Add(row);
        }
    }

    static MonsterKind ParseKind(int typeValue)
    {
        if (Enum.IsDefined(typeof(MonsterKind), typeValue))
        {
            return (MonsterKind)typeValue;
        }

        return MonsterKind.Normal;
    }

    static MonsterDefinitionTable CreateFallback()
    {
        return LoadFromCsv(@"mon_id,mon_level,mon_type,mon_hp,mon_mp,mon_dmg,mon_speed,give_exp,mon_prefab
2001,1,0,10,0,1,3.5,10,SPUM_orc_m1");
    }
}
