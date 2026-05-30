using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Weapon_test 무기 선택 드롭다운 UI 생성.
public static class WeaponTestUiFactory
{
    public const string CanvasName = "WeaponTestUI";

    public static Canvas FindOrCreateCanvas(int sortingOrder = 400)
    {
        EnsureEventSystem();

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.name == CanvasName)
            {
                canvases[i].sortingOrder = sortingOrder;
                return canvases[i];
            }
        }

        GameObject canvasObject = new GameObject(CanvasName);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static Dropdown CreateWeaponDropdown(Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        DefaultControls.Resources resources = new DefaultControls.Resources();
        GameObject dropdownObject = DefaultControls.CreateDropdown(resources);
        dropdownObject.name = "WeaponDropdown";

        RectTransform rectTransform = dropdownObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        SetTopRightLayout(rectTransform, anchoredPosition, size);

        return dropdownObject.GetComponent<Dropdown>();
    }

    static void SetTopRightLayout(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
