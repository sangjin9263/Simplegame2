using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어를 향해 달려오는 몬스터 이동·애니메이션입니다.
public class MonsterMovement : MonoBehaviour
{
    // 초당 이동 거리입니다.
    [SerializeField] float moveSpeed = 3.5f;

    // 발 Y 위치입니다.
    [SerializeField] float groundHeight = 0f;

    // 플레이어와 이 거리 이하면 멈춥니다.
    [SerializeField] float stopDistance = 0.9f;

    [SerializeField] float collisionRadius = 0.22f;
    [SerializeField] float collisionHeight = 1.05f;

    // 다른 몬스터와 이 거리 안이면 밀어냅니다.
    [SerializeField] float separationRadius = 0.65f;

    // 몬스터끼리 최소 이 거리는 유지합니다 (겹침 방지).
    [SerializeField] float minMonsterSpacing = 0.55f;

    // 겹침 해소를 몇 번 반복할지입니다 (뭉칠수록 2~3).
    [SerializeField] int overlapResolvePasses = 2;

    // 몬스터 겹침 방지 강도입니다.
    [SerializeField] float separationStrength = 1.2f;

    // 분리 방향을 부드럽게 바꿉니다 (클수록 빠르게 반응).
    [SerializeField] float separationSmoothing = 8f;

    // 이 수 이상 붙으면 방향 스무딩을 켭니다.
    [SerializeField] int crowdedNeighborThreshold = 2;

    [SerializeField] SPUM_Prefabs spumPrefabs;

    Transform playerTransform;
    SpumLocomotionAnimation locomotionAnimation;
    int lastFlipSide;

    Vector3 smoothedSeparation;
    Vector3 smoothedFacingDirection;

    static readonly List<Transform> ActiveMonsters = new List<Transform>();

    public static int ActiveMonsterCount => ActiveMonsters.Count;

    public static Transform GetActiveMonster(int index)
    {
        if (index < 0 || index >= ActiveMonsters.Count)
        {
            return null;
        }

        return ActiveMonsters[index];
    }

    public bool IsChasingPlayer()
    {
        Vector3 toPlayer = GetVectorToPlayer();
        return toPlayer.sqrMagnitude > stopDistance * stopDistance;
    }

    // 공격 모션 후 걷기/대기 애니를 다시 맞춥니다.
    public void RestoreLocomotionAfterAttack()
    {
        bool wantsMove = IsChasingPlayer();
        ResetAnimatorForLocomotion(wantsMove);

        if (locomotionAnimation != null)
        {
            locomotionAnimation.ForceSetMoving(wantsMove);
        }
    }

    void ResetAnimatorForLocomotion(bool moving)
    {
        if (spumPrefabs == null || spumPrefabs._anim == null)
        {
            return;
        }

        Animator animator = spumPrefabs._anim;
        animator.SetBool("isDeath", false);
        animator.SetBool("5_Debuff", false);
        animator.ResetTrigger("2_Attack");
        animator.SetBool("1_Move", moving);
    }

    CapsuleMotor.Settings motorSettings;

    void OnEnable()
    {
        if (!ActiveMonsters.Contains(transform))
        {
            ActiveMonsters.Add(transform);
        }
    }

    void OnDisable()
    {
        ActiveMonsters.Remove(transform);
    }

    void Awake()
    {
        motorSettings = new CapsuleMotor.Settings
        {
            radius = collisionRadius,
            height = collisionHeight,
            groundY = groundHeight,
            skin = 0.015f,
            embeddedIgnoreDistance = 0.02f
        };
    }

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

        SnapToGround();
        TryEscapeTreeOverlap();
        smoothedFacingDirection = transform.forward;
        StartCoroutine(InitializeSpumNextFrame());
    }

    void TryEscapeTreeOverlap()
    {
        if (MonsterSpawnPlacement.TryResolveClearPosition(
                transform.position,
                collisionRadius,
                collisionHeight,
                groundHeight,
                out Vector3 clearPosition))
        {
            transform.position = clearPosition;
        }
    }

    IEnumerator InitializeSpumNextFrame()
    {
        yield return null;

        if (spumPrefabs == null)
        {
            yield break;
        }

        locomotionAnimation = new SpumLocomotionAnimation(spumPrefabs);
        locomotionAnimation.Initialize();
    }

    void Update()
    {
        MonsterHealth health = GetComponent<MonsterHealth>();
        if (health != null && health.IsDead)
        {
            return;
        }

        MonsterAttack monsterAttack = GetComponent<MonsterAttack>();
        if (monsterAttack != null && monsterAttack.IsAttacking)
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

        Vector3 toPlayer = GetVectorToPlayer();
        float stopDistanceSqr = stopDistance * stopDistance;
        bool isChasing = toPlayer.sqrMagnitude > stopDistanceSqr;

        if (!isChasing)
        {
            smoothedSeparation = Vector3.zero;

            if (monsterAttack == null || !monsterAttack.IsAttacking)
            {
                if (locomotionAnimation != null)
                {
                    locomotionAnimation.SetMoving(false);
                }
            }

            return;
        }

        int neighborCount = CountNeighborsInSeparationRadius();
        bool isCrowded = neighborCount >= crowdedNeighborThreshold;

        float stepDistance = moveSpeed * Time.deltaTime;
        Vector3 chaseDirection = toPlayer.normalized;
        Vector3 rawSeparation = GetSeparationFromOtherMonsters();
        float separationBlend = Mathf.Clamp01(Time.deltaTime * separationSmoothing);
        smoothedSeparation = Vector3.Lerp(smoothedSeparation, rawSeparation, separationBlend);

        if (smoothedSeparation.sqrMagnitude < 0.01f)
        {
            smoothedSeparation = Vector3.zero;
        }

        Vector3 separationPush = GetLateralSeparation(chaseDirection, smoothedSeparation, separationStrength);
        Vector3 desiredDirection = chaseDirection;

        if (separationPush.sqrMagnitude > 0.0001f)
        {
            desiredDirection = (chaseDirection + separationPush).normalized;
        }

        Vector3 moveDirection = PickMoveDirection(chaseDirection, desiredDirection, stepDistance);

        Vector3 positionBefore = transform.position;
        CapsuleMotor.Move(
            transform,
            moveDirection * stepDistance,
            motorSettings,
            transform,
            MoverRole.Monster);

        Vector3 moved = transform.position - positionBefore;
        moved.y = 0f;

        if (isCrowded && moved.sqrMagnitude < stepDistance * stepDistance * 0.04f)
        {
            Vector3 slideDirection = GetCrowdedSlideDirection(chaseDirection);
            CapsuleMotor.Move(
                transform,
                slideDirection * stepDistance,
                motorSettings,
                transform,
                MoverRole.Monster);

            moveDirection = slideDirection;
        }

        Vector3 facingDirection = moveDirection;
        if (isCrowded)
        {
            float facingBlend = Mathf.Clamp01(Time.deltaTime * 14f);
            if (smoothedFacingDirection.sqrMagnitude < 0.0001f)
            {
                smoothedFacingDirection = facingDirection;
            }
            else
            {
                smoothedFacingDirection = Vector3.Slerp(smoothedFacingDirection, facingDirection, facingBlend).normalized;
            }

            facingDirection = smoothedFacingDirection;
        }
        else
        {
            smoothedFacingDirection = facingDirection;
        }

        if (spumPrefabs != null)
        {
            SpumSpriteFlip.ApplyByMoveDirection(spumPrefabs.transform, facingDirection, ref lastFlipSide);
        }

        if (locomotionAnimation != null)
        {
            locomotionAnimation.SetMoving(true);
        }
    }

    // 이동이 끝난 뒤 겹친 몬스터를 밀어냅니다 (멈춰 있어도 실행).
    void LateUpdate()
    {
        int passes = Mathf.Max(1, overlapResolvePasses);
        for (int i = 0; i < passes; i++)
        {
            ResolveOverlapWithOtherMonsters();
        }
    }

    void ResolveOverlapWithOtherMonsters()
    {
        Vector3 position = transform.position;
        Vector3 pushOffset = Vector3.zero;

        for (int i = 0; i < ActiveMonsters.Count; i++)
        {
            Transform other = ActiveMonsters[i];
            if (other == null || other == transform)
            {
                continue;
            }

            Vector3 away = position - other.position;
            away.y = 0f;
            float distance = away.magnitude;

            if (distance >= minMonsterSpacing)
            {
                continue;
            }

            if (distance < 0.001f)
            {
                away = GetDeterministicSeparationDirection(other);
                distance = 0.001f;
            }

            float overlap = minMonsterSpacing - distance;
            pushOffset += away.normalized * overlap;
        }

        if (pushOffset.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float pushDistance = Mathf.Min(pushOffset.magnitude, minMonsterSpacing * 0.6f);
        Vector3 pushDirection = pushOffset.normalized;
        CapsuleMotor.Move(
            transform,
            pushDirection * pushDistance,
            motorSettings,
            transform,
            MoverRole.Monster);
    }

    Vector3 PickMoveDirection(Vector3 chaseDirection, Vector3 desiredDirection, float stepDistance)
    {
        float directAllowed = CapsuleMotor.GetAllowedMoveDistance(
            transform.position,
            desiredDirection,
            stepDistance,
            motorSettings,
            transform,
            MoverRole.Monster);

        if (directAllowed >= stepDistance * 0.85f)
        {
            return desiredDirection;
        }

        Vector3 steerDirection = CapsuleMotor.FindSteeringDirection(
            transform.position,
            chaseDirection,
            stepDistance,
            motorSettings,
            transform,
            MoverRole.Monster);

        if (steerDirection.sqrMagnitude > 0.0001f)
        {
            return steerDirection;
        }

        return desiredDirection;
    }

    // 추적 방향을 막는 분리 힘은 빼고, 옆으로만 밀어냅니다.
    Vector3 GetLateralSeparation(Vector3 chaseDirection, Vector3 separation, float strength)
    {
        if (separation.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 push = separation * strength;
        float forwardAmount = Vector3.Dot(push, chaseDirection);

        if (forwardAmount < 0f)
        {
            push -= chaseDirection * forwardAmount;
        }

        return push;
    }

    // 뭉쳤을 때 플레이어 주위를 돌며 빈 곳으로 붙습니다.
    Vector3 GetCrowdedSlideDirection(Vector3 chaseDirection)
    {
        Vector3 tangent = Vector3.Cross(Vector3.up, chaseDirection);
        float side = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        return tangent * side;
    }

    int CountNeighborsInSeparationRadius()
    {
        int count = 0;

        for (int i = 0; i < ActiveMonsters.Count; i++)
        {
            Transform other = ActiveMonsters[i];
            if (other == null || other == transform)
            {
                continue;
            }

            Vector3 diff = transform.position - other.position;
            diff.y = 0f;
            if (diff.sqrMagnitude <= separationRadius * separationRadius)
            {
                count++;
            }
        }

        return count;
    }

    Vector3 GetSeparationFromOtherMonsters()
    {
        Vector3 pushSum = Vector3.zero;

        for (int i = 0; i < ActiveMonsters.Count; i++)
        {
            Transform other = ActiveMonsters[i];
            if (other == null || other == transform)
            {
                continue;
            }

            Vector3 away = transform.position - other.position;
            away.y = 0f;
            float distance = away.magnitude;

            if (distance > separationRadius)
            {
                continue;
            }

            if (distance < 0.001f)
            {
                away = GetDeterministicSeparationDirection(other);
                distance = 0.001f;
            }

            float pushWeight = (separationRadius - distance) / separationRadius;
            pushSum += away.normalized * pushWeight;
        }

        if (pushSum.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return pushSum.normalized;
    }

    Vector3 GetDeterministicSeparationDirection(Transform other)
    {
        int hash = transform.GetInstanceID() ^ (other.GetInstanceID() * 13);
        float angle = (hash & 255) / 255f * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    Vector3 GetVectorToPlayer()
    {
        if (playerTransform == null)
        {
            return Vector3.zero;
        }

        Vector3 playerPosition = playerTransform.position;
        PlayerMovement playerMovement = playerTransform.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerPosition = PlayerMovement.LastWorldCenter;
        }
        else if (PlayerWorldPosition.TryGetWorldCenter(groundHeight, out Vector3 trackedPosition))
        {
            playerPosition = trackedPosition;
        }

        Vector3 toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;
        return toPlayer;
    }

    void SnapToGround()
    {
        Vector3 position = transform.position;
        position.y = groundHeight;
        transform.position = position;
    }
}
