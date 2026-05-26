using UnityEngine;

// 이전 별도 미니맵 루트 생성 — PlayerHudRoot로 통합됨. 호환용 래퍼만 유지합니다.
public static class RadarMinimapUiLayoutFactory
{
    public static GameObject CreateMinimapRoot()
    {
        BuiltHudCompat built = CreateHudCompat();
        return built.root;
    }

    struct BuiltHudCompat
    {
        public GameObject root;
    }

    static BuiltHudCompat CreateHudCompat()
    {
        PlayerHudUiLayoutFactory.BuiltHud built = PlayerHudUiLayoutFactory.CreateHudRoot();
        return new BuiltHudCompat { root = built.root };
    }
}
