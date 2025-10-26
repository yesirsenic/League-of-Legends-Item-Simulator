using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DPSCalculator
{
    private const float BaseSkillDamage = 300f;
    private const float ApRatio = 0.8f;
    private const float Cooldown = 5f;
    private const float CritMultiplier = 1.75f;

    public static float CalculateDPS(ChampionStats champ, ItemStats item, int level, float dummyArmor, float dummyMR)
    {
        // 기본 물리 / 마법 계산
        float adDps = CalculatePhysicalDPS(champ, item, level, dummyArmor);
        float apDps = CalculateMagicalDPS(item, dummyMR);

        // ✅ 피해 유형 보정 (champ.damageType 기반)
        float typeWeight = 1f;
        switch (champ.damageType.ToLower())
        {
            case "physical": typeWeight = 1f; break;
            case "magical": typeWeight = 0f; break;
            case "mixed": typeWeight = 0.5f; break;
            case "true": typeWeight = 0.8f; break;
        }

        // 혼합 결과 반환
        return adDps * typeWeight + apDps * (1 - typeWeight);
    }

    public static float CalculatePhysicalDPS(ChampionStats champ, ItemStats item, int level, float dummyArmor)
    {
        float totalAD = champ.GetAD(level) + item.ad;
        float totalAS = champ.GetAS(level) * (1 + item.aspeed);
        float armorMul = 100f / (100f + dummyArmor);
        float critMul = 1 + Mathf.Clamp01(item.crit) * (CritMultiplier - 1);
        return totalAD * totalAS * armorMul * critMul;
    }

    public static float CalculateMagicalDPS(ItemStats item, float dummyMR)
    {
        float totalAP = item.ap;
        float mrMul = 100f / (100f + dummyMR);
        float damage = (BaseSkillDamage + ApRatio * totalAP) * mrMul;
        return damage / Cooldown;
    }
}