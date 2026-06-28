using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

public class UseItemSelecter(ILogProvider logProvider, IInputProvider inputProvider)
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly TargetResolver _targetResolver = new TargetResolver();

    public SelectionResult<SelectItemData> SelectingItem
        (IReadOnlyDictionary<GameId<IItemId>, int> itemInventory, ConditionContext conditionContext)
    {
        var useableItems = GetUseableItems(itemInventory, conditionContext);
        SelectingText(useableItems) ;
        return Selecting(useableItems);
    }

    private SelectionResult<SelectItemData> Selecting(IReadOnlyList<SelectItemData> selectItemDatas)
    {
        SelectItemData? currentSelect = null;
        while(true)
        {
            string? input = _inputProvider.Input();
            if(string.IsNullOrEmpty(input))
            {
                if (currentSelect != null)
                    return new SelectionSuccess<SelectItemData>((SelectItemData)currentSelect);
                else
                    _logProvider.Log("アイテムを選択してください");
            }
            else if(!int.TryParse(input, out int inputNum) || (inputNum < 0 || selectItemDatas.Count < inputNum))
            {
                _logProvider.Log("正しい選択肢を入力してください");
            }
            else if(inputNum == 0)
            {
                return new SelectionCancel<SelectItemData>();
            }
            else
            {
                var selectItem = selectItemDatas[inputNum - 1];
                if (!selectItem.CanUse)
                    _logProvider.Log("そのアイテムは使用できません");
                else
                {
                    currentSelect = selectItem;
                    _logProvider.Log($"[選択中]-->" +
                        $"[{selectItem.ItemName}({GetCategoryText(selectItem.ItemCategory)})×{selectItem.Amount}]");
                }
            }
        }
    }

    private TargetResolveResult ConditionCheck
        (TargetData targetData, ConditionData conditionData, ConditionContext conditionContext)
    {
        var result = _targetResolver.TargetResolve(conditionData, conditionContext, targetData);
        return result;
    }

    private List<SelectItemData> GetUseableItems
        (IReadOnlyDictionary<GameId<IItemId>, int> itemInventory, ConditionContext conditionContext)
    {
        List<SelectItemData> items = new List<SelectItemData>();
        foreach (var item in itemInventory)
        {
            var itemData = ItemMasterData.GetItemData(item.Key);
            bool canUse = itemData.ItemCategory == ItemCategory.Consumable || itemData.ItemCategory == ItemCategory.Tool;
            TargetResolveResult targetResolve = (canUse) ?
                ConditionCheck(itemData.TargetData, itemData.ConditionData, conditionContext)
                : new(new(), TargetSelectType.Self, 0);
            canUse = (targetResolve.TargetCandidates.Count > 0); 
            SelectItemData data =
                new SelectItemData(item.Key, itemData.ItemName, itemData.ItemCategory, item.Value, targetResolve, canUse);
            items.Add(data);
        }
        return items;
    }

    private void SelectingText(IReadOnlyList<SelectItemData> selectItemDatas)
    {
        StringBuilder text = new StringBuilder();
        int n = 1;
        text.Append("|もどる|==>[0]");
        foreach(var item in selectItemDatas)
        {
            string category = GetCategoryText(item.ItemCategory);
            string canUse = (item.CanUse) ? "使用可能" : "使用不可";
            string t = $"\n|[{item.ItemName}({category}):×{item.Amount}]<{canUse}>|==>[{n}]";
            text.Append(t);
            n++;
        }
        _logProvider.Log(text.ToString());
    }

    private string GetCategoryText(ItemCategory itemCategory)
    {
        return (itemCategory) switch
        {
            ItemCategory.Consumable => "消耗品",
            ItemCategory.Tool => "道具",
            ItemCategory.Unique => "効果素材",
            ItemCategory.Valuable => "大事なもの",
            ItemCategory.Material => "素材",
            _ => "想定外の品"
        };
    }
}



public record FieldContext
(
    FieldType FieldType,
    int FloorNumber
);

public record EffectContent
(
    Entity User,
    IReadOnlyList<Entity> Targets,

    BattleManager? BattleManager,
    DungeonManager? DungeonManager,

    BattleCalculator BattleCalculator,
    IRandomProvider RandomProvider
);

public record SelectItemData
(
    GameId<IItemId> ItemId,
    string ItemName,
    ItemCategory ItemCategory,
    int Amount,
    TargetResolveResult TargetResolveResult,
    bool CanUse
);