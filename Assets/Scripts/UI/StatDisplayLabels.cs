using System.Collections.Generic;

// CSV는 영어 stat_id만 쓰고, 화면 표시 문구는 여기서 한글로 매핑합니다.
public static class StatDisplayLabels
{
    static readonly Dictionary<string, string> KoreanByStatId = new Dictionary<string, string>
    {
        { "level", "LV" },
        { "hp", "체력" },
        { "mp", "마력" },
        { "exp", "경험치" },
    };

    public static string ToKoreanLabel(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId))
        {
            return string.Empty;
        }

        string key = statId.Trim().ToLowerInvariant();
        if (KoreanByStatId.TryGetValue(key, out string label))
        {
            return label;
        }

        return statId;
    }

    public static string FormatValueText(string statId, int current, int max)
    {
        return ToKoreanLabel(statId) + ": " + current + "/" + max;
    }
}
