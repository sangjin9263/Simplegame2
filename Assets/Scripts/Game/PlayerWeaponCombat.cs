using System.Collections;
using UnityEngine;

// 무기 장착과 마우스 왼쪽 버튼 유지 검기 공격을 처리합니다.
public class PlayerWeaponCombat : MonoBehaviour
{
    const string DefaultSlashVfxPath = "Assets/Prefabs/Weapon/1/White Slash v1.prefab";

    [SerializeField] SPUM_Prefabs spumPrefabs;

    [Header("공격")]
    [SerializeField] float attackCooldown = 0.45f;
    [SerializeField] float attackActiveDelay = 0.08f;
    [SerializeField] float attackAnimDuration = 0.3f;

    // White Slash v1, rotation (-90,0,-135), scale 1.0 기준 지면 타격 크기입니다.
    const float SlashHitLengthAtUnitScale = 3.48f;
    const float SlashHitWidthAtUnitScale = 1.74f;

    [Header("검기")]
    [SerializeField] GameObject slashVfxPrefab;
    const int DefaultSwordWaveDamage = 3;

    [SerializeField] int swordWaveDamage = DefaultSwordWaveDamage;
    [SerializeField] float waveSpeed = 14f;
    [Tooltip("검기 이펙트(시각) 크기입니다.")]
    [SerializeField] float slashVfxScale = 0.3f;
    [Tooltip("검기 타격 판정 크기입니다. 기본 0.85를 유지하면 기존 판정과 동일합니다.")]
    [SerializeField] float slashHitboxScale = 0.3f;
    [SerializeField] Vector3 slashVfxRotationOffset = new Vector3(-90f, 0f, -135f);
    [Tooltip("공격 방향으로 캐릭터에서 검기까지 앞쪽 거리입니다. Slash Vfx Scale 과 무관합니다.")]
    [SerializeField] float slashSpawnForwardOffset = 0.22f;
    [SerializeField] float slashSpawnSideOffset = 0.28f;
    [SerializeField] float slashSpawnHeightOffset = 0.45f;
    [SerializeField] float slashMaxVerticalHitDelta = 1.2f;

    bool hasWeapon;
    bool isAttacking;
    float nextAttackTime;

    public bool IsAttacking => isAttacking;
    Sprite equippedWeaponSprite;

    PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        EnsureSlashVfxPrefab();
    }

    void Update()
    {
        if (!hasWeapon)
        {
            return;
        }

        // SPUM 내부에서 무기 슬롯이 꺼져도 장착 상태에서는 매 프레임 다시 붙입니다.
        if (equippedWeaponSprite != null)
        {
            SpumWeaponEquip.TryEquip(spumPrefabs, equippedWeaponSprite, SpumWeaponVisualKind.Melee);
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

    public bool HasEquippedWeapon => hasWeapon;

    public bool TryEquipWeapon(Sprite weaponSprite)
    {
        if (weaponSprite == null)
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

        if (!SpumWeaponEquip.TryEquip(spumPrefabs, weaponSprite, SpumWeaponVisualKind.Melee))
        {
            return false;
        }

        equippedWeaponSprite = weaponSprite;
        hasWeapon = true;
        return true;
    }

    public void UnequipWeapon()
    {
        hasWeapon = false;
        equippedWeaponSprite = null;
        if (spumPrefabs != null)
        {
            SpumWeaponEquip.Clear(spumPrefabs);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        if (equippedWeaponSprite != null)
        {
            SpumWeaponEquip.TryEquip(spumPrefabs, equippedWeaponSprite, SpumWeaponVisualKind.Melee);
        }

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
        SpawnSwordWave();

        yield return new WaitForSeconds(attackAnimDuration);
        isAttacking = false;
        RestoreLocomotionAnimation();
    }

    void SpawnSwordWave()
    {
        EnsureSlashVfxPrefab();

        Vector3 attackDirection = GetAttackDirection();
        Vector3 origin = transform.position;
        origin.y = GroundHeightSampler.GetSurfaceY(origin, GameSession.GroundY);

        int facingSide = playerMovement != null ? playerMovement.LastFlipSide : 0;
        if (facingSide == 0)
        {
            facingSide = 1;
        }

        float hitboxScale = Mathf.Max(0.1f, slashHitboxScale);
        float scaledHitLength = SlashHitLengthAtUnitScale * hitboxScale;
        float scaledHitWidth = SlashHitWidthAtUnitScale * hitboxScale;

        SwordWave.Settings settings = new SwordWave.Settings
        {
            waveSpeed = waveSpeed,
            waveMaxDistance = scaledHitLength,
            waveWidth = scaledHitWidth,
            spawnForwardOffset = slashSpawnForwardOffset,
            spawnSideOffset = slashSpawnSideOffset * facingSide,
            spawnHeightOffset = slashSpawnHeightOffset,
            slashVfxScale = slashVfxScale,
            slashVfxRotationOffset = slashVfxRotationOffset,
            maxLifetime = scaledHitLength / Mathf.Max(waveSpeed, 0.01f) + 0.15f,
            waveDamage = swordWaveDamage > 0 ? swordWaveDamage : DefaultSwordWaveDamage,
            maxVerticalHitDelta = slashMaxVerticalHitDelta
        };

        SwordWave.Spawn(origin, attackDirection, transform, slashVfxPrefab, origin.y, settings);
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

    public void EnsureSlashVfxPrefab()
    {
        slashVfxPrefab = GameAssets.LoadSlashVfxPrefab(slashVfxPrefab);
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
        EnsureSlashVfxPrefab();
    }
#endif
}
