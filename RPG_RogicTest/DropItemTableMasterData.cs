using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DropItemTableMasterData
{
    public static IReadOnlyDictionary<GameId<IDropItemTableId>, List<DropItemData>> DropItemDatabase => _dropItemDatabase;
    private static readonly Dictionary<GameId<IDropItemTableId>, List<DropItemData>> _dropItemDatabase = new();

    public static void Load() //アイテムドロップ率は小数表記
    {
        _dropItemDatabase.Clear();

        _dropItemDatabase["drop_table_000"] = new List<DropItemData>
        {
            new DropItemData(ItemID:"item_test_000", Amount: 1, Rarity: ItemRarity.Common, DropRate: 1f),
            new DropItemData("item_test_001", 1, ItemRarity.Common, 0.6f),
            new DropItemData("item_test_001", 1, ItemRarity.Common, 0.4f)
        };
        _dropItemDatabase["drop_table_001"] = new List<DropItemData>
        {
            new DropItemData("item_test_001", 4, ItemRarity.Common, 0.7f),
            new DropItemData("item_test_002", 2, ItemRarity.Rare, 0.2f)
        };

    }

    public static List<DropItemData> GetDropItemTable(GameId<IDropItemTableId> dropTableID)
    {
        if(DropItemDatabase.TryGetValue(dropTableID, out var dropTable))
        {
            return dropTable;
        }
        throw new Exception($"ドロップテーブルID:{dropTableID}のデータが見つかりません");
    }
}



public record DropItemData
(
    GameId<IItemId>? ItemID,
    int Amount,
    ItemRarity Rarity,
    float DropRate //小数点表記
);

