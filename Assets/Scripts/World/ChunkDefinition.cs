using System.Collections.Generic;
using UnityEngine;

// 청크 한 칸에 무엇을 깔지 정의하는 ScriptableObject입니다.
[CreateAssetMenu(fileName = "ChunkDefinition", menuName = "Game/Chunk Definition")]
public class ChunkDefinition : ScriptableObject
{
    // 바닥으로 쓸 프리팹입니다 (구버전 호환).
    public GameObject floorPrefab;

    // Floor 루트 Y (구버전 호환).
    public float floorPositionY = 0f;

    // 청크마다 랜덤으로 고를 바닥 종류입니다 (Main_land 등).
    public List<ChunkFloorVariant> floorVariants = new List<ChunkFloorVariant>();

    // 잔디 타일 프리팹 루트에서 잔디 윗면까지의 높이입니다.
    public const float GrassSurfaceHeight = 1.843768f;

    // 수동으로 고정 위치에 놓을 오브젝트 목록입니다 (선택).
    public List<ChunkPropEntry> propEntries = new List<ChunkPropEntry>();

    // 청크마다 랜덤 개수·위치로 깔 규칙 목록입니다 (나무 등).
    public List<ChunkRandomPropRule> randomPropRules = new List<ChunkRandomPropRule>();

    // 청크마다 추가 지형 오브젝트를 배치하는 규칙입니다.
    public List<ChunkTerrainFeatureRule> terrainFeatureRules = new List<ChunkTerrainFeatureRule>();

    // 청크 좌표에 맞는 바닥(평지 / 경사)을 고릅니다.
    public bool TryPickFloorVariant(ChunkCoordinate coordinate, out ChunkFloorVariant picked)
    {
        if (ChunkFloorLayout.TryPickFloorVariant(this, coordinate, out picked))
        {
            return true;
        }

        if (floorPrefab != null)
        {
            picked = new ChunkFloorVariant
            {
                prefab = floorPrefab,
                positionY = floorPositionY,
                isSlope = false
            };
            return true;
        }

        picked = default;
        return false;
    }

    public IEnumerable<GameObject> EnumerateFloorPrefabsForPool()
    {
        HashSet<GameObject> unique = new HashSet<GameObject>();

        if (floorVariants != null)
        {
            for (int i = 0; i < floorVariants.Count; i++)
            {
                GameObject prefab = floorVariants[i].prefab;
                if (prefab != null && unique.Add(prefab))
                {
                    yield return prefab;
                }
            }
        }

        if (floorPrefab != null && unique.Add(floorPrefab))
        {
            yield return floorPrefab;
        }
    }
}

// 청크 바닥 한 종류(프리팹 + 높이 + 등장 비율)입니다.
[System.Serializable]
public class ChunkFloorVariant
{
    public GameObject prefab;
    public float positionY;
    public bool isSlope;

    // 같은 청크 종류 안에서 이 타일이 나올 가중치입니다 (Main 5 : Slope 3 : High 2 등).
    [Min(0)]
    public int spawnWeight = 1;
}

// 나중에 청크마다 배치할 오브젝트 한 종류 정보입니다.
[System.Serializable]
public class ChunkPropEntry
{
    public GameObject prefab;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;

    [Range(0f, 1f)]
    public float spawnChance = 1f;
}

// 청크 좌표마다 floorVariants 가중치로 바닥 타일을 고릅니다.
public static class ChunkFloorLayout
{
    const float HighCoreChance = 0.012f;
    const float ExtraSlopeChance = 0.7f;
    const int SaltHighCore = 0x51A7E1;
    const int SaltHighRing = 0x3C91D7;

    static readonly Vector2Int[] CardinalOffsets =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    public static bool TryPickFloorVariant(ChunkDefinition definition, ChunkCoordinate coordinate, out ChunkFloorVariant picked)
    {
        picked = default;

        if (definition == null)
        {
            return false;
        }

        if (definition.floorVariants != null && definition.floorVariants.Count > 0)
        {
            if (TryPickConnectedTerrainVariant(definition.floorVariants, coordinate, out picked))
            {
                return true;
            }

            if (TryPickWeightedVariant(definition.floorVariants, coordinate, out picked))
            {
                return true;
            }
        }

        if (!TryGetMainLandPrefab(definition, out GameObject mainLandPrefab))
        {
            return false;
        }

        picked = new ChunkFloorVariant
        {
            prefab = mainLandPrefab,
            positionY = 0f,
            isSlope = false,
            spawnWeight = 1
        };
        return true;
    }

    public static bool IsMainLandPrefab(GameObject prefab)
    {
        return prefab != null && prefab.name.Contains("Main_land");
    }

    public static bool IsHighLandPrefab(GameObject prefab)
    {
        return prefab != null && prefab.name.Contains("High_land");
    }

    public static bool IsSlopePrefab(GameObject prefab)
    {
        return prefab != null && prefab.name.Contains("Slope");
    }

    static bool TryPickWeightedVariant(
        List<ChunkFloorVariant> variants,
        ChunkCoordinate coordinate,
        out ChunkFloorVariant picked)
    {
        picked = default;
        int totalWeight = 0;

        for (int i = 0; i < variants.Count; i++)
        {
            ChunkFloorVariant variant = variants[i];
            if (variant.prefab == null || variant.spawnWeight <= 0)
            {
                continue;
            }

            totalWeight += variant.spawnWeight;
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int seed = coordinate.x * 73856093 ^ coordinate.z * 19349663 ^ 0x4C414E44;
        Random.State oldState = Random.state;
        Random.InitState(seed);

        int roll = Random.Range(0, totalWeight);
        Random.state = oldState;

        int cumulative = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            ChunkFloorVariant variant = variants[i];
            if (variant.prefab == null || variant.spawnWeight <= 0)
            {
                continue;
            }

            cumulative += variant.spawnWeight;
            if (roll < cumulative)
            {
                picked = new ChunkFloorVariant
                {
                    prefab = variant.prefab,
                    positionY = variant.positionY,
                    isSlope = variant.isSlope || IsSlopePrefab(variant.prefab),
                    spawnWeight = variant.spawnWeight
                };
                return true;
            }
        }

        return false;
    }

    static bool TryPickConnectedTerrainVariant(
        List<ChunkFloorVariant> variants,
        ChunkCoordinate coordinate,
        out ChunkFloorVariant picked)
    {
        picked = default;

        if (!TryPickVariantByName(variants, IsMainLandPrefab, coordinate, SaltHighCore ^ 0x101, out ChunkFloorVariant mainVariant)
            || !TryPickVariantByName(variants, IsSlopePrefab, coordinate, SaltHighCore ^ 0x202, out ChunkFloorVariant slopeVariant)
            || !TryPickVariantByName(variants, IsHighLandPrefab, coordinate, SaltHighCore ^ 0x303, out ChunkFloorVariant highVariant))
        {
            return false;
        }

        // High는 중심 타일만 뽑고, 주변은 slope로 연결되게 만듭니다.
        if (IsHighCore(coordinate))
        {
            picked = CopyVariant(highVariant);
            return true;
        }

        if (ShouldPlaceSlopeForHighConnection(coordinate))
        {
            picked = CopyVariant(slopeVariant);
            picked.isSlope = true;
            return true;
        }

        picked = CopyVariant(mainVariant);
        return true;
    }

    static bool ShouldPlaceSlopeForHighConnection(ChunkCoordinate coordinate)
    {
        bool hasAdjacentHigh = false;

        for (int i = 0; i < CardinalOffsets.Length; i++)
        {
            Vector2Int offset = CardinalOffsets[i];
            ChunkCoordinate neighbor = new ChunkCoordinate(coordinate.x + offset.x, coordinate.z + offset.y);
            if (!IsHighCore(neighbor))
            {
                continue;
            }

            hasAdjacentHigh = true;

            // 각 High 타일마다 최소 1개 방향은 무조건 slope를 보장합니다.
            int guaranteedDirection = GetDirectionHash(neighbor, SaltHighRing) & 3;
            int currentDirection = GetDirectionIndexFromOffset(-offset.x, -offset.y);
            if (currentDirection == guaranteedDirection)
            {
                return true;
            }
        }

        if (!hasAdjacentHigh)
        {
            return false;
        }

        // 추가 slope를 확률로 열어 자연스럽게 연결 폭을 만듭니다.
        return GetChance(coordinate, SaltHighRing) < ExtraSlopeChance;
    }

    static int GetDirectionIndexFromOffset(int x, int z)
    {
        if (x > 0)
        {
            return 0;
        }

        if (x < 0)
        {
            return 1;
        }

        if (z > 0)
        {
            return 2;
        }

        return 3;
    }

    static bool IsHighCore(ChunkCoordinate coordinate)
    {
        return GetChance(coordinate, SaltHighCore) < HighCoreChance;
    }

    static float GetChance(ChunkCoordinate coordinate, int salt)
    {
        uint hash = (uint)(coordinate.x * 73856093 ^ coordinate.z * 19349663 ^ salt);
        hash ^= hash >> 16;
        hash *= 0x7feb352d;
        hash ^= hash >> 15;
        hash *= 0x846ca68b;
        hash ^= hash >> 16;
        return (hash & 0x00FFFFFF) / 16777215f;
    }

    static int GetDirectionHash(ChunkCoordinate coordinate, int salt)
    {
        float chance = GetChance(coordinate, salt);
        return Mathf.FloorToInt(chance * 1024f);
    }

    static bool TryPickVariantByName(
        List<ChunkFloorVariant> variants,
        System.Func<GameObject, bool> matcher,
        ChunkCoordinate coordinate,
        int salt,
        out ChunkFloorVariant variant)
    {
        variant = default;
        if (variants == null || matcher == null)
        {
            return false;
        }

        List<ChunkFloorVariant> matched = new List<ChunkFloorVariant>();

        for (int i = 0; i < variants.Count; i++)
        {
            ChunkFloorVariant candidate = variants[i];
            if (candidate.prefab == null || !matcher(candidate.prefab))
            {
                continue;
            }

            matched.Add(candidate);
        }

        if (matched.Count == 0)
        {
            return false;
        }

        int pickIndex = Mathf.Clamp(
            Mathf.FloorToInt(GetChance(coordinate, salt) * matched.Count),
            0,
            matched.Count - 1);
        variant = matched[pickIndex];
        return true;
    }

    static ChunkFloorVariant CopyVariant(ChunkFloorVariant source)
    {
        return new ChunkFloorVariant
        {
            prefab = source.prefab,
            positionY = source.positionY,
            isSlope = source.isSlope || IsSlopePrefab(source.prefab),
            spawnWeight = source.spawnWeight
        };
    }

    static bool TryGetMainLandPrefab(ChunkDefinition definition, out GameObject mainLand)
    {
        mainLand = null;

        if (definition.floorVariants != null)
        {
            for (int i = 0; i < definition.floorVariants.Count; i++)
            {
                GameObject prefab = definition.floorVariants[i].prefab;
                if (prefab != null && IsMainLandPrefab(prefab))
                {
                    mainLand = prefab;
                    return true;
                }
            }
        }

        if (definition.floorPrefab != null && IsMainLandPrefab(definition.floorPrefab))
        {
            mainLand = definition.floorPrefab;
            return true;
        }

#if UNITY_EDITOR
        if (mainLand == null)
        {
            mainLand = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Land/Main_land.prefab");
        }
#endif

        return mainLand != null;
    }
}
