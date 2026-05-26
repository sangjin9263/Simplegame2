using System.Collections;
using UnityEngine;

// 플레이어 주변에 오크 몬스터를 랜덤 프리팹으로 소환합니다.
public class MonsterSpawner : MonoBehaviour
{
    const int SpawnOctantCount = 8;

    [Header("몬스터 프리팹 (m1~m6)")]
    [SerializeField] GameObject[] monsterPrefabs;

    [Header("소환 수")]
    [SerializeField] int spawnCount = 12;

    [Header("소환 위치 (플레이어 기준)")]
    [SerializeField] float spawnRadiusMin = 22f;
    [SerializeField] float spawnRadiusMax = 38f;
    [SerializeField] float octantAngleJitter = 20f;

    [SerializeField] float groundHeight = 0f;
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

    static readonly string[] DefaultPrefabPaths =
    {
        "Assets/Prefabs/SPUM_orc_m1.prefab",
        "Assets/Prefabs/SPUM_orc_m2.prefab",
        "Assets/Prefabs/SPUM_orc_m3.prefab",
        "Assets/Prefabs/SPUM_orc_m4.prefab",
        "Assets/Prefabs/SPUM_orc_m5.prefab",
        "Assets/Prefabs/SPUM_orc_m6.prefab"
    };

    void Start()
    {
        RefreshPlayerReference();
        EnsurePlayerWorldPositionTracker();

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
        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return false;
        }

        playerTransform = playerObject.transform;
        return true;
    }

    void EnsurePlayerWorldPositionTracker()
    {
        if (playerTransform == null)
        {
            return;
        }

        if (playerTransform.GetComponent<PlayerMovement>() == null)
        {
            playerTransform.gameObject.AddComponent<PlayerMovement>();
        }

        if (playerTransform.GetComponent<PlayerWorldPosition>() == null)
        {
            playerTransform.gameObject.AddComponent<PlayerWorldPosition>();
        }

        if (playerTransform.GetComponent<PlayerStats>() == null)
        {
            playerTransform.gameObject.AddComponent<PlayerStats>();
        }

    }

    bool TryGetSpawnCenter(out Vector3 center)
    {
        if (playerTransform != null)
        {
            PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                center = PlayerMovement.LastWorldCenter;
                center.y = groundHeight;
                return true;
            }
        }

        if (PlayerWorldPosition.TryGetWorldCenter(groundHeight, out center))
        {
            return true;
        }

        if (!RefreshPlayerReference())
        {
            center = Vector3.zero;
            return false;
        }

        center = playerTransform.position;
        center.y = groundHeight;
        return true;
    }

    bool TrySpawnOneMonster()
    {
        if (!TryGetSpawnCenter(out Vector3 center))
        {
            return false;
        }

        GameObject prefab = PickRandomPrefab();
        if (prefab == null)
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
        instance.name = prefab.name + "_Spawned";
        SetupSpawnedMonster(instance, spawnPosition);
        return true;
    }

    GameObject PickRandomPrefab()
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

                candidate.y = groundHeight;
            }

            if (MonsterSpawnPlacement.TryResolveClearPosition(
                    candidate,
                    monsterSpawnRadius,
                    monsterSpawnHeight,
                    groundHeight,
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
        return new Vector3(x, groundHeight, z);
    }

    void SetupSpawnedMonster(GameObject monster, Vector3 spawnPosition)
    {
        WorldCollision.ApplyMonster(monster);

        RectTransform rectTransform = monster.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = spawnPosition;
        }

        if (monster.GetComponent<MonsterMovement>() == null)
        {
            monster.AddComponent<MonsterMovement>();
        }

        if (monster.GetComponent<MonsterHealth>() == null)
        {
            monster.AddComponent<MonsterHealth>();
        }

        if (monster.GetComponent<MonsterHitReaction>() == null)
        {
            monster.AddComponent<MonsterHitReaction>();
        }

        if (monster.GetComponent<MonsterAttack>() == null)
        {
            monster.AddComponent<MonsterAttack>();
        }

        MonsterFarDespawn farDespawn = monster.GetComponent<MonsterFarDespawn>();
        if (farDespawn == null)
        {
            farDespawn = monster.AddComponent<MonsterFarDespawn>();
        }

        farDespawn.NotifySpawned(spawnRadiusMax + 12f);

        Transform unitRoot = monster.transform.Find("UnitRoot");
        if (unitRoot != null && unitRoot.GetComponent<BillboardFaceCamera>() == null)
        {
            unitRoot.gameObject.AddComponent<BillboardFaceCamera>();
        }
    }

    void EnsureMonsterPrefabs()
    {
        if (monsterPrefabs != null && monsterPrefabs.Length > 0)
        {
            bool hasAny = false;
            for (int i = 0; i < monsterPrefabs.Length; i++)
            {
                if (monsterPrefabs[i] != null)
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
        monsterPrefabs = new GameObject[DefaultPrefabPaths.Length];
        for (int i = 0; i < DefaultPrefabPaths.Length; i++)
        {
            monsterPrefabs[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPrefabPaths[i]);
        }
#endif
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
    }
#endif
}
