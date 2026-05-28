using UnityEngine;

// 플레이어에서 너무 멀어진 몬스터를 제거합니다 (옛 스폰 지점 추적 몹 정리).
public class MonsterFarDespawn : MonoBehaviour
{
    [SerializeField] float despawnDistance = 48f;
    [SerializeField] float checkInterval = 0.5f;
    [SerializeField] float minLifetimeBeforeDespawn = 4f;

    float nextCheckTime;
    float spawnedAtTime;
    MonsterHealth monsterHealth;

    void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
    }

    void OnEnable()
    {
        spawnedAtTime = Time.time;
    }

    public void NotifySpawned(float suggestedDespawnDistance)
    {
        spawnedAtTime = Time.time;
        despawnDistance = Mathf.Max(despawnDistance, suggestedDespawnDistance);
    }

    public void ConfigureForKind(MonsterKind kind)
    {
        enabled = kind != MonsterKind.Boss;
    }

    void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;

        if (Time.time - spawnedAtTime < minLifetimeBeforeDespawn)
        {
            return;
        }

        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return;
        }

        if (!GameSession.TryGetPlayerWorldCenter(out Vector3 playerPosition))
        {
            return;
        }

        Vector3 toPlayer = transform.position - playerPosition;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > despawnDistance * despawnDistance)
        {
            Destroy(gameObject);
        }
    }
}
