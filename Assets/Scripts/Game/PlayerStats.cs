using System;
using UnityEngine;

// 플레이어 LV / EXP / HP / MP — CSV 레벨 테이블 기반.
[DefaultExecutionOrder(-150)]
public class PlayerStats : MonoBehaviour
{
    public const string LevelId = "level";
    public const string HpId = "hp";
    public const string MpId = "mp";
    public const string ExpId = "exp";

    [SerializeField] TextAsset statsCsvOverride;
    [SerializeField] bool syncHpFromPlayerHealth = true;

    PlayerLevelProgressionTable progressionTable;
    PlayerHealth playerHealth;

    int level = 1;
    int levelMax = 1;
    int hp;
    int hpMax;
    int mp;
    int mpMax;
    int exp;
    int expMax;

    public event Action OnStatsChanged;

    public int Level => level;
    public int LevelMax => levelMax;
    public int Hp => hp;
    public int HpMax => hpMax;
    public int Mp => mp;
    public int MpMax => mpMax;
    public int Exp => exp;
    public int ExpMax => expMax;

    void Awake()
    {
        progressionTable = LoadTable();
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = gameObject.AddComponent<PlayerHealth>();
        }

        ApplyLevel(1, resetExp: true, refillHpMp: true);
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHpChanged += HandleHpChanged;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHpChanged -= HandleHpChanged;
        }
    }

    void Start()
    {
        NotifyChanged();
    }

    PlayerLevelProgressionTable LoadTable()
    {
        if (statsCsvOverride != null)
        {
            return PlayerLevelProgressionTable.LoadFromCsv(statsCsvOverride.text);
        }

        return PlayerLevelProgressionTable.LoadDefault();
    }

    public void ApplyLevel(int targetLevel, bool resetExp, bool refillHpMp)
    {
        if (progressionTable == null)
        {
            return;
        }

        levelMax = Mathf.Max(1, progressionTable.MaxLevel);
        level = Mathf.Clamp(targetLevel, 1, levelMax);

        if (!progressionTable.TryGetByLevel(level, out PlayerLevelProgressionRow row))
        {
            return;
        }

        hpMax = row.hp;
        mpMax = row.mp;

        if (refillHpMp)
        {
            hp = hpMax;
            mp = mpMax;
        }
        else
        {
            hp = Mathf.Clamp(hp, 0, hpMax);
            mp = Mathf.Clamp(mp, 0, mpMax);
        }

        if (resetExp)
        {
            exp = row.expTotal;
        }

        RefreshExpToNextLevel();

        if (playerHealth != null)
        {
            playerHealth.ApplyMaxHp(hpMax, refillHpMp);
            if (!refillHpMp)
            {
                SyncHpFromPlayerHealth(playerHealth.CurrentHp, playerHealth.MaxHp, false);
            }
        }
    }

    void RefreshExpToNextLevel()
    {
        if (progressionTable == null)
        {
            expMax = 1;
            return;
        }

        if (!progressionTable.TryGetByLevel(level, out PlayerLevelProgressionRow current))
        {
            expMax = 1;
            return;
        }

        if (!progressionTable.TryGetNextLevel(level, out PlayerLevelProgressionRow next))
        {
            expMax = 1;
            exp = Mathf.Max(exp, current.expTotal);
            return;
        }

        int expIntoLevel = Mathf.Max(0, exp - current.expTotal);
        expMax = Mathf.Max(1, next.expTotal - current.expTotal);
        exp = expIntoLevel;
    }

    void HandleHpChanged(int currentHp, int maxHp, bool refilled)
    {
        if (!syncHpFromPlayerHealth)
        {
            return;
        }

        SyncHpFromPlayerHealth(currentHp, maxHp, notify: true);
    }

    public void SyncHpFromPlayerHealth(int currentHp, int maxHp, bool notify)
    {
        hpMax = Mathf.Max(1, maxHp);
        hp = Mathf.Clamp(currentHp, 0, hpMax);

        if (notify)
        {
            NotifyChanged();
        }
    }

    public void AddExp(int amount)
    {
        if (amount <= 0 || progressionTable == null)
        {
            return;
        }

        if (!progressionTable.TryGetByLevel(level, out PlayerLevelProgressionRow current))
        {
            return;
        }

        int totalExp = current.expTotal + Mathf.Max(0, exp) + amount;

        while (progressionTable.TryGetNextLevel(level, out PlayerLevelProgressionRow next)
            && totalExp >= next.expTotal)
        {
            level = next.level;
            hpMax = next.hp;
            mpMax = next.mp;
            hp = hpMax;
            mp = mpMax;

            if (playerHealth != null)
            {
                playerHealth.ApplyMaxHp(hpMax, true);
            }

            current = next;
        }

        exp = Mathf.Max(0, totalExp - current.expTotal);
        RefreshExpToNextLevel();
        NotifyChanged();
    }

    public void SetMp(int current, int max)
    {
        mpMax = Mathf.Max(1, max);
        mp = Mathf.Clamp(current, 0, mpMax);
        NotifyChanged();
    }

    void NotifyChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
