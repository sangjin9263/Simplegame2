using UnityEngine;

// Weapon_test 씬에서 무기 전환·튜닝·테스트 공격을 돕습니다.
public class WeaponTestSession : MonoBehaviour
{
    const float SlashHitLengthAtUnitScale = 3.48f;

    [SerializeField] GameObject player;
    [SerializeField] Transform trainingDummy;

    [Header("Current Tuning")]
    [SerializeField] WeaponVisualKind activeWeaponKind = WeaponVisualKind.Melee;
    [SerializeField] WeaponVisualTuningSnapshot activeSnapshot;

    PlayerWeaponCombat meleeCombat;
    PlayerRangedCombat rangedCombat;
    PlayerMagicCombat magicCombat;

    public GameObject Player => player;
    public WeaponVisualKind ActiveWeaponKind => activeWeaponKind;

    public int ActiveWeaponId =>
        activeSnapshot != null ? activeSnapshot.weaponId : WeaponVisualTuningPersistence.GetWeaponId(activeWeaponKind);
    public WeaponVisualTuningSnapshot ActiveSnapshot
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
        ResolvePlayerReferences();
        PrepareTrainingDummy();
    }

    void EnsureActiveSnapshot()
    {
        if (activeSnapshot == null)
        {
            activeSnapshot = WeaponVisualTuningSnapshot.CreateDefault(activeWeaponKind);
        }
    }

    void Start()
    {
        EquipWeapon(activeWeaponKind);
    }

    void ApplyWeaponTestMeleeDefaults()
    {
        ResolvePlayerReferences();
        if (meleeCombat == null)
        {
            return;
        }

        WeaponVisualTuningSnapshot meleeSnapshot = meleeCombat.ExportVisualTuning();
        float minimumScale = 0.1f;

        if (player != null && trainingDummy != null)
        {
            Vector3 playerFlat = player.transform.position;
            Vector3 dummyFlat = trainingDummy.position;
            playerFlat.y = 0f;
            dummyFlat.y = 0f;
            float distanceToDummy = Vector3.Distance(playerFlat, dummyFlat);
            minimumScale = (distanceToDummy + 0.75f) / SlashHitLengthAtUnitScale;
        }

        if (meleeSnapshot.visualScale < minimumScale)
        {
            meleeSnapshot.visualScale = minimumScale;
            meleeCombat.ApplyVisualTuning(meleeSnapshot);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipWeapon(WeaponVisualKind.Melee);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipWeapon(WeaponVisualKind.Ranged);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EquipWeapon(WeaponVisualKind.Magic);
        }
    }

    public void ResolvePlayerReferences()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player == null)
        {
            return;
        }

        meleeCombat = player.GetComponent<PlayerWeaponCombat>();
        rangedCombat = player.GetComponent<PlayerRangedCombat>();
        magicCombat = player.GetComponent<PlayerMagicCombat>();
    }

    public void PrepareTrainingDummy()
    {
        if (trainingDummy == null)
        {
            GameObject dummyObject = GameObject.Find("TrainingDummy");
            if (dummyObject != null)
            {
                trainingDummy = dummyObject.transform;
            }
        }

        if (trainingDummy == null)
        {
            return;
        }

        MonsterAttack attack = trainingDummy.GetComponent<MonsterAttack>();
        if (attack != null)
        {
            attack.enabled = false;
        }

        MonsterRangedAttack rangedAttack = trainingDummy.GetComponent<MonsterRangedAttack>();
        if (rangedAttack != null)
        {
            rangedAttack.enabled = false;
        }

        MonsterMovement movement = trainingDummy.GetComponent<MonsterMovement>();
        if (movement != null)
        {
            movement.enabled = true;
        }

        MonsterHealth health = trainingDummy.GetComponent<MonsterHealth>();
        if (health != null)
        {
            health.SetInfiniteHp(true);
        }

        MonsterHitReaction hitReaction = trainingDummy.GetComponent<MonsterHitReaction>();
        if (hitReaction == null)
        {
            hitReaction = trainingDummy.GetComponentInChildren<MonsterHitReaction>();
        }

        if (hitReaction != null)
        {
            hitReaction.SetKnockbackEnabled(false);
        }
    }

    public void EquipWeapon(WeaponVisualKind kind)
    {
        EquipWeaponById(WeaponVisualTuningPersistence.GetWeaponId(kind));
    }

    public void EquipWeaponById(int weaponId)
    {
        ResolvePlayerReferences();
        if (player == null)
        {
            return;
        }

        WeaponDefinitionTable table = WeaponDefinitionTable.LoadForEditing();
        if (!table.TryGetById(weaponId, out WeaponDefinitionRow row))
        {
            Debug.LogWarning("[WeaponTest] CSV에 없는 Weapon_ID: " + weaponId);
            return;
        }

        WeaponVisualKind kind = WeaponVisualTuningCsvBridge.KindFromWeaponType(row.weaponType);
        activeWeaponKind = kind;
        UnequipAllWeapons();

        switch (kind)
        {
            case WeaponVisualKind.Melee:
                meleeCombat?.EnsureSlashVfxPrefab();
                meleeCombat?.TryEquipWeapon(GameAssets.LoadDefaultWeaponSprite(null));
                break;

            case WeaponVisualKind.Ranged:
                rangedCombat?.EnsureWeaponAssets();
                rangedCombat?.TryEquipBow(GameAssets.LoadBowSprite(null));
                break;

            case WeaponVisualKind.Magic:
                magicCombat?.EnsureMagicAssets();
                magicCombat?.TryEquipStaff(GameAssets.LoadStaffSprite(null));
                break;
        }

        if (TryLoadSavedTuning(weaponId))
        {
            ApplyActiveSnapshot();
        }
        else if (TryLoadFromCsv(weaponId))
        {
            ApplyActiveSnapshot();
        }
        else
        {
            if (activeWeaponKind == WeaponVisualKind.Melee)
            {
                ApplyWeaponTestMeleeDefaults();
            }

            PullTuningFromPlayer();
        }
    }

    public bool TryLoadSavedTuning(int weaponId)
    {
        if (!WeaponVisualTuningPersistence.TryLoad(weaponId, out WeaponVisualTuningSnapshot loaded))
        {
            return false;
        }

        activeSnapshot = loaded.Clone();
        activeWeaponKind = loaded.kind;
        return true;
    }

    public void SaveCurrentTuning()
    {
        EnsureActiveSnapshot();
        if (activeSnapshot == null)
        {
            return;
        }

        activeSnapshot.kind = activeWeaponKind;
        WeaponVisualTuningSnapshot saveCopy = activeSnapshot.Clone();
        WeaponVisualTuningPersistence.Save(saveCopy);
        SaveSnapshotToCsv(saveCopy);
    }

    public bool TryLoadFromCsv(int weaponId)
    {
        WeaponDefinitionTable table = WeaponDefinitionTable.LoadForEditing();
        if (!table.TryGetById(weaponId, out WeaponDefinitionRow row))
        {
            return false;
        }

        activeSnapshot = WeaponVisualTuningCsvBridge.ToSnapshot(row);
        MergeSpawnAndRotationFromDefaults(activeSnapshot, activeSnapshot.kind);
        activeWeaponKind = activeSnapshot.kind;
        return true;
    }

    public void ReloadFromCsv()
    {
        if (TryLoadFromCsv(ActiveWeaponId))
        {
            ApplyActiveSnapshot();
        }
    }

    static void MergeSpawnAndRotationFromDefaults(WeaponVisualTuningSnapshot snapshot, WeaponVisualKind kind)
    {
        WeaponVisualTuningSnapshot defaults = WeaponVisualTuningSnapshot.CreateDefault(kind);
        snapshot.spawnForwardOffset = defaults.spawnForwardOffset;
        snapshot.spawnSideOffset = defaults.spawnSideOffset;
        snapshot.spawnHeightOffset = defaults.spawnHeightOffset;
        snapshot.visualRotationOffset = defaults.visualRotationOffset;
    }

    static void SaveSnapshotToCsv(WeaponVisualTuningSnapshot snapshot)
    {
#if UNITY_EDITOR
        WeaponDefinitionTable table = WeaponDefinitionTable.LoadForEditing();
        table.UpsertFromSnapshot(snapshot);
        table.SaveToEditorCsv();
        Debug.Log("[WeaponTuning] CSV 저장 완료: " + WeaponDefinitionTable.EditorCsvPath);
#endif
    }

    public void PullTuningFromPlayer()
    {
        ResolvePlayerReferences();
        if (player == null)
        {
            return;
        }

        if (WeaponVisualTuningApplier.TryReadFromPlayer(player, activeWeaponKind, out WeaponVisualTuningSnapshot snapshot))
        {
            activeSnapshot = snapshot;
        }
    }

    public void ApplyActiveSnapshot()
    {
        ResolvePlayerReferences();
        if (player == null || activeSnapshot == null)
        {
            return;
        }

        WeaponVisualTuningApplier.TryApplyToPlayer(player, activeSnapshot);
    }

    public void LoadSnapshot(WeaponVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        activeSnapshot = snapshot.Clone();
        if (activeSnapshot.kind != activeWeaponKind)
        {
            EquipWeaponById(activeSnapshot.weaponId);
        }
        else
        {
            ApplyActiveSnapshot();
        }
    }

    public void ResetActiveSnapshotToDefaults()
    {
        activeSnapshot = WeaponVisualTuningSnapshot.CreateDefault(activeWeaponKind);
        ApplyActiveSnapshot();
        if (activeWeaponKind == WeaponVisualKind.Melee)
        {
            ApplyWeaponTestMeleeDefaults();
            PullTuningFromPlayer();
        }
    }

    public bool TriggerTestAttack()
    {
        ResolvePlayerReferences();
        ApplyActiveSnapshot();
        return player != null && WeaponVisualTuningApplier.TryRequestTestAttack(player, activeWeaponKind);
    }

    void UnequipAllWeapons()
    {
        meleeCombat?.UnequipWeapon();
        rangedCombat?.UnequipBow();
        magicCombat?.UnequipStaff();
    }

}
