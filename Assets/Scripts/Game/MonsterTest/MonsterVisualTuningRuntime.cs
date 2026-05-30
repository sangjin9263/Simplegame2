using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(MonsterTestSession))]
public class MonsterVisualTuningRuntime : MonoBehaviour
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

    MonsterTestSession session;
    bool panelVisible;
    Vector2 scroll;

    bool showCsvData = true;
    bool showTiming = true;
    bool showMovement = true;
    bool showProjectile = true;

    bool monsterDropdownExpanded;
    List<MonsterDefinitionRow> monsterRows;
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
    GUIStyle panelTextFieldStyle;
    GUIStyle panelLabelButtonStyle;
    GUIStyle tooltipBoxStyle;
    GUIStyle tooltipLabelStyle;
    bool stylesReady;

    readonly Dictionary<string, string> numericEditBuffers = new Dictionary<string, string>();

    static readonly Dictionary<string, string> FieldTooltips = BuildFieldTooltips();

    void Awake()
    {
        session = GetComponent<MonsterTestSession>();
        panelVisible = showPanelOnStart;
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
        DrawMonsterBar();
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
        DrawHoverTooltip();
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

        panelTextFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleRight,
            padding = new RectOffset(4, 4, 2, 2),
            normal = { background = MakeTintTexture(PanelBgRaised), textColor = PanelText },
            focused = { background = MakeTintTexture(PanelBgSelected), textColor = PanelText }
        };

        panelLabelButtonStyle = new GUIStyle(panelLabelStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(0, 0, 2, 2),
            border = new RectOffset(0, 0, 0, 0),
            normal = { background = null, textColor = PanelText },
            hover = { background = MakeTintTexture(new Color(0.17f, 0.20f, 0.25f, 0.45f)), textColor = PanelText },
            active = { background = MakeTintTexture(new Color(0.20f, 0.26f, 0.34f, 0.55f)), textColor = PanelText },
            focused = { background = MakeTintTexture(new Color(0.17f, 0.20f, 0.25f, 0.45f)), textColor = PanelText }
        };

        tooltipBoxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTintTexture(new Color(0.05f, 0.06f, 0.09f, 0.96f)) },
            border = new RectOffset(6, 6, 6, 6),
            padding = new RectOffset(10, 10, 8, 8)
        };

        tooltipLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = false,
            normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
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
        GUILayout.Label("Monster_test — Tuning", headerStyle);
        GUILayout.Label("F8=패널  |  T=마우스  |  휠=스크롤  |  1/2/3=타입", panelLabelStyle);
        GUILayout.Label("저장=JSON+CSV  |  CSV=공격·Stop Distance 포함  |  우선순위: JSON > CSV", panelLabelStyle);
        liveApply = GUILayout.Toggle(liveApply, "슬라이더 변경 즉시 적용", panelToggleStyle);
    }

    void DrawMonsterBar()
    {
        EnsureMonsterRows();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CSV", panelButtonStyle, GUILayout.Width(60f), GUILayout.Height(28f)))
        {
            InvalidateMonsterRows();
            session.ReloadFromCsv();
        }

        Rect captionRect = GUILayoutUtility.GetRect(0f, 28f, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();

        DrawMonsterDropdown(captionRect);

        if (monsterDropdownExpanded && monsterRows.Count > 0)
        {
            GUILayout.Space(monsterRows.Count * DropdownRowHeight + 6f);
        }
    }

    void EnsureMonsterRows()
    {
        if (monsterRows != null)
        {
            return;
        }

        monsterRows = new List<MonsterDefinitionRow>(MonsterDefinitionTable.LoadForEditing().Rows);
    }

    void InvalidateMonsterRows()
    {
        monsterRows = null;
    }

    void DrawMonsterDropdown(Rect captionRect)
    {
        if (monsterRows == null || monsterRows.Count == 0)
        {
            GUI.Label(captionRect, "monster_default.csv 없음", dropdownCaptionStyle);
            return;
        }

        int currentIndex = FindCurrentMonsterIndex();
        string caption = GetMonsterLabel(monsterRows[currentIndex]);

        if (GUI.Button(captionRect, caption + "  ▼", dropdownCaptionStyle))
        {
            monsterDropdownExpanded = !monsterDropdownExpanded;
        }

        if (!monsterDropdownExpanded)
        {
            return;
        }

        Rect listRect = new Rect(
            captionRect.x,
            captionRect.yMax + 2f,
            captionRect.width,
            DropdownRowHeight * monsterRows.Count + 4f);

        GUI.Box(listRect, GUIContent.none, dropdownListStyle);

        for (int i = 0; i < monsterRows.Count; i++)
        {
            Rect itemRect = new Rect(
                listRect.x + 3f,
                listRect.y + 2f + i * DropdownRowHeight,
                listRect.width - 6f,
                DropdownRowHeight - 2f);

            GUIStyle itemStyle = i == currentIndex ? dropdownItemSelectedStyle : dropdownItemStyle;
            if (GUI.Button(itemRect, GetMonsterLabel(monsterRows[i]), itemStyle))
            {
                monsterDropdownExpanded = false;
                int monId = monsterRows[i].monId;
                if (monId != session.ActiveMonId)
                {
                    session.EquipMonsterById(monId);
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
            monsterDropdownExpanded = false;
        }
    }

    int FindCurrentMonsterIndex()
    {
        int activeMonId = session.ActiveMonId;
        for (int i = 0; i < monsterRows.Count; i++)
        {
            if (monsterRows[i].monId == activeMonId)
            {
                return i;
            }
        }

        return 0;
    }

    static string GetMonsterLabel(MonsterDefinitionRow row)
    {
        return string.IsNullOrEmpty(row.monName) ? row.monId.ToString() : row.monName;
    }

    void DrawActionBar()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("저장", "현재 튜닝을 JSON + monster_default.csv에 저장합니다."), panelButtonStyle))
        {
            session.ApplyActiveSnapshot();
            session.SaveCurrentTuning();
        }

        if (GUILayout.Button(new GUIContent("되돌리기", "마지막으로 「저장」한 값으로 되돌립니다. (CSV/코드 기본값 아님)"), panelButtonStyle))
        {
            session.RevertToLastSaved();
        }

        if (GUILayout.Button(
                new GUIContent("CSV·SampleScene 적용", "CSV 그리고 SampleScene에 적용 — JSON/CSV 저장 후 몬스터 프리팹 + Resources CSV 반영"),
                panelButtonStyle))
        {
            session.ApplyToCsvAndSampleScene();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("스폰 리셋", "몬스터를 플레이어로부터 8m 스폰 지점으로 다시 배치합니다. Stop Distance 확인용."), panelButtonStyle))
        {
            session.ResetMonsterSpawn();
        }

        GUILayout.EndHorizontal();
        if (GUILayout.Button(
                new GUIContent(
                    "스폰 리셋 → 플레이어로부터 8m 지점으로 재배치 (Stop Distance 확인용)",
                    "스폰 리셋 버튼: 몬스터를 8m 지점으로 되돌려 Stop Distance·추격을 다시 테스트합니다."),
                panelLabelButtonStyle))
        {
        }
    }

    void DrawSnapshotFields(MonsterVisualTuningSnapshot snapshot)
    {
        if (snapshot == null)
        {
            GUILayout.Label("스냅샷을 찾을 수 없습니다.", panelLabelStyle);
            return;
        }

        bool changed = false;

        showCsvData = GUILayout.Toggle(showCsvData, "CSV 데이터 (monster_default)", panelToggleStyle);
        if (showCsvData)
        {
            changed |= DrawIntSlider("DMG", ref snapshot.damage, 0, 999);
            changed |= DrawIntSlider("Mon LV", ref snapshot.level, 1, 99);
            changed |= DrawIntSlider("HP", ref snapshot.hp, 1, 9999);
            changed |= DrawIntSlider("MP", ref snapshot.mp, 0, 999);
            changed |= DrawIntSlider("MP Regen", ref snapshot.mpRegen, 0, 99);
            changed |= DrawFloatSlider("Move Speed", ref snapshot.moveSpeed, 0f, 12f);
            changed |= DrawIntSlider("Give EXP", ref snapshot.giveExp, 0, 9999);
            changed |= DrawTextField("Prefab", ref snapshot.prefabName);
            changed |= DrawTextField("Projectile", ref snapshot.projectilePrefab);
            changed |= DrawTextField("Hit Impact", ref snapshot.hitImpact);
        }

        showTiming = GUILayout.Toggle(showTiming, "공격 타이밍", panelToggleStyle);
        if (showTiming)
        {
            changed |= DrawFloatSlider("Attack Range", ref snapshot.attackRange, 0.5f, 20f);
            changed |= DrawFloatSlider("Cooldown", ref snapshot.attackCooldown, 0.05f, 5f);
            changed |= DrawFloatSlider("Anim Duration", ref snapshot.attackAnimDuration, 0.05f, 2f);
            if (snapshot.kind == MonsterKind.Melee)
            {
                changed |= DrawFloatSlider("Damage Apply N", ref snapshot.damageApplyNormalizedTime, 0.05f, 1f);
            }
            else
            {
                changed |= DrawFloatSlider("Fire Delay N", ref snapshot.fireDelayNormalizedTime, 0.05f, 1f);
            }
        }

        showMovement = GUILayout.Toggle(showMovement, "이동", panelToggleStyle);
        if (showMovement)
        {
            changed |= DrawFloatSlider("Stop Distance", ref snapshot.stopDistance, 0.1f, 20f);
        }

        if (snapshot.kind == MonsterKind.Ranged || snapshot.kind == MonsterKind.Mage)
        {
            showProjectile = GUILayout.Toggle(showProjectile, "투사체", panelToggleStyle);
            if (showProjectile)
            {
                changed |= DrawFloatSlider("Spawn Forward", ref snapshot.projectileSpawnForwardOffset, -1f, 2f);
                changed |= DrawFloatSlider("Spawn Height", ref snapshot.projectileSpawnHeightOffset, -1f, 2f);
                changed |= DrawFloatSlider("Aim Height", ref snapshot.targetAimHeightOffset, -1f, 2f);

                if (snapshot.kind == MonsterKind.Mage)
                {
                    changed |= DrawFloatSlider("Energy Speed", ref snapshot.energyProjectileSpeed, 1f, 40f);
                    changed |= DrawFloatSlider("Energy Hit R", ref snapshot.energyProjectileHitRadius, 0.05f, 3f);
                    changed |= DrawFloatSlider("Energy Lifetime", ref snapshot.energyProjectileMaxLifetime, 0.1f, 10f);
                    changed |= DrawFloatSlider("Energy Scale Mul", ref snapshot.energyVisualScaleMultiplier, 0.1f, 2f);
                }
                else
                {
                    changed |= DrawFloatSlider("Arrow Speed", ref snapshot.arrowProjectileSpeed, 1f, 60f);
                    changed |= DrawFloatSlider("Arrow Max Range", ref snapshot.arrowProjectileMaxRange, 1f, 40f);
                    changed |= DrawFloatSlider("Arrow Hit R", ref snapshot.arrowProjectileHitRadius, 0.05f, 3f);
                    changed |= DrawFloatSlider("Arrow Scale", ref snapshot.arrowVisualScale, 0.01f, 2f);
                    changed |= DrawVector3Sliders("Arrow Rot Offset", ref snapshot.arrowVisualRotationOffset, -180f, 180f);
                    changed |= DrawFloatSlider("Arrow Scale Mul", ref snapshot.arrowVisualScaleMultiplier, 0.1f, 2f);
                    changed |= DrawFloatSlider("Arc Min", ref snapshot.arrowArcHeightMin, 0f, 2f);
                    changed |= DrawFloatSlider("Arc Max", ref snapshot.arrowArcHeightMax, 0f, 3f);
                }
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
        string controlName = "monster_float_" + label;
        if (!numericEditBuffers.TryGetValue(controlName, out string buffer))
        {
            buffer = FormatFloat(value);
            numericEditBuffers[controlName] = buffer;
        }

        bool focused = GUI.GetNameOfFocusedControl() == controlName;

        GUILayout.BeginHorizontal();
        DrawFieldLabel(label);
        float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(120f), GUILayout.ExpandWidth(true));

        GUI.SetNextControlName(controlName);
        string newBuffer = GUILayout.TextField(buffer, panelTextFieldStyle, GUILayout.Width(52f));
        GUILayout.EndHorizontal();

        if (!Mathf.Approximately(sliderValue, value))
        {
            value = Mathf.Clamp(sliderValue, min, max);
            numericEditBuffers[controlName] = FormatFloat(value);
        }
        else if (newBuffer != buffer)
        {
            numericEditBuffers[controlName] = newBuffer;
            if (TryParseFloat(newBuffer, out float parsed))
            {
                value = Mathf.Clamp(parsed, min, max);
            }
        }
        else if (!focused)
        {
            numericEditBuffers[controlName] = FormatFloat(value);
        }

        return !Mathf.Approximately(before, value);
    }

    bool DrawIntSlider(string label, ref int value, int min, int max)
    {
        int before = value;
        string controlName = "monster_int_" + label;
        if (!numericEditBuffers.TryGetValue(controlName, out string buffer))
        {
            buffer = FormatInt(value);
            numericEditBuffers[controlName] = buffer;
        }

        bool focused = GUI.GetNameOfFocusedControl() == controlName;

        GUILayout.BeginHorizontal();
        DrawFieldLabel(label);
        int sliderValue = Mathf.RoundToInt(
            GUILayout.HorizontalSlider(value, min, max, GUILayout.MinWidth(120f), GUILayout.ExpandWidth(true)));

        GUI.SetNextControlName(controlName);
        string newBuffer = GUILayout.TextField(buffer, panelTextFieldStyle, GUILayout.Width(52f));
        GUILayout.EndHorizontal();

        if (sliderValue != value)
        {
            value = Mathf.Clamp(sliderValue, min, max);
            numericEditBuffers[controlName] = FormatInt(value);
        }
        else if (newBuffer != buffer)
        {
            numericEditBuffers[controlName] = newBuffer;
            if (TryParseInt(newBuffer, out int parsed))
            {
                value = Mathf.Clamp(parsed, min, max);
            }
        }
        else if (!focused)
        {
            numericEditBuffers[controlName] = FormatInt(value);
        }

        return value != before;
    }

    static string FormatFloat(float value)
    {
        return value.ToString("F3", CultureInfo.InvariantCulture);
    }

    static string FormatInt(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    static bool TryParseFloat(string text, out float value)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    static bool TryParseInt(string text, out int value)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    }

    bool DrawTextField(string label, ref string value)
    {
        string before = value ?? string.Empty;
        GUILayout.BeginHorizontal();
        DrawFieldLabel(label);
        value = GUILayout.TextField(before, GUILayout.MinWidth(180f), GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        return (value ?? string.Empty) != before;
    }

    bool DrawVector3Sliders(string label, ref Vector3 value, float min, float max)
    {
        DrawFieldLabel(label);
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

    void DrawFieldLabel(string label)
    {
        FieldTooltips.TryGetValue(label, out string tooltip);
        GUIContent content = new GUIContent(label, tooltip ?? string.Empty);
        GUILayout.Button(content, panelLabelButtonStyle, GUILayout.Width(130f));
    }

    void DrawHoverTooltip()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        string text = GUI.tooltip;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        const float maxWidth = 300f;
        float textHeight = tooltipLabelStyle.CalcHeight(new GUIContent(text), maxWidth);
        float width = Mathf.Min(maxWidth, tooltipLabelStyle.CalcSize(new GUIContent(text)).x + 8f);
        width = Mathf.Max(width, 180f);

        Vector2 mouse = Event.current.mousePosition;
        float x = mouse.x - width - 14f;
        float y = mouse.y - textHeight * 0.5f;

        if (x < 8f)
        {
            x = mouse.x + 14f;
        }

        y = Mathf.Clamp(y, 8f, Screen.height - textHeight - 8f);

        int previousDepth = GUI.depth;
        GUI.depth = -1000;

        Rect tooltipRect = new Rect(x, y, width, textHeight + 4f);
        GUI.Box(tooltipRect, GUIContent.none, tooltipBoxStyle);
        GUI.Label(tooltipRect, text, tooltipLabelStyle);

        GUI.depth = previousDepth;
    }

    static Dictionary<string, string> BuildFieldTooltips()
    {
        return new Dictionary<string, string>
        {
            { "DMG", "몬스터 공격력. 근접/원거리/마법 모두 최종 피해량에 반영됩니다." },
            { "Mon LV", "몬스터 레벨. CSV 저장용 기본 정보입니다." },
            { "HP", "몬스터 최대 체력입니다." },
            { "MP", "마법 몬스터 MP. 현재는 저장만 되고 스킬 소모 로직은 미연동입니다." },
            { "MP Regen", "MP 자동 회복량. 현재는 저장만 됩니다." },
            { "Move Speed", "플레이어를 추격할 때 초당 이동 거리입니다." },
            { "Give EXP", "처치 시 플레이어에게 주는 경험치입니다." },
            { "Prefab", "스폰에 쓰는 몬스터 프리팹 이름 (예: SPUM_orc_m7)." },
            { "Projectile", "원거리/마법 투사체 프리팹 이름 (예: Arrow01)." },
            { "Hit Impact", "명중 시 재생할 임팩트 VFX 이름입니다." },
            { "Attack Range", "이 거리 안에 플레이어가 있으면 공격을 시작합니다. 원거리는 보통 13 전후." },
            { "Cooldown", "공격 사이 최소 대기 시간(초)입니다." },
            { "Anim Duration", "공격 모션 길이(초). 애니가 없으면 이 값을 사용합니다." },
            { "Damage Apply N", "근접: 공격 애니 길이 대비 몇 % 지점에서 피해를 적용할지 (0~1)." },
            { "Fire Delay N", "원거리/마법: 공격 애니 길이 대비 몇 % 지점에서 투사체를 발사할지 (0~1)." },
            { "Stop Distance", "플레이어 몸통 중심과 몬스터 중심 사이 최소 간격. 이보다 가까워지면 추격을 멈춥니다." },
            { "Spawn Forward", "몬스터 몸에서 앞쪽으로 얼마나 떨어진 곳에서 발사할지. 크면 손/활 앞에서 나갑니다." },
            { "Spawn Height", "지면 기준 위로 올린 발사 높이. 가슴~머리 높이를 맞출 때 조절합니다." },
            { "Aim Height", "플레이어 조준점 높이 보정. 맞는 위치가 너무 발/머리면 조절합니다." },
            { "Arrow Speed", "화살 날아가는 속도. 빠를수록 멀리까지 잘 갑니다." },
            { "Arrow Max Range", "화살 최대 사거리. 속도와 함께 수명(사라지는 시점)을 결정합니다." },
            { "Arrow Hit R", "명중 판정 반경. 클수록 살짝 빗나가도 맞습니다." },
            { "Arrow Scale", "화살 프리팹 기본 크기입니다." },
            { "Arrow Rot Offset", "화살 모델 회전 보정. 날아가는 방향과 모델이 어긋날 때 X/Y/Z로 맞춥니다." },
            { "  X", "Arrow Rot Offset의 X축 회전(도)." },
            { "  Y", "Arrow Rot Offset의 Y축 회전(도)." },
            { "  Z", "Arrow Rot Offset의 Z축 회전(도)." },
            { "Arrow Scale Mul", "Arrow Scale에 곱하는 추가 배율. 최종 크기 = Scale × Mul." },
            { "Arc Min", "활 포물선 최소 높이. 가까운 거리에서도 이만큼은 뜹니다." },
            { "Arc Max", "활 포물선 최대 높이. 멀리 쏠수록 위로 더 뜹니다." },
            { "Energy Speed", "에너지볼 이동 속도입니다." },
            { "Energy Hit R", "에너지볼 명중 판정 반경입니다." },
            { "Energy Lifetime", "에너지볼이 사라지기까지 시간(초)입니다." },
            { "Energy Scale Mul", "에너지볼 크기 배율입니다." }
        };
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnMonsterTestScene()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        MonsterTestSession session = Object.FindObjectOfType<MonsterTestSession>();
        if (session == null)
        {
            return;
        }

        if (session.GetComponent<MonsterVisualTuningRuntime>() == null)
        {
            session.gameObject.AddComponent<MonsterVisualTuningRuntime>();
        }
    }
#endif
}
