using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// weapon_default.csv Weapon_Name 목록으로 Unity UI Dropdown 을 채웁니다.
[DisallowMultipleComponent]
public class WeaponTestWeaponDropdown : MonoBehaviour
{
    [SerializeField] WeaponTestSession session;
    [SerializeField] Dropdown dropdown;

    readonly List<int> weaponIds = new List<int>();
    bool suppressEquipCallback;
    int lastSyncedWeaponId = -1;
    Canvas rootCanvas;

    void Awake()
    {
        if (session == null)
        {
            session = GetComponent<WeaponTestSession>();
        }

        EnsureDropdownUi();
    }

    void OnEnable()
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        RefreshOptions();
        SyncSelectionToSession(true);
    }

    void OnDisable()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
    }

    void Update()
    {
        if (session == null || dropdown == null)
        {
            return;
        }

        int activeWeaponId = session.ActiveWeaponId;
        if (activeWeaponId != lastSyncedWeaponId)
        {
            SyncSelectionToSession(false);
        }
    }

    public void SetOverlayVisible(bool visible)
    {
        EnsureDropdownUi();
        if (rootCanvas != null)
        {
            rootCanvas.enabled = visible;
        }
    }

    public void PlaceAtScreenRect(Rect screenRect)
    {
        EnsureDropdownUi();
        if (dropdown == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        RectTransform dropdownRect = dropdown.transform as RectTransform;

        // IMGUI 좌표(위=0) → 스크린 좌표(아래=0)
        Vector2 screenTopLeft = new Vector2(screenRect.xMin, Screen.height - screenRect.yMin);
        Vector2 screenBottomRight = new Vector2(screenRect.xMax, Screen.height - screenRect.yMax);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTopLeft, null, out Vector2 localTopLeft)
            && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenBottomRight, null, out Vector2 localBottomRight))
        {
            dropdownRect.anchorMin = new Vector2(0f, 1f);
            dropdownRect.anchorMax = new Vector2(0f, 1f);
            dropdownRect.pivot = new Vector2(0f, 1f);
            dropdownRect.anchoredPosition = localTopLeft;
            dropdownRect.sizeDelta = new Vector2(
                Mathf.Max(120f, localBottomRight.x - localTopLeft.x),
                Mathf.Max(28f, localTopLeft.y - localBottomRight.y));
        }
    }

    public void RefreshOptions()
    {
        EnsureDropdownUi();
        if (dropdown == null)
        {
            return;
        }

        weaponIds.Clear();
        var options = new List<Dropdown.OptionData>();

        IReadOnlyList<WeaponDefinitionRow> rows = WeaponDefinitionTable.LoadForEditing().Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            WeaponDefinitionRow row = rows[i];
            weaponIds.Add(row.weaponId);
            string label = string.IsNullOrEmpty(row.weaponName) ? row.weaponId.ToString() : row.weaponName;
            options.Add(new Dropdown.OptionData(label));
        }

        suppressEquipCallback = true;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        suppressEquipCallback = false;

        SyncSelectionToSession(true);
    }

    public void SyncSelectionToSession(bool forceRefreshLabel)
    {
        if (session == null || dropdown == null || weaponIds.Count == 0)
        {
            return;
        }

        int activeWeaponId = session.ActiveWeaponId;
        int index = weaponIds.IndexOf(activeWeaponId);
        if (index < 0)
        {
            index = 0;
        }

        if (!forceRefreshLabel && dropdown.value == index && lastSyncedWeaponId == activeWeaponId)
        {
            return;
        }

        suppressEquipCallback = true;
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
        suppressEquipCallback = false;

        lastSyncedWeaponId = activeWeaponId;
    }

    void OnDropdownValueChanged(int index)
    {
        if (suppressEquipCallback || session == null || index < 0 || index >= weaponIds.Count)
        {
            return;
        }

        int weaponId = weaponIds[index];
        if (weaponId == session.ActiveWeaponId)
        {
            return;
        }

        session.EquipWeaponById(weaponId);
        lastSyncedWeaponId = weaponId;
    }

    void EnsureDropdownUi()
    {
        if (dropdown != null)
        {
            return;
        }

        rootCanvas = WeaponTestUiFactory.FindOrCreateCanvas();
        dropdown = WeaponTestUiFactory.CreateWeaponDropdown(
            rootCanvas.transform,
            Vector2.zero,
            new Vector2(280f, 32f));
        dropdown.transform.SetAsLastSibling();
    }
}
