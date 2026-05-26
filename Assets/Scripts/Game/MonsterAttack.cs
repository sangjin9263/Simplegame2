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
    float nextAttackTime;
    bool isAttacking;
    int lastFlipSide;

    public bool IsAttacking => isAttacking;

    void Start()
    {
        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
    }

    void Update()
    {
        MonsterHealth health = GetComponent<MonsterHealth>();
        if (health != null && health.IsDead)
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

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
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
        PlayerMovement playerMovement = playerTransform.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerPosition = PlayerMovement.LastWorldCenter;
        }
        else if (PlayerWorldPosition.TryGetWorldCenter(0f, out Vector3 trackedPosition))
        {
            playerPosition = trackedPosition;
        }

        toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;
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

        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth == null || !playerHealth.IsAlive)
        {
            return;
        }

        playerHealth.TakeDamage(attackDamage);
    }
}
