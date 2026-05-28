using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Player HUD 프리팹 생성·갱신 (에디터 전용). PlayerStatusPanel(LV/HP)은 유지합니다.
public static class PlayerHudPrefabCreator
{
    const string PrefabFolder = "Assets/Prefabs/UI";
    const string PrefabPath = PrefabFolder + "/PlayerHudRoot.prefab";
    const string ResourcesPrefabPath = "Assets/Resources/PlayerHudRoot.prefab";

    [InitializeOnLoadMethod]
    static void AutoCreatePrefabOnLoad()
    {
        EditorApplication.delayCall += TryAutoCreatePrefab;
    }

    static void TryAutoCreatePrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        CreatePrefabAndSetupScene();
    }

    [MenuItem("Tools/Game/Create Player HUD Prefab")]
    public static void CreatePrefabAndSetupScene()
    {
        RebuildPrefabAsset();
        EnsureHudInActiveScene();
    }

    [MenuItem("Tools/Game/Rebuild Player HUD Prefab")]
    public static void RebuildPrefabAndSetupScene()
    {
        PlayerStatsCsvSync.SyncFromMenu();
        RebuildPrefabAsset();
        EnsureHudInActiveScene();
        Debug.Log("[PlayerHudPrefabCreator] 미니맵·타이머·인벤만 갱신. LV/HP 패널은 그대로 둡니다.");
    }

    [MenuItem("Tools/Game/Restore Player Status Panel Layout")]
    public static void RestoreStatusPanelLayoutMenu()
    {
        RestoreStatusPanelLayoutInPrefab();
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerHudPrefabCreator] LV/HP 패널 레이아웃 복구 완료.");
    }

    [MenuItem("Tools/Game/Remove Runtime HUD Bootstrap From Scene")]
    public static void RemoveRuntimeBootstrapFromScene()
    {
        RemoveHudBootstrapsFromScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[PlayerHudPrefabCreator] GameHudBootstrap 제거됨.");
    }

    static void RebuildPrefabAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            PlayerHudUiLayoutFactory.BuiltHud builtHud = PlayerHudUiLayoutFactory.CreateHudRoot();
            SavePrefab(builtHud.root);
            Object.DestroyImmediate(builtHud.root);
        }
        else
        {
            using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root.GetComponent<PlayerHudRoot>() == null)
                {
                    root.AddComponent<PlayerHudRoot>();
                }

                if (root.GetComponent<PlayerStatusHudView>() == null)
                {
                    root.AddComponent<PlayerStatusHudView>();
                }

                root.GetComponent<PlayerHudRoot>().EnsureLayout();
                RestoreStatusPanelLayoutInPrefabRoot(root);
                RemoveMonsterBlipsFromPrefab(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RestoreStatusPanelLayoutInPrefab()
    {
        using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
        {
            RestoreStatusPanelLayoutInPrefabRoot(scope.prefabContentsRoot);
            RemoveMonsterBlipsFromPrefab(scope.prefabContentsRoot);
        }
    }

    static void RestoreStatusPanelLayoutInPrefabRoot(GameObject root)
    {
        Transform status = root.transform.Find("Canvas/PlayerStatusPanel");
        if (status == null)
        {
            return;
        }

        RectTransform panel = status.GetComponent<RectTransform>();
        panel.anchoredPosition = new Vector2(24f, 24f);

        Transform levelBox = status.Find("LevelBox");
        if (levelBox == null)
        {
            return;
        }

        RectTransform levelRect = levelBox.GetComponent<RectTransform>();
        levelRect.pivot = new Vector2(0f, 0f);
        levelRect.anchorMin = new Vector2(0f, 0f);
        levelRect.anchorMax = new Vector2(0f, 1f);
        levelRect.anchoredPosition = Vector2.zero;
        levelRect.sizeDelta = new Vector2(72f, 0f);

        Transform levelBackground = levelBox.Find("LevelBackground");
        if (levelBackground != null)
        {
            RectTransform bgRect = levelBackground.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }
    }

    [MenuItem("Tools/Game/Clean Up Monster Blips From HUD")]
    public static void CleanUpMonsterBlipsMenu()
    {
        using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
        {
            RemoveMonsterBlipsFromPrefab(scope.prefabContentsRoot);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesPrefabPath) != null)
        {
            using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(ResourcesPrefabPath))
            {
                RemoveMonsterBlipsFromPrefab(scope.prefabContentsRoot);
            }
        }

        RemoveMonsterBlipsFromSceneHud();
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerHudPrefabCreator] MonsterBlip_* 정리 완료. PlayerBlip만 남깁니다.");
    }

    static void RemoveMonsterBlipsFromSceneHud()
    {
        RadarMinimapView[] minimaps = Object.FindObjectsByType<RadarMinimapView>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < minimaps.Length; i++)
        {
            RemoveMonsterBlipsFromTransform(FindBlipsRoot(minimaps[i].transform));
        }
    }

    static void RemoveMonsterBlipsFromPrefab(GameObject root)
    {
        RemoveMonsterBlipsFromTransform(FindBlipsRoot(root.transform));
    }

    static Transform FindBlipsRoot(Transform hudRoot)
    {
        Transform blips = hudRoot.Find("Canvas/RadarMinimap/MapClip/Blips");
        if (blips != null)
        {
            return blips;
        }

        blips = hudRoot.Find("Canvas/RadarMinimap/Blips");
        return blips;
    }

    static void RemoveMonsterBlipsFromTransform(Transform blips)
    {
        if (blips == null)
        {
            return;
        }

        for (int i = blips.childCount - 1; i >= 0; i--)
        {
            Transform child = blips.GetChild(i);
            if (child.name.StartsWith("MonsterBlip_"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    static void SavePrefab(GameObject root)
    {
        if (!Directory.Exists(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
        }

        string resourcesFolder = Path.GetDirectoryName(ResourcesPrefabPath);
        if (!string.IsNullOrEmpty(resourcesFolder) && !Directory.Exists(resourcesFolder))
        {
            Directory.CreateDirectory(resourcesFolder);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(ResourcesPrefabPath);
        }

        AssetDatabase.CopyAsset(PrefabPath, ResourcesPrefabPath);
    }

    static void EnsureHudInActiveScene()
    {
        RemoveHudBootstrapsFromScene();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            return;
        }

        if (Object.FindFirstObjectByType<PlayerStatusHudView>() == null)
        {
            PrefabUtility.InstantiatePrefab(prefab);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    static void RemoveHudBootstrapsFromScene()
    {
        PlayerHudBootstrap[] bootstraps = Object.FindObjectsByType<PlayerHudBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < bootstraps.Length; i++)
        {
            if (bootstraps[i] != null)
            {
                Object.DestroyImmediate(bootstraps[i].gameObject);
            }
        }

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (obj != null && obj.name == "GameHudBootstrap")
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}
