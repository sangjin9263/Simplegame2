#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Weapon_test Play 모드에서 VFX 비주얼을 실시간으로 조절하는 에디터 창입니다.
public class WeaponTestTuningWindow : EditorWindow
{
    const string SceneName = "Weapon_test";
    const string ProfileFolder = "Assets/Data/WeaponTuning";

    WeaponTestSession session;
    WeaponVisualTuningProfile profileAsset;
    Vector2 scroll;
    bool liveApply = true;
    bool showProjectileStats = true;
    bool showSpawnPoint = true;
    bool showVisual = true;
    bool showTiming = true;
    List<WeaponDefinitionRow> csvWeaponRows;

    [MenuItem("Simplegame2/Weapon Test Tuning")]
    static void OpenWindow()
    {
        WeaponTestTuningWindow window = GetWindow<WeaponTestTuningWindow>("Weapon Test Tuning");
        window.minSize = new Vector2(360f, 520f);
        window.Show();
    }

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        RefreshSession();
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    void OnPlayModeChanged(PlayModeStateChange state)
    {
        RefreshSession();
        Repaint();
    }

    void Update()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        WeaponTestSession current = FindSession();
        if (current != session)
        {
            session = current;
            Repaint();
        }
    }

    void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(6f);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Weapon_test 씬을 연 뒤 Play 모드에서 사용하세요.", MessageType.Info);
            if (GUILayout.Button("Weapon_test 씬 열기"))
            {
                OpenWeaponTestScene();
            }

            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Contains(SceneName))
        {
            EditorGUILayout.HelpBox("현재 씬이 Weapon_test 가 아닙니다.", MessageType.Warning);
            return;
        }

        RefreshSession();
        if (session == null)
        {
            EditorGUILayout.HelpBox("WeaponTestSession 을 찾을 수 없습니다. Setup Weapon Test Scene 을 다시 실행하세요.", MessageType.Error);
            return;
        }

        DrawWeaponSelector();
        EditorGUILayout.Space(4f);
        DrawActionButtons();
        EditorGUILayout.Space(6f);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSnapshotEditor(session.ActiveSnapshot);
        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        EditorGUILayout.LabelField("Weapon Test — VFX Visual Tuning", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("단축키: F8=패널, T=마우스, 1/2/3=무기", EditorStyles.miniLabel);
        liveApply = EditorGUILayout.ToggleLeft("슬라이더 변경 즉시 적용", liveApply);
    }

    void DrawWeaponSelector()
    {
        if (csvWeaponRows == null)
        {
            csvWeaponRows = new List<WeaponDefinitionRow>(WeaponDefinitionTable.LoadForEditing().Rows);
        }

        if (csvWeaponRows.Count == 0)
        {
            EditorGUILayout.HelpBox("weapon_default.csv 에 무기가 없습니다.", MessageType.Warning);
            return;
        }

        string[] weaponNames = new string[csvWeaponRows.Count];
        int selectedIndex = 0;
        int activeWeaponId = session.ActiveWeaponId;
        for (int i = 0; i < csvWeaponRows.Count; i++)
        {
            weaponNames[i] = string.IsNullOrEmpty(csvWeaponRows[i].weaponName)
                ? csvWeaponRows[i].weaponId.ToString()
                : csvWeaponRows[i].weaponName;
            if (csvWeaponRows[i].weaponId == activeWeaponId)
            {
                selectedIndex = i;
            }
        }

        EditorGUILayout.LabelField("무기 (CSV Weapon_Name)", EditorStyles.miniLabel);
        int newIndex = EditorGUILayout.Popup(selectedIndex, weaponNames);
        if (newIndex != selectedIndex)
        {
            session.EquipWeaponById(csvWeaponRows[newIndex].weaponId);
        }
    }

    void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("플레이어에서 읽기"))
        {
            session.PullTuningFromPlayer();
        }

        if (GUILayout.Button("적용"))
        {
            session.ApplyActiveSnapshot();
        }

        if (GUILayout.Button("테스트 공격"))
        {
            session.TriggerTestAttack();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("기본값으로 리셋"))
        {
            session.ResetActiveSnapshotToDefaults();
        }

        profileAsset = (WeaponVisualTuningProfile)EditorGUILayout.ObjectField(profileAsset, typeof(WeaponVisualTuningProfile), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("프로필 → 현재값"))
        {
            if (profileAsset != null)
            {
                session.LoadSnapshot(profileAsset.snapshot.Clone());
            }
        }

        if (GUILayout.Button("현재값 -> 프로필 저장"))
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[WeaponTestTuning] 프로필 저장은 Play 모드를 종료한 뒤 사용하세요.");
            }
            else
            {
                SaveSnapshotToProfile();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawSnapshotEditor(WeaponVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        EditorGUI.BeginChangeCheck();

        showTiming = EditorGUILayout.Foldout(showTiming, "공격 타이밍", true);
        if (showTiming)
        {
            EditorGUI.indentLevel++;
            snapshot.attackCooldown = EditorGUILayout.FloatField("Cooldown", snapshot.attackCooldown);
            snapshot.attackActiveDelay = EditorGUILayout.FloatField("Active Delay", snapshot.attackActiveDelay);
            snapshot.attackAnimDuration = EditorGUILayout.FloatField("Anim Duration", snapshot.attackAnimDuration);
            EditorGUI.indentLevel--;
        }

        showSpawnPoint = EditorGUILayout.Foldout(showSpawnPoint, "시작 위치 (Spawn Point)", true);
        if (showSpawnPoint)
        {
            EditorGUI.indentLevel++;
            snapshot.spawnForwardOffset = EditorGUILayout.FloatField("Forward Offset", snapshot.spawnForwardOffset);
            if (snapshot.kind == WeaponVisualKind.Melee)
            {
                snapshot.spawnSideOffset = EditorGUILayout.FloatField("Side Offset", snapshot.spawnSideOffset);
            }

            snapshot.spawnHeightOffset = EditorGUILayout.FloatField("Height Offset", snapshot.spawnHeightOffset);
            EditorGUI.indentLevel--;
        }

        showVisual = EditorGUILayout.Foldout(showVisual, snapshot.kind == WeaponVisualKind.Melee
            ? "비주얼 (크기·회전 — 타격 판정도 동일 비율)"
            : "비주얼 (크기·회전)", true);
        if (showVisual)
        {
            EditorGUI.indentLevel++;
            string scaleLabel = snapshot.kind == WeaponVisualKind.Melee ? "Slash Scale (VFX + Hitbox)" : "Visual Scale";
            snapshot.visualScale = EditorGUILayout.FloatField(scaleLabel, snapshot.visualScale);
            snapshot.visualRotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", snapshot.visualRotationOffset);
            EditorGUI.indentLevel--;
        }

        showProjectileStats = EditorGUILayout.Foldout(showProjectileStats, "투사체 / 판정", true);
        if (showProjectileStats)
        {
            EditorGUI.indentLevel++;
            snapshot.moveSpeed = EditorGUILayout.FloatField("Speed", snapshot.moveSpeed);

            if (snapshot.kind != WeaponVisualKind.Melee)
            {
                snapshot.maxRange = EditorGUILayout.FloatField("Max Range", snapshot.maxRange);
                snapshot.hitRadius = EditorGUILayout.FloatField("Hit Radius", snapshot.hitRadius);
            }

            if (snapshot.kind == WeaponVisualKind.Magic)
            {
                snapshot.turnSpeed = EditorGUILayout.FloatField("Turn Speed", snapshot.turnSpeed);
                snapshot.explosionRadius = EditorGUILayout.FloatField("Explosion Radius", snapshot.explosionRadius);
                snapshot.maxHitTargets = EditorGUILayout.IntField("Max Hit Targets", snapshot.maxHitTargets);
                snapshot.maxLifetime = EditorGUILayout.FloatField("Max Lifetime", snapshot.maxLifetime);
                snapshot.targetSearchRange = EditorGUILayout.FloatField("Search Range", snapshot.targetSearchRange);
            }

            EditorGUI.indentLevel--;
        }

        if (EditorGUI.EndChangeCheck() && liveApply)
        {
            session.ApplyActiveSnapshot();
        }
    }

    void SaveSnapshotToProfile()
    {
        session.PullTuningFromPlayer();
        WeaponVisualTuningSnapshot snapshot = session.ActiveSnapshot.Clone();

        if (profileAsset == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            if (!AssetDatabase.IsValidFolder(ProfileFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "WeaponTuning");
            }

            string path = $"{ProfileFolder}/Weapon_{snapshot.weaponId}.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            profileAsset = CreateInstance<WeaponVisualTuningProfile>();
            AssetDatabase.CreateAsset(profileAsset, path);
        }

        profileAsset.snapshot = snapshot;
        EditorUtility.SetDirty(profileAsset);
        AssetDatabase.SaveAssets();
        Debug.Log("[WeaponTestTuning] Saved profile: " + AssetDatabase.GetAssetPath(profileAsset));
    }

    void RefreshSession()
    {
        session = FindSession();
    }

    static WeaponTestSession FindSession()
    {
        return Object.FindObjectOfType<WeaponTestSession>();
    }

    static void OpenWeaponTestScene()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Weapon_test.unity");
    }
}
#endif
