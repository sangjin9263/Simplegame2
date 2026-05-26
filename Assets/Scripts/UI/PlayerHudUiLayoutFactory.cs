using UnityEngine;
using UnityEngine.UI;

// PlayerHudRoot 프리팹 UI 생성 (에디터 메뉴 / 런타임 폴백).
public static class PlayerHudUiLayoutFactory
{
    public struct BuiltHud
    {
        public GameObject root;
        public PlayerStatusHudView statusView;
        public RadarMinimapView minimapView;
        public GameSessionHudView sessionView;
    }

    const float MinimapDiameter = 156f;
    const float TopLeftMargin = 20f;

    public static BuiltHud CreateHudRoot()
    {
        Sprite sprite = HudUiSprites.White;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject root = new GameObject(
            "PlayerHudRoot",
            typeof(RectTransform),
            typeof(PlayerStatusHudView),
            typeof(PlayerHudRoot),
            typeof(GameSessionHudView));

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchFull(canvasRect);

        RadarMinimapView minimapView = BuildRadarMinimap(canvasRect);
        GameSessionHudView sessionView = BuildSessionHud(canvasRect, font, sprite);
        PlayerStatusHudView statusView = BuildStatusPanel(canvasRect, font, sprite, root);
        BuildInventoryPlaceholders(canvasRect, font, sprite);

        return new BuiltHud
        {
            root = root,
            statusView = statusView,
            minimapView = minimapView,
            sessionView = sessionView
        };
    }

    public static RadarMinimapView BuildRadarMinimap(RectTransform canvasRect)
    {
        Sprite whiteSprite = HudUiSprites.White;
        Sprite circleSprite = HudUiSprites.Circle;

        GameObject panelObject = new GameObject("RadarMinimap", typeof(RectTransform), typeof(RadarMinimapView));
        panelObject.transform.SetParent(canvasRect, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(TopLeftMargin, -TopLeftMargin);
        panelRect.sizeDelta = new Vector2(MinimapDiameter, MinimapDiameter);

        Image ringImage = CreateImage("Ring", panelRect, circleSprite, new Color(0.55f, 0.58f, 0.62f, 0.95f));
        StretchFull(ringImage.rectTransform);

        Image backgroundImage = CreateImage("Background", panelRect, circleSprite, new Color(0.02f, 0.02f, 0.03f, 0.92f));
        RectTransform backgroundRect = backgroundImage.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(MinimapDiameter - 6f, MinimapDiameter - 6f);
        backgroundRect.anchoredPosition = Vector2.zero;

        GameObject blipsRootObject = new GameObject("Blips", typeof(RectTransform));
        blipsRootObject.transform.SetParent(panelRect, false);
        RectTransform blipsRoot = blipsRootObject.GetComponent<RectTransform>();
        StretchFull(blipsRoot);

        Image playerBlipImage = CreateImage("PlayerBlip", blipsRoot, whiteSprite, new Color(0.2f, 0.72f, 1f, 1f));
        RectTransform playerBlip = playerBlipImage.rectTransform;
        playerBlip.anchorMin = new Vector2(0.5f, 0.5f);
        playerBlip.anchorMax = new Vector2(0.5f, 0.5f);
        playerBlip.pivot = new Vector2(0.5f, 0.5f);
        playerBlip.sizeDelta = new Vector2(12f, 12f);
        playerBlip.anchoredPosition = Vector2.zero;

        RadarMinimapView view = panelObject.GetComponent<RadarMinimapView>();
        view.Configure(blipsRoot, playerBlip);
        return view;
    }

    public static GameSessionHudView BuildSessionHud(RectTransform canvasRect, Font font = null, Sprite sprite = null)
    {
        font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sprite ??= HudUiSprites.White;

        float minimapRight = TopLeftMargin + MinimapDiameter + 12f;

        GameObject sessionRoot = new GameObject("TopLeftSessionHud", typeof(RectTransform), typeof(GameSessionHudView));
        sessionRoot.transform.SetParent(canvasRect, false);
        RectTransform sessionRect = sessionRoot.GetComponent<RectTransform>();
        sessionRect.anchorMin = new Vector2(0f, 1f);
        sessionRect.anchorMax = new Vector2(0f, 1f);
        sessionRect.pivot = new Vector2(0f, 1f);
        sessionRect.anchoredPosition = new Vector2(minimapRight, -TopLeftMargin);
        sessionRect.sizeDelta = new Vector2(132f, MinimapDiameter);

        Image timerBackground = CreateImage("TimerBackground", sessionRect, sprite, new Color(0.08f, 0.1f, 0.14f, 0.72f));
        RectTransform timerBackgroundRect = timerBackground.rectTransform;
        timerBackgroundRect.anchorMin = new Vector2(0f, 1f);
        timerBackgroundRect.anchorMax = new Vector2(1f, 1f);
        timerBackgroundRect.pivot = new Vector2(0.5f, 1f);
        timerBackgroundRect.sizeDelta = new Vector2(0f, 44f);
        timerBackgroundRect.anchoredPosition = Vector2.zero;

        Text timerText = CreateText("TimerText", timerBackgroundRect, font, 24, TextAnchor.MiddleCenter);
        StretchFull(timerText.rectTransform);
        timerText.text = "30:00";
        timerText.fontStyle = FontStyle.Bold;

        Image killBackground = CreateImage("KillCountBackground", sessionRect, sprite, new Color(0.08f, 0.1f, 0.14f, 0.72f));
        RectTransform killBackgroundRect = killBackground.rectTransform;
        killBackgroundRect.anchorMin = new Vector2(0f, 1f);
        killBackgroundRect.anchorMax = new Vector2(1f, 1f);
        killBackgroundRect.pivot = new Vector2(0.5f, 1f);
        killBackgroundRect.sizeDelta = new Vector2(0f, 44f);
        killBackgroundRect.anchoredPosition = new Vector2(0f, -52f);

        Text killCaption = CreateText("KillCaption", killBackgroundRect, font, 14, TextAnchor.UpperCenter);
        RectTransform killCaptionRect = killCaption.rectTransform;
        killCaptionRect.anchorMin = new Vector2(0f, 0.45f);
        killCaptionRect.anchorMax = new Vector2(1f, 1f);
        killCaptionRect.offsetMin = Vector2.zero;
        killCaptionRect.offsetMax = Vector2.zero;
        killCaption.text = "카운트";
        killCaption.color = new Color(0.85f, 0.88f, 0.92f, 1f);

        Text killValue = CreateText("KillValue", killBackgroundRect, font, 22, TextAnchor.LowerCenter);
        RectTransform killValueRect = killValue.rectTransform;
        killValueRect.anchorMin = new Vector2(0f, 0f);
        killValueRect.anchorMax = new Vector2(1f, 0.55f);
        killValueRect.offsetMin = Vector2.zero;
        killValueRect.offsetMax = Vector2.zero;
        killValue.text = "0";
        killValue.fontStyle = FontStyle.Bold;

        GameSessionHudView sessionView = sessionRoot.GetComponent<GameSessionHudView>();
        sessionView.Configure(timerText, killValue);
        return sessionView;
    }

    public static void BuildInventoryPlaceholders(RectTransform canvasRect, Font font = null, Sprite sprite = null)
    {
        font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sprite ??= HudUiSprites.White;

        GameObject inventoryPanel = new GameObject("InventoryPanel", typeof(RectTransform));
        inventoryPanel.transform.SetParent(canvasRect, false);
        RectTransform inventoryRect = inventoryPanel.GetComponent<RectTransform>();
        inventoryRect.anchorMin = new Vector2(1f, 0f);
        inventoryRect.anchorMax = new Vector2(1f, 1f);
        inventoryRect.pivot = new Vector2(1f, 0.5f);
        inventoryRect.anchoredPosition = new Vector2(-20f, 0f);
        inventoryRect.sizeDelta = new Vector2(300f, -48f);

        Image inventoryBackground = CreateImage("Background", inventoryRect, sprite, new Color(0.08f, 0.1f, 0.14f, 0.55f));
        StretchFull(inventoryBackground.rectTransform);

        Text inventoryLabel = CreateText("Label", inventoryRect, font, 22, TextAnchor.MiddleCenter);
        StretchFull(inventoryLabel.rectTransform);
        inventoryLabel.text = "인벤토리 UI";
        inventoryLabel.color = new Color(0.9f, 0.92f, 0.96f, 0.9f);

        GameObject toggleObject = new GameObject("InventoryToggle", typeof(RectTransform));
        toggleObject.transform.SetParent(canvasRect, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0f);
        toggleRect.anchorMax = new Vector2(1f, 0f);
        toggleRect.pivot = new Vector2(1f, 0f);
        toggleRect.anchoredPosition = new Vector2(-20f, 20f);
        toggleRect.sizeDelta = new Vector2(96f, 40f);

        Image toggleBackground = CreateImage("Background", toggleRect, sprite, new Color(0.12f, 0.16f, 0.22f, 0.9f));
        StretchFull(toggleBackground.rectTransform);

        Text toggleLabel = CreateText("Label", toggleRect, font, 16, TextAnchor.MiddleCenter);
        StretchFull(toggleLabel.rectTransform);
        toggleLabel.text = "인벤토리";
        toggleLabel.fontStyle = FontStyle.Bold;
    }

    static PlayerStatusHudView BuildStatusPanel(RectTransform canvasRect, Font font, Sprite sprite, GameObject root)
    {
        GameObject panelObject = new GameObject("PlayerStatusPanel", typeof(RectTransform));
        panelObject.transform.SetParent(canvasRect, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(24f, 24f);
        panelRect.sizeDelta = new Vector2(420f, 132f);

        Image panelBackground = CreateImage("PanelBackground", panelRect, sprite, new Color(0.08f, 0.1f, 0.14f, 0.55f));
        StretchFull(panelBackground.rectTransform);

        GameObject levelBoxObject = new GameObject("LevelBox", typeof(RectTransform));
        levelBoxObject.transform.SetParent(panelRect, false);
        RectTransform levelBoxRect = levelBoxObject.GetComponent<RectTransform>();
        levelBoxRect.anchorMin = new Vector2(0f, 0f);
        levelBoxRect.anchorMax = new Vector2(0f, 1f);
        levelBoxRect.pivot = new Vector2(0f, 0.5f);
        levelBoxRect.anchoredPosition = new Vector2(0f, 0f);
        levelBoxRect.sizeDelta = new Vector2(72f, 0f);
        levelBoxRect.pivot = new Vector2(0f, 0f);

        Image levelBackground = CreateImage("LevelBackground", levelBoxRect, sprite, new Color(0.12f, 0.16f, 0.22f, 0.9f));
        StretchFull(levelBackground.rectTransform);

        Text levelCaption = CreateText("LevelCaption", levelBoxRect, font, 16, TextAnchor.UpperCenter);
        RectTransform levelCaptionRect = levelCaption.rectTransform;
        levelCaptionRect.anchorMin = new Vector2(0f, 0.55f);
        levelCaptionRect.anchorMax = new Vector2(1f, 1f);
        levelCaptionRect.offsetMin = Vector2.zero;
        levelCaptionRect.offsetMax = Vector2.zero;
        levelCaption.text = "LV";
        levelCaption.fontStyle = FontStyle.Bold;

        Text levelValue = CreateText("LevelValue", levelBoxRect, font, 28, TextAnchor.LowerCenter);
        RectTransform levelValueRect = levelValue.rectTransform;
        levelValueRect.anchorMin = new Vector2(0f, 0f);
        levelValueRect.anchorMax = new Vector2(1f, 0.55f);
        levelValueRect.offsetMin = Vector2.zero;
        levelValueRect.offsetMax = Vector2.zero;
        levelValue.text = "1";

        GameObject barsColumn = new GameObject("BarsColumn", typeof(RectTransform));
        barsColumn.transform.SetParent(panelRect, false);
        RectTransform barsRect = barsColumn.GetComponent<RectTransform>();
        barsRect.anchorMin = new Vector2(0f, 0f);
        barsRect.anchorMax = new Vector2(1f, 1f);
        barsRect.offsetMin = new Vector2(84f, 8f);
        barsRect.offsetMax = new Vector2(-8f, -8f);

        StatBarWidgets hp = CreateStatBarRow(barsRect, "HpRow", font, sprite, "체력: 10/10", new Color(0.92f, 0.2f, 0.18f, 1f), 0f);
        StatBarWidgets mp = CreateStatBarRow(barsRect, "MpRow", font, sprite, "마력: 5/5", new Color(0.2f, 0.45f, 0.95f, 1f), -44f);
        StatBarWidgets exp = CreateStatBarRow(barsRect, "ExpRow", font, sprite, "경험치: 0/500", new Color(0.95f, 0.82f, 0.15f, 1f), -88f);

        PlayerStatusHudView view = root.GetComponent<PlayerStatusHudView>();
        view.Configure(
            levelValue,
            hp.valueText,
            mp.valueText,
            exp.valueText,
            hp.fillRect,
            mp.fillRect,
            exp.fillRect,
            hp.fillImage,
            mp.fillImage,
            exp.fillImage);

        return view;
    }

    struct StatBarWidgets
    {
        public Text valueText;
        public RectTransform fillRect;
        public Image fillImage;
    }

    static StatBarWidgets CreateStatBarRow(
        RectTransform parent,
        string rowName,
        Font font,
        Sprite sprite,
        string previewText,
        Color fillColor,
        float yOffset)
    {
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, yOffset);
        rowRect.sizeDelta = new Vector2(0f, 36f);

        Image barBackground = CreateImage("BarBackground", rowRect, sprite, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        RectTransform barBackgroundRect = barBackground.rectTransform;
        barBackgroundRect.anchorMin = new Vector2(0f, 0f);
        barBackgroundRect.anchorMax = new Vector2(1f, 0.45f);
        barBackgroundRect.offsetMin = Vector2.zero;
        barBackgroundRect.offsetMax = Vector2.zero;

        Image barFill = CreateImage("BarFill", barBackgroundRect, sprite, fillColor);
        RectTransform barFillRect = barFill.rectTransform;
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        barFillRect.pivot = new Vector2(0f, 0.5f);

        Text valueText = CreateText("ValueLabel", rowRect, font, 18, TextAnchor.MiddleLeft);
        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(0f, 0.55f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        valueText.text = previewText;
        valueText.fontStyle = FontStyle.Bold;
        valueText.transform.SetAsLastSibling();

        return new StatBarWidgets
        {
            valueText = valueText,
            fillRect = barFillRect,
            fillImage = barFill
        };
    }

    static Text CreateText(string name, RectTransform parent, Font font, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.color = Color.white;
        return text;
    }

    static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void StretchFull(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
