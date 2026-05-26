using System.IO;
using UnityEditor;
using UnityEngine;

// 미니맵 원형 스프라이트 생성·프리팹 적용.
public static class MinimapSpriteTools
{
    const string SpritePath = "Assets/UI/MinimapCircle.png";
    const string ResourcesPath = "Assets/Resources/UI/MinimapCircle.png";

    [MenuItem("Tools/Game/Generate Minimap Circle Sprite")]
    public static void GenerateCircleSprite()
    {
        EnsureCircleSpriteAsset();
        Debug.Log("[MinimapSpriteTools] 원형 스프라이트 생성: " + ResourcesPath);
    }

    [MenuItem("Tools/Game/Apply Circular Minimap To Prefab")]
    public static void ApplyCircularMinimapToPrefab()
    {
        EnsureCircleSpriteAsset();

        const string prefabPath = "Assets/Prefabs/UI/PlayerHudRoot.prefab";
        using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            Transform minimap = scope.prefabContentsRoot.transform.Find("Canvas/RadarMinimap");
            if (minimap == null)
            {
                Debug.LogWarning("[MinimapSpriteTools] RadarMinimap 없음.");
                return;
            }

            RadarMinimapView.ApplyCircleLayout(minimap);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[MinimapSpriteTools] PlayerHudRoot 미니맵 원형 적용 완료.");
    }

    public static Sprite EnsureCircleSpriteAsset()
    {
        string directory = Path.GetDirectoryName(ResourcesPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(ResourcesPath))
        {
            Texture2D texture = CreateCircleTexture(128);
            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(ResourcesPath, png);
            Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
        }

        TextureImporter importer = AssetImporter.GetAtPath(ResourcesPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ResourcesPath);
    }

    public static Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
