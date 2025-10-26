using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class ChampionDatabase : MonoBehaviour
{
    public string championCSVFile = "lol_champion_stats_14.20.1.csv";

    private List<ChampionStats> champions = new List<ChampionStats>();

    void Awake()
    {
        LoadCSV();
    }

    void LoadCSV()
    {
        string path = Path.Combine(Application.streamingAssetsPath, championCSVFile);
        if (!File.Exists(path))
        {
            Debug.LogError("❌ 챔피언 CSV 파일을 찾을 수 없습니다: " + path);
            return;
        }

        byte[] rawBytes = File.ReadAllBytes(path);
        bool hasBom = rawBytes.Length >= 3 &&
                      rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF;
        Encoding encodingToUse = hasBom ? Encoding.UTF8 : Encoding.GetEncoding(949);
        string csvText = encodingToUse.GetString(rawBytes).TrimStart('\uFEFF', '\u200B', '\u0000');

        Debug.Log($"[INFO] 인코딩 감지: {(hasBom ? "UTF-8(BOM)" : "CP949")}");

        var lines = csvText.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV 데이터가 비어 있습니다.");
            return;
        }

        char delim = lines[0].Contains('\t') ? '\t' : ',';
        var csvSplit = new Regex($"{delim}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        string[] rawHeaders = csvSplit.Split(lines[0]);
        string[] headers = rawHeaders.Select(CleanHeader).ToArray();

        // 주요 열 인덱스 전부 찾기
        int nameIndex = Find(headers, "champion_name");
        int enIndex = Find(headers, "champion_en");
        int roleIndex = Find(headers, "role");
        int dmgTypeIndex = Find(headers, "damage_type");
        int hpBase = Find(headers, "hp_base");
        int hpGrowth = Find(headers, "hp_growth");
        int adBase = Find(headers, "ad_base");
        int adGrowth = Find(headers, "ad_growth");
        int asBase = Find(headers, "as_base");
        int asGrowth = Find(headers, "as_growth");
        int armorBase = Find(headers, "armor_base");
        int armorGrowth = Find(headers, "armor_growth");
        int mrBase = Find(headers, "mr_base");
        int mrGrowth = Find(headers, "mr_growth");
        int hpRegenBase = Find(headers, "hp_regen_base");
        int hpRegenGrowth = Find(headers, "hp_regen_growth");
        int msBase = Find(headers, "ms_base");

        champions.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] cols = csvSplit.Split(line);
            if (cols.Length <= System.Math.Max(nameIndex, enIndex)) continue;

            ChampionStats c = new ChampionStats();
            c.name = CleanValue(cols[nameIndex]);
            c.championEN = CleanValue(cols[enIndex]);
            c.role = CleanValue(cols[roleIndex]);
            c.damageType = CleanValue(cols[dmgTypeIndex]);

            // --- 기본 스탯 ---
            c.hp_base = ParseF(cols, hpBase);
            c.hp_regen_base = ParseF(cols, hpRegenBase);
            c.ad_base = ParseF(cols, adBase);
            c.as_base = ParseF(cols, asBase);
            c.armor_base = ParseF(cols, armorBase);
            c.mr_base = ParseF(cols, mrBase);
            c.ms_base = ParseF(cols, msBase);

            // --- 성장 스탯 ---
            c.hp_growth = ParseF(cols, hpGrowth);
            c.hp_regen_growth = ParseF(cols, hpRegenGrowth);
            c.ad_growth = ParseF(cols, adGrowth);
            c.as_growth = ParseF(cols, asGrowth);
            c.armor_growth = ParseF(cols, armorGrowth);
            c.mr_growth = ParseF(cols, mrGrowth);

            if (!string.IsNullOrEmpty(c.name))
                champions.Add(c);
        }

        Debug.Log($"✅ 챔피언 CSV 로드 완료 ({champions.Count}명)");
    }

    string CleanHeader(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\"", "").Replace("\r", "").Replace("\n", "").Trim().ToLowerInvariant();
    }
    string CleanValue(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\"", "").Replace("\r", "").Replace("\n", "").Trim();
    }
    int Find(string[] headers, string key)
    {
        for (int i = 0; i < headers.Length; i++)
            if (headers[i] == key) return i;
        return -1;
    }
    float ParseF(string[] cols, int idx)
    {
        if (idx < 0 || idx >= cols.Length) return 0f;
        float.TryParse(CleanValue(cols[idx]), NumberStyles.Any, CultureInfo.InvariantCulture, out float v);
        return v;
    }

    public ChampionStats GetChampion(string name)
    {
        return champions.Find(c => c.name == name || c.championEN == name);
    }

    public List<string> GetChampionNames(bool english = false)
    {
        List<string> names = new List<string>();
        foreach (var c in champions)
        {
            if (!string.IsNullOrEmpty(c.name))
                names.Add(english ? c.championEN : c.name);
        }
        return names;
    }
}