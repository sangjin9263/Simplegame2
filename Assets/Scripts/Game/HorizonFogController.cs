using UnityEngine;

// Lighting > Environment > Fog 와 동일하게 안개를 켜고, 하늘 지평선 색에 맞춥니다.
[ExecuteAlways]
public class HorizonFogController : MonoBehaviour
{
    // 안개를 켤지 여부입니다.
    [SerializeField] bool enableFog = true;

    // Linear 안개 모드 (Lighting 창과 같습니다).
    [SerializeField] FogMode fogMode = FogMode.Linear;

    // 안개 색 (하늘 지평선과 비슷하게 맞춥니다).
    [SerializeField] Color fogColor = new Color(0.42f, 0.24f, 0.20f, 1f);

    // 가까운 곳은 선명하게 보이는 거리입니다.
    [SerializeField] float fogStartDistance = 12f;

    // 멀리 갈수록 하늘색으로 사라지는 거리입니다 (넓은 맵 끝까지 안개).
    [SerializeField] float fogEndDistance = 90f;

    // 스카이박스 머티리얼 (비우면 RenderSettings.skybox 사용).
    [SerializeField] Material skyboxMaterial;

    // 스카이박스 Tint에서 안개 색을 자동으로 맞출지 여부입니다 (노을 하늘은 끄고 Fog Color 직접 지정).
    [SerializeField] bool syncFogColorFromSkybox = false;

    // Tint 기반 자동 색을 이 배율만큼 어둡게 해 지평선 느낌을 냅니다.
    [SerializeField] float skyboxColorDarken = 1.4f;

    // 환경광도 안개에 맞출지 여부입니다.
    [SerializeField] bool matchAmbientColors = true;

    // 켜질 때 적용합니다.
    void OnEnable()
    {
        ApplyFogSettings();
    }

    // 인스펙터 값 변경 시 바로 적용합니다.
    void OnValidate()
    {
        ApplyFogSettings();
    }

    // 플레이 중에도 안개가 꺼지지 않게 유지합니다.
    void Update()
    {
        if (!enableFog)
        {
            return;
        }

        if (!RenderSettings.fog)
        {
            ApplyFogSettings();
        }
    }

    // Lighting > Environment > Fog 설정을 RenderSettings에 넣습니다.
    [ContextMenu("Apply Fog Settings")]
    public void ApplyFogSettings()
    {
        if (syncFogColorFromSkybox)
        {
            SyncFogColorFromSkybox();
        }

        RenderSettings.fog = enableFog;
        if (!enableFog)
        {
            return;
        }

        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;

        if (matchAmbientColors)
        {
            RenderSettings.ambientSkyColor = Color.Lerp(fogColor, Color.white, 0.25f);
            RenderSettings.ambientEquatorColor = fogColor;
            RenderSettings.ambientGroundColor = Color.Lerp(fogColor * 0.6f, new Color(0.2f, 0.35f, 0.15f), 0.5f);
        }
    }

    // 스카이박스 Tint 색을 읽어 안개 색을 맞춥니다.
    [ContextMenu("Sync Fog Color From Skybox")]
    public void SyncFogColorFromSkybox()
    {
        Material sky = skyboxMaterial != null ? skyboxMaterial : RenderSettings.skybox;
        if (sky == null)
        {
            return;
        }

        if (sky.HasProperty("_Tint"))
        {
            Color tint = sky.GetColor("_Tint");
            float exposure = sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1f;
            fogColor = new Color(
                Mathf.Clamp01(tint.r * exposure / skyboxColorDarken),
                Mathf.Clamp01(tint.g * exposure / skyboxColorDarken),
                Mathf.Clamp01(tint.b * exposure / skyboxColorDarken),
                1f
            );
        }
    }
}
