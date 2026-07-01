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
            new ItemEffectData(new List<EffectBase>() 
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

    public static bool TryUseItem(GameId<IItemId> itemId, ConditionContext conditionContext, out ItemData itemData)
    {
        var data = GetItemData(itemId);
        foreach(var condition in data.ConditionData.Conditions)
        {
            bool can = condition.CanExecute(conditionContext);
            if(data.ConditionData.LogicalOperator == LogicalOperator.And && !can)
            {
                itemData = data;
                return false;
            }
            else if (data.ConditionData.LogicalOperator == LogicalOperator.Or && can)
            {
                itemData = data;
                return true;
            }
        }
        itemData = data;
        return data.ConditionData.LogicalOperator == LogicalOperator.And;
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
    List<EffectBase> ItemEffects
);

public record ConditionData
(
    LogicalOperator LogicalOperator,
    List<ConditionBase> Conditions
)
{
    public static readonly ConditionData Empty = new(LogicalOperator.And, []);
    public static readonly ConditionData Default 
        = new(LogicalOperator.And, [new LifeStateCondition(LifeState.Alive, ConditionTarget.Target)]);
    public static ConditionData And(List<ConditionBase> conditions)
        => new(LogicalOperator.And, conditions);
    public static ConditionData Or(List<ConditionBase> conditions)
        => new(LogicalOperator.Or, conditions);
}

public record struct TargetData
(
    TargetType TargetType,
    TargetSelectType TargetSelectType,
    int TargetAmount
);