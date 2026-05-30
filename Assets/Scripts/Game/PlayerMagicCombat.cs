using System.Collections;
using UnityEngine;

// 스태프 장착과 화염구 유도 공격을 처리합니다.
public class PlayerMagicCombat : MonoBehaviour
{
    [SerializeField] SPUM_Prefabs spumPrefabs;
    [SerializeField] Sprite staffSprite;
    [SerializeField] GameObject fireballPrefab;

    [Header("공격")]
    [SerializeField] float attackCooldown = 0.95f;
    [SerializeField] float attackActiveDelay = 0.08f;
    [SerializeField] float attackAnimDuration = 0.28f;

    [Header("화염구")]
    [SerializeField] float projectileSpeed = 11.5f;
    [SerializeField] float projectileTurnSpeed = 540f;
    [SerializeField] float projectileHitRadius = 0.5f;
    [SerializeField] float projectileExplosionRadius = 1.55f;
    [SerializeField] int projectileMaxHitTargets = 3;
    [SerializeField] int projectileDamage = 6;
    [SerializeField] float projectileMaxLifetime = 5f;
    [SerializeField] float projectileSpawnForwardOffset = 0.58f;
    [SerializeField] float projectileSpawnHeightOffset = 0.72f;
    [SerializeField] float targetSearchRange = 24f;

    [Header("화염구 비주얼")]
    [SerializeField] Vector3 projectileVisualRotationOffset = Vector3.zero;
    [SerializeField] float projectileVisualScale = 1f;

    bool hasStaff;
    bool isAttacking;
    float nextAttackTime;
    int equipRefreshPhase;

    const int WeaponEquipRefreshIntervalFrames = 10;

    public bool IsAttacking => isAttacking;
    public bool HasStaff => hasStaff;

    public WeaponVisualTuningSnapshot ExportVisualTuning()
    {
        return new WeaponVisualTuningSnapshot
        {
            weaponId = 3003,
            weaponName = "Fire Staff",
            kind = WeaponVisualKind.Magic,
            attackCooldown = attackCooldown,
            attackActiveDelay = attackActiveDelay,
            attackAnimDuration = attackAnimDuration,
            spawnForwardOffset = projectileSpawnForwardOffset,
            spawnHeightOffset = projectileSpawnHeightOffset,
            visualScale = projectileVisualScale,
            visualRotationOffset = projectileVisualRotationOffset,
            moveSpeed = projectileSpeed,
            hitRadius = projectileHitRadius,
            turnSpeed = projectileTurnSpeed,
            explosionRadius = projectileExplosionRadius,
            maxHitTargets = projectileMaxHitTargets,
            maxLifetime = projectileMaxLifetime,
            targetSearchRange = targetSearchRange,
            damage = projectileDamage
        };
    }

    public void ApplyVisualTuning(WeaponVisualTuningSnapshot snapshot)
    {
        if (snapshot == null || snapshot.kind != WeaponVisualKind.Magic)
        {
            return;
        }

        attackCooldown = snapshot.attackCooldown;
        attackActiveDelay = snapshot.attackActiveDelay;
        attackAnimDuration = snapshot.attackAnimDuration;
        projectileSpawnForwardOffset = snapshot.spawnForwardOffset;
        projectileSpawnHeightOffset = snapshot.spawnHeightOffset;
        projectileVisualScale = snapshot.visualScale;
        projectileVisualRotationOffset = snapshot.visualRotationOffset;
        projectileSpeed = snapshot.moveSpeed;
        projectileHitRadius = snapshot.hitRadius;
        projectileTurnSpeed = snapshot.turnSpeed;
        projectileExplosionRadius = snapshot.explosionRadius;
        projectileMaxHitTargets = snapshot.maxHitTargets;
        projectileMaxLifetime = snapshot.maxLifetime;
        targetSearchRange = snapshot.targetSearchRange;
        if (snapshot.damage > 0)
        {
            projectileDamage = snapshot.damage;
        }
    }

    public bool RequestTestAttack()
    {
        if (!hasStaff || isAttacking)
        {
            return false;
        }

        nextAttackTime = 0f;
        StartCoroutine(AttackRoutine());
        return true;
    }

    PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        EnsureMagicAssets();
        equipRefreshPhase = Mathf.Abs(GetInstanceID()) % WeaponEquipRefreshIntervalFrames;
    }

    void Update()
    {
        if (!hasStaff)
        {
            return;
        }

        if (staffSprite != null
            && (Time.frameCount % WeaponEquipRefreshIntervalFrames) == equipRefreshPhase)
        {
            SpumWeaponEquip.TryEquip(spumPrefabs, staffSprite, SpumWeaponVisualKind.Staff);
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (isAttacking || Time.time < nextAttackTime)
        {
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    public bool TryEquipStaff(Sprite staffWeaponSprite)
    {
        EnsureMagicAssets();

        Sprite spriteToEquip = staffWeaponSprite != null ? staffWeaponSprite : staffSprite;
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

        if (!SpumWeaponEquip.TryEquip(spumPrefabs, spriteToEquip, SpumWeaponVisualKind.Staff))
        {
            return false;
        }

        staffSprite = spriteToEquip;
        hasStaff = true;
        return true;
    }

    public void UnequipStaff()
    {
        hasStaff = false;
        if (spumPrefabs != null)
        {
            SpumWeaponEquip.Clear(spumPrefabs);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        if (HasAnimationClips(PlayerState.ATTACK))
        {
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, 0);
        }

        yield return new WaitForSeconds(attackActiveDelay);
        SpawnFireball();

        yield return new WaitForSeconds(attackAnimDuration);
        isAttacking = false;

        if (playerMovement != null && playerMovement.LocomotionAnimation != null)
        {
            playerMovement.LocomotionAnimation.ForceSetMoving(playerMovement.IsWantingToMove());
        }
    }

    void SpawnFireball()
    {
        EnsureMagicAssets();
        if (fireballPrefab == null)
        {
            return;
        }

        if (!TryFindLockTarget(out Transform target, out float targetSurfaceY))
        {
            return;
        }

        Vector3 origin = transform.position;
        float surfaceY = GroundHeightSampler.GetSurfaceY(origin, GameSession.GroundY);
        origin.y = surfaceY + projectileSpawnHeightOffset;

        Vector3 toTargetFlat = target.position - origin;
        toTargetFlat.y = 0f;
        if (toTargetFlat.sqrMagnitude > 0.0001f)
        {
            origin += toTargetFlat.normalized * projectileSpawnForwardOffset;
        }

        HomingFireballProjectile.Settings settings = new HomingFireballProjectile.Settings
        {
            speed = projectileSpeed,
            turnSpeedDegreesPerSecond = projectileTurnSpeed,
            hitRadius = projectileHitRadius,
            explosionRadius = projectileExplosionRadius,
            maxHitTargets = projectileMaxHitTargets,
            damage = projectileDamage,
            maxLifetime = projectileMaxLifetime,
            visualScale = projectileVisualScale,
            visualRotationOffset = projectileVisualRotationOffset
        };

        HomingFireballProjectile.Spawn(origin, target, transform, fireballPrefab, targetSurfaceY, settings);
    }

    bool TryFindLockTarget(out Transform target, out float targetSurfaceY)
    {
        target = null;
        targetSurfaceY = 0f;

        float bestDistanceSqr = float.MaxValue;
        Vector3 origin = transform.position;
        float maxRangeSqr = targetSearchRange * targetSearchRange;

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

            Vector3 diff = monster.position - origin;
            diff.y = 0f;
            float distanceSqr = diff.sqrMagnitude;
            if (distanceSqr > maxRangeSqr || distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            target = monster;
            targetSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(monster.position, monster.position.y);
            bestDistanceSqr = distanceSqr;
        }

        return target != null;
    }

    public void EnsureMagicAssets()
    {
        staffSprite = GameAssets.LoadStaffSprite(staffSprite);
        fireballPrefab = GameAssets.LoadFireballPrefab(fireballPrefab);
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
}
