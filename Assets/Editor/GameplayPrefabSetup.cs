#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 플레이어·몬스터 프리팹에 게임플레이 컴포넌트를 한 번에 붙입니다.
[InitializeOnLoad]
public static class GameplayPrefabSetup
{
    const string SessionKey = "Simplegame2_GameplayPrefabsReady";

    static GameplayPrefabSetup()
    {
        EditorApplication.delayCall += TryAutoSetupOnce;
    }

    static void TryAutoSetupOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        GameObject playerSample = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject monsterSample = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Char/mon/SPUM_orc_m1.prefab");

        bool playerReady = playerSample != null
            && playerSample.GetComponent<CharacterController>() != null
            && playerSample.GetComponent<PlayerRangedCombat>() != null;
        bool monsterReady = monsterSample != null
            && monsterSample.GetComponent<CharacterController>() != null;

        if (playerReady && monsterReady)
        {
            SessionState.SetBool(SessionKey, true);
            return;
        }

        SetupAll();
        SessionState.SetBool(SessionKey, true);
    }

    const string PlayerPrefabPath = "Assets/Prefabs/Char/SPUM_main.prefab";

    [MenuItem("Simplegame2/Setup Gameplay Prefabs")]
    public static void SetupAll()
    {
        SetupPlayerPrefab();
        SetupMonsterPrefabs();
        SetupPlayersInLoadedScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[GameplayPrefabSetup] Finished. Player prefab + open scenes updated.");
    }

    static void SetupPlayerPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[GameplayPrefabSetup] Player prefab not found: " + PlayerPrefabPath);
            return;
        }

        SetupPlayerGameObject(prefabRoot);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    static void SetupPlayersInLoadedScenes()
    {
        int sceneCount = UnityEditor.SceneManagement.EditorSceneManager.sceneCount;
        for (int sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                PlayerMovement[] players = roots[rootIndex].GetComponentsInChildren<PlayerMovement>(true);
                for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
                {
                    if (players[playerIndex] == null)
                    {
                        continue;
                    }

                    SetupPlayerGameObject(players[playerIndex].gameObject);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }
    }

    static void SetupPlayerGameObject(GameObject playerRoot)
    {
        EnsureComponent<PlayerMovement>(playerRoot);
        ConfigurePlayerCharacterController(playerRoot);
        EnsureComponent<PlayerWorldPosition>(playerRoot);
        EnsureComponent<PlayerStats>(playerRoot);
        EnsureComponent<PlayerHealth>(playerRoot);
        EnsureComponent<PlayerWeaponCombat>(playerRoot);
        EnsureComponent<PlayerRangedCombat>(playerRoot);
        EnsureComponent<PlayerMagicCombat>(playerRoot);
        EnsureComponent<PlayerGameplayStartGate>(playerRoot);
    }

    static void SetupMonsterPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("SPUM_orc_m t:Prefab", new[] { "Assets/Prefabs/Char/mon" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null)
            {
                continue;
            }

            EnsureComponent<MonsterMovement>(prefabRoot);
            ConfigureMonsterCharacterController(prefabRoot);
            EnsureComponent<MonsterHealth>(prefabRoot);
            EnsureComponent<MonsterHitReaction>(prefabRoot);
            EnsureComponent<MonsterAttack>(prefabRoot);
            EnsureComponent<MonsterRangedAttack>(prefabRoot);
            EnsureComponent<MonsterFarDespawn>(prefabRoot);

            bool isRangedMonster = IsRangedMonsterPrefab(prefabRoot.name);
            MonsterAttack meleeAttack = prefabRoot.GetComponent<MonsterAttack>();
            if (meleeAttack != null)
            {
                meleeAttack.enabled = !isRangedMonster;
            }

            MonsterRangedAttack rangedAttack = prefabRoot.GetComponent<MonsterRangedAttack>();
            if (rangedAttack != null)
            {
                rangedAttack.enabled = isRangedMonster;
                if (IsArrowRangedMonsterPrefab(prefabRoot.name))
                {
                    ApplyDefaultArrowRangedSettings(rangedAttack);
                }
            }

            Transform unitRoot = prefabRoot.transform.Find("UnitRoot");
            if (unitRoot != null)
            {
                EnsureComponent<BillboardFaceCamera>(unitRoot.gameObject);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    static void EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target.GetComponent<T>() == null)
        {
            target.AddComponent<T>();
        }
    }

    static void ConfigurePlayerCharacterController(GameObject root)
    {
        CharacterController controller = root.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = root.AddComponent<CharacterController>();
        }

        controller.height = 1.05f;
        controller.radius = 0.22f;
        controller.center = new Vector3(0f, 0.525f, 0f);
        controller.slopeLimit = 50f;
        controller.stepOffset = 0.4f;
        controller.skinWidth = 0.02f;
        controller.minMoveDistance = 0f;
    }

    static void ConfigureMonsterCharacterController(GameObject root)
    {
        CharacterController controller = root.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = root.AddComponent<CharacterController>();
        }

        controller.height = 1.05f;
        controller.radius = 0.22f;
        controller.center = new Vector3(0f, 0.525f, 0f);
        controller.slopeLimit = 50f;
        controller.stepOffset = 0.35f;
        controller.skinWidth = 0.02f;
        controller.minMoveDistance = 0f;
    }

    static bool IsArrowRangedMonsterPrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return false;
        }

        return prefabName.IndexOf("SPUM_orc_m7", System.StringComparison.OrdinalIgnoreCase) >= 0
            || prefabName.IndexOf("SPUM_orc_m8", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void ApplyDefaultArrowRangedSettings(MonsterRangedAttack rangedAttack)
    {
        SerializedObject serializedAttack = new SerializedObject(rangedAttack);
        serializedAttack.FindProperty("arrowVisualRotationOffset").vector3Value = new Vector3(-90f, -90f, -45f);
        serializedAttack.FindProperty("arrowVisualScale").floatValue = 0.35f;
        serializedAttack.FindProperty("projectileSpawnHeightOffset").floatValue = 0.65f;
        serializedAttack.FindProperty("projectileSpawnForwardOffset").floatValue = 0.55f;
        serializedAttack.FindProperty("arrowProjectileSpeed").floatValue = 22f;
        serializedAttack.FindProperty("arrowProjectileMaxRange").floatValue = 16f;
        serializedAttack.FindProperty("arrowProjectileHitRadius").floatValue = 0.6f;
        serializedAttack.FindProperty("targetAimHeightOffset").floatValue = 0.45f;
        serializedAttack.ApplyModifiedPropertiesWithoutUndo();
    }

    static bool IsRangedMonsterPrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return false;
        }

        return prefabName.IndexOf("SPUM_orc_m7", System.StringComparison.OrdinalIgnoreCase) >= 0
            || prefabName.IndexOf("SPUM_orc_m8", System.StringComparison.OrdinalIgnoreCase) >= 0
            || prefabName.IndexOf("SPUM_orc_m9", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif
