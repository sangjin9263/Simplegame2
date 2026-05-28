using UnityEngine;

// ChunkPool에 넣기 전에 프리팹이 GameObject인지 확인합니다.
public static class ChunkPrefabUtility
{
    const string DefaultMainLandPrefabPath = "Assets/Prefabs/Land/Main_land.prefab";

    public static bool TryGetInstantiablePrefab(Object candidate, out GameObject prefab)
    {
        prefab = null;
        if (candidate is GameObject gameObject && gameObject.GetComponent<Transform>() != null)
        {
            prefab = gameObject;
            return true;
        }

#if UNITY_EDITOR
        GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMainLandPrefabPath);
        if (loaded != null && loaded.GetComponent<Transform>() != null)
        {
            prefab = loaded;
            return true;
        }
#endif

        return false;
    }
}
