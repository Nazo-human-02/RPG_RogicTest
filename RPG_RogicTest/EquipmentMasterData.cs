using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class EquipmentMasterData
{
    private static readonly Dictionary<GameId<IEquipmentId>, EquipmentData> _equipmentDatabase = new();
    public static void Load()
    {
        _equipmentDatabase["equip_head_001"] = new(
            new("テスト用ヘルメット", "equip_head_001", EquipmentType.Armor, BodyParts.Head),
            new(
                hp:new(baseFlat:100000f), //+100
                atk:new(finalRate:3f))); //3倍
        _equipmentDatabase["equip_chest_001"] = new(
            new("テスト用アーマー", "equip_chest_001", EquipmentType.Armor, BodyParts.Chest),
            new(
                hp:new(baseFlat:200f), //+200
                def:new(finalRate:5f)  //5倍
                )
            ); 
    }
    public static EquipmentData GetEquipment(GameId<IEquipmentId> equipmentID)
    {
        if (_equipmentDatabase.TryGetValue(equipmentID, out var equipment))
        {
            return equipment;
        }
        throw new Exception($"装備ID:{equipmentID}のデータが見つかりません");
    }
}

public record EquipmentData
(
    EquipmentInfo EquipmentInfo,
    ModifierStat ModifierStat,
    bool IsCursed = false
);

public record EquipmentInfo
(
    string Name, 
    GameId<IEquipmentId> EquipmentID, 
    EquipmentType EquipmentType,
    BodyParts BodyParts
);
