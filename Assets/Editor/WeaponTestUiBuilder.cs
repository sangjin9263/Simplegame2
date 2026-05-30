#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Weapon_test 씬에 Unity UI Dropdown(무기 선택)을 배치합니다.
public static class WeaponTestUiBuilder
{
    public static void EnsureWeaponDropdownUi(WeaponTestSession session)
    {
        if (session == null)
        {
            return;
        }

        WeaponTestWeaponDropdown weaponDropdown = session.GetComponent<WeaponTestWeaponDropdown>();
        if (weaponDropdown == null)
        {
            weaponDropdown = session.gameObject.AddComponent<WeaponTestWeaponDropdown>();
        }

        SerializedObject serializedDropdown = new SerializedObject(weaponDropdown);
        serializedDropdown.FindProperty("session").objectReferenceValue = session;

        Dropdown dropdown = serializedDropdown.FindProperty("dropdown").objectReferenceValue as Dropdown;
        if (dropdown == null)
        {
            Canvas canvas = WeaponTestUiFactory.FindOrCreateCanvas();
            dropdown = WeaponTestUiFactory.CreateWeaponDropdown(
                canvas.transform,
                new Vector2(-408f, -20f),
                new Vector2(280f, 32f));
            serializedDropdown.FindProperty("dropdown").objectReferenceValue = dropdown;
        }

        serializedDropdown.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(session.gameObject);
    }
}
#endif
