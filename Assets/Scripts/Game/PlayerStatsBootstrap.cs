using UnityEngine;

// 플레이어에 PlayerStats를 보장합니다 (HUD와 무관하게 CSV 적용).
public static class PlayerStatsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsurePlayerStats()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return;
        }

        if (playerObject.GetComponent<PlayerStats>() == null)
        {
            playerObject.AddComponent<PlayerStats>();
        }
    }
}
