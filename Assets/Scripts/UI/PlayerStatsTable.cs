using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PlayerLevelProgressionRow
{
    public int id;
    public int level;
    public int expRequired;
    public int hp;
    public int mp;
}

// CSV: stat_id,level,exp,hp,mp — exp는 현재 level에서 다음 level까지 필요한 경험치.
public class PlayerLevelProgressionTable
{
    const string DefaultResourcePath = "Config/player_stats_default";

    readonly List<PlayerLevelProgressionRow> rows = new List<PlayerLevelProgressionRow>();
    readonly Dictionary<int, PlayerLevelProgressionRow> byLevel = new Dictionary<int, PlayerLevelProgressionRow>();

    public IReadOnlyList<PlayerLevelProgressionRow> Rows => rows;
    public int MaxLevel { get; private set; } = 1;

    public static PlayerLevelProgressionTable LoadDefault()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>(DefaultResourcePath);
        if (csvAsset == null)
        {
            Debug.LogWarning("[PlayerLevelProgressionTable] CSV not found at Resources/" + DefaultResourcePath);
            return CreateFallback();
        }

        return LoadFromCsv(csvAsset.text);
    }

    public static PlayerLevelProgressionTable LoadFromCsv(string csvText)
    {
        var table = new PlayerLevelProgressionTable();
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
            if (cells.Length < 5)
            {
                continue;
            }

            if (!int.TryParse(cells[0].Trim(), out int id)
                || !int.TryParse(cells[1].Trim(), out int level)
                || !int.TryParse(cells[2].Trim(), out int expRequired)
                || !int.TryParse(cells[3].Trim(), out int hp)
                || !int.TryParse(cells[4].Trim(), out int mp))
            {
                continue;
            }

            var row = new PlayerLevelProgressionRow
            {
                id = id,
                level = Mathf.Max(1, level),
                expRequired = Mathf.Max(0, expRequired),
                hp = Mathf.Max(1, hp),
                mp = Mathf.Max(0, mp)
            };

            table.rows.Add(row);
            table.byLevel[row.level] = row;
            table.MaxLevel = Mathf.Max(table.MaxLevel, row.level);
        }

        table.rows.Sort((a, b) => a.level.CompareTo(b.level));
        return table;
    }

    public bool TryGetByLevel(int level, out PlayerLevelProgressionRow row)
    {
        return byLevel.TryGetValue(Mathf.Max(1, level), out row);
    }

    public bool TryGetNextLevel(int level, out PlayerLevelProgressionRow row)
    {
        row = default;
        if (!TryGetByLevel(level, out PlayerLevelProgressionRow current))
        {
            return false;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].level == current.level + 1)
            {
                row = rows[i];
                return true;
            }
        }

        return false;
    }

    public int GetExpRequiredForNextLevel(int level)
    {
        if (!TryGetByLevel(level, out PlayerLevelProgressionRow current))
        {
            return 1;
        }

        if (!TryGetNextLevel(level, out _))
        {
            return 0;
        }

        return Mathf.Max(1, current.expRequired);
    }

    static PlayerLevelProgressionTable CreateFallback()
    {
        return LoadFromCsv(@"stat_id,level,exp,hp,mp
1001,1,500,10,5
1002,2,800,15,8");
    }
}
