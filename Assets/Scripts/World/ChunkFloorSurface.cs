using UnityEngine;

// 청크 바닥 프리팹 루트(Y)에서 실제로 걸을 수 있는 윗면까지의 높이입니다.
public static class ChunkFloorSurface
{
    const float MainLandSurfaceOffset = 2f;

    public static float GetSurfaceYOffsetAboveChunkRoot(GameObject floorPrefab)
    {
        if (floorPrefab == null)
        {
            return 0f;
        }

        string prefabName = floorPrefab.name;
        if (prefabName.Contains("Main_land"))
        {
            return MainLandSurfaceOffset;
        }

        if (prefabName.Contains("High_land"))
        {
            return 4f;
        }

        if (prefabName.Contains("Slope"))
        {
            return 0f;
        }

        return EstimateFromColliders(floorPrefab);
    }

    public static float ResolveSurfaceY(Vector3 worldPosition, float chunkRootY, GameObject floorPrefab)
    {
        float analyticY = chunkRootY + GetSurfaceYOffsetAboveChunkRoot(floorPrefab);
        float sampledY = GroundHeightSampler.GetSurfaceY(worldPosition, analyticY);
        return Mathf.Max(analyticY, sampledY);
    }

    static float EstimateFromColliders(GameObject floorPrefab)
    {
        Collider[] colliders = floorPrefab.GetComponentsInChildren<Collider>(true);
        if (colliders == null || colliders.Length == 0)
        {
            return 0f;
        }

        float maxTop = 0f;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            float top = GetColliderTopLocalY(collider);
            if (top > maxTop)
            {
                maxTop = top;
            }
        }

        return maxTop;
    }

    static float GetColliderTopLocalY(Collider collider)
    {
        if (collider is BoxCollider box)
        {
            return box.center.y + box.size.y * 0.5f;
        }

        if (collider is CapsuleCollider capsule)
        {
            return capsule.center.y + capsule.height * 0.5f;
        }

        if (collider is SphereCollider sphere)
        {
            return sphere.center.y + sphere.radius;
        }

        return collider.bounds.max.y;
    }

    // 나무 메시 밑부분을 지정한 월드 Y에 맞춥니다.
    public static void AlignPropVisualBaseToWorldY(GameObject root, float targetBaseWorldY)
    {
        if (root == null)
        {
            return;
        }

        if (!TryGetPropVisualBottomWorldY(root, out float bottomWorldY))
        {
            return;
        }

        float delta = targetBaseWorldY - bottomWorldY;
        if (Mathf.Abs(delta) < 0.001f)
        {
            return;
        }

        Vector3 position = root.transform.position;
        position.y += delta;
        root.transform.position = position;
    }

    static bool TryGetPropVisualBottomWorldY(GameObject root, out float bottomWorldY)
    {
        bottomWorldY = root.transform.position.y;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        float minY = float.MaxValue;
        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            minY = Mathf.Min(minY, renderer.bounds.min.y);
            found = true;
        }

        if (!found)
        {
            return false;
        }

        bottomWorldY = minY;
        return true;
    }
}
