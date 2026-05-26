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
        BindReferencesIfNeeded();
    }

    void BindReferencesIfNeeded()
    {
        if (blipsRoot != null && playerBlip != null)
        {
            return;
        }

        Transform mapClipBlips = transform.Find("MapClip/Blips");
        if (mapClipBlips != null)
        {
            blipsRoot ??= mapClipBlips as RectTransform;
            playerBlip ??= mapClipBlips.Find("PlayerBlip") as RectTransform;
            return;
        }

        Transform legacyBlips = transform.Find("Blips");
        if (legacyBlips != null)
        {
            blipsRoot ??= legacyBlips as RectTransform;
            playerBlip ??= legacyBlips.Find("PlayerBlip") as RectTransform;
        }
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

    const string MapClipName = "MapClip";
    const string BlipsName = "Blips";
    const float CircleInnerPadding = 6f;

    public static void ApplyCircleLayout(Transform minimapRoot)
    {
        if (minimapRoot == null)
        {
            return;
        }

        Sprite circleSprite = HudUiSprites.Circle;
        if (circleSprite == null)
        {
            return;
        }

        RectTransform panel = minimapRoot as RectTransform;
        if (panel == null)
        {
            return;
        }

        float diameter = panel.rect.height > 1f ? panel.rect.height : panel.sizeDelta.y;
        if (diameter <= 1f)
        {
            diameter = 156f;
        }

        float innerDiameter = Mathf.Max(32f, diameter - CircleInnerPadding);

        Transform ring = panel.Find("Ring");
        if (ring != null)
        {
            SetupCircleRing(ring, circleSprite);
        }

        Transform legacyBackground = panel.Find("Background");
        if (legacyBackground != null)
        {
            legacyBackground.gameObject.SetActive(false);
        }

        Transform mapClip = panel.Find(MapClipName);
        if (mapClip == null)
        {
            mapClip = CreateCircleMapClip(panel, circleSprite, innerDiameter).transform;
        }
        else
        {
            SetupCircleMapClip(mapClip, circleSprite, innerDiameter);
        }

        Transform blips = panel.Find(BlipsName);
        if (blips == null)
        {
            blips = mapClip.Find(BlipsName);
        }

        if (blips != null && blips.parent != mapClip)
        {
            blips.SetParent(mapClip, false);
            StretchBlipsRect(blips as RectTransform);
        }

        if (blips == null)
        {
            GameObject blipsObject = new GameObject(BlipsName, typeof(RectTransform));
            blipsObject.transform.SetParent(mapClip, false);
            StretchBlipsRect(blipsObject.GetComponent<RectTransform>());
        }

        RadarMinimapView view = minimapRoot.GetComponent<RadarMinimapView>();
        if (view != null)
        {
            RectTransform blipsRoot = minimapRoot.Find(MapClipName + "/" + BlipsName) as RectTransform;
            RectTransform playerBlipRect = blipsRoot != null
                ? blipsRoot.Find("PlayerBlip") as RectTransform
                : null;
            if (blipsRoot != null && playerBlipRect != null)
            {
                view.Configure(blipsRoot, playerBlipRect);
            }
        }
    }

    static void SetupCircleRing(Transform ring, Sprite circleSprite)
    {
        Image image = ring.GetComponent<Image>();
        if (image == null)
        {
            image = ring.gameObject.AddComponent<Image>();
        }

        image.sprite = circleSprite;
        image.color = new Color(0.55f, 0.58f, 0.62f, 0.95f);
        image.raycastTarget = false;

        RectTransform rect = ring as RectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        ring.SetAsFirstSibling();
    }

    static GameObject CreateCircleMapClip(RectTransform panel, Sprite circleSprite, float innerDiameter)
    {
        GameObject clipObject = new GameObject(MapClipName, typeof(RectTransform), typeof(Image), typeof(Mask));
        clipObject.transform.SetParent(panel, false);
        SetupCircleMapClip(clipObject.transform, circleSprite, innerDiameter);
        return clipObject;
    }

    static void SetupCircleMapClip(Transform mapClip, Sprite circleSprite, float innerDiameter)
    {
        RectTransform rect = mapClip as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(innerDiameter, innerDiameter);
        rect.anchoredPosition = Vector2.zero;

        Image image = mapClip.GetComponent<Image>();
        if (image == null)
        {
            image = mapClip.gameObject.AddComponent<Image>();
        }

        image.sprite = circleSprite;
        image.color = new Color(0.02f, 0.02f, 0.03f, 0.92f);
        image.raycastTarget = false;

        Mask mask = mapClip.GetComponent<Mask>();
        if (mask == null)
        {
            mask = mapClip.gameObject.AddComponent<Mask>();
        }

        mask.showMaskGraphic = true;
        mapClip.SetSiblingIndex(1);
    }

    static void StretchBlipsRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
