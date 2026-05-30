using UnityEngine;

// 플레이어 월드 좌표를 다른 시스템(스폰·AI)에 전달합니다.
public class PlayerWorldPosition : MonoBehaviour
{
    [SerializeField] Transform trackTarget;

    Vector3 worldCenter;

    public Vector3 WorldCenter => worldCenter;

    void Awake()
    {
        EnsureTrackTarget();
        worldCenter = ReadWorldCenter(trackTarget);
        if (GetComponent<CharacterController>() == null)
        {
            worldCenter.y = GroundHeightSampler.GetCharacterSurfaceY(worldCenter, GameSession.GroundY);
        }
    }

    void LateUpdate()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && movement.enabled)
        {
            return;
        }

        EnsureTrackTarget();
        worldCenter = ReadWorldCenter(trackTarget);
    }

    public void SyncFromMovement(Vector3 centerFromMovement)
    {
        worldCenter = centerFromMovement;
    }

    public static bool TryGetWorldCenter(out Vector3 center)
    {
        return GameSession.TryGetPlayerWorldCenter(out center);
    }

    void EnsureTrackTarget()
    {
        if (trackTarget != null)
        {
            return;
        }

        Transform unitRoot = transform.Find("UnitRoot");
        if (unitRoot != null)
        {
            trackTarget = unitRoot;
            return;
        }

        SPUM_Prefabs spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        if (spumPrefabs != null)
        {
            trackTarget = spumPrefabs.transform;
            return;
        }

        trackTarget = transform;
    }

    static Vector3 ReadWorldCenter(Transform target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Vector3 position = target.position;
        if (target is RectTransform rectTransform)
        {
            position = rectTransform.TransformPoint(Vector3.zero);
        }

        return position;
    }
}
