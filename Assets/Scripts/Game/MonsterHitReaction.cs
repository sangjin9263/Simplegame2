using System.Collections;
using UnityEngine;

// 몬스터가 맞으면 잠깐 붉게 보이고 뒤로 밀려납니다.
public class MonsterHitReaction : MonoBehaviour
{
    [SerializeField] float knockbackDistance = 0.72f;
    [SerializeField] float hitFlashDuration = 0.12f;
    [SerializeField] Color hitFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [SerializeField] float collisionRadius = 0.22f;
    [SerializeField] float collisionHeight = 1.05f;
    [SerializeField] float groundHeight = 0f;

    SpriteRenderer[] spriteRenderers;
    Color[] originalColors;
    Coroutine hitRoutine;

    CapsuleMotor.Settings motorSettings;
    MonsterHealth monsterHealth;

    void Awake()
    {
        motorSettings = new CapsuleMotor.Settings
        {
            radius = collisionRadius,
            height = collisionHeight,
            groundY = groundHeight,
            skin = 0.015f,
            embeddedIgnoreDistance = 0.02f
        };

        monsterHealth = GetComponent<MonsterHealth>();
        CacheRenderers();
    }

    void CacheRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    public void ApplyHit(Vector3 knockbackDirection, Transform attacker, int damage)
    {
        if (monsterHealth == null)
        {
            monsterHealth = GetComponent<MonsterHealth>();
        }

        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return;
        }

        int appliedDamage = Mathf.Max(1, damage);
        if (monsterHealth != null)
        {
            monsterHealth.TakeDamage(appliedDamage);
            if (monsterHealth.IsDead)
            {
                return;
            }
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        hitRoutine = StartCoroutine(HitRoutine(knockbackDirection));
    }

    IEnumerator HitRoutine(Vector3 knockbackDirection)
    {
        knockbackDirection.y = 0f;
        if (knockbackDirection.sqrMagnitude > 0.0001f)
        {
            knockbackDirection.Normalize();
            CapsuleMotor.Move(
                transform,
                knockbackDirection * knockbackDistance,
                motorSettings,
                transform,
                MoverRole.Monster);
        }

        SetSpriteColors(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreSpriteColors();
        hitRoutine = null;
    }

    void SetSpriteColors(Color color)
    {
        if (spriteRenderers == null)
        {
            CacheRenderers();
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = color;
        }
    }

    void RestoreSpriteColors()
    {
        if (spriteRenderers == null || originalColors == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = originalColors[i];
        }
    }
}
