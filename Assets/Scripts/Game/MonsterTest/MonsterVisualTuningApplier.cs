using UnityEngine;

public static class MonsterVisualTuningApplier
{
    public static bool TryReadFromMonster(GameObject monster, out MonsterVisualTuningSnapshot snapshot)
    {
        snapshot = null;
        if (monster == null)
        {
            return false;
        }

        MonsterStats stats = monster.GetComponent<MonsterStats>();
        int monId = stats != null ? stats.MonId : 0;
        MonsterKind kind = stats != null ? stats.Kind : MonsterKind.Melee;
        string monName = string.Empty;

        MonsterAttack melee = monster.GetComponent<MonsterAttack>();
        MonsterRangedAttack ranged = monster.GetComponent<MonsterRangedAttack>();
        MonsterMovement movement = monster.GetComponent<MonsterMovement>();

        if (kind == MonsterKind.Melee && melee != null && melee.enabled)
        {
            snapshot = melee.ExportVisualTuning(kind, monId, monName);
        }
        else if ((kind == MonsterKind.Ranged || kind == MonsterKind.Mage) && ranged != null && ranged.enabled)
        {
            snapshot = ranged.ExportVisualTuning(kind, monId, monName);
        }
        else if (melee != null && melee.enabled)
        {
            snapshot = melee.ExportVisualTuning(MonsterKind.Melee, monId, monName);
        }
        else if (ranged != null && ranged.enabled)
        {
            snapshot = ranged.ExportVisualTuning(
                kind == MonsterKind.Mage ? MonsterKind.Mage : MonsterKind.Ranged,
                monId,
                monName);
        }
        else
        {
            snapshot = MonsterVisualTuningSnapshot.CreateDefault(kind);
            snapshot.monId = monId;
        }

        if (movement != null)
        {
            snapshot.moveSpeed = movement.GetConfiguredMoveSpeed();
            snapshot.stopDistance = movement.GetConfiguredStopDistance();
        }

        MonsterHealth health = monster.GetComponent<MonsterHealth>();
        if (health != null)
        {
            snapshot.hp = health.MaxHp;
        }

        if (stats != null)
        {
            snapshot.level = stats.Level;
            snapshot.mp = stats.MaxMp;
            snapshot.mpRegen = stats.MpRegen;
        }

        return snapshot != null;
    }

    public static bool TryApplyToMonster(GameObject monster, MonsterVisualTuningSnapshot snapshot)
    {
        if (monster == null || snapshot == null)
        {
            return false;
        }

        MonsterDefinitionRow row = MonsterVisualTuningCsvBridge.ToRow(snapshot);
        MonsterStats.Apply(monster, row);
        ApplyTuningComponents(monster, snapshot);
        return true;
    }

    public static void TryApplySavedTuning(GameObject monster, int monId)
    {
        if (monster == null || monId == 0)
        {
            return;
        }

        if (!MonsterVisualTuningPersistence.TryLoad(monId, out MonsterVisualTuningSnapshot tuning))
        {
            return;
        }

        ApplyTuningComponents(monster, tuning);
    }

    static void ApplyTuningComponents(GameObject monster, MonsterVisualTuningSnapshot snapshot)
    {
        if (monster == null || snapshot == null)
        {
            return;
        }

        MonsterMovement movement = monster.GetComponent<MonsterMovement>();
        if (movement != null)
        {
            movement.Configure(snapshot.moveSpeed);
            movement.ConfigureStopDistance(snapshot.stopDistance);
        }

        MonsterAttack melee = monster.GetComponent<MonsterAttack>();
        MonsterRangedAttack ranged = monster.GetComponent<MonsterRangedAttack>();
        bool useRanged = snapshot.kind == MonsterKind.Ranged || snapshot.kind == MonsterKind.Mage;

        if (melee != null)
        {
            melee.enabled = !useRanged;
            if (!useRanged)
            {
                melee.ApplyVisualTuning(snapshot);
            }
        }

        if (ranged != null)
        {
            ranged.enabled = useRanged;
            if (useRanged)
            {
                ranged.ApplyVisualTuning(snapshot);
            }
        }
    }

    public static bool TryRequestTestAttack(GameObject monster, MonsterKind kind)
    {
        if (monster == null)
        {
            return false;
        }

        if (kind == MonsterKind.Melee)
        {
            MonsterAttack melee = monster.GetComponent<MonsterAttack>();
            return melee != null && melee.enabled && melee.RequestTestAttack();
        }

        MonsterRangedAttack ranged = monster.GetComponent<MonsterRangedAttack>();
        return ranged != null && ranged.enabled && ranged.RequestTestAttack();
    }
}
