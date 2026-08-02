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

    public MenuState CurrentMenuState => _currentMenuState;
    private MenuState _currentMenuState = MenuState.MainMenu;
    public bool IsClosed => _isClosed;
    private bool _isClosed = true;
    public Action<ISelectorRequest>? OpenSelector { get; set; } = null; 
    private Inventory _inventory = new();

    public ConditionContext ConditionContext => _conditionContext with { User = _currentMember};
    private ConditionContext _conditionContext = conditionContext;
    private Action? _onUseItem = null;
    protected override void Initialize(PartyController partyController)
    {
        _inventory = partyController.Inventory;
        
        base.Initialize(partyController);
    }
    public void OpenMenu(PartyController partyController)
    {
        ValidMenu(partyController);
        _isClosed = false;
    }
    public void HandleInput(int num)
    {
        switch (CurrentMenuState)
        {
            case MenuState.MainMenu:
                MainCommand(num); 
                break;
            case MenuState.Detail:
                DetailCommand(num);
                break;
        }
    }
    private void MainCommand(int num)
    {
        if(num < 0 || num >_commands.Count)
        {
            SelectErrorText(-2);
            return;
        }
        _commands[num].Execute.Invoke();
    }
    private void DetailCommand(int num)
    {
        if(num != 0 && num != 1)
        {
            SelectErrorText(-2);
            return;
        }
        else if(num == 0)
        {
            RefreshMainMenu();
        }
        else if (num == 1)
        {
            if (_onUseItem is null)
                _screen.Append(ScreenLayer.InputArea, "そのアイテムは使用できません");
            else 
                _onUseItem!.Invoke();
        }
    }
    public override void ValidMenu(PartyController partyController)
    {
        Initialize(partyController);
        RefreshMainMenu();
    }
    private void RefreshMainMenu()
    {
        SetCommandDict();
        _currentMenuState = MenuState.MainMenu;
        Render(_currentMember);
    }
    private void TryChangePage(int num)
    {
        if(ChangePage(num))
        {
            RefreshMainMenu();
        }
        else
        {
            _screen.Append(ScreenLayer.InputArea, "ページの変更に失敗しました。");
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
        _screen.RefreshInput(sb.ToString());
    }

    private void ValidItemDetail(GameId<IItemId> itemId)
    {
        _currentMenuState = MenuState.Detail;
        ItemData itemData = ItemMasterData.GetItemData(itemId);
        UseLessType useLessType =
            UseValidator.TryUseItem(ConditionContext, itemData, out var result);
        bool canUse = UseValidator.CanUse(useLessType);
        _onUseItem = (canUse) ? 
            () => RequestTargetSelector(result, itemData, ConditionContext.RandomProvider): null;
        RenderItemDetail(canUse, itemData);
    }
    private void RenderItemDetail(bool canUse, ItemData itemData)
    {
        StringBuilder sb = new();

        sb.AppendLine($"{itemData.ItemName}|アイテムの説明");

        string text = "<0>|もどる";
        text += (canUse) ? "<1>使用する" : "\033[9m<1>使用する\033[0m";
        sb.AppendLine(text);

        _screen.RefreshInput(sb.ToString());
    }
    private void SetCommandDict()
    {
        _commands.Clear();
        int num = 1;
        foreach(var item in _inventory.ItemInventory)
        {
            ItemData itemData = ItemMasterData.GetItemData(item.Key);
            string text = 
                ($"<{num}>|[({TextMasterData.GetCategoryText(itemData.ItemCategory)}){itemData.ItemName}×{item.Value}]");

            _commands[num] = new(text, item.Value, () => ValidItemDetail(item.Key));
            num++;
        }
        int i = 1;
        foreach(var member in _displayMembers)
        {
            string text = $"<{num}>|使用者を[{member.Name}]に変更";
            _commands[num] = new(text, 0, () => TryChangePage(i));
            i++;
            num++;
        }
        _commands[0] = new("<0>|もどる|番号を入力しアイテム詳細", 0, Close);
    }
    public void Close()
    {
        _isClosed = true;
    }
    private void RequestTargetSelector
        (TargetResolveResult result, ItemData itemData, IRandomProvider random)
    {
        RequestOpenSelector<List<Entity>> request = 
            new(_targetSelector,() => _targetSelector.Open(result),
            (targets) => UseItem(itemData, targets.Value, random), _ => ValidItemDetail(itemData.ItemId));
        OpenSelector?.Invoke(request);
    }
    private void UseItem(ItemData itemData, List<Entity> targets, IRandomProvider random)
    {
        foreach(Entity entity in targets)
        {
            EffectContent effectContent = new(_currentMember, entity, null, _battleCalculator, random);
            foreach(var effect in itemData.ItemEffectData.ItemEffects)
            {
                var result = effect.ApplyEffect(effectContent, ActionSource.FromItem(itemData.ItemId));
            }
        }
        _inventory.RemoveItem(itemData.ItemId, 1);
        if(_inventory.GetItemAmount(itemData.ItemId) > 0)
            ValidItemDetail(itemData.ItemId);
        else
        {
            RefreshMainMenu();
        }
    }

    public void UpdateCondition(ConditionContext conditionContext)
    {
        _conditionContext = conditionContext;
    }
}