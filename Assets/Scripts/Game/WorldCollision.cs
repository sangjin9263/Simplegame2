using UnityEngine;

// 나무·몬스터·플레이어 등 충돌 관련 유틸리티입니다.
public static class WorldCollision
{
    public const string TreeObstacleTag = "TreeObstacle";
    public const string MonsterTag = "Monster";
    public const string PlayerTag = "Player";

    const string ObstacleLayerName = "Obstacle";

    static int cachedObstacleLayer = int.MinValue;
    static int cachedObstacleQueryMask = int.MinValue;

    public static int ObstacleLayer
    {
        get
        {
            if (cachedObstacleLayer == int.MinValue)
            {
                cachedObstacleLayer = LayerMask.NameToLayer(ObstacleLayerName);
            }

            return cachedObstacleLayer;
        }
    }

    // 나무 Obstacle 레이어 마스크입니다 (CharacterController가 Ground는 직접 처리).
    public static int QueryLayerMask
    {
        get
        {
            if (cachedObstacleQueryMask != int.MinValue)
            {
                return cachedObstacleQueryMask;
            }

            int obstacle = ObstacleLayer;
            cachedObstacleQueryMask = obstacle >= 0 ? 1 << obstacle : 0;
            return cachedObstacleQueryMask;
        }
    }

    public static void ApplyTreeObstacle(GameObject root)
    {
        PropCollisionLayers.ApplyToRoot(root);
    }

    public static void ApplyMonster(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        try
        {
            root.tag = MonsterTag;
        }
        catch (UnityException)
        {
        }

        root.layer = 0;
        SetLayerRecursive(root.transform, 0);
    }

    static void SetLayerRecursive(Transform target, int layer)
    {
        target.gameObject.layer = layer;
        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursive(target.GetChild(i), layer);
        }
    }
}
