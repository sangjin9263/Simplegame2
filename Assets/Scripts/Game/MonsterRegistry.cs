using System.Collections.Generic;
using UnityEngine;

// 살아 있는 몬스터 HP 목록(미니맵·검색용)입니다.
public static class MonsterRegistry
{
    static readonly List<MonsterHealth> AliveMonsters = new List<MonsterHealth>();
    static readonly HashSet<MonsterHealth> AliveMonsterSet = new HashSet<MonsterHealth>();

    public static IReadOnlyList<MonsterHealth> Alive => AliveMonsters;

    public static void Clear()
    {
        AliveMonsters.Clear();
        AliveMonsterSet.Clear();
    }

    public static void Register(MonsterHealth health)
    {
        if (health == null || !AliveMonsterSet.Add(health))
        {
            return;
        }

        AliveMonsters.Add(health);
    }

    public static void Unregister(MonsterHealth health)
    {
        if (health == null || !AliveMonsterSet.Remove(health))
        {
            return;
        }

        AliveMonsters.Remove(health);
    }

    // fromPosition에서 가장 가까운 살아 있는 몬스터 방향을 찾습니다 (공격 자동 조준용).
    public static bool TryGetNearestAliveFlatDirection(
        Vector3 fromPosition,
        float maxRange,
        out Vector3 flatDirection)
    {
        return TryGetNearestAliveFlatDirection(fromPosition, maxRange, 0f, fromPosition.y, out flatDirection);
    }

    public static bool TryGetNearestAliveFlatDirection(
        Vector3 fromPosition,
        float maxRange,
        float maxVerticalDelta,
        float referenceSurfaceY,
        out Vector3 flatDirection)
    {
        flatDirection = Vector3.zero;
        if (maxRange <= 0f)
        {
            return false;
        }

        float maxRangeSqr = maxRange * maxRange;
        MonsterHealth nearest = null;
        float nearestSqr = maxRangeSqr;

        for (int i = AliveMonsters.Count - 1; i >= 0; i--)
        {
            MonsterHealth health = AliveMonsters[i];
            if (health == null || health.IsDead)
            {
                if (health != null)
                {
                    AliveMonsterSet.Remove(health);
                }

                AliveMonsters.RemoveAt(i);
                continue;
            }

            Vector3 monsterWorld = health.transform.position;
            float monsterSurfaceY = GroundHeightSampler.GetCharacterSurfaceY(
                monsterWorld,
                monsterWorld.y);

            if (maxVerticalDelta > 0f
                && Mathf.Abs(monsterSurfaceY - referenceSurfaceY) > maxVerticalDelta)
            {
                continue;
            }

            if (GroundHeightSampler.IsWalkableTerrainBlockingLine(
                    fromPosition,
                    referenceSurfaceY,
                    monsterWorld,
                    monsterSurfaceY))
            {
                continue;
            }

            Vector3 toMonster = health.transform.position - fromPosition;
            toMonster.y = 0f;
            float distanceSqr = toMonster.sqrMagnitude;
            if (distanceSqr < 0.0001f || distanceSqr > nearestSqr)
            {
                continue;
            }

            nearestSqr = distanceSqr;
            nearest = health;
        }

        if (nearest == null)
        {
            return false;
        }

        flatDirection = nearest.transform.position - fromPosition;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        flatDirection.Normalize();
        return true;
    }

    public static void CopyAliveSnapshot(List<MonsterHealth> destination)
    {
        destination.Clear();

        for (int i = AliveMonsters.Count - 1; i >= 0; i--)
        {
            MonsterHealth health = AliveMonsters[i];
            if (health == null || health.IsDead)
            {
                if (health != null)
                {
                    AliveMonsterSet.Remove(health);
                }

                AliveMonsters.RemoveAt(i);
                continue;
            }

            destination.Add(health);
        }
    }
}
