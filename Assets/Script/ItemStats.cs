using UnityEngine;

[System.Serializable]
public class ItemStats
{
    public string name;
    public string itemEN;
    public string itemCategory;

    // 공격
    public float ad, ap, aspeed, crit;
    public float haste, lifesteal;
    public float flat_armor_pen, pct_armor_pen;
    public float flat_magic_pen, pct_magic_pen;

    // ✅ 생존/기타 
    public float hp, armor, mr;
    public float hp_regen;      
    public float mana, mana_regen;
    public float tenacity, healing_amp;
    public float ms_flat, ms_pct;
}
