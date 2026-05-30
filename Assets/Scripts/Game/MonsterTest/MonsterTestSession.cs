using UnityEngine;

// Monster_test 씬에서 몬스터 선택·튜닝·저장/되돌리기를 돕습니다.
public class MonsterTestSession : MonoBehaviour
{
    const float MainLandSurfaceY = 2f;
    const float SpawnDistanceFromPlayer = 8f;

    [SerializeField] GameObject playerTarget;
    [SerializeField] Transform monsterSpawnAnchor;
    [SerializeField] MonsterKind activeMonsterKind = MonsterKind.Melee;
    [SerializeField] MonsterVisualTuningSnapshot activeSnapshot;

    GameObject activeMonster;
    MonsterVisualTuningSnapshot lastSavedSnapshot;

    public GameObject PlayerTarget => playerTarget;
    public GameObject ActiveMonster => activeMonster;
    public MonsterKind ActiveMonsterKind => activeMonsterKind;

    public int ActiveMonId =>
        activeSnapshot != null ? activeSnapshot.monId : MonsterVisualTuningPersistence.GetDefaultMonId(activeMonsterKind);

    public MonsterVisualTuningSnapshot ActiveSnapshot
    {
        get
        {
            EnsureActiveSnapshot();
            return activeSnapshot;
        }
    }

    void Awake()
    {
        EnsureActiveSnapshot();
        ResolveReferences();
        PreparePlayerTarget();
    }

    void Start()
    {
        EquipMonsterById(ActiveMonId);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipFirstMonsterOfKind(MonsterKind.Melee);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipFirstMonsterOfKind(MonsterKind.Ranged);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EquipFirstMonsterOfKind(MonsterKind.Mage);
        }
    }

    void EnsureActiveSnapshot()
    {
        if (activeSnapshot == null)
        {
            activeSnapshot = MonsterVisualTuningSnapshot.CreateDefault(activeMonsterKind);
        }
    }

    public void ResolveReferences()
    {
        if (playerTarget == null)
        {
            playerTarget = GameObject.FindGameObjectWithTag("Player");
        }

        if (monsterSpawnAnchor == null)
        {
            GameObject anchorObject = GameObject.Find("MonsterSpawnAnchor");
            if (anchorObject != null)
            {
                monsterSpawnAnchor = anchorObject.transform;
            }
        }
    }

    public void PreparePlayerTarget()
    {
        if (playerTarget == null)
        {
            return;
        }

        PlayerMovement movement = playerTarget.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        PlayerWeaponCombat melee = playerTarget.GetComponent<PlayerWeaponCombat>();
        if (melee != null)
        {
            melee.enabled = false;
        }

        PlayerRangedCombat ranged = playerTarget.GetComponent<PlayerRangedCombat>();
        if (ranged != null)
        {
            ranged.enabled = false;
        }

        PlayerMagicCombat magic = playerTarget.GetComponent<PlayerMagicCombat>();
        if (magic != null)
        {
            magic.enabled = false;
        }

        PlayerHealth health = playerTarget.GetComponent<PlayerHealth>();
        if (health != null)
        {
            if (health.MaxHp <= 0)
            {
                health.ApplyMaxHp(100, true);
            }

            health.SetInfiniteHp(true);
        }

        CharacterController playerController = playerTarget.GetComponent<CharacterController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    public void EquipMonster(MonsterKind kind)
    {
        EquipMonsterById(MonsterVisualTuningPersistence.GetDefaultMonId(kind));
    }

    public void EquipFirstMonsterOfKind(MonsterKind kind)
    {
        MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
        for (int i = 0; i < table.Rows.Count; i++)
        {
            MonsterDefinitionRow row = table.Rows[i];
            if (row.kind != kind)
            {
                continue;
            }

            EquipMonsterById(row.monId);
            return;
        }

        EquipMonster(kind);
    }

    public void EquipMonsterById(int monId)
    {
        ResolveReferences();

        MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
        if (!table.TryGetById(monId, out MonsterDefinitionRow row))
        {
            Debug.LogWarning("[MonsterTest] CSV에 없는 Mon_ID: " + monId);
            return;
        }

        activeMonsterKind = row.kind;
        SpawnMonster(row);

        if (TryLoadSavedTuning(monId))
        {
            ApplyActiveSnapshot();
        }
        else if (TryLoadFromCsv(monId))
        {
            ApplyActiveSnapshot();
        }
        else
        {
            PullTuningFromMonster();
        }

        RefreshLastSavedBaseline();
    }

    void RefreshLastSavedBaseline()
    {
        if (MonsterVisualTuningPersistence.TryLoad(ActiveMonId, out MonsterVisualTuningSnapshot loaded))
        {
            lastSavedSnapshot = loaded.Clone();
            return;
        }

        lastSavedSnapshot = null;
    }

    void SpawnMonster(MonsterDefinitionRow row)
    {
        if (playerTarget == null)
        {
            Debug.LogWarning("[MonsterTest] PlayerTarget 없음");
            return;
        }

        if (activeMonster != null)
        {
            Destroy(activeMonster);
            activeMonster = null;
        }

        GameObject prefab = GameAssets.LoadMonsterPrefab(row.prefabName);
        if (prefab == null)
        {
            Debug.LogWarning("[MonsterTest] 프리팹 없음: " + row.prefabName);
            return;
        }

        Vector3 position = GetSpawnPosition();
        Quaternion rotation = GetSpawnRotation(position);

        activeMonster = Instantiate(prefab, position, rotation);
        activeMonster.name = "TestMonster_" + row.monId;
        AlignRectTransformToWorld(activeMonster, position, rotation);
        PrepareTestMonster(activeMonster);
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 playerFlat = SpumChasePosition.GetFlatChasePoint(playerTarget.transform);
        Vector3 direction = GetSpawnDirection(playerFlat);
        Vector3 spawnFlat = playerFlat + direction * SpawnDistanceFromPlayer;
        return new Vector3(spawnFlat.x, MainLandSurfaceY, spawnFlat.z);
    }

    Vector3 GetSpawnDirection(Vector3 playerFlat)
    {
        if (monsterSpawnAnchor != null)
        {
            Vector3 anchorFlat = new Vector3(
                monsterSpawnAnchor.position.x,
                0f,
                monsterSpawnAnchor.position.z);

            Vector3 direction = anchorFlat - playerFlat;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }
        }

        return Vector3.forward;
    }

    Quaternion GetSpawnRotation(Vector3 spawnPosition)
    {
        Vector3 playerFlat = SpumChasePosition.GetFlatChasePoint(playerTarget.transform);
        Vector3 spawnFlat = new Vector3(spawnPosition.x, 0f, spawnPosition.z);
        Vector3 toPlayer = playerFlat - spawnFlat;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            return Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        }

        return Quaternion.Euler(0f, 180f, 0f);
    }

    static void AlignRectTransformToWorld(GameObject character, Vector3 worldPosition, Quaternion worldRotation)
    {
        RectTransform rectTransform = character.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.position = worldPosition;
        rectTransform.rotation = worldRotation;
    }

    void PrepareTestMonster(GameObject monster)
    {
        if (monster == null)
        {
            return;
        }

        MonsterFarDespawn farDespawn = monster.GetComponent<MonsterFarDespawn>();
        if (farDespawn != null)
        {
            farDespawn.enabled = false;
        }

        MonsterHealth health = monster.GetComponent<MonsterHealth>();
        if (health != null)
        {
            health.SetInfiniteHp(false);
        }

        MonsterMovement movement = monster.GetComponent<MonsterMovement>();
        if (movement != null)
        {
            movement.enabled = true;
        }

        MonsterRangedAttack ranged = monster.GetComponent<MonsterRangedAttack>();
        if (ranged != null)
        {
            ranged.SetSyncArrowFromPlayer(false);
        }
    }

    public bool TryLoadSavedTuning(int monId)
    {
        if (!MonsterVisualTuningPersistence.TryLoad(monId, out MonsterVisualTuningSnapshot loaded))
        {
            return false;
        }

        activeSnapshot = loaded.Clone();
        activeMonsterKind = loaded.kind;
        return true;
    }

    public bool TryLoadFromCsv(int monId)
    {
        MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
        if (!table.TryGetById(monId, out MonsterDefinitionRow row))
        {
            return false;
        }

        activeSnapshot = MonsterVisualTuningCsvBridge.ToSnapshot(row);
        activeMonsterKind = activeSnapshot.kind;
        return true;
    }

    public void ReloadFromCsv()
    {
        if (TryLoadFromCsv(ActiveMonId))
        {
            ApplyActiveSnapshot();
        }
    }

    public void SaveCurrentTuning()
    {
        EnsureActiveSnapshot();
        if (activeSnapshot == null)
        {
            return;
        }

        activeSnapshot.kind = activeMonsterKind;
        MonsterVisualTuningSnapshot saveCopy = activeSnapshot.Clone();
        MonsterVisualTuningPersistence.Save(saveCopy);
        SaveSnapshotToCsv(saveCopy);
        lastSavedSnapshot = saveCopy.Clone();
    }

    static void SaveSnapshotToCsv(MonsterVisualTuningSnapshot snapshot)
    {
#if UNITY_EDITOR
        MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
        table.UpsertFromSnapshot(snapshot);
        table.SaveToEditorCsv();
        Debug.Log("[MonsterTuning] CSV 저장 완료: " + MonsterDefinitionTable.EditorCsvPath);
#endif
    }

    public void PullTuningFromMonster()
    {
        if (activeMonster == null)
        {
            return;
        }

        if (MonsterVisualTuningApplier.TryReadFromMonster(activeMonster, out MonsterVisualTuningSnapshot snapshot))
        {
            activeSnapshot = snapshot;
            activeSnapshot.monId = ActiveMonId;
            activeSnapshot.kind = activeMonsterKind;

            MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
            if (table.TryGetById(ActiveMonId, out MonsterDefinitionRow row))
            {
                activeSnapshot.monName = row.monName;
                activeSnapshot.prefabName = row.prefabName;
                activeSnapshot.projectilePrefab = row.projectilePrefab;
                activeSnapshot.hitImpact = row.hitImpact;
            }
        }
    }

    public void ApplyActiveSnapshot()
    {
        if (activeMonster == null || activeSnapshot == null)
        {
            return;
        }

        activeSnapshot.kind = activeMonsterKind;
        MonsterVisualTuningApplier.TryApplyToMonster(activeMonster, activeSnapshot);
    }

    public void RevertToLastSaved()
    {
        if (lastSavedSnapshot != null)
        {
            activeSnapshot = lastSavedSnapshot.Clone();
            activeMonsterKind = activeSnapshot.kind;
            ApplyActiveSnapshot();
            return;
        }

        if (TryLoadSavedTuning(ActiveMonId))
        {
            lastSavedSnapshot = activeSnapshot.Clone();
            ApplyActiveSnapshot();
            return;
        }

        Debug.LogWarning("[MonsterTest] 되돌릴 저장 값이 없습니다. 먼저 「저장」을 눌러주세요.");
    }

    public void ApplyToCsvAndSampleScene()
    {
#if UNITY_EDITOR
        EnsureActiveSnapshot();
        if (activeSnapshot == null)
        {
            return;
        }

        ApplyActiveSnapshot();
        SaveCurrentTuning();
        MonsterVisualTuningSampleSceneApplier.ApplyMonster(activeSnapshot.Clone());
        Debug.Log($"[MonsterTest] CSV·SampleScene 적용 요청 완료 — Mon_ID={activeSnapshot.monId}, Cooldown={activeSnapshot.attackCooldown:F2}");
#else
        Debug.LogWarning("[MonsterTest] CSV/SampleScene 반영은 에디터에서만 가능합니다.");
#endif
    }

    public void ResetMonsterSpawn()
    {
        ResolveReferences();

        MonsterDefinitionTable table = MonsterDefinitionTable.LoadForEditing();
        if (!table.TryGetById(ActiveMonId, out MonsterDefinitionRow row))
        {
            return;
        }

        SpawnMonster(row);
        ApplyActiveSnapshot();
    }
}
