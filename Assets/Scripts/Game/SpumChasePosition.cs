using UnityEngine;

// SPUM 캐릭터 추격/공격 거리 계산용 몸통 중심 (XZ).
public static class SpumChasePosition
{
    public static Vector3 GetFlatChasePoint(Transform root)
    {
        if (root == null)
        {
            return Vector3.zero;
        }

        Transform anchor = ResolveBodyAnchor(root);
        Vector3 world = anchor.position;
        if (anchor is RectTransform rectTransform)
        {
            world = rectTransform.TransformPoint(Vector3.zero);
        }

        return new Vector3(world.x, 0f, world.z);
    }

    public static float GetCollisionRadius(Transform root)
    {
        if (root == null)
        {
            return 0f;
        }

        CharacterController controller = root.GetComponent<CharacterController>();
        return controller != null && controller.enabled ? controller.radius : 0f;
    }

    static Transform ResolveBodyAnchor(Transform root)
    {
        Transform unitRoot = root.Find("UnitRoot");
        if (unitRoot != null)
        {
            return unitRoot;
        }

        SPUM_Prefabs spumPrefabs = root.GetComponentInChildren<SPUM_Prefabs>();
        if (spumPrefabs != null)
        {
            return spumPrefabs.transform;
        }

        return root;
    }
}
