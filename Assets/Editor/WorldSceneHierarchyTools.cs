using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 월드 스트리밍 런타임 오브젝트가 Hierarchy/씬 파일을 지저분하게 만들지 않도록 정리합니다.
[InitializeOnLoad]
public static class WorldSceneHierarchyTools
{
    const string FloorCloneName = "Floor_Grass_Bright(Clone)";
    const string WorldChunksName = "WorldChunks";
    const string WorldStreamerName = "WorldStreamer";

    static WorldSceneHierarchyTools()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        EditorApplication.delayCall += CleanupAfterPlayMode;
    }

    static void CleanupAfterPlayMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        CleanupRuntimeWorldObjects(markSceneDirty: true);
    }

    [MenuItem("Tools/Game/Clean Scene Hierarchy (Runtime World)")]
    public static void CleanActiveSceneFromMenu()
    {
        int removed = CleanupRuntimeWorldObjects(markSceneDirty: true);
        OrganizeSceneRoots();
        Debug.Log("[WorldSceneHierarchyTools] 런타임 바닥/청크 " + removed + "개 제거, 씬 루트 정리 완료.");
    }

    public static int CleanupRuntimeWorldObjects(bool markSceneDirty)
    {
        int removed = 0;

        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            Transform target = transforms[i];
            if (target == null)
            {
                continue;
            }

            if (target.name != FloorCloneName)
            {
                continue;
            }

            Object.DestroyImmediate(target.gameObject);
            removed++;
        }

        GameObject worldChunks = GameObject.Find(WorldChunksName);
        if (worldChunks != null)
        {
            Transform chunksTransform = worldChunks.transform;
            for (int i = chunksTransform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(chunksTransform.GetChild(i).gameObject);
                removed++;
            }
        }

        GameObject chunkPool = GameObject.Find("ChunkPool");
        if (chunkPool != null)
        {
            Object.DestroyImmediate(chunkPool);
            removed++;
        }

        if (markSceneDirty && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        return removed;
    }

    [MenuItem("Tools/Game/Organize Scene Hierarchy Roots")]
    public static void OrganizeSceneRoots()
    {
        EnsureRootGroup("--- Systems ---", new[]
        {
            "GameHudBootstrap",
            "MonsterSpawner",
            "WeaponPickupSpawner",
            "EventSystem",
            "PlayerHudRoot"
        });

        EnsureRootGroup("--- World ---", new[]
        {
            WorldStreamerName,
            WorldChunksName,
            "Environment"
        });

        ReparentWorldChunksUnderStreamer();

        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    static void EnsureRootGroup(string groupName, string[] childNames)
    {
        GameObject group = GameObject.Find(groupName);
        if (group == null)
        {
            group = new GameObject(groupName);
        }

        Transform groupTransform = group.transform;

        for (int i = 0; i < childNames.Length; i++)
        {
            GameObject child = GameObject.Find(childNames[i]);
            if (child == null || child.transform.parent == groupTransform)
            {
                continue;
            }

            child.transform.SetParent(groupTransform, true);
        }
    }

    static void ReparentWorldChunksUnderStreamer()
    {
        GameObject streamer = GameObject.Find(WorldStreamerName);
        GameObject worldChunks = GameObject.Find(WorldChunksName);
        if (streamer == null || worldChunks == null)
        {
            return;
        }

        if (worldChunks.transform.parent != streamer.transform)
        {
            worldChunks.transform.SetParent(streamer.transform, true);
        }
    }
}
