using System.Collections.Generic;
using UnityEngine;

// 스트리머가 깐 바닥 청크를 등록해, 레이가 실패할 때 기대 지면 높이를 알려줍니다.
public static class ChunkFloorGroundRegistry
{
    public struct Entry
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public float floorRootY;
        public GameObject floorPrefab;
    }

    static readonly Dictionary<Vector2Int, Entry> activeEntries = new Dictionary<Vector2Int, Entry>();
    static float registeredChunkSize = 6f;

    public static void Clear()
    {
        activeEntries.Clear();
    }

    public static void Register(ChunkCoordinate coordinate, float chunkSize, float floorRootY, GameObject floorPrefab)
    {
        if (floorPrefab == null || chunkSize <= 0f)
        {
            return;
        }

        registeredChunkSize = chunkSize;
        float minX = coordinate.x * chunkSize;
        float minZ = coordinate.z * chunkSize;
        Vector2Int key = coordinate.ToVector2Int();
        activeEntries[key] = new Entry
        {
            minX = minX,
            maxX = minX + chunkSize,
            minZ = minZ,
            maxZ = minZ + chunkSize,
            floorRootY = floorRootY,
            floorPrefab = floorPrefab
        };
    }

    public static void Unregister(ChunkCoordinate coordinate)
    {
        activeEntries.Remove(coordinate.ToVector2Int());
    }

    public static bool TryGetEntryAtWorldPosition(Vector3 worldPosition, out Entry entry)
    {
        ChunkCoordinate coordinate = ChunkCoordinate.FromWorldPosition(worldPosition, registeredChunkSize);
        if (activeEntries.TryGetValue(coordinate.ToVector2Int(), out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }

    public static bool TryGetEntryAtCoordinate(ChunkCoordinate coordinate, out Entry entry)
    {
        if (activeEntries.TryGetValue(coordinate.ToVector2Int(), out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }

    public static float GetExpectedSurfaceY(in Entry entry)
    {
        return entry.floorRootY + ChunkFloorSurface.GetSurfaceYOffsetAboveChunkRoot(entry.floorPrefab);
    }
}
