using UnityEngine;

// WASD로 캐릭터를 움직이고 SPUM 애니메이션을 바꾸는 스크립트입니다.
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float groundHeight = 0f;

    [SerializeField] float collisionRadius = 0.22f;
    [SerializeField] float collisionHeight = 1.05f;

    [SerializeField] SPUM_Prefabs spumPrefabs;
    [SerializeField] Transform spriteRoot;

    Camera mainCamera;
    SpumLocomotionAnimation locomotionAnimation;
    int lastFlipSide;
    Vector3 lastMoveDirection;

    CapsuleMotor.Settings motorSettings;

    // 스폰·AI·디스폰용 — 매 프레임 갱신되는 플레이어 월드 좌표입니다.
    public static Vector3 LastWorldCenter { get; private set; }

    public SpumLocomotionAnimation LocomotionAnimation => locomotionAnimation;
    public int LastFlipSide => lastFlipSide;

    public Vector3 GroundWorldPosition
    {
        get
        {
            if (PlayerWorldPosition.TryGetWorldCenter(groundHeight, out Vector3 center))
            {
                return center;
            }

            return ReadWorldCenterFromTransform();
        }
    }

    // 검기가 나갈 방향: 이동 입력 → 마지막 이동 → 바라보는 좌우.
    public Vector3 GetAttackDirection()
    {
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

    // 공격 시 검기 방향에 맞춰 좌우 반전합니다 (대각 포함).
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
        motorSettings = new CapsuleMotor.Settings
        {
            radius = collisionRadius,
            height = collisionHeight,
            groundY = groundHeight,
            skin = 0.015f,
            embeddedIgnoreDistance = 0.05f
        };
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

        SnapToGroundHeight();
        EnsureWorldPositionTracker();
        UpdateLastWorldCenter();
    }

    void EnsureWorldPositionTracker()
    {
        if (GetComponent<PlayerWorldPosition>() == null)
        {
            gameObject.AddComponent<PlayerWorldPosition>();
        }
    }

    void LateUpdate()
    {
        UpdateLastWorldCenter();

        PlayerWorldPosition tracker = GetComponent<PlayerWorldPosition>();
        if (tracker != null)
        {
            tracker.Refresh();
        }
    }

    void UpdateLastWorldCenter()
    {
        LastWorldCenter = ReadWorldCenterFromTransform();
    }

    void Update()
    {
        Vector3 inputDirection = ReadMoveInput();
        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);
        bool wantsToMove = moveDirection.sqrMagnitude > 0.0001f;

        if (wantsToMove)
        {
            lastMoveDirection = moveDirection.normalized;

            CapsuleMotor.Move(
                transform,
                moveDirection * moveSpeed * Time.deltaTime,
                motorSettings,
                transform,
                MoverRole.Player);

            ApplyWorldPositionAfterMove();

            Transform flipTarget = spumPrefabs != null ? spumPrefabs.transform : spriteRoot;
            SpumSpriteFlip.ApplyByMoveDirection(flipTarget, moveDirection, ref lastFlipSide);
        }

        PlayerWeaponCombat weaponCombat = GetComponent<PlayerWeaponCombat>();
        if (weaponCombat != null && weaponCombat.IsAttacking)
        {
            return;
        }

        if (locomotionAnimation != null)
        {
            locomotionAnimation.SetMoving(wantsToMove);
        }
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

    void SnapToGroundHeight()
    {
        ApplyWorldPosition(ReadWorldCenterFromTransform());
    }

    void ApplyWorldPositionAfterMove()
    {
        ApplyWorldPosition(ReadWorldCenterFromTransform());

        PlayerWorldPosition tracker = GetComponent<PlayerWorldPosition>();
        if (tracker != null)
        {
            tracker.Refresh();
        }
    }

    void ApplyWorldPosition(Vector3 worldPosition)
    {
        worldPosition.y = groundHeight;

        if (transform is RectTransform rectTransform)
        {
            rectTransform.position = worldPosition;
            return;
        }

        transform.position = worldPosition;
    }

    Vector3 ReadWorldCenterFromTransform()
    {
        Transform target = transform;
        Transform unitRoot = transform.Find("UnitRoot");
        if (unitRoot != null)
        {
            target = unitRoot;
        }

        Vector3 position = target.position;
        if (target is RectTransform rectTransform)
        {
            position = rectTransform.TransformPoint(Vector3.zero);
        }

        position.y = groundHeight;
        return position;
    }
}
