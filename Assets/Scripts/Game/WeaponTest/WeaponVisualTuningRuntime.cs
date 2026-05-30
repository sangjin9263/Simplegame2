using System.Collections.Generic;
using UnityEngine;

// Weapon_test 씬 Play 중 VFX 비주얼을 조절하는 런타임 패널 (F8 토글).
[RequireComponent(typeof(WeaponTestSession))]
public class WeaponVisualTuningRuntime : MonoBehaviour
{
    const float PanelWidth = 400f;
    const float DropdownRowHeight = 26f;

    static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.12f, 1f);
    static readonly Color PanelBgRaised = new Color(0.13f, 0.15f, 0.19f, 0.98f);
    static readonly Color PanelBgHover = new Color(0.17f, 0.20f, 0.25f, 0.98f);
    static readonly Color PanelBgSelected = new Color(0.20f, 0.26f, 0.34f, 0.98f);
    static readonly Color PanelText = new Color(0.88f, 0.91f, 0.95f, 1f);

    [Header("Panel")]
    [SerializeField] bool showPanelOnStart = true;
    [SerializeField] KeyCode toggleKey = KeyCode.F8;

    [Header("Apply")]
    [SerializeField] bool liveApply = true;

    internal WeaponTestSession TuningSession => session;

    WeaponTestSession session;
    bool panelVisible;
    Vector2 scroll;

    bool showCsvData = true;
    bool showTiming = true;
    bool showSpawnPoint = true;
    bool showVisual = true;
    bool showProjectileStats = true;

    bool weaponDropdownExpanded;
    List<WeaponDefinitionRow> weaponRows;
    Rect panelScreenRect;

    GUIStyle headerStyle;
    GUIStyle boxStyle;
    GUIStyle panelLabelStyle;
    GUIStyle panelButtonStyle;
    GUIStyle panelToggleStyle;
    GUIStyle dropdownCaptionStyle;
    GUIStyle dropdownListStyle;
    GUIStyle dropdownItemStyle;
    GUIStyle dropdownItemSelectedStyle;
    bool stylesReady;

    void Awake()
    {
        session = GetComponent<WeaponTestSession>();
        panelVisible = showPanelOnStart;
        DisableLegacyUiDropdown();
    }

    static void DisableLegacyUiDropdown()
    {
        WeaponTestWeaponDropdown[] legacyDropdowns = FindObjectsByType<WeaponTestWeaponDropdown>(FindObjectsSortMode.None);
        for (int i = 0; i < legacyDropdowns.Length; i++)
        {
            legacyDropdowns[i].SetOverlayVisible(false);
        }

        GameObject canvasObject = GameObject.Find(WeaponTestUiFactory.CanvasName);
        if (canvasObject != null)
        {
            canvasObject.SetActive(false);
        }
    }

    void Update()
    {
        if (session == null)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            panelVisible = !panelVisible;
            if (panelVisible)
            {
                scroll = Vector2.zero;
            }
        }
    }

    void OnGUI()
    {
        if (session == null || !panelVisible)
        {
            return;
        }

        EnsureGuiStyles();

        float top = 16f;
        float height = Screen.height - top - 16f;
        Rect area = new Rect(Screen.width - PanelWidth - 12f, top, PanelWidth, height);
        panelScreenRect = area;

        GUILayout.BeginArea(area);
        GUI.Box(new Rect(0f, 0f, PanelWidth, height), GUIContent.none, boxStyle);

        const float pad = 10f;
        float innerWidth = PanelWidth - pad * 2f;
        float scrollHeight = height - pad * 2f;

        GUILayout.BeginHorizontal();
        GUILayout.Space(pad);
        GUILayout.BeginVertical(GUILayout.Width(innerWidth));

        scroll = GUILayout.BeginScrollView(
            scroll,
            false,
            true,
            GUILayout.Width(innerWidth),
            GUILayout.Height(scrollHeight));

        DrawHeader();
        GUILayout.Space(6f);
        DrawWeaponBar();
        GUILayout.Space(4f);
        DrawActionBar();
        GUILayout.Space(8f);
        DrawSnapshotFields(session.ActiveSnapshot);
        GUILayout.Space(32f);
        GUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.Space(pad);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        HandlePanelScrollWheel();
    }

    void HandlePanelScrollWheel()
    {
        if (Event.current.type != EventType.ScrollWheel)
        {
            return;
        }

        if (!panelScreenRect.Contains(Event.current.mousePosition))
        {
            return;
        }

        scroll.y += Event.current.delta.y * 32f;
        scroll.y = Mathf.Max(0f, scroll.y);
        Event.current.Use();
    }

    void EnsureGuiStyles()
    {
        if (stylesReady)
        {
            return;
        }

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
            normal = { textColor = PanelText }
        };

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(0, 0, 0, 0),
            normal = { background = MakeTintTexture(PanelBg) }
        };

        panelLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = PanelText }
        };

        panelButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            normal = { background = MakeTintTexture(PanelBgRaised), textColor = PanelText },
            hover = { background = MakeTintTexture(PanelBgHover), textColor = PanelText },
            active = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText }
        };

        panelToggleStyle = new GUIStyle(panelButtonStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 8, 4, 4)
        };

        dropdownCaptionStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            padding = new RectOffset(10, 28, 5, 5),
            normal = { background = MakeTintTexture(PanelBgRaised), textColor = PanelText },
            hover = { background = MakeTintTexture(PanelBgHover), textColor = PanelText },
            active = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText },
            focused = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText }
        };

        dropdownListStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTintTexture(PanelBg) },
            border = new RectOffset(4, 4, 4, 4)
        };

        dropdownItemStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            padding = new RectOffset(12, 8, 4, 4),
            normal = { background = MakeTintTexture(PanelBg), textColor = PanelText },
            hover = { background = MakeTintTexture(PanelBgHover), textColor = PanelText },
            active = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText }
        };

        dropdownItemSelectedStyle = new GUIStyle(dropdownItemStyle)
        {
            normal = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText },
            hover = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText }
        };

        stylesReady = true;
    }

    static Texture2D MakeTintTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void DrawHeader()
    {
        GUILayout.Label("Weapon_test — VFX Tuning", headerStyle);
        GUILayout.Label("F8=패널  |  T=마우스  |  휠=스크롤  |  1/2/3=무기", panelLabelStyle);
        GUILayout.Label("저장=JSON+CSV  |  우선순위: JSON > CSV > 기본값", panelLabelStyle);
        liveApply = GUILayout.Toggle(liveApply, "슬라이더 변경 즉시 적용", panelToggleStyle);
    }

    void DrawWeaponBar()
    {
        EnsureWeaponRows();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CSV", panelButtonStyle, GUILayout.Width(60f), GUILayout.Height(28f)))
        {
            InvalidateWeaponRows();
            session.ReloadFromCsv();
        }

        Rect captionRect = GUILayoutUtility.GetRect(0f, 28f, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        DrawWeaponDropdown(captionRect);

        if (weaponDropdownExpanded && weaponRows.Count > 0)
        {
            GUILayout.Space(weaponRows.Count * DropdownRowHeight + 6f);
        }
    }

    void EnsureWeaponRows()
    {
        if (weaponRows != null)
        {
            return;
        }

        weaponRows = new List<WeaponDefinitionRow>(WeaponDefinitionTable.LoadForEditing().Rows);
    }

    void InvalidateWeaponRows()
    {
        weaponRows = null;
    }

    void DrawWeaponDropdown(Rect captionRect)
    {
        if (weaponRows == null || weaponRows.Count == 0)
        {
            GUI.Label(captionRect, "weapon_default.csv 없음", dropdownCaptionStyle);
            return;
        }

        int currentIndex = FindCurrentWeaponIndex();
        string caption = GetWeaponLabel(weaponRows[currentIndex]);

        if (GUI.Button(captionRect, caption + "  ▼", dropdownCaptionStyle))
        {
            weaponDropdownExpanded = !weaponDropdownExpanded;
        }

        if (!weaponDropdownExpanded)
        {
            return;
        }

        Rect listRect = new Rect(
            captionRect.x,
            captionRect.yMax + 2f,
            captionRect.width,
            DropdownRowHeight * weaponRows.Count + 4f);

        GUI.Box(listRect, GUIContent.none, dropdownListStyle);

        for (int i = 0; i < weaponRows.Count; i++)
        {
            Rect itemRect = new Rect(
                listRect.x + 3f,
                listRect.y + 2f + i * DropdownRowHeight,
                listRect.width - 6f,
                DropdownRowHeight - 2f);

            GUIStyle itemStyle = i == currentIndex ? dropdownItemSelectedStyle : dropdownItemStyle;
            if (GUI.Button(itemRect, GetWeaponLabel(weaponRows[i]), itemStyle))
            {
                weaponDropdownExpanded = false;
                int weaponId = weaponRows[i].weaponId;
                if (weaponId != session.ActiveWeaponId)
                {
                    session.EquipWeaponById(weaponId);
                }
            }
        }

        CloseDropdownOnOutsideClick(captionRect, listRect);
    }

    void CloseDropdownOnOutsideClick(Rect captionRect, Rect listRect)
    {
        if (Event.current.type != EventType.MouseDown)
        {
            return;
        }

        Rect panelArea = new Rect(Screen.width - PanelWidth - 12f, 16f, PanelWidth, Screen.height - 32f);
        Vector2 mouseInPanel = Event.current.mousePosition - panelArea.position;
        Rect combined = Rect.MinMaxRect(
            Mathf.Min(captionRect.x, listRect.x),
            Mathf.Min(captionRect.y, listRect.y),
            Mathf.Max(captionRect.xMax, listRect.xMax),
            Mathf.Max(captionRect.yMax, listRect.yMax));

        if (!combined.Contains(mouseInPanel))
        {
            weaponDropdownExpanded = false;
        }
    }

    int FindCurrentWeaponIndex()
    {
        int activeWeaponId = session.ActiveWeaponId;
        for (int i = 0; i < weaponRows.Count; i++)
        {
            if (weaponRows[i].weaponId == activeWeaponId)
            {
                return i;
            }
        }

        return 0;
    }

    static string GetWeaponLabel(WeaponDefinitionRow row)
    {
        return string.IsNullOrEmpty(row.weaponName) ? row.weaponId.ToString() : row.weaponName;
    }

    void DrawActionBar()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("저장", panelButtonStyle))
        {
            session.ApplyActiveSnapshot();
            session.SaveCurrentTuning();
        }

        if (GUILayout.Button("되돌리기", panelButtonStyle))
        {
            session.ResetActiveSnapshotToDefaults();
        }

        GUILayout.EndHorizontal();

        DrawSampleSceneApplyButton();
    }

    void DrawSnapshotFields(WeaponVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            GUILayout.Label("스냅샷을 찾을 수 없습니다.", panelLabelStyle);
            return;
        }

        bool changed = false;

        showCsvData = GUILayout.Toggle(showCsvData, "CSV 데이터 (weapon_default)", panelToggleStyle);
        if (showCsvData)
        {
            changed |= DrawIntSlider("DMG", ref snapshot.damage, 1, 999);
            changed |= DrawIntSlider("Weapon LV", ref snapshot.weaponLevel, 1, 99);
            changed |= DrawTextField("Weapon Name", ref snapshot.weaponName);
            changed |= DrawTextField("Weapon Image", ref snapshot.weaponImage);
            changed |= DrawTextField("Equ Position", ref snapshot.equPosition);
            changed |= DrawTextField("Vfx Prefab1", ref snapshot.vfxPrefab1);
            changed |= DrawTextField("Hit Impact", ref snapshot.weaponHitImpact);
        }

        showTiming = GUILayout.Toggle(showTiming, "공격 타이밍", panelToggleStyle);
        if (showTiming)
        {
            changed |= DrawFloatSlider("Cooldown", ref snapshot.attackCooldown, 0.05f, 3f);
            changed |= DrawFloatSlider("Active Delay", ref snapshot.attackActiveDelay, 0f, 1f);
            changed |= DrawFloatSlider("Anim Duration", ref snapshot.attackAnimDuration, 0.05f, 2f);
            GUILayout.Label("Cooldown=공격 간격 | 연타 한계는 Anim Duration", panelLabelStyle);
        }

        showSpawnPoint = GUILayout.Toggle(showSpawnPoint, "시작 위치 (Spawn Point)", panelToggleStyle);
        if (showSpawnPoint)
        {
            changed |= DrawFloatSlider("Forward Offset", ref snapshot.spawnForwardOffset, -2f, 3f);
            if (snapshot.kind == WeaponVisualKind.Melee)
            {
                changed |= DrawFloatSlider("Side Offset", ref snapshot.spawnSideOffset, -2f, 2f);
            }

            changed |= DrawFloatSlider("Height Offset", ref snapshot.spawnHeightOffset, -1f, 3f);
        }

        string visualLabel = snapshot.kind == WeaponVisualKind.Melee
            ? "비주얼 (Slash Scale = VFX + Hitbox)"
            : "비주얼 (크기·회전)";
        showVisual = GUILayout.Toggle(showVisual, visualLabel, panelToggleStyle);
        if (showVisual)
        {
            changed |= DrawFloatSlider("Visual Scale", ref snapshot.visualScale, 0.01f, 4f);
            changed |= DrawVector3Sliders("Rotation Offset", ref snapshot.visualRotationOffset, -180f, 180f);
        }

        showProjectileStats = GUILayout.Toggle(showProjectileStats, "투사체 / 판정", panelToggleStyle);
        if (showProjectileStats)
        {
            changed |= DrawFloatSlider("Speed", ref snapshot.moveSpeed, 1f, 60f);

            if (snapshot.kind != WeaponVisualKind.Melee)
            {
                changed |= DrawFloatSlider("Max Range", ref snapshot.maxRange, 1f, 40f);
                changed |= DrawFloatSlider("Hit Radius", ref snapshot.hitRadius, 0.05f, 3f);
                changed |= DrawFloatSlider("Vfx Lifetime", ref snapshot.maxLifetime, 0.05f, 20f);
            }
            else
            {
                changed |= DrawFloatSlider("Vfx Lifetime", ref snapshot.vfx1Lifetime, 0.05f, 5f);
            }

            if (snapshot.kind == WeaponVisualKind.Magic)
            {
                changed |= DrawFloatSlider("Turn Speed", ref snapshot.turnSpeed, 0f, 1080f);
                changed |= DrawFloatSlider("Explosion Radius", ref snapshot.explosionRadius, 0.1f, 8f);
                changed |= DrawIntSlider("Max Hit Targets", ref snapshot.maxHitTargets, 1, 20);
                changed |= DrawFloatSlider("Search Range", ref snapshot.targetSearchRange, 1f, 60f);
            }
        }

        if (changed && liveApply)
        {
            session.ApplyActiveSnapshot();
        }
    }

    bool DrawFloatSlider(string label, ref float value, float min, float max)
    {
        float before = value;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, panelLabelStyle, GUILayout.Width(130f));
        float newValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(160f), GUILayout.ExpandWidth(true));
        GUILayout.Label(newValue.ToString("F3"), panelLabelStyle, GUILayout.Width(52f));
        GUILayout.EndHorizontal();
        value = Mathf.Clamp(newValue, min, max);
        return !Mathf.Approximately(before, value);
    }

    bool DrawIntSlider(string label, ref int value, int min, int max)
    {
        int before = value;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, panelLabelStyle, GUILayout.Width(130f));
        int newValue = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(160f), GUILayout.ExpandWidth(true)));
        GUILayout.Label(newValue.ToString(), panelLabelStyle, GUILayout.Width(52f));
        GUILayout.EndHorizontal();
        value = Mathf.Clamp(newValue, min, max);
        return value != before;
    }

    bool DrawTextField(string label, ref string value)
    {
        string before = value ?? string.Empty;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, panelLabelStyle, GUILayout.Width(130f));
        value = GUILayout.TextField(before, GUILayout.MinWidth(180f), GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        return (value ?? string.Empty) != before;
    }

    bool DrawVector3Sliders(string label, ref Vector3 value, float min, float max)
    {
        GUILayout.Label(label, panelLabelStyle);
        bool changed = false;
        float x = value.x;
        float y = value.y;
        float z = value.z;
        changed |= DrawFloatSlider("  X", ref x, min, max);
        changed |= DrawFloatSlider("  Y", ref y, min, max);
        changed |= DrawFloatSlider("  Z", ref z, min, max);
        value = new Vector3(x, y, z);
        return changed;
    }

    void DrawSampleSceneApplyButton()
    {
#if UNITY_EDITOR
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("SampleScene 반영", panelButtonStyle))
        {
            TuningSession.ApplyActiveSnapshot();
            TuningSession.SaveCurrentTuning();
            WeaponVisualTuningSampleSceneApplier.ApplyAllSavedToSampleScene();
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("SampleScene 반영 → SPUM_main + weapon_default.csv", panelLabelStyle);
#endif
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnWeaponTestScene()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        WeaponTestSession session = Object.FindObjectOfType<WeaponTestSession>();
        if (session == null)
        {
            return;
        }

        if (session.GetComponent<WeaponVisualTuningRuntime>() == null)
        {
            session.gameObject.AddComponent<WeaponVisualTuningRuntime>();
        }
    }
#endif
}
