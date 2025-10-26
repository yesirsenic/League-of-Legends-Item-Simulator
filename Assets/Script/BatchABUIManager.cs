using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BatchABUIManager : MonoBehaviour
{
    [Header("📁 Database References")]
    public ChampionDatabase champDB;
    public ItemDatabase itemDB;

    [Header("📊 UI References (Legacy UI)")]
    public Dropdown categoryDropdown;   // 역할군 선택
    public Dropdown variableDropdown;   // 변수 선택 (AD/AP/HP 등)
    public Slider deltaSlider;          // ±% 조정 슬라이더
    public Text deltaLabel;             // 슬라이더 값 표시
    public Button runButton;            // 실행 버튼
    public Text resultText;             // 결과 표시 영역 (멀티라인 Text)

    [Header("📂 CSV 설정")]
    public string inputCSV = "SimulationResults_Sorted.csv"; // 입력 CSV
    public string outputCSV = "AB_UI_ChampionResults.csv";   // 출력 CSV

    private List<ResultRow> results = new List<ResultRow>();
    private ChampionStats selectedChampion;

    [Serializable]
    private class ResultRow
    {
        public string Champion;
        public string Category;
        public string Item;
        public float AD, AP, EHP, TTK, Sustain, Comp;
    }

    void Start()
    {
        
        
    }

    // ✅ CSV 읽기
    public void LoadResultsCSV()
    {
        string path = Path.Combine(Application.dataPath, "Results", inputCSV);
        if (!File.Exists(path))
        {
            Debug.LogError($"❌ 시뮬레이션 CSV를 찾을 수 없습니다: {path}");
            return;
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8);
        results.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 9) continue;
            try
            {
                results.Add(new ResultRow
                {
                    Champion = cols[0].Trim(),
                    Category = cols[1].Trim(),
                    Item = cols[2].Trim(),
                    Comp = float.Parse(cols[8])
                });
            }
            catch { continue; }
        }

        Debug.Log($"✅ 결과 CSV 로드 완료 ({results.Count}개)");
        SetupDropdowns();
        deltaSlider.onValueChanged.AddListener(OnSliderChanged);
        runButton.onClick.AddListener(OnRunClicked);
        deltaLabel.text = $"{deltaSlider.value:+0;-0}%";
    }

    // ✅ 드롭다운 초기화
    void SetupDropdowns()
    {
        var cats = results.Select(r => r.Category).Distinct().ToList();
        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(cats);

        variableDropdown.ClearOptions();
        variableDropdown.AddOptions(new List<string> {
            "AD","AP","AS","HP","Armor","MR","Haste","Lifesteal"
        });
    }

    void OnSliderChanged(float v)
    {
        deltaLabel.text = $"{v:+0;-0}%";
    }

    // ✅ 실행 버튼
    public void OnRunClicked()
    {

        string selectedCategory = categoryDropdown.options[categoryDropdown.value].text;
        string variable = variableDropdown.options[variableDropdown.value].text;
        float delta = deltaSlider.value;

        var sb = new StringBuilder();
        sb.AppendLine($"[A/B 테스트] Category: {selectedCategory} | 변수: {variable} | 변경: {delta:+0;-0}%");
        sb.AppendLine("-----------------------------------------------------");

        var csvOut = new StringBuilder();
        csvOut.AppendLine("Champion,Category,Item,Variable,Change%,Comp_A,Comp_B,Δ%");

        // 선택된 카테고리 아이템 필터
        var subset = results.Where(r => r.Category == selectedCategory).ToList();

        // 카테고리 내에서 각 챔피언별 상위/하위 3개씩만 추출
        var grouped = subset.GroupBy(r => r.Champion);
        foreach (var group in grouped)
        {
            var champ = champDB.GetChampion(group.Key);
            if (champ == null)
            {
                sb.AppendLine($"⚠️ {group.Key} 챔피언 데이터를 찾을 수 없습니다.");
                continue;
            }

            sb.AppendLine($"\n🧩 Champion: {champ.name}");

            var topBottom = group.OrderByDescending(r => r.Comp).Take(3)
                                 .Concat(group.OrderBy(r => r.Comp).Take(3))
                                 .ToList();

            foreach (var row in topBottom)
            {
                var item = itemDB.GetItem(row.Item);
                if (item == null)
                {
                    sb.AppendLine($"⚠️ {row.Item} (ItemDB에 없음)");
                    continue;
                }

                var A = Evaluate(champ, item);
                var B = Evaluate(champ, CloneWithDelta(item, variable, delta));

                float deltaComp = Mathf.Approximately(A.Comp, 0f)
                    ? 0f : ((B.Comp - A.Comp) / Mathf.Abs(A.Comp)) * 100f;

                sb.AppendLine($"{row.Item,-22} | Δ{variable}={delta:+0;-0}% → {A.Comp:F3} → {B.Comp:F3} ({deltaComp:+0.0;-0.0}%)");
                csvOut.AppendLine($"{champ.name},{row.Category},{row.Item},{variable},{delta:+0;-0},{A.Comp:F3},{B.Comp:F3},{deltaComp:F2}");
            }
        }

        resultText.text = sb.ToString();

        // 결과 저장
        string outPath = Path.Combine(Application.dataPath, "Results", outputCSV);
        File.WriteAllText(outPath, csvOut.ToString(), Encoding.UTF8);
        Debug.Log($"✅ A/B 결과 저장 완료 → {outPath}");


#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    // ✅ 계산 로직
    struct Metrics { public float AD, AP, EHP, Sustain, Comp; }

    Metrics Evaluate(ChampionStats champ, ItemStats item)
    {
        float adDps = DPSCalculator.CalculatePhysicalDPS(champ, item, 18, 100f);
        float apDps = DPSCalculator.CalculateMagicalDPS(item, 100f);

        float hp = champ.GetHP(18) + item.hp;
        float totalArmor = champ.GetArmor(18) + item.armor;
        float totalMR = champ.GetMR(18) + item.mr;

        BalanceMetrics.CalcEHPs(hp, totalArmor, totalMR,
            out float ehpPhys, out float ehpMag, out float ehpMix, 0.7f);

        BalanceMetrics.CalcSustain(champ.GetHPRegen(18), item.hp_regen, item.lifesteal, adDps,
            out _, out _, out float sustain);

        RoleType roleType;
        if (!Enum.TryParse(champ.role, true, out roleType)) roleType = RoleType.Bruiser;
        var w = RoleWeights.Get(roleType);

        float comp = w.adDps * (adDps / 500f)
                   + w.apDps * (apDps / 150f)
                   + w.ehp * (ehpMix / 4000f)
                   + w.sustain * Mathf.Clamp01(sustain / 20f);

        return new Metrics { AD = adDps, AP = apDps, EHP = ehpMix, Sustain = sustain, Comp = comp };
    }

    // ✅ 아이템 복제 후 변수 조정
    ItemStats CloneWithDelta(ItemStats src, string var, float pct)
    {
        var t = JsonUtility.FromJson<ItemStats>(JsonUtility.ToJson(src));
        float m = 1f + pct / 100f;
        switch (var.ToLower())
        {
            case "ad": t.ad *= m; break;
            case "ap": t.ap *= m; break;
            case "as": t.aspeed *= m; break;
            case "hp": t.hp *= m; break;
            case "armor": t.armor *= m; break;
            case "mr": t.mr *= m; break;
            case "haste": t.haste *= m; break;
            case "lifesteal": t.lifesteal *= m; break;
        }
        return t;
    }
}
