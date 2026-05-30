using System.Collections.Generic;
using UnityEngine;
// 카메라 → 캐릭터 직선에 걸리는 나무만 페이드 (가장 단순한 가림 판정).
[DefaultExecutionOrder(250)]
public class TreeOcclusionFadeController : MonoBehaviour
{
    static readonly int FadePropertyId = Shader.PropertyToID("_Fade");

    const string DefaultShaderPath = "Assets/Shaders/TreeOcclusionFade.shader";

    [Header("나무 시야 페이드")]
    [SerializeField] bool enableTreeOcclusionFade = true;

    [SerializeField] Transform followTarget;
    [SerializeField] Shader fadeShaderAsset;
    [SerializeField] float fadeSpeed = 7f;

    [Tooltip("100%일 때 나무가 거의 안 보이게 되는 최소 Fade 값")]
    [SerializeField] float minOccludedFade = 0.05f;

    [Tooltip("0=가릴 때 거의 안 투명, 100=최대 투명화 (Inspector에서 조절)")]
    [SerializeField] [Range(0f, 100f)] float occlusionTransparencyPercent = 40f;

    [Header("가림 판정")]
    [Tooltip("캐릭터 발 높이 (월드 Y 보정)")]
    [SerializeField] float playerFeetHeight = 0.15f;

    [Tooltip("캐릭터 가슴/머리 높이 — 직선 끝점 (나무 줄기 통과용)")]
    [SerializeField] float playerFocusHeight = 0.85f;

    [Tooltip("나무 박스를 XZ로 이만큼 넓혀 맞춤")]
    [SerializeField] float blockTestRadius = 0.1f;

    [Tooltip("플레이어에서 이 거리 안 나무만 검사")]
    [SerializeField] float occlusionCheckRadius = 22f;

    [SerializeField] float treeCacheInterval = 0.5f;

    readonly Dictionary<int, TreeFadeGroup> fadeGroups = new Dictionary<int, TreeFadeGroup>();
    readonly HashSet<int> fadingThisFrame = new HashSet<int>();
    readonly List<int> groupsToRemove = new List<int>();
    readonly List<Transform> cachedTrees = new List<Transform>();
    readonly HashSet<Transform> cachedTreeSet = new HashSet<Transform>();
    readonly Dictionary<int, Renderer> primaryRendererByTreeId = new Dictionary<int, Renderer>();

    Shader fadeShader;
    Camera targetCamera;
    float nextTreeCacheTime;

    static TreeOcclusionFadeController activeInstance;

    float GetOccludedFadeTarget()
    {
        return Mathf.Lerp(1f, minOccludedFade, occlusionTransparencyPercent / 100f);
    }

    sealed class TreeFadeGroup
    {
        public Renderer[] renderers;
        public Material[][] originalMaterials;
        public Material[][] fadeMaterials;
        public float currentFade = 1f;
        public float targetFade = 1f;
        public bool usingFadeMaterials;
    }

    void OnEnable()
    {
        activeInstance = this;
    }

    void OnDisable()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    public static void NotifyTreeSpawned(Transform treeRoot)
    {
        if (treeRoot == null || activeInstance == null)
        {
            return;
        }

        activeInstance.RegisterTree(treeRoot);
    }

    public void RegisterTree(Transform treeRoot)
    {
        if (treeRoot == null || !cachedTreeSet.Add(treeRoot))
        {
            return;
        }

        cachedTrees.Add(treeRoot);
        CachePrimaryRenderer(treeRoot);
    }

    void CachePrimaryRenderer(Transform treeRoot)
    {
        int key = treeRoot.GetInstanceID();
        if (primaryRendererByTreeId.ContainsKey(key))
        {
            return;
        }

        Renderer renderer = treeRoot.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = treeRoot.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            primaryRendererByTreeId[key] = renderer;
        }
    }

    void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        fadeShader = fadeShaderAsset;
        if (fadeShader == null)
        {
            fadeShader = Shader.Find("Game/TreeOcclusionFade");
        }

#if UNITY_EDITOR
        if (fadeShader == null)
        {
            fadeShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(DefaultShaderPath);
        }
#endif
    }

    void Start()
    {
        if (followTarget == null && GameSession.TryGetPlayerTransform(out Transform player))
        {
            followTarget = player;
        }

        if (cachedTrees.Count == 0)
        {
            RefreshTreeCache();
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null || followTarget == null || fadeShader == null)
        {
            return;
        }

        if (!enableTreeOcclusionFade)
        {
            RestoreAllFadeGroups();
            return;
        }

        Vector3 playerBase = GetPlayerBasePosition();
        Vector3 playerFeet = playerBase + Vector3.up * playerFeetHeight;
        Vector3 playerFocus = playerBase + Vector3.up * playerFocusHeight;
        Vector3 cameraPosition = targetCamera.transform.position;

        float maxCheckDistanceSqr = occlusionCheckRadius * occlusionCheckRadius;
        fadingThisFrame.Clear();

        for (int i = 0; i < cachedTrees.Count; i++)
        {
            Transform treeRoot = cachedTrees[i];
            if (treeRoot == null)
            {
                continue;
            }

            if ((treeRoot.position - playerBase).sqrMagnitude > maxCheckDistanceSqr)
            {
                continue;
            }

            int treeId = treeRoot.GetInstanceID();
            if (!primaryRendererByTreeId.TryGetValue(treeId, out Renderer renderer))
            {
                CachePrimaryRenderer(treeRoot);
                if (!primaryRendererByTreeId.TryGetValue(treeId, out renderer))
                {
                    continue;
                }
            }

            if (renderer == null)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            if (!BlocksView(bounds, cameraPosition, playerFeet, playerFocus, blockTestRadius))
            {
                continue;
            }

            int key = treeRoot.GetInstanceID();
            fadingThisFrame.Add(key);

            if (!fadeGroups.TryGetValue(key, out TreeFadeGroup group))
            {
                group = CreateFadeGroup(treeRoot);
                if (group == null)
                {
                    continue;
                }

                fadeGroups.Add(key, group);
            }

            group.targetFade = GetOccludedFadeTarget();
            EnsureFadeMaterials(group);
            group.currentFade = Mathf.MoveTowards(
                group.currentFade,
                group.targetFade,
                fadeSpeed * Time.deltaTime);
            ApplyFade(group);
        }

        groupsToRemove.Clear();
        foreach (KeyValuePair<int, TreeFadeGroup> pair in fadeGroups)
        {
            if (fadingThisFrame.Contains(pair.Key))
            {
                continue;
            }

            TreeFadeGroup group = pair.Value;
            group.targetFade = 1f;
            group.currentFade = Mathf.MoveTowards(group.currentFade, 1f, fadeSpeed * Time.deltaTime);
            ApplyFade(group);

            if (group.currentFade >= 0.999f && group.usingFadeMaterials)
            {
                RestoreOriginalMaterials(group);
            }

            if (group.currentFade >= 0.999f && !group.usingFadeMaterials)
            {
                groupsToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < groupsToRemove.Count; i++)
        {
            fadeGroups.Remove(groupsToRemove[i]);
        }
    }

    void RefreshTreeCache()
    {
        nextTreeCacheTime = Time.time + treeCacheInterval;
        cachedTrees.Clear();
        cachedTreeSet.Clear();
        primaryRendererByTreeId.Clear();

        GameObject[] trees = GameObject.FindGameObjectsWithTag(PropCollisionLayers.TreeObstacleTag);
        for (int i = 0; i < trees.Length; i++)
        {
            if (trees[i] != null)
            {
                RegisterTree(trees[i].transform);
            }
        }
    }

    /// <summary>
    /// 카메라에서 캐릭터(발/가슴)로 쏜 직선이 나무에 맞으면 true.
    /// </summary>
    static bool BlocksView(
        Bounds bounds,
        Vector3 cameraPosition,
        Vector3 playerFeet,
        Vector3 playerFocus,
        float inflateRadius)
    {
        Bounds testBounds = bounds;
        if (inflateRadius > 0f)
        {
            testBounds.Expand(new Vector3(inflateRadius * 2f, 0f, inflateRadius * 2f));
        }

        if (testBounds.Contains(playerFeet) || testBounds.Contains(playerFocus))
        {
            return true;
        }

        return HitsSegment(testBounds, cameraPosition, playerFeet)
            || HitsSegment(testBounds, cameraPosition, playerFocus);
    }

    static bool HitsSegment(Bounds bounds, Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.001f)
        {
            return false;
        }

        Ray ray = new Ray(from, direction / distance);
        return bounds.IntersectRay(ray, out float hitDistance) && hitDistance <= distance;
    }

    Vector3 GetPlayerBasePosition()
    {
        Vector3 position = followTarget.position;
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 tracked))
        {
            position = tracked;
        }

        return position;
    }

    TreeFadeGroup CreateFadeGroup(Transform treeRoot)
    {
        Renderer[] renderers = treeRoot.GetComponentsInChildren<Renderer>(false);
        if (renderers == null || renderers.Length == 0)
        {
            return null;
        }

        var group = new TreeFadeGroup
        {
            renderers = renderers,
            originalMaterials = new Material[renderers.Length][],
            fadeMaterials = new Material[renderers.Length][],
            currentFade = 1f,
            targetFade = 1f,
            usingFadeMaterials = false
        };

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] sourceMaterials = renderers[i].materials;
            group.originalMaterials[i] = sourceMaterials;
            group.fadeMaterials[i] = new Material[sourceMaterials.Length];
        }

        return group;
    }

    void EnsureFadeMaterials(TreeFadeGroup group)
    {
        if (group.usingFadeMaterials)
        {
            return;
        }

        for (int i = 0; i < group.renderers.Length; i++)
        {
            Renderer renderer = group.renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] sourceMaterials = group.originalMaterials[i];
            Material[] fadeMaterials = group.fadeMaterials[i];

            for (int m = 0; m < sourceMaterials.Length; m++)
            {
                fadeMaterials[m] = CreateFadeMaterial(sourceMaterials[m]);
            }

            renderer.materials = fadeMaterials;
        }

        group.usingFadeMaterials = true;
    }

    Material CreateFadeMaterial(Material source)
    {
        Material fadeMaterial = new Material(fadeShader);
        fadeMaterial.name = source != null ? source.name + "_OcclusionFade" : "TreeOcclusionFade";

        if (source != null)
        {
            if (source.HasProperty("_BaseMap") && fadeMaterial.HasProperty("_BaseMap"))
            {
                fadeMaterial.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            }
            else if (source.HasProperty("_MainTex") && fadeMaterial.HasProperty("_BaseMap"))
            {
                fadeMaterial.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
            }

            if (source.HasProperty("_BaseColor"))
            {
                fadeMaterial.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            }
            else if (source.HasProperty("_Color"))
            {
                fadeMaterial.SetColor("_BaseColor", source.GetColor("_Color"));
            }
        }

        fadeMaterial.SetFloat(FadePropertyId, 1f);
        return fadeMaterial;
    }

    static void ApplyFade(TreeFadeGroup group)
    {
        if (!group.usingFadeMaterials)
        {
            return;
        }

        float fade = Mathf.Clamp01(group.currentFade);

        for (int i = 0; i < group.fadeMaterials.Length; i++)
        {
            Material[] materials = group.fadeMaterials[i];
            if (materials == null)
            {
                continue;
            }

            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null)
                {
                    materials[m].SetFloat(FadePropertyId, fade);
                }
            }
        }
    }

    static void RestoreOriginalMaterials(TreeFadeGroup group)
    {
        for (int i = 0; i < group.renderers.Length; i++)
        {
            Renderer renderer = group.renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.materials = group.originalMaterials[i];
        }

        group.usingFadeMaterials = false;
        group.currentFade = 1f;
        group.targetFade = 1f;
    }

    void RestoreAllFadeGroups()
    {
        if (fadeGroups.Count == 0)
        {
            return;
        }

        groupsToRemove.Clear();
        foreach (KeyValuePair<int, TreeFadeGroup> pair in fadeGroups)
        {
            TreeFadeGroup group = pair.Value;
            if (group.usingFadeMaterials)
            {
                RestoreOriginalMaterials(group);
            }

            groupsToRemove.Add(pair.Key);
        }

        for (int i = 0; i < groupsToRemove.Count; i++)
        {
            fadeGroups.Remove(groupsToRemove[i]);
        }

        fadingThisFrame.Clear();
    }

    void OnDestroy()
    {
        foreach (KeyValuePair<int, TreeFadeGroup> pair in fadeGroups)
        {
            TreeFadeGroup group = pair.Value;
            if (group.usingFadeMaterials)
            {
                RestoreOriginalMaterials(group);
            }

            DestroyFadeMaterialInstances(group);
        }

        fadeGroups.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (followTarget == null)
        {
            return;
        }

        Camera cam = targetCamera != null ? targetCamera : GetComponent<Camera>();
        if (cam == null)
        {
            return;
        }

        Vector3 playerBase = GetPlayerBasePosition();
        Vector3 feet = playerBase + Vector3.up * playerFeetHeight;
        Vector3 focus = playerBase + Vector3.up * playerFocusHeight;
        Vector3 camPos = cam.transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(camPos, feet);
        Gizmos.DrawLine(camPos, focus);
        Gizmos.DrawWireSphere(feet, 0.25f);
        Gizmos.DrawWireSphere(focus, 0.25f);
    }

    void OnValidate()
    {
        blockTestRadius = Mathf.Clamp(blockTestRadius, 0f, 4f);
        occlusionCheckRadius = Mathf.Clamp(occlusionCheckRadius, 6f, 50f);
        minOccludedFade = Mathf.Clamp(minOccludedFade, 0.02f, 0.5f);
        occlusionTransparencyPercent = Mathf.Clamp(occlusionTransparencyPercent, 0f, 100f);
        playerFeetHeight = Mathf.Max(0f, playerFeetHeight);
        playerFocusHeight = Mathf.Max(playerFeetHeight, playerFocusHeight);

        if (fadeShaderAsset == null)
        {
            fadeShaderAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(DefaultShaderPath);
        }
    }
#endif

    static void DestroyFadeMaterialInstances(TreeFadeGroup group)
    {
        for (int i = 0; i < group.fadeMaterials.Length; i++)
        {
            Material[] materials = group.fadeMaterials[i];
            if (materials == null)
            {
                continue;
            }

            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null)
                {
                    Object.Destroy(materials[m]);
                }
            }
        }
    }
}
