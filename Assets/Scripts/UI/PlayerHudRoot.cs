using UnityEngine;

// PlayerHudRoot 프리팹 루트. UI는 프리팹/씬에 미리 배치해 두고, 런타임에는 생성하지 않습니다.
[DisallowMultipleComponent]
public class PlayerHudRoot : MonoBehaviour
{
    void Awake()
    {
        EnsureLayout();
    }

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

        RemoveDeprecatedHudElements(canvasRect);

        if (GetComponentInChildren<RadarMinimapView>(true) == null)
        {
            PlayerHudUiLayoutFactory.BuildRadarMinimap(canvasRect);
        }

        if (canvasRect.Find("TopLeftSessionHud") == null)
        {
            PlayerHudUiLayoutFactory.BuildSessionHud(canvasRect);
        }
    }

    static void RemoveDeprecatedHudElements(RectTransform canvasRect)
    {
        DestroyUiChild(canvasRect.Find("InventoryPanel"));
        DestroyUiChild(canvasRect.Find("InventoryToggle"));
        DestroyUiChild(canvasRect.Find("AimCursor"));
    }

    static void DestroyUiChild(Transform child)
    {
        if (child == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(child.gameObject);
            return;
        }

#if UNITY_EDITOR
        DestroyImmediate(child.gameObject);
#endif
    }
}
