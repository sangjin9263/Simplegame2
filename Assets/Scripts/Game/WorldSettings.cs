using UnityEngine;

// 맵 공통 값(바닥 높이 등)을 한 곳에서 관리합니다.
[CreateAssetMenu(fileName = "WorldSettings", menuName = "Simplegame2/World Settings")]
public class WorldSettings : ScriptableObject
{
    const string ResourcesPath = "WorldSettings";

    [SerializeField] float defaultGroundY;
    [SerializeField] float characterFeetYOffset = 0.15f;

    static WorldSettings active;

    public float DefaultGroundY => defaultGroundY;

    public static WorldSettings Active
    {
        get
        {
            if (active != null)
            {
                return active;
            }

            active = Resources.Load<WorldSettings>(ResourcesPath);
            if (active == null)
            {
                active = CreateInstance<WorldSettings>();
            }

            return active;
        }
    }

    public static float GroundY => Active.DefaultGroundY;

    public static float CharacterFeetYOffset => Active.characterFeetYOffset;

    public static void SetActive(WorldSettings settings)
    {
        active = settings != null ? settings : Active;
    }
}
