#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Weapon_test 저장값을 SPUM_main 프리팹 + weapon_default.csv 에 반영합니다.
public static class WeaponVisualTuningSampleSceneApplier
{
    const string PlayerPrefabPath = "Assets/Prefabs/Char/SPUM_main.prefab";
    const string CsvSourcePath = "Assets/Data/CSV/weapon_default.csv";
    const string CsvResourcesPath = "Assets/Resources/Config/weapon_default.csv";
    const float MeleeHitLengthAtUnitScale = 3.48f;

    static readonly UTF8Encoding Utf8WithBom = new UTF8Encoding(true);

    public static void ApplyAllSavedToSampleScene()
    {
        int appliedCount = 0;
        ApplyWeaponIfSaved(WeaponVisualKind.Melee, ref appliedCount);
        ApplyWeaponIfSaved(WeaponVisualKind.Ranged, ref appliedCount);
        ApplyWeaponIfSaved(WeaponVisualKind.Magic, ref appliedCount);

        SyncWeaponCsvToResources();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WeaponTuning] SampleScene 반영 완료 — SPUM_main + CSV ({appliedCount}개 무기). SampleScene Play 시 적용됩니다.");
    }

    static void SyncWeaponCsvToResources()
    {
        if (!File.Exists(CsvSourcePath))
        {
            return;
        }

        string csvText = File.ReadAllText(CsvSourcePath, Encoding.UTF8);
        string targetDirectory = Path.GetDirectoryName(CsvResourcesPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.WriteAllText(CsvSourcePath, csvText, Utf8WithBom);
        File.WriteAllText(CsvResourcesPath, csvText, Utf8WithBom);
    }

    static void ApplyWeaponIfSaved(WeaponVisualKind kind, ref int appliedCount)
    {
        int weaponId = WeaponVisualTuningPersistence.GetWeaponId(kind);
        if (!WeaponVisualTuningPersistence.TryLoad(weaponId, out WeaponVisualTuningSnapshot snapshot))
        {
            Debug.LogWarning($"[WeaponTuning] 저장 파일 없음: Weapon_{weaponId}.json — 먼저 「저장」 하세요.");
            return;
        }

        ApplySnapshotToPlayerPrefab(snapshot);
        ApplySnapshotToCsv(snapshot);
        appliedCount++;
    }

    static void ApplySnapshotToPlayerPrefab(WeaponVisualTuningSnapshot snapshot)
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[WeaponTuning] SPUM_main 프리팹을 찾을 수 없습니다: " + PlayerPrefabPath);
            return;
        }

        switch (snapshot.kind)
        {
            case WeaponVisualKind.Ranged:
                ApplyToRangedCombat(prefabRoot, snapshot);
                break;
            case WeaponVisualKind.Magic:
                ApplyToMagicCombat(prefabRoot, snapshot);
                break;
            default:
                ApplyToMeleeCombat(prefabRoot, snapshot);
                break;
        }

        EditorUtility.SetDirty(prefabRoot);
        PrefabUtility.SavePrefabAsset(prefabRoot);
    }

    static void ApplyToMeleeCombat(GameObject prefabRoot, WeaponVisualTuningSnapshot snapshot)
    {
        PlayerWeaponCombat combat = prefabRoot.GetComponent<PlayerWeaponCombat>();
        if (combat == null)
        {
            Debug.LogError("[WeaponTuning] PlayerWeaponCombat 없음");
            return;
        }

        SerializedObject serialized = new SerializedObject(combat);
        SetFloat(serialized, "attackCooldown", snapshot.attackCooldown);
        SetFloat(serialized, "attackActiveDelay", snapshot.attackActiveDelay);
        SetFloat(serialized, "attackAnimDuration", snapshot.attackAnimDuration);
        SetFloat(serialized, "waveSpeed", snapshot.moveSpeed);
        SetFloat(serialized, "slashVfxScale", snapshot.visualScale);
        SetVector3(serialized, "slashVfxRotationOffset", snapshot.visualRotationOffset);
        SetFloat(serialized, "slashSpawnForwardOffset", snapshot.spawnForwardOffset);
        SetFloat(serialized, "slashSpawnSideOffset", snapshot.spawnSideOffset);
        SetFloat(serialized, "slashSpawnHeightOffset", snapshot.spawnHeightOffset);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyToRangedCombat(GameObject prefabRoot, WeaponVisualTuningSnapshot snapshot)
    {
        PlayerRangedCombat combat = prefabRoot.GetComponent<PlayerRangedCombat>();
        if (combat == null)
        {
            Debug.LogError("[WeaponTuning] PlayerRangedCombat 없음");
            return;
        }

        SerializedObject serialized = new SerializedObject(combat);
        SetFloat(serialized, "attackCooldown", snapshot.attackCooldown);
        SetFloat(serialized, "attackActiveDelay", snapshot.attackActiveDelay);
        SetFloat(serialized, "attackAnimDuration", snapshot.attackAnimDuration);
        SetFloat(serialized, "projectileSpeed", snapshot.moveSpeed);
        SetFloat(serialized, "projectileMaxRange", snapshot.maxRange);
        SetFloat(serialized, "projectileHitRadius", snapshot.hitRadius);
        SetFloat(serialized, "arrowVisualScale", snapshot.visualScale);
        SetVector3(serialized, "arrowVisualRotationOffset", snapshot.visualRotationOffset);
        SetFloat(serialized, "projectileSpawnForwardOffset", snapshot.spawnForwardOffset);
        SetFloat(serialized, "projectileSpawnHeightOffset", snapshot.spawnHeightOffset);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyToMagicCombat(GameObject prefabRoot, WeaponVisualTuningSnapshot snapshot)
    {
        PlayerMagicCombat combat = prefabRoot.GetComponent<PlayerMagicCombat>();
        if (combat == null)
        {
            Debug.LogError("[WeaponTuning] PlayerMagicCombat 없음");
            return;
        }

        SerializedObject serialized = new SerializedObject(combat);
        SetFloat(serialized, "attackCooldown", snapshot.attackCooldown);
        SetFloat(serialized, "attackActiveDelay", snapshot.attackActiveDelay);
        SetFloat(serialized, "attackAnimDuration", snapshot.attackAnimDuration);
        SetFloat(serialized, "projectileSpeed", snapshot.moveSpeed);
        SetFloat(serialized, "projectileHitRadius", snapshot.hitRadius);
        SetFloat(serialized, "projectileTurnSpeed", snapshot.turnSpeed);
        SetFloat(serialized, "projectileExplosionRadius", snapshot.explosionRadius);
        SetInt(serialized, "projectileMaxHitTargets", snapshot.maxHitTargets);
        SetFloat(serialized, "projectileMaxLifetime", snapshot.maxLifetime);
        SetFloat(serialized, "targetSearchRange", snapshot.targetSearchRange);
        SetFloat(serialized, "projectileVisualScale", snapshot.visualScale);
        SetVector3(serialized, "projectileVisualRotationOffset", snapshot.visualRotationOffset);
        SetFloat(serialized, "projectileSpawnForwardOffset", snapshot.spawnForwardOffset);
        SetFloat(serialized, "projectileSpawnHeightOffset", snapshot.spawnHeightOffset);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplySnapshotToCsv(WeaponVisualTuningSnapshot snapshot)
    {
        if (!File.Exists(CsvSourcePath))
        {
            Debug.LogWarning("[WeaponTuning] CSV 없음: " + CsvSourcePath);
            return;
        }

        string[] lines = File.ReadAllLines(CsvSourcePath, Encoding.UTF8);
        int headerIndex = FindHeaderLineIndex(lines);
        if (headerIndex < 0)
        {
            Debug.LogError("[WeaponTuning] CSV 헤더 행을 찾을 수 없습니다.");
            return;
        }

        string[] headers = ParseCsvLine(lines[headerIndex]);
        Dictionary<string, int> columnIndex = BuildColumnIndex(headers);
        int weaponIdColumn = columnIndex["Weapon_ID"];

        bool updated = false;
        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            List<string> cells = new List<string>(ParseCsvLine(line));
            if (cells.Count <= weaponIdColumn)
            {
                continue;
            }

            if (!int.TryParse(cells[weaponIdColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rowWeaponId)
                || rowWeaponId != snapshot.weaponId)
            {
                continue;
            }

            WriteCommonTimingCells(cells, columnIndex, snapshot);
            WriteKindSpecificCells(cells, columnIndex, snapshot);
            lines[i] = string.Join(",", cells);
            updated = true;
            break;
        }

        if (!updated)
        {
            Debug.LogWarning($"[WeaponTuning] CSV 에 Weapon_ID={snapshot.weaponId} 행이 없습니다.");
            return;
        }

        File.WriteAllText(CsvSourcePath, string.Join("\n", lines) + "\n", Utf8WithBom);
    }

    static void WriteCommonTimingCells(List<string> cells, Dictionary<string, int> columnIndex, WeaponVisualTuningSnapshot snapshot)
    {
        SetCsvFloat(cells, columnIndex, "Atk_cooldown", snapshot.attackCooldown);
        SetCsvFloat(cells, columnIndex, "Atk_Active_Delay", snapshot.attackActiveDelay);
        SetCsvFloat(cells, columnIndex, "Atk_Anim_Duration", snapshot.attackAnimDuration);
        SetCsvFloat(cells, columnIndex, "Vfx1_Scale", snapshot.visualScale);
        SetCsvFloat(cells, columnIndex, "Vfx1_Speed", snapshot.moveSpeed);
    }

    static void WriteKindSpecificCells(List<string> cells, Dictionary<string, int> columnIndex, WeaponVisualTuningSnapshot snapshot)
    {
        switch (snapshot.kind)
        {
            case WeaponVisualKind.Ranged:
                SetCsvFloat(cells, columnIndex, "Vfx1_Range", snapshot.maxRange);
                SetCsvFloat(cells, columnIndex, "Vfx1_Hit_Radius", snapshot.hitRadius);
                break;

            case WeaponVisualKind.Magic:
                SetCsvFloat(cells, columnIndex, "Vfx1_Hit_Radius", snapshot.hitRadius);
                SetCsvFloat(cells, columnIndex, "Vfx1_Turn_Speed", snapshot.turnSpeed);
                SetCsvFloat(cells, columnIndex, "Vfx1_Explosion_Radius", snapshot.explosionRadius);
                SetCsvInt(cells, columnIndex, "Vfx1_Max_Targets", snapshot.maxHitTargets);
                SetCsvFloat(cells, columnIndex, "Vfx1_Lifetime", snapshot.maxLifetime);
                SetCsvFloat(cells, columnIndex, "Vfx1_Search_Range", snapshot.targetSearchRange);
                break;

            default:
                SetCsvFloat(cells, columnIndex, "Vfx1_Range", MeleeHitLengthAtUnitScale);
                break;
        }
    }

    static int FindHeaderLineIndex(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("Weapon_ID,"))
            {
                return i;
            }
        }

        return -1;
    }

    static Dictionary<string, int> BuildColumnIndex(string[] headers)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
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

    static void SetCsvFloat(List<string> cells, Dictionary<string, int> columnIndex, string columnName, float value)
    {
        if (!columnIndex.TryGetValue(columnName, out int index))
        {
            return;
        }

        EnsureCellCount(cells, index + 1);
        cells[index] = value.ToString(CultureInfo.InvariantCulture);
    }

    static void SetCsvInt(List<string> cells, Dictionary<string, int> columnIndex, string columnName, int value)
    {
        if (!columnIndex.TryGetValue(columnName, out int index))
        {
            return;
        }

        EnsureCellCount(cells, index + 1);
        cells[index] = value.ToString(CultureInfo.InvariantCulture);
    }

    static void EnsureCellCount(List<string> cells, int size)
    {
        while (cells.Count < size)
        {
            cells.Add(string.Empty);
        }
    }

    static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    static void SetVector3(SerializedObject serialized, string propertyName, Vector3 value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.vector3Value = value;
        }
    }
}
#endif
