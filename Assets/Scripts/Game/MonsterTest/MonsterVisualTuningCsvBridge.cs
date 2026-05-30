using UnityEngine;

public static class MonsterVisualTuningCsvBridge
{
    public static MonsterVisualTuningSnapshot ToSnapshot(MonsterDefinitionRow row)
    {
        MonsterVisualTuningSnapshot defaults = MonsterVisualTuningSnapshot.CreateDefault(row.kind);
        float attackRange = Resolve(row.attackRange, defaults.attackRange);
        float attackCooldown = Resolve(row.attackCooldown, defaults.attackCooldown);
        float attackAnimDuration = Resolve(row.attackAnimDuration, defaults.attackAnimDuration);
        float fireDelayNormalizedTime = Resolve(row.fireDelayNormalizedTime, defaults.fireDelayNormalizedTime);
        float stopDistance = Resolve(row.stopDistance, defaults.stopDistance);
        float damageApplyNormalizedTime = row.kind == MonsterKind.Melee
            ? Resolve(row.fireDelayNormalizedTime, defaults.damageApplyNormalizedTime)
            : defaults.damageApplyNormalizedTime;

        return new MonsterVisualTuningSnapshot
        {
            monId = row.monId,
            monName = row.monName,
            kind = row.kind,
            level = row.level,
            hp = row.hp,
            mp = row.mp,
            mpRegen = row.mpRegen,
            damage = row.damage,
            moveSpeed = row.moveSpeed,
            giveExp = row.giveExp,
            prefabName = row.prefabName,
            projectilePrefab = row.projectilePrefab,
            hitImpact = row.hitImpact,
            attackRange = attackRange,
            attackCooldown = attackCooldown,
            attackAnimDuration = attackAnimDuration,
            damageApplyNormalizedTime = damageApplyNormalizedTime,
            fireDelayNormalizedTime = fireDelayNormalizedTime,
            stopDistance = stopDistance,
            projectileSpawnForwardOffset = defaults.projectileSpawnForwardOffset,
            projectileSpawnHeightOffset = defaults.projectileSpawnHeightOffset,
            targetAimHeightOffset = defaults.targetAimHeightOffset,
            arrowProjectileSpeed = defaults.arrowProjectileSpeed,
            arrowProjectileMaxRange = defaults.arrowProjectileMaxRange,
            arrowProjectileHitRadius = defaults.arrowProjectileHitRadius,
            arrowVisualScale = defaults.arrowVisualScale,
            arrowVisualRotationOffset = defaults.arrowVisualRotationOffset,
            arrowVisualScaleMultiplier = defaults.arrowVisualScaleMultiplier,
            arrowArcHeightMin = defaults.arrowArcHeightMin,
            arrowArcHeightMax = defaults.arrowArcHeightMax,
            arrowArcHeightDistanceMultiplier = defaults.arrowArcHeightDistanceMultiplier,
            energyProjectileSpeed = defaults.energyProjectileSpeed,
            energyProjectileHitRadius = defaults.energyProjectileHitRadius,
            energyProjectileMaxLifetime = defaults.energyProjectileMaxLifetime,
            energyVisualScaleMultiplier = defaults.energyVisualScaleMultiplier
        };
    }

    public static MonsterDefinitionRow ToRow(MonsterVisualTuningSnapshot snapshot)
    {
        float fireDelayNormalizedTime = snapshot.kind == MonsterKind.Melee
            ? snapshot.damageApplyNormalizedTime
            : snapshot.fireDelayNormalizedTime;

        return new MonsterDefinitionRow
        {
            monId = snapshot.monId,
            monName = snapshot.monName,
            kind = snapshot.kind,
            level = snapshot.level,
            hp = snapshot.hp,
            mp = snapshot.mp,
            mpRegen = snapshot.mpRegen,
            damage = snapshot.damage,
            moveSpeed = snapshot.moveSpeed,
            giveExp = snapshot.giveExp,
            attackRange = snapshot.attackRange,
            attackCooldown = snapshot.attackCooldown,
            attackAnimDuration = snapshot.attackAnimDuration,
            fireDelayNormalizedTime = fireDelayNormalizedTime,
            stopDistance = snapshot.stopDistance,
            prefabName = snapshot.prefabName,
            projectilePrefab = snapshot.projectilePrefab,
            hitImpact = snapshot.hitImpact
        };
    }

    static float Resolve(float rowValue, float defaultValue)
    {
        return float.IsNaN(rowValue) ? defaultValue : rowValue;
    }
}
