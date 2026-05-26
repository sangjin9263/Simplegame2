using UnityEngine;
using UnityEngine.UI;

// 상단 좌측 타이머·킬 카운트 (현재는 플레이스홀더).
public class GameSessionHudView : MonoBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField] Text killCountText;
    [SerializeField] float sessionDurationSeconds = 30f * 60f;

    float remainingSeconds;
    int killCount;

    public void Configure(Text timer, Text killCount)
    {
        timerText = timer;
        killCountText = killCount;
    }

    void Start()
    {
        BindReferencesIfNeeded();
        remainingSeconds = sessionDurationSeconds;
        RefreshKillCount();
        RefreshTimer();
    }

    void BindReferencesIfNeeded()
    {
        Transform sessionRoot = transform;
        if (sessionRoot.name != "TopLeftSessionHud")
        {
            sessionRoot = transform.Find("Canvas/TopLeftSessionHud");
        }

        if (sessionRoot == null)
        {
            return;
        }

        if (timerText == null)
        {
            timerText = sessionRoot.Find("TimerBackground/TimerText")?.GetComponent<Text>();
        }

        if (killCountText == null)
        {
            killCountText = sessionRoot.Find("KillCountBackground/KillValue")?.GetComponent<Text>();
        }
    }

    void Update()
    {
        if (!Application.isPlaying || remainingSeconds <= 0f)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        RefreshTimer();
    }

    public void AddKill(int amount = 1)
    {
        killCount = Mathf.Max(0, killCount + amount);
        RefreshKillCount();
    }

    void RefreshTimer()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    void RefreshKillCount()
    {
        if (killCountText != null)
        {
            killCountText.text = killCount.ToString();
        }
    }
}
