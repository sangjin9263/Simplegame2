using UnityEngine;

// WASD + CharacterController.Move 로 이동합니다 (지형·나무는 물리 캡슐이 처리).
[RequireComponent(typeof(PlayerWorldPosition))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -24f;
    [SerializeField] float groundedStickForce = 2f;

    [SerializeField] float collisionRadius = 0.22f;
    [SerializeField] float collisionHeight = 1.05f;
    [SerializeField] float slopeLimit = 50f;
    [SerializeField] float stepOffset = 0.4f;

    [Header("공격 방향")]
    [SerializeField] float attackAutoAimRange = 14f;
    [SerializeField] float attackAutoAimMaxHeightDelta = 1.2f;

    [SerializeField] SPUM_Prefabs spumPrefabs;
    [SerializeField] Transform spriteRoot;

    Camera mainCamera;
    SpumLocomotionAnimation locomotionAnimation;
    CharacterController characterController;
    int lastFlipSide;
    Vector3 lastMoveDirection;
    Vector3 worldCenter;
    float verticalVelocity;

    PlayerWorldPosition worldPositionTracker;
    PlayerWeaponCombat weaponCombat;

    public Vector3 WorldCenter => worldCenter;
    public SpumLocomotionAnimation LocomotionAnimation => locomotionAnimation;
    public int LastFlipSide => lastFlipSide;

    public Vector3 GroundWorldPosition => worldCenter;

    public Vector3 GetAttackDirection()
    {
        if (TryGetAutoAimAttackDirection(out Vector3 autoAimDirection))
        {
            lastMoveDirection = autoAimDirection;
            return autoAimDirection;
        }

        Vector3 inputDirection = ReadMoveInput();
        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = moveDirection.normalized;
            return lastMoveDirection;
        }

        if (lastMoveDirection.sqrMagnitude > 0.0001f)
        {
            return lastMoveDirection;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null && lastFlipSide != 0)
        {
            Vector3 cameraRight = mainCamera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
            return (lastFlipSide > 0 ? cameraRight : -cameraRight).normalized;
        }

        if (mainCamera != null)
        {
            Vector3 intoScreen = mainCamera.transform.forward;
            intoScreen.y = 0f;
            if (intoScreen.sqrMagnitude > 0.0001f)
            {
                return intoScreen.normalized;
            }
        }

        return transform.forward;
    }

    bool TryGetAutoAimAttackDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (attackAutoAimRange <= 0f)
        {
            return false;
        }

        Vector3 origin = worldCenter;
        if (origin.sqrMagnitude < 0.0001f)
        {
            origin = transform.position;
        }

        float referenceSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(origin, GameSession.GroundY);
        return MonsterRegistry.TryGetNearestAliveFlatDirection(
            origin,
            attackAutoAimRange,
            attackAutoAimMaxHeightDelta,
            referenceSurfaceY,
            out direction);
    }

    public void ApplyFacingForDirection(Vector3 facingDirection)
    {
        Transform flipTarget = spumPrefabs != null ? spumPrefabs.transform : spriteRoot;
        SpumSpriteFlip.ApplyByFacingDirection(flipTarget, facingDirection, ref lastFlipSide);
    }

    public bool IsWantingToMove()
    {
        Vector3 inputDirection = ReadMoveInput();
        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);
        return moveDirection.sqrMagnitude > 0.0001f;
    }

    void Awake()
    {
        GameSession.RegisterPlayer(this);
        worldPositionTracker = GetComponent<PlayerWorldPosition>();
        weaponCombat = GetComponent<PlayerWeaponCombat>();
        characterController = GetComponent<CharacterController>();
        ApplyCharacterControllerSettings();
    }

    void Start()
    {
        mainCamera = Camera.main;

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        if (spumPrefabs != null)
        {
            locomotionAnimation = new SpumLocomotionAnimation(spumPrefabs);
            locomotionAnimation.Initialize();
        }

        if (spriteRoot == null)
        {
            Transform unitRoot = transform.Find("UnitRoot");
            if (unitRoot != null)
            {
                spriteRoot = unitRoot;
            }
        }

        SnapToGroundOnLoad();
        RefreshWorldCenter();
    }

    void LateUpdate()
    {
        RefreshWorldCenter();

        if (worldPositionTracker != null)
        {
            worldPositionTracker.SyncFromMovement(worldCenter);
        }
    }

    void RefreshWorldCenter()
    {
        worldCenter = transform.position;
    }

    void Update()
    {
        Vector3 inputDirection = ReadMoveInput();
        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);
        bool wantsToMove = moveDirection.sqrMagnitude > 0.0001f;

        Vector3 moveDirectionFlat = moveDirection;
        moveDirectionFlat.y = 0f;

        if (wantsToMove)
        {
            lastMoveDirection = moveDirectionFlat.normalized;

            Transform flipTarget = spumPrefabs != null ? spumPrefabs.transform : spriteRoot;
            SpumSpriteFlip.ApplyByMoveDirection(flipTarget, moveDirectionFlat, ref lastFlipSide);
        }

        ApplyCharacterControllerMove(moveDirectionFlat);

        if (weaponCombat != null && weaponCombat.IsAttacking)
        {
            return;
        }

        if (locomotionAnimation != null)
        {
            locomotionAnimation.SetMoving(wantsToMove);
        }
    }

    // 월드 로딩 직후·게이트 해제 시 발 위치를 지면에 맞춥니다.
    public void SnapToGroundOnLoad()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
            ApplyCharacterControllerSettings();
        }

        Vector3 position = transform.position;
        if (TryFindGroundY(position, out float groundY))
        {
            position.y = groundY;
        }

        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        transform.position = position;
        Physics.SyncTransforms();
        characterController.enabled = wasEnabled;
        verticalVelocity = 0f;
    }

    void ApplyCharacterControllerMove(Vector3 moveDirectionFlat)
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

        Vector3 horizontal = moveDirectionFlat * moveSpeed;
        Vector3 motion = (horizontal + Vector3.up * verticalVelocity) * Time.deltaTime;
        characterController.Move(motion);
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

    static bool TryFindGroundY(Vector3 worldPosition, out float groundY)
    {
        groundY = worldPosition.y;
        int mask = GroundHeightSampler.GroundLayerMask;
        if (mask == 0)
        {
            return GroundHeightSampler.TryGetSurfaceY(worldPosition, GameSession.GroundY, out groundY);
        }

        Vector3 origin = worldPosition + Vector3.up * 4f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 12f, mask, QueryTriggerInteraction.Ignore)
            && GroundHeightSampler.IsWalkableGroundCollider(hit.collider))
        {
            groundY = hit.point.y;
            return true;
        }

        return GroundHeightSampler.TryGetSurfaceY(worldPosition, GameSession.GroundY, out groundY);
    }

    Vector3 ReadMoveInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            vertical -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1f;
        }

        return new Vector3(horizontal, 0f, vertical);
    }

    Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
    {
        if (inputDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera == null)
        {
            return inputDirection;
        }

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = mainCamera.transform.right;
        right.y = 0f;
        right.Normalize();

        return forward * inputDirection.z + right * inputDirection.x;
    }
}
