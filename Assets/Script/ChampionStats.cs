using UnityEngine;

[System.Serializable]
public class ChampionStats
{
    public string name;
    public string championEN;
    public int stringnum;

    // --- 기본 스탯 ---
    public float hp_base, hp_growth;
    public float hp_regen_base, hp_regen_growth;
    public float mana_base, mana_growth;
    public float mana_regen_base, mana_regen_growth;
    public float ad_base, ad_growth;
    public float as_base, as_growth;
    public float armor_base, armor_growth;
    public float mr_base, mr_growth;
    public float range_base, ms_base;
    public string role;
    public string damageType;

    // --- 계산 메서드 ---
    public float GetHP(int level) => hp_base + hp_growth * (level - 1);
    public float GetAD(int level) => ad_base + ad_growth * (level - 1);
    public float GetAS(int level) => as_base * (1 + (as_growth / 100f) * (level - 1));
    public float GetArmor(int level) => armor_base + armor_growth * (level - 1);
    public float GetMR(int level) => mr_base + mr_growth * (level - 1);
    public float GetHPRegen(int level) => hp_regen_base + hp_regen_growth * (level - 1);
    public float GetMana(int level) => mana_base + mana_growth * (level - 1);
    public float GetManaRegen(int level) => mana_regen_base + mana_regen_growth * (level - 1);
}
