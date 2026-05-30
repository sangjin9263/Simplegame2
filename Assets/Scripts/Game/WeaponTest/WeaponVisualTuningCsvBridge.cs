using UnityEngine;

// CSV 행 ↔ 튜닝 스냅샷 변환.
public static class WeaponVisualTuningCsvBridge
{
    const float MeleeHitLengthAtUnitScale = 3.48f;

    public static WeaponVisualTuningSnapshot ToSnapshot(WeaponDefinitionRow row)
    {
        WeaponVisualKind kind = KindFromWeaponType(row.weaponType);
        var snapshot = new WeaponVisualTuningSnapshot
        {
            weaponId = row.weaponId,
            weaponName = row.weaponName,
            kind = kind,
            weaponLevel = row.weaponLevel,
            damage = row.damage,
            attackCooldown = row.atkCooldown,
            attackActiveDelay = row.atkActiveDelay,
            attackAnimDuration = row.atkAnimDuration,
            equPosition = row.equPosition,
            weaponImage = row.weaponImage,
            vfxPrefab1 = row.vfxPrefab1,
            vfxPrefab2 = row.vfxPrefab2,
            weaponHitImpact = row.weaponHitImpact,
            visualScale = row.vfx1Scale,
            moveSpeed = row.vfx1Speed,
            vfx1Lifetime = row.vfx1Lifetime,
            maxRange = kind == WeaponVisualKind.Melee ? row.vfx1Range : row.vfx1Range,
            hitRadius = row.vfx1HitRadius,
            turnSpeed = row.vfx1TurnSpeed,
            explosionRadius = row.vfx1ExplosionRadius,
            maxHitTargets = row.vfx1MaxTargets,
            maxLifetime = row.vfx1Lifetime,
            targetSearchRange = row.vfx1SearchRange
        };

        return snapshot;
    }

    public static WeaponDefinitionRow ToRow(WeaponVisualTuningSnapshot snapshot)
    {
        int weaponType = snapshot.kind switch
        {
            WeaponVisualKind.Ranged => 2,
            WeaponVisualKind.Magic => 3,
            _ => 1
        };

        float vfx1Range = snapshot.kind == WeaponVisualKind.Melee
            ? MeleeHitLengthAtUnitScale
            : snapshot.maxRange;

        return new WeaponDefinitionRow
        {
            weaponId = snapshot.weaponId,
            weaponName = snapshot.weaponName,
            weaponType = weaponType,
            weaponLevel = snapshot.weaponLevel,
            damage = snapshot.damage,
            atkCooldown = snapshot.attackCooldown,
            atkActiveDelay = snapshot.attackActiveDelay,
            atkAnimDuration = snapshot.attackAnimDuration,
            equPosition = snapshot.equPosition,
            weaponImage = snapshot.weaponImage,
            vfxPrefab1 = snapshot.vfxPrefab1,
            vfxPrefab2 = snapshot.vfxPrefab2,
            weaponHitImpact = snapshot.weaponHitImpact,
            vfx1Scale = snapshot.visualScale,
            vfx1Speed = snapshot.moveSpeed,
            vfx1Range = vfx1Range,
            vfx1HitRadius = snapshot.hitRadius,
            vfx1TurnSpeed = snapshot.turnSpeed,
            vfx1ExplosionRadius = snapshot.explosionRadius,
            vfx1MaxTargets = snapshot.maxHitTargets,
            vfx1Lifetime = snapshot.kind == WeaponVisualKind.Melee ? snapshot.vfx1Lifetime : snapshot.maxLifetime,
            vfx1SearchRange = snapshot.targetSearchRange
        };
    }

    public static WeaponVisualKind KindFromWeaponType(int weaponType)
    {
        switch (weaponType)
        {
            case 2:
                return WeaponVisualKind.Ranged;
            case 3:
                return WeaponVisualKind.Magic;
            default:
                return WeaponVisualKind.Melee;
        }
    }
}
