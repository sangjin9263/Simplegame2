using System;
using UnityEngine;

// 플레이어 HP와 피격. 최대 HP는 PlayerStats(CSV)가 설정합니다.
[DefaultExecutionOrder(50)]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int maxHp;
    [SerializeField] bool infiniteHp;

    int currentHp;
    bool initialized;

    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public bool IsAlive => initialized && (infiniteHp || currentHp > 0);
    public bool InfiniteHp => infiniteHp;

    public void SetInfiniteHp(bool enabled)
    {
        infiniteHp = enabled;
        if (infiniteHp && currentHp <= 0)
        {
            currentHp = Mathf.Max(1, maxHp);
        }

        NotifyHpChanged(false);
    }

    public event Action<int, int, bool> OnHpChanged;

    public void ApplyMaxHp(int newMaxHp, bool healToFull)
    {
        maxHp = Mathf.Max(1, newMaxHp);
        if (healToFull || !initialized)
        {
            currentHp = maxHp;
        }
        else
        {
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }

        initialized = true;
        NotifyHpChanged(false);
    }

    public bool TakeDamage(int damage)
    {
        if (!initialized || damage <= 0)
        {
            return false;
        }

        if (!IsAlive)
        {
            return false;
        }

        if (infiniteHp)
        {
            NotifyHpChanged(false);
            return true;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        NotifyHpChanged(false);
        return true;
    }

    public void HealToFull()
    {
        if (!initialized)
        {
            return;
        }

        currentHp = maxHp;
        NotifyHpChanged(false);
    }

    void NotifyHpChanged(bool refilled)
    {
        OnHpChanged?.Invoke(currentHp, maxHp, refilled);
    }
}
