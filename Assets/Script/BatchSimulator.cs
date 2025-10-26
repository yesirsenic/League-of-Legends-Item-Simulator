using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BatchSimulator : MonoBehaviour
{
    public ChampionDatabase champDB;
    public ItemDatabase itemDB;

    public void RunBatch(ChampionStats champ, int level, float armor, float mr, float physMix, bool robust)
    {
        if (champ == null)
        {
            Debug.LogError("❌ 챔피언이 null 입니다.");
            return;
        }

        Debug.Log($"[DEBUG] Champion: {champ.name} ({champ.role} / {champ.damageType})");

        RoleWeight w = RoleWeights.Get(ParseRole(champ.role));
        float typeWeight = ParseDamageType(champ.damageType);

        // ✅ 결과 리스트
        List<(string Champ, string Category, string Item, float AD, float AP, float EHP, float TTK, float Sustain, float Comp)> results = new();

        foreach (var item in itemDB.Items)
        {
            float adDps = DPSCalculator.CalculatePhysicalDPS(champ, item, level, armor);
            float apDps = DPSCalculator.CalculateMagicalDPS(item, mr);

            float hp = champ.GetHP(level) + item.hp;
            float totalArmor = champ.GetArmor(level) + item.armor;
            float totalMR = champ.GetMR(level) + item.mr;

            BalanceMetrics.CalcEHPs(hp, totalArmor, totalMR, out float ehpPhys, out float ehpMag, out float ehpMix, physMix);
            BalanceMetrics.CalcTTKs(ehpPhys, ehpMag, ehpMix, out float ttkPhys, out float ttkMag, out float ttkMix);

            float ehp = robust ? Mathf.Min(ehpPhys, ehpMag) : ehpMix;
            float ttk = robust ? Mathf.Min(ttkPhys, ttkMag) : ttkMix;

            BalanceMetrics.CalcSustain(champ.GetHPRegen(level), item.hp_regen, item.lifesteal, adDps,
                out float regenHPS, out float vampPS, out float sustainCombined);

            float comp = w.adDps * (adDps / 500f)
                       + w.apDps * (apDps / 150f)
                       + w.ehp * (ehp / 4000f)
                       + w.ttk * Mathf.Clamp01(ttk / 10f)
                       + w.sustain * Mathf.Clamp01(sustainCombined / 20f);

            results.Add((champ.name, item.itemCategory, item.name, adDps, apDps, ehp, ttk, sustainCombined, comp));
        }

        // ✅ 정렬 (카테고리별 그룹 → 점수 내림차순)
        results = results
            .OrderBy(r => r.Category)
            .ThenByDescending(r => r.Comp)
            .ToList();

        // ✅ CSV 생성
        var sb = new StringBuilder();
        sb.AppendLine("Champion,Category,Item,AD_DPS,AP_DPS,EHP,TTK,Sustain,Composite");

        foreach (var r in results)
            sb.AppendLine($"{r.Champ},{r.Category},{r.Item},{r.AD:F1},{r.AP:F1},{r.EHP:F0},{r.TTK:F1},{r.Sustain:F2},{r.Comp:F3}");

        string path = Path.Combine(Application.dataPath, "Results/SimulationResults_Sorted.csv");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"✅ 정렬된 CSV 저장 완료 → {path}");



#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    // ✅ 역할군 문자열을 RoleType으로 변환
    RoleType ParseRole(string role)
    {
        if (string.IsNullOrEmpty(role)) return RoleType.Bruiser;
        if (System.Enum.TryParse(role, true, out RoleType r)) return r;
        return RoleType.Bruiser;
    }

    // ✅ 피해유형을 비율(0~1)로 변환
    float ParseDamageType(string dmg)
    {
        if (string.IsNullOrEmpty(dmg)) return 1f;
        switch (dmg.ToLower())
        {
            case "physical": return 1f;
            case "magical": return 0f;
            case "mixed": return 0.5f;
            case "true": return 0.8f; // 고정 피해는 AD기반에 가깝게 처리
            default: return 1f;
        }
    }
}
