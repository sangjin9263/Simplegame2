using UnityEngine;

[CreateAssetMenu(fileName = "WeaponVisualTuning", menuName = "Simplegame2/Weapon Visual Tuning Profile")]
public class WeaponVisualTuningProfile : ScriptableObject
{
    public WeaponVisualTuningSnapshot snapshot;

    void OnEnable()
    {
        if (snapshot == null)
        {
            snapshot = WeaponVisualTuningSnapshot.CreateDefault(WeaponVisualKind.Melee);
        }
    }
}
