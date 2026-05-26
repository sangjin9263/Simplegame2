using System.Collections;
using UnityEngine;

// 월드(바닥 청크) 로딩이 끝난 뒤 게임플레이를 시작하기 위한 공용 신호입니다.
public static class WorldLoadCoordinator
{
    public static bool IsWorldReady { get; private set; }

    public static void ResetForPlay()
    {
        IsWorldReady = false;
    }

    public static void NotifyWorldReady()
    {
        IsWorldReady = true;
    }

    public static IEnumerator WaitUntilWorldReady()
    {
        while (!IsWorldReady)
        {
            yield return null;
        }
    }
}
