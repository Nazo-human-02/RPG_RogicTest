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
    public List<ActionUnit[]> CreateEnemyActions(ConditionContext conditionContext)
    {
        List<ActionUnit[]> actionUnits = new();
        foreach(var enemy in conditionContext.BattleSession!.GetAliveEnemy())
        {
            var target = GetHeadTarget(conditionContext.BattleSession.GetAliveParty().Cast<Entity>().ToList());
            if(target == null)
                continue;
            Guid guid = Guid.NewGuid();
            ActionUnit actionUnit = new ActionUnit(ActionType.Attack, ActionSource.Default, enemy, target, guid:guid);
            actionUnits.Add([actionUnit]);
        }
        return actionUnits;
    }
    public void CreatePlayerCommand(ConditionContext conditionContext, Action<ActionType> onSelected, Action onCanceled)
    {
        RequestOpenSelector<ActionType> request = 
            new(_commandSelector, 
            () => _commandSelector.Open(conditionContext.User!), 
            (success) => onSelected(success.Value),
            _ => onCanceled());
    }
    public void SelectSkill(ConditionContext conditionContext, Action<Skill> onSelected, Action onCanceled)
    {
        RequestOpenSelector<Skill> request =
            new(_skillSelector, () => _skillSelector.Open(conditionContext.User!), 
            (success) => onSelected(success.Value),_ => onCanceled());
    }
    public void SelectItem(ConditionContext conditionContext, Action<SelectItemData> onSelected, Action onCanceled)
    {
        RequestOpenSelector<SelectItemData> request =
            new(_itemSelector,
            () => _itemSelector.Open(conditionContext.PartyController.Inventory.ItemInventory, conditionContext), 
            (success) => onSelected(success.Value), _ => onCanceled());
    }
    public void SelectTargets(TargetResolveResult resolveResult, Action<List<Entity>> onSelected, Action onCanceled)
    {
        RequestOpenSelector<List<Entity>> request =
            new(_targetSelector, () => _targetSelector.Open(resolveResult), 
            (success) => onSelected(success.Value), _ => onCanceled());
    }
    public List<ActionUnit[]> CreatePlayerActions(ConditionContext conditionContext)
    {
        _commandSelector.InitializeCommand();
        List<ActionUnit[]> actionUnits = new();
        foreach(var member in conditionContext.BattleSession!.GetAliveParty())
        {
            var target = GetHeadTarget(conditionContext.BattleSession.GetAliveEnemy().Cast<Entity>().ToList());
            if(target == null)
                continue;
            while (true)
            {
                ActionType actionType = _commandSelector.WaitCommandSelect(member);
                var units = GetActionUnits(actionType, member, conditionContext with { User = member });
                if (units == null || units.Length == 0)
                    continue;
                else
                {
                    actionUnits.Add(units);
                    break;
                }
            }
        }
        return actionUnits;
    }

    private Entity? GetHeadTarget(List<Entity> entities) //仮置き
    {
        return entities.FirstOrDefault();
    }

    private ActionUnit[]? GetActionUnits(ActionType actionType, Entity actor, ConditionContext conditionContext)
    {
        return (actionType) switch
        { 
            ActionType.Attack => ActionTypeAttack(actor, conditionContext),
            ActionType.Skill => ActionTypeSkill(actor, conditionContext),
            ActionType.UseItem => ActionTypeItem(actor, conditionContext),
            ActionType.Guard => ActionTypeGuard(actor, conditionContext),
            ActionType.Escape => ActionTypeEscape(actor, conditionContext),
            _ => throw new NotImplementedException("アクションタイプ:例外")

        };
    }
    private ActionUnit[]? ActionTypeAttack(Entity actor, ConditionContext conditionContext, ConditionData? conditionData = null)
    {
        ConditionData condition =
            (conditionData != null) ? conditionData : ConditionData.Default;
        TargetData targetData = new TargetData(TargetType.Enemy, TargetSelectType.Self, 1);
        TargetResolveResult resolveResult = TargetResolver.GetTargetResolve(condition, conditionContext, targetData);
        var result = _targetSelector.SelectingTargets(resolveResult);
        if (result is not SelectionSuccess<List<Entity>> targets || targets.Value.Count == 0)
        {
            return null;
        }
        ActionUnit action = new(ActionType.Attack, ActionSource.Default, actor, targets.Value.First());
        return [action];
    }
    private ActionUnit[]? ActionTypeSkill(Entity actor, ConditionContext conditionContext)
    {
        while (true)
        {
            var skill = SelectUseSkill(actor);
            if (skill is not SelectionSuccess<Skill> success)
                return null; //commandSelectに戻る
            SelectionResult<List<Entity>> result = SelectTargets(actor, success.Value, conditionContext);
            if (result is not SelectionSuccess<List<Entity>> targets || targets.Value.Count == 0)
            {
                continue; //skillSelectに戻る
            }
            return ActionUnitCreator.GetActionUnit(ActionType.Skill, ActionSource.FromSkill(success.Value), 
                actor, targets.Value, success.Value);
            
        }
    }

    private ActionUnit[]? ActionTypeItem(Entity actor, ConditionContext conditionContext)
    {
        while (true)
        {
            var result = _itemSelector.SelectingItem
                (conditionContext.PartyController.Inventory.ItemInventory, conditionContext);
            if (result is not SelectionSuccess<SelectItemData> success)
            {
                return null; //commandSelectに戻る
            }
            var targetResult = _targetSelector.SelectingTargets(success.Value.TargetResolveResult);
            if (targetResult is not SelectionSuccess<List<Entity>> targets || targets.Value.Count == 0)
            {
                continue; //itemSelectに戻る
            }
            UseItemInfo useInfo = new UseItemInfo() { ItemId = success.Value.ItemId };
            return ActionUnitCreator.GetActionUnit(ActionType.UseItem, ActionSource.Default, actor, targets.Value, useItemInfo: useInfo);
        }
    }

    private ActionUnit[] ActionTypeGuard(Entity actor, ConditionContext conditionContext)
    {
        ActionUnit actionUnit = new ActionUnit(ActionType.Guard, ActionSource.Default, actor, actor);
        return [actionUnit];
    }

    private ActionUnit[] ActionTypeEscape(Entity actor, ConditionContext conditionContext)
    {
        ActionUnit actionUnit = new(ActionType.Escape, ActionSource.Default, actor, actor);
        return [actionUnit];
    }
    private SelectionResult<Skill> SelectUseSkill(Entity entity)
    {
        return _skillSelector.SkillSelect(entity);
    }

    private SelectionResult<List<Entity>> SelectTargets(Entity entity, Skill skill, ConditionContext conditionContext)
    {
        var resolveResult = 
            TargetResolver.GetTargetResolve(skill.ConditionData, conditionContext with {User = entity }, skill.TargetData);
        return _targetSelector.SelectingTargets(resolveResult);
    }
    private SelectionResult<List<Entity>> SelectTargets(TargetResolveResult targetResolveResult)
    {
        return _targetSelector.SelectingTargets(targetResolveResult);
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
