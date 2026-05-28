using UnityEngine;

// 빌드에서도 쓸 수 있게 Resources로 프리팹·스프라이트를 불러옵니다.
public static class GameAssets
{
    const string SlashVfxResourcesPath = "VFX/WhiteSlashV1";
    // 구 경로 호환 (에디터 폴백)
    const string SlashVfxEditorPathLegacy = "Assets/Weapon/1/White Slash v1.prefab";
    const string WeaponSpriteResourcesPath = "Addons/Ver300/0_Unit/0_Sprite/8_Weapons/0_Sword/New_Weapon_06";
    const string BowSpriteResourcesPath = "Addons/Ver300/0_Unit/0_Sprite/8_Weapons/3_Bow/New_Weapon_10";
    const string StaffSpriteResourcesPath = "Addons/RetroHeroes/0_Unit/0_Sprite/6_Weapons/5_Wand/Weapon_Sorcerer";
    const string ArrowPrefabResourcesPath = "Weapons/Arrow01";
    const string FireballPrefabResourcesPath = "Weapons/Projectile_Fire_Ball_Lv2";
    const string EnergyBallPrefabResourcesPath = "Weapons/Projectile_Energy_Ball_B";
    const string HitImpactPrefabResourcesPath = "Impact/Hit/Impact_Hit_Lv1";
    const string FireImpactPrefabResourcesPath = "Impact/Fire/Impact_Fire_Lv1";
    const string EnergyImpactPrefabResourcesPath = "Impact/Magic/Impact_Energy_Ball";

#if UNITY_EDITOR
    const string SlashVfxEditorPath = "Assets/Prefabs/Weapon/1/White Slash v1.prefab";
    const string WeaponSpriteEditorPath = "Assets/Prefabs/Weapon/1/New_Weapon_06.png";
    const string BowSpriteEditorPath = "Assets/Prefabs/Weapon/2/New_Weapon_10.png";
    const string StaffSpriteEditorPath = "Assets/Prefabs/Weapon/4/Weapon_Sorcerer.png";
    const string ArrowPrefabEditorPath = "Assets/Prefabs/Weapon/3/Arrow01.prefab";
    const string FireballPrefabEditorPath = "Assets/Prefabs/Weapon/4/Projectile_Fire_Ball_Lv2.prefab";
    const string EnergyBallPrefabEditorPath = "Assets/Prefabs/Weapon/5/Projectile_Energy_Ball_B.prefab";
    const string HitImpactPrefabEditorPath = "Assets/Prefabs/impact/hit/Impact_Hit_Lv1.prefab";
    const string FireImpactPrefabEditorPath = "Assets/Prefabs/impact/fire/Impact_Fire_Lv1.prefab";
    const string EnergyImpactPrefabEditorPath = "Assets/Prefabs/impact/magic/Impact_Energy_Ball.prefab";
    const string MonsterPrefabEditorFolder = "Assets/Prefabs/Char/mon";
#endif

    public static GameObject LoadSlashVfxPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(SlashVfxResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        GameObject fromEditor = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SlashVfxEditorPath);
        if (fromEditor != null)
        {
            return fromEditor;
        }

        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SlashVfxEditorPathLegacy);
#else
        return null;
#endif
    }

    public static Sprite LoadDefaultWeaponSprite(Sprite assignedSprite)
    {
        return LoadSprite(assignedSprite, WeaponSpriteResourcesPath, WeaponSpriteEditorPath);
    }

    public static Sprite LoadBowSprite(Sprite assignedSprite)
    {
        return LoadSprite(assignedSprite, BowSpriteResourcesPath, BowSpriteEditorPath);
    }

    public static Sprite LoadStaffSprite(Sprite assignedSprite)
    {
        return LoadSprite(assignedSprite, StaffSpriteResourcesPath, StaffSpriteEditorPath);
    }

    public static GameObject LoadArrowPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(ArrowPrefabResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabEditorPath);
#else
        return null;
#endif
    }

    public static GameObject LoadFireballPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(FireballPrefabResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FireballPrefabEditorPath);
#else
        return null;
#endif
    }

    public static GameObject LoadHitImpactPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(HitImpactPrefabResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(HitImpactPrefabEditorPath);
#else
        return null;
#endif
    }

    public static GameObject LoadFireImpactPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(FireImpactPrefabResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FireImpactPrefabEditorPath);
#else
        return null;
#endif
    }

    public static GameObject LoadEnergyBallPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(EnergyBallPrefabResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EnergyBallPrefabEditorPath);
#else
        return null;
#endif
    }

    public static GameObject LoadEnergyImpactPrefab(GameObject assignedPrefab)
    {
        if (assignedPrefab != null)
        {
            return assignedPrefab;
        }

        GameObject fromResources = Resources.Load<GameObject>(EnergyImpactPrefabResourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EnergyImpactPrefabEditorPath);
#else
        return null;
#endif
    }

    static Sprite LoadFirstSprite(string resourcesPath)
    {
        if (string.IsNullOrEmpty(resourcesPath))
        {
            return null;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0];
        }

        return Resources.Load<Sprite>(resourcesPath);
    }

    static Sprite LoadSprite(Sprite assignedSprite, string resourcesPath, string editorPath)
    {
        if (assignedSprite != null)
        {
            return assignedSprite;
        }

        Sprite fromResources = LoadFirstSprite(resourcesPath);
        if (fromResources != null)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(editorPath);
#else
        return null;
#endif
    }

    public static GameObject[] LoadDefaultMonsterPrefabs(GameObject[] assignedPrefabs)
    {
        if (assignedPrefabs != null && HasAnyPrefab(assignedPrefabs))
        {
            return MergeMonsterPrefabs(assignedPrefabs);
        }

        GameObject[] fromResources = Resources.LoadAll<GameObject>("Monsters");
        if (fromResources != null && fromResources.Length > 0)
        {
            return fromResources;
        }

#if UNITY_EDITOR
        string[] paths =
        {
            MonsterPrefabEditorFolder + "/SPUM_orc_m1.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m2.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m3.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m4.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m5.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m6.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m7.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m8.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m9.prefab"
        };

        GameObject[] loaded = new GameObject[paths.Length];
        int count = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab == null)
            {
                continue;
            }

            loaded[count] = prefab;
            count++;
        }

        if (count == 0)
        {
            return assignedPrefabs;
        }

        if (count == loaded.Length)
        {
            return loaded;
        }

        GameObject[] trimmed = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            trimmed[i] = loaded[i];
        }

        return trimmed;
#endif
        return assignedPrefabs;
    }

    static GameObject[] MergeMonsterPrefabs(GameObject[] assignedPrefabs)
    {
#if UNITY_EDITOR
        string[] paths =
        {
            MonsterPrefabEditorFolder + "/SPUM_orc_m7.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m8.prefab",
            MonsterPrefabEditorFolder + "/SPUM_orc_m9.prefab"
        };

        int extraCount = 0;
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject extra = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (extra == null || ContainsPrefab(assignedPrefabs, extra))
            {
                continue;
            }

            extraCount++;
        }

        if (extraCount == 0)
        {
            return assignedPrefabs;
        }

        GameObject[] merged = new GameObject[assignedPrefabs.Length + extraCount];
        for (int i = 0; i < assignedPrefabs.Length; i++)
        {
            merged[i] = assignedPrefabs[i];
        }

        int index = assignedPrefabs.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject extra = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (extra == null || ContainsPrefab(merged, extra))
            {
                continue;
            }

            merged[index] = extra;
            index++;
        }

        return merged;
#else
        return assignedPrefabs;
#endif
    }

    static bool HasAnyPrefab(GameObject[] prefabs)
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsPrefab(GameObject[] prefabs, GameObject target)
    {
        if (prefabs == null || target == null)
        {
            return false;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == target)
            {
                return true;
            }
        }

        return false;
    }
}
