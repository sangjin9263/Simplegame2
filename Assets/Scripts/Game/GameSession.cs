using UnityEngine;

// 플레이어·월드 설정 참조를 한 번만 찾아 캐시합니다.
public static class GameSession
{
    static Transform playerTransform;
    static PlayerMovement playerMovement;
    static PlayerWorldPosition playerWorldPosition;
    static PlayerStats playerStats;
    static PlayerHealth playerHealth;
    static PlayerWeaponCombat playerWeaponCombat;
    static PlayerRangedCombat playerRangedCombat;
    static PlayerMagicCombat playerMagicCombat;
    static bool playerSearchDone;

    public static Transform PlayerTransform => playerTransform;
    public static PlayerMovement PlayerMovement => playerMovement;
    public static PlayerStats PlayerStats => playerStats;
    public static PlayerHealth PlayerHealth => playerHealth;
    public static PlayerWeaponCombat PlayerWeaponCombat => playerWeaponCombat;
    public static PlayerRangedCombat PlayerRangedCombat => playerRangedCombat;
    public static PlayerMagicCombat PlayerMagicCombat => playerMagicCombat;
    public static float GroundY => WorldSettings.GroundY;

    public static float CharacterFeetYOffset => WorldSettings.CharacterFeetYOffset;

    public static bool TryGetPlayerTransform(out Transform transform)
    {
        EnsurePlayerCached();
        transform = playerTransform;
        return transform != null;
    }

    public static bool TryGetPlayerWorldCenter(out Vector3 center)
    {
        EnsurePlayerCached();

        if (playerMovement != null)
        {
            center = playerMovement.WorldCenter;
            return true;
        }

        if (playerWorldPosition != null)
        {
            center = playerWorldPosition.WorldCenter;
            return true;
        }

        if (playerTransform != null)
        {
            center = playerTransform.position;
            return true;
        }

        center = Vector3.zero;
        return false;
    }

    public static void RegisterPlayer(PlayerMovement movement)
    {
        if (movement == null)
        {
            return;
        }

        playerTransform = movement.transform;
        playerMovement = movement;
        playerWorldPosition = movement.GetComponent<PlayerWorldPosition>();
        playerStats = movement.GetComponent<PlayerStats>();
        playerHealth = movement.GetComponent<PlayerHealth>();
        playerWeaponCombat = movement.GetComponent<PlayerWeaponCombat>();
        playerRangedCombat = movement.GetComponent<PlayerRangedCombat>();
        playerMagicCombat = movement.GetComponent<PlayerMagicCombat>();
        playerSearchDone = true;
    }

    public static void BindWorldSettings(WorldSettings settings)
    {
        if (settings != null)
        {
            WorldSettings.SetActive(settings);
        }
    }

    public static void ResetForPlay()
    {
        playerTransform = null;
        playerMovement = null;
        playerWorldPosition = null;
        playerStats = null;
        playerHealth = null;
        playerWeaponCombat = null;
        playerRangedCombat = null;
        playerMagicCombat = null;
        playerSearchDone = false;
        MonsterRegistry.Clear();
    }

    static void EnsurePlayerCached()
    {
        if (playerTransform != null)
        {
            return;
        }

        if (playerSearchDone)
        {
            return;
        }

        playerSearchDone = true;

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return;
        }

        playerTransform = playerObject.transform;
        playerMovement = playerObject.GetComponent<PlayerMovement>();
        playerWorldPosition = playerObject.GetComponent<PlayerWorldPosition>();
        playerStats = playerObject.GetComponent<PlayerStats>();
        playerHealth = playerObject.GetComponent<PlayerHealth>();
        playerWeaponCombat = playerObject.GetComponent<PlayerWeaponCombat>();
        playerRangedCombat = playerObject.GetComponent<PlayerRangedCombat>();
        playerMagicCombat = playerObject.GetComponent<PlayerMagicCombat>();
    }
}
