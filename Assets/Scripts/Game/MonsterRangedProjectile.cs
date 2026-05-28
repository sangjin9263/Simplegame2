using UnityEngine;

// 몬스터 원거리 투사체로 플레이어에게 피해를 줍니다.
public class MonsterRangedProjectile : MonoBehaviour
{
    public struct Settings
    {
        public float speed;
        public float hitRadius;
        public int damage;
        public float maxLifetime;
        public bool useEnergyImpact;
        public Vector3 visualRotationOffset;
        public float visualScale;
        public float aimHeightOffset;
        public Vector3 targetPoint;
        public float trajectoryArcHeight;
    }

    const float DefaultVisualScale = 0.35f;

    [SerializeField] float speed = 14f;
    [SerializeField] float hitRadius = 0.42f;
    [SerializeField] int damage = 1;
    [SerializeField] float maxLifetime = 2.5f;
    [SerializeField] bool useEnergyImpact;
    Vector3 moveDirection;
    Vector3 fireOrigin;
    Vector3 targetPoint;
    float totalTravelDistance;
    float traveledDistance;
    float trajectoryArcHeight;
    float aliveTime;
    Vector3 visualRotationOffset;
    float visualScale = DefaultVisualScale;
    float aimHeightOffset = 0.45f;
    GameObject visualInstance;

    public static MonsterRangedProjectile Spawn(
        Vector3 origin,
        Vector3 direction,
        GameObject projectilePrefab,
        Settings settings)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        GameObject projectileObject = new GameObject("MonsterRangedProjectile");
        MonsterRangedProjectile projectile = projectileObject.AddComponent<MonsterRangedProjectile>();
        projectile.Configure(origin, direction, projectilePrefab, settings);
        return projectile;
    }

    void Configure(Vector3 origin, Vector3 direction, GameObject projectilePrefab, Settings settings)
    {
        speed = settings.speed > 0f ? settings.speed : speed;
        hitRadius = settings.hitRadius > 0f ? settings.hitRadius : hitRadius;
        damage = settings.damage > 0 ? settings.damage : damage;
        maxLifetime = settings.maxLifetime > 0f ? settings.maxLifetime : maxLifetime;
        useEnergyImpact = settings.useEnergyImpact;
        visualRotationOffset = settings.visualRotationOffset;
        visualScale = settings.visualScale > 0f ? settings.visualScale : DefaultVisualScale;
        aimHeightOffset = settings.aimHeightOffset > 0f ? settings.aimHeightOffset : aimHeightOffset;
        targetPoint = settings.targetPoint;
        trajectoryArcHeight = Mathf.Max(0f, settings.trajectoryArcHeight);

        moveDirection = direction;
        fireOrigin = origin;
        totalTravelDistance = Vector3.Distance(origin, targetPoint);
        if (totalTravelDistance < 0.01f)
        {
            totalTravelDistance = speed * maxLifetime;
            targetPoint = origin + direction * totalTravelDistance;
        }

        transform.position = origin;
        transform.rotation = Quaternion.identity;

        if (projectilePrefab != null)
        {
            visualInstance = Instantiate(projectilePrefab, transform);
            visualInstance.name = projectilePrefab.name;
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one * visualScale;
        }

        ApplyVisualRotation();
    }

    void Update()
    {
        aliveTime += Time.deltaTime;

        Vector3 previousPosition = transform.position;
        if (!useEnergyImpact && trajectoryArcHeight > 0.01f)
        {
            UpdateArcPosition(Time.deltaTime);
        }
        else
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        Vector3 velocity = transform.position - previousPosition;
        if (velocity.sqrMagnitude > 0.000001f)
        {
            moveDirection = velocity.normalized;
        }

        ApplyVisualRotation();

        TryHitPlayer();

        if (aliveTime >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void UpdateArcPosition(float deltaTime)
    {
        traveledDistance += speed * deltaTime;
        float progress = Mathf.Clamp01(traveledDistance / Mathf.Max(totalTravelDistance, 0.01f));

        Vector3 position = Vector3.Lerp(fireOrigin, targetPoint, progress);
        position.y += Mathf.Sin(progress * Mathf.PI) * trajectoryArcHeight;
        transform.position = position;
    }

    void ApplyVisualRotation()
    {
        if (visualInstance == null || moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (useEnergyImpact)
        {
            visualInstance.transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            return;
        }

        visualInstance.transform.rotation = RangedProjectile.BuildVisualRotation(moveDirection, visualRotationOffset);
    }

    void TryHitPlayer()
    {
        PlayerHealth playerHealth = GameSession.PlayerHealth;
        if (playerHealth == null || !playerHealth.IsAlive)
        {
            return;
        }

        if (!GameSession.TryGetPlayerTransform(out Transform playerTransform))
        {
            return;
        }

        Vector3 playerWorld = playerTransform.position;
        float playerSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(playerWorld, playerWorld.y);
        float aimX = playerWorld.x;
        float aimZ = playerWorld.z;
        if (GameSession.TryGetPlayerWorldCenter(out Vector3 trackedCenter))
        {
            aimX = trackedCenter.x;
            aimZ = trackedCenter.z;
        }

        Vector3 aimPoint = new Vector3(aimX, playerSurfaceY + aimHeightOffset, aimZ);
        if ((aimPoint - transform.position).sqrMagnitude > hitRadius * hitRadius)
        {
            return;
        }

        if (useEnergyImpact)
        {
            ImpactVfx.SpawnEnergyImpact(aimPoint);
        }
        else
        {
            ImpactVfx.SpawnHitImpact(aimPoint);
        }

        playerHealth.TakeDamage(damage);
        Destroy(gameObject);
    }
}
