using UnityEngine;

// 바닥에 떨어진 무기입니다. 플레이어가 닿으면 자동으로 줍습니다.
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] Sprite weaponSprite;
    [SerializeField] float pickupRadius = 0.75f;
    [SerializeField] float groundHeight = 0f;
    [SerializeField] float billboardHeight = 0.4f;

    // 맵에 보이도록 스프라이트 크기 배율입니다.
    [SerializeField] float worldDisplayScale = 3f;

    SpriteRenderer worldSprite;
    bool pickedUp;

    public Sprite WeaponSprite => weaponSprite;

    public void Configure(Sprite sprite, float height)
    {
        weaponSprite = sprite;
        groundHeight = height;
        BuildWorldVisual();
    }

    void Start()
    {
        BuildWorldVisual();
    }

    void BuildWorldVisual()
    {
        if (weaponSprite == null)
        {
            return;
        }

        if (worldSprite == null)
        {
            GameObject visualObject = new GameObject("WeaponVisual");
            visualObject.transform.SetParent(transform, false);
            worldSprite = visualObject.AddComponent<SpriteRenderer>();
            worldSprite.sortingOrder = 20;

            if (visualObject.GetComponent<BillboardFaceCamera>() == null)
            {
                visualObject.AddComponent<BillboardFaceCamera>();
            }
        }

        worldSprite.sprite = weaponSprite;
        worldSprite.transform.localPosition = new Vector3(0f, billboardHeight, 0f);
        worldSprite.transform.localScale = Vector3.one * worldDisplayScale;
        worldSprite.sortingOrder = 30;
    }

    void Update()
    {
        if (pickedUp)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return;
        }

        Vector3 playerPosition = playerObject.transform.position;
        Vector3 pickupPosition = transform.position;
        Vector3 flat = playerPosition - pickupPosition;
        flat.y = 0f;

        if (flat.sqrMagnitude > pickupRadius * pickupRadius)
        {
            return;
        }

        PlayerWeaponCombat weaponCombat = playerObject.GetComponent<PlayerWeaponCombat>();
        if (weaponCombat == null)
        {
            return;
        }

        if (!weaponCombat.TryEquipWeapon(weaponSprite))
        {
            return;
        }

        pickedUp = true;
        Destroy(gameObject);
    }
}
