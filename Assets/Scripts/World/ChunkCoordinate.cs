using UnityEngine;

// 청크 격자 좌표(X,Z)를 나타내는 구조체입니다.
public struct ChunkCoordinate : System.IEquatable<ChunkCoordinate>
{
    // 격자 X 번호입니다.
    public int x;

    // 격자 Z 번호입니다.
    public int z;

    // x, z로 좌표를 만듭니다.
    public ChunkCoordinate(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    // Vector2Int에서 변환합니다.
    public ChunkCoordinate(Vector2Int vector)
    {
        x = vector.x;
        z = vector.y;
    }

    // Vector2Int로 바꿉니다 (딕셔너리 키용).
    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, z);
    }

    // 월드 위치에서 청크 좌표를 계산합니다.
    public static ChunkCoordinate FromWorldPosition(Vector3 worldPosition, float chunkSize)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkSize);
        return new ChunkCoordinate(chunkX, chunkZ);
    }

    // 같은 좌표인지 비교합니다.
    public bool Equals(ChunkCoordinate other)
    {
        return x == other.x && z == other.z;
    }

    // object와 비교합니다.
    public override bool Equals(object obj)
    {
        return obj is ChunkCoordinate other && Equals(other);
    }

    // 해시 코드를 만듭니다.
    public override int GetHashCode()
    {
        return x * 397 ^ z;
    }

    // 문자열로 표시합니다 (디버그용).
    public override string ToString()
    {
        return $"({x}, {z})";
    }
}
