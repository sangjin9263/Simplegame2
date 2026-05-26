using UnityEngine;

// (선택) 빈 씬에서만 HUD 프리팹을 스폰합니다. SampleScene처럼 PlayerHudRoot가 있으면 비활성화하세요.
public class PlayerHudBootstrap : MonoBehaviour
{
    const string DefaultPrefabAssetPath = "Assets/Prefabs/UI/PlayerHudRoot.prefab";
    const string ResourcesPrefabName = "PlayerHudRoot";

    [SerializeField] GameObject playerHudPrefab;
    [SerializeField] bool spawnIfMissingInScene;

    void Awake()
    {
        if (!spawnIfMissingInScene)
        {
            return;
        }

        if (FindFirstObjectByType<PlayerStatusHudView>() != null)
        {
            return;
        }

        PlayerHpHudView[] legacyViews = FindObjectsByType<PlayerHpHudView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < legacyViews.Length; i++)
        {
            if (legacyViews[i] != null)
            {
                Destroy(legacyViews[i].gameObject);
            }
        }

        GameObject prefab = ResolvePrefab();
        if (prefab != null)
        {
            Instantiate(prefab);
            return;
        }

        PlayerHudUiLayoutFactory.CreateHudRoot();
    }

    GameObject ResolvePrefab()
    {
        if (playerHudPrefab != null)
        {
            return playerHudPrefab;
        }

#if UNITY_EDITOR
        GameObject editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPrefabAssetPath);
        if (editorPrefab != null)
        {
            return editorPrefab;
        }
#endif
        return Resources.Load<GameObject>(ResourcesPrefabName);
    }
}
