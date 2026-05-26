using UnityEngine;

// 구형 HP HUD만 정리합니다. UI 생성/스폰은 하지 않습니다 (씬·프리팹에 직접 배치).
public static class GameHudRuntimeInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CleanupLegacyHudOnly()
    {
        PlayerHpHudView[] legacyViews = Object.FindObjectsByType<PlayerHpHudView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < legacyViews.Length; i++)
        {
            if (legacyViews[i] != null)
            {
                Object.Destroy(legacyViews[i].gameObject);
            }
        }
    }
}
