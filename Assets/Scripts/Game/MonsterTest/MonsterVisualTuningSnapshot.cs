using System;
using UnityEngine;

[Serializable]
public class MonsterVisualTuningSnapshot
{
    public int monId = 2001;
    public string monName = "Monster";
    public MonsterKind kind = MonsterKind.Melee;

    [Header("CSV — 기본 스탯")]
    public int level = 1;
    public int hp = 10;
    public int mp;
    public int mpRegen;
    public int damage = 1;
    public float moveSpeed = 3.5f;
    public int giveExp = 10;
    public string prefabName = "SPUM_orc_m1";
    public string projectilePrefab = string.Empty;
    public string hitImpact = "Impact_Hit_Lv1";

    [Header("Attack Timing")]
    public float attackRange = 1.1f;
    public float attackCooldown = 1.1f;
    public float attackAnimDuration = 0.45f;
    public float damageApplyNormalizedTime = 0.4f;
    public float fireDelayNormalizedTime = 0.35f;

    [Header("Movement")]
    public float stopDistance = 0.9f;

    [Header("Projectile Spawn")]
    public float projectileSpawnForwardOffset = 0.55f;
    public float projectileSpawnHeightOffset = 0.65f;
    public float targetAimHeightOffset = 0.45f;

    [Header("Arrow Projectile")]
    public float arrowProjectileSpeed = 22f;
    public float arrowProjectileMaxRange = 16f;
    public float arrowProjectileHitRadius = 0.6f;
    public float arrowVisualScale = 0.35f;
    public Vector3 arrowVisualRotationOffset = new Vector3(90f, -90f, -45f);
    public float arrowVisualScaleMultiplier = 0.48f;
    public float arrowArcHeightMin = 0.3f;
    public float arrowArcHeightMax = 0.9f;
    public float arrowArcHeightDistanceMultiplier = 0.07f;

    [Header("Energy Projectile")]
    public float energyProjectileSpeed = 14f;
    public float energyProjectileHitRadius = 0.42f;
    public float energyProjectileMaxLifetime = 2.5f;
    public float energyVisualScaleMultiplier = 0.8f;

    public static MonsterVisualTuningSnapshot CreateDefault(MonsterKind kind)
    {
        switch (kind)
        {
            case MonsterKind.Ranged:
                return new MonsterVisualTuningSnapshot
                {
                    monId = 2007,
                    monName = "Orc Ranged",
                    kind = MonsterKind.Ranged,
                    prefabName = "SPUM_orc_m7",
                    projectilePrefab = "Arrow01",
                    attackRange = 13f,
                    attackCooldown = 1.35f,
                    moveSpeed = 3.5f
                };

            case MonsterKind.Mage:
                return new MonsterVisualTuningSnapshot
                {
                    monId = 2009,
                    monName = "Orc Mage",
                    kind = MonsterKind.Mage,
                    prefabName = "SPUM_orc_m9",
                    projectilePrefab = "Projectile_Energy_Ball_B",
                    hitImpact = "Impact_Energy_Ball",
                    mp = 10,
                    mpRegen = 2,
                    attackRange = 13f,
                    attackCooldown = 1.35f,
                    moveSpeed = 3.5f
                };

            default:
                return new MonsterVisualTuningSnapshot
                {
                    monId = 2001,
                    monName = "Orc Melee",
                    kind = MonsterKind.Melee,
                    prefabName = "SPUM_orc_m1",
                    attackRange = 1.1f,
                    attackCooldown = 1.1f,
                    moveSpeed = 3.5f
                };
        }
    }

    public MonsterVisualTuningSnapshot Clone()
    {
        return JsonUtility.FromJson<MonsterVisualTuningSnapshot>(JsonUtility.ToJson(this));
    }
}
