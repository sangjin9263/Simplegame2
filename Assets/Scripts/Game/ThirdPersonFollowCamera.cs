using UnityEngine;

// 마우스로 시점을 돌리며 플레이어 뒤에서 따라가는 3인칭 카메라입니다.
public class ThirdPersonFollowCamera : MonoBehaviour
{
    // 따라갈 플레이어 Transform입니다.
    [SerializeField] Transform followTarget;

    // 플레이어와 카메라 사이 거리입니다.
    [SerializeField] float distance = 8f;

    // 좌우 회전(도)을 저장합니다. 마우스 X로 바뀝니다.
    [SerializeField] float yawAngle = 0f;

    // 위아래 각도(도)를 저장합니다. 마우스 Y로 바뀝니다 (상하 허용 시).
    [SerializeField] float pitchAngle = 48f;

    // true면 마우스 상하는 무시하고 pitch를 고정합니다.
    [SerializeField] bool lockVerticalRotation = true;

    // 상하 고정 시 사용할 고정 각도(도)입니다.
    [SerializeField] float fixedPitchAngle = 48f;

    // 플레이 중 옵션을 켜고 끄는 키입니다.
    [SerializeField] KeyCode toggleVerticalLockKey = KeyCode.F1;

    // 플레이 중 키로 토글할지 여부입니다.
    [SerializeField] bool allowRuntimeToggle = true;

    // 마우스 좌우 감도입니다.
    [SerializeField] float mouseSensitivityX = 3f;

    // 마우스 위아래 감도입니다 (상하 허용 시만 사용).
    [SerializeField] float mouseSensitivityY = 2f;

    // 위로 올릴 수 있는 최대 각도입니다.
    [SerializeField] float minPitch = 25f;

    // 아래로 내릴 수 있는 최소 각도입니다.
    [SerializeField] float maxPitch = 70f;

    // 플레이 시 마우스 커서를 숨길지 여부입니다.
    [SerializeField] bool lockCursorOnPlay = true;

    // 사용할 카메라입니다.
    [SerializeField] Camera targetCamera;

    // 커서가 잠겼는지 저장합니다.
    bool isCursorLocked;

    // 게임 시작 시 플레이어 연결과 커서 설정을 합니다.
    void Start()
    {
        // 카메라가 플레이어 자식이면 분리합니다.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        // followTarget이 비어 있으면 Player를 찾습니다.
        if (followTarget == null && GameSession.TryGetPlayerTransform(out Transform player))
        {
            followTarget = player;
        }

        // 상하 고정이 켜져 있으면 pitch를 고정값으로 맞춥니다.
        ApplyVerticalLockState();

        // 커서 잠금을 켭니다.
        if (lockCursorOnPlay)
        {
            LockCursor();
        }
    }

    // 매 프레임 마우스 입력으로 각도를 바꿉니다.
    void Update()
    {
        // 테스트용: 키로 상하 고정 옵션을 켜고 끕니다.
        if (allowRuntimeToggle && Input.GetKeyDown(toggleVerticalLockKey))
        {
            lockVerticalRotation = !lockVerticalRotation;
            ApplyVerticalLockState();
            Debug.Log($"[카메라] 상하 고정: {(lockVerticalRotation ? "켜짐 (좌우만)" : "꺼짐 (상하+좌우)")}");
        }

        // Esc 키로 커서 잠금을 풉니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // 마우스 왼쪽 클릭 시 다시 잠급니다.
        if (Input.GetMouseButtonDown(0) && !isCursorLocked)
        {
            LockCursor();
        }

        // 커서가 잠겨 있을 때만 시점을 돌립니다.
        if (isCursorLocked)
        {
            RotateViewWithMouse();
        }
    }

    // 플레이어 위치에 맞춰 카메라를 배치합니다.
    void LateUpdate()
    {
        // 따라갈 대상이 없으면 Player를 다시 찾습니다.
        if (followTarget == null && GameSession.TryGetPlayerTransform(out Transform player))
        {
            followTarget = player;
        }

        // 대상이 없으면 종료합니다.
        if (followTarget == null)
        {
            return;
        }

        // 카메라 Transform을 준비합니다.
        Transform cameraTransform = GetCameraTransform();
        if (cameraTransform == null)
        {
            return;
        }

        // 상하 고정이면 매 프레임 pitch를 고정값으로 유지합니다.
        if (lockVerticalRotation)
        {
            pitchAngle = fixedPitchAngle;
        }

        // yaw와 pitch로 플레이어 주위 위치를 계산합니다.
        Quaternion orbitRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        Vector3 offset = orbitRotation * new Vector3(0f, 0f, -distance);
        cameraTransform.position = followTarget.position + offset;

        // 플레이어를 바라보게 회전합니다.
        Vector3 lookDirection = followTarget.position - cameraTransform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            cameraTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }

    // 마우스 이동량만큼 yaw/pitch 각도를 바꿉니다.
    void RotateViewWithMouse()
    {
        // 마우스 X/Y 이동 값을 가져옵니다.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 좌우로 돌리면 yaw가 변합니다.
        yawAngle += mouseX * mouseSensitivityX;

        // 상하 고정이 꺼져 있을 때만 pitch를 바꿉니다.
        if (!lockVerticalRotation)
        {
            pitchAngle -= mouseY * mouseSensitivityY;
            pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
        }
    }

    // 상하 고정 옵션 상태에 맞게 pitch를 적용합니다.
    void ApplyVerticalLockState()
    {
        // 고정이 켜지면 인스펙터의 fixedPitchAngle을 사용합니다.
        if (lockVerticalRotation)
        {
            pitchAngle = fixedPitchAngle;
        }
    }

    // 마우스 커서를 숨기고 잠급니다.
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isCursorLocked = true;
    }

    // 마우스 커서 잠금을 해제합니다.
    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isCursorLocked = false;
    }

    // 카메라 Transform을 가져옵니다.
    Transform GetCameraTransform()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        return targetCamera != null ? targetCamera.transform : transform;
    }
}
