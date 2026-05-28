using System.Collections;
using UnityEngine;

// 활 장착과 마우스 유지 화살 공격을 처리합니다.
public class PlayerRangedCombat : MonoBehaviour
{
    [SerializeField] SPUM_Prefabs spumPrefabs;
    [SerializeField] Sprite bowSprite;
    [SerializeField] GameObject arrowPrefab;

    [Header("공격")]
    [SerializeField] float attackCooldown = 0.55f;
    [SerializeField] float attackActiveDelay = 0.12f;
    [SerializeField] float attackAnimDuration = 0.28f;

    [Header("화살")]
    [SerializeField] float projectileSpeed = 22f;
    [SerializeField] float projectileMaxRange = 16f;
    [SerializeField] float projectileHitRadius = 0.6f;
    [SerializeField] int projectileDamage = 4;
    [SerializeField] float projectileSpawnForwardOffset = 0.55f;
    [SerializeField] float projectileSpawnHeightOffset = 0.65f;
    [SerializeField] float projectileMaxVerticalHitDelta = 1.2f;
    [SerializeField] float verticalAutoAimHeightOffset = 0.45f;
    [SerializeField] float verticalAutoAimForwardDot = 0.2f;

    [Header("화살 비주얼 (검기와 같은 방식)")]
    [Tooltip("검기 slashVfxRotationOffset 과 같이 (X, Y추가요, Z) 로 조정합니다.")]
    [SerializeField] Vector3 arrowVisualRotationOffset = new Vector3(90f, -90f, -45f);
    [SerializeField] float arrowVisualScale = 0.168f;

    public struct ArrowProjectileProfile
    {
        public Vector3 visualRotationOffset;
        public float visualScale;
        public float spawnHeightOffset;
        public float spawnForwardOffset;
        public float speed;
        public float maxRange;
        public float hitRadius;
        public float aimHeightOffset;
    }

    bool hasBow;
    bool isAttacking;
    float nextAttackTime;

    public bool IsAttacking => isAttacking;
    public bool HasBow => hasBow;

    PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        EnsureWeaponAssets();
    }

    void Update()
    {
        if (!hasBow)
        {
            return;
        }

        // SPUM 내부에서 무기 슬롯이 초기화되어도 활 장착 상태를 유지합니다.
        Sprite spriteToEquip = bowSprite;
        if (spriteToEquip != null)
        {
            SpumWeaponEquip.TryEquip(spumPrefabs, spriteToEquip, SpumWeaponVisualKind.Bow);
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (isAttacking)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    public bool TryEquipBow(Sprite bowWeaponSprite)
    {
        EnsureWeaponAssets();

        Sprite spriteToEquip = bowWeaponSprite != null ? bowWeaponSprite : bowSprite;
        if (spriteToEquip == null)
        {
            return false;
        }

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponent<SPUM_Prefabs>();
            if (spumPrefabs == null)
            {
                spumPrefabs = GetComponentInChildren<SPUM_Prefabs>(true);
            }
        }

        if (spumPrefabs == null)
        {
            return false;
        }

        if (!SpumWeaponEquip.TryEquip(spumPrefabs, spriteToEquip, SpumWeaponVisualKind.Bow))
        {
            return false;
        }

        bowSprite = spriteToEquip;
        hasBow = true;
        return true;
    }

    public void UnequipBow()
    {
        hasBow = false;
        if (spumPrefabs != null)
        {
            SpumWeaponEquip.Clear(spumPrefabs);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        Vector3 attackDirection = GetAttackDirection();
        if (playerMovement != null)
        {
            playerMovement.ApplyFacingForDirection(attackDirection);
        }

        if (HasAnimationClips(PlayerState.ATTACK))
        {
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, 0);
        }

        yield return new WaitForSeconds(attackActiveDelay);
        SpawnArrow(attackDirection);

        yield return new WaitForSeconds(attackAnimDuration);
        isAttacking = false;
        RestoreLocomotionAnimation();
    }

    void SpawnArrow(Vector3 attackDirection)
    {
        EnsureWeaponAssets();
        if (arrowPrefab == null)
        {
            return;
        }

        Vector3 origin = transform.position;
        float surfaceY = GroundHeightSampler.GetSurfaceY(origin, GameSession.GroundY);
        origin.y = surfaceY + projectileSpawnHeightOffset;
        origin += attackDirection * projectileSpawnForwardOffset;
        Vector3 shotDirection = BuildShotDirection(origin, attackDirection, surfaceY);

        float maxLifetime = projectileMaxRange / Mathf.Max(projectileSpeed, 0.01f) + 0.1f;
        RangedProjectile.Settings settings = new RangedProjectile.Settings
        {
            speed = projectileSpeed,
            maxRange = projectileMaxRange,
            hitRadius = projectileHitRadius,
            damage = projectileDamage,
            maxVerticalHitDelta = projectileMaxVerticalHitDelta,
            maxLifetime = maxLifetime,
            visualRotationOffset = arrowVisualRotationOffset,
            visualScale = arrowVisualScale
        };

        RangedProjectile.Spawn(origin, shotDirection, transform, arrowPrefab, surfaceY, settings);
    }

    Vector3 BuildShotDirection(Vector3 origin, Vector3 fallbackFlatDirection, float fireSurfaceY)
    {
        Vector3 baseDirection = fallbackFlatDirection;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude < 0.0001f)
        {
            baseDirection = transform.forward;
            baseDirection.y = 0f;
        }
        baseDirection.Normalize();

        float bestScore = float.MaxValue;
        Vector3 bestDirection = baseDirection;
        bool found = false;

        for (int i = 0; i < MonsterMovement.ActiveMonsterCount; i++)
        {
            Transform monster = MonsterMovement.GetActiveMonster(i);
            if (monster == null)
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

            Vector3 monsterWorld = monster.position;
            float monsterSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(monsterWorld, monsterWorld.y);
            Vector3 monsterAimPoint = new Vector3(monsterWorld.x, monsterSurfaceY + verticalAutoAimHeightOffset, monsterWorld.z);
            Vector3 toMonster = monsterAimPoint - origin;
            Vector3 toMonsterFlat = toMonster;
            toMonsterFlat.y = 0f;

            float flatDistance = toMonsterFlat.magnitude;
            if (flatDistance < 0.05f || flatDistance > projectileMaxRange)
            {
                continue;
            }

            Vector3 flatDirection = toMonsterFlat / flatDistance;
            if (Vector3.Dot(flatDirection, baseDirection) < verticalAutoAimForwardDot)
            {
                continue;
            }

            if (GroundHeightSampler.IsWalkableTerrainBlockingLine(
                    origin,
                    fireSurfaceY,
                    monsterWorld,
                    monsterSurfaceY))
            {
                continue;
            }

            float score = flatDistance;
            if (score < bestScore)
            {
                bestScore = score;
                bestDirection = toMonster.normalized;
                found = true;
            }
        }

        return found ? bestDirection : baseDirection;
    }

    void RestoreLocomotionAnimation()
    {
        if (playerMovement == null || playerMovement.LocomotionAnimation == null)
        {
            return;
        }

        playerMovement.LocomotionAnimation.ForceSetMoving(playerMovement.IsWantingToMove());
    }

    Vector3 GetAttackDirection()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            return playerMovement.GetAttackDirection();
        }

        return transform.forward;
    }

    public void EnsureWeaponAssets()
    {
        bowSprite = GameAssets.LoadBowSprite(bowSprite);
        arrowPrefab = GameAssets.LoadArrowPrefab(arrowPrefab);
    }

    public ArrowProjectileProfile GetArrowProjectileProfile()
    {
        return new ArrowProjectileProfile
        {
            visualRotationOffset = arrowVisualRotationOffset,
            visualScale = arrowVisualScale,
            spawnHeightOffset = projectileSpawnHeightOffset,
            spawnForwardOffset = projectileSpawnForwardOffset,
            speed = projectileSpeed,
            maxRange = projectileMaxRange,
            hitRadius = projectileHitRadius,
            aimHeightOffset = verticalAutoAimHeightOffset
        };
    }

    bool HasAnimationClips(PlayerState state)
    {
        if (spumPrefabs == null)
        {
            return false;
        }

        string key = state.ToString();
        if (!spumPrefabs.StateAnimationPairs.ContainsKey(key))
        {
            return false;
        }

        return spumPrefabs.StateAnimationPairs[key] != null && spumPrefabs.StateAnimationPairs[key].Count > 0;
    }

#if UNITY_EDITOR
    void Reset()
    {
        EnsureWeaponAssets();
    }
#endif
}
