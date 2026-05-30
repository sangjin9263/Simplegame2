#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// monster_default.csv 한글/컬럼 스키마를 UTF-8 BOM으로 재생성합니다.
public static class MonsterDefaultCsvRebuild
{
    [MenuItem("Simplegame2/Rebuild Monster Default CSV")]
    public static void Rebuild()
    {
        string csv = BuildCsvContent();
        var utf8Bom = new UTF8Encoding(true);
        File.WriteAllText(MonsterDefinitionTable.EditorCsvPath, csv, utf8Bom);
        MonsterDefinitionTable.SyncToResources();
        AssetDatabase.Refresh();
        Debug.Log("[MonsterDefaultCsvRebuild] monster_default.csv 재생성 완료 (UTF-8 BOM)");
    }

    static string BuildCsvContent()
    {
        const string orc = "\uC624\uD06C";
        const string melee = "\uADFC\uC811";
        const string ranged = "\uC6D0\uAC70\uB9AC";
        const string mage = "\uC8FC\uC220\uC0AC";
        const string monsterData = "\uBAAC\uC2A4\uD130 \uB370\uC774\uD130";
        const string weapon = "\uBB34\uAE30";
        const string and = "\uC640";
        const string separate = "\uAD6C\uBD84";
        const string meleeKo = "\uADFC\uC811";
        const string rangedKo = "\uC6D0\uAC70\uB9AC";
        const string magicKo = "\uB9C8\uBC95";
        const string bossKo = "\uBCF4\uC2A4";
        const string extraJson = "\uD22C\uC0AC\uCCB4 VFX \uB4F1 \uBD80\uAC00 \uD29C\uB2DD";

        string[] lines =
        {
            "# monster_default.csv - " + monsterData + " (Mon_ID: 2001+, " + weapon + " 3001+" + and + " " + separate + ")",
            "# Mon_Type: 1=" + meleeKo + "(Melee) 2=" + rangedKo + "(Ranged) 3=" + magicKo + "(Mage) 4=" + bossKo + "(Boss)",
            "#",
            "# mon_projectile / mon_hit_impact : type 2,3(" + rangedKo + "," + magicKo + ") \uC704\uC8FC. " + meleeKo + "\uC740 mon_hit_impact\uB9CC",
            "# mon_atk_range / mon_cooldown / mon_anim_duration / mon_fire_delay_N / stop_distance : \uACF5\uACA9 \uAE30\uBCF8 \uD29C\uB2DD (\uADDC\uC811\uC740 mon_fire_delay_N = Damage Apply N)",
            "# " + extraJson + ": Assets/Data/MonsterTuning/Monster_2001.json",
            "#",
            "mon_id,mon_name,mon_type,mon_level,mon_hp,mon_mp,mon_mp_regen,mon_dmg,mon_speed,give_exp,mon_atk_range,mon_cooldown,mon_anim_duration,mon_fire_delay_N,stop_distance,mon_prefab,mon_projectile,mon_hit_impact",
            "2001," + orc + " " + melee + "1,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m1,,Impact_Hit_Lv1",
            "2002," + orc + " " + melee + "2,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m2,,Impact_Hit_Lv1",
            "2003," + orc + " " + melee + "3,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m3,,Impact_Hit_Lv1",
            "2004," + orc + " " + melee + "4,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m4,,Impact_Hit_Lv1",
            "2005," + orc + " " + melee + "5,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m5,,Impact_Hit_Lv1",
            "2006," + orc + " " + melee + "6,1,1,10,0,0,1,3.5,10,1.2,1.1,0.45,0.4,0.9,SPUM_orc_m6,,Impact_Hit_Lv1",
            "2007," + orc + " " + ranged + "1,2,1,10,0,0,1,3.5,10,4.3,1.35,0.45,0.35,4,SPUM_orc_m7,Arrow01,Impact_Hit_Lv1",
            "2008," + orc + " " + ranged + "2,2,1,10,0,0,1,3.5,10,4.3,1.35,0.45,0.35,4,SPUM_orc_m8,Arrow01,Impact_Hit_Lv1",
            "2009," + orc + " " + mage + "1,3,1,10,10,2,1,3.5,10,7.3,1.35,0.45,0.35,7,SPUM_orc_m9,Projectile_Energy_Ball_B,Impact_Energy_Ball"
        };

        return string.Join("\r\n", lines) + "\r\n";
    }
}
#endif
