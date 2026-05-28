using UnityEngine;

// 프리팹에 필수 컴포넌트가 있는지 확인합니다 (런타임 자동 추가 없음).
public static class GameplayComponents
{
    public static bool EnsurePlayer(GameObject player, bool logIfMissing)
    {
        if (player == null)
        {
            return false;
        }

        bool ok = true;
        ok &= Has<PlayerMovement>(player, logIfMissing);
        ok &= Has<PlayerWorldPosition>(player, logIfMissing);
        ok &= Has<PlayerStats>(player, logIfMissing);
        ok &= Has<PlayerHealth>(player, logIfMissing);
        ok &= Has<PlayerWeaponCombat>(player, logIfMissing);
        ok &= Has<PlayerRangedCombat>(player, logIfMissing);
        ok &= Has<PlayerMagicCombat>(player, logIfMissing);
        return ok;
    }

    public static bool EnsureMonster(GameObject monster, bool logIfMissing)
    {
        if (monster == null)
        {
            return false;
        }

        bool ok = true;
        ok &= Has<MonsterMovement>(monster, logIfMissing);
        ok &= Has<MonsterHealth>(monster, logIfMissing);
        ok &= Has<MonsterHitReaction>(monster, logIfMissing);
        ok &= Has<MonsterAttack>(monster, logIfMissing);
        ok &= Has<MonsterFarDespawn>(monster, logIfMissing);
        return ok;
    }

    static bool Has<T>(GameObject target, bool logIfMissing) where T : Component
    {
        if (target.GetComponent<T>() != null)
        {
            return true;
        }

        if (logIfMissing)
        {
            Debug.LogError(
                "[GameplayComponents] Missing " + typeof(T).Name + " on " + target.name
                + ". Run menu Simplegame2/Setup Gameplay Prefabs.",
                target);
        }

        return false;
    }
}
