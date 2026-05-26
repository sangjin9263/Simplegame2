using UnityEngine;

// 플레이어에서 너무 멀어진 몬스터를 제거합니다 (옛 스폰 지점 추적 몹 정리).
public class MonsterFarDespawn : MonoBehaviour
{
    [SerializeField] float despawnDistance = 48f;
    [SerializeField] float checkInterval = 0.5f;
    [SerializeField] float minLifetimeBeforeDespawn = 4f;

    float nextCheckTime;
    float spawnedAtTime;

    void OnEnable()
    {
        spawnedAtTime = Time.time;
    }

    public void NotifySpawned(float suggestedDespawnDistance)
    {
        spawnedAtTime = Time.time;
        despawnDistance = Mathf.Max(despawnDistance, suggestedDespawnDistance);
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

        MonsterHealth health = GetComponent<MonsterHealth>();
        if (health != null && health.IsDead)
        {
            return;
        }

        if (!TryGetPlayerGroundPosition(out Vector3 playerPosition))
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

    static bool TryGetPlayerGroundPosition(out Vector3 playerPosition)
    {
        if (PlayerWorldPosition.TryGetWorldCenter(0f, out playerPosition))
        {
            playerPosition.y = 0f;
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(WorldCollision.PlayerTag);
        if (playerObject == null)
        {
            playerPosition = Vector3.zero;
            return false;
        }

        PlayerMovement movement = playerObject.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            playerPosition = PlayerMovement.LastWorldCenter;
            playerPosition.y = 0f;
            return true;
        }

        playerPosition = playerObject.transform.position;
        playerPosition.y = 0f;
        return true;
    }
}
