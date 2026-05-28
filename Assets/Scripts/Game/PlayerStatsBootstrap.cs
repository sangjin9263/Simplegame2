using UnityEngine;

// 씬 로드 후 플레이어를 GameSession에 연결합니다.
public static class PlayerStatsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureSessionPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return;
        }

        EnsureRequiredPlayerComponents(playerObject);

        PlayerMovement movement = playerObject.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            GameSession.RegisterPlayer(movement);
        }

        GameplayComponents.EnsurePlayer(playerObject, logIfMissing: false);
    }

    static void EnsureRequiredPlayerComponents(GameObject playerObject)
    {
        if (playerObject.GetComponent<PlayerRangedCombat>() == null)
        {
            playerObject.AddComponent<PlayerRangedCombat>();
        }

        if (playerObject.GetComponent<PlayerMagicCombat>() == null)
        {
            playerObject.AddComponent<PlayerMagicCombat>();
        }
    }
}
