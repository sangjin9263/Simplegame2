using System.Collections;
using UnityEngine;

// 플레이어 근처에서 몬스터 공격 모션을 재생합니다.
public class MonsterAttack : MonoBehaviour
{
    [SerializeField] float attackRange = 1.1f;
    [SerializeField] float attackCooldown = 1.1f;
    [SerializeField] float attackAnimDuration = 0.45f;
    [SerializeField] int attackDamage = 1;
    [SerializeField] float damageApplyNormalizedTime = 0.4f;

    [SerializeField] SPUM_Prefabs spumPrefabs;

    Transform playerTransform;
    MonsterHealth monsterHealth;
    MonsterMovement monsterMovement;
    float nextAttackTime;
    bool isAttacking;
    int lastFlipSide;

    public bool IsAttacking => isAttacking;

    public void Configure(int damage)
    {
        attackDamage = Mathf.Max(0, damage);
    }

    public MonsterVisualTuningSnapshot ExportVisualTuning(MonsterKind kind, int monId, string monName)
    {
        return new MonsterVisualTuningSnapshot
        {
            monId = monId,
            monName = monName ?? string.Empty,
            kind = kind,
            damage = attackDamage,
            attackRange = attackRange,
            attackCooldown = attackCooldown,
            attackAnimDuration = attackAnimDuration,
            damageApplyNormalizedTime = damageApplyNormalizedTime
        };
    }

    public void ApplyVisualTuning(MonsterVisualTuningSnapshot snapshot)
    {
        if (snapshot == null || snapshot.kind != MonsterKind.Melee)
        {
            return;
        }

        attackRange = snapshot.attackRange;
        attackCooldown = snapshot.attackCooldown;
        attackAnimDuration = snapshot.attackAnimDuration;
        damageApplyNormalizedTime = snapshot.damageApplyNormalizedTime;
        if (snapshot.damage > 0)
        {
            attackDamage = snapshot.damage;
        }
    }

    public bool RequestTestAttack()
    {
        if (isAttacking || (monsterHealth != null && monsterHealth.IsDead))
        {
            return false;
        }

        if (playerTransform == null && !TryResolvePlayer())
        {
            return false;
        }

        if (!TryGetVectorToPlayer(out Vector3 toPlayer))
        {
            return false;
        }

        nextAttackTime = 0f;
        StartCoroutine(AttackRoutine(toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.forward));
        return true;
    }

    bool TryResolvePlayer()
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

    void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        monsterMovement = GetComponent<MonsterMovement>();
    }

    void Start()
    {
        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        if (!GameSession.TryGetPlayerTransform(out Transform player))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }
        else
        {
            playerTransform = player;
        }
    }

    void Update()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
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

        if (playerTransform == null && GameSession.TryGetPlayerTransform(out Transform player))
        {
            playerTransform = player;
        }

        if (playerTransform == null)
        {
            return;
        }

        if (!TryGetVectorToPlayer(out Vector3 toPlayer))
        {
            return;
        }

        if (toPlayer.sqrMagnitude > attackRange * attackRange)
        {
            return;
        }

        StartCoroutine(AttackRoutine(toPlayer.normalized));
    }

    IEnumerator AttackRoutine(Vector3 facingDirection)
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        ApplyFacing(facingDirection);
        float waitDuration = PlayAttackAnimation();
        float damageDelay = Mathf.Clamp(waitDuration * damageApplyNormalizedTime, 0.05f, waitDuration);
        yield return new WaitForSeconds(damageDelay);
        TryDamagePlayer();

        float remaining = Mathf.Max(0f, waitDuration - damageDelay);
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        isAttacking = false;
        RestoreLocomotion();
    }

    void RestoreLocomotion()
    {
        if (monsterMovement != null)
        {
            monsterMovement.RestoreLocomotionAfterAttack();
            return;
        }

        if (spumPrefabs == null || spumPrefabs._anim == null)
        {
            return;
        }

        bool wantsMove = false;
        if (TryGetVectorToPlayer(out Vector3 toPlayer))
        {
            wantsMove = toPlayer.sqrMagnitude > attackRange * attackRange;
        }

        Animator animator = spumPrefabs._anim;
        animator.SetBool("isDeath", false);
        animator.SetBool("5_Debuff", false);
        animator.ResetTrigger("2_Attack");
        animator.SetBool("1_Move", wantsMove);
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

    bool TryGetVectorToPlayer(out Vector3 toPlayer)
    {
        toPlayer = Vector3.zero;

        if (playerTransform == null)
        {
            return false;
        }

        Vector3 playerPosition = playerTransform.position;
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 trackedPosition))
        {
            playerPosition = trackedPosition;
        }

        Vector3 monsterFlat = SpumChasePosition.GetFlatChasePoint(transform);
        Vector3 playerFlat = new Vector3(playerPosition.x, 0f, playerPosition.z);
        toPlayer = playerFlat - monsterFlat;
        return true;
    }

    void TryDamagePlayer()
    {
        if (playerTransform == null || attackDamage <= 0)
        {
            return;
        }

        if (!TryGetVectorToPlayer(out Vector3 toPlayer))
        {
            return;
        }

        if (toPlayer.sqrMagnitude > attackRange * attackRange)
        {
            return;
        }

        PlayerHealth playerHealth = GameSession.PlayerHealth;
        if (playerHealth == null || !playerHealth.IsAlive)
        {
            return;
        }

        playerHealth.TakeDamage(attackDamage);
    }
}
