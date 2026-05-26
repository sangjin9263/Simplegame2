using UnityEngine;

// 스프라이트가 항상 카메라를 바라보게 하는 빌보드 스크립트입니다.
public class BillboardFaceCamera : MonoBehaviour
{
    // 바라볼 카메라를 저장하는 변수입니다.
    Transform cameraTransform;

    // 게임 시작 시 카메라를 찾습니다.
    void Start()
    {
        // 메인 카메라가 있으면 그 위치를 사용합니다.
        if (Camera.main != null)
        {
            // 카메라의 Transform을 변수에 넣습니다.
            cameraTransform = Camera.main.transform;
        }
    }

    // 움직임이 모두 끝난 뒤에 방향을 맞춥니다.
    void LateUpdate()
    {
        // 카메라가 없으면 아무 것도 하지 않습니다.
        if (cameraTransform == null)
        {
            // 메인 카메라를 다시 찾아 봅니다.
            if (Camera.main == null)
            {
                // 찾지 못하면 여기서 종료합니다.
                return;
            }
            // 찾았으면 변수에 저장합니다.
            cameraTransform = Camera.main.transform;
        }

        // 카메라 쪽을 향하는 방향을 구합니다.
        Vector3 lookDirection = cameraTransform.position - transform.position;
        // 위아래 기울기는 빼고 좌우만 돌리게 합니다.
        lookDirection.y = 0f;

        // 방향 길이가 거의 0이면 돌리지 않습니다.
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            // 너무 가까우면 회전을 건너뜁니다.
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        // 거의 같은 각도면 돌리지 않습니다 (덜덜 떨림 방지).
        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.25f)
        {
            return;
        }

        transform.rotation = targetRotation;
    }
}
