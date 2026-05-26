using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 중심 레이더 미니맵 — 주변 몬스터 위치만 표시합니다.
public class RadarMinimapView : MonoBehaviour
{
    [SerializeField] RectTransform blipsRoot;
    [SerializeField] RectTransform playerBlip;
    [SerializeField] float worldRange = 36f;
    [SerializeField] float mapRadius = 72f;
    [SerializeField] float monsterBlipSize = 7f;
    [SerializeField] int maxMonsterBlips = 96;
    [SerializeField] float monsterScanInterval = 0.2f;
    [SerializeField] Color monsterBlipColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] bool alignToCameraView = true;

    readonly List<RectTransform> monsterBlipPool = new List<RectTransform>();
    Transform playerTransform;
    Camera viewCamera;
    MonsterHealth[] scannedMonsters = System.Array.Empty<MonsterHealth>();
    float nextMonsterScanTime;
    bool poolReady;

    public void Configure(RectTransform blipsContainer, RectTransform centerPlayerBlip)
    {
        blipsRoot = blipsContainer;
        playerBlip = centerPlayerBlip;
        EnsureBlipPool();
    }

    void Start()
    {
        EnsureBlipPool();
        BindPlayer();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!BindPlayer())
        {
            HideAllMonsterBlips();
            return;
        }

        if (Time.unscaledTime >= nextMonsterScanTime)
        {
            scannedMonsters = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
            nextMonsterScanTime = Time.unscaledTime + monsterScanInterval;
        }

        RefreshMonsterBlips();
    }

    bool BindPlayer()
    {
        if (playerTransform != null)
        {
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

    void RefreshMonsterBlips()
    {
        EnsureBlipPool();

        Vector3 playerPosition = GetPlayerWorldCenter();
        int visibleCount = 0;

        for (int i = 0; i < scannedMonsters.Length; i++)
        {
            MonsterHealth monster = scannedMonsters[i];
            if (monster == null || monster.IsDead)
            {
                continue;
            }

            if (!TryGetMapPosition(playerPosition, GetMonsterWorldCenter(monster), out Vector2 mapPosition))
            {
                continue;
            }

            if (visibleCount >= monsterBlipPool.Count)
            {
                break;
            }

            RectTransform blip = monsterBlipPool[visibleCount];
            blip.gameObject.SetActive(true);
            blip.anchoredPosition = mapPosition;
            visibleCount++;
        }

        for (int i = visibleCount; i < monsterBlipPool.Count; i++)
        {
            monsterBlipPool[i].gameObject.SetActive(false);
        }

        if (playerBlip != null)
        {
            playerBlip.SetAsLastSibling();
        }
    }

    Vector3 GetPlayerWorldCenter()
    {
        PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            return movement.GroundWorldPosition;
        }

        return playerTransform.position;
    }

    static Vector3 GetMonsterWorldCenter(MonsterHealth monster)
    {
        Vector3 position = monster.transform.position;
        position.y = 0f;
        return position;
    }

    bool TryGetMapPosition(Vector3 playerPosition, Vector3 worldPosition, out Vector2 mapPosition)
    {
        Vector3 offset = worldPosition - playerPosition;
        offset.y = 0f;

        float range = Mathf.Max(1f, worldRange);
        float localRight;
        float localForward;

        if (alignToCameraView && TryGetViewAxes(out Vector3 viewRight, out Vector3 viewForward))
        {
            localRight = Vector3.Dot(offset, viewRight);
            localForward = Vector3.Dot(offset, viewForward);
        }
        else
        {
            localRight = offset.x;
            localForward = offset.z;
        }

        mapPosition = new Vector2(
            localRight / range * mapRadius,
            localForward / range * mapRadius);

        float distance = mapPosition.magnitude;
        if (distance > mapRadius)
        {
            mapPosition = mapPosition / distance * mapRadius;
        }

        return true;
    }

    bool TryGetViewAxes(out Vector3 viewRight, out Vector3 viewForward)
    {
        viewRight = Vector3.right;
        viewForward = Vector3.forward;

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (viewCamera == null)
        {
            return false;
        }

        viewRight = viewCamera.transform.right;
        viewRight.y = 0f;
        if (viewRight.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        viewRight.Normalize();

        // 화면 위쪽(플레이어 앞쪽) = 카메라가 바라보는 방향(지면 투영).
        viewForward = viewCamera.transform.forward;
        viewForward.y = 0f;
        if (viewForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        viewForward.Normalize();
        return true;
    }

    void EnsureBlipPool()
    {
        if (poolReady || blipsRoot == null)
        {
            return;
        }

        for (int i = 0; i < maxMonsterBlips; i++)
        {
            monsterBlipPool.Add(CreateMonsterBlip(i));
        }

        poolReady = true;
    }

    RectTransform CreateMonsterBlip(int index)
    {
        GameObject blipObject = new GameObject("MonsterBlip_" + index, typeof(RectTransform), typeof(Image));
        blipObject.transform.SetParent(blipsRoot, false);

        RectTransform rect = blipObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(monsterBlipSize, monsterBlipSize);
        rect.anchoredPosition = Vector2.zero;

        Image image = blipObject.GetComponent<Image>();
        image.sprite = HudUiSprites.White;
        image.color = monsterBlipColor;
        image.raycastTarget = false;

        blipObject.SetActive(false);
        return rect;
    }

    void HideAllMonsterBlips()
    {
        for (int i = 0; i < monsterBlipPool.Count; i++)
        {
            if (monsterBlipPool[i] != null)
            {
                monsterBlipPool[i].gameObject.SetActive(false);
            }
        }
    }
}
