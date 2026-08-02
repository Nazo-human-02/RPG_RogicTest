using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InventoryMenu(TargetSelector targetSelect, BattleCalculator battleCalculator, ConditionContext conditionContext,
    IScreenProvider screenProvider, IInputProvider inputProvider) :
    MemberMenuBase(inputProvider, screenProvider), IUpdateCondition, IMenu
{
    private readonly TargetSelector _targetSelector = targetSelect;
    private readonly BattleCalculator _battleCalculator = battleCalculator;

    private Dictionary<int, ItemCommand> _commands = new();
    private Inventory _inventory = new();

    public ConditionContext ConditionContext => _conditionContext with { User = _currentMember};
    private ConditionContext _conditionContext = conditionContext;
    protected override void Initialize(PartyController partyController)
    {
        _inventory = partyController.Inventory;
        
        base.Initialize(partyController);
    }
    public void OpenMenu(PartyController partyController)
    {
        ValidMenu(partyController);
    }
    public override void ValidMenu(PartyController partyController)
    {
        Initialize(partyController);
        SetCommandDict(partyController);
        Render(_currentMember);

        while(true)
        {
            string? inputText = _input.Input();
            if (string.IsNullOrEmpty(inputText) || !int.TryParse(inputText, out int inputNum))
            {
                _screen.Set(ScreenLayer.Content, "入力が正しくありません");
            }
            else if (inputNum == 0)
            {
                break;
            }
            else if(inputNum < 1 || inputNum > _commands.Count)
            {
                _screen.Set(ScreenLayer.Content, "選択肢にない番号です");
            }
            else
            {
                _commands[inputNum].Execute.Invoke();
                SetCommandDict(partyController);
                Render(_currentMember);
            }
            _screen.RefreshUntil();
        }
    }
    protected override void Render(Entity member)
    {
        StringBuilder sb = new();
        sb.AppendLine("=持ち物=");
        sb.AppendLine($"[使用者:{member.Name}]");
        foreach(var command in _commands)
        {
            sb.AppendLine(command.Value.Text);
        }
        sb.AppendLine("<0>でもどる|番号を入力しアイテム詳細");
        _screen.Set(ScreenLayer.InputArea, sb.ToString());
        _screen.Clear(ScreenLayer.Content);
        _screen.RefreshUntil();
    }

    private void ValidItemDetail(GameId<IItemId> itemId)
    {
        ItemData itemData = ItemMasterData.GetItemData(itemId);
        UseLessType useLessType =
            UseValidator.TryUseItem(ConditionContext, itemData, out var result);
        bool canUse = UseValidator.CanUse(useLessType);

        RenderItemDetail(canUse, itemData);
        while (true)
        {
            string? t = _input.Input();
            if (string.IsNullOrEmpty(t) || !int.TryParse(t, out int n) || (n != 0 && n != 1))
            {
                _screen.Set(ScreenLayer.Content, "入力が正しくありません");
            }
            else if (n == 0)
            {
                break;
            }
            else if (n == 1 && !canUse)
            {
                _screen.Set(ScreenLayer.Content, "そのアイテムは使えません");
            }
            else
            {
                var targets = _targetSelector.SelectingTargets(result);
                if (targets is SelectionSuccess<List<Entity>> success)
                {
                    UseItem(_inventory, itemData, success.Value, ConditionContext.RandomProvider);
                    break;
                }

                RenderItemDetail(canUse, itemData);
            }
            _screen.RefreshUntil();
        }
    }
    private void RenderItemDetail(bool canUse, ItemData itemData)
    {
        StringBuilder sb = new();

        sb.AppendLine($"{itemData.ItemName}|アイテムの説明");

        string text = "<0>|もどる";
        text += (canUse) ? "<1>使用する" : "\033[9m<1>使用する\033[0m";
        sb.AppendLine(text);

        _screen.Set(ScreenLayer.InputArea, sb.ToString());
        _screen.Clear(ScreenLayer.Content);
        _screen.RefreshUntil();
    }
    private void SetCommandDict(PartyController partyController)
    {
        _commands.Clear();
        int num = 1;
        foreach(var item in partyController.Inventory.ItemInventory)
        {
            ItemData itemData = ItemMasterData.GetItemData(item.Key);
            string text = 
                ($"<{num}>|[({TextMasterData.GetCategoryText(itemData.ItemCategory)}){itemData.ItemName}×{item.Value}]");

            _commands[num] = new(text, item.Value, () => ValidItemDetail(item.Key));
            num++;
        }
        int i = 1;
        foreach(var member in partyController.PartyMember)
        {
            string text = $"<{num}>|使用者を[{member.Name}]に変更";
            _commands[num] = new(text, 0, () => ChangePage(i));
            i++;
            num++;
        }
    }

    private void UseItem(Inventory inventory, ItemData itemData, List<Entity> targets, IRandomProvider random)
    {
        foreach(Entity entity in targets)
        {
            EffectContent effectContent = new(_currentMember, entity, null, _battleCalculator, random);
            foreach(var effect in itemData.ItemEffectData.ItemEffects)
            {
                var result = effect.ApplyEffect(effectContent, ActionSource.FromItem(itemData.ItemId));
            }
        }
        inventory.RemoveItem(itemData.ItemId, 1);
    }

    public void UpdateCondition(ConditionContext conditionContext)
    {
        _conditionContext = conditionContext;
    }
}

public record ItemCommand
(
    string Text,
    int Amount,
    Action Execute
);