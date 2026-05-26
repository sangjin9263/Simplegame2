using System.Collections.Generic;
using UnityEngine;

// SPUM 걷기/대기 애니메이션을 상태가 바뀔 때만 재생합니다.
public class SpumLocomotionAnimation
{
    readonly SPUM_Prefabs spumPrefabs;
    PlayerState lastState = PlayerState.IDLE;
    bool ready;

    public bool IsReady => ready;

    public SpumLocomotionAnimation(SPUM_Prefabs prefabs)
    {
        spumPrefabs = prefabs;
    }

    public void Initialize()
    {
        if (spumPrefabs == null)
        {
            return;
        }

        if (!spumPrefabs.allListsHaveItemsExist())
        {
            spumPrefabs.PopulateAnimationLists();
        }

        spumPrefabs.OverrideControllerInit();
        ready = true;
        SetMoving(false);
    }

    // 걷는 중이면 MOVE, 아니면 IDLE을 재생합니다 (같은 상태면 건너뜁니다).
    public void SetMoving(bool moving)
    {
        if (!ready || spumPrefabs == null)
        {
            return;
        }

        PlayerState state = moving ? PlayerState.MOVE : PlayerState.IDLE;
        if (state == lastState)
        {
            return;
        }

        lastState = state;
        PlayState(state);
    }

    // 공격 후 등 애니메이션을 다시 맞출 때 사용합니다 (캐시 무시).
    public void ForceSetMoving(bool moving)
    {
        if (!ready || spumPrefabs == null)
        {
            return;
        }

        PlayerState state = moving ? PlayerState.MOVE : PlayerState.IDLE;
        lastState = state;
        PlayState(state);
    }

    void PlayState(PlayerState state)
    {
        string key = state.ToString();
        if (!spumPrefabs.StateAnimationPairs.ContainsKey(key))
        {
            if (state != PlayerState.IDLE)
            {
                PlayState(PlayerState.IDLE);
            }

            return;
        }

        List<AnimationClip> clips = spumPrefabs.StateAnimationPairs[key];
        if (clips == null || clips.Count == 0)
        {
            if (state != PlayerState.IDLE)
            {
                PlayState(PlayerState.IDLE);
            }

            return;
        }

        spumPrefabs.PlayAnimation(state, 0);
    }
}
