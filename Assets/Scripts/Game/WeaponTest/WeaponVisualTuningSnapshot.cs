using System;
using UnityEngine;

[Serializable]
public class WeaponVisualTuningSnapshot
{
    public int weaponId = 3001;
    public string weaponName = "Weapon";
    public WeaponVisualKind kind = WeaponVisualKind.Melee;

    [Header("CSV — 기본 정보")]
    public int weaponLevel = 1;
    public int damage = 3;
    public string equPosition = "R_Weapon";
    public string weaponImage = string.Empty;
    public string vfxPrefab1 = string.Empty;
    public string vfxPrefab2 = string.Empty;
    public string weaponHitImpact = "Impact_Hit_Lv1";
    public float vfx1Lifetime = 1f;

    [Header("Attack Timing")]
    public float attackCooldown = 0.45f;
    public float attackActiveDelay = 0.08f;
    public float attackAnimDuration = 0.3f;

    [Header("Spawn Point")]
    public float spawnForwardOffset = 0.22f;
    public float spawnSideOffset = 0.28f;
    public float spawnHeightOffset = 0.45f;

    [Header("Visual")]
    public float visualScale = 0.3f;
    public Vector3 visualRotationOffset = new Vector3(-90f, 0f, -135f);

    [Header("Projectile / Hitbox")]
    public float moveSpeed = 14f;
    public float maxRange = 16f;
    public float hitRadius = 0.6f;
    public float turnSpeed = 540f;
    public float explosionRadius = 1.55f;
    public int maxHitTargets = 3;
    public float maxLifetime = 5f;
    public float targetSearchRange = 24f;

    public static WeaponVisualTuningSnapshot CreateDefault(WeaponVisualKind kind)
    {
        switch (kind)
        {
            case WeaponVisualKind.Ranged:
                return new WeaponVisualTuningSnapshot
                {
                    weaponId = 3002,
                    weaponName = "활",
                    kind = WeaponVisualKind.Ranged,
                    weaponLevel = 1,
                    damage = 4,
                    equPosition = "R_Weapon;L_Weapon",
                    weaponImage = "New_Weapon_10",
                    vfxPrefab1 = "Arrow01",
                    weaponHitImpact = "Impact_Hit_Lv1",
                    vfx1Lifetime = 0.83f,
                    attackCooldown = 0.55f,
                    attackActiveDelay = 0.12f,
                    attackAnimDuration = 0.28f,
                    spawnForwardOffset = 0.55f,
                    spawnHeightOffset = 0.65f,
                    visualScale = 0.168f,
                    visualRotationOffset = new Vector3(90f, -90f, -45f),
                    moveSpeed = 22f,
                    maxRange = 16f,
                    hitRadius = 0.6f
                };

            case WeaponVisualKind.Magic:
                return new WeaponVisualTuningSnapshot
                {
                    weaponId = 3003,
                    weaponName = "화염스태프",
                    kind = WeaponVisualKind.Magic,
                    weaponLevel = 1,
                    damage = 6,
                    equPosition = "R_Weapon",
                    weaponImage = "Weapon_Sorcerer",
                    vfxPrefab1 = "Projectile_Fire_Ball_Lv2",
                    vfxPrefab2 = "Projectile_Fire_Ball_Lv4",
                    weaponHitImpact = "Impact_Hit_Lv1",
                    vfx1Lifetime = 5f,
                    attackCooldown = 0.95f,
                    attackActiveDelay = 0.08f,
                    attackAnimDuration = 0.28f,
                    spawnForwardOffset = 0.58f,
                    spawnHeightOffset = 0.72f,
                    visualScale = 1f,
                    visualRotationOffset = Vector3.zero,
                    moveSpeed = 11.5f,
                    hitRadius = 0.5f,
                    turnSpeed = 540f,
                    explosionRadius = 1.55f,
                    maxHitTargets = 3,
                    maxLifetime = 5f,
                    targetSearchRange = 24f
                };

            default:
                return new WeaponVisualTuningSnapshot
                {
                    weaponId = 3001,
                    weaponName = "검",
                    kind = WeaponVisualKind.Melee,
                    weaponLevel = 1,
                    damage = 3,
                    equPosition = "R_Weapon",
                    weaponImage = "New_Weapon_06",
                    vfxPrefab1 = "White Slash v1",
                    weaponHitImpact = "Impact_Hit_Lv1",
                    vfx1Lifetime = 1f,
                    attackCooldown = 0.45f,
                    attackActiveDelay = 0.08f,
                    attackAnimDuration = 0.3f,
                    spawnForwardOffset = 0.22f,
                    spawnSideOffset = 0.28f,
                    spawnHeightOffset = 0.45f,
                    visualScale = 0.3f,
                    visualRotationOffset = new Vector3(-90f, 0f, -135f),
                    moveSpeed = 14f
                };
        }
    }

    public WeaponVisualTuningSnapshot Clone()
    {
        return JsonUtility.FromJson<WeaponVisualTuningSnapshot>(JsonUtility.ToJson(this));
    }
}
