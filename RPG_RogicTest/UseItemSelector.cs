using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

public class UseItemSelecter(ILogProvider logProvider, IInputProvider inputProvider, IScreenProvider screenProvider)
    : ISelector<SelectItemData>
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly IScreenProvider _screenProvider = screenProvider;

    private Dictionary<int, SelectionCommand<SelectItemData>> _selectionCommands = new();
    private void InitializeCommands(IReadOnlyDictionary<GameId<IItemId>, int> itemInventory, ConditionContext conditionContext)
    {
        _selectionCommands.Clear();
        int n = 1;
        foreach (var item in itemInventory)
        {
            var itemData = ItemMasterData.GetItemData(item.Key);
            UseLessType useLessCheck = UseValidator.TryUseItem(conditionContext, itemData, out TargetResolveResult result);
            bool canUse = UseValidator.CanUse(useLessCheck);
            SelectItemData data =
                new SelectItemData(item.Key, itemData.ItemName, itemData.ItemCategory, item.Value, result, canUse);

            string category = GetCategoryText(itemData.ItemCategory);
            string useAbleText = (canUse) ? "使用可能" : "使用不可";
            string t = $"\n|[{itemData.ItemName}({category}):×{item.Value}]<{useAbleText}>|==>[{n}]";
            _selectionCommands[n] = new(t, n, () => new SelectionSuccess<SelectItemData>(data));
            n++;
        }
        _selectionCommands[0] = new("|もどる|==>[0]", 0, () => new SelectionCancel<SelectItemData>());
    }
    public void Open(Dictionary<GameId<IItemId>, int> itemInventory, ConditionContext conditionContext)
    {
        InitializeCommands(itemInventory, conditionContext);
        Render();
    }
    public void HandleInput(int num, out SelectionResult<SelectItemData>? result)
    {
        result = null;
        if (num < 0 || num > _selectionCommands.Count)
        {
            _screenProvider.Set(ScreenLayer.Content, "選択肢の範囲外です");
            _screenProvider.RefreshUntil();
            return;
        }
        result = _selectionCommands[num].Execute.Invoke();
        if(result is SelectionSuccess<SelectItemData> success && !success.Value.CanUse)
        {
            _screenProvider.Set(ScreenLayer.Content, "そのアイテムは使用できません");
            _screenProvider.RefreshUntil();
            result = null;
        }
    }
    public SelectionResult<SelectItemData> SelectingItem
        (IReadOnlyDictionary<GameId<IItemId>, int> itemInventory, ConditionContext conditionContext)
    {
        InitializeCommands(itemInventory, conditionContext);
        Render() ;
        while(true) //仮置きのやつ、戦闘でのwhileを状態遷移に変更出来たら改良、消す予定
        {
            string? input = _inputProvider.Input();
            if(string.IsNullOrEmpty(input) || !int.TryParse(input, out int inputNum))
            {
                _screenProvider.Set(ScreenLayer.Content, "入力が正しくありません");
                _screenProvider.RefreshUntil();
            }
            else
            {
                HandleInput(inputNum, out var result);
                if(result is not null)
                {
                    return result;
                }
            }
        }
    }

    private void Render()
    {
        StringBuilder text = new StringBuilder();
        foreach(var command in _selectionCommands.Values)
        {
            text.Append(command.Text);
        }
        _screenProvider.RefreshInput(text.ToString());
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
    Entity Target,

    BattleManager? BattleManager,

    BattleCalculator BattleCalculator,
    IRandomProvider RandomProvider
)
{ 
    public bool IsBattle => BattleManager != null; 
}

public record SelectItemData
(
    GameId<IItemId> ItemId,
    string ItemName,
    ItemCategory ItemCategory,
    int Amount,
    TargetResolveResult TargetResolveResult,
    bool CanUse
);