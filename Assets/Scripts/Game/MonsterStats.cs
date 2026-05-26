using UnityEngine;

// 몬스터 CSV 한 행을 런타임에 붙입니다.
public class MonsterStats : MonoBehaviour
{
    [SerializeField] int monId;
    [SerializeField] int level = 1;
    [SerializeField] MonsterKind kind = MonsterKind.Normal;
    [SerializeField] int maxMp;

    public int MonId => monId;
    public int Level => level;
    public MonsterKind Kind => kind;
    public int MaxMp => maxMp;
    public bool IsBoss => kind == MonsterKind.Boss;
    public bool IsNormal => kind == MonsterKind.Normal;

    public static void Apply(GameObject monster, MonsterDefinitionRow row)
    {
        if (monster == null)
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
        if (health == null)
        {
            health = monster.AddComponent<MonsterHealth>();
        }

        health.Configure(row.hp, row.giveExp, row.kind);

        MonsterMovement movement = monster.GetComponent<MonsterMovement>();
        if (movement == null)
        {
            movement = monster.AddComponent<MonsterMovement>();
        }

        movement.Configure(row.moveSpeed);

        MonsterAttack attack = monster.GetComponent<MonsterAttack>();
        if (attack == null)
        {
            attack = monster.AddComponent<MonsterAttack>();
        }

        attack.Configure(row.damage);

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
    }
}
