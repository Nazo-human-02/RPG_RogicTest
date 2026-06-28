using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ItemMasterData
{
    public static IReadOnlyDictionary<GameId<IItemId>, ItemData> ItemMasterDatabase => _itemMasterDatabase;
    private static readonly Dictionary<GameId<IItemId>, ItemData> _itemMasterDatabase = new();

    public static void Load()
    {
        _itemMasterDatabase.Clear();

        _itemMasterDatabase["item_test_000"] = new ItemData("item_test_000", "テスト用アイテム", ItemCategory.Consumable, 999,
            new TargetData(TargetType.Ally, TargetSelectType.Self, 1),
            new ItemEffectData(new List<ItemEffectBase>() 
            { 
                new HealEffect(1000, true, ReferType.Max, TargetPoint.HP)
            }),
            new ConditionData( LogicalOperator.And, new List<ConditionBase>() 
            {
                new LevelCondition(10, ConditionTarget.User, CompareType.MoreOrEqual) 
            }));

        _itemMasterDatabase["item_test_001"] = new ItemData("item_test_001", "テスト用素材1", ItemCategory.Material, 999,
            new TargetData(TargetType.None, TargetSelectType.Self, 0),
            new ItemEffectData(new()), new ConditionData(LogicalOperator.And, new()));

        _itemMasterDatabase["item_test_002"] = new ItemData("item_test_002", "テスト用素材2", ItemCategory.Material, 999,
            new TargetData(TargetType.None, TargetSelectType.Self, 0),
            new ItemEffectData([]), new ConditionData(LogicalOperator.And, []));

    }

    public static ItemData GetItemData(GameId<IItemId> itemID)
    {
        if (ItemMasterDatabase.TryGetValue(itemID, out var data))
            return data;
        throw new InvalidOperationException($"アイテムID:{itemID}が見つかりませんでした");
    }
}


public record ItemData
(
    GameId<IItemId> ItemId,
    string ItemName,
    ItemCategory ItemCategory,
    int MaxStack,
    TargetData TargetData,
    ItemEffectData ItemEffectData,
    ConditionData ConditionData
);

public record ItemEffectData
(
    List<ItemEffectBase> ItemEffects
);

public record ConditionData
(
    LogicalOperator LogicalOperator,
    List<ConditionBase> Conditions
)
{
    public static readonly ConditionData Empty = new(LogicalOperator.And, []);
}

public record TargetData
(
    TargetType TargetType,
    TargetSelectType TargetSelectType,
    int TargetAmount
);