using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 HP HUD 뷰 (프리팹/씬 UI 참조용).
public class PlayerHpHudView : MonoBehaviour
{
    [SerializeField] Text hpLabelText;
    [SerializeField] Text statusText;
    [SerializeField] RectTransform fillRect;
    [SerializeField] Image fillImage;
    [SerializeField] Color fillColor = new Color(0.2f, 0.85f, 0.35f, 0.95f);
    [SerializeField] Color fillLowColor = new Color(0.95f, 0.25f, 0.15f, 0.95f);

    PlayerHealth playerHealth;

    public void Configure(Text hpText, Text status, RectTransform hpFillRect, Image hpFillImage)
    {
        hpLabelText = hpText;
        statusText = status;
        fillRect = hpFillRect;
        fillImage = hpFillImage;
    }

    void Start()
    {
        BindReferencesIfNeeded();
        StartCoroutine(WaitAndBindPlayerHealth());
    }

    IEnumerator WaitAndBindPlayerHealth()
    {
        for (int i = 0; i < 60; i++)
        {
            BindPlayerHealth();
            if (playerHealth != null)
            {
                yield break;
            }

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHpChanged -= HandleHpChanged;
        }
    }

    public void BindPlayerHealth()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHpChanged -= HandleHpChanged;
        }

        playerHealth = FindPlayerHealth();
        if (playerHealth == null)
        {
            SetHpText(0, 0);
            SetStatusMessage("PlayerHealth 없음", Color.red);
            return;
        }

        playerHealth.OnHpChanged += HandleHpChanged;
        Refresh(playerHealth.CurrentHp, playerHealth.MaxHp, false);
    }

    void HandleHpChanged(int currentHp, int maxHp, bool refilled)
    {
        Refresh(currentHp, maxHp, refilled);

        if (refilled)
        {
            SetStatusMessage("HP 0 → " + maxHp + " (테스트 리필)", new Color(1f, 0.92f, 0.35f, 1f));
            StopAllCoroutines();
            StartCoroutine(ClearStatusAfterDelay(2.5f));
        }
        else
        {
            SetStatusMessage("데미지 -1", new Color(1f, 0.55f, 0.45f, 1f));
            StopAllCoroutines();
            StartCoroutine(ClearStatusAfterDelay(0.6f));
        }
    }

    IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    void Refresh(int currentHp, int maxHp, bool refilled)
    {
        SetHpText(currentHp, maxHp);

        float normalized = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        normalized = Mathf.Clamp01(normalized);

        if (fillRect != null)
        {
            fillRect.anchorMax = new Vector2(normalized, 1f);
        }

        if (fillImage != null)
        {
            fillImage.color = normalized <= 0.2f ? fillLowColor : fillColor;
        }

        if (refilled && statusText != null)
        {
            statusText.color = new Color(1f, 0.92f, 0.35f, 1f);
        }
    }

    void SetHpText(int currentHp, int maxHp)
    {
        if (hpLabelText == null)
        {
            return;
        }

        hpLabelText.text = "HP " + currentHp + " / " + maxHp;
    }

    void SetStatusMessage(string message, Color color)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = color;
    }

    void BindReferencesIfNeeded()
    {
        if (hpLabelText == null)
        {
            hpLabelText = FindText("Canvas/PlayerHpPanel/HpLabel");
        }

        if (statusText == null)
        {
            statusText = FindText("Canvas/PlayerHpPanel/HpStatus");
        }

        if (fillRect == null || fillImage == null)
        {
            Transform fillTransform = transform.Find("Canvas/PlayerHpPanel/HpBarBackground/HpBarFill");
            if (fillTransform != null)
            {
                fillRect = fillTransform.GetComponent<RectTransform>();
                fillImage = fillTransform.GetComponent<Image>();
            }
        }
    }

    Text FindText(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Text>();
    }

    static PlayerHealth FindPlayerHealth()
    {
        return GameSession.PlayerHealth;
    }
}
