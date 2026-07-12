using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InventoryMenu(TargetResolver targetResolver, TargetSelect targetSelect, BattleCalculator battleCalculator)
{
    private readonly TargetResolver _targetResolver = targetResolver;
    private readonly TargetSelect _targetSelector = targetSelect;
    private readonly BattleCalculator _battleCalculator = battleCalculator;

    private List<GameId<IItemId>> _itemDisplay = new();
    private void Initialize(Inventory inventory, IScreenProvider screen)
    {
        _itemDisplay.Clear();

        StringBuilder sb = new();
        sb.AppendLine("=持ち物=");
        int num = 1;
        foreach (var item in inventory.ItemInventory)
        {
            ItemData itemData = ItemMasterData.GetItemData(item.Key);
            sb.AppendLine
                ($"<{num}>|[({TextMasterData.GetCategoryText(itemData.ItemCategory)}){itemData.ItemName}×{item.Value}]");
            _itemDisplay.Add(item.Key);
            num++;
        }
        sb.AppendLine("<0>でもどる|番号を入力しアイテム詳細");

        screen.Set(ScreenLayer.InputArea, sb.ToString());
        screen.Clear(ScreenLayer.Content);
        screen.RefreshUntil();
    }
    public void ValidInventoryView(Inventory inventory, ConditionContext conditionContext,
        IScreenProvider screen, IInputProvider input, IRandomProvider random)
    {
        Initialize(inventory, screen);

        while(true)
        {
            string? inputText = input.Input();
            if(string.IsNullOrEmpty(inputText) || !int.TryParse(inputText, out int inputNum))
            {
                screen.Set(ScreenLayer.Content, "入力が正しくありません");
            }
            else if (inputNum < 0 || inputNum > _itemDisplay.Count)
            {
                screen.Set(ScreenLayer.Content, "選択肢にない番号です");
            }
            else if (inputNum == 0)
            {
                break;
            }
            else
            {
                var itemId = _itemDisplay[inputNum - 1];
                bool canUse = ItemMasterData.TryUseItem(itemId, conditionContext, out ItemData itemData);
                TargetResolveResult result = TargetResolveResult.NullResult();
                if(canUse)
                {
                    result = _targetResolver.TargetResolve(itemData.ConditionData, conditionContext, itemData.TargetData);
                    canUse = (result.TargetCandidates.Count > 0);
                }
                string text = "<0>|もどる";
                text += (canUse) ? "<1>使用する" : "\033[9m<1>使用する\033[0m";

                screen.Set(ScreenLayer.InputArea, $"{itemData.ItemName}|アイテムの説明");
                screen.Append(ScreenLayer.InputArea, text);
                screen.RefreshUntil();

                while(true)
                {
                    string? t = input.Input();
                    if(string.IsNullOrEmpty(t) || !int.TryParse(t, out int n) || (n != 0 && n != 1))
                    {
                        screen.Set(ScreenLayer.Content, "入力が正しくありません");
                    }
                    else if(n == 0)
                    {
                        Initialize(inventory, screen);
                        break;
                    }
                    else if (n == 1 && !canUse)
                    {
                        screen.Set(ScreenLayer.Content, "そのアイテムは使えません");
                    }
                    else
                    {
                        var targets = _targetSelector.SelectingTargets(result);
                        if (targets is not SelectionSuccess<List<Entity>> success)
                        {
                            Initialize(inventory, screen);
                        }
                        else
                        {
                            UseItem(inventory, itemData, success.Value, random);
                            Initialize(inventory, screen);
                        }
                    }
                    screen.RefreshUntil();
                }
            }
            screen.RefreshUntil();
        }
    }

    private void UseItem(Inventory inventory, ItemData itemData, List<Entity> targets, IRandomProvider random)
    {
        foreach(Entity entity in targets)
        {
            EffectContent effectContent = new(entity, entity, null, _battleCalculator, random);
            foreach(var effect in itemData.ItemEffectData.ItemEffects)
            {
                var result = effect.ApplyEffect(effectContent, ActionSource.FromItem(itemData.ItemId));
            }
        }
        inventory.RemoveItem(itemData.ItemId, 1);
    }
}