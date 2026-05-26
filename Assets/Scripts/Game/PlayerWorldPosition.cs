using UnityEngine;

// 플레이어의 실제 월드 좌표(스폰·AI 기준)를 매 프레임 갱신합니다.
public class PlayerWorldPosition : MonoBehaviour
{
    public static PlayerWorldPosition Instance { get; private set; }

    [SerializeField] float groundHeight = 0f;
    [SerializeField] Transform trackTarget;

    Vector3 worldCenter;

    public Vector3 WorldCenter => worldCenter;

    void Awake()
    {
        Instance = this;
        EnsureTrackTarget();
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void LateUpdate()
    {
        Refresh();
    }

    public void Refresh()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            worldCenter = PlayerMovement.LastWorldCenter;
            worldCenter.y = groundHeight;
            return;
        }

        EnsureTrackTarget();
        worldCenter = ReadWorldCenter(trackTarget);
        worldCenter.y = groundHeight;
    }

    public static bool TryGetWorldCenter(float groundHeight, out Vector3 center)
    {
        if (Instance == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
            if (playerObject != null)
            {
                Instance = playerObject.GetComponent<PlayerWorldPosition>();
                if (Instance == null)
                {
                    Instance = playerObject.AddComponent<PlayerWorldPosition>();
                }
            }
        }

        if (Instance == null)
        {
            center = Vector3.zero;
            return false;
        }

        Instance.groundHeight = groundHeight;
        Instance.Refresh();
        center = Instance.worldCenter;
        center.y = groundHeight;
        return true;
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
