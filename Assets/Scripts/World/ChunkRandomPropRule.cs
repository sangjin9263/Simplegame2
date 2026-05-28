using System;
using UnityEngine;

// 청크 한 칸마다 프리팹을 랜덤 개수·위치로 깔 규칙입니다.
[Serializable]
public class ChunkRandomPropRule
{
    // 이 규칙을 사용할지 여부입니다.
    public bool enabled = true;

    // 이 중 하나를 랜덤으로 골라 배치합니다 (PineTree_Bright, PineTree_Dark 등).
    public GameObject[] prefabs;

    // 청크당 최소 개수입니다.
    public int minCountPerChunk = 1;

    // 청크당 최대 개수입니다.
    public int maxCountPerChunk = 4;

    // 청크 가장자리에서 안쪽으로 띄울 거리입니다.
    public float edgePadding = 0.8f;

    // 오브젝트끼리 최소 거리입니다 (겹침 방지, 청크 경계 포함).
    public float minDistanceBetweenProps = 2.2f;

    // 청크 바닥 루트 기준 Y 오프셋입니다 (나무는 보통 ChunkDefinition.GrassSurfaceHeight).
    public float positionY = ChunkDefinition.GrassSurfaceHeight;

    // Y축으로만 랜덤 회전할지 여부입니다.
    public bool randomRotationY = true;

    // 위치를 못 찾을 때 재시도 횟수(나무 1그루당)입니다.
    public int maxPlacementAttempts = 10;

    // (0,0) 플레이어 주변 이 반경(미터) 안에는 나무를 안 깝니다.
    public float playerSafeRadius = 5f;

    // Slope 바닥 주변 이 반경(미터) 안에는 나무를 안 깝니다.
    public float slopeAvoidRadius = 45f;

    // Main_land 와 High_land 경계 주변 이 반경(미터) 안에는 나무를 안 깝니다.
    public float mainHighBoundaryAvoidRadius = 2.4f;
}
