using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public struct MonsterDefinitionRow
{
    public int monId;
    public string monName;
    public MonsterKind kind;
    public int level;
    public int hp;
    public int mp;
    public int mpRegen;
    public int damage;
    public float moveSpeed;
    public int giveExp;
    public float attackRange;
    public float attackCooldown;
    public float attackAnimDuration;
    public float fireDelayNormalizedTime;
    public float stopDistance;
    public string prefabName;
    public string projectilePrefab;
    public string hitImpact;
}

// monster_default.csv 로드·저장 (WeaponDefinitionTable 과 동일 패턴).
public class MonsterDefinitionTable
{
    public const string EditorCsvPath = "Assets/Data/CSV/monster_default.csv";
    public const string ResourcesConfigPath = "Config/monster_default";

    static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true);

    readonly List<MonsterDefinitionRow> rows = new List<MonsterDefinitionRow>();
    readonly Dictionary<int, MonsterDefinitionRow> byId = new Dictionary<int, MonsterDefinitionRow>();
    readonly Dictionary<string, MonsterDefinitionRow> byPrefabName = new Dictionary<string, MonsterDefinitionRow>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MonsterDefinitionRow> Rows => rows;

    public static MonsterDefinitionTable LoadDefault()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>(ResourcesConfigPath);
        if (csvAsset == null)
        {
            Debug.LogWarning("[MonsterDefinitionTable] Resources/" + ResourcesConfigPath + " 없음");
            return CreateFallback();
        }

        return LoadFromCsv(csvAsset.text);
    }

    public static MonsterDefinitionTable LoadForEditing()
    {
#if UNITY_EDITOR
        if (File.Exists(EditorCsvPath))
        {
            return LoadFromCsv(File.ReadAllText(EditorCsvPath, Encoding.UTF8));
        }
#endif
        return LoadDefault();
    }

    public static MonsterDefinitionTable LoadFromCsv(string csvText)
    {
        var table = new MonsterDefinitionTable();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return table;
        }

        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int headerIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("mon_id,", StringComparison.OrdinalIgnoreCase))
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex < 0)
        {
            return table;
        }

        Dictionary<string, int> columns = BuildColumnIndex(ParseCsvLine(lines[headerIndex]));
        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            string[] cells = ParseCsvLine(line);
            if (!TryParseRow(cells, columns, out MonsterDefinitionRow row))
            {
                continue;
            }

            table.rows.Add(row);
            table.byId[row.monId] = row;
            table.byPrefabName[row.prefabName] = row;
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

    public void UpsertRow(MonsterDefinitionRow row)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].monId != row.monId)
            {
                continue;
            }

            rows[i] = row;
            byId[row.monId] = row;
            byPrefabName[row.prefabName] = row;
            return;
        }

        rows.Add(row);
        byId[row.monId] = row;
        byPrefabName[row.prefabName] = row;
    }

    public void UpsertFromSnapshot(MonsterVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        UpsertRow(MonsterVisualTuningCsvBridge.ToRow(snapshot));
    }

    public void SaveToEditorCsv()
    {
#if UNITY_EDITOR
        SaveToFile(EditorCsvPath);
        SyncToResources();
#endif
    }

    public static void SyncToResources()
    {
#if UNITY_EDITOR
        const string targetPath = "Assets/Resources/Config/monster_default.csv";
        if (!File.Exists(EditorCsvPath))
        {
            return;
        }

        string csvText = File.ReadAllText(EditorCsvPath, Encoding.UTF8);
        string targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.WriteAllText(EditorCsvPath, csvText, Utf8WithBom);
        File.WriteAllText(targetPath, csvText, Utf8WithBom);
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    public void SaveToFile(string path)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine(ReadCommentBlockFromExistingFile(path));
        builder.AppendLine(BuildHeaderLine());
        for (int i = 0; i < rows.Count; i++)
        {
            builder.AppendLine(BuildDataLine(rows[i]));
        }

        File.WriteAllText(path, builder.ToString(), Utf8WithBom);
    }

    public void CollectSpawnableNormals(List<MonsterDefinitionRow> buffer, HashSet<string> availablePrefabNames)
    {
        buffer.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            MonsterDefinitionRow row = rows[i];
            if (!IsSpawnableRegularKind(row.kind))
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

    static bool TryParseRow(string[] cells, Dictionary<string, int> columns, out MonsterDefinitionRow row)
    {
        row = default;
        if (!TryGetInt(cells, columns, "mon_id", out int monId))
        {
            return false;
        }

        if (!TryGetInt(cells, columns, "mon_type", out int typeValue))
        {
            return false;
        }

        string prefabName = GetString(cells, columns, "mon_prefab");
        if (string.IsNullOrEmpty(prefabName))
        {
            return false;
        }

        row.monId = monId;
        row.monName = GetString(cells, columns, "mon_name");
        row.kind = ParseKind(typeValue);
        row.level = TryGetInt(cells, columns, "mon_level", out int level) ? Mathf.Max(1, level) : 1;
        row.hp = TryGetInt(cells, columns, "mon_hp", out int hp) ? Mathf.Max(1, hp) : 1;
        row.mp = TryGetInt(cells, columns, "mon_mp", out int mp) ? Mathf.Max(0, mp) : 0;
        row.mpRegen = GetInt(cells, columns, "mon_mp_regen", 0);
        if (row.mpRegen == 0)
        {
            row.mpRegen = GetInt(cells, columns, "mon_mp_Regen", 0);
        }

        row.damage = TryGetInt(cells, columns, "mon_dmg", out int damage) ? Mathf.Max(0, damage) : 0;
        row.moveSpeed = GetFloat(cells, columns, "mon_speed", 3.5f);
        row.giveExp = TryGetInt(cells, columns, "give_exp", out int giveExp) ? Mathf.Max(0, giveExp) : 0;
        row.attackRange = TryGetFloat(cells, columns, "mon_atk_range", out float attackRange) ? attackRange : float.NaN;
        row.attackCooldown = TryGetFloat(cells, columns, "mon_cooldown", out float attackCooldown) ? attackCooldown : float.NaN;
        row.attackAnimDuration = TryGetFloat(cells, columns, "mon_anim_duration", out float attackAnimDuration)
            ? attackAnimDuration
            : float.NaN;
        row.fireDelayNormalizedTime = TryGetFloat(cells, columns, "mon_fire_delay_N", out float fireDelayNormalizedTime)
            ? fireDelayNormalizedTime
            : float.NaN;
        row.stopDistance = TryGetFloat(cells, columns, "stop_distance", out float stopDistance) ? stopDistance : float.NaN;
        row.prefabName = prefabName;
        row.projectilePrefab = GetString(cells, columns, "mon_projectile");
        row.hitImpact = GetString(cells, columns, "mon_hit_impact");
        if (string.IsNullOrEmpty(row.hitImpact))
        {
            row.hitImpact = GetString(cells, columns, "Weapon_Hit_Impact");
        }

        row.mpRegen = Mathf.Max(0, row.mpRegen);
        row.moveSpeed = Mathf.Max(0f, row.moveSpeed);
        return true;
    }

    static string BuildHeaderLine()
    {
        return "mon_id,mon_name,mon_type,mon_level,mon_hp,mon_mp,mon_mp_regen,mon_dmg,mon_speed,give_exp,mon_atk_range,mon_cooldown,mon_anim_duration,mon_fire_delay_N,stop_distance,mon_prefab,mon_projectile,mon_hit_impact";
    }

    static string BuildDataLine(MonsterDefinitionRow row)
    {
        return string.Join(",",
            row.monId.ToString(CultureInfo.InvariantCulture),
            row.monName ?? string.Empty,
            ((int)row.kind).ToString(CultureInfo.InvariantCulture),
            row.level.ToString(CultureInfo.InvariantCulture),
            row.hp.ToString(CultureInfo.InvariantCulture),
            row.mp.ToString(CultureInfo.InvariantCulture),
            row.mpRegen.ToString(CultureInfo.InvariantCulture),
            row.damage.ToString(CultureInfo.InvariantCulture),
            row.moveSpeed.ToString(CultureInfo.InvariantCulture),
            row.giveExp.ToString(CultureInfo.InvariantCulture),
            FormatOptionalFloat(row.attackRange),
            FormatOptionalFloat(row.attackCooldown),
            FormatOptionalFloat(row.attackAnimDuration),
            FormatOptionalFloat(row.fireDelayNormalizedTime),
            FormatOptionalFloat(row.stopDistance),
            row.prefabName ?? string.Empty,
            row.projectilePrefab ?? string.Empty,
            row.hitImpact ?? string.Empty);
    }

    static string FormatOptionalFloat(float value)
    {
        if (float.IsNaN(value))
        {
            return string.Empty;
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    static string ReadCommentBlockFromExistingFile(string path)
    {
        if (!File.Exists(path))
        {
            return DefaultCommentBlock();
        }

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);
        var comments = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("#") || string.IsNullOrWhiteSpace(lines[i]))
            {
                comments.AppendLine(lines[i]);
                continue;
            }

            break;
        }

        string block = comments.ToString();
        return string.IsNullOrWhiteSpace(block) ? DefaultCommentBlock() : block.TrimEnd() + "\n";
    }

    static string DefaultCommentBlock()
    {
        return
            "# monster_default.csv - 몬스터 데이터 (Mon_ID: 2001+)\n" +
            "# Mon_Type: 1=근접 2=원거리 3=마법 4=보스\n" +
            "# mon_projectile / mon_hit_impact: type 2·3 위주\n";
    }

    static Dictionary<string, int> BuildColumnIndex(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            map[headers[i].Trim()] = i;
        }

        return map;
    }

    static string[] ParseCsvLine(string line)
    {
        return line.Split(',');
    }

    static bool TryGetInt(string[] cells, Dictionary<string, int> columns, string name, out int value)
    {
        value = 0;
        if (!columns.TryGetValue(name, out int index) || index >= cells.Length)
        {
            return false;
        }

        return int.TryParse(cells[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    static int GetInt(string[] cells, Dictionary<string, int> columns, string name, int defaultValue)
    {
        return TryGetInt(cells, columns, name, out int value) ? value : defaultValue;
    }

    static bool TryGetFloat(string[] cells, Dictionary<string, int> columns, string name, out float value)
    {
        value = 0f;
        if (!columns.TryGetValue(name, out int index) || index >= cells.Length)
        {
            return false;
        }

        string text = cells[index].Trim();
        if (text.Length == 0)
        {
            return false;
        }

        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    static float GetFloat(string[] cells, Dictionary<string, int> columns, string name, float defaultValue)
    {
        if (!columns.TryGetValue(name, out int index) || index >= cells.Length)
        {
            return defaultValue;
        }

        return float.TryParse(cells[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : defaultValue;
    }

    static string GetString(string[] cells, Dictionary<string, int> columns, string name)
    {
        if (!columns.TryGetValue(name, out int index) || index >= cells.Length)
        {
            return string.Empty;
        }

        return cells[index].Trim();
    }

    static MonsterKind ParseKind(int typeValue)
    {
        if (Enum.IsDefined(typeof(MonsterKind), typeValue))
        {
            return (MonsterKind)typeValue;
        }

        return MonsterKind.None;
    }

    static bool IsSpawnableRegularKind(MonsterKind kind)
    {
        return kind == MonsterKind.Melee
            || kind == MonsterKind.Ranged
            || kind == MonsterKind.Mage;
    }

    static MonsterDefinitionTable CreateFallback()
    {
        return LoadFromCsv(
            "# fallback\n" +
            "mon_id,mon_name,mon_type,mon_level,mon_hp,mon_mp,mon_mp_regen,mon_dmg,mon_speed,give_exp,mon_atk_range,mon_cooldown,mon_anim_duration,mon_fire_delay_N,stop_distance,mon_prefab,mon_projectile,mon_hit_impact\n" +
            "2001,오크 근접1,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m1,,Impact_Hit_Lv1");
    }
}
