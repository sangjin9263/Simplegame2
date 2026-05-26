using UnityEngine;

// 메인 카메라에 나무 시야 페이드 컨트롤러를 붙입니다.
public static class TreeOcclusionFadeInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallOnMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (mainCamera.GetComponent<TreeOcclusionFadeController>() != null)
        {
            return;
        }

        mainCamera.gameObject.AddComponent<TreeOcclusionFadeController>();
    }
}
