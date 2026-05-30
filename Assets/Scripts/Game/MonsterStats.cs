using UnityEngine;

// 몬스터 CSV 한 행을 런타임에 붙입니다.
public class MonsterStats : MonoBehaviour
{
    [SerializeField] int monId;
    [SerializeField] int level = 1;
    [SerializeField] MonsterKind kind = MonsterKind.Melee;
    [SerializeField] int maxMp;
    [SerializeField] int mpRegen;

    public int MonId => monId;
    public int Level => level;
    public MonsterKind Kind => kind;
    public int MaxMp => maxMp;
    public int MpRegen => mpRegen;
    public bool IsBoss => kind == MonsterKind.Boss;
    public bool IsMelee => kind == MonsterKind.Melee;

    public static void Apply(GameObject monster, MonsterDefinitionRow row)
    {
        if (monster == null)
        {
            return;
        }

        if (!GameplayComponents.EnsureMonster(monster, logIfMissing: true))
        {
            return;
        }

        MonsterStats stats = monster.GetComponent<MonsterStats>();
        if (stats == null)
        {
            stats = monster.AddComponent<MonsterStats>();
        }

        stats.Initialize(row);

        MonsterHealth health = monster.GetComponent<MonsterHealth>();
        health.Configure(row.hp, row.giveExp, row.kind);

        MonsterMovement movement = monster.GetComponent<MonsterMovement>();
        movement.Configure(row.moveSpeed);

        MonsterAttack attack = monster.GetComponent<MonsterAttack>();
        MonsterRangedAttack rangedAttack = monster.GetComponent<MonsterRangedAttack>();

        if (rangedAttack != null)
        {
            rangedAttack.Configure(row.damage);
        }

        bool useRangedAttack = row.monId != 0 ? IsRangedKind(row.kind) : IsRangedMonster(monster.name);
        if (attack != null)
        {
            attack.enabled = !useRangedAttack;
            if (!useRangedAttack)
            {
                attack.Configure(row.damage);
            }
        }

        if (rangedAttack != null)
        {
            rangedAttack.enabled = useRangedAttack;
        }

        MonsterFarDespawn farDespawn = monster.GetComponent<MonsterFarDespawn>();
        if (farDespawn != null)
        {
            farDespawn.ConfigureForKind(row.kind);
        }
    }

    void Initialize(MonsterDefinitionRow row)
    {
        monId = row.monId;
        level = row.level;
        kind = row.kind;
        maxMp = row.mp;
        mpRegen = row.mpRegen;
    }

    static bool IsRangedMonster(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.IndexOf("SPUM_orc_m7", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("SPUM_orc_m8", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("SPUM_orc_m9", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsRangedKind(MonsterKind monsterKind)
    {
        return monsterKind == MonsterKind.Ranged || monsterKind == MonsterKind.Mage;
    }
}
