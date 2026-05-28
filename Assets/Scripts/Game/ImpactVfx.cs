using UnityEngine;

// 피격 이펙트를 공용으로 스폰합니다.
public static class ImpactVfx
{
    static GameObject cachedHitImpactPrefab;
    static GameObject cachedFireImpactPrefab;
    static GameObject cachedEnergyImpactPrefab;

    public static void SpawnHitImpact(Vector3 worldPosition)
    {
        cachedHitImpactPrefab = GameAssets.LoadHitImpactPrefab(cachedHitImpactPrefab);
        Spawn(cachedHitImpactPrefab, worldPosition);
    }

    public static void SpawnFireImpact(Vector3 worldPosition)
    {
        cachedFireImpactPrefab = GameAssets.LoadFireImpactPrefab(cachedFireImpactPrefab);
        Spawn(cachedFireImpactPrefab, worldPosition);
    }

    public static void SpawnEnergyImpact(Vector3 worldPosition)
    {
        cachedEnergyImpactPrefab = GameAssets.LoadEnergyImpactPrefab(cachedEnergyImpactPrefab);
        Spawn(cachedEnergyImpactPrefab, worldPosition);
    }

    static void Spawn(GameObject prefab, Vector3 worldPosition)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Object.Instantiate(prefab, worldPosition, Quaternion.identity);
        Object.Destroy(instance, 1.6f);
    }
}
