using System;
using UnityEngine;

// 청크마다 추가 지형을 랜덤으로 깔 규칙입니다.
[Serializable]
public class ChunkTerrainFeatureRule
{
    public bool enabled = true;

    // 배치할 지형 프리팹입니다.
    public GameObject prefab;

    // 청크당 이 확률로 1개 스폰합니다.
    [Range(0f, 1f)]
    public float spawnChancePerChunk = 0.1f;

    // (0,0) 플레이어 시작 근처에는 두지 않습니다.
    public float playerSafeRadius = 18f;

    // 90° 단위로 Y 회전할지 여부입니다.
    public bool randomRotationY = true;

    // 지형 프리팹 루트 Y.
    public float positionY = 0f;
}
