using UnityEngine;

// 캡슐 충돌을 쓰는 캐릭터·몬스터 공통 이동입니다.
public static class CapsuleMotor
{
    public struct Settings
    {
        public float radius;
        public float height;
        public float groundY;
        public float skin;
        public float embeddedIgnoreDistance;
    }

    const float MinMoveSqr = 0.0000001f;

    static readonly float[] SteeringAngleOffsets =
    {
        0f, 25f, -25f, 50f, -50f, 75f, -75f, 100f, -100f
    };

    public static void Move(Transform body, Vector3 delta, Settings settings, Transform ignoreRoot, MoverRole role)
    {
        if (body == null || delta.sqrMagnitude < MinMoveSqr)
        {
            return;
        }

        delta.y = 0f;
        float totalDistance = delta.magnitude;
        if (totalDistance < MinMoveSqr)
        {
            return;
        }

        Vector3 direction = delta / totalDistance;
        float moved = TryMoveSegment(body.position, direction * totalDistance, settings, ignoreRoot, role, out Vector3 newPosition);

        if (moved < MinMoveSqr)
        {
            return;
        }

        newPosition.y = settings.groundY;
        body.position = newPosition;
    }

    public static Vector3 FindSteeringDirection(
        Vector3 startPosition,
        Vector3 goalDirection,
        float moveDistance,
        Settings settings,
        Transform ignoreRoot,
        MoverRole role)
    {
        goalDirection.y = 0f;
        if (goalDirection.sqrMagnitude < MinMoveSqr || moveDistance < MinMoveSqr)
        {
            return Vector3.zero;
        }

        goalDirection.Normalize();

        float bestScore = -1f;
        Vector3 bestDirection = Vector3.zero;
        float minUsefulMove = moveDistance * 0.12f;
        float probeDistance = moveDistance * 1.5f;

        for (int i = 0; i < SteeringAngleOffsets.Length; i++)
        {
            Vector3 candidate = Quaternion.Euler(0f, SteeringAngleOffsets[i], 0f) * goalDirection;
            float allowed = GetAllowedMoveDistance(startPosition, candidate, probeDistance, settings, ignoreRoot, role);

            if (allowed < minUsefulMove)
            {
                continue;
            }

            float towardGoal = Vector3.Dot(candidate, goalDirection);
            float score = allowed * 2f + towardGoal * moveDistance;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = candidate;
            }
        }

        if (bestScore >= 0f)
        {
            return bestDirection;
        }

        return GetWallSlideDirection(startPosition, goalDirection, moveDistance, settings, ignoreRoot, role);
    }

    static Vector3 GetWallSlideDirection(
        Vector3 startPosition,
        Vector3 goalDirection,
        float moveDistance,
        Settings settings,
        Transform ignoreRoot,
        MoverRole role)
    {
        GetCapsuleEnds(startPosition, settings, out Vector3 bottom, out Vector3 top);

        if (!Physics.CapsuleCast(
                bottom,
                top,
                settings.radius,
                goalDirection,
                out RaycastHit hit,
                moveDistance + settings.skin,
                WorldCollision.QueryLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return goalDirection;
        }

        if (!WorldCollision.BlocksMovement(hit.collider, ignoreRoot, role))
        {
            return goalDirection;
        }

        Vector3 slide = Vector3.ProjectOnPlane(goalDirection, hit.normal);
        slide.y = 0f;

        if (slide.sqrMagnitude < MinMoveSqr)
        {
            return Vector3.zero;
        }

        slide.Normalize();
        float allowed = GetAllowedMoveDistance(startPosition, slide, moveDistance, settings, ignoreRoot, role);
        if (allowed < moveDistance * 0.08f)
        {
            return Vector3.zero;
        }

        return slide;
    }

    public static float GetAllowedMoveDistance(
        Vector3 startPosition,
        Vector3 direction,
        float maxDistance,
        Settings settings,
        Transform ignoreRoot,
        MoverRole role)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < MinMoveSqr || maxDistance < MinMoveSqr)
        {
            return 0f;
        }

        direction.Normalize();
        TryMoveSegment(startPosition, direction * maxDistance, settings, ignoreRoot, role, out Vector3 endPosition);

        Vector3 flatDelta = endPosition - startPosition;
        flatDelta.y = 0f;
        return flatDelta.magnitude;
    }

    static float TryMoveSegment(
        Vector3 startPosition,
        Vector3 delta,
        Settings settings,
        Transform ignoreRoot,
        MoverRole role,
        out Vector3 endPosition)
    {
        endPosition = startPosition;
        float maxDistance = delta.magnitude;
        if (maxDistance < MinMoveSqr)
        {
            return 0f;
        }

        Vector3 direction = delta / maxDistance;
        GetCapsuleEnds(startPosition, settings, out Vector3 bottom, out Vector3 top);

        if (WorldCollision.QueryLayerMask == 0)
        {
            endPosition = startPosition + direction * maxDistance;
            return maxDistance;
        }

        RaycastHit[] hits = Physics.CapsuleCastAll(
            bottom,
            top,
            settings.radius,
            direction,
            maxDistance + settings.skin,
            WorldCollision.QueryLayerMask,
            QueryTriggerInteraction.Ignore);

        float allowed = maxDistance;
        bool blocked = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider blocker = hits[i].collider;
            if (!WorldCollision.BlocksMovement(blocker, ignoreRoot, role))
            {
                continue;
            }

            if (hits[i].distance <= settings.embeddedIgnoreDistance)
            {
                if (IsInsideCollider(startPosition, blocker))
                {
                    blocked = true;
                    allowed = 0f;
                    continue;
                }

                if (!IsMovingIntoCollider(startPosition, direction, blocker))
                {
                    continue;
                }
            }

            blocked = true;
            allowed = Mathf.Min(allowed, hits[i].distance);
        }

        if (blocked)
        {
            allowed = Mathf.Max(0f, allowed - settings.skin);
        }

        endPosition = startPosition + direction * allowed;
        return allowed;
    }

    static bool IsInsideCollider(Vector3 startPosition, Collider blocker)
    {
        Vector3 closest = blocker.ClosestPoint(startPosition);
        Vector3 pushOut = startPosition - closest;
        pushOut.y = 0f;
        return pushOut.sqrMagnitude < 0.0004f;
    }

    static bool IsMovingIntoCollider(Vector3 startPosition, Vector3 direction, Collider blocker)
    {
        Vector3 closest = blocker.ClosestPoint(startPosition);
        Vector3 pushOut = startPosition - closest;
        pushOut.y = 0f;

        if (pushOut.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        return Vector3.Dot(direction, pushOut.normalized) < 0f;
    }

    static void GetCapsuleEnds(Vector3 feetPosition, Settings settings, out Vector3 bottom, out Vector3 top)
    {
        float cylinderHeight = Mathf.Max(settings.height - settings.radius * 2f, 0.05f);
        bottom = feetPosition + Vector3.up * settings.radius;
        top = bottom + Vector3.up * cylinderHeight;
    }
}
