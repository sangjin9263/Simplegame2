using UnityEngine;

// SPUM 캐릭터 손에 무기 스프라이트를 붙입니다.
public static class SpumWeaponEquip
{
    public static bool TryEquip(SPUM_Prefabs spumPrefabs, Sprite weaponSprite)
    {
        if (spumPrefabs == null || weaponSprite == null)
        {
            return false;
        }

        SpriteRenderer weaponRenderer = FindWeaponSpriteRenderer(spumPrefabs.transform);
        if (weaponRenderer == null)
        {
            return false;
        }

        weaponRenderer.sprite = weaponSprite;
        weaponRenderer.enabled = true;
        weaponRenderer.gameObject.SetActive(true);

        Transform weaponRoot = weaponRenderer.transform.parent;
        if (weaponRoot != null)
        {
            weaponRoot.gameObject.SetActive(true);
        }

        return true;
    }

    public static void Clear(SPUM_Prefabs spumPrefabs)
    {
        if (spumPrefabs == null)
        {
            return;
        }

        SpriteRenderer weaponRenderer = FindWeaponSpriteRenderer(spumPrefabs.transform);
        if (weaponRenderer == null)
        {
            return;
        }

        weaponRenderer.sprite = null;
        weaponRenderer.enabled = false;
    }

    static SpriteRenderer FindWeaponSpriteRenderer(Transform root)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].gameObject.name.Contains("Weapon"))
            {
                return renderers[i];
            }
        }

        return null;
    }
}
