using UnityEngine;

public static class WeaponVisualTuningApplier
{
    public static bool TryReadFromPlayer(GameObject player, WeaponVisualKind kind, out WeaponVisualTuningSnapshot snapshot)
    {
        snapshot = null;
        if (player == null)
        {
            return false;
        }

        switch (kind)
        {
            case WeaponVisualKind.Melee:
                PlayerWeaponCombat melee = player.GetComponent<PlayerWeaponCombat>();
                if (melee == null)
                {
                    return false;
                }

                snapshot = melee.ExportVisualTuning();
                return true;

            case WeaponVisualKind.Ranged:
                PlayerRangedCombat ranged = player.GetComponent<PlayerRangedCombat>();
                if (ranged == null)
                {
                    return false;
                }

                snapshot = ranged.ExportVisualTuning();
                return true;

            case WeaponVisualKind.Magic:
                PlayerMagicCombat magic = player.GetComponent<PlayerMagicCombat>();
                if (magic == null)
                {
                    return false;
                }

                snapshot = magic.ExportVisualTuning();
                return true;

            default:
                return false;
        }
    }

    public static bool TryApplyToPlayer(GameObject player, WeaponVisualTuningSnapshot snapshot)
    {
        if (player == null || snapshot == null)
        {
            return false;
        }

        switch (snapshot.kind)
        {
            case WeaponVisualKind.Melee:
                PlayerWeaponCombat melee = player.GetComponent<PlayerWeaponCombat>();
                if (melee == null)
                {
                    return false;
                }

                melee.ApplyVisualTuning(snapshot);
                return true;

            case WeaponVisualKind.Ranged:
                PlayerRangedCombat ranged = player.GetComponent<PlayerRangedCombat>();
                if (ranged == null)
                {
                    return false;
                }

                ranged.ApplyVisualTuning(snapshot);
                return true;

            case WeaponVisualKind.Magic:
                PlayerMagicCombat magic = player.GetComponent<PlayerMagicCombat>();
                if (magic == null)
                {
                    return false;
                }

                magic.ApplyVisualTuning(snapshot);
                return true;

            default:
                return false;
        }
    }

    public static bool TryRequestTestAttack(GameObject player, WeaponVisualKind kind)
    {
        if (player == null)
        {
            return false;
        }

        switch (kind)
        {
            case WeaponVisualKind.Melee:
                PlayerWeaponCombat melee = player.GetComponent<PlayerWeaponCombat>();
                return melee != null && melee.RequestTestAttack();

            case WeaponVisualKind.Ranged:
                PlayerRangedCombat ranged = player.GetComponent<PlayerRangedCombat>();
                return ranged != null && ranged.RequestTestAttack();

            case WeaponVisualKind.Magic:
                PlayerMagicCombat magic = player.GetComponent<PlayerMagicCombat>();
                return magic != null && magic.RequestTestAttack();

            default:
                return false;
        }
    }
}
