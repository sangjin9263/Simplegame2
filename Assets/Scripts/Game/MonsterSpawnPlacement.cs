using UnityEngine;

// 몬스터 스폰 위치가 나무 등 장애물과 겹치지 않는지 검사합니다.
public static class MonsterSpawnPlacement
{
    static readonly Vector2[] ClearanceOffsets =
    {
        Vector2.zero,
        new Vector2(0.8f, 0f),
        new Vector2(-0.8f, 0f),
        new Vector2(0f, 0.8f),
        new Vector2(0f, -0.8f),
        new Vector2(0.65f, 0.65f),
        new Vector2(-0.65f, 0.65f),
        new Vector2(0.65f, -0.65f),
        new Vector2(-0.65f, -0.65f),
        new Vector2(1.2f, 0f),
        new Vector2(-1.2f, 0f),
        new Vector2(0f, 1.2f),
        new Vector2(0f, -1.2f)
    };

    public static bool IsSpawnClear(
        Vector3 feetPosition,
        float radius,
        float height)
    {
        if (WorldCollision.QueryLayerMask == 0)
        {
            return true;
        }

        GetCapsuleEnds(feetPosition, radius, height, out Vector3 bottom, out Vector3 top);

        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            WorldCollision.QueryLayerMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (PropCollisionLayers.IsTreeObstacleCollider(overlaps[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryResolveClearPosition(
        Vector3 candidate,
        float radius,
        float height,
        float fallbackGroundY,
        out Vector3 resolved)
    {
        float groundY = GroundHeightSampler.GetCharacterSurfaceY(candidate, fallbackGroundY);
        candidate.y = groundY;
        resolved = candidate;

        for (int i = 0; i < ClearanceOffsets.Length; i++)
        {
            Vector3 test = candidate;
            test.x += ClearanceOffsets[i].x;
            test.z += ClearanceOffsets[i].y;
            test.y = GroundHeightSampler.GetCharacterSurfaceY(test, fallbackGroundY);

            if (!IsSpawnClear(test, radius, height))
            {
                continue;
            }

            resolved = test;
            return true;
        }

        return false;
    }

    static void GetCapsuleEnds(
        Vector3 feetPosition,
        float radius,
        float height,
        out Vector3 bottom,
        out Vector3 top)
    {
        float cylinderHeight = Mathf.Max(height - radius * 2f, 0.05f);
        bottom = feetPosition + Vector3.up * radius;
        top = bottom + Vector3.up * cylinderHeight;
    }
}
