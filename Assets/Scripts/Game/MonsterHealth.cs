using System.Collections;
using UnityEngine;

// 몬스터 HP, 피격, 사망을 처리합니다.
public class MonsterHealth : MonoBehaviour
{
    const int DefaultMaxHp = 10;

    [SerializeField] int maxHp = DefaultMaxHp;
    [SerializeField] int expOnDeathForTest = 1;
    [SerializeField] float deathDestroyDelay = 0.05f;
    [SerializeField] float defaultDeathAnimDuration = 0.8f;
    [SerializeField] SPUM_Prefabs spumPrefabs;

    int currentHp;
    bool isDead;
    bool isDying;
    bool expGranted;
    MonsterWorldHpBar hpBar;
    MonsterMovement monsterMovement;
    MonsterHitReaction hitReaction;

    MonsterKind kind = MonsterKind.Normal;

    public bool IsDead => isDead || isDying;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;
    public MonsterKind Kind => kind;
    public bool IsBoss => kind == MonsterKind.Boss;

    public void Configure(int hp, int expReward, MonsterKind monsterKind)
    {
        kind = monsterKind;
        maxHp = Mathf.Max(1, hp);
        currentHp = maxHp;
        expOnDeathForTest = Mathf.Max(0, expReward);
        RefreshHpBar();
    }

    void Awake()
    {
        if (maxHp <= 0)
        {
            maxHp = DefaultMaxHp;
        }

        currentHp = maxHp;
        monsterMovement = GetComponent<MonsterMovement>();
        hitReaction = GetComponent<MonsterHitReaction>();

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }
    }

    void Start()
    {
        EnsureHpBar();
        RefreshHpBar();
    }

    public bool TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return false;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        RefreshHpBar();

        if (currentHp <= 0)
        {
            BeginDeath();
            return true;
        }

        return true;
    }

    void EnsureHpBar()
    {
        if (hpBar != null)
        {
            return;
        }

        Transform barAnchor = transform;
        Transform unitRoot = transform.Find("UnitRoot");
        if (unitRoot != null)
        {
            barAnchor = unitRoot;
        }

        GameObject barObject = new GameObject("MonsterHpBar");
        barObject.transform.SetParent(barAnchor, false);
        hpBar = barObject.AddComponent<MonsterWorldHpBar>();
        hpBar.Build(barAnchor);
    }

    void RefreshHpBar()
    {
        if (hpBar == null)
        {
            return;
        }

        float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        hpBar.SetFill(ratio);
    }

    void BeginDeath()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;
        GrantExpOnce();

        if (monsterMovement != null)
        {
            monsterMovement.enabled = false;
        }

        if (hitReaction != null)
        {
            hitReaction.enabled = false;
        }

        if (hpBar != null)
        {
            hpBar.SetVisible(false);
        }

        StartCoroutine(DeathRoutine());
    }

    void GrantExpOnce()
    {
        if (expGranted || expOnDeathForTest <= 0)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            return;
        }

        PlayerStats stats = playerObject.GetComponent<PlayerStats>();
        if (stats == null)
        {
            return;
        }

        expGranted = true;
        stats.AddExp(expOnDeathForTest);

        GameSessionHudView sessionHud = Object.FindFirstObjectByType<GameSessionHudView>();
        if (sessionHud != null)
        {
            sessionHud.AddKill(1);
        }
    }

    IEnumerator DeathRoutine()
    {
        float waitDuration = PlayDeathAnimation();
        yield return new WaitForSeconds(waitDuration + deathDestroyDelay);

        isDead = true;
        Destroy(gameObject);
    }

    float PlayDeathAnimation()
    {
        float duration = defaultDeathAnimDuration;

        if (spumPrefabs != null
            && spumPrefabs.DEATH_List != null
            && spumPrefabs.DEATH_List.Count > 0
            && spumPrefabs.DEATH_List[0] != null)
        {
            duration = spumPrefabs.DEATH_List[0].length;
        }

        Animator animator = spumPrefabs != null ? spumPrefabs._anim : null;
        if (animator == null)
        {
            return duration;
        }

        animator.SetBool("1_Move", false);
        animator.SetBool("5_Debuff", false);
        animator.SetBool("isDeath", true);
        animator.ResetTrigger("4_Death");
        animator.SetTrigger("4_Death");

        return duration;
    }
}
