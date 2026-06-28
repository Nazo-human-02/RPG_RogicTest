using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleActionQueue(GameSelectionService gameSelection)
{
    private readonly CommandSelect _commandSelect = gameSelection.CommandSelect;
    private readonly SkillSelection _skillSelection = gameSelection.SkillSelection;
    private readonly UseItemSelecter _itemSelector = gameSelection.UseItemSelecter;
    private readonly TargetSelect _targetSelect = gameSelection.TargetSelect;
    private readonly TargetResolver _targetResolver = gameSelection.TargetResolver;
    public List<ActionUnit[]> CreateEnemyActions(ConditionContext conditionContext)
    {
        List<ActionUnit[]> actionUnits = new();
        foreach(var enemy in conditionContext.BattleSession!.GetAliveEnemy())
        {
            var target = GetHeadTarget(conditionContext.BattleSession.GetAliveParty().Cast<Entity>().ToList());
            if(target == null)
                continue;
            Guid guid = Guid.NewGuid();
            ActionUnit actionUnit = new ActionUnit(ActionType.Attack, enemy, target, guid:guid);
            actionUnits.Add([actionUnit]);
        }
        return actionUnits;
    }

    public List<ActionUnit[]> CreatePlayerActions(ConditionContext conditionContext)
    {
        _commandSelect.InitializeCommand();
        List<ActionUnit[]> actionUnits = new();
        foreach(var member in conditionContext.BattleSession!.GetAliveParty())
        {
            var target = GetHeadTarget(conditionContext.BattleSession.GetAliveEnemy().Cast<Entity>().ToList());
            if(target == null)
                continue;
            while (true)
            {
                ActionType actionType = _commandSelect.WaitCommandSelect(member);
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
            ActionType.Item => ActionTypeItem(actor, conditionContext),
            ActionType.Guard => ActionTypeGuard(actor, conditionContext),
            ActionType.Escape => ActionTypeEscape(actor, conditionContext),
            _ => throw new NotImplementedException("アクションタイプ:例外")

        };
    }
    private ActionUnit[]? ActionTypeAttack(Entity actor, ConditionContext conditionContext, ConditionData? conditionData = null)
    {
        ConditionData condition = (conditionData != null) ? conditionData : ConditionData.Empty;
        TargetData targetData = new TargetData(TargetType.Enemy, TargetSelectType.Self, 1);
        TargetResolveResult resolveResult = _targetResolver.TargetResolve(condition, conditionContext, targetData);
        var result = _targetSelect.SelectingTargets(resolveResult);
        if (result is not SelectionSuccess<List<Entity>> targets || targets.Value.Count == 0)
        {
            return null;
        }
        ActionUnit action = new(ActionType.Attack, actor, targets.Value.First());
        return [action];
    }
    private ActionUnit[]? ActionTypeSkill(Entity actor, ConditionContext conditionContext)
    {
        while (true)
        {
            var skill = SelectUseSkill(actor);
            if (skill is not SelectionSuccess<Skill> success)
                return null; //commandSelectに戻る
            SelectionResult<List<Entity>> result = SelectTargets(actor, success.Value, conditionContext.BattleSession!);
            if (result is not SelectionSuccess<List<Entity>> targets || targets.Value.Count == 0)
            {
                continue; //skillSelectに戻る
            }
            return ActionUnitCreator.GetActionUnit(ActionType.Skill, actor, targets.Value, success.Value);
            
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
            var targetResult = _targetSelect.SelectingTargets(success.Value.TargetResolveResult);
            if (targetResult is not SelectionSuccess<List<Entity>> targets || targets.Value.Count == 0)
            {
                continue; //itemSelectに戻る
            }
            UseItemInfo useInfo = new UseItemInfo() { ItemId = success.Value.ItemId };
            return ActionUnitCreator.GetActionUnit(ActionType.Item, actor, targets.Value, useItemInfo: useInfo);
        }
    }

    private ActionUnit[] ActionTypeGuard(Entity actor, ConditionContext conditionContext)
    {
        ActionUnit actionUnit = new ActionUnit(ActionType.Guard, actor, actor);
        return [actionUnit];
    }

    private ActionUnit[] ActionTypeEscape(Entity actor, ConditionContext conditionContext)
    {
        ActionUnit actionUnit = new(ActionType.Escape, actor, actor);
        return [actionUnit];
    }
    private SelectionResult<Skill> SelectUseSkill(Entity entity)
    {
        return _skillSelection.SkillSelect(entity);
    }

    private SelectionResult<List<Entity>> SelectTargets(Entity entity, Skill skill, BattleSession battleSession)
    {
        return _targetSelect.SelectingTargets(entity, battleSession, skill.TargetType, skill.TargetAmount);
    }
    private SelectionResult<List<Entity>> SelectTargets(TargetResolveResult targetResolveResult)
    {
        return _targetSelect.SelectingTargets(targetResolveResult);
    }
}

public record GameSelectionService
(
    CommandSelect CommandSelect,
    TargetSelect TargetSelect,
    TargetResolver TargetResolver,
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
