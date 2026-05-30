#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MonsterTestSceneSetup
{
    const string ScenePath = "Assets/Scenes/Monster_test.unity";
    const string PlayerPrefabPath = "Assets/Prefabs/Char/SPUM_main.prefab";
    const string MainLandPrefabPath = "Assets/Prefabs/Land/Main_land.prefab";
    const float MainLandSurfaceY = 2f;
    const float MainLandChunkSize = 6f;
    const int MainLandGridHalfExtent = 1;
    const float PlayerWorldZ = -0.75f;
    const float SpawnDistanceFromPlayer = 8f;

    [MenuItem("Simplegame2/Setup Monster Test Scene")]
    public static void SetupFromMenu()
    {
        SetupScene();
    }

    public static void SetupScene()
    {
        EnsureSceneAssetExists();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ClearRootObjects(scene);

        ApplyGrayRenderSettings();
        CreateMainLand();
        CreateDirectionalLight();
        EnsureEventSystem();

        GameObject player = CreatePlayerTarget();
        Transform monsterAnchor = CreateMonsterSpawnAnchor();
        CreateMainCamera(player);
        CreateMonsterTestSession(player, monsterAnchor);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[MonsterTestSceneSetup] Monster_test scene ready.");
    }

    static void EnsureSceneAssetExists()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            return;
        }

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, ScenePath);
    }

    static void ClearRootObjects(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Object.DestroyImmediate(roots[i]);
        }
    }

    static void ApplyGrayRenderSettings()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f, 1f);
        RenderSettings.fog = false;
    }

    static void CreateMainLand()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainLandPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[MonsterTestSceneSetup] Main_land prefab not found: " + MainLandPrefabPath);
            return;
        }

        GameObject gridRoot = new GameObject("MainLandGrid");
        gridRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        for (int chunkX = -MainLandGridHalfExtent; chunkX <= MainLandGridHalfExtent; chunkX++)
        {
            for (int chunkZ = -MainLandGridHalfExtent; chunkZ <= MainLandGridHalfExtent; chunkZ++)
            {
                GameObject mainLand = PrefabUtility.InstantiatePrefab(prefab, gridRoot.transform) as GameObject;
                mainLand.name = $"Main_land_{chunkX}_{chunkZ}";
                mainLand.transform.localPosition = new Vector3(
                    chunkX * MainLandChunkSize,
                    0f,
                    chunkZ * MainLandChunkSize);
                mainLand.transform.localRotation = Quaternion.identity;
            }
        }
    }

    static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2f;
        light.color = Color.white;
        light.colorTemperature = 5000f;
        light.useColorTemperature = true;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    static GameObject CreatePlayerTarget()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[MonsterTestSceneSetup] Player prefab not found: " + PlayerPrefabPath);
            return null;
        }

        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        player.name = "PlayerTarget";
        player.tag = "Player";
        PlaceCharacterOnMainLand(player, 0f, 0f, PlayerWorldZ);

        PlayerGameplayStartGate startGate = player.GetComponent<PlayerGameplayStartGate>();
        if (startGate != null)
        {
            startGate.enabled = false;
        }

        return player;
    }

    static Transform CreateMonsterSpawnAnchor()
    {
        GameObject anchorObject = new GameObject("MonsterSpawnAnchor");
        float spawnZ = PlayerWorldZ + SpawnDistanceFromPlayer;
        anchorObject.transform.position = new Vector3(0f, MainLandSurfaceY, spawnZ);
        anchorObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        return anchorObject.transform;
    }

    static void CreateMonsterTestSession(GameObject player, Transform monsterAnchor)
    {
        GameObject sessionObject = new GameObject("MonsterTestSession");
        MonsterTestSession session = sessionObject.AddComponent<MonsterTestSession>();
        sessionObject.AddComponent<MonsterVisualTuningRuntime>();

        SerializedObject serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("playerTarget").objectReferenceValue = player;
        serializedSession.FindProperty("monsterSpawnAnchor").objectReferenceValue = monsterAnchor;
        serializedSession.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateMainCamera(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        RectTransform followTarget = player.GetComponent<RectTransform>();
        if (followTarget == null)
        {
            Debug.LogError("[MonsterTestSceneSetup] Player RectTransform not found.");
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Transform cameraTransform = cameraObject.transform;
        cameraTransform.SetPositionAndRotation(
            new Vector3(0f, 6f, -5f),
            Quaternion.Euler(20f, 0f, 0f));

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0f);
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        camera.depth = -1;

        cameraObject.AddComponent<AudioListener>();

        UniversalAdditionalCameraData urpCameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpCameraData.renderPostProcessing = true;

        ThirdPersonFollowCamera followCamera = cameraObject.AddComponent<ThirdPersonFollowCamera>();
        SerializedObject serializedFollowCamera = new SerializedObject(followCamera);
        serializedFollowCamera.FindProperty("followTarget").objectReferenceValue = followTarget;
        serializedFollowCamera.FindProperty("distance").floatValue = 10f;
        serializedFollowCamera.FindProperty("yawAngle").floatValue = 0f;
        serializedFollowCamera.FindProperty("pitchAngle").floatValue = 25f;
        serializedFollowCamera.FindProperty("lockVerticalRotation").boolValue = true;
        serializedFollowCamera.FindProperty("fixedPitchAngle").floatValue = 25f;
        serializedFollowCamera.FindProperty("toggleVerticalLockKey").intValue = 282;
        serializedFollowCamera.FindProperty("allowRuntimeToggle").boolValue = true;
        serializedFollowCamera.FindProperty("mouseSensitivityX").floatValue = 3f;
        serializedFollowCamera.FindProperty("mouseSensitivityY").floatValue = 2f;
        serializedFollowCamera.FindProperty("minPitch").floatValue = 15f;
        serializedFollowCamera.FindProperty("maxPitch").floatValue = 70f;
        serializedFollowCamera.FindProperty("lockCursorOnPlay").boolValue = true;

        SerializedProperty toggleCursorLockKeyProperty = serializedFollowCamera.FindProperty("toggleCursorLockKey");
        if (toggleCursorLockKeyProperty != null)
        {
            toggleCursorLockKeyProperty.intValue = (int)KeyCode.T;
        }

        SerializedProperty relockCursorOnLeftClickProperty = serializedFollowCamera.FindProperty("relockCursorOnLeftClick");
        if (relockCursorOnLeftClickProperty != null)
        {
            relockCursorOnLeftClickProperty.boolValue = false;
        }

        serializedFollowCamera.ApplyModifiedPropertiesWithoutUndo();
    }

    static void PlaceCharacterOnMainLand(GameObject character, float worldX, float yawDegrees, float worldZ)
    {
        if (character == null)
        {
            return;
        }

        Vector3 worldPosition = new Vector3(worldX, MainLandSurfaceY, worldZ);
        Quaternion worldRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Transform characterTransform = character.transform;
        characterTransform.SetPositionAndRotation(worldPosition, worldRotation);

        RectTransform rectTransform = character.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = worldPosition;
            rectTransform.rotation = worldRotation;
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(characterTransform);
        if (rectTransform != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform);
        }
    }
}
#endif
