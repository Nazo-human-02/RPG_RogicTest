using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Equipment(EquipmentInfo equipmentInfo, ModifierStat modifierStat, bool isCursed = false)
{
    public readonly EquipmentInfo EquipmentInfo = equipmentInfo;
    public readonly ModifierStat ModifierStat = modifierStat;
    public readonly bool IsCursed = isCursed;
    public Equipment Clone()
    {
        return (Equipment)this.MemberwiseClone();
    }

    public string GetModifierDescription()
    {
        var description = TextMasterData.GetModifierStatText(ModifierStat);
        return description;
    }
    public static Equipment Blank(BodyParts bodyParts)
        => new(new("なし", "equip_000", EquipmentType.Blank, bodyParts), new());
}