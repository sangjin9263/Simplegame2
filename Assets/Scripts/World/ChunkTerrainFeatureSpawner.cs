using UnityEngine;

// 청크 시드로 언덕 프리팹을 격자에 맞춰 배치합니다.
public static class ChunkTerrainFeatureSpawner
{
    public static void TrySpawnForChunk(
        ChunkInstance chunkInstance,
        ChunkCoordinate coordinate,
        Vector3 chunkWorldOrigin,
        ChunkTerrainFeatureRule rule,
        ChunkPool pool,
        Transform parent)
    {
        if (rule == null || !rule.enabled)
        {
            return;
        }

        if (!ChunkPrefabUtility.TryGetInstantiablePrefab(rule.prefab, out GameObject terrainPrefab))
        {
            return;
        }

        rule.prefab = terrainPrefab;

        if (pool == null || parent == null || chunkInstance == null)
        {
            return;
        }

        int seed = coordinate.x * 92837111 ^ coordinate.z * 689287499 ^ 0x5F3759DF;
        Random.State oldState = Random.state;
        Random.InitState(seed);

        if (Random.value > rule.spawnChancePerChunk)
        {
            Random.state = oldState;
            return;
        }

        Vector3 worldPosition = new Vector3(chunkWorldOrigin.x, rule.positionY, chunkWorldOrigin.z);
        if (!IsOutsidePlayerSafeZone(worldPosition, rule.playerSafeRadius))
        {
            Random.state = oldState;
            return;
        }

        float yaw = 0f;
        if (rule.randomRotationY)
        {
            yaw = Random.Range(0, 4) * 90f;
        }

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        GameObject spawned = pool.Get(rule.prefab, worldPosition, rotation, parent);
        if (spawned != null)
        {
            if (spawned.GetComponent<WalkableTerrainFeature>() == null)
            {
                spawned.AddComponent<WalkableTerrainFeature>();
            }

            PropCollisionLayers.EnableWalkableTerrainColliders(spawned);
            spawned.name = rule.prefab.name + "_" + coordinate.x + "_" + coordinate.z;
            chunkInstance.AddSpawnedProp(spawned, rule.prefab);
        }

        Random.state = oldState;
    }

    static bool IsOutsidePlayerSafeZone(Vector3 worldPosition, float safeRadius)
    {
        if (safeRadius <= 0f)
        {
            return true;
        }

        Vector3 flat = worldPosition;
        flat.y = 0f;
        return flat.sqrMagnitude >= safeRadius * safeRadius;
    }
}
