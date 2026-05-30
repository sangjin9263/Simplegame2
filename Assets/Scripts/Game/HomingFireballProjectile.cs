using UnityEngine;
using System.Collections.Generic;

// 스태프 화염구 유도 투사체입니다.
public class HomingFireballProjectile : MonoBehaviour
{
    public struct Settings
    {
        public float speed;
        public float turnSpeedDegreesPerSecond;
        public float hitRadius;
        public float explosionRadius;
        public int maxHitTargets;
        public int damage;
        public float maxLifetime;
        public float visualScale;
        public Vector3 visualRotationOffset;
    }

    [SerializeField] float speed = 11.5f;
    [SerializeField] float turnSpeedDegreesPerSecond = 540f;
    [SerializeField] float hitRadius = 0.5f;
    [SerializeField] float explosionRadius = 1.55f;
    [SerializeField] int maxHitTargets = 3;
    [SerializeField] int damage = 6;
    [SerializeField] float maxLifetime = 5f;

    Transform target;
    Transform attacker;
    MonsterHealth targetHealth;
    Vector3 moveDirection;
    float targetSurfaceY;
    float aliveTime;
    GameObject visualInstance;
    readonly HashSet<int> hitMonsterIds = new HashSet<int>();

    public static HomingFireballProjectile Spawn(
        Vector3 origin,
        Transform targetTransform,
        Transform attackerTransform,
        GameObject projectilePrefab,
        float targetSurface,
        Settings settings)
    {
        if (targetTransform == null)
        {
            return null;
        }

        GameObject projectileObject = new GameObject("HomingFireballProjectile");
        HomingFireballProjectile projectile = projectileObject.AddComponent<HomingFireballProjectile>();
        projectile.Configure(origin, targetTransform, attackerTransform, projectilePrefab, targetSurface, settings);
        return projectile;
    }

    void Configure(
        Vector3 origin,
        Transform targetTransform,
        Transform attackerTransform,
        GameObject projectilePrefab,
        float targetSurface,
        Settings settings)
    {
        speed = settings.speed > 0f ? settings.speed : speed;
        turnSpeedDegreesPerSecond = settings.turnSpeedDegreesPerSecond > 0f
            ? settings.turnSpeedDegreesPerSecond
            : turnSpeedDegreesPerSecond;
        hitRadius = settings.hitRadius > 0f ? settings.hitRadius : hitRadius;
        explosionRadius = settings.explosionRadius > 0f ? settings.explosionRadius : explosionRadius;
        maxHitTargets = settings.maxHitTargets > 0 ? settings.maxHitTargets : maxHitTargets;
        damage = settings.damage > 0 ? settings.damage : damage;
        maxLifetime = settings.maxLifetime > 0f ? settings.maxLifetime : maxLifetime;

        target = targetTransform;
        attacker = attackerTransform;
        targetHealth = targetTransform.GetComponent<MonsterHealth>();
        if (targetHealth == null)
        {
            targetHealth = targetTransform.GetComponentInChildren<MonsterHealth>();
        }
        targetSurfaceY = targetSurface;
        transform.position = origin;

        Vector3 initialTarget = GetTargetPoint();
        Vector3 initialDirection = initialTarget - origin;
        moveDirection = initialDirection.sqrMagnitude > 0.0001f ? initialDirection.normalized : Vector3.forward;

        if (projectilePrefab != null)
        {
            visualInstance = Instantiate(projectilePrefab, transform);
            visualInstance.name = projectilePrefab.name;
            visualInstance.transform.localPosition = Vector3.zero;

            float scale = settings.visualScale > 0f ? settings.visualScale : 1f;
            visualInstance.transform.localScale = Vector3.one * scale;
            visualInstance.transform.localRotation = Quaternion.Euler(settings.visualRotationOffset);
        }
    }

    void Update()
    {
        aliveTime += Time.deltaTime;
        if (aliveTime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        MonsterHealth health = targetHealth;
        if (health != null && health.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        targetSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(target.position, target.position.y);
        Vector3 targetPoint = GetTargetPoint();
        Vector3 desiredDirection = targetPoint - transform.position;
        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            float maxRadians = turnSpeedDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
            moveDirection = Vector3.RotateTowards(moveDirection, desiredDirection.normalized, maxRadians, 0f).normalized;
        }

        transform.position += moveDirection * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        float hitDistance = hitRadius + 0.35f;
        if ((targetPoint - transform.position).sqrMagnitude <= hitDistance * hitDistance)
        {
            ImpactVfx.SpawnFireImpact(targetPoint);
            ApplyExplosionDamage(targetPoint);
            Destroy(gameObject);
        }
    }

    Vector3 GetTargetPoint()
    {
        Vector3 targetWorld = target.position;
        return new Vector3(targetWorld.x, targetSurfaceY + 0.45f, targetWorld.z);
    }

    void ApplyExplosionDamage(Vector3 impactPoint)
    {
        hitMonsterIds.Clear();
        int appliedCount = 0;
        int targetCap = Mathf.Max(1, maxHitTargets);
        float radiusSqr = explosionRadius * explosionRadius;

        while (appliedCount < targetCap)
        {
            Transform bestMonster = null;
            MonsterHealth bestHealth = null;
            float bestDistanceSqr = float.MaxValue;

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

                if (!MonsterMovement.TryGetCombatCache(monster, out MonsterMovement.MonsterCombatCache combat))
                {
                    continue;
                }

                MonsterHealth health = combat.Health;

                if (health != null && health.IsDead)
                {
                    continue;
                }

                float surfaceY = GroundHeightSampler.GetCharacterSurfaceY(monster.position, monster.position.y);
                Vector3 monsterPoint = new Vector3(monster.position.x, surfaceY + 0.35f, monster.position.z);
                float distanceSqr = (monsterPoint - impactPoint).sqrMagnitude;
                if (distanceSqr > radiusSqr || distanceSqr >= bestDistanceSqr)
                {
                    continue;
                }

                bestMonster = monster;
                bestHealth = health;
                bestDistanceSqr = distanceSqr;
            }

            if (bestMonster == null)
            {
                break;
            }

            hitMonsterIds.Add(bestMonster.GetInstanceID());
            appliedCount++;

            Vector3 knockbackDirection = bestMonster.position - impactPoint;
            knockbackDirection.y = 0f;
            if (knockbackDirection.sqrMagnitude < 0.0001f)
            {
                knockbackDirection = moveDirection;
            }
            else
            {
                knockbackDirection.Normalize();
            }

            if (!MonsterMovement.TryGetCombatCache(bestMonster, out MonsterMovement.MonsterCombatCache bestCombat))
            {
                continue;
            }

            if (bestCombat.HitReaction != null)
            {
                bestCombat.HitReaction.ApplyHit(knockbackDirection, attacker, damage);
            }
            else if (bestHealth != null)
            {
                bestHealth.TakeDamage(damage);
            }
        }
    }
}
