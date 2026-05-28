using System.Collections;

using UnityEngine;



// 맵에 무기 줍기 오브젝트를 배치합니다.

public class WeaponPickupSpawner : MonoBehaviour

{

    [SerializeField] Sprite meleeWeaponSprite;

    [SerializeField] Sprite bowWeaponSprite;
    [SerializeField] Sprite staffWeaponSprite;



    [Header("근접 무기 스폰 위치")]

    [SerializeField] Vector3[] meleeSpawnPositions =

    {

        new Vector3(4f, 0f, 2f),

        new Vector3(-4f, 0f, 3f)

    };



    [Header("활 스폰 위치")]

    [SerializeField] Vector3[] bowSpawnPositions =

    {

        new Vector3(6f, 0f, -3f)

    };

    [Header("스태프 스폰 위치")]
    [SerializeField] Vector3[] staffSpawnPositions =
    {
        new Vector3(-6f, 0f, -3f)
    };



    void Start()

    {

        ValidatePlayerSetup();

        StartCoroutine(SpawnPickupsWhenWorldReady());

    }



    IEnumerator SpawnPickupsWhenWorldReady()

    {

        yield return WorldLoadCoordinator.WaitUntilWorldReady();

        EnsureWeaponSprites();



        PlayerWeaponCombat weaponCombat = GameSession.PlayerWeaponCombat;

        if (weaponCombat != null)

        {

            weaponCombat.EnsureSlashVfxPrefab();

        }



        PlayerRangedCombat rangedCombat = GameSession.PlayerRangedCombat;

        if (rangedCombat != null)

        {

            rangedCombat.EnsureWeaponAssets();

        }

        PlayerMagicCombat magicCombat = GameSession.PlayerMagicCombat;
        if (magicCombat != null)
        {
            magicCombat.EnsureMagicAssets();
        }



        if (meleeWeaponSprite != null)

        {

            for (int i = 0; i < meleeSpawnPositions.Length; i++)

            {

                SpawnPickup(WeaponPickupKind.Melee, meleeWeaponSprite, meleeSpawnPositions[i]);

            }

        }



        if (bowWeaponSprite != null)

        {

            for (int i = 0; i < bowSpawnPositions.Length; i++)

            {

                SpawnPickup(WeaponPickupKind.Bow, bowWeaponSprite, bowSpawnPositions[i]);

            }

        }

        if (staffWeaponSprite != null)
        {
            for (int i = 0; i < staffSpawnPositions.Length; i++)
            {
                SpawnPickup(WeaponPickupKind.Staff, staffWeaponSprite, staffSpawnPositions[i]);
            }
        }

    }



    void ValidatePlayerSetup()

    {

        if (!GameSession.TryGetPlayerTransform(out Transform player))

        {

            return;

        }



        if (player.GetComponent<PlayerRangedCombat>() == null)
        {
            player.gameObject.AddComponent<PlayerRangedCombat>();
            GameSession.RegisterPlayer(player.GetComponent<PlayerMovement>());
        }

        if (player.GetComponent<PlayerMagicCombat>() == null)
        {
            player.gameObject.AddComponent<PlayerMagicCombat>();
            GameSession.RegisterPlayer(player.GetComponent<PlayerMovement>());
        }

        GameplayComponents.EnsurePlayer(player.gameObject, logIfMissing: true);

    }



    void EnsureWeaponSprites()

    {

        meleeWeaponSprite = GameAssets.LoadDefaultWeaponSprite(meleeWeaponSprite);

        bowWeaponSprite = GameAssets.LoadBowSprite(bowWeaponSprite);
        staffWeaponSprite = GameAssets.LoadStaffSprite(staffWeaponSprite);

    }



    void SpawnPickup(WeaponPickupKind kind, Sprite sprite, Vector3 localPosition)

    {

        if (sprite == null)

        {

            return;

        }



        Vector3 worldPosition = localPosition;

        worldPosition.y = GroundHeightSampler.GetSurfaceY(worldPosition, GameSession.GroundY);



        string objectName = "MeleeWeaponPickup";
        if (kind == WeaponPickupKind.Bow)
        {
            objectName = "BowPickup";
        }
        else if (kind == WeaponPickupKind.Staff)
        {
            objectName = "StaffPickup";
        }

        GameObject pickupObject = new GameObject(objectName);

        pickupObject.transform.position = worldPosition;



        WeaponPickup pickup = pickupObject.AddComponent<WeaponPickup>();

        pickup.Configure(kind, sprite, worldPosition.y);

    }

}

