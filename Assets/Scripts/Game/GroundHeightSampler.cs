using UnityEngine;

// 스폰·배치용: Ground 레이어에서 지면 Y를 찾아 반환합니다.
public static class GroundHeightSampler
{
    const string GroundLayerName = "Ground";
    const float FootProbeUp = 0.55f;
    const float FootProbeDown = 3f;
    const float SurfaceRayStartHeight = 24f;
    const float MinWalkableSurfaceNormalY = 0.25f;

    static readonly Vector3[] FootProbeOffsets =
    {
        Vector3.zero,
        new Vector3(0.18f, 0f, 0f),
        new Vector3(-0.18f, 0f, 0.18f),
        new Vector3(0f, 0f, -0.18f)
    };

    static int cachedGroundLayer = int.MinValue;
    static RaycastHit[] raycastHitBuffer = new RaycastHit[12];

    public static int GroundLayer
    {
        get
        {
            if (cachedGroundLayer == int.MinValue)
            {
                cachedGroundLayer = LayerMask.NameToLayer(GroundLayerName);
            }

            return cachedGroundLayer;
        }
    }

    public static int GroundLayerMask
    {
        get
        {
            int layer = GroundLayer;
            if (layer < 0)
            {
                return 0;
            }

            return 1 << layer;
        }
    }

    // 월드 위치 아래 지면 Y를 반환합니다 (스폰·배치·무기용).
    public static bool TryGetSurfaceY(Vector3 worldPosition, float fallbackY, out float surfaceY)
    {
        surfaceY = fallbackY;
        float searchY = Mathf.Max(worldPosition.y, fallbackY);

        if (TrySampleFeetGround(
                worldPosition,
                searchY,
                searchY - 8f,
                searchY + 2f,
                out float feetGroundY))
        {
            surfaceY = feetGroundY;
            return true;
        }

        int mask = GroundLayerMask;
        if (mask == 0)
        {
            return false;
        }

        float rayStartY = searchY + SurfaceRayStartHeight;
        Vector3 origin = new Vector3(worldPosition.x, rayStartY, worldPosition.z);
        int hitCount = RaycastDownWalkable(origin, SurfaceRayStartHeight + 12f, mask, out float bestY);
        if (hitCount > 0)
        {
            surfaceY = bestY;
            return true;
        }

        return false;
    }

    public static float GetSurfaceY(Vector3 worldPosition, float fallbackY)
    {
        if (TryGetSurfaceY(worldPosition, fallbackY, out float surfaceY))
        {
            return surfaceY;
        }

        return fallbackY;
    }

    // 프롭·오브젝트 트랜스폼을 지면에 스냅합니다.
    public static void SnapTransformToSurface(Transform target, float fallbackY)
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = target.position;
        position.y = GetSurfaceY(position, fallbackY);
        target.position = position;
    }

    public static float ApplyCharacterFeetOffset(float groundY)
    {
        return groundY + WorldSettings.CharacterFeetYOffset;
    }

    // 캐릭터 발 오프셋을 더한 지면 Y를 반환합니다 (스폰 배치용).
    public static float GetCharacterSurfaceY(Vector3 worldPosition, float fallbackY)
    {
        return ApplyCharacterFeetOffset(GetSurfaceY(worldPosition, fallbackY));
    }

    // 캐릭터 트랜스폼을 지면에 스냅합니다 (초기 배치용, 이동 프레임이 아님).
    public static void SnapCharacterTransformToSurface(Transform target, float fallbackY)
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = target.position;
        position.y = GetCharacterSurfaceY(position, fallbackY);
        target.position = position;
    }

    public static bool IsWalkableGroundCollider(Collider collider)
    {
        if (collider == null || !collider.enabled)
        {
            return false;
        }

        return collider.GetComponentInParent<WalkableTerrainFeature>() != null;
    }

    // 두 지점 사이에 slope/바닥 지형이 막고 있으면 true (검기·자동 조준 가림 판정).
    public static bool IsWalkableTerrainBlockingLine(
        Vector3 fromWorld,
        float fromSurfaceY,
        Vector3 toWorld,
        float toSurfaceY,
        float bodyClearance = 0.55f,
        float endSlack = 0.35f)
    {
        int mask = GroundLayerMask;
        if (mask == 0)
        {
            return false;
        }

        Vector3 origin = new Vector3(fromWorld.x, fromSurfaceY + bodyClearance, fromWorld.z);
        Vector3 target = new Vector3(toWorld.x, toSurfaceY + bodyClearance, toWorld.z);
        Vector3 delta = target - origin;
        float distance = delta.magnitude;
        if (distance < 0.05f)
        {
            return false;
        }

        Vector3 direction = delta / distance;
        const float castRadius = 0.22f;

        if (!Physics.SphereCast(
                origin,
                castRadius,
                direction,
                out RaycastHit hit,
                distance,
                mask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (!IsWalkableGroundCollider(hit.collider))
        {
            return false;
        }

        return hit.distance < distance - endSlack;
    }

    // flat 방향으로 distance 만큼 갈 때 walkable 지형(경사·절벽)에 막히면 true.
    public static bool IsWalkableTerrainBlockingDirection(
        Vector3 fromWorld,
        float fromSurfaceY,
        Vector3 flatDirection,
        float distance,
        float bodyClearance = 0.55f,
        float endSlack = 0.2f)
    {
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.0001f || distance < 0.05f)
        {
            return false;
        }

        Vector3 target = fromWorld + flatDirection.normalized * distance;
        float toSurfaceY = GetCharacterSurfaceY(target, fromSurfaceY);
        return IsWalkableTerrainBlockingLine(
            fromWorld,
            fromSurfaceY,
            target,
            toSurfaceY,
            bodyClearance,
            endSlack);
    }

    // flat 방향으로 최대 얼마나 막히지 않고 갈 수 있는지(미터) 반환합니다.
    public static float GetMaxWalkableClearDistance(
        Vector3 fromWorld,
        float fromSurfaceY,
        Vector3 flatDirection,
        float maxDistance,
        float stepDistance = 0.55f,
        float bodyClearance = 0.55f,
        float endSlack = 0.18f)
    {
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.0001f || maxDistance < 0.05f)
        {
            return 0f;
        }

        float step = Mathf.Max(0.25f, stepDistance);
        float clearDistance = 0f;

        for (float distance = step; distance <= maxDistance + 0.001f; distance += step)
        {
            if (IsWalkableTerrainBlockingDirection(
                    fromWorld,
                    fromSurfaceY,
                    flatDirection,
                    distance,
                    bodyClearance,
                    endSlack))
            {
                break;
            }

            clearDistance = distance;
        }

        return clearDistance;
    }

    static bool TrySampleFeetGround(
        Vector3 worldPosition,
        float currentY,
        float minAllowedY,
        float maxAllowedY,
        out float groundY)
    {
        groundY = currentY;
        int mask = GroundLayerMask;
        if (mask == 0)
        {
            return false;
        }

        float rayLength = FootProbeUp + FootProbeDown;
        bool found = false;
        float closestDistance = float.MaxValue;
        float bestY = currentY;

        for (int probe = 0; probe < FootProbeOffsets.Length; probe++)
        {
            Vector3 origin = worldPosition + FootProbeOffsets[probe] + Vector3.up * FootProbeUp;
            int hitCount = RaycastDownWalkable(origin, rayLength, mask, out _);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = raycastHitBuffer[i];
                if (!IsWalkableGroundCollider(hit.collider))
                {
                    continue;
                }

                if (hit.normal.y < MinWalkableSurfaceNormalY)
                {
                    continue;
                }

                float hitY = hit.point.y;
                if (hitY < minAllowedY || hitY > maxAllowedY)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    bestY = hitY;
                    found = true;
                }
            }
        }

        if (found)
        {
            groundY = bestY;
        }

        return found;
    }

    // 아래로 쏜 레이 결과를 버퍼에 담아 Walkable 지면만 골라 bestY를 반환합니다.
    static int RaycastDownWalkable(Vector3 origin, float maxDistance, int layerMask, out float bestY)
    {
        bestY = float.MinValue;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            raycastHitBuffer,
            maxDistance,
            layerMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return 0;
        }

        if (hitCount >= raycastHitBuffer.Length)
        {
            raycastHitBuffer = new RaycastHit[hitCount + 4];
            hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                raycastHitBuffer,
                maxDistance,
                layerMask,
                QueryTriggerInteraction.Ignore);
        }

        int walkableHits = 0;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHitBuffer[i];
            if (!IsWalkableGroundCollider(hit.collider))
            {
                continue;
            }

            raycastHitBuffer[walkableHits] = hit;
            walkableHits++;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
            }
        }

        return walkableHits;
    }
}
