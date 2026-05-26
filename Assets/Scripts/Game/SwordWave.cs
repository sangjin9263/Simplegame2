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
    }

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
    Vector3 hitStartPosition;
    Vector3 startPosition;
    Transform attacker;
    float traveledDistance;
    float aliveTime;
    int waveDamage = 3;
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
        spawnHeightOffset = settings.spawnHeightOffset;
        slashVfxScale = settings.slashVfxScale;
        slashVfxRotationOffset = settings.slashVfxRotationOffset;
        maxLifetime = settings.maxLifetime;
        waveDamage = settings.waveDamage > 0 ? settings.waveDamage : 3;

        groundHeight = height;
        moveDirection = direction;
        attacker = attackerTransform;
        slashVfxPrefab = vfxPrefab;

        Vector3 sideDirection = Vector3.Cross(Vector3.up, moveDirection).normalized;
        hitOrigin = origin
            + moveDirection * spawnForwardOffset
            + sideDirection * sideOffset;
        hitOrigin.y = groundHeight;

        hitStartPosition = hitOrigin;
        startPosition = hitStartPosition;
        startPosition.y = groundHeight + spawnHeightOffset;
        transform.position = startPosition;

        SpawnSlashVfx();
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
        float step = waveSpeed * Time.deltaTime;
        traveledDistance += step;

        hitStartPosition = hitOrigin + moveDirection * traveledDistance;
        startPosition = hitStartPosition;
        startPosition.y = groundHeight + spawnHeightOffset;
        transform.position = startPosition;

        TryHitMonsters();

        if (traveledDistance >= waveMaxDistance || aliveTime >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void TryHitMonsters()
    {
        float halfWidth = waveWidth * 0.5f;

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

            Vector3 monsterPosition = monster.position;
            monsterPosition.y = 0f;

            Vector3 toMonster = monsterPosition - hitOrigin;
            float along = Vector3.Dot(toMonster, moveDirection);
            if (along < -halfWidth || along > traveledDistance + halfWidth)
            {
                continue;
            }

            Vector3 closestPoint = hitOrigin + moveDirection * along;
            Vector3 sideVector = monsterPosition - closestPoint;
            sideVector.y = 0f;
            if (sideVector.sqrMagnitude > halfWidth * halfWidth)
            {
                continue;
            }

            MonsterHealth health = monster.GetComponent<MonsterHealth>();
            if (health == null)
            {
                health = monster.GetComponentInChildren<MonsterHealth>();
            }

            if (health != null && health.IsDead)
            {
                continue;
            }

            MonsterHitReaction hitReaction = monster.GetComponent<MonsterHitReaction>();
            if (hitReaction == null)
            {
                hitReaction = monster.GetComponentInChildren<MonsterHitReaction>();
            }

            if (hitReaction == null && health == null)
            {
                continue;
            }

            Vector3 knockbackDirection = monsterPosition - hitOrigin;
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
                hitReaction.ApplyHit(knockbackDirection, attacker, waveDamage);
            }
            else
            {
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
        }
    }
}
