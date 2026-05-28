using UnityEngine;

// 바닥에 떨어진 무기입니다. 플레이어가 닿으면 자동으로 줍습니다.
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] WeaponPickupKind pickupKind = WeaponPickupKind.Melee;
    [SerializeField] Sprite weaponSprite;
    [SerializeField] float pickupRadius = 0.75f;
    [SerializeField] float groundHeight = 0f;
    [SerializeField] float billboardHeight = 0.4f;
    [SerializeField] float worldDisplayScale = 3f;

    SpriteRenderer worldSprite;
    bool pickedUp;

    public Sprite WeaponSprite => weaponSprite;
    public WeaponPickupKind PickupKind => pickupKind;

    public void Configure(WeaponPickupKind kind, Sprite sprite, float height)
    {
        pickupKind = kind;
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

        if (!GameSession.TryGetPlayerWorldCenter(out Vector3 playerPosition))
        {
            return;
        }

        Vector3 flat = playerPosition - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > pickupRadius * pickupRadius)
        {
            return;
        }

        if (TryPickupWeapon())
        {
            pickedUp = true;
            Destroy(gameObject);
        }
    }

    bool TryPickupWeapon()
    {
        if (pickupKind == WeaponPickupKind.Bow)
        {
            PlayerRangedCombat rangedCombat = GameSession.PlayerRangedCombat;
            if (rangedCombat == null || !rangedCombat.TryEquipBow(weaponSprite))
            {
                return false;
            }

            PlayerWeaponCombat meleeCombat = GameSession.PlayerWeaponCombat;
            if (meleeCombat != null)
            {
                meleeCombat.UnequipWeapon();
            }

            PlayerMagicCombat magicCombat = GameSession.PlayerMagicCombat;
            if (magicCombat != null)
            {
                magicCombat.UnequipStaff();
            }

            return true;
        }

        if (pickupKind == WeaponPickupKind.Staff)
        {
            PlayerMagicCombat magicCombat = GameSession.PlayerMagicCombat;
            if (magicCombat == null || !magicCombat.TryEquipStaff(weaponSprite))
            {
                return false;
            }

            PlayerWeaponCombat meleeCombat = GameSession.PlayerWeaponCombat;
            if (meleeCombat != null)
            {
                meleeCombat.UnequipWeapon();
            }

            PlayerRangedCombat rangedCombat = GameSession.PlayerRangedCombat;
            if (rangedCombat != null)
            {
                rangedCombat.UnequipBow();
            }

            return true;
        }

        PlayerWeaponCombat weaponCombat = GameSession.PlayerWeaponCombat;
        if (weaponCombat == null || !weaponCombat.TryEquipWeapon(weaponSprite))
        {
            return false;
        }

        PlayerRangedCombat ranged = GameSession.PlayerRangedCombat;
        if (ranged != null)
        {
            ranged.UnequipBow();
        }

        PlayerMagicCombat magic = GameSession.PlayerMagicCombat;
        if (magic != null)
        {
            magic.UnequipStaff();
        }

        return true;
    }
}

