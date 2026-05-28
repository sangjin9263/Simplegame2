using System.Collections.Generic;
using UnityEngine;

// Main_land / High_land 청크 위에 나무 등을 랜덤 배치합니다.
public static class ChunkRandomPropSpawner
{
    const float DefaultSlopeAvoidRadius = 12f;
    const float DefaultMainHighBoundaryAvoidRadius = 1.6f;
    const float HighLandSupportProbeRadius = 0.55f;
    const float HighLandSupportHeightTolerance = 0.45f;

    public static void SpawnForChunk(
        ChunkInstance chunkInstance,
        ChunkCoordinate coordinate,
        Vector3 chunkWorldOrigin,
        float chunkSize,
        ChunkDefinition chunkDefinition,
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

        GameObject floorPrefab = chunkInstance.floorPrefabUsed;
        if (!IsTreeSpawnFloor(floorPrefab))
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

        if (maxX <= minX || maxZ <= minZ)
        {
            Random.state = oldState;
            return;
        }

        for (int i = 0; i < targetCount; i++)
        {
            if (!TryPickPosition(
                    minX,
                    maxX,
                    minZ,
                    maxZ,
                    chunkSize,
                    rule,
                    placedPositions,
                    chunkWorldOrigin,
                    coordinate,
                    chunkDefinition,
                    floorPrefab,
                    out Vector3 worldPosition))
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
            ChunkFloorSurface.AlignPropVisualBaseToWorldY(spawned, worldPosition.y);
            worldPosition = spawned.transform.position;
            placedPositions.Add(worldPosition);
            chunkInstance.treePlacementPositions.Add(worldPosition);
            ChunkTreePlacementIndex.Register(worldPosition);
            chunkInstance.AddSpawnedProp(spawned, prefab);
        }

        Random.state = oldState;
    }

    static bool TryPickPosition(
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        float chunkSize,
        ChunkRandomPropRule rule,
        List<Vector3> placedPositions,
        Vector3 chunkWorldOrigin,
        ChunkCoordinate chunkCoordinate,
        ChunkDefinition chunkDefinition,
        GameObject floorPrefab,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        float minDistance = Mathf.Max(0.5f, rule.minDistanceBetweenProps);
        bool isHighLandFloor = ChunkFloorLayout.IsHighLandPrefab(floorPrefab);

        for (int attempt = 0; attempt < rule.maxPlacementAttempts; attempt++)
        {
            float localX = Random.Range(minX, maxX);
            float localZ = Random.Range(minZ, maxZ);
            worldPosition = chunkWorldOrigin + new Vector3(localX, 0f, localZ);

            float expectedY = chunkWorldOrigin.y + ChunkFloorSurface.GetSurfaceYOffsetAboveChunkRoot(floorPrefab);
            if (!GroundHeightSampler.TryGetSurfaceY(worldPosition, expectedY, out float sampledGroundY))
            {
                continue;
            }

            // High_land는 윗면(평평한 상단)에서만 나무를 허용해서 절벽/바깥 공중 배치를 막습니다.
            if (isHighLandFloor && sampledGroundY < expectedY - 0.6f)
            {
                continue;
            }

            if (isHighLandFloor
                && !HasStableHighLandSupport(
                    worldPosition,
                    expectedY,
                    HighLandSupportProbeRadius,
                    HighLandSupportHeightTolerance))
            {
                continue;
            }

            worldPosition.y = sampledGroundY + rule.positionY;

            if (!IsOutsidePlayerSafeZone(worldPosition, rule.playerSafeRadius))
            {
                continue;
            }

            if (!IsFarEnough(worldPosition, placedPositions, minDistance))
            {
                continue;
            }

            if (!ChunkTreePlacementIndex.HasClearance(worldPosition, minDistance))
            {
                continue;
            }

            float slopeAvoidRadius = rule.slopeAvoidRadius > 0f
                ? rule.slopeAvoidRadius
                : DefaultSlopeAvoidRadius;
            if (IsNearSlopeChunk(worldPosition, chunkCoordinate, chunkSize, chunkDefinition, slopeAvoidRadius))
            {
                continue;
            }

            float mainHighBoundaryAvoidRadius = rule.mainHighBoundaryAvoidRadius > 0f
                ? rule.mainHighBoundaryAvoidRadius
                : DefaultMainHighBoundaryAvoidRadius;
            if (IsNearMainHighBoundary(
                    worldPosition,
                    chunkCoordinate,
                    chunkSize,
                    chunkDefinition,
                    floorPrefab,
                    mainHighBoundaryAvoidRadius))
            {
                continue;
            }

            return true;
        }

        return false;
    }

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

    static bool IsTreeSpawnFloor(GameObject floorPrefab)
    {
        return ChunkFloorLayout.IsMainLandPrefab(floorPrefab)
            || ChunkFloorLayout.IsHighLandPrefab(floorPrefab);
    }

    static bool IsNearSlopeChunk(
        Vector3 worldPosition,
        ChunkCoordinate candidateChunk,
        float chunkSize,
        ChunkDefinition chunkDefinition,
        float slopeAvoidRadius)
    {
        if (slopeAvoidRadius <= 0f || chunkSize <= 0f)
        {
            return false;
        }

        int searchRange = Mathf.Max(1, Mathf.CeilToInt(slopeAvoidRadius / chunkSize));
        float avoidRadiusSqr = slopeAvoidRadius * slopeAvoidRadius;

        for (int offsetX = -searchRange; offsetX <= searchRange; offsetX++)
        {
            for (int offsetZ = -searchRange; offsetZ <= searchRange; offsetZ++)
            {
                ChunkCoordinate nearby = new ChunkCoordinate(candidateChunk.x + offsetX, candidateChunk.z + offsetZ);
                if (!IsSlopeChunk(nearby, chunkDefinition))
                {
                    continue;
                }

                float minX = nearby.x * chunkSize;
                float minZ = nearby.z * chunkSize;
                float maxX = minX + chunkSize;
                float maxZ = minZ + chunkSize;

                float closestX = Mathf.Clamp(worldPosition.x, minX, maxX);
                float closestZ = Mathf.Clamp(worldPosition.z, minZ, maxZ);
                float dx = worldPosition.x - closestX;
                float dz = worldPosition.z - closestZ;
                if (dx * dx + dz * dz <= avoidRadiusSqr)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool IsSlopeChunk(ChunkCoordinate coordinate, ChunkDefinition chunkDefinition)
    {
        if (chunkDefinition != null
            && chunkDefinition.TryPickFloorVariant(coordinate, out ChunkFloorVariant variant)
            && ChunkFloorLayout.IsSlopePrefab(variant.prefab))
        {
            return true;
        }

        if (ChunkFloorGroundRegistry.TryGetEntryAtCoordinate(coordinate, out ChunkFloorGroundRegistry.Entry entry)
            && ChunkFloorLayout.IsSlopePrefab(entry.floorPrefab))
        {
            return true;
        }

        return false;
    }

    static bool IsNearMainHighBoundary(
        Vector3 worldPosition,
        ChunkCoordinate candidateChunk,
        float chunkSize,
        ChunkDefinition chunkDefinition,
        GameObject currentFloorPrefab,
        float boundaryAvoidRadius)
    {
        if (boundaryAvoidRadius <= 0f || chunkSize <= 0f || chunkDefinition == null || currentFloorPrefab == null)
        {
            return false;
        }

        bool currentIsMain = ChunkFloorLayout.IsMainLandPrefab(currentFloorPrefab);
        bool currentIsHigh = ChunkFloorLayout.IsHighLandPrefab(currentFloorPrefab);
        if (!currentIsMain && !currentIsHigh)
        {
            return false;
        }

        int searchRange = Mathf.Max(1, Mathf.CeilToInt(boundaryAvoidRadius / chunkSize));
        float boundaryAvoidRadiusSqr = boundaryAvoidRadius * boundaryAvoidRadius;

        for (int offsetX = -searchRange; offsetX <= searchRange; offsetX++)
        {
            for (int offsetZ = -searchRange; offsetZ <= searchRange; offsetZ++)
            {
                if (offsetX == 0 && offsetZ == 0)
                {
                    continue;
                }

                ChunkCoordinate nearby = new ChunkCoordinate(candidateChunk.x + offsetX, candidateChunk.z + offsetZ);
                if (!chunkDefinition.TryPickFloorVariant(nearby, out ChunkFloorVariant nearbyVariant))
                {
                    continue;
                }

                bool nearbyIsMain = ChunkFloorLayout.IsMainLandPrefab(nearbyVariant.prefab);
                bool nearbyIsHigh = ChunkFloorLayout.IsHighLandPrefab(nearbyVariant.prefab);
                if (!nearbyIsMain && !nearbyIsHigh)
                {
                    continue;
                }

                bool isOppositeHeightPair = (currentIsMain && nearbyIsHigh) || (currentIsHigh && nearbyIsMain);
                if (!isOppositeHeightPair)
                {
                    continue;
                }

                float minX = nearby.x * chunkSize;
                float minZ = nearby.z * chunkSize;
                float maxX = minX + chunkSize;
                float maxZ = minZ + chunkSize;

                float closestX = Mathf.Clamp(worldPosition.x, minX, maxX);
                float closestZ = Mathf.Clamp(worldPosition.z, minZ, maxZ);
                float dx = worldPosition.x - closestX;
                float dz = worldPosition.z - closestZ;
                if (dx * dx + dz * dz <= boundaryAvoidRadiusSqr)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool HasStableHighLandSupport(
        Vector3 centerWorldPosition,
        float expectedTopY,
        float probeRadius,
        float yTolerance)
    {
        Vector3[] offsets =
        {
            Vector3.zero,
            new Vector3(probeRadius, 0f, 0f),
            new Vector3(-probeRadius, 0f, 0f),
            new Vector3(0f, 0f, probeRadius),
            new Vector3(0f, 0f, -probeRadius)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 probeWorld = centerWorldPosition + offsets[i];
            if (!GroundHeightSampler.TryGetSurfaceY(probeWorld, expectedTopY, out float probeY))
            {
                return false;
            }

            if (probeY < expectedTopY - yTolerance)
            {
                return false;
            }
        }

        return true;
    }
}
