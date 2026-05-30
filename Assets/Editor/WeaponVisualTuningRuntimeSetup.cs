#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WeaponVisualTuningRuntimeSetup
{
    const string WeaponTestSceneName = "Weapon_test";

    [MenuItem("Simplegame2/Add Weapon VFX Tuning UI To Weapon Test Scene")]
    static void AddToWeaponTestScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains(WeaponTestSceneName))
        {
            Debug.LogWarning("[WeaponVisualTuningRuntimeSetup] Weapon_test 씬에서 실행하세요.");
            return;
        }

        WeaponTestSession session = Object.FindObjectOfType<WeaponTestSession>();
        if (session == null)
        {
            Debug.LogError("[WeaponVisualTuningRuntimeSetup] WeaponTestSession 이 없습니다. Setup Weapon Test Scene 을 실행하세요.");
            return;
        }

        if (session.GetComponent<WeaponVisualTuningRuntime>() != null)
        {
            Debug.Log("[WeaponVisualTuningRuntimeSetup] 이미 Weapon_test 에 튜닝 UI 가 있습니다.");
            Selection.activeGameObject = session.gameObject;
            return;
        }

        Undo.AddComponent<WeaponVisualTuningRuntime>(session.gameObject);
        Selection.activeGameObject = session.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[WeaponVisualTuningRuntimeSetup] Weapon_test 에 F8 튜닝 패널을 추가했습니다.");
    }

    [MenuItem("Simplegame2/Add Weapon VFX Tuning UI To Weapon Test Scene", true)]
    static bool ValidateAddToWeaponTestScene()
    {
        return !Application.isPlaying;
    }
}
#endif
