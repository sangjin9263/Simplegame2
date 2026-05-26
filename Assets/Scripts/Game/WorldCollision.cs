using UnityEngine;

// 누가 이동하는지에 따라 막는 대상이 달라집니다.
public enum MoverRole
{
    Player,
    Monster
}

// 나무·몬스터·플레이어 등 이동을 막는 대상을 판별합니다.
public static class WorldCollision
{
    public const string TreeObstacleTag = "TreeObstacle";
    public const string MonsterTag = "Monster";
    public const string PlayerTag = "Player";

    const string ObstacleLayerName = "Obstacle";

    static int cachedObstacleLayer = int.MinValue;

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

    // CapsuleCast에 쓸 레이어 마스크입니다 (나무 Obstacle만 검사).
    public static int QueryLayerMask
    {
        get
        {
            int obstacle = ObstacleLayer;
            if (obstacle >= 0)
            {
                return 1 << obstacle;
            }

            return 0;
        }
    }

    // 이 콜라이더가 이동을 막는지 확인합니다 (자기 자신 제외).
    public static bool BlocksMovement(Collider collider, Transform moverRoot, MoverRole role)
    {
        if (collider == null || !collider.enabled)
        {
            return false;
        }

        if (moverRoot != null)
        {
            if (collider.transform == moverRoot || collider.transform.IsChildOf(moverRoot))
            {
                return false;
            }
        }

        if (!PropCollisionLayers.IsTreeObstacleCollider(collider))
        {
            return false;
        }

        // 나무는 플레이어·몬스터 모두 막습니다.
        return true;
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
