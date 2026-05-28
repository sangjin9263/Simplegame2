using System.Collections;
using UnityEngine;

// 원거리 몬스터가 플레이어를 향해 투사체를 발사합니다.
public class MonsterRangedAttack : MonoBehaviour
{
    enum ProjectileKind
    {
        Arrow = 0,
        EnergyBall = 1
    }

    [Header("사거리 공격")]
    [SerializeField] float attackRange = 13f;
    [SerializeField] float attackCooldown = 1.35f;
    [SerializeField] float attackAnimDuration = 0.45f;
    [SerializeField] float fireDelayNormalizedTime = 0.35f;
    [SerializeField] int attackDamage = 1;
    [SerializeField] ProjectileKind projectileKind = ProjectileKind.Arrow;

    [Header("투사체")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float arrowProjectileSpeed = 22f;
    [SerializeField] float arrowProjectileMaxRange = 16f;
    [SerializeField] float arrowProjectileHitRadius = 0.6f;
    [SerializeField] float energyProjectileSpeed = 14f;
    [SerializeField] float energyProjectileHitRadius = 0.42f;
    [SerializeField] float energyProjectileMaxLifetime = 2.5f;
    [SerializeField] float projectileSpawnForwardOffset = 0.55f;
    [SerializeField] float projectileSpawnHeightOffset = 0.65f;
    [SerializeField] float targetAimHeightOffset = 0.45f;
    [SerializeField] Vector3 arrowVisualRotationOffset = new Vector3(90f, -90f, -45f);
    [SerializeField] float arrowVisualScale = 0.35f;
    [SerializeField] float arrowVisualScaleMultiplier = 0.48f;
    [SerializeField] float arrowArcHeightMin = 0.3f;
    [SerializeField] float arrowArcHeightMax = 0.9f;
    [SerializeField] float arrowArcHeightDistanceMultiplier = 0.07f;
    [SerializeField] float energyVisualScaleMultiplier = 0.8f;

    [SerializeField] SPUM_Prefabs spumPrefabs;

    Transform playerTransform;
    MonsterHealth monsterHealth;
    float nextAttackTime;
    bool isAttacking;
    int lastFlipSide;

    public bool IsAttacking => isAttacking;

    public void Configure(int damage)
    {
        attackDamage = Mathf.Max(0, damage);
    }

    void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
    }

    void Start()
    {
        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        if (transform.name.IndexOf("m9", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            projectileKind = ProjectileKind.EnergyBall;
        }
        else if (transform.name.IndexOf("m7", System.StringComparison.OrdinalIgnoreCase) >= 0
                 || transform.name.IndexOf("m8", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            projectileKind = ProjectileKind.Arrow;
        }

        EnsureProjectileAsset();
        if (projectileKind == ProjectileKind.Arrow)
        {
            SyncArrowSettingsFromPlayer();
        }

        ResolvePlayer();
    }

    void Update()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return;
        }

        if (isAttacking || Time.time < nextAttackTime)
        {
            return;
        }

        if (!ResolvePlayer())
        {
            return;
        }

        Vector3 toPlayer = GetVectorToPlayer();
        if (toPlayer.sqrMagnitude > attackRange * attackRange)
        {
            return;
        }

        float attackerSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(transform.position, transform.position.y);
        float playerSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(playerTransform.position, playerTransform.position.y);
        if (GroundHeightSampler.IsWalkableTerrainBlockingLine(
                transform.position,
                attackerSurfaceY,
                playerTransform.position,
                playerSurfaceY))
        {
            return;
        }

        StartCoroutine(AttackRoutine(toPlayer));
    }

    IEnumerator AttackRoutine(Vector3 toPlayer)
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        Vector3 facingDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.forward;
        ApplyFacing(facingDirection);
        float waitDuration = PlayAttackAnimation();

        float fireDelay = Mathf.Clamp(waitDuration * fireDelayNormalizedTime, 0.05f, waitDuration);
        yield return new WaitForSeconds(fireDelay);
        FireProjectile();

        float remaining = Mathf.Max(0f, waitDuration - fireDelay);
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        isAttacking = false;
        RestoreLocomotion();
    }

    bool ResolvePlayer()
    {
        if (playerTransform != null)
        {
            return true;
        }

        if (GameSession.TryGetPlayerTransform(out Transform player))
        {
            playerTransform = player;
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return false;
        }

        playerTransform = playerObject.transform;
        return true;
    }

    Vector3 GetVectorToPlayer()
    {
        Vector3 playerPosition = playerTransform.position;
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 trackedPosition))
        {
            playerPosition = trackedPosition;
        }

        return playerPosition - transform.position;
    }

    void FireProjectile()
    {
        if (!ResolvePlayer())
        {
            return;
        }

        if (projectileKind == ProjectileKind.Arrow)
        {
            SyncArrowSettingsFromPlayer();
        }

        EnsureProjectileAsset();
        if (projectilePrefab == null)
        {
            return;
        }

        Vector3 origin = transform.position;
        float attackerSurfaceY = GroundHeightSampler.GetSurfaceY(origin, GameSession.GroundY);
        origin.y = attackerSurfaceY + projectileSpawnHeightOffset;

        Vector3 toPlayer = GetVectorToPlayer();
        Vector3 toPlayerFlat = toPlayer;
        toPlayerFlat.y = 0f;
        if (toPlayerFlat.sqrMagnitude > 0.0001f)
        {
            origin += toPlayerFlat.normalized * projectileSpawnForwardOffset;
        }

        Vector3 targetPosition = GetPlayerAimPoint();

        Vector3 direction = targetPosition - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        bool energyImpact = projectileKind == ProjectileKind.EnergyBall;
        bool isEnergyProjectile = projectileKind == ProjectileKind.EnergyBall;
        float projectileSpeed = isEnergyProjectile ? energyProjectileSpeed : arrowProjectileSpeed;
        float projectileHitRadius = isEnergyProjectile ? energyProjectileHitRadius : arrowProjectileHitRadius;
        float projectileLifetime = isEnergyProjectile
            ? energyProjectileMaxLifetime
            : arrowProjectileMaxRange / Mathf.Max(projectileSpeed, 0.01f) + 0.1f;
        float projectileVisualScale = isEnergyProjectile
            ? arrowVisualScale * Mathf.Clamp(energyVisualScaleMultiplier, 0.1f, 1f)
            : arrowVisualScale * Mathf.Clamp(arrowVisualScaleMultiplier, 0.1f, 1f);
        float trajectoryArcHeight = 0f;
        if (!isEnergyProjectile)
        {
            float shotDistance = Vector3.Distance(origin, targetPosition);
            trajectoryArcHeight = Mathf.Clamp(
                shotDistance * arrowArcHeightDistanceMultiplier,
                arrowArcHeightMin,
                arrowArcHeightMax);
        }

        var settings = new MonsterRangedProjectile.Settings
        {
            speed = projectileSpeed,
            hitRadius = projectileHitRadius,
            damage = attackDamage,
            maxLifetime = projectileLifetime,
            useEnergyImpact = energyImpact,
            visualRotationOffset = arrowVisualRotationOffset,
            visualScale = projectileVisualScale,
            aimHeightOffset = targetAimHeightOffset,
            targetPoint = targetPosition,
            trajectoryArcHeight = trajectoryArcHeight
        };

        MonsterRangedProjectile.Spawn(origin, direction.normalized, projectilePrefab, settings);
    }

    Vector3 GetPlayerAimPoint()
    {
        Vector3 playerWorld = playerTransform.position;
        float playerSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(playerWorld, playerWorld.y);

        float aimX = playerWorld.x;
        float aimZ = playerWorld.z;
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 trackedCenter))
        {
            aimX = trackedCenter.x;
            aimZ = trackedCenter.z;
        }

        return new Vector3(aimX, playerSurfaceY + targetAimHeightOffset, aimZ);
    }

    void SyncArrowSettingsFromPlayer()
    {
        GameSession.TryGetPlayerTransform(out _);
        PlayerRangedCombat playerRanged = GameSession.PlayerRangedCombat;
        if (playerRanged != null)
        {
            ApplyArrowProfile(playerRanged.GetArrowProjectileProfile());
            return;
        }

        ApplyArrowProfile(new PlayerRangedCombat.ArrowProjectileProfile
        {
            visualRotationOffset = new Vector3(90f, -90f, -45f),
            visualScale = 0.168f,
            spawnHeightOffset = 0.65f,
            spawnForwardOffset = 0.55f,
            speed = 22f,
            maxRange = 16f,
            hitRadius = 0.6f,
            aimHeightOffset = 0.45f
        });
    }

    void ApplyArrowProfile(PlayerRangedCombat.ArrowProjectileProfile profile)
    {
        arrowVisualRotationOffset = profile.visualRotationOffset;
        arrowVisualScale = profile.visualScale;
        projectileSpawnHeightOffset = profile.spawnHeightOffset;
        projectileSpawnForwardOffset = profile.spawnForwardOffset;
        arrowProjectileSpeed = profile.speed;
        arrowProjectileMaxRange = profile.maxRange;
        arrowProjectileHitRadius = profile.hitRadius;
        targetAimHeightOffset = profile.aimHeightOffset;
    }

    void EnsureProjectileAsset()
    {
        if (projectileKind == ProjectileKind.EnergyBall)
        {
            projectilePrefab = GameAssets.LoadEnergyBallPrefab(projectilePrefab);
        }
        else
        {
            projectilePrefab = GameAssets.LoadArrowPrefab(projectilePrefab);
        }
    }

    void ApplyFacing(Vector3 facingDirection)
    {
        if (spumPrefabs == null)
        {
            return;
        }

        SpumSpriteFlip.ApplyByFacingDirection(spumPrefabs.transform, facingDirection, ref lastFlipSide);
    }

    float PlayAttackAnimation()
    {
        float duration = attackAnimDuration;

        if (spumPrefabs != null
            && spumPrefabs.ATTACK_List != null
            && spumPrefabs.ATTACK_List.Count > 0
            && spumPrefabs.ATTACK_List[0] != null)
        {
            duration = spumPrefabs.ATTACK_List[0].length;
            if (spumPrefabs.OverrideController != null)
            {
                spumPrefabs.OverrideController["ATTACK"] = spumPrefabs.ATTACK_List[0];
            }
        }

        Animator animator = spumPrefabs != null ? spumPrefabs._anim : null;
        if (animator == null)
        {
            return duration;
        }

        animator.SetBool("1_Move", false);
        animator.SetBool("5_Debuff", false);
        animator.SetBool("isDeath", false);
        animator.ResetTrigger("2_Attack");
        animator.SetTrigger("2_Attack");
        return duration;
    }

    void RestoreLocomotion()
    {
        MonsterMovement movement = GetComponent<MonsterMovement>();
        if (movement != null)
        {
            movement.RestoreLocomotionAfterAttack();
            return;
        }

        if (spumPrefabs == null || spumPrefabs._anim == null)
        {
            return;
        }

        Vector3 toPlayer = ResolvePlayer() ? GetVectorToPlayer() : Vector3.zero;
        bool wantsMove = toPlayer.sqrMagnitude > attackRange * attackRange;
        Animator animator = spumPrefabs._anim;
        animator.SetBool("isDeath", false);
        animator.SetBool("5_Debuff", false);
        animator.ResetTrigger("2_Attack");
        animator.SetBool("1_Move", wantsMove);
    }
}
