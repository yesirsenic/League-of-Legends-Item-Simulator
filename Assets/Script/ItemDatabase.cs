using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public string itemCSVFile = "lol_core_items_DB.csv";

    private List<ItemStats> items = new List<ItemStats>();
    public IReadOnlyList<ItemStats> Items => items;

    void Awake()
    {
        LoadCSV();
    }

    void LoadCSV()
    {
        string path = Path.Combine(Application.streamingAssetsPath, itemCSVFile);
        if (!File.Exists(path))
        {
            Debug.LogError("❌ 아이템 CSV 파일을 찾을 수 없습니다: " + path);
            return;
        }

        // 인코딩 처리
        byte[] raw = File.ReadAllBytes(path);
        bool hasBom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
        Encoding enc = hasBom ? Encoding.UTF8 : Encoding.GetEncoding(949);
        string csvText = enc.GetString(raw).TrimStart('\uFEFF');

        // 줄 분리
        var lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            Debug.LogError("❌ CSV 데이터가 비어 있습니다.");
            return;
        }

        // 따옴표 내 쉼표 무시 Split
        var csvSplit = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        // 헤더 파싱 + 정규화(공백제거/소문자)
        string[] rawHeaders = csvSplit.Split(lines[0]);
        string[] headers = rawHeaders.Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToArray();

        // 헬퍼: 인덱스 구하기
        int Idx(string name)
        {
            int idx = Array.IndexOf(headers, name.ToLowerInvariant());
            return idx; // 없으면 -1
        }

        // 🔎 네가 제공한 헤더 이름들 정확히 매핑
        int idxItemName = Idx("item_name");      // ⚠ CSV가 "Item_name"이라면 아래처럼도 시도
        if (idxItemName < 0) idxItemName = Idx("item_name".Replace("_", "_")); // 그냥 예비
        if (idxItemName < 0) idxItemName = Array.IndexOf(rawHeaders, "Item_name");

        int idxItemEN = Idx("item_en");
        int idxCategory = Idx("item_category");  // 우리가 추가한 분류 열
                                                 // (주의: 맨 앞 "category" 열은 네 목차성 카테고리면 무시해도 됨)

        int idxAD = Idx("ad");
        int idxAP = Idx("ap");
        int idxAS = Idx("as");
        int idxCrit = Idx("crit");
        int idxCritDmgMod = Idx("crit_damage_mod");
        int idxHaste = Idx("haste");
        int idxLS = Idx("lifesteal");

        int idxFArPen = Idx("flat_armor_pen");
        int idxPArPen = Idx("pct_armor_pen");
        int idxFMpPen = Idx("flat_magic_pen");
        int idxPMpPen = Idx("pct_magic_pen");

        int idxHP = Idx("hp");
        int idxArmor = Idx("armor");
        int idxMR = Idx("mr");
        int idxTenacity = Idx("tenacity");
        int idxHealAmp = Idx("healing_amp");
        int idxMana = Idx("mana");
        int idxHPRegen = Idx("hp_regen");
        int idxMPRegen = Idx("mana_regen");
        int idxMSFlat = Idx("ms_flat");
        int idxMSPct = Idx("ms_pct");

        // 디버그: 인덱스 정상 잡혔는지 확인
        Debug.Log($"[IDX] name={idxItemName}, en={idxItemEN}, cat={idxCategory}, ad={idxAD}, ap={idxAP}, as={idxAS}, crit={idxCrit}, hp={idxHP}, armor={idxArmor}, mr={idxMR}, hp_regen={idxHPRegen}, lifesteal={idxLS}");

        items.Clear();
        var inv = CultureInfo.InvariantCulture;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] cols = csvSplit.Split(line).Select(s => s.Trim().Trim('"')).ToArray();
            if (cols.Length < headers.Length) continue;

            float PF(int idx) => (idx < 0 || idx >= cols.Length || string.IsNullOrEmpty(cols[idx])) ? 0f : float.Parse(cols[idx], inv);
            string PS(int idx) => (idx < 0 || idx >= cols.Length) ? "" : cols[idx];

            var it = new ItemStats();
            it.name = PS(idxItemName);
            it.itemEN = PS(idxItemEN);
            it.itemCategory = PS(idxCategory); // 빈 값이면 정렬시 ""로 나옴

            it.ad = PF(idxAD);
            it.ap = PF(idxAP);
            it.aspeed = PF(idxAS);
            it.crit = PF(idxCrit);
            it.haste = PF(idxHaste);
            it.lifesteal = PF(idxLS);

            it.flat_armor_pen = PF(idxFArPen);
            it.pct_armor_pen = PF(idxPArPen);
            it.flat_magic_pen = PF(idxFMpPen);
            it.pct_magic_pen = PF(idxPMpPen);

            it.hp = PF(idxHP);
            it.armor = PF(idxArmor);
            it.mr = PF(idxMR);
            it.tenacity = PF(idxTenacity);
            it.healing_amp = PF(idxHealAmp);
            it.mana = PF(idxMana);
            it.hp_regen = PF(idxHPRegen);
            it.mana_regen = PF(idxMPRegen);
            it.ms_flat = PF(idxMSFlat);
            it.ms_pct = PF(idxMSPct);

            // 필수 키만 존재하면 추가
            if (!string.IsNullOrEmpty(it.name))
                items.Add(it);
        }

        Debug.Log($"✅ 아이템 CSV 로드 완료 (총 {items.Count}개)");
    }

    string SafeGet(string[] arr, int index)
    {
        if (index < 0 || index >= arr.Length) return "";
        return arr[index].Trim();
    }

    public ItemStats GetItem(string name)
    {
        return items.Find(i => i.name == name || i.itemEN == name);
    }

    public List<string> GetItemNames(bool english = false)
    {
        List<string> names = new List<string>();
        foreach (var i in items)
            names.Add(english ? i.itemEN : i.name);
        return names;
    }
}
