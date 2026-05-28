using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어를 향해 달려오는 몬스터 이동·애니메이션입니다.
[RequireComponent(typeof(CharacterController))]
public class MonsterMovement : MonoBehaviour
{
    // 초당 이동 거리입니다.
    [SerializeField] float moveSpeed = 3.5f;

    [SerializeField] float gravity = -24f;
    [SerializeField] float groundedStickForce = 2f;
    [SerializeField] float slopeLimit = 50f;
    [SerializeField] float stepOffset = 0.35f;

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
    [SerializeField] int crowdScanIntervalFrames = 3;
    [SerializeField] int overlapResolveIntervalFrames = 2;

    [Header("지형 우회 (slope / high land)")]
    [SerializeField] float terrainAvoidMaxProbeDistance = 6f;
    [SerializeField] float terrainAvoidMinClearDistance = 0.45f;
    [SerializeField] float elevationBlockThreshold = 0.95f;
    [SerializeField] int stuckFramesBeforeTerrainAvoid = 1;
    [SerializeField] float stuckMoveProgressRatio = 0.18f;
    [SerializeField] float wallNormalMemorySeconds = 0.45f;
    [SerializeField] int terrainScanIntervalFrames = 10;
    [SerializeField] float terrainDetourProbeStepDistance = 1.2f;
    [SerializeField] float terrainDetourScoreDistanceWeight = 4f;
    [SerializeField] float terrainDetourScoreSightBonus = 16f;
    [SerializeField] float terrainDetourScoreDistancePenalty = 0.22f;

    const float TerrainDetourArcStepDegrees = 35f;
    const int MaxTerrainDetourCandidates = 16;

    [SerializeField] SPUM_Prefabs spumPrefabs;

    Transform playerTransform;
    MonsterHealth monsterHealth;
    MonsterAttack monsterAttack;
    MonsterRangedAttack monsterRangedAttack;
    SpumLocomotionAnimation locomotionAnimation;
    int lastFlipSide;

    Vector3 smoothedSeparation;
    Vector3 smoothedFacingDirection;
    Vector3 lastTerrainDetourDirection;
    Vector3 lastTerrainWallNormal;
    float lastTerrainWallNormalTime;
    int stuckFrameCount;
    int updatePhase;
    int cachedNeighborCount;
    Vector3 cachedRawSeparation;
    bool hasCachedTerrainDetour;
    bool cachedPathBlockedByTerrain;
    Vector3 cachedTerrainDetourDirection;
    readonly Vector3[] terrainDetourCandidates = new Vector3[MaxTerrainDetourCandidates];

    static readonly List<Transform> ActiveMonsters = new List<Transform>();

    public static int ActiveMonsterCount => ActiveMonsters.Count;

    public void Configure(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

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

    CharacterController characterController;
    float verticalVelocity;

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
        monsterHealth = GetComponent<MonsterHealth>();
        monsterAttack = GetComponent<MonsterAttack>();
        monsterRangedAttack = GetComponent<MonsterRangedAttack>();
        characterController = GetComponent<CharacterController>();
        ApplyCharacterControllerSettings();
        updatePhase = Mathf.Abs(GetInstanceID()) % 16;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        terrainScanIntervalFrames = Mathf.Max(2, terrainScanIntervalFrames);
        crowdScanIntervalFrames = Mathf.Max(1, crowdScanIntervalFrames);
        overlapResolveIntervalFrames = Mathf.Max(1, overlapResolveIntervalFrames);
        terrainDetourProbeStepDistance = Mathf.Clamp(terrainDetourProbeStepDistance, 0.5f, 2.5f);
        terrainDetourScoreDistanceWeight = Mathf.Clamp(terrainDetourScoreDistanceWeight, 1f, 8f);
        terrainDetourScoreSightBonus = Mathf.Clamp(terrainDetourScoreSightBonus, 4f, 30f);
        terrainDetourScoreDistancePenalty = Mathf.Clamp(terrainDetourScoreDistancePenalty, 0.05f, 0.6f);

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        ApplyCharacterControllerSettings();
    }
#endif

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

        SnapToGroundOnLoad();
        TryEscapeTreeOverlap();
        smoothedFacingDirection = transform.forward;
        StartCoroutine(InitializeSpumNextFrame());
    }

    void TryEscapeTreeOverlap()
    {
        float fallbackY = transform.position.y;
        if (MonsterSpawnPlacement.TryResolveClearPosition(
                transform.position,
                collisionRadius,
                collisionHeight,
                fallbackY,
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
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return;
        }

        if ((monsterAttack != null && monsterAttack.IsAttacking)
            || (monsterRangedAttack != null && monsterRangedAttack.IsAttacking))
        {
            return;
        }

        if (playerTransform == null)
        {
            if (!GameSession.TryGetPlayerTransform(out Transform player))
            {
                return;
            }

            playerTransform = player;
        }

        Vector3 toPlayer = GetVectorToPlayer();
        float stopDistanceSqr = stopDistance * stopDistance;
        bool isChasing = toPlayer.sqrMagnitude > stopDistanceSqr;

        if (!isChasing)
        {
            smoothedSeparation = Vector3.zero;

            bool anyAttackPlaying = (monsterAttack != null && monsterAttack.IsAttacking)
                || (monsterRangedAttack != null && monsterRangedAttack.IsAttacking);
            if (!anyAttackPlaying)
            {
                if (locomotionAnimation != null)
                {
                    locomotionAnimation.SetMoving(false);
                }
            }

            return;
        }

        bool shouldScanCrowd = ShouldRunStaggered(crowdScanIntervalFrames);
        if (shouldScanCrowd)
        {
            ComputeNeighborAndSeparation(out cachedNeighborCount, out cachedRawSeparation);
        }

        int neighborCount = cachedNeighborCount;
        bool isCrowded = neighborCount >= crowdedNeighborThreshold;

        float stepDistance = moveSpeed * Time.deltaTime;
        Vector3 chaseDirection = toPlayer.normalized;
        Vector3 rawSeparation = shouldScanCrowd ? cachedRawSeparation : smoothedSeparation;
        float separationBlend = Mathf.Clamp01(Time.deltaTime * separationSmoothing);
        smoothedSeparation = Vector3.Lerp(smoothedSeparation, rawSeparation, separationBlend);

        if (smoothedSeparation.sqrMagnitude < 0.01f)
        {
            smoothedSeparation = Vector3.zero;
        }

        bool shouldScanTerrain = ShouldRunStaggered(terrainScanIntervalFrames)
            || stuckFrameCount >= Mathf.Max(2, stuckFramesBeforeTerrainAvoid * 2);

        float monsterSurfaceY = 0f;
        float playerSurfaceY = 0f;
        bool pathBlockedByTerrain = cachedPathBlockedByTerrain;
        Vector3 preferredDetourDirection = cachedTerrainDetourDirection;
        if (shouldScanTerrain)
        {
            monsterSurfaceY = GetMonsterSurfaceY();
            playerSurfaceY = GetPlayerSurfaceY(transform.position + toPlayer);
            pathBlockedByTerrain = IsTerrainChaseBlocked(monsterSurfaceY, playerSurfaceY);
            cachedPathBlockedByTerrain = pathBlockedByTerrain;

            hasCachedTerrainDetour = pathBlockedByTerrain
                && TryGetTerrainDetourDirection(
                    chaseDirection,
                    toPlayer,
                    monsterSurfaceY,
                    playerSurfaceY,
                    out preferredDetourDirection);

            cachedTerrainDetourDirection = preferredDetourDirection;
        }

        Vector3 steeringDirection = chaseDirection;
        if (pathBlockedByTerrain && hasCachedTerrainDetour)
        {
            steeringDirection = preferredDetourDirection;
        }

        Vector3 separationPush = GetLateralSeparation(steeringDirection, smoothedSeparation, separationStrength);
        Vector3 desiredDirection = steeringDirection;

        if (separationPush.sqrMagnitude > 0.0001f)
        {
            desiredDirection = (steeringDirection + separationPush).normalized;
        }

        Vector3 moveDirection = desiredDirection;

        Vector3 moveDirectionFlat = moveDirection;
        moveDirectionFlat.y = 0f;

        Vector3 positionBefore = transform.position;
        MoveWithCharacterController(moveDirectionFlat, stepDistance);

        Vector3 moved = transform.position - positionBefore;
        moved.y = 0f;

        float stuckMoveSqr = stepDistance * stepDistance * stuckMoveProgressRatio;
        if (moved.sqrMagnitude < stuckMoveSqr)
        {
            stuckFrameCount++;
        }
        else
        {
            stuckFrameCount = 0;
        }

        bool shouldTerrainAvoid = pathBlockedByTerrain
            || stuckFrameCount >= stuckFramesBeforeTerrainAvoid;

        if (shouldTerrainAvoid
            && moved.sqrMagnitude < stuckMoveSqr
            && hasCachedTerrainDetour)
        {
            MoveWithCharacterController(cachedTerrainDetourDirection, stepDistance * 1.35f);
            moveDirection = cachedTerrainDetourDirection;
        }
        else if (isCrowded && moved.sqrMagnitude < stepDistance * stepDistance * 0.04f)
        {
            Vector3 slideDirection = GetCrowdedSlideDirection(chaseDirection);
            MoveWithCharacterController(slideDirection, stepDistance);
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
        if (ShouldRunStaggered(overlapResolveIntervalFrames))
        {
            int passes = Mathf.Max(1, overlapResolvePasses);
            for (int i = 0; i < passes; i++)
            {
                ResolveOverlapWithOtherMonsters();
            }
        }

        ApplyGroundingVelocity();
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
        MoveWithCharacterController(pushDirection, pushDistance);
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

    void ComputeNeighborAndSeparation(out int neighborCount, out Vector3 separation)
    {
        int count = 0;
        Vector3 pushSum = Vector3.zero;

        for (int i = 0; i < ActiveMonsters.Count; i++)
        {
            Transform other = ActiveMonsters[i];
            if (other == null || other == transform)
            {
                continue;
            }

            Vector3 diff = transform.position - other.position;
            diff.y = 0f;
            float distance = diff.magnitude;
            if (distance <= separationRadius)
            {
                count++;

                if (distance < 0.001f)
                {
                    diff = GetDeterministicSeparationDirection(other);
                    distance = 0.001f;
                }

                float pushWeight = (separationRadius - distance) / separationRadius;
                pushSum += diff.normalized * pushWeight;
            }
        }

        neighborCount = count;
        separation = pushSum.sqrMagnitude < 0.0001f ? Vector3.zero : pushSum.normalized;
    }

    bool ShouldRunStaggered(int intervalFrames)
    {
        if (intervalFrames <= 1)
        {
            return true;
        }

        return ((Time.frameCount + updatePhase) % intervalFrames) == 0;
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
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 trackedPosition))
        {
            playerPosition = trackedPosition;
        }

        Vector3 toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;
        return toPlayer;
    }

    float GetMonsterSurfaceY()
    {
        Vector3 position = transform.position;
        return GroundHeightSampler.GetCharacterSurfaceY(position, position.y);
    }

    float GetPlayerSurfaceY(Vector3 playerPosition)
    {
        return GroundHeightSampler.GetCharacterSurfaceY(playerPosition, playerPosition.y);
    }

    bool IsTerrainChaseBlocked(float monsterSurfaceY, float playerSurfaceY)
    {
        if (playerTransform == null)
        {
            return false;
        }

        if (playerSurfaceY - monsterSurfaceY > elevationBlockThreshold)
        {
            return true;
        }

        Vector3 playerPosition = playerTransform.position;
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 trackedPosition))
        {
            playerPosition = trackedPosition;
        }

        return GroundHeightSampler.IsWalkableTerrainBlockingLine(
            transform.position,
            monsterSurfaceY,
            playerPosition,
            playerSurfaceY);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!GroundHeightSampler.IsWalkableGroundCollider(hit.collider))
        {
            return;
        }

        Vector3 wallNormal = hit.normal;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude < 0.01f)
        {
            return;
        }

        lastTerrainWallNormal = wallNormal.normalized;
        lastTerrainWallNormalTime = Time.time;
    }

    void AddTerrainDetourCandidate(ref int count, Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        direction.Normalize();
        for (int i = 0; i < count; i++)
        {
            if (Vector3.Dot(terrainDetourCandidates[i], direction) > 0.98f)
            {
                return;
            }
        }

        if (count >= terrainDetourCandidates.Length)
        {
            return;
        }

        terrainDetourCandidates[count] = direction;
        count++;
    }

    bool TryGetTerrainDetourDirection(
        Vector3 chaseDirection,
        Vector3 toPlayer,
        float monsterSurfaceY,
        float playerSurfaceY,
        out Vector3 detourDirection)
    {
        detourDirection = chaseDirection;

        if (chaseDirection.sqrMagnitude < 0.0001f || toPlayer.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        Vector3 playerPosition = transform.position + toPlayer;
        int candidateCount = 0;

        for (float yaw = -140f; yaw <= 140f; yaw += TerrainDetourArcStepDegrees)
        {
            AddTerrainDetourCandidate(ref candidateCount, Quaternion.Euler(0f, yaw, 0f) * chaseDirection);
        }

        if (Time.time - lastTerrainWallNormalTime <= wallNormalMemorySeconds)
        {
            Vector3 wallTangent = Vector3.Cross(Vector3.up, lastTerrainWallNormal);
            AddTerrainDetourCandidate(ref candidateCount, wallTangent);
            AddTerrainDetourCandidate(ref candidateCount, -wallTangent);
        }

        float bestScore = float.MinValue;
        Vector3 bestDirection = Vector3.zero;
        bool found = false;

        for (int i = 0; i < candidateCount; i++)
        {
            Vector3 candidate = terrainDetourCandidates[i];
            float clearDistance = GroundHeightSampler.GetMaxWalkableClearDistance(
                transform.position,
                monsterSurfaceY,
                candidate,
                terrainAvoidMaxProbeDistance,
                terrainDetourProbeStepDistance);

            if (clearDistance < terrainAvoidMinClearDistance)
            {
                continue;
            }

            Vector3 probePosition = transform.position + candidate * clearDistance;
            float probeSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(probePosition, monsterSurfaceY);

            float score = clearDistance * terrainDetourScoreDistanceWeight;
            if (!GroundHeightSampler.IsWalkableTerrainBlockingLine(
                    probePosition,
                    probeSurfaceY,
                    playerPosition,
                    playerSurfaceY))
            {
                score += terrainDetourScoreSightBonus;
            }

            Vector3 toPlayerFromProbe = playerPosition - probePosition;
            toPlayerFromProbe.y = 0f;
            score -= toPlayerFromProbe.sqrMagnitude * terrainDetourScoreDistancePenalty;

            if (lastTerrainDetourDirection.sqrMagnitude > 0.0001f
                && Vector3.Dot(candidate, lastTerrainDetourDirection) > 0.55f)
            {
                score += 1.25f;
            }

            if (Time.time - lastTerrainWallNormalTime <= wallNormalMemorySeconds)
            {
                float wallAlignment = Mathf.Abs(Vector3.Dot(candidate, Vector3.Cross(Vector3.up, lastTerrainWallNormal)));
                score += wallAlignment * 2.5f;
            }

            int sideBias = ((GetInstanceID() >> 3) + i) & 1;
            score += sideBias * 0.05f;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = candidate;
                found = true;
            }
        }

        if (!found)
        {
            bestDirection = GetCrowdedSlideDirection(chaseDirection);
            found = bestDirection.sqrMagnitude > 0.0001f;
        }

        if (!found)
        {
            return false;
        }

        detourDirection = bestDirection.normalized;
        lastTerrainDetourDirection = detourDirection;
        return true;
    }

    void MoveWithCharacterController(Vector3 moveDirectionFlat, float distance)
    {
        if (characterController == null)
        {
            return;
        }

        Vector3 horizontal = moveDirectionFlat.sqrMagnitude > 0.0001f
            ? moveDirectionFlat.normalized * distance
            : Vector3.zero;
        Vector3 motion = horizontal + Vector3.up * verticalVelocity * Time.deltaTime;
        CollisionFlags flags = characterController.Move(motion);

        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedStickForce;
        }
    }

    // 이동하지 않아도 바닥에 붙도록 중력을 유지합니다.
    void ApplyGroundingVelocity()
    {
        if (characterController == null)
        {
            return;
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedStickForce;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void ApplyCharacterControllerSettings()
    {
        if (characterController == null)
        {
            return;
        }

        characterController.height = collisionHeight;
        characterController.radius = collisionRadius;
        characterController.center = new Vector3(0f, collisionHeight * 0.5f, 0f);
        characterController.slopeLimit = slopeLimit;
        characterController.stepOffset = stepOffset;
        characterController.skinWidth = 0.02f;
        characterController.minMoveDistance = 0f;
    }

    public void SnapToGroundOnLoad()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
            ApplyCharacterControllerSettings();
        }

        Vector3 position = transform.position;
        float fallbackY = GameSession.GroundY + WorldSettings.CharacterFeetYOffset;
        position.y = GroundHeightSampler.GetCharacterSurfaceY(position, fallbackY);

        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.position = position;
        Physics.SyncTransforms();
        characterController.enabled = wasEnabled;
        verticalVelocity = -groundedStickForce;
    }
}
