using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스폰된 오브젝트와 원본 프리팹을 짝지어 둡니다 (풀 반환용).
public struct SpawnedPropRecord
{
    public GameObject instance;
    public GameObject prefab;
}

// 현재 씬에 살아 있는 청크 하나의 정보입니다.
public class ChunkInstance
{
    public ChunkCoordinate coordinate;
    public GameObject floorObject;
    public GameObject floorPrefabUsed;
    public float floorWorldY;
    public List<SpawnedPropRecord> spawnedProps = new List<SpawnedPropRecord>();

    // 이 청크에 깐 나무의 월드 XZ (청크 제거 시 전역 겹침 인덱스에서 빼기 위함).
    public readonly List<Vector3> treePlacementPositions = new List<Vector3>();

    public ChunkInstance(ChunkCoordinate coordinate)
    {
        this.coordinate = coordinate;
    }

    public void AddSpawnedProp(GameObject instance, GameObject prefab)
    {
        if (instance == null || prefab == null)
        {
            return;
        }

        spawnedProps.Add(new SpawnedPropRecord { instance = instance, prefab = prefab });
    }

    public void ClearSpawnedList()
    {
        spawnedProps.Clear();
    }
}

// 플레이어 주변에 바닥 청크를 무한히 생성·제거하는 스트리머입니다 (뱀서라이크 맵).
[DefaultExecutionOrder(-200)]
public class WorldChunkStreamer : MonoBehaviour
{
    // 청크 한 칸의 가로·세로 크기(미터)입니다. Floor_Grass_Bright = 6.
    [SerializeField] float chunkSize = 6f;

    // 플레이어 기준으로 몇 칸까지 바닥을 유지할지입니다 (12 = 25x25, 약 150m 너비).
    [SerializeField] int viewRadius = 12;

    [SerializeField] WorldSettings worldSettings;

    // 청크 내용 정의(바닥 프리팹, 나중에 오브젝트 목록).
    [SerializeField] ChunkDefinition chunkDefinition;

    // Definition이 비었을 때 쓸 바닥 프리팹 (인스펙터에 넣어 두세요).
    [SerializeField] GameObject fallbackFloorPrefab;

    // 따라갈 플레이어입니다. 비우면 Player 태그를 찾습니다.
    [SerializeField] Transform playerTransform;

    // 생성된 청크들의 부모 오브젝트입니다.
    [SerializeField] Transform chunksParent;

    // 풀에 미리 만들어 둘 바닥 개수입니다 (너무 크면 Play 시 멈춤).
    [SerializeField] int prewarmCount = 80;

    // 나무 풀에 미리 넣을 개수(프리팹 종류당)입니다.
    [SerializeField] int propPrewarmCountPerPrefab = 50;

    // 한 프레임에 생성할 청크 수입니다 (나눠서 로드).
    [SerializeField] int chunksPerFrame = 35;

    // 지금 활성인 청크들입니다 (좌표 → 인스턴스).
    readonly Dictionary<Vector2Int, ChunkInstance> activeChunks = new Dictionary<Vector2Int, ChunkInstance>();

    // 오브젝트 재사용 풀입니다.
    ChunkPool chunkPool;

    // 마지막으로 계산한 플레이어 청크 좌표입니다.
    ChunkCoordinate lastPlayerChunk;

    // 플레이어 청크가 바뀌었는지 여부입니다.
    bool playerChunkChanged;

    // 아직 생성하지 않은 청크 목록입니다.
    readonly List<ChunkCoordinate> pendingChunkSpawns = new List<ChunkCoordinate>();

    // RefreshAllChunks마다 new 하지 않고 재사용해 GC 스파이크를 줄입니다.
    readonly HashSet<Vector2Int> neededChunkCoordsScratch = new HashSet<Vector2Int>();
    readonly List<Vector2Int> chunksToRemoveScratch = new List<Vector2Int>();

    Coroutine chunkSpawnCoroutine;

    const string DefaultMainLandPrefabPath = "Assets/Prefabs/Land/Main_land.prefab";

    void Awake()
    {
        WorldLoadCoordinator.ResetForPlay();
        GameSession.ResetForPlay();
        ChunkFloorGroundRegistry.Clear();
        GameSession.BindWorldSettings(worldSettings);
    }

    // 시작 시 바닥 청크를 먼저 모두 깔고, 이후 몬스터/아이템이 스폰되도록 신호를 보냅니다.
    void Start()
    {
        StartCoroutine(BootstrapWorldRoutine());
    }

    IEnumerator BootstrapWorldRoutine()
    {
        InitializeStreamer();
        EnsurePlayerGameplayGate();
        DisableLegacyFloorCollidersInScene();
        EnableWalkableTerrainCollidersInScene();
        ClearStaleChunkChildren();
        activeChunks.Clear();
        ChunkTreePlacementIndex.Clear();

        yield return null;

        yield return SpawnInitialChunksAroundPlayerRoutine();
        WorldLoadCoordinator.NotifyWorldReady();
    }

    void EnsurePlayerGameplayGate()
    {
        if (playerTransform == null)
        {
            return;
        }

        if (playerTransform.GetComponent<PlayerGameplayStartGate>() == null)
        {
            Debug.LogWarning(
                "[WorldChunkStreamer] PlayerGameplayStartGate is missing on player. "
                + "Run Simplegame2/Setup Gameplay Prefabs.",
                playerTransform);
        }
    }

    IEnumerator SpawnInitialChunksAroundPlayerRoutine()
    {
        if (playerTransform == null || chunkDefinition == null)
        {
            yield break;
        }

        ChunkCoordinate center = ChunkCoordinate.FromWorldPosition(playerTransform.position, chunkSize);
        pendingChunkSpawns.Clear();

        for (int offsetX = -viewRadius; offsetX <= viewRadius; offsetX++)
        {
            for (int offsetZ = -viewRadius; offsetZ <= viewRadius; offsetZ++)
            {
                ChunkCoordinate coord = new ChunkCoordinate(center.x + offsetX, center.z + offsetZ);
                Vector2Int key = coord.ToVector2Int();
                if (!activeChunks.ContainsKey(key))
                {
                    pendingChunkSpawns.Add(coord);
                }
            }
        }

        int spawnBudget = Mathf.Max(1, chunksPerFrame);
        while (pendingChunkSpawns.Count > 0)
        {
            int spawnCountThisFrame = Mathf.Min(spawnBudget, pendingChunkSpawns.Count);
            for (int i = 0; i < spawnCountThisFrame; i++)
            {
                ChunkCoordinate coord = pendingChunkSpawns[0];
                pendingChunkSpawns.RemoveAt(0);
                SpawnChunk(coord);
            }

            yield return null;
        }
    }

    // 씬에 남아 있는 옛 바닥 타일 콜라이더를 끕니다 (이동 검사 방해 방지).
    void DisableLegacyFloorCollidersInScene()
    {
        if (chunksParent == null)
        {
            return;
        }

        Transform sceneRoot = chunksParent.parent != null ? chunksParent.parent : chunksParent;
        Collider[] colliders = sceneRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            if (collider.transform.IsChildOf(chunksParent))
            {
                continue;
            }

            if (!collider.name.Contains("Floor_Grass") && !collider.gameObject.name.Contains("Floor_Grass"))
            {
                continue;
            }

            if (collider.GetComponentInParent<WalkableTerrainFeature>() != null)
            {
                continue;
            }

            collider.enabled = false;
        }
    }

    // StaticTerrain 등 씬에 둔 WalkableTerrainFeature 콜라이더를 켭니다.
    void EnableWalkableTerrainCollidersInScene()
    {
        WalkableTerrainFeature[] features = FindObjectsByType<WalkableTerrainFeature>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < features.Length; i++)
        {
            if (features[i] != null)
            {
                PropCollisionLayers.EnableWalkableTerrainColliders(features[i].gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (chunkSpawnCoroutine != null)
        {
            StopCoroutine(chunkSpawnCoroutine);
        }
    }

    // 이전 Play에서 저장된 청크 자식을 비웁니다 (중복·멈춤 방지).
    void ClearStaleChunkChildren()
    {
        if (chunksParent == null)
        {
            return;
        }

        for (int i = chunksParent.childCount - 1; i >= 0; i--)
        {
            Destroy(chunksParent.GetChild(i).gameObject);
        }
    }

    // 스트리머가 처음 쓸 때 필요한 오브젝트를 준비합니다.
    void InitializeStreamer()
    {
        EnsureChunkDefinitionValid();

        if (playerTransform == null && GameSession.TryGetPlayerTransform(out Transform player))
        {
            playerTransform = player;
        }

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (chunksParent == null)
        {
            Transform existing = transform.Find("WorldChunks");
            if (existing != null)
            {
                chunksParent = existing;
            }
            else
            {
                GameObject parentObject = new GameObject("WorldChunks");
                parentObject.transform.SetParent(transform, false);
                chunksParent = parentObject.transform;
            }
        }
        else if (chunksParent.parent != transform)
        {
            chunksParent.SetParent(transform, false);
        }

        if (chunkPool == null)
        {
            Transform existingPool = transform.Find("ChunkPool");
            Transform poolParent = existingPool != null ? existingPool : new GameObject("ChunkPool").transform;
            poolParent.SetParent(transform, false);
            GameObject poolRootObject = poolParent.gameObject;
            chunkPool = new ChunkPool(poolRootObject.transform);

            PrewarmFloorPrefabs();
            PrewarmRandomPropPrefabs();
            PrewarmTerrainFeaturePrefabs();
        }
    }

    void PrewarmFloorPrefabs()
    {
        if (chunkDefinition == null || chunkPool == null)
        {
            return;
        }

        int perPrefabCount = Mathf.Max(8, prewarmCount / 2);
        foreach (GameObject prefab in chunkDefinition.EnumerateFloorPrefabsForPool())
        {
            chunkPool.Prewarm(prefab, perPrefabCount);
        }
    }

    void PrewarmTerrainFeaturePrefabs()
    {
        if (chunkDefinition == null || chunkDefinition.terrainFeatureRules == null)
        {
            return;
        }

        HashSet<GameObject> warmed = new HashSet<GameObject>();

        for (int i = 0; i < chunkDefinition.terrainFeatureRules.Count; i++)
        {
            ChunkTerrainFeatureRule rule = chunkDefinition.terrainFeatureRules[i];
            if (rule == null || !rule.enabled || warmed.Contains(rule.prefab))
            {
                continue;
            }

            if (!ChunkPrefabUtility.TryGetInstantiablePrefab(rule.prefab, out GameObject terrainPrefab))
            {
                Debug.LogWarning("[WorldChunkStreamer] Terrain feature prefab is invalid.");
                continue;
            }

            rule.prefab = terrainPrefab;
            if (warmed.Contains(terrainPrefab))
            {
                continue;
            }

            warmed.Add(terrainPrefab);
            chunkPool.Prewarm(terrainPrefab, 12);
        }
    }

    // Definition에 등록된 랜덤 오브젝트 프리팹을 풀에 미리 채웁니다.
    void PrewarmRandomPropPrefabs()
    {
        if (chunkDefinition == null || chunkDefinition.randomPropRules == null)
        {
            return;
        }

        HashSet<GameObject> warmed = new HashSet<GameObject>();

        for (int r = 0; r < chunkDefinition.randomPropRules.Count; r++)
        {
            ChunkRandomPropRule rule = chunkDefinition.randomPropRules[r];
            if (rule == null || !rule.enabled || rule.prefabs == null)
            {
                continue;
            }

            for (int p = 0; p < rule.prefabs.Length; p++)
            {
                GameObject prefab = rule.prefabs[p];
                if (prefab == null || warmed.Contains(prefab))
                {
                    continue;
                }

                warmed.Add(prefab);
                chunkPool.Prewarm(prefab, propPrewarmCountPerPrefab);
            }
        }
    }

    // 매 프레임 플레이어 청크가 바뀌었는지 확인합니다.
    void Update()
    {
        if (playerTransform == null)
        {
            return;
        }

        ChunkCoordinate currentChunk = ChunkCoordinate.FromWorldPosition(playerTransform.position, chunkSize);
        if (!currentChunk.Equals(lastPlayerChunk))
        {
            lastPlayerChunk = currentChunk;
            playerChunkChanged = true;
        }
    }

    // 청크 변경은 LateUpdate에서 처리해 플레이어 이동 후에 맞춥니다.
    void LateUpdate()
    {
        if (playerTransform == null || chunkDefinition == null)
        {
            return;
        }

        if (playerChunkChanged)
        {
            playerChunkChanged = false;
            RefreshAllChunks();
        }
    }

    // 플레이어 주변 청크를 맞추고, 멀리 있는 청크는 제거합니다.
    void RefreshAllChunks()
    {
        ChunkCoordinate center = ChunkCoordinate.FromWorldPosition(playerTransform.position, chunkSize);
        neededChunkCoordsScratch.Clear();

        for (int offsetX = -viewRadius; offsetX <= viewRadius; offsetX++)
        {
            for (int offsetZ = -viewRadius; offsetZ <= viewRadius; offsetZ++)
            {
                ChunkCoordinate coord = new ChunkCoordinate(center.x + offsetX, center.z + offsetZ);
                neededChunkCoordsScratch.Add(coord.ToVector2Int());
            }
        }

        chunksToRemoveScratch.Clear();
        foreach (KeyValuePair<Vector2Int, ChunkInstance> pair in activeChunks)
        {
            if (!neededChunkCoordsScratch.Contains(pair.Key))
            {
                chunksToRemoveScratch.Add(pair.Key);
            }
        }

        for (int i = 0; i < chunksToRemoveScratch.Count; i++)
        {
            DespawnChunk(chunksToRemoveScratch[i]);
        }

        pendingChunkSpawns.Clear();
        foreach (Vector2Int needed in neededChunkCoordsScratch)
        {
            if (!activeChunks.ContainsKey(needed))
            {
                pendingChunkSpawns.Add(new ChunkCoordinate(needed.x, needed.y));
            }
        }

        if (chunkSpawnCoroutine != null)
        {
            StopCoroutine(chunkSpawnCoroutine);
        }

        if (pendingChunkSpawns.Count > 0)
        {
            chunkSpawnCoroutine = StartCoroutine(SpawnPendingChunksRoutine());
        }
    }

    IEnumerator SpawnPendingChunksRoutine()
    {
        int spawnBudget = Mathf.Max(1, chunksPerFrame);

        while (pendingChunkSpawns.Count > 0)
        {
            int spawnCountThisFrame = Mathf.Min(spawnBudget, pendingChunkSpawns.Count);
            for (int i = 0; i < spawnCountThisFrame; i++)
            {
                ChunkCoordinate coord = pendingChunkSpawns[0];
                pendingChunkSpawns.RemoveAt(0);

                if (!activeChunks.ContainsKey(coord.ToVector2Int()))
                {
                    SpawnChunk(coord);
                }
            }

            yield return null;
        }

        chunkSpawnCoroutine = null;
    }

    // 청크 한 칸을 생성합니다 (바닥 + 나중에 오브젝트).
    void SpawnChunk(ChunkCoordinate coordinate)
    {
        if (chunkDefinition == null || !chunkDefinition.TryPickFloorVariant(coordinate, out ChunkFloorVariant floorVariant))
        {
            return;
        }

        if (!ChunkPrefabUtility.TryGetInstantiablePrefab(floorVariant.prefab, out GameObject floorPrefab))
        {
            return;
        }

        Vector3 worldPosition = ChunkToWorldPosition(coordinate, floorVariant.positionY);
        ChunkInstance instance = new ChunkInstance(coordinate);
        instance.floorPrefabUsed = floorPrefab;
        instance.floorWorldY = floorVariant.positionY;

        instance.floorObject = chunkPool.Get(
            floorPrefab,
            worldPosition,
            Quaternion.identity,
            chunksParent
        );

        if (instance.floorObject != null)
        {
            string floorName = floorPrefab.name;
            instance.floorObject.name = "Chunk_" + coordinate.x + "_" + coordinate.z + "_" + floorName;
            ApplyFloorChunkCollision(instance.floorObject);
        }

        SpawnPropsForChunk(instance, coordinate);
        activeChunks[coordinate.ToVector2Int()] = instance;
        ChunkFloorGroundRegistry.Register(
            coordinate,
            chunkSize,
            instance.floorWorldY,
            instance.floorPrefabUsed);
    }

    static void ApplyFloorChunkCollision(GameObject floorObject)
    {
        if (floorObject == null)
        {
            return;
        }

        if (floorObject.GetComponent<WalkableTerrainFeature>() == null
            && floorObject.GetComponentInChildren<WalkableTerrainFeature>(true) == null)
        {
            floorObject.AddComponent<WalkableTerrainFeature>();
        }

        PropCollisionLayers.EnableWalkableTerrainColliders(floorObject);
    }

    Vector3 ChunkToWorldPosition(ChunkCoordinate coordinate)
    {
        if (chunkDefinition != null && chunkDefinition.TryPickFloorVariant(coordinate, out ChunkFloorVariant variant))
        {
            return ChunkToWorldPosition(coordinate, variant.positionY);
        }

        return ChunkToWorldPosition(coordinate, chunkDefinition != null ? chunkDefinition.floorPositionY : 0f);
    }

    Vector3 ChunkToWorldPosition(ChunkCoordinate coordinate, float worldY)
    {
        float worldX = coordinate.x * chunkSize;
        float worldZ = coordinate.z * chunkSize;
        return new Vector3(worldX, worldY, worldZ);
    }

    // Definition에 등록된 오브젝트를 이 청크에 배치합니다.
    void SpawnPropsForChunk(ChunkInstance instance, ChunkCoordinate coordinate)
    {
        if (chunkDefinition.propEntries != null && chunkDefinition.propEntries.Count > 0)
        {
            SpawnManualPropsForChunk(instance, coordinate);
        }

        SpawnTerrainFeaturesForChunk(instance, coordinate);
        SpawnRandomPropsForChunk(instance, coordinate);
    }

    Vector3 GetChunkOrigin(ChunkInstance instance, ChunkCoordinate coordinate)
    {
        return ChunkToWorldPosition(coordinate, instance.floorWorldY);
    }

    void SpawnTerrainFeaturesForChunk(ChunkInstance instance, ChunkCoordinate coordinate)
    {
        if (chunkDefinition.terrainFeatureRules == null || chunkDefinition.terrainFeatureRules.Count == 0)
        {
            return;
        }

        Vector3 chunkOrigin = GetChunkOrigin(instance, coordinate);

        for (int i = 0; i < chunkDefinition.terrainFeatureRules.Count; i++)
        {
            ChunkTerrainFeatureRule rule = chunkDefinition.terrainFeatureRules[i];
            ChunkTerrainFeatureSpawner.TrySpawnForChunk(
                instance,
                coordinate,
                chunkOrigin,
                rule,
                chunkPool,
                chunksParent);
        }
    }

    // 고정 위치·확률로 놓는 수동 오브젝트입니다.
    void SpawnManualPropsForChunk(ChunkInstance instance, ChunkCoordinate coordinate)
    {
        int seed = coordinate.x * 73856093 ^ coordinate.z * 19349663;
        Random.State oldState = Random.state;
        Random.InitState(seed);

        for (int i = 0; i < chunkDefinition.propEntries.Count; i++)
        {
            ChunkPropEntry entry = chunkDefinition.propEntries[i];
            if (entry == null || entry.prefab == null)
            {
                continue;
            }

            if (Random.value > entry.spawnChance)
            {
                continue;
            }

            Vector3 propWorldPosition = GetChunkOrigin(instance, coordinate) + entry.localPosition;
            Quaternion propRotation = Quaternion.Euler(entry.localEulerAngles);
            GameObject propObject = chunkPool.Get(entry.prefab, propWorldPosition, propRotation, chunksParent);
            PropCollisionLayers.ApplyToRoot(propObject);
            instance.AddSpawnedProp(propObject, entry.prefab);
        }

        Random.state = oldState;
    }

    // 청크마다 랜덤 규칙으로 나무 등을 배치합니다.
    void SpawnRandomPropsForChunk(ChunkInstance instance, ChunkCoordinate coordinate)
    {
        if (chunkDefinition.randomPropRules == null || chunkDefinition.randomPropRules.Count == 0)
        {
            return;
        }

        Vector3 chunkOrigin = GetChunkOrigin(instance, coordinate);

        for (int i = 0; i < chunkDefinition.randomPropRules.Count; i++)
        {
            ChunkRandomPropRule rule = chunkDefinition.randomPropRules[i];
            ChunkRandomPropSpawner.SpawnForChunk(
                instance,
                coordinate,
                chunkOrigin,
                chunkSize,
                chunkDefinition,
                rule,
                chunkPool,
                chunksParent
            );
        }
    }

    // 청크를 제거하고 오브젝트를 풀에 돌려줍니다.
    void DespawnChunk(Vector2Int key)
    {
        if (!activeChunks.TryGetValue(key, out ChunkInstance instance))
        {
            return;
        }

        ChunkTreePlacementIndex.UnregisterChunk(instance);

        if (instance.floorObject != null && instance.floorPrefabUsed != null)
        {
            chunkPool.Return(instance.floorObject, instance.floorPrefabUsed);
        }

        for (int i = 0; i < instance.spawnedProps.Count; i++)
        {
            SpawnedPropRecord record = instance.spawnedProps[i];
            if (record.instance == null)
            {
                continue;
            }

            if (record.prefab != null)
            {
                chunkPool.Return(record.instance, record.prefab);
            }
            else
            {
                Object.Destroy(record.instance);
            }
        }

        instance.ClearSpawnedList();
        activeChunks.Remove(key);
        ChunkFloorGroundRegistry.Unregister(instance.coordinate);
    }

    void EnsureChunkDefinitionValid()
    {
        if (chunkDefinition == null)
        {
            return;
        }

        if (chunkDefinition.floorVariants != null && chunkDefinition.floorVariants.Count > 0)
        {
            bool hasAny = false;
            for (int i = 0; i < chunkDefinition.floorVariants.Count; i++)
            {
                if (chunkDefinition.floorVariants[i].prefab != null)
                {
                    hasAny = true;
                    break;
                }
            }

            if (hasAny)
            {
                return;
            }
        }

#if UNITY_EDITOR
        GameObject mainLand = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultMainLandPrefabPath);

        if (mainLand != null)
        {
            chunkDefinition.floorVariants = new List<ChunkFloorVariant>
            {
                new ChunkFloorVariant { prefab = mainLand, positionY = 0f, isSlope = false }
            };
            chunkDefinition.floorPositionY = 0f;
            return;
        }
#endif

        if (fallbackFloorPrefab != null)
        {
            chunkDefinition.floorPrefab = fallbackFloorPrefab;
        }
    }

    // 에디터에서 플레이어 위치 기준으로 즉시 갱신합니다.
    [ContextMenu("Refresh Chunks Now")]
    public void ForceRefresh()
    {
        InitializeStreamer();
        RefreshAllChunks();
    }
}

// 청크용 오브젝트를 미리 만들어 두고 재사용하는 풀입니다.
public class ChunkPool
{
    readonly Transform poolRoot;
    readonly Dictionary<GameObject, Queue<GameObject>> poolsByPrefab = new Dictionary<GameObject, Queue<GameObject>>();

    public ChunkPool(Transform poolRoot)
    {
        this.poolRoot = poolRoot;
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!ChunkPrefabUtility.TryGetInstantiablePrefab(prefab, out GameObject validPrefab))
        {
            return null;
        }

        prefab = validPrefab;

        GameObject instance;
        if (poolsByPrefab.TryGetValue(prefab, out Queue<GameObject> queue) && queue.Count > 0)
        {
            instance = queue.Dequeue();
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
        }
        else
        {
            instance = Object.Instantiate(prefab, position, rotation, parent);
        }

        return instance;
    }

    public void Return(GameObject instance, GameObject prefab)
    {
        if (instance == null || prefab == null)
        {
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);

        if (!poolsByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            poolsByPrefab[prefab] = queue;
        }

        queue.Enqueue(instance);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (!ChunkPrefabUtility.TryGetInstantiablePrefab(prefab, out GameObject validPrefab) || count <= 0)
        {
            return;
        }

        prefab = validPrefab;

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Object.Instantiate(prefab, poolRoot.position, Quaternion.identity, poolRoot);
            instance.SetActive(false);
            Return(instance, prefab);
        }
    }
}
