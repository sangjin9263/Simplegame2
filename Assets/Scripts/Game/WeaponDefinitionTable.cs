using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public struct WeaponDefinitionRow
{
    public int weaponId;
    public string weaponName;
    public int weaponType;
    public int weaponLevel;
    public int damage;
    public float atkCooldown;
    public float atkActiveDelay;
    public float atkAnimDuration;
    public string equPosition;
    public string weaponImage;
    public string vfxPrefab1;
    public string vfxPrefab2;
    public string weaponHitImpact;
    public float vfx1Scale;
    public float vfx1Speed;
    public float vfx1Range;
    public float vfx1HitRadius;
    public float vfx1TurnSpeed;
    public float vfx1ExplosionRadius;
    public int vfx1MaxTargets;
    public float vfx1Lifetime;
    public float vfx1SearchRange;
}

// weapon_default.csv 로드·저장 (MonsterDefinitionTable 과 동일 패턴).
public class WeaponDefinitionTable
{
    public const string EditorCsvPath = "Assets/Data/CSV/weapon_default.csv";
    public const string ResourcesConfigPath = "Config/weapon_default";
    const float MeleeHitLengthAtUnitScale = 3.48f;

    static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true);

    readonly List<WeaponDefinitionRow> rows = new List<WeaponDefinitionRow>();
    readonly Dictionary<int, WeaponDefinitionRow> byId = new Dictionary<int, WeaponDefinitionRow>();

    public IReadOnlyList<WeaponDefinitionRow> Rows => rows;

    public static WeaponDefinitionTable LoadDefault()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>(ResourcesConfigPath);
        if (csvAsset == null)
        {
            Debug.LogWarning("[WeaponDefinitionTable] Resources/" + ResourcesConfigPath + " 없음");
            return new WeaponDefinitionTable();
        }

        return LoadFromCsv(csvAsset.text);
    }

    public static WeaponDefinitionTable LoadForEditing()
    {
#if UNITY_EDITOR
        if (File.Exists(EditorCsvPath))
        {
            return LoadFromCsv(File.ReadAllText(EditorCsvPath, Encoding.UTF8));
        }
#endif
        return LoadDefault();
    }

    public static WeaponDefinitionTable LoadFromCsv(string csvText)
    {
        var table = new WeaponDefinitionTable();
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return table;
        }

        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int headerIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("Weapon_ID,", StringComparison.Ordinal))
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
            if (!TryParseRow(cells, columns, out WeaponDefinitionRow row))
            {
                continue;
            }

            table.rows.Add(row);
            table.byId[row.weaponId] = row;
        }

        return table;
    }

    public bool TryGetById(int weaponId, out WeaponDefinitionRow row)
    {
        return byId.TryGetValue(weaponId, out row);
    }

    public void UpsertFromSnapshot(WeaponVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        WeaponDefinitionRow row = WeaponVisualTuningCsvBridge.ToRow(snapshot);
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].weaponId != row.weaponId)
            {
                continue;
            }

            rows[i] = row;
            byId[row.weaponId] = row;
            return;
        }

        rows.Add(row);
        byId[row.weaponId] = row;
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
        const string targetPath = "Assets/Resources/Config/weapon_default.csv";
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
            "# weapon_default.csv - 무기 데이터 (Weapon_ID: 3001+)\n" +
            "# Weapon_Type: 1=Melee 2=Ranged 3=Magic\n" +
            "# Vfx1_Scale: 검=VFX+히트박스 | 활/스태프=투사체 scale\n" +
            "# Spawn offset 등은 WeaponTuning JSON 에 저장\n";
    }

    static string BuildHeaderLine()
    {
        return "Weapon_ID,Weapon_Name,Weapon_Type,Weapon_LV,DMG,Atk_cooldown,Atk_Active_Delay,Atk_Anim_Duration,Equ_position,Weapon_image,Vfx_Prefab1,Vfx_Prefab2,Weapon_Charge_Impact,Weapon_Charge_time,Weapon_Hit_Impact,Vfx1_Number,Vfx1_Scale,Vfx1_Speed,Vfx1_Range,Vfx1_Hit_Radius,Vfx1_Turn_Speed,Vfx1_Explosion_Radius,Vfx1_Max_Targets,Vfx1_Lifetime,Vfx1_Search_Range,Vfx2_Number,Vfx2_Speed,Vfx2_Range,Vfx2_Hit_Radius,Vfx2_Turn_Speed,Vfx2_Explosion_Radius,Vfx2_Max_Targets,Vfx2_Lifetime,Vfx2_Search_Range";
    }

    string BuildDataLine(WeaponDefinitionRow row)
    {
        float vfx1Range = row.weaponType == 1 ? MeleeHitLengthAtUnitScale : row.vfx1Range;
        return string.Join(",",
            row.weaponId.ToString(CultureInfo.InvariantCulture),
            row.weaponName ?? string.Empty,
            row.weaponType.ToString(CultureInfo.InvariantCulture),
            row.weaponLevel.ToString(CultureInfo.InvariantCulture),
            row.damage.ToString(CultureInfo.InvariantCulture),
            row.atkCooldown.ToString(CultureInfo.InvariantCulture),
            row.atkActiveDelay.ToString(CultureInfo.InvariantCulture),
            row.atkAnimDuration.ToString(CultureInfo.InvariantCulture),
            row.equPosition ?? string.Empty,
            row.weaponImage ?? string.Empty,
            row.vfxPrefab1 ?? string.Empty,
            row.vfxPrefab2 ?? string.Empty,
            string.Empty,
            string.Empty,
            row.weaponHitImpact ?? string.Empty,
            "1",
            row.vfx1Scale.ToString(CultureInfo.InvariantCulture),
            row.vfx1Speed.ToString(CultureInfo.InvariantCulture),
            vfx1Range.ToString(CultureInfo.InvariantCulture),
            row.vfx1HitRadius.ToString(CultureInfo.InvariantCulture),
            row.vfx1TurnSpeed.ToString(CultureInfo.InvariantCulture),
            row.vfx1ExplosionRadius.ToString(CultureInfo.InvariantCulture),
            row.vfx1MaxTargets.ToString(CultureInfo.InvariantCulture),
            row.vfx1Lifetime.ToString(CultureInfo.InvariantCulture),
            row.vfx1SearchRange.ToString(CultureInfo.InvariantCulture),
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    static bool TryParseRow(string[] cells, Dictionary<string, int> columns, out WeaponDefinitionRow row)
    {
        row = default;
        if (!TryGetInt(cells, columns, "Weapon_ID", out int weaponId))
        {
            return false;
        }

        row.weaponId = weaponId;
        row.weaponName = GetString(cells, columns, "Weapon_Name");
        row.weaponType = TryGetInt(cells, columns, "Weapon_Type", out int weaponType) ? weaponType : 1;
        row.weaponLevel = TryGetInt(cells, columns, "Weapon_LV", out int weaponLevel) ? weaponLevel : 1;
        row.damage = TryGetInt(cells, columns, "DMG", out int damage) ? damage : 1;
        row.atkCooldown = GetFloat(cells, columns, "Atk_cooldown", 0.45f);
        row.atkActiveDelay = GetFloat(cells, columns, "Atk_Active_Delay", 0.08f);
        row.atkAnimDuration = GetFloat(cells, columns, "Atk_Anim_Duration", 0.3f);
        row.equPosition = GetString(cells, columns, "Equ_position");
        row.weaponImage = GetString(cells, columns, "Weapon_image");
        row.vfxPrefab1 = GetString(cells, columns, "Vfx_Prefab1");
        row.vfxPrefab2 = GetString(cells, columns, "Vfx_Prefab2");
        row.weaponHitImpact = GetString(cells, columns, "Weapon_Hit_Impact");
        row.vfx1Scale = GetFloat(cells, columns, "Vfx1_Scale", 1f);
        row.vfx1Speed = GetFloat(cells, columns, "Vfx1_Speed", 14f);
        row.vfx1Range = GetFloat(cells, columns, "Vfx1_Range", 0f);
        row.vfx1HitRadius = GetFloat(cells, columns, "Vfx1_Hit_Radius", 0f);
        row.vfx1TurnSpeed = GetFloat(cells, columns, "Vfx1_Turn_Speed", 0f);
        row.vfx1ExplosionRadius = GetFloat(cells, columns, "Vfx1_Explosion_Radius", 0f);
        row.vfx1MaxTargets = TryGetInt(cells, columns, "Vfx1_Max_Targets", out int maxTargets) ? maxTargets : 0;
        row.vfx1Lifetime = GetFloat(cells, columns, "Vfx1_Lifetime", 1f);
        row.vfx1SearchRange = GetFloat(cells, columns, "Vfx1_Search_Range", 0f);
        return true;
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
}
