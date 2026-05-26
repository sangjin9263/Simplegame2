using UnityEngine;
using UnityEngine.UI;

// 몬스터 머리 위 월드 HP 바입니다.
public class MonsterWorldHpBar : MonoBehaviour
{
    [SerializeField] float heightOffset = 1.15f;
    [SerializeField] Vector2 barWorldSize = new Vector2(0.7f, 0.07f);
    [SerializeField] Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    [SerializeField] Color fillColor = new Color(0.92f, 0.15f, 0.12f, 0.95f);

    RectTransform fillRect;
    Canvas canvas;

    static Sprite sharedWhiteSprite;

    public void Build(Transform followTarget)
    {
        if (fillRect != null)
        {
            return;
        }

        Sprite uiSprite = GetSharedWhiteSprite();

        GameObject canvasObject = new GameObject("HpBarCanvas");
        canvasObject.transform.SetParent(followTarget, false);
        canvasObject.transform.localPosition = new Vector3(0f, heightOffset, 0f);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        if (canvasObject.GetComponent<BillboardFaceCamera>() == null)
        {
            canvasObject.AddComponent<BillboardFaceCamera>();
        }

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = barWorldSize * 100f;
        canvasRect.localScale = Vector3.one * 0.01f;

        GameObject backgroundObject = CreateBarImage("HpBarBackground", canvasRect, uiSprite, backgroundColor);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        StretchFull(backgroundRect);

        GameObject fillObject = CreateBarImage("HpBarFill", canvasRect, uiSprite, fillColor);
        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0f, 0.5f);

        SetFill(1f);
    }

    static GameObject CreateBarImage(string objectName, RectTransform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        return imageObject;
    }

    static void StretchFull(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    public void SetFill(float normalized)
    {
        if (fillRect == null)
        {
            return;
        }

        normalized = Mathf.Clamp01(normalized);
        fillRect.anchorMax = new Vector2(normalized, 1f);
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null)
        {
            canvas.enabled = visible;
        }
    }

    static Sprite GetSharedWhiteSprite()
    {
        if (sharedWhiteSprite != null)
        {
            return sharedWhiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        sharedWhiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        sharedWhiteSprite.name = "MonsterHpBarWhite";

        return sharedWhiteSprite;
    }
}
