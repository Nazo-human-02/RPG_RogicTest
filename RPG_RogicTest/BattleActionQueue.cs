using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleActionQueue(GameSelectionService gameSelection)
{
    private readonly CommandSelect _commandSelector = gameSelection.CommandSelect;
    private readonly SkillSelection _skillSelector = gameSelection.SkillSelection;
    private readonly UseItemSelecter _itemSelector = gameSelection.UseItemSelecter;
    private readonly TargetSelector _targetSelector = gameSelection.TargetSelect;

    private Action<ISelectorRequest>? _openRequest;
    public void Initialize(Action<ISelectorRequest> openRequest)
    {
        _openRequest = openRequest;
    }
    public void CreatePlayerCommand(ConditionContext conditionContext, Action<ActionType> onSelected, Action onCanceled)
    {
        RequestOpenSelector<ActionType> request = 
            new(_commandSelector, 
            () => _commandSelector.Open(conditionContext.User!), 
            (success) => onSelected(success.Value),
            _ => onCanceled());
        _openRequest?.Invoke(request);
    }
    public void SelectSkill(ConditionContext conditionContext, Action<Skill> onSelected, Action onCanceled)
    {
        RequestOpenSelector<Skill> request =
            new(_skillSelector, () => _skillSelector.Open(conditionContext.User!), 
            (success) => onSelected(success.Value),_ => onCanceled());
        _openRequest?.Invoke(request);
    }
    public void SelectItem(ConditionContext conditionContext, Action<SelectItemData> onSelected, Action onCanceled)
    {
        RequestOpenSelector<SelectItemData> request =
            new(_itemSelector,
            () => _itemSelector.Open(conditionContext.PartyController.Inventory.ItemInventory, conditionContext), 
            (success) => onSelected(success.Value), _ => onCanceled());
        _openRequest?.Invoke(request);
    }
    public void SelectTargets(TargetResolveResult resolveResult, Action<List<Entity>> onSelected, Action onCanceled)
    {
        RequestOpenSelector<List<Entity>> request =
            new(_targetSelector, () => _targetSelector.Open(resolveResult), 
            (success) => onSelected(success.Value), _ => onCanceled());
        _openRequest?.Invoke(request);
    }
}

public record GameSelectionService
(
    CommandSelect CommandSelect,
    TargetSelector TargetSelect,
    SkillSelection SkillSelection,
    UseItemSelecter UseItemSelecter
);


public record ConditionContext
(
    bool IsBattle,
    int CurrentTurn,

    Entity? User,
    Entity? Target,

    PartyController PartyController,
    BattleSession? BattleSession,


    FieldContext FieldContext,

    IRandomProvider RandomProvider
);
