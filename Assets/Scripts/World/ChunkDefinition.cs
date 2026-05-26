using System.Collections.Generic;
using UnityEngine;

// 청크 한 칸에 무엇을 깔지 정의하는 ScriptableObject입니다.
[CreateAssetMenu(fileName = "ChunkDefinition", menuName = "Game/Chunk Definition")]
public class ChunkDefinition : ScriptableObject
{
    // 바닥으로 쓸 프리팹입니다 (Floor_Grass_Bright 등).
    public GameObject floorPrefab;

    // 바닥 오브젝트의 Y 위치입니다 (잔디 윗면이 0이 되게 -1.844 근처).
    public float floorPositionY = -1.843768f;

    // 수동으로 고정 위치에 놓을 오브젝트 목록입니다 (선택).
    public List<ChunkPropEntry> propEntries = new List<ChunkPropEntry>();

    // 청크마다 랜덤 개수·위치로 깔 규칙 목록입니다 (나무 등).
    public List<ChunkRandomPropRule> randomPropRules = new List<ChunkRandomPropRule>();
}

// 나중에 청크마다 배치할 오브젝트 한 종류 정보입니다.
[System.Serializable]
public class ChunkPropEntry
{
    // 배치할 프리팹입니다.
    public GameObject prefab;

    // 청크 바닥 기준 로컬 위치입니다.
    public Vector3 localPosition;

    // 청크 기준 로컬 회전(도)입니다.
    public Vector3 localEulerAngles;

    // 0~1, 이 값보다 작은 랜덤이면 스폰합니다 (1이면 항상).
    [Range(0f, 1f)]
    public float spawnChance = 1f;
}
