using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public Dropdown championDropdown;
    public Dropdown itemDropdown;
    public InputField levelField;
    public InputField armorField;
    public InputField mrField;
    public Text resultText;
    public Image championIcon;  
    public Image itemIcon;      

    [Header("Database References")]
    public ChampionDatabase champDB;
    public ItemDatabase itemDB;

    [Header("Slider")]
    public Slider physMixSlider;
    public Toggle robustToggle;
    public Text physMixLabel;
    public float physMix = 0.7f;          
    public bool useRobustDefense = false;

    [Header("AllSearch")]
    public BatchSimulator batchSimulator;

    void Start()
    {
        
        championDropdown.ClearOptions();
        itemDropdown.ClearOptions();

        List<string> champNames = champDB.GetChampionNames();
        List<string> itemNames = itemDB.GetItemNames();

        championDropdown.AddOptions(champNames);
        itemDropdown.AddOptions(itemNames);


        championDropdown.onValueChanged.AddListener(OnChampionChanged);
        itemDropdown.onValueChanged.AddListener(OnItemChanged);

        resultText.text = "DPS 계산기 준비 완료";


        if (champNames.Count > 0)
        {
            championDropdown.value = 1; 
            championDropdown.value = 0; 
            OnChampionChanged(0);
        }

        if (itemNames.Count > 0)
        {
            itemDropdown.value = 1;
            itemDropdown.value = 0;
            OnItemChanged(0);
        }

        if (physMixSlider != null)
        {
            physMixSlider.value = 0.7f;   
            physMix = 0.7f;               
        }
    }

    void OnChampionChanged(int index)
    {
        Debug.Log($"[ChampionChanged] index={index}, name={championDropdown.options[index].text}");
        string champName = championDropdown.options[index].text;
        ChampionStats champ = champDB.GetChampion(champName);

        if (champ == null)
        {
            Debug.LogWarning($"⚠ 챔피언 데이터를 찾을 수 없습니다: {champName}");
            return;
        }

        string iconName = champ.championEN;
        Debug.Log($"[ChampionIcon] Loading: ChampionIcons/{iconName}");
        Sprite sprite = Resources.Load<Sprite>($"ChampionIcons/{iconName}");

        if (sprite != null)
        {
            championIcon.sprite = sprite;
            championIcon.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"⚠ 챔피언 아이콘을 찾을 수 없습니다: {iconName}");
            championIcon.color = new Color(1, 1, 1, 0);
        }
    }

    void OnItemChanged(int index)
    {
        string itemName = itemDropdown.options[index].text;
        ItemStats item = itemDB.GetItem(itemName);
        if (item == null) return;

        
        string iconName = item.itemEN.Replace("'", "").Replace("’", "");

        
        Sprite sprite = Resources.Load<Sprite>($"ItemIcons/{iconName}");

        
        if (sprite == null)
            sprite = Resources.Load<Sprite>($"ItemIcons/{item.itemEN}");

        if (sprite != null)
        {
            itemIcon.sprite = sprite;
            itemIcon.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"⚠ 아이템 아이콘을 찾을 수 없습니다: {iconName}");
            itemIcon.color = new Color(1, 1, 1, 0);
        }
    }

    public void OnCalculate()
    {
        if (championDropdown.options.Count == 0 || itemDropdown.options.Count == 0)
        {
            resultText.text = "⚠ 데이터베이스가 비어 있습니다.";
            return;
        }

        string champName = championDropdown.options[championDropdown.value].text;
        string itemName = itemDropdown.options[itemDropdown.value].text;

        ChampionStats champ = champDB.GetChampion(champName);
        ItemStats item = itemDB.GetItem(itemName);

        Debug.Log($"[DEBUG] {itemName} → EN={item.itemEN}, AD={item.ad}, AP={item.ap}, AS={item.aspeed}");

        if (champ == null || item == null)
        {
            resultText.text = $"⚠ 챔피언 혹은 아이템 데이터를 찾을 수 없습니다.";
            return;
        }

        if (!int.TryParse(levelField.text, out int level)) level = 1;
        if (!float.TryParse(armorField.text, out float dummyArmor)) dummyArmor = 40;
        if (!float.TryParse(mrField.text, out float dummyMR)) dummyMR = 50;

        // ✅ DPS 계산 (피해유형 포함)
        float adDps = DPSCalculator.CalculatePhysicalDPS(champ, item, level, dummyArmor);
        float apDps = DPSCalculator.CalculateMagicalDPS(item, dummyMR);
        float mixedDps = DPSCalculator.CalculateDPS(champ, item, level, dummyArmor, dummyMR);

        // ✅ 역할군별 가중치 가져오기
        RoleType roleType;
        if (!System.Enum.TryParse(champ.role, true, out roleType))
            roleType = RoleType.Bruiser;
        RoleWeight w = RoleWeights.Get(roleType);

        // ✅ 피해유형 비율
        float typeWeight = 1f;
        switch (champ.damageType.ToLower())
        {
            case "physical": typeWeight = 1f; break;
            case "magical": typeWeight = 0f; break;
            case "mixed": typeWeight = 0.5f; break;
            case "true": typeWeight = 0.8f; break;
        }

        // 스탯 계산
        float hp = champ.GetHP(level) + item.hp;
        float totalArmor = champ.GetArmor(level) + item.armor;
        float totalMR = champ.GetMR(level) + item.mr;

        BalanceMetrics.CalcEHPs(hp, totalArmor, totalMR, out float ehpPhys, out float ehpMag, out float ehpMix, physMix);
        BalanceMetrics.CalcTTKs(ehpPhys, ehpMag, ehpMix, out float ttkPhys, out float ttkMag, out float ttkMix);

        float ehp = useRobustDefense ? Mathf.Min(ehpPhys, ehpMag) : ehpMix;
        float ttk = useRobustDefense ? Mathf.Min(ttkPhys, ttkMag) : ttkMix;

        BalanceMetrics.CalcSustain(
            champ.GetHPRegen(level),
            item.hp_regen,
            item.lifesteal,
            adDps,
            out float regenHPS,
            out float vampPS,
            out float sustainCombined
        );

        float hasteImpact = 1f + (item.haste / 100f);

        Debug.Log($"idx hp={item.hp}, armor={item.armor}, mr={item.mr}, hp_regen={item.hp_regen}, lifesteal={item.lifesteal}");

        // ✅ 종합 점수 계산 (역할 + 피해유형 반영)
        float comp = w.adDps * (adDps / 500f) * typeWeight
                   + w.apDps * (apDps / 150f) * (1 - typeWeight)
                   + w.ehp * (ehp / 4000f)
                   + w.ttk * Mathf.Clamp01(ttk / 10f)
                   + w.sustain * Mathf.Clamp01(sustainCombined / 20f);

        // ✅ 출력
        resultText.text =
            $"<b>{champName}</b> + <b>{itemName}</b>\n" +
            $"({champ.role} / {champ.damageType})\n\n" +
            $"AD DPS : {adDps:F1}\n" +
            $"AP DPS : {apDps:F1}\n" +
            $"Mixed DPS : {mixedDps:F1}\n" +
            $"EHP : {ehp:F0}\n" +
            $"TTK : {ttk:F2}s\n" +
            $"Sustain : {sustainCombined:F2}\n" +
            $"Composite Score : {comp * 100f:F1}%";
    }

    // 🔘 슬라이더 변경 시 호출
    public void OnPhysMixChanged(float value)
    {
        physMix = Mathf.Clamp01(value);
        Debug.Log($"[DEBUG] OnPhysMixChanged 호출됨 : {physMix}");
        if (physMixLabel != null)
            physMixLabel.text = $"물리 피해 비중 : {(physMix * 100f):F0}%";
    }

    // ☑️ 토글 변경 시 호출
    public void OnRobustToggle(bool isOn)
    {
        useRobustDefense = isOn;
    }

    public void OnExportCSV()
    {
        string champName = championDropdown.options[championDropdown.value].text;
        ChampionStats champ = champDB.GetChampion(champName);

        if (!int.TryParse(levelField.text, out int level)) level = 12;
        if (!float.TryParse(armorField.text, out float armor)) armor = 40;
        if (!float.TryParse(mrField.text, out float mr)) mr = 50;

        batchSimulator.RunBatch(champ, level, armor, mr, physMix, useRobustDefense);
    }
}