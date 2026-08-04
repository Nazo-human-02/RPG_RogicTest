using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleCommandFlow
{
    private readonly BattleServices _battleServices;

    private CommandSelectionContext? _actionSourceInfo;

    private Action<ActionUnit[]>? _onComplete;
    private Action? _onCancelToPreviousActor;
    private Stack<SelectStep> _selectFlow = new();

    public BattleCommandFlow(BattleServices battleServices)
    {
        _battleServices = battleServices;
    }
    public void StartSelect(Entity actor, ConditionContext conditionContext, 
        Action<ActionUnit[]> onComplete, Action onCanceled)
    {
        _actionSourceInfo = new(actor);
        _onComplete = onComplete;
        _onCancelToPreviousActor = onCanceled;
        _selectFlow.Clear();
        SelectCommand(conditionContext);
    }

    private void SelectCommand(ConditionContext conditionContext)
    {
        if (_actionSourceInfo!.Actor is CharacterBase)
        {
            _selectFlow.Push(SelectStep.SelectCommand);
            _battleServices.BattleActionQueue.CreatePlayerCommand
                (conditionContext, (success) => OnCommandSelected(success, conditionContext), _onCancelToPreviousActor!);
        }
        else if (_actionSourceInfo.Actor is EnemyCharacter)
        {
            //どうしようかな
        }
    }
    private void OnCommandSelected(ActionType actionType, ConditionContext conditionContext)
    {
        _actionSourceInfo!.SetActionType(actionType);
        switch (actionType)
        {
            case ActionType.Attack:
                OnDefaultAttack(conditionContext);
                break;

            case ActionType.Skill:
                SelectSkill(conditionContext);
                break;

            case ActionType.Guard:
                OnGuard(conditionContext);
                break;

            case ActionType.Escape:
                OnEscape(conditionContext);
                break;

            case ActionType.Item:
                SelectItem(conditionContext);
                break;
        }
    }
    private void SelectSkill(ConditionContext conditionContext)
    {
        _selectFlow.Push(SelectStep.SelectSkill);
        _battleServices.BattleActionQueue.SelectSkill
            (conditionContext,(select) => OnSelectedSkill(select, conditionContext), () => OnCanceled(conditionContext));
    }
    private void SelectItem(ConditionContext conditionContext)
    {
        _selectFlow.Push(SelectStep.SelectItem);
        _battleServices.BattleActionQueue.SelectItem
            (conditionContext,(select) => OnSelectedItem(select, conditionContext), () => OnCanceled(conditionContext));
    }
    private void SelectTargets(ConditionContext conditionContext)
    {
        _selectFlow.Push(SelectStep.SelectTargets);
        _battleServices.BattleActionQueue.SelectTargets
            (_actionSourceInfo!.TargetResolveResult!, OnSelectedTarget, () => OnCanceled(conditionContext));
    }
    private void OnCanceled(ConditionContext conditionContext)
    {
        if(_selectFlow.TryPop(out _))
        {
            if(_selectFlow.TryPeek(out SelectStep step))
            {
                switch (step)
                {
                    case SelectStep.SelectCommand:
                        SelectCommand(conditionContext); break;
                    case SelectStep.SelectSkill:
                        SelectSkill(conditionContext); break;
                    case SelectStep.SelectItem:
                        SelectItem(conditionContext); break;
                    case SelectStep.SelectTargets:
                        SelectTargets(conditionContext); break;
                }
                return;
            }
        }
        _onCancelToPreviousActor?.Invoke();
    }
    private void OnDefaultAttack(ConditionContext conditionContext)
    {
        Skill? defaultSkill = _actionSourceInfo!.Actor.DefaultSkill;
        var result = (defaultSkill is null) ?
            TargetResolver.GetTargetResolve
            (ConditionData.Default, conditionContext, TargetData.SingleTarget) :
            TargetResolver.GetTargetResolve
            (defaultSkill.ConditionData, conditionContext, defaultSkill.TargetData);
        _actionSourceInfo.SetResolveResult(result);
        _actionSourceInfo.SetActionSource(ActionSource.Default(defaultSkill));
        SelectTargets(conditionContext);
    }
    private void OnGuard(ConditionContext conditionContext)
    {
        var result = TargetResolver.GetTargetResolve
            (ConditionData.Empty, conditionContext, TargetData.Self);
        _actionSourceInfo!.SetResolveResult(result);
        _actionSourceInfo.SetActionSource(ActionSource.Default());
        SelectTargets(conditionContext);
    }
    private void OnEscape(ConditionContext conditionContext)
    {
        var result = TargetResolver.GetTargetResolve
            (ConditionData.Empty, conditionContext, TargetData.Self);
        _actionSourceInfo!.SetResolveResult(result);
        _actionSourceInfo.SetActionSource(ActionSource.Default());
        SelectTargets(conditionContext);
    }
    private void OnSelectedSkill(Skill skill, ConditionContext conditionContext)
    {
        _actionSourceInfo!.SetSkill(skill);
        var result =
            TargetResolver.GetTargetResolve(skill.ConditionData, conditionContext, skill.TargetData);
        _actionSourceInfo.SetResolveResult(result);
        _actionSourceInfo.SetActionSource(ActionSource.Default(skill: skill));
        SelectTargets(conditionContext);
    }
    private void OnSelectedItem(SelectItemData selectItemData, ConditionContext conditionContext)
    {
        UseItemInfo itemInfo = new() { ItemId = selectItemData.ItemId }; //アイテム関連はあとから要整理
        _actionSourceInfo!.SetItem(itemInfo);
        _actionSourceInfo.SetResolveResult(selectItemData.TargetResolveResult);
        _actionSourceInfo.SetActionSource(ActionSource.Default(itemId: selectItemData.ItemId));
        SelectTargets(conditionContext);
    }
    private void OnSelectedTarget(List<Entity> targets)
    {
        ActionUnit[] actions = ActionUnitCreator.GetActionUnit
            (_actionSourceInfo!.ActionType, _actionSourceInfo.ActionSource!,
            _actionSourceInfo.Actor, targets, _actionSourceInfo.Skill, _actionSourceInfo.ItemInfo);

        _onComplete!.Invoke(actions);
    }
}

public class CommandSelectionContext(Entity actor)
{
    public Entity Actor = actor;
    public ActionType ActionType { get; private set; }
    public TargetResolveResult? TargetResolveResult { get; private set; }
    public Skill? Skill { get; private set; }
    public UseItemInfo? ItemInfo { get; private set; }
    public ActionSource? ActionSource { get; private set; }
    public void SetActionType(ActionType actionType)
        => ActionType = actionType;
    public void SetResolveResult(TargetResolveResult targetResolveResult)
        => TargetResolveResult = targetResolveResult;
    public void SetSkill(Skill skill)
        => Skill = skill;
    public void SetItem(UseItemInfo selectItemData)
        => ItemInfo = selectItemData;
    public void SetActionSource(ActionSource actionSource)
        => ActionSource = actionSource;
    [MemberNotNull(nameof(TargetResolveResult))]
    public void CheckResolveResult()
    {
        if (TargetResolveResult is null)
        {
            throw new ArgumentNullException(nameof(TargetResolveResult));
        }
    }
    [MemberNotNull(nameof(Skill))]
    public void CheckSkill()
    {
        if (Skill is null)
        {
            throw new ArgumentNullException(nameof(Skill));
        }
    }
    [MemberNotNull(nameof(ItemInfo))]
    public void CheckItemData()
    {
        if (ItemInfo is null)
        {
            throw new ArgumentNullException(nameof(ItemInfo));
        }
    }
    [MemberNotNull(nameof(ActionSource))]
    public void CheckActionSource()
    {
        if (ActionSource is null)
        {
            throw new ArgumentNullException(nameof(ActionSource));
        }
    }

}