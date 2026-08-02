using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class TextMasterData
{
    public static string GetModifierStatText(ModifierStat modifierStat)
    {
        StringBuilder sb = new StringBuilder();
        foreach(var statType in Enum.GetValues<StatType>())
        {
            var modifiableStat = modifierStat[statType];
            var text = GetModifiableStatText(modifiableStat, statType);
            if(!string.IsNullOrEmpty(text))
            {
                sb.AppendLine(text);
            }
        }
        return sb.ToString().TrimEnd();
    }
    public static string GetModifiableStatText(ModifiableStat modifiableStat, StatType statType)
    {
        StringBuilder sb = new StringBuilder();
        foreach(var modType in Enum.GetValues<ModifierType>())
        {
            float value = modifiableStat[modType];
            bool hasValue = modType switch
            {
                ModifierType.BaseFlat or ModifierType.FlatOffset => value != 0,
                ModifierType.RatePercent or ModifierType.FinalRate => value != 1.0f,
                _ => throw new ArgumentOutOfRangeException(nameof(modType), modType, null)
            };
            if (hasValue)
            {
                if(modType is ModifierType.RatePercent or ModifierType.FinalRate)
                {
                    value = (value - 1.0f) * 100f;
                }
                var text = GetModifierText(modType, statType, value);
                sb.AppendLine(text);
            }
        }
        return sb.ToString().TrimEnd();
    }
    public static string GetStatText(StatType statType)
    {
        return statType switch
        {
            StatType.Hp => "体力",
            StatType.Mp => "魔力",
            StatType.Atk => "攻撃力",
            StatType.Def => "防御力",
            StatType.Agi => "敏捷性",
            StatType.Cri => "会心ダメージ",
            StatType.Criper => "会心率",
            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, null)
        };
    }
    public static string GetCategoryText(ItemCategory itemCategory)
    {
        return itemCategory switch
        {
            ItemCategory.Consumable => "消耗品",
            ItemCategory.Unique => "効果素材",
            ItemCategory.Tool => "道具",
            ItemCategory.Valuable => "貴重品",
            ItemCategory.Material => "素材",
            _ => throw new ArgumentOutOfRangeException(nameof(itemCategory), itemCategory, null)
        };
    }
    public static string GetModifierText(ModifierType modifierType, StatType statType, float value)
    {
        var stattext = GetStatText(statType);
        string valueText = value.ToString("+0;-0") +
            (modifierType is ModifierType.RatePercent or ModifierType.FinalRate ? "%" : "");

        return modifierType switch
        {
            ModifierType.BaseFlat => $"基礎{stattext}:{valueText}", //加算はマイナス値も想定
            ModifierType.FlatOffset => $"{stattext}:{valueText}",
            ModifierType.RatePercent => $"{stattext}:{valueText}",
            ModifierType.FinalRate => $"最終{stattext}:{valueText}",
            _ => throw new ArgumentOutOfRangeException(nameof(modifierType), modifierType, null)
        };
    }

    public static string GetTargetPointText(TargetPoint targetPoint)
    {
        return targetPoint switch
        {
            TargetPoint.HP => "体力",
            TargetPoint.MP => "魔力",
            _ => throw new ArgumentOutOfRangeException(nameof(targetPoint), targetPoint, null)
        };
    }
    public static string GetReferTypeText(ReferType referType)
    {
        return referType switch
        {
            ReferType.Max => "最大値",
            ReferType.Current => "現在値",
            _ => throw new ArgumentOutOfRangeException(nameof(referType), referType, null)
        };
    }
    public static string GetBodyPartsText(BodyParts bodyParts)
    {
        return bodyParts switch
        {
            BodyParts.Head => "頭",
            BodyParts.Chest => "胴",
            BodyParts.Legs => "脚",
            BodyParts.Feet => "足",
            BodyParts.Arms => "腕",
            BodyParts.Hands => "手",
            BodyParts.LeftHand => "左手",
            BodyParts.RightHand => "右手",
            BodyParts.Blank => "なし",
            _ => throw new ArgumentOutOfRangeException(nameof(bodyParts), bodyParts, null)
        };
    }
    public static string GetEquipmentTypeText(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Armor => "防具",
            EquipmentType.Weapon => "武器",
            EquipmentType.Blank => "なし",
            _ => throw new ArgumentOutOfRangeException(nameof(equipmentType), equipmentType, null)
        };
    }
    public static string GetMenuTypeText(MenuType menuType)
    {
        return menuType switch
        {
            MenuType.Inventory => "持ち物",
            MenuType.Status => "ステータス",
            MenuType.Equipment => "装備",
            MenuType.Skill => "スキル",
            MenuType.Save => "セーブ",
            _ => throw new ArgumentOutOfRangeException(nameof(menuType), menuType, null)
        };
    }
    public static string GetEquipmentsText(IReadOnlyDictionary<BodyParts, EquipmentSet> equipments)
    {
        StringBuilder sb = new StringBuilder();
        foreach(var bodyParts in Enum.GetValues<BodyParts>())
        {
            if(bodyParts is BodyParts.Blank) 
                continue;
            var equipment = equipments[bodyParts];
            var bodyPartsText = GetBodyPartsText(bodyParts);
            var equipmentName = equipment.Equipment.EquipmentInfo.Name;
            sb.AppendLine($"[{bodyPartsText}:{equipmentName}]");
        }
        return sb.ToString().TrimEnd();
    }
    public static string GetFieldTypeText(FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Dungeon => "ダンジョン",
            FieldType.OutSide => "外の世界",
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, null)
        };
    }
    public static string GetPartyText(PartyController partyController)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===========================================================");
        sb.AppendLine($"[所持金] {partyController.OwnedGold} G");
        sb.AppendLine(GetInventoryText(partyController.Inventory));
        sb.AppendLine("-----------------------------------------------------------");
        sb.AppendLine("[パーティメンバー]");
        foreach(var member in partyController.PartyMember)
        {
            sb.AppendLine(GetCharacterStatusText(member));
        }
        return sb.ToString().TrimEnd();
    }
    public static string GetInventoryText(Inventory inventory)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[インベントリ]\n");
        foreach(var item in inventory.ItemInventory)
        {
            var itemData = ItemMasterData.GetItemData(item.Key);
            var categoryText = GetCategoryText(itemData.ItemCategory);
            sb.Append($"[{itemData.ItemName}({categoryText}):×{item.Value}]");
        }
        return sb.ToString().TrimEnd();
    }
    public static string GetCharacterStatusText(Entity entity)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(new string('=', Console.WindowWidth));
        sb.AppendLine($"[名前] {entity.Name} | [レベル] {entity.Stat.expSet.CurrentLevel}" +
            $"(次のレベルまで{entity.Stat.expSet.NextRequiredExp}exp)");
        sb.AppendLine($"[HP] {entity.Stat.CurrentHp} / {entity.Stat.TotalHP}");
        sb.AppendLine($"[MP] {entity.Stat.CurrentMp} / {entity.Stat.TotalMP}");
        sb.AppendLine(new string ('=', Console.WindowWidth));
        return sb.ToString().TrimEnd();
    }
    public static string GetCharacterSubWindowText(Entity entity)
    {
        StringBuilder sb = new StringBuilder();
        var substatus = GetCharacterSubStatusText(entity);
        var equip = GetEquipmentMenuText(entity.EquipmentController.Equipments);
        var skills = GetHasSkillText(entity);
        sb.AppendLine(MergeColumns(substatus, equip, Console.WindowWidth / 5));
        sb.AppendLine(new string('-', Console.WindowWidth));
        sb.AppendLine(skills);
        sb.AppendLine(new string('=', Console.WindowWidth));

        return sb.ToString().TrimEnd();
    }
    public static string GetCharacterSubStatusText(Entity entity) 
    {
        StringBuilder sb = new();
        sb.AppendLine("[ステータス]");
        sb.AppendLine($"[攻撃力]".PadRight(8) +$"{entity.Stat.TotalAtk}".PadLeft(6));
        sb.AppendLine($"[防御力]".PadRight(8) + $" {entity.Stat.TotalDef}".PadLeft(6));
        sb.AppendLine($"[敏捷性]".PadRight(8) + $" {entity.Stat.TotalAgi}".PadLeft(6));
        sb.AppendLine($"[会心率]".PadRight(8) + $" {entity.Stat.TotalCriPer} %".PadLeft(6));
        sb.AppendLine($"[会心倍率]".PadRight(8) + $" {entity.Stat.TotalCri * 100f} %".PadLeft(6));
        return sb.ToString().TrimEnd();
    }
    public static string GetHasSkillText(Entity entity)
    {
        const int maxColumn = 5;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[スキル]");
        int column = 0;
        foreach (var skill in entity.ValidSkills)
        {
            column++;
            if(column % maxColumn == 0)
            {
                sb.AppendLine($" [{skill.SkillInfo.SkillName}] ");
            }
            else
            {
                sb.Append($" [{skill.SkillInfo.SkillName}] ");
            }
        }
        return sb.ToString().TrimEnd();
    }
    public static string GetEquipmentMenuText(IReadOnlyDictionary<BodyParts, EquipmentSet> equipments)
    {
        const int maxColumn = 2;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[装備]");
        int column = 0;
        foreach (var bodyParts in Enum.GetValues<BodyParts>())
        {
            if(bodyParts is BodyParts.Blank) 
                continue;

            column++;

            var equipment = equipments[bodyParts];
            var bodyPartsText = GetBodyPartsText(bodyParts);
            var equipmentName = equipment.Equipment.EquipmentInfo.Name;
            if(column % maxColumn == 0)
            {
                sb.AppendLine($" [ {bodyPartsText} : {equipmentName} ] ");
            }
            else
            {
                sb.Append($" [ {bodyPartsText} : {equipmentName} ] ");
            }
        }
        return sb.ToString();
    }
    public static string GetDungeonHeaderText(int currentFloor)
    {
        var sb = new StringBuilder();
        sb.AppendLine(new string('=', 30));
        sb.AppendLine("||"+$"[ダンジョン] = {currentFloor}層 =".PadRight(20) + "||");
        sb.AppendLine(new string('=', 30));
        return sb.ToString().TrimEnd();
    }
    public static string GetEncounterEnemyText(IReadOnlyList<EnemyCharacter> enemies)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(new string('=', Console.WindowWidth));
        foreach(var enemy in enemies)
        {
            sb.Append($"[Lv{enemy.Stat.expSet.CurrentLevel} : {enemy.Name}|{enemy.Stat.CurrentHp}/{enemy.Stat.TotalHP}HP]");
        }
        //sb.AppendLine("が現れた");
        sb.AppendLine(new string('=', Console.WindowWidth));
        return sb.ToString().TrimEnd();
    }




    public static string MergeColumns(string leftText, string rightText, int leftWidth)
    {
        StringBuilder sb = new StringBuilder();

        string[] leftLines = leftText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        string[] rightLines = rightText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        int maxLines = Math.Max(leftLines.Length, rightLines.Length);

        for (int i = 0; i < maxLines; i++)
        {
            string left = i < leftLines.Length ? leftLines[i].TrimEnd() : "";
            string right = i < rightLines.Length ? rightLines[i].TrimEnd() : "";

            sb.AppendLine($"{left.PadRight(leftWidth)}| {right}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string GetWindowLine()
        => new string('=', Console.WindowWidth);

    public static string GetWindowSmallLine()
        => new string ('-', Console.WindowWidth);
}
