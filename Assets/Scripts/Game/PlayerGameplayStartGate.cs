using System.Collections;
using UnityEngine;

// 바닥 청크 로딩이 끝날 때까지 플레이어 이동·표시를 잠급니다.
[DefaultExecutionOrder(-80)]
public class PlayerGameplayStartGate : MonoBehaviour
{
    [SerializeField] bool hideVisualUntilWorldReady = true;

    PlayerMovement playerMovement;
    Renderer[] cachedRenderers;
    bool isLocked;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (hideVisualUntilWorldReady)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    void OnEnable()
    {
        if (!WorldLoadCoordinator.IsWorldReady)
        {
            ApplyLockedState(true);
            StartCoroutine(WaitForWorldReady());
        }
    }

    IEnumerator WaitForWorldReady()
    {
        yield return WorldLoadCoordinator.WaitUntilWorldReady();
        ApplyLockedState(false);
    }

    void ApplyLockedState(bool locked)
    {
        isLocked = locked;

        if (playerMovement != null)
        {
            playerMovement.enabled = !locked;
        }

        if (!hideVisualUntilWorldReady || cachedRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].enabled = !locked;
            }
        }
    }
}
