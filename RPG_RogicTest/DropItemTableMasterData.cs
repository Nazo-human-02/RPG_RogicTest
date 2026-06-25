using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DropItemTableMasterData
{
    public static IReadOnlyDictionary<GameId<IDropItemTableId>, List<DropItemData>> DropItemDatabase => _dropItemDatabase;
    private static readonly Dictionary<GameId<IDropItemTableId>, List<DropItemData>> _dropItemDatabase = new();

    public static void Load()
    {
        _dropItemDatabase.Clear();

        _dropItemDatabase["drop_table_000"] = new List<DropItemData>
        {
            new DropItemData("item_test_000", 1, ItemRarity.Common, 100),
            new DropItemData("item_test_001", 2, ItemRarity.Common, 60),
            new DropItemData("item_test_001", 3, ItemRarity.Common, 40)
        };
        _dropItemDatabase["drop_table_001"] = new List<DropItemData>
        {
            new DropItemData("item_test_001", 5, ItemRarity.Common, 70),
            new DropItemData("item_test_002", 2, ItemRarity.Rare, 20),
            new DropItemData(null, 0, ItemRarity.Common, 40)
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
    int DropWeight
);

