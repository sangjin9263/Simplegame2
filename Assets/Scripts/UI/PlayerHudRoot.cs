using UnityEngine;

// PlayerHudRoot 프리팹 루트. UI는 프리팹/씬에 미리 배치해 두고, 런타임에는 생성하지 않습니다.
[DisallowMultipleComponent]
public class PlayerHudRoot : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Bake Missing HUD Layout (Editor Only)")]
    void BakeMissingLayoutInEditor()
    {
        EnsureLayout();
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
#endif

    public void EnsureLayout()
    {
        RectTransform canvasRect = transform.Find("Canvas") as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (GetComponentInChildren<RadarMinimapView>(true) == null)
        {
            PlayerHudUiLayoutFactory.BuildRadarMinimap(canvasRect);
        }

        if (canvasRect.Find("TopLeftSessionHud") == null)
        {
            PlayerHudUiLayoutFactory.BuildSessionHud(canvasRect);
        }

        if (canvasRect.Find("InventoryPanel") == null)
        {
            PlayerHudUiLayoutFactory.BuildInventoryPlaceholders(canvasRect);
        }
    }
}
