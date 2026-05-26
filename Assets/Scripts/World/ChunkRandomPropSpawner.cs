using System.Collections.Generic;
using UnityEngine;

// 청크 좌표 시드로 나무 등을 랜덤 배치하는 도구입니다.
public static class ChunkRandomPropSpawner
{
    // 한 청크에 규칙대로 오브젝트를 생성합니다.
    public static void SpawnForChunk(
        ChunkInstance chunkInstance,
        ChunkCoordinate coordinate,
        Vector3 chunkWorldOrigin,
        float chunkSize,
        ChunkRandomPropRule rule,
        ChunkPool pool,
        Transform parent)
    {
        if (rule == null || !rule.enabled || rule.prefabs == null || rule.prefabs.Length == 0)
        {
            return;
        }

        if (pool == null || parent == null || chunkInstance == null)
        {
            return;
        }

        int seed = coordinate.x * 73856093 ^ coordinate.z * 19349663;
        Random.State oldState = Random.state;
        Random.InitState(seed);

        int targetCount = Random.Range(rule.minCountPerChunk, rule.maxCountPerChunk + 1);
        List<Vector3> placedPositions = new List<Vector3>(targetCount);

        float minX = rule.edgePadding;
        float maxX = chunkSize - rule.edgePadding;
        float minZ = rule.edgePadding;
        float maxZ = chunkSize - rule.edgePadding;

        for (int i = 0; i < targetCount; i++)
        {
            if (!TryPickPosition(minX, maxX, minZ, maxZ, rule, placedPositions, chunkWorldOrigin, out Vector3 worldPosition))
            {
                continue;
            }

            GameObject prefab = rule.prefabs[Random.Range(0, rule.prefabs.Length)];
            if (prefab == null)
            {
                continue;
            }

            Quaternion rotation = rule.randomRotationY
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : Quaternion.identity;

            GameObject spawned = pool.Get(prefab, worldPosition, rotation, parent);
            if (spawned == null)
            {
                continue;
            }

            PropCollisionLayers.ApplyToRoot(spawned);
            placedPositions.Add(worldPosition);
            chunkInstance.AddSpawnedProp(spawned, prefab);
        }

        Random.state = oldState;
    }

    // 겹치지 않는 위치를 고릅니다.
    static bool TryPickPosition(
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        ChunkRandomPropRule rule,
        List<Vector3> placedPositions,
        Vector3 chunkWorldOrigin,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        for (int attempt = 0; attempt < rule.maxPlacementAttempts; attempt++)
        {
            float localX = Random.Range(minX, maxX);
            float localZ = Random.Range(minZ, maxZ);
            worldPosition = chunkWorldOrigin + new Vector3(localX, rule.positionY, localZ);

            if (!IsOutsidePlayerSafeZone(worldPosition, rule.playerSafeRadius))
            {
                continue;
            }

            if (IsFarEnough(worldPosition, placedPositions, rule.minDistanceBetweenProps))
            {
                return true;
            }
        }

        return false;
    }

    // 이미 놓인 것과 충분히 떨어져 있는지 확인합니다.
    static bool IsFarEnough(Vector3 candidate, List<Vector3> placedPositions, float minDistance)
    {
        float minDistanceSqr = minDistance * minDistance;

        for (int i = 0; i < placedPositions.Count; i++)
        {
            Vector3 diff = placedPositions[i] - candidate;
            diff.y = 0f;
            if (diff.sqrMagnitude < minDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    // 플레이어 시작 지점 근처에는 나무를 두지 않습니다.
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
