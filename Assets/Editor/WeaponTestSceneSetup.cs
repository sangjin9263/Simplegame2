#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Weapon_test: SampleScene과 동일한 Player/Camera + Main_land + 허수아비.
public static class WeaponTestSceneSetup
{
    const string ScenePath = "Assets/Scenes/Weapon_test.unity";
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string PlayerPrefabPath = "Assets/Prefabs/Char/SPUM_main.prefab";
    const string DummyPrefabPath = "Assets/Prefabs/Char/mon/SPUM_orc_m1.prefab";
    const string MainLandPrefabPath = "Assets/Prefabs/Land/Main_land.prefab";
    const float MainLandSurfaceY = 2f;
    const float MainLandChunkSize = 6f;
    const int MainLandGridHalfExtent = 1;

    [MenuItem("Simplegame2/Setup Weapon Test Scene")]
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

        GameObject player = CreatePlayerLikeSampleScene();
        GameObject dummy = CreateDummyLikeSampleScene();
        CreateMainCameraLikeSampleScene(player);
        CreateWeaponTestSession(player, dummy);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[WeaponTestSceneSetup] Weapon_test scene ready (SampleScene player/camera + 3x3 Main_land).");
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
            Debug.LogError("[WeaponTestSceneSetup] Main_land prefab not found: " + MainLandPrefabPath);
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

    static GameObject CreatePlayerLikeSampleScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[WeaponTestSceneSetup] Player prefab not found: " + PlayerPrefabPath);
            return null;
        }

        GameObject player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        player.name = "Player";
        player.tag = "Player";

        PlaceCharacterOnMainLand(player, 0f, 1f);

        // SampleScene은 WorldChunkStreamer가 NotifyWorldReady()를 호출합니다.
        // Weapon_test는 Main_land만 두므로, 월드 대기 게이트를 끕니다.
        PlayerGameplayStartGate startGate = player.GetComponent<PlayerGameplayStartGate>();
        if (startGate != null)
        {
            startGate.enabled = false;
        }

        return player;
    }

    static GameObject CreateDummyLikeSampleScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[WeaponTestSceneSetup] Dummy prefab not found: " + DummyPrefabPath);
            return null;
        }

        GameObject dummy = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        dummy.name = "TrainingDummy";

        PlaceCharacterOnMainLand(dummy, 2.5f, -1f);
        ConfigureTrainingDummy(dummy);
        return dummy;
    }

    static void ConfigureTrainingDummy(GameObject dummy)
    {
        DisableComponent<MonsterAttack>(dummy);
        DisableComponent<MonsterRangedAttack>(dummy);

        MonsterMovement movement = dummy.GetComponent<MonsterMovement>();
        if (movement != null)
        {
            movement.enabled = true;
            SerializedObject serializedMovement = new SerializedObject(movement);
            SerializedProperty moveSpeedProperty = serializedMovement.FindProperty("moveSpeed");
            if (moveSpeedProperty != null)
            {
                moveSpeedProperty.floatValue = 0f;
            }

            serializedMovement.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(movement);
        }

        MonsterHealth health = dummy.GetComponent<MonsterHealth>();
        if (health != null)
        {
            SerializedObject serializedHealth = new SerializedObject(health);
            SerializedProperty infiniteHpProperty = serializedHealth.FindProperty("infiniteHp");
            if (infiniteHpProperty != null)
            {
                infiniteHpProperty.boolValue = true;
            }

            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(health);
        }

        MonsterHitReaction hitReaction = dummy.GetComponent<MonsterHitReaction>();
        if (hitReaction == null)
        {
            hitReaction = dummy.GetComponentInChildren<MonsterHitReaction>();
        }

        if (hitReaction != null)
        {
            SerializedObject serializedHitReaction = new SerializedObject(hitReaction);
            SerializedProperty enableKnockbackProperty = serializedHitReaction.FindProperty("enableKnockback");
            if (enableKnockbackProperty != null)
            {
                enableKnockbackProperty.boolValue = false;
            }

            serializedHitReaction.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(hitReaction);
        }
    }

    static void CreateWeaponTestSession(GameObject player, GameObject dummy)
    {
        GameObject sessionObject = new GameObject("WeaponTestSession");
        WeaponTestSession session = sessionObject.AddComponent<WeaponTestSession>();
        sessionObject.AddComponent<WeaponVisualTuningRuntime>();

        SerializedObject serializedSession = new SerializedObject(session);
        serializedSession.FindProperty("player").objectReferenceValue = player;
        if (dummy != null)
        {
            serializedSession.FindProperty("trainingDummy").objectReferenceValue = dummy.transform;
        }

        serializedSession.ApplyModifiedPropertiesWithoutUndo();
    }

    static void DisableComponent<T>(GameObject root) where T : Behaviour
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            components[i].enabled = false;
        }
    }

    static void CreateMainCameraLikeSampleScene(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        RectTransform followTarget = player.GetComponent<RectTransform>();
        if (followTarget == null)
        {
            Debug.LogError("[WeaponTestSceneSetup] Player RectTransform not found.");
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Transform cameraTransform = cameraObject.transform;
        cameraTransform.SetPositionAndRotation(
            new Vector3(0f, 6f, -8f),
            new Quaternion(0.20106587f, 0f, 0f, 0.9795777f));
        cameraTransform.localScale = new Vector3(1f, 0.99999994f, 1f);

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
        serializedFollowCamera.FindProperty("distance").floatValue = 8f;
        serializedFollowCamera.FindProperty("yawAngle").floatValue = 0f;
        serializedFollowCamera.FindProperty("pitchAngle").floatValue = 25f;
        serializedFollowCamera.FindProperty("lockVerticalRotation").boolValue = true;
        serializedFollowCamera.FindProperty("fixedPitchAngle").floatValue = 25f;
        serializedFollowCamera.FindProperty("toggleVerticalLockKey").intValue = 282;
        serializedFollowCamera.FindProperty("allowRuntimeToggle").boolValue = true;
        serializedFollowCamera.FindProperty("mouseSensitivityX").floatValue = 3f;
        serializedFollowCamera.FindProperty("mouseSensitivityY").floatValue = 2f;
        serializedFollowCamera.FindProperty("minPitch").floatValue = 25f;
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

        serializedFollowCamera.FindProperty("targetCamera").objectReferenceValue = null;
        serializedFollowCamera.ApplyModifiedPropertiesWithoutUndo();
        AlignCameraToFollowTarget(cameraTransform, followTarget, followCamera);
    }

    static void PlaceCharacterOnMainLand(GameObject character, float worldX, float yawDegrees)
    {
        if (character == null)
        {
            return;
        }

        Transform characterTransform = character.transform;
        characterTransform.position = new Vector3(worldX, MainLandSurfaceY, 0f);
        characterTransform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        RectTransform rectTransform = character.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(worldX, MainLandSurfaceY);
            rectTransform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(characterTransform);
        if (rectTransform != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform);
        }
    }

    static void AlignCameraToFollowTarget(Transform cameraTransform, Transform followTarget, ThirdPersonFollowCamera followCamera)
    {
        if (cameraTransform == null || followTarget == null || followCamera == null)
        {
            return;
        }

        float distance = 8f;
        float pitchAngle = 25f;
        float yawAngle = 0f;

        SerializedObject serializedFollowCamera = new SerializedObject(followCamera);
        SerializedProperty distanceProperty = serializedFollowCamera.FindProperty("distance");
        SerializedProperty pitchProperty = serializedFollowCamera.FindProperty("fixedPitchAngle");
        SerializedProperty yawProperty = serializedFollowCamera.FindProperty("yawAngle");
        if (distanceProperty != null)
        {
            distance = distanceProperty.floatValue;
        }

        if (pitchProperty != null)
        {
            pitchAngle = pitchProperty.floatValue;
        }

        if (yawProperty != null)
        {
            yawAngle = yawProperty.floatValue;
        }

        Quaternion orbitRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        Vector3 offset = orbitRotation * new Vector3(0f, 0f, -distance);
        cameraTransform.position = followTarget.position + offset;

        Vector3 lookDirection = followTarget.position - cameraTransform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            cameraTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}
#endif
