using System.Linq;
using UnityEngine;

public static class BalanceMetrics
{
    // --- 유효 체력 ---
    public static void CalcEHPs(float hp, float armor, float mr, out float ehpPhys, out float ehpMag, out float ehpMix, float physMix = 0.7f)
    {
        float armorFactor = Mathf.Sqrt((100f + armor) / 100f);
        float mrFactor = Mathf.Sqrt((100f + mr) / 100f);
        ehpPhys = hp * armorFactor;
        ehpMag = hp * mrFactor;
        ehpMix = physMix * ehpPhys + (1f - physMix) * ehpMag;
    }

    // --- 버틸 시간 ---
    public static void CalcTTKs(float ehpPhys, float ehpMag, float ehpMix,
        out float ttkPhys, out float ttkMag, out float ttkMix,
        float enemyPhysDps = 150f, float enemyMagDps = 70f, float physMix = 0.7f)
    {
        float incomingPhys = enemyPhysDps;
        float incomingMag = enemyMagDps;
        float incomingMix = physMix * incomingPhys + (1f - physMix) * incomingMag;

        ttkPhys = ehpPhys / Mathf.Max(1f, incomingPhys);
        ttkMag = ehpMag / Mathf.Max(1f, incomingMag);
        ttkMix = ehpMix / Mathf.Max(1f, incomingMix);
    }

    // --- 유지력 ---
    public static void CalcSustain(float champRegen, float itemRegen, float lifesteal, float adDps,
        out float regenHPS, out float vampPS, out float sustainCombined)
    {
        regenHPS = champRegen + itemRegen;       // 초당 체력 재생
        vampPS = lifesteal * adDps;            // 초당 흡혈 기대치
        sustainCombined = regenHPS + vampPS;     // 합산
    }
}
