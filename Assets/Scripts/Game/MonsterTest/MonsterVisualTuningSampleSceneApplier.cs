#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Monster_test 저장값을 몬스터 프리팹 + monster_default.csv 에 반영합니다.
public static class MonsterVisualTuningSampleSceneApplier
{
    const string MonsterPrefabFolder = "Assets/Prefabs/Char/mon";

    static readonly List<MonsterVisualTuningSnapshot> PendingPrefabApplies = new List<MonsterVisualTuningSnapshot>();
    static bool playModeHookRegistered;

    public static void ApplyMonster(MonsterVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
        table.UpsertFromSnapshot(snapshot);
        table.SaveToEditorCsv();

        if (EditorApplication.isPlaying)
        {
            QueuePrefabApply(snapshot);
            Debug.Log(
                $"[MonsterTuning] JSON/CSV 저장 완료 (Mon_ID={snapshot.monId}). "
                + "Play 모드에서는 프리팹 저장이 불가해 Play 종료 후 자동 반영됩니다. "
                + "SampleScene Play 시에는 JSON 튜닝이 즉시 적용됩니다.");
            return;
        }

        ApplySnapshotToPrefab(snapshot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MonsterTuning] SampleScene 반영 완료 — {snapshot.prefabName} 프리팹 + CSV (Mon_ID={snapshot.monId}).");
    }

    static void QueuePrefabApply(MonsterVisualTuningSnapshot snapshot)
    {
        EnsurePlayModeHook();

        MonsterVisualTuningSnapshot copy = snapshot.Clone();
        for (int i = PendingPrefabApplies.Count - 1; i >= 0; i--)
        {
            if (PendingPrefabApplies[i].monId == copy.monId)
            {
                PendingPrefabApplies.RemoveAt(i);
            }
        }

        PendingPrefabApplies.Add(copy);
    }

    static void EnsurePlayModeHook()
    {
        if (playModeHookRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        playModeHookRegistered = true;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || PendingPrefabApplies.Count == 0)
        {
            return;
        }

        List<MonsterVisualTuningSnapshot> pending = new List<MonsterVisualTuningSnapshot>(PendingPrefabApplies);
        PendingPrefabApplies.Clear();

        for (int i = 0; i < pending.Count; i++)
        {
            ApplySnapshotToPrefab(pending[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MonsterTuning] Play 종료 — 대기 중이던 몬스터 프리팹 {pending.Count}개 반영 완료.");
    }

    static void ApplySnapshotToPrefab(MonsterVisualTuningSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.prefabName))
        {
            Debug.LogError("[MonsterTuning] prefabName 이 비어 있습니다.");
            return;
        }

        string prefabPath = MonsterPrefabFolder + "/" + snapshot.prefabName.Trim() + ".prefab";
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabContents == null)
        {
            Debug.LogError("[MonsterTuning] 몬스터 프리팹을 찾을 수 없습니다: " + prefabPath);
            return;
        }

        try
        {
            ApplyHealth(prefabContents, snapshot);
            ApplyMovement(prefabContents, snapshot);

            bool useRanged = snapshot.kind == MonsterKind.Ranged || snapshot.kind == MonsterKind.Mage;
            if (useRanged)
            {
                ApplyRangedAttack(prefabContents, snapshot);
            }
            else
            {
                ApplyMeleeAttack(prefabContents, snapshot);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    static void ApplyHealth(GameObject prefabRoot, MonsterVisualTuningSnapshot snapshot)
    {
        MonsterHealth health = prefabRoot.GetComponent<MonsterHealth>();
        if (health == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(health);
        SetInt(serialized, "maxHp", snapshot.hp);
        SetInt(serialized, "expOnDeathForTest", snapshot.giveExp);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyMovement(GameObject prefabRoot, MonsterVisualTuningSnapshot snapshot)
    {
        MonsterMovement movement = prefabRoot.GetComponent<MonsterMovement>();
        if (movement == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(movement);
        SetFloat(serialized, "moveSpeed", snapshot.moveSpeed);
        SetFloat(serialized, "stopDistance", snapshot.stopDistance);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyMeleeAttack(GameObject prefabRoot, MonsterVisualTuningSnapshot snapshot)
    {
        MonsterAttack attack = prefabRoot.GetComponent<MonsterAttack>();
        if (attack == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(attack);
        SetFloat(serialized, "attackRange", snapshot.attackRange);
        SetFloat(serialized, "attackCooldown", snapshot.attackCooldown);
        SetFloat(serialized, "attackAnimDuration", snapshot.attackAnimDuration);
        SetFloat(serialized, "damageApplyNormalizedTime", snapshot.damageApplyNormalizedTime);
        SetInt(serialized, "attackDamage", snapshot.damage);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyRangedAttack(GameObject prefabRoot, MonsterVisualTuningSnapshot snapshot)
    {
        MonsterRangedAttack ranged = prefabRoot.GetComponent<MonsterRangedAttack>();
        if (ranged == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(ranged);
        SetFloat(serialized, "attackRange", snapshot.attackRange);
        SetFloat(serialized, "attackCooldown", snapshot.attackCooldown);
        SetFloat(serialized, "attackAnimDuration", snapshot.attackAnimDuration);
        SetFloat(serialized, "fireDelayNormalizedTime", snapshot.fireDelayNormalizedTime);
        SetInt(serialized, "attackDamage", snapshot.damage);
        SetBool(serialized, "syncArrowFromPlayer", false);
        SetEnum(serialized, "projectileKind", snapshot.kind == MonsterKind.Mage ? 1 : 0);
        SetFloat(serialized, "projectileSpawnForwardOffset", snapshot.projectileSpawnForwardOffset);
        SetFloat(serialized, "projectileSpawnHeightOffset", snapshot.projectileSpawnHeightOffset);
        SetFloat(serialized, "targetAimHeightOffset", snapshot.targetAimHeightOffset);
        SetFloat(serialized, "arrowProjectileSpeed", snapshot.arrowProjectileSpeed);
        SetFloat(serialized, "arrowProjectileMaxRange", snapshot.arrowProjectileMaxRange);
        SetFloat(serialized, "arrowProjectileHitRadius", snapshot.arrowProjectileHitRadius);
        SetFloat(serialized, "arrowVisualScale", snapshot.arrowVisualScale);
        SetVector3(serialized, "arrowVisualRotationOffset", snapshot.arrowVisualRotationOffset);
        SetFloat(serialized, "arrowVisualScaleMultiplier", snapshot.arrowVisualScaleMultiplier);
        SetFloat(serialized, "arrowArcHeightMin", snapshot.arrowArcHeightMin);
        SetFloat(serialized, "arrowArcHeightMax", snapshot.arrowArcHeightMax);
        SetFloat(serialized, "arrowArcHeightDistanceMultiplier", snapshot.arrowArcHeightDistanceMultiplier);
        SetFloat(serialized, "energyProjectileSpeed", snapshot.energyProjectileSpeed);
        SetFloat(serialized, "energyProjectileHitRadius", snapshot.energyProjectileHitRadius);
        SetFloat(serialized, "energyProjectileMaxLifetime", snapshot.energyProjectileMaxLifetime);
        SetFloat(serialized, "energyVisualScaleMultiplier", snapshot.energyVisualScaleMultiplier);
        serialized.ApplyModifiedPropertiesWithoutUndo();
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

    static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
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

    static void SetEnum(SerializedObject serialized, string propertyName, int enumIndex)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = enumIndex;
        }
    }
}
#endif
