using System.Collections;
using UnityEngine;

// 무기 장착과 마우스 왼쪽 클릭 검기 공격을 처리합니다.
public class PlayerWeaponCombat : MonoBehaviour
{
    const string DefaultSlashVfxPath = "Assets/Weapon/1/White Slash v1.prefab";

    [SerializeField] SPUM_Prefabs spumPrefabs;
    [SerializeField] float groundHeight = 0f;

    [Header("공격")]
    [SerializeField] float attackCooldown = 0.45f;
    [SerializeField] float attackActiveDelay = 0.08f;
    [SerializeField] float attackAnimDuration = 0.3f;

    // White Slash v1, rotation (-90,0,-135), scale 1.0 기준 지면 타격 크기입니다.
    const float SlashHitLengthAtUnitScale = 3.48f;
    const float SlashHitWidthAtUnitScale = 1.74f;
    const float SlashSpawnForwardAtUnitScale = 0.39f;

    [Header("검기")]
    [SerializeField] GameObject slashVfxPrefab;
    const int DefaultSwordWaveDamage = 3;

    [SerializeField] int swordWaveDamage = DefaultSwordWaveDamage;
    [SerializeField] float waveSpeed = 14f;
    [SerializeField] float slashVfxScale = 1.15f;
    [SerializeField] Vector3 slashVfxRotationOffset = new Vector3(-90f, 0f, -135f);
    [SerializeField] float slashSpawnSideOffset = 0.28f;
    [SerializeField] float slashSpawnHeightOffset = 0.45f;

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
        if (!hasWeapon || isAttacking)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    public bool TryEquipWeapon(Sprite weaponSprite)
    {
        if (weaponSprite == null || spumPrefabs == null)
        {
            return false;
        }

        if (!SpumWeaponEquip.TryEquip(spumPrefabs, weaponSprite))
        {
            return false;
        }

        equippedWeaponSprite = weaponSprite;
        hasWeapon = true;
        return true;
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
        origin.y = groundHeight;

        int facingSide = playerMovement != null ? playerMovement.LastFlipSide : 0;
        if (facingSide == 0)
        {
            facingSide = 1;
        }

        float scaledHitLength = SlashHitLengthAtUnitScale * slashVfxScale;
        float scaledHitWidth = SlashHitWidthAtUnitScale * slashVfxScale;

        SwordWave.Settings settings = new SwordWave.Settings
        {
            waveSpeed = waveSpeed,
            waveMaxDistance = scaledHitLength,
            waveWidth = scaledHitWidth,
            spawnForwardOffset = SlashSpawnForwardAtUnitScale * slashVfxScale,
            spawnSideOffset = slashSpawnSideOffset * facingSide,
            spawnHeightOffset = slashSpawnHeightOffset,
            slashVfxScale = slashVfxScale,
            slashVfxRotationOffset = slashVfxRotationOffset,
            maxLifetime = scaledHitLength / Mathf.Max(waveSpeed, 0.01f) + 0.15f,
            waveDamage = swordWaveDamage > 0 ? swordWaveDamage : DefaultSwordWaveDamage
        };

        SwordWave.Spawn(origin, attackDirection, transform, slashVfxPrefab, groundHeight, settings);
    }

    void RestoreLocomotionAnimation()
    {
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
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
        if (slashVfxPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        slashVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSlashVfxPath);
#endif
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
