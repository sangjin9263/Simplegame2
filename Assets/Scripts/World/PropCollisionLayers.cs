using UnityEngine;



// 나무 등 플레이어가 통과하면 안 되는 오브젝트 레이어·태그를 맞춥니다.

public static class PropCollisionLayers

{

    const string ObstacleLayerName = "Obstacle";

    const string GroundLayerName = "Ground";

    public const string TreeObstacleTag = "TreeObstacle";



    static readonly Vector3 TrunkColliderSize = new Vector3(0.5f, 1.05f, 0.5f);

    static readonly Vector3 TrunkColliderCenter = new Vector3(0f, 0.52f, 0f);



    static int cachedLayer = int.MinValue;



    public static int ObstacleLayer

    {

        get

        {

            if (cachedLayer == int.MinValue)

            {

                cachedLayer = LayerMask.NameToLayer(ObstacleLayerName);

            }



            return cachedLayer;

        }

    }



    public static void ApplyToRoot(GameObject root)

    {

        if (root == null)

        {

            return;

        }



        try

        {

            root.tag = TreeObstacleTag;

        }

        catch (UnityException)

        {

            // TagManager에 태그가 없을 때는 레이어만 적용합니다.

        }



        int layer = ObstacleLayer;

        if (layer >= 0)

        {

            ApplyLayerRecursive(root.transform, layer);

        }



        RemoveOcclusionFadeVolume(root);



        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)

        {

            colliders[i].enabled = true;

            colliders[i].isTrigger = false;



            if (colliders[i] is BoxCollider box)

            {

                box.size = TrunkColliderSize;

                box.center = TrunkColliderCenter;

            }

        }



        TreeOcclusionFadeController.NotifyTreeSpawned(root.transform);

    }



    static void RemoveOcclusionFadeVolume(GameObject root)

    {

        Transform existing = root.transform.Find("OcclusionFadeVolume");

        if (existing == null)

        {

            return;

        }



        if (Application.isPlaying)

        {

            Object.Destroy(existing.gameObject);

        }

    }



    public static bool IsTreeObstacleCollider(Collider collider)

    {

        if (collider == null || !collider.enabled)

        {

            return false;

        }



        if (collider.CompareTag(TreeObstacleTag))

        {

            return true;

        }



        int obstacle = ObstacleLayer;

        return obstacle >= 0 && collider.gameObject.layer == obstacle;

    }



    public static int GroundLayer => GroundHeightSampler.GroundLayer;



    // WalkableTerrainFeature 바닥 — Ground 레이어 콜라이더를 켭니다.

    public static void EnableWalkableTerrainColliders(GameObject root)

    {

        if (root == null)

        {

            return;

        }



        int layer = GroundLayer;

        if (layer >= 0)

        {

            ApplyLayerRecursive(root.transform, layer);

        }



        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)

        {

            colliders[i].enabled = true;

            colliders[i].isTrigger = false;

        }



        if (IsSlopeTerrainRoot(root))

        {

            ConfigureSlopeStepColliders(root);

        }

    }



    static bool IsSlopeTerrainRoot(GameObject root)

    {

        return root != null && root.name.Contains("Slope");

    }



    // Slope는 보이는 경사 메쉬(MeshCollider)만 쓰고, Cube 계단 콜라이더는 끕니다.

    static void ConfigureSlopeStepColliders(GameObject root)

    {

        MeshCollider meshCollider = root.GetComponent<MeshCollider>();

        if (meshCollider == null)

        {

            meshCollider = root.AddComponent<MeshCollider>();

        }



        MeshFilter meshFilter = root.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)

        {

            meshCollider.sharedMesh = meshFilter.sharedMesh;

        }



        meshCollider.convex = false;

        meshCollider.enabled = true;



        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)

        {

            if (colliders[i] == meshCollider)

            {

                continue;

            }



            colliders[i].enabled = false;

        }

    }



    // 바닥 타일 콜라이더는 이동 검사에서 제외합니다.

    public static void DisableFloorColliders(GameObject root)

    {

        if (root == null)

        {

            return;

        }



        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)

        {

            colliders[i].enabled = false;

        }

    }



    static void ApplyLayerRecursive(Transform target, int layer)

    {

        target.gameObject.layer = layer;

        for (int i = 0; i < target.childCount; i++)

        {

            ApplyLayerRecursive(target.GetChild(i), layer);

        }

    }

}

