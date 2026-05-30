using System.Collections.Generic;
using UnityEngine;

// 앞으로 나가는 검기입니다. 지나가는 몬스터를 타격합니다.
public class SwordWave : MonoBehaviour
{
    public struct Settings
    {
        public float waveSpeed;
        public float waveMaxDistance;
        public float waveWidth;
        public float spawnForwardOffset;
        public float spawnSideOffset;
        public float spawnHeightOffset;
        public float slashVfxScale;
        public Vector3 slashVfxRotationOffset;
        public float maxLifetime;
        public int waveDamage;
        public float maxVerticalHitDelta;
    }

    const float DefaultMaxVerticalHitDelta = 1.2f;
    const float SlashVfxDuration = 1f;

    [SerializeField] float waveSpeed = 14f;
    [SerializeField] float waveMaxDistance = 5.5f;
    [SerializeField] float waveWidth = 2.4f;
    [SerializeField] float spawnForwardOffset = 0.35f;
    [SerializeField] float groundHeight = 0f;
    [SerializeField] float spawnHeightOffset = 0.45f;
    [SerializeField] float maxLifetime = 1.2f;

    [Header("이펙트 (White Slash v1)")]
    [SerializeField] GameObject slashVfxPrefab;
    [SerializeField] Vector3 slashVfxRotationOffset = new Vector3(-90f, 0f, -135f);
    [SerializeField] float slashVfxScale = 1f;

    Vector3 moveDirection;
    Vector3 hitOrigin;
    Vector3 startPosition;
    Transform attacker;
    float spawnSideOffsetMagnitude;
    float aliveTime;
    float hitSweepDuration;
    int waveDamage = 3;
    float maxVerticalHitDelta = DefaultMaxVerticalHitDelta;
    readonly HashSet<int> hitMonsterIds = new HashSet<int>();

    GameObject vfxInstance;

    public static SwordWave Spawn(
        Vector3 origin,
        Vector3 direction,
        Transform attackerTransform,
        GameObject vfxPrefab,
        float height,
        Settings settings)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        GameObject waveObject = new GameObject("SwordWave");
        SwordWave wave = waveObject.AddComponent<SwordWave>();
        wave.Configure(origin, direction, attackerTransform, vfxPrefab, height, settings);
        return wave;
    }

    void Configure(Vector3 origin, Vector3 direction, Transform attackerTransform, GameObject vfxPrefab, float height, Settings settings)
    {
        waveSpeed = settings.waveSpeed;
        waveMaxDistance = settings.waveMaxDistance;
        waveWidth = settings.waveWidth;
        spawnForwardOffset = settings.spawnForwardOffset;
        float sideOffset = settings.spawnSideOffset;
        spawnSideOffsetMagnitude = Mathf.Abs(sideOffset);
        spawnHeightOffset = settings.spawnHeightOffset;
        // 옆 spawn offset 때문에 정면 몬스터가 폭 판정 밖으로 빠지지 않게 보정합니다.
        waveWidth = settings.waveWidth + spawnSideOffsetMagnitude * 2f;
        slashVfxScale = settings.slashVfxScale;
        slashVfxRotationOffset = settings.slashVfxRotationOffset;
        maxLifetime = settings.maxLifetime;
        waveDamage = settings.waveDamage > 0 ? settings.waveDamage : 3;
        maxVerticalHitDelta = settings.maxVerticalHitDelta > 0f
            ? settings.maxVerticalHitDelta
            : DefaultMaxVerticalHitDelta;

        groundHeight = height;
        moveDirection = direction;
        attacker = attackerTransform;
        slashVfxPrefab = vfxPrefab;

        Vector3 sideDirection = Vector3.Cross(Vector3.up, moveDirection).normalized;
        hitOrigin = origin
            + moveDirection * spawnForwardOffset
            + sideDirection * sideOffset;
        hitOrigin.y = groundHeight;

        startPosition = hitOrigin;
        startPosition.y = groundHeight + spawnHeightOffset;
        transform.position = startPosition;

        hitSweepDuration = Mathf.Max(0.08f, waveMaxDistance / Mathf.Max(waveSpeed, 0.01f));
        maxLifetime = SlashVfxDuration;

        SpawnSlashVfx();
    }

    public void CancelAndDestroy()
    {
        Destroy(gameObject);
    }

    void SpawnSlashVfx()
    {
        if (slashVfxPrefab == null)
        {
            return;
        }

        Quaternion effectRotation = BuildSlashEffectRotation(moveDirection, slashVfxRotationOffset);
        vfxInstance = Instantiate(slashVfxPrefab, transform.position, effectRotation, transform);
        vfxInstance.transform.localScale = Vector3.one * slashVfxScale;
    }

    static Quaternion BuildSlashEffectRotation(Vector3 flatDirection, Vector3 rotationOffset)
    {
        float yaw = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(rotationOffset.x, yaw + rotationOffset.y, rotationOffset.z);
    }

    void Update()
    {
        aliveTime += Time.deltaTime;
        TryHitMonsters();

        if (aliveTime >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    float GetCurrentHitReach()
    {
        float progress = Mathf.Clamp01(aliveTime / hitSweepDuration);
        progress = 1f - (1f - progress) * (1f - progress);
        return waveMaxDistance * progress;
    }

    void TryHitMonsters()
    {
        float halfWidth = waveWidth * 0.5f;
        float hitReach = GetCurrentHitReach();

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
            float monsterSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(
                monsterWorld,
                monsterWorld.y);
            if (Mathf.Abs(monsterSurfaceY - groundHeight) > maxVerticalHitDelta)
            {
                continue;
            }

            Vector3 monsterPosition = monsterWorld;
            monsterPosition.y = 0f;
            Vector3 hitOriginFlat = hitOrigin;
            hitOriginFlat.y = 0f;

            Vector3 toMonster = monsterPosition - hitOriginFlat;
            float along = Vector3.Dot(toMonster, moveDirection);
            float flatDistance = toMonster.magnitude;
            bool pointBlank = flatDistance <= halfWidth + spawnSideOffsetMagnitude + 0.35f;
            if (!pointBlank && (along < -halfWidth || along > hitReach + halfWidth))
            {
                continue;
            }

            Vector3 closestPoint = hitOriginFlat + moveDirection * along;
            Vector3 sideVector = monsterPosition - closestPoint;
            sideVector.y = 0f;
            if (sideVector.sqrMagnitude > halfWidth * halfWidth)
            {
                continue;
            }

            bool sameFloor = Mathf.Abs(monsterSurfaceY - groundHeight) <= maxVerticalHitDelta;
            bool inSweepRange = along >= -halfWidth && along <= hitReach + halfWidth;
            bool skipTerrainBlock = sameFloor && inSweepRange;

            if (!skipTerrainBlock
                && GroundHeightSampler.IsWalkableTerrainBlockingLine(
                    hitOrigin,
                    groundHeight,
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

            Vector3 knockbackDirection = monsterPosition - hitOriginFlat;
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
                Vector3 impactPosition = monsterWorld;
                impactPosition.y = monsterSurfaceY + 0.35f;
                ImpactVfx.SpawnHitImpact(impactPosition);
                hitReaction.ApplyHit(knockbackDirection, attacker, waveDamage);
            }
            else
            {
                Vector3 impactPosition = monsterWorld;
                impactPosition.y = monsterSurfaceY + 0.35f;
                ImpactVfx.SpawnHitImpact(impactPosition);
                health.TakeDamage(waveDamage);
            }

            hitMonsterIds.Add(monsterId);
        }
    }

    void OnDestroy()
    {
        if (vfxInstance != null)
        {
            Destroy(vfxInstance);
            vfxInstance = null;
        }
    }
}
