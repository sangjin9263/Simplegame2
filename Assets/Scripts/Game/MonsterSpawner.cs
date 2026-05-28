using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 주변에 오크 몬스터를 랜덤 프리팹으로 소환합니다.
public class MonsterSpawner : MonoBehaviour
{
    const int SpawnOctantCount = 8;

    [Header("몬스터 프리팹 (m1~m6)")]
    [SerializeField] GameObject[] monsterPrefabs;

    [Header("몬스터 테이블")]
    [SerializeField] TextAsset monsterCsvOverride;

    [Header("소환 수")]
    [SerializeField] int spawnCount = 12;
    [Header("소환 비율 (근접:원거리:마법사)")]
    [SerializeField] float meleeSpawnWeight = 8f;
    [SerializeField] float rangedSpawnWeight = 1f;
    [SerializeField] float mageSpawnWeight = 1f;

    [Header("소환 위치 (플레이어 기준)")]
    [SerializeField] float spawnRadiusMin = 22f;
    [SerializeField] float spawnRadiusMax = 38f;
    [SerializeField] float octantAngleJitter = 20f;

    [SerializeField] float minSpawnDistanceFromPlayer = 18f;

    [SerializeField] Transform playerTransform;
    [SerializeField] Transform monstersParent;

    [Header("시작 시 자동 소환")]
    [SerializeField] bool spawnOnStart = true;

    [SerializeField] int monstersPerFrame = 2;

    [Header("지속 소환")]
    [SerializeField] bool continuousSpawnEnabled = true;
    [SerializeField] float continuousSpawnInterval = 1f;

    [Header("스폰 검사")]
    [SerializeField] int maxSpawnAttemptsPerMonster = 16;
    [SerializeField] float monsterSpawnRadius = 0.22f;
    [SerializeField] float monsterSpawnHeight = 1.05f;

    int nextSpawnOctant;

    MonsterDefinitionTable monsterTable;
    readonly List<MonsterDefinitionRow> spawnPool = new List<MonsterDefinitionRow>();
    readonly Dictionary<string, GameObject> prefabsByName = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);

    void Start()
    {
        RefreshPlayerReference();

        if (monstersParent == null)
        {
            GameObject parentObject = new GameObject("Monsters");
            monstersParent = parentObject.transform;
        }

        if (spawnOnStart)
        {
            StartCoroutine(BeginSpawnWhenWorldReady());
        }
    }

    IEnumerator BeginSpawnWhenWorldReady()
    {
        yield return WorldLoadCoordinator.WaitUntilWorldReady();
        yield return SpawnInitialMonstersRoutine();

        if (continuousSpawnEnabled)
        {
            StartCoroutine(ContinuousSpawnRoutine());
        }
    }

    [ContextMenu("Spawn Monsters Now")]
    public void SpawnMonsters()
    {
        StartCoroutine(SpawnInitialMonstersRoutine());
    }

    IEnumerator SpawnInitialMonstersRoutine()
    {
        yield return null;

        if (!PrepareSpawnContext())
        {
            yield break;
        }

        int spawned = 0;
        int budget = Mathf.Max(1, monstersPerFrame);

        while (spawned < spawnCount)
        {
            int countThisFrame = Mathf.Min(budget, spawnCount - spawned);
            for (int i = 0; i < countThisFrame; i++)
            {
                if (TrySpawnOneMonster())
                {
                    spawned++;
                }
            }

            yield return null;
        }
    }

    IEnumerator ContinuousSpawnRoutine()
    {
        if (!PrepareSpawnContext())
        {
            yield break;
        }

        float interval = Mathf.Max(0.1f, continuousSpawnInterval);
        WaitForSeconds wait = new WaitForSeconds(interval);

        while (true)
        {
            yield return wait;
            RefreshPlayerReference();
            TrySpawnOneMonster();
        }
    }

    bool PrepareSpawnContext()
    {
        EnsureMonsterPrefabs();
        EnsureMonsterTable();
        RebuildPrefabMap();
        RebuildSpawnPool();

        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("[MonsterSpawner] 몬스터 프리팹이 비어 있습니다.");
            return false;
        }

        if (!RefreshPlayerReference())
        {
            Debug.LogWarning("[MonsterSpawner] Player Transform이 없습니다.");
            return false;
        }

        return true;
    }

    bool RefreshPlayerReference()
    {
        if (GameSession.TryGetPlayerTransform(out Transform player))
        {
            playerTransform = player;
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return false;
        }

        playerTransform = playerObject.transform;
        PlayerMovement movement = playerObject.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            GameSession.RegisterPlayer(movement);
        }

        return true;
    }

    bool TryGetSpawnCenter(out Vector3 center)
    {
        if (GameSession.TryGetPlayerWorldCenter(out center))
        {
            return true;
        }

        if (!RefreshPlayerReference())
        {
            center = Vector3.zero;
            return false;
        }

        center = playerTransform.position;
        center.y = GroundHeightSampler.GetCharacterSurfaceY(center, GameSession.GroundY);
        return true;
    }

    bool TrySpawnOneMonster()
    {
        if (!TryGetSpawnCenter(out Vector3 center))
        {
            return false;
        }

        if (!TryPickSpawnEntry(out MonsterDefinitionRow row, out GameObject prefab))
        {
            return false;
        }

        if (!TryPickClearSpawnPosition(center, out Vector3 spawnPosition))
        {
            return false;
        }

        GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        if (monstersParent != null)
        {
            instance.transform.SetParent(monstersParent, true);
        }

        instance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        GroundHeightSampler.SnapTransformToSurface(instance.transform, spawnPosition.y);
        spawnPosition = instance.transform.position;
        instance.name = prefab.name + "_Spawned";
        SetupSpawnedMonster(instance, spawnPosition, row);
        return true;
    }

    bool TryPickSpawnEntry(out MonsterDefinitionRow row, out GameObject prefab)
    {
        row = default;
        prefab = null;

        if (spawnPool.Count > 0)
        {
            if (TryPickWeightedSpawnKind(out MonsterKind targetKind)
                && TryPickSpawnEntryByKind(targetKind, out row, out prefab))
            {
                return true;
            }

            int attempts = spawnPool.Count * 2;
            for (int i = 0; i < attempts; i++)
            {
                MonsterDefinitionRow candidate = spawnPool[Random.Range(0, spawnPool.Count)];
                if (prefabsByName.TryGetValue(candidate.prefabName, out prefab) && prefab != null)
                {
                    row = candidate;
                    return true;
                }
            }
        }

        prefab = PickRandomPrefabWithoutTable();
        if (prefab == null)
        {
            return false;
        }

        if (monsterTable != null && monsterTable.TryGetByPrefabName(prefab.name, out row))
        {
            return true;
        }

        row = default;
        return true;
    }

    bool TryPickWeightedSpawnKind(out MonsterKind kind)
    {
        kind = MonsterKind.Melee;
        float melee = Mathf.Max(0f, meleeSpawnWeight);
        float ranged = Mathf.Max(0f, rangedSpawnWeight);
        float mage = Mathf.Max(0f, mageSpawnWeight);
        float total = melee + ranged + mage;
        if (total <= 0.0001f)
        {
            return false;
        }

        float roll = Random.Range(0f, total);
        if (roll < melee)
        {
            kind = MonsterKind.Melee;
            return true;
        }

        roll -= melee;
        if (roll < ranged)
        {
            kind = MonsterKind.Ranged;
            return true;
        }

        kind = MonsterKind.Mage;
        return true;
    }

    bool TryPickSpawnEntryByKind(MonsterKind kind, out MonsterDefinitionRow row, out GameObject prefab)
    {
        row = default;
        prefab = null;
        if (spawnPool.Count == 0)
        {
            return false;
        }

        int start = Random.Range(0, spawnPool.Count);
        for (int offset = 0; offset < spawnPool.Count; offset++)
        {
            MonsterDefinitionRow candidate = spawnPool[(start + offset) % spawnPool.Count];
            if (candidate.kind != kind)
            {
                continue;
            }

            if (!prefabsByName.TryGetValue(candidate.prefabName, out GameObject candidatePrefab)
                || candidatePrefab == null)
            {
                continue;
            }

            row = candidate;
            prefab = candidatePrefab;
            return true;
        }

        return false;
    }

    GameObject PickRandomPrefabWithoutTable()
    {
        int attempts = monsterPrefabs.Length * 2;
        for (int i = 0; i < attempts; i++)
        {
            GameObject candidate = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    bool TryPickClearSpawnPosition(Vector3 playerCenter, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        int attempts = Mathf.Max(1, maxSpawnAttemptsPerMonster);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector3 candidate = PickSpawnPositionAroundPlayer(playerCenter, attempt);

            Vector3 toSpawn = candidate - playerCenter;
            toSpawn.y = 0f;
            if (toSpawn.sqrMagnitude < minSpawnDistanceFromPlayer * minSpawnDistanceFromPlayer)
            {
                if (toSpawn.sqrMagnitude > 0.0001f)
                {
                    candidate = playerCenter + toSpawn.normalized * minSpawnDistanceFromPlayer;
                }
                else
                {
                    candidate = playerCenter + Vector3.forward * minSpawnDistanceFromPlayer;
                }

                candidate.y = GroundHeightSampler.GetCharacterSurfaceY(candidate, GameSession.GroundY);
            }

            if (MonsterSpawnPlacement.TryResolveClearPosition(
                    candidate,
                    monsterSpawnRadius,
                    monsterSpawnHeight,
                    candidate.y,
                    out spawnPosition))
            {
                return true;
            }
        }

        return false;
    }

    Vector3 PickSpawnPositionAroundPlayer(Vector3 playerCenter, int attemptIndex)
    {
        float angle;

        if (attemptIndex == 0)
        {
            int octant = nextSpawnOctant % SpawnOctantCount;
            nextSpawnOctant++;

            float sectorSize = 360f / SpawnOctantCount;
            float baseAngleDeg = octant * sectorSize;
            float jitter = Random.Range(-octantAngleJitter, octantAngleJitter);
            angle = (baseAngleDeg + jitter) * Mathf.Deg2Rad;
        }
        else
        {
            angle = Random.Range(0f, Mathf.PI * 2f);
        }

        float minRadius = Mathf.Max(minSpawnDistanceFromPlayer, spawnRadiusMin);
        float maxRadius = Mathf.Max(minRadius + 0.01f, spawnRadiusMax);

        float minSqr = minRadius * minRadius;
        float maxSqr = maxRadius * maxRadius;
        float distance = Mathf.Sqrt(Random.Range(minSqr, maxSqr));

        float x = playerCenter.x + Mathf.Cos(angle) * distance;
        float z = playerCenter.z + Mathf.Sin(angle) * distance;
        Vector3 spawnXZ = new Vector3(x, 0f, z);
        float groundY = GroundHeightSampler.GetCharacterSurfaceY(spawnXZ, GameSession.GroundY);
        return new Vector3(x, groundY, z);
    }

    void SetupSpawnedMonster(GameObject monster, Vector3 spawnPosition, MonsterDefinitionRow row)
    {
        WorldCollision.ApplyMonster(monster);
        GameplayComponents.EnsureMonster(monster, logIfMissing: true);

        RectTransform rectTransform = monster.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = spawnPosition;
        }

        MonsterFarDespawn farDespawn = monster.GetComponent<MonsterFarDespawn>();

        if (row.monId != 0)
        {
            MonsterStats.Apply(monster, row);
        }
        else if (monsterTable != null
                 && monsterTable.TryGetByPrefabName(GetPrefabBaseName(monster.name), out MonsterDefinitionRow fallbackRow))
        {
            MonsterStats.Apply(monster, fallbackRow);
        }

        if (farDespawn != null)
        {
            farDespawn.NotifySpawned(spawnRadiusMax + 12f);
        }
    }

    void EnsureMonsterTable()
    {
        if (monsterCsvOverride != null)
        {
            monsterTable = MonsterDefinitionTable.LoadFromCsv(monsterCsvOverride.text);
            return;
        }

        monsterTable = MonsterDefinitionTable.LoadDefault();
    }

    void RebuildPrefabMap()
    {
        prefabsByName.Clear();

        if (monsterPrefabs == null)
        {
            return;
        }

        for (int i = 0; i < monsterPrefabs.Length; i++)
        {
            GameObject prefab = monsterPrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            prefabsByName[prefab.name] = prefab;
        }
    }

    void RebuildSpawnPool()
    {
        spawnPool.Clear();

        if (monsterTable == null)
        {
            return;
        }

        var availableNames = new HashSet<string>(prefabsByName.Keys, System.StringComparer.OrdinalIgnoreCase);
        monsterTable.CollectSpawnableNormals(spawnPool, availableNames);
    }

    static string GetPrefabBaseName(string instanceName)
    {
        const string spawnedSuffix = "_Spawned";
        if (instanceName.EndsWith(spawnedSuffix))
        {
            return instanceName.Substring(0, instanceName.Length - spawnedSuffix.Length);
        }

        return instanceName;
    }

    void EnsureMonsterPrefabs()
    {
        monsterPrefabs = GameAssets.LoadDefaultMonsterPrefabs(monsterPrefabs);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (spawnCount < 0)
        {
            spawnCount = 0;
        }

        if (spawnRadiusMax < spawnRadiusMin)
        {
            spawnRadiusMax = spawnRadiusMin;
        }

        if (continuousSpawnInterval < 0.1f)
        {
            continuousSpawnInterval = 0.1f;
        }

        minSpawnDistanceFromPlayer = Mathf.Max(0f, minSpawnDistanceFromPlayer);
        spawnRadiusMin = Mathf.Max(minSpawnDistanceFromPlayer, spawnRadiusMin);
        meleeSpawnWeight = Mathf.Max(0f, meleeSpawnWeight);
        rangedSpawnWeight = Mathf.Max(0f, rangedSpawnWeight);
        mageSpawnWeight = Mathf.Max(0f, mageSpawnWeight);
        if (meleeSpawnWeight + rangedSpawnWeight + mageSpawnWeight <= 0.0001f)
        {
            meleeSpawnWeight = 1f;
        }
    }
#endif
}
