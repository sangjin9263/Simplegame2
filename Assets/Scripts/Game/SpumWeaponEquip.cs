using UnityEngine;

public enum SpumWeaponVisualKind
{
    Melee,
    Bow,
    Staff
}

// 픽업 시 Player 하위의 R_Weapon/L_Weapon 스프라이트를 직접 바꿉니다.
public static class SpumWeaponEquip
{
    const int WeaponSortingOrder = 32;

    public static bool TryEquip(SPUM_Prefabs spumPrefabs, Sprite weaponSprite, SpumWeaponVisualKind visualKind)
    {
        if (weaponSprite == null || !TryResolveSpumPrefabs(ref spumPrefabs))
        {
            return false;
        }

        Transform root = spumPrefabs.transform;
        SpriteRenderer rightWeapon = FindWeaponRenderer(root, "R_Weapon");
        SpriteRenderer leftWeapon = FindWeaponRenderer(root, "L_Weapon");
        if (rightWeapon == null)
        {
            // 일부 프리팹은 SPUM_Prefabs가 상위에 있고 무기 슬롯이 다른 자식 트리에 있습니다.
            rightWeapon = FindWeaponRenderer(spumPrefabs.transform.root, "R_Weapon");
            leftWeapon = FindWeaponRenderer(spumPrefabs.transform.root, "L_Weapon");
        }
        if (rightWeapon == null)
        {
            return false;
        }

        switch (visualKind)
        {
            case SpumWeaponVisualKind.Bow:
                ApplySprite(leftWeapon, weaponSprite);
                ApplySprite(rightWeapon, weaponSprite);
                break;
            case SpumWeaponVisualKind.Staff:
                ClearSprite(leftWeapon);
                ApplySprite(rightWeapon, weaponSprite);
                break;
            default:
                ClearSprite(leftWeapon);
                ApplySprite(rightWeapon, weaponSprite);
                break;
        }

        return true;
    }

    public static bool TryEquip(SPUM_Prefabs spumPrefabs, Sprite weaponSprite)
    {
        return TryEquip(spumPrefabs, weaponSprite, SpumWeaponVisualKind.Melee);
    }

    public static void Clear(SPUM_Prefabs spumPrefabs)
    {
        if (!TryResolveSpumPrefabs(ref spumPrefabs))
        {
            return;
        }

        Transform root = spumPrefabs.transform;
        ClearSprite(FindWeaponRenderer(root, "L_Weapon"));
        ClearSprite(FindWeaponRenderer(root, "R_Weapon"));
    }

    static bool TryResolveSpumPrefabs(ref SPUM_Prefabs spumPrefabs)
    {
        if (spumPrefabs != null)
        {
            return true;
        }

        if (!GameSession.TryGetPlayerTransform(out Transform player))
        {
            return false;
        }

        spumPrefabs = player.GetComponent<SPUM_Prefabs>();
        if (spumPrefabs == null)
        {
            spumPrefabs = player.GetComponentInChildren<SPUM_Prefabs>(true);
        }

        return spumPrefabs != null;
    }

    static SpriteRenderer FindWeaponRenderer(Transform root, string nodeName)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].gameObject.name == nodeName)
            {
                return renderers[i];
            }
        }

        return null;
    }

    static void ApplySprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sprite = sprite;
        renderer.enabled = sprite != null;
        renderer.sortingOrder = WeaponSortingOrder;
        renderer.color = Color.white;
        renderer.gameObject.SetActive(true);
        if (renderer.transform.parent != null)
        {
            renderer.transform.parent.gameObject.SetActive(true);
        }
        if (renderer.transform.parent != null && renderer.transform.parent.parent != null)
        {
            renderer.transform.parent.parent.gameObject.SetActive(true);
        }
    }

    static void ClearSprite(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sprite = null;
        renderer.enabled = false;
    }
}
