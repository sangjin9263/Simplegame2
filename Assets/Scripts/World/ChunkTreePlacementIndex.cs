using System.Collections.Generic;
using UnityEngine;

// 청크 경계를 넘어서도 나무가 겹치지 않게 월드 XZ 위치를 기록합니다.
public static class ChunkTreePlacementIndex
{
    static readonly List<Vector3> PlacedTreePositions = new List<Vector3>();

    public static void Clear()
    {
        PlacedTreePositions.Clear();
    }

    public static bool HasClearance(Vector3 worldPosition, float minDistance)
    {
        if (minDistance <= 0f || PlacedTreePositions.Count == 0)
        {
            return true;
        }

        float minDistanceSqr = minDistance * minDistance;

        for (int i = 0; i < PlacedTreePositions.Count; i++)
        {
            Vector3 diff = PlacedTreePositions[i] - worldPosition;
            diff.y = 0f;
            if (diff.sqrMagnitude < minDistanceSqr)
            {
                return false;
            }
        }

        return true;
    }

    public static void Register(Vector3 worldPosition)
    {
        PlacedTreePositions.Add(worldPosition);
    }

    public static void UnregisterChunk(ChunkInstance chunkInstance)
    {
        if (chunkInstance == null || chunkInstance.treePlacementPositions.Count == 0)
        {
            return;
        }

        IReadOnlyList<Vector3> chunkPositions = chunkInstance.treePlacementPositions;
        for (int c = 0; c < chunkPositions.Count; c++)
        {
            Vector3 removeTarget = chunkPositions[c];
            for (int i = PlacedTreePositions.Count - 1; i >= 0; i--)
            {
                Vector3 diff = PlacedTreePositions[i] - removeTarget;
                diff.y = 0f;
                if (diff.sqrMagnitude < 0.01f)
                {
                    PlacedTreePositions.RemoveAt(i);
                    break;
                }
            }
        }

        chunkInstance.treePlacementPositions.Clear();
    }
}
