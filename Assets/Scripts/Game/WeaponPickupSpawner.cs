using System.Collections;
using UnityEngine;

// 맵에 무기 줍기 오브젝트를 배치합니다.
public class WeaponPickupSpawner : MonoBehaviour
{
    const string DefaultWeaponSpritePath = "Assets/Weapon/New_Weapon_06.png";

    [SerializeField] Sprite weaponSprite;
    [SerializeField] float groundHeight = 0f;

    [Header("스폰 위치 (플레이어 시작 근처에서 보기 쉬운 곳)")]
    [SerializeField] Vector3[] spawnPositions =
    {
        new Vector3(4f, 0f, 2f),
        new Vector3(-4f, 0f, 3f),
        new Vector3(6f, 0f, -3f)
    };

    void Start()
    {
        EnsurePlayerWeaponCombat();
        StartCoroutine(SpawnPickupsWhenWorldReady());
    }

    IEnumerator SpawnPickupsWhenWorldReady()
    {
        yield return WorldLoadCoordinator.WaitUntilWorldReady();
        EnsureWeaponSprite();

        if (weaponSprite == null)
        {
            Debug.LogWarning("[WeaponPickupSpawner] weaponSprite가 비어 있습니다. Inspector에 New_Weapon_06를 넣어 주세요.");
            yield break;
        }

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            SpawnPickup(spawnPositions[i]);
        }
    }

    void EnsurePlayerWeaponCombat()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return;
        }

        PlayerWeaponCombat weaponCombat = playerObject.GetComponent<PlayerWeaponCombat>();
        if (weaponCombat == null)
        {
            weaponCombat = playerObject.AddComponent<PlayerWeaponCombat>();
        }

        weaponCombat.EnsureSlashVfxPrefab();

        if (playerObject.GetComponent<PlayerMovement>() == null)
        {
            playerObject.AddComponent<PlayerMovement>();
        }

        if (playerObject.GetComponent<PlayerWorldPosition>() == null)
        {
            playerObject.AddComponent<PlayerWorldPosition>();
        }

        if (playerObject.GetComponent<PlayerStats>() == null)
        {
            playerObject.AddComponent<PlayerStats>();
        }

    }

    void EnsureWeaponSprite()
    {
        if (weaponSprite != null)
        {
            return;
        }

#if UNITY_EDITOR
        weaponSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DefaultWeaponSpritePath);
#endif
    }

    void SpawnPickup(Vector3 spawnPosition)
    {
        Vector3 worldPosition = spawnPosition;
        worldPosition.y = groundHeight;

        GameObject pickupObject = new GameObject("WeaponPickup_New_Weapon_06");
        pickupObject.transform.position = worldPosition;

        WeaponPickup pickup = pickupObject.AddComponent<WeaponPickup>();
        pickup.Configure(weaponSprite, groundHeight);
    }

#if UNITY_EDITOR
    void Reset()
    {
        EnsureWeaponSprite();
    }
#endif
}
