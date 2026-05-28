using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 하단 왼쪽 LV / HP / MP / EXP HUD.
public class PlayerStatusHudView : MonoBehaviour
{
    [SerializeField] Text levelValueText;
    [SerializeField] Text hpValueText;
    [SerializeField] Text mpValueText;
    [SerializeField] Text expValueText;
    [SerializeField] RectTransform hpFillRect;
    [SerializeField] RectTransform mpFillRect;
    [SerializeField] RectTransform expFillRect;
    [SerializeField] Image hpFillImage;
    [SerializeField] Image mpFillImage;
    [SerializeField] Image expFillImage;

    PlayerStats playerStats;

    public void Configure(
        Text levelText,
        Text hpText,
        Text mpText,
        Text expText,
        RectTransform hpFill,
        RectTransform mpFill,
        RectTransform expFill,
        Image hpImage,
        Image mpImage,
        Image expImage)
    {
        levelValueText = levelText;
        hpValueText = hpText;
        mpValueText = mpText;
        expValueText = expText;
        hpFillRect = hpFill;
        mpFillRect = mpFill;
        expFillRect = expFill;
        hpFillImage = hpImage;
        mpFillImage = mpImage;
        expFillImage = expImage;
    }

    void Start()
    {
        BindReferencesIfNeeded();
        StartCoroutine(WaitAndBindStats());
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= Refresh;
        }
    }

    IEnumerator WaitAndBindStats()
    {
        for (int i = 0; i < 60; i++)
        {
            BindPlayerStats();
            if (playerStats != null)
            {
                yield break;
            }

            yield return null;
        }
    }

    void BindPlayerStats()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= Refresh;
        }

        playerStats = GameSession.PlayerStats;
        if (playerStats == null && GameSession.TryGetPlayerTransform(out Transform player))
        {
            playerStats = player.GetComponent<PlayerStats>();
        }

        if (playerStats == null)
        {
            return;
        }

        playerStats.OnStatsChanged += Refresh;
        Refresh();
    }

    void Refresh()
    {
        if (playerStats == null)
        {
            return;
        }

        if (levelValueText != null)
        {
            levelValueText.text = playerStats.Level.ToString();
        }

        SetBar(
            hpValueText,
            hpFillRect,
            hpFillImage,
            PlayerStats.HpId,
            playerStats.Hp,
            playerStats.HpMax,
            new Color(0.92f, 0.2f, 0.18f, 1f));

        SetBar(
            mpValueText,
            mpFillRect,
            mpFillImage,
            PlayerStats.MpId,
            playerStats.Mp,
            playerStats.MpMax,
            new Color(0.2f, 0.45f, 0.95f, 1f));

        SetBar(
            expValueText,
            expFillRect,
            expFillImage,
            PlayerStats.ExpId,
            playerStats.Exp,
            playerStats.ExpMax,
            new Color(0.95f, 0.82f, 0.15f, 1f));
    }

    static void SetBar(
        Text valueText,
        RectTransform fillRect,
        Image fillImage,
        string statId,
        int current,
        int max,
        Color fillColor)
    {
        if (valueText != null)
        {
            valueText.text = StatDisplayLabels.FormatValueText(statId, current, max);
        }

        float normalized = max > 0 ? (float)current / max : 0f;
        normalized = Mathf.Clamp01(normalized);

        if (fillRect != null)
        {
            fillRect.anchorMax = new Vector2(normalized, 1f);
        }

        if (fillImage != null)
        {
            fillImage.color = fillColor;
        }
    }

    void BindReferencesIfNeeded()
    {
        Transform panel = transform.Find("Canvas/PlayerStatusPanel");
        if (panel == null)
        {
            return;
        }

        if (levelValueText == null)
        {
            levelValueText = panel.Find("LevelBox/LevelValue")?.GetComponent<Text>();
        }

        BindBar(panel, "HpRow", ref hpValueText, ref hpFillRect, ref hpFillImage);
        BindBar(panel, "MpRow", ref mpValueText, ref mpFillRect, ref mpFillImage);
        BindBar(panel, "ExpRow", ref expValueText, ref expFillRect, ref expFillImage);
    }

    static void BindBar(
        Transform panel,
        string rowName,
        ref Text valueText,
        ref RectTransform fillRect,
        ref Image fillImage)
    {
        Transform row = panel.Find(rowName);
        if (row == null)
        {
            return;
        }

        if (valueText == null)
        {
            valueText = row.Find("ValueLabel")?.GetComponent<Text>();
        }

        Transform fillTransform = row.Find("BarBackground/BarFill");
        if (fillTransform != null)
        {
            if (fillRect == null)
            {
                fillRect = fillTransform.GetComponent<RectTransform>();
            }

            if (fillImage == null)
            {
                fillImage = fillTransform.GetComponent<Image>();
            }
        }
    }
}
