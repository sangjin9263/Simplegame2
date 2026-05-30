using System.Collections.Generic;
using UnityEngine;

// 화살 발사체입니다. Arrow01 프리팹을 그대로 쓰고 직선으로 날아갑니다.
public class RangedProjectile : MonoBehaviour
{
    public struct Settings
    {
        public float speed;
        public float maxRange;
        public float hitRadius;
        public int damage;
        public float maxVerticalHitDelta;
        public float maxLifetime;
        public Vector3 visualRotationOffset;
        public float visualScale;
    }

    const float DefaultMaxVerticalHitDelta = 1.2f;
    const float DefaultVisualScale = 0.35f;

    [SerializeField] float speed = 22f;
    [SerializeField] float maxRange = 16f;
    [SerializeField] float hitRadius = 0.35f;
    [SerializeField] float maxLifetime = 2f;
    [SerializeField] float maxVerticalHitDelta = DefaultMaxVerticalHitDelta;
    [SerializeField] int damage = 4;
    Vector3 moveDirection;
    Vector3 fireOrigin;
    float fireSurfaceY;
    float traveledDistance;
    float aliveTime;
    Transform attacker;
    Vector3 visualRotationOffset;
    float visualScale = DefaultVisualScale;
    GameObject visualInstance;
    readonly HashSet<int> hitMonsterIds = new HashSet<int>();

    public static RangedProjectile Spawn(
        Vector3 origin,
        Vector3 direction,
        Transform attackerTransform,
        GameObject arrowPrefab,
        float surfaceY,
        Settings settings)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        GameObject projectileObject = new GameObject("ArrowProjectile");
        RangedProjectile projectile = projectileObject.AddComponent<RangedProjectile>();
        projectile.Configure(origin, direction, attackerTransform, arrowPrefab, surfaceY, settings);
        return projectile;
    }

    void Configure(
        Vector3 origin,
        Vector3 direction,
        Transform attackerTransform,
        GameObject arrowPrefab,
        float surfaceY,
        Settings settings)
    {
        speed = settings.speed;
        maxRange = settings.maxRange;
        hitRadius = settings.hitRadius;
        damage = settings.damage > 0 ? settings.damage : 4;
        maxVerticalHitDelta = settings.maxVerticalHitDelta > 0f
            ? settings.maxVerticalHitDelta
            : DefaultMaxVerticalHitDelta;
        maxLifetime = settings.maxLifetime > 0f ? settings.maxLifetime : 2f;
        visualRotationOffset = settings.visualRotationOffset;
        visualScale = settings.visualScale > 0f ? settings.visualScale : DefaultVisualScale;

        moveDirection = direction;
        attacker = attackerTransform;
        fireOrigin = origin;
        fireSurfaceY = surfaceY;

        transform.position = origin;
        transform.rotation = Quaternion.identity;

        if (arrowPrefab != null)
        {
            visualInstance = Instantiate(arrowPrefab, transform);
            visualInstance.name = arrowPrefab.name;
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one * visualScale;
        }

        ApplyVisualRotation();
    }

    void Update()
    {
        aliveTime += Time.deltaTime;
        float step = speed * Time.deltaTime;
        traveledDistance += step;
        transform.position += moveDirection * step;

        TryHitMonsters();

        if (traveledDistance >= maxRange || aliveTime >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void ApplyVisualRotation()
    {
        if (visualInstance == null || moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        visualInstance.transform.rotation = BuildVisualRotation(moveDirection, visualRotationOffset);
    }

    public static Quaternion BuildVisualRotation(Vector3 direction, Vector3 rotationOffset)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        Quaternion aimRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        return aimRotation * Quaternion.Euler(rotationOffset);
    }

    void TryHitMonsters()
    {
        float hitRadiusSqr = hitRadius * hitRadius;
        Vector3 projectilePosition = transform.position;

        for (int i = 0; i < MonsterMovement.ActiveMonsterCount; i++)
        {
            Transform monster = MonsterMovement.GetActiveMonster(i);
            if (monster == null)
            {
                continue;
            }

            int monsterId = monster.GetInstanceID();
            if (hitMonsterIds.Contains(monsterId))
            {
                continue;
            }

            Vector3 monsterWorld = monster.position;
            float monsterSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(monsterWorld, monsterWorld.y);
            Vector3 monsterHitPoint = new Vector3(monsterWorld.x, monsterSurfaceY + 0.45f, monsterWorld.z);
            Vector3 toMonster = monsterHitPoint - projectilePosition;
            if (toMonster.sqrMagnitude > hitRadiusSqr)
            {
                continue;
            }

            if (GroundHeightSampler.IsWalkableTerrainBlockingLine(
                    fireOrigin,
                    fireSurfaceY,
                    monsterWorld,
                    monsterSurfaceY))
            {
                continue;
            }

            if (!MonsterMovement.TryGetCombatCache(monster, out MonsterMovement.MonsterCombatCache combat))
            {
                continue;
            }

            MonsterHealth health = combat.Health;
            MonsterHitReaction hitReaction = combat.HitReaction;

            if (health != null && health.IsDead)
            {
                continue;
            }

            if (hitReaction == null && health == null)
            {
                continue;
            }

            Vector3 knockbackDirection = monsterWorld - fireOrigin;
            knockbackDirection.y = 0f;
            if (knockbackDirection.sqrMagnitude < 0.0001f)
            {
                knockbackDirection = moveDirection;
            }
            else
            {
                knockbackDirection.Normalize();
            }

            if (hitReaction != null)
            {
                ImpactVfx.SpawnHitImpact(monsterHitPoint);
                hitReaction.ApplyHit(knockbackDirection, attacker, damage);
            }
            else
            {
                ImpactVfx.SpawnHitImpact(monsterHitPoint);
                health.TakeDamage(damage);
            }

            hitMonsterIds.Add(monsterId);
            Destroy(gameObject);
            return;
        }
    }
}
