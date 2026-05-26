using UnityEngine;

// 카메라 기준 좌우 반전을 방향이 바뀔 때만 적용합니다.
public static class SpumSpriteFlip
{
    const float MoveFlipThreshold = 0.01f;
    const float AttackFlipThreshold = 0.001f;

    public static void ApplyByMoveDirection(Transform flipTarget, Vector3 moveDirection, ref int lastFlipSide)
    {
        ApplyFacing(flipTarget, moveDirection, ref lastFlipSide, MoveFlipThreshold);
    }

    public static void ApplyByFacingDirection(Transform flipTarget, Vector3 facingDirection, ref int lastFlipSide)
    {
        ApplyFacing(flipTarget, facingDirection, ref lastFlipSide, AttackFlipThreshold);
    }

    static void ApplyFacing(
        Transform flipTarget,
        Vector3 direction,
        ref int lastFlipSide,
        float sideThreshold)
    {
        if (flipTarget == null || direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        float side = Vector3.Dot(direction.normalized, cameraRight);
        if (Mathf.Abs(side) < sideThreshold)
        {
            return;
        }

        int flipSide = side > 0f ? 1 : -1;
        if (flipSide == lastFlipSide)
        {
            return;
        }

        lastFlipSide = flipSide;

        if (flipSide > 0f)
        {
            flipTarget.localScale = new Vector3(1f, 1f, 1f);
        }
        else
        {
            flipTarget.localScale = new Vector3(-1f, 1f, 1f);
        }
    }
}
