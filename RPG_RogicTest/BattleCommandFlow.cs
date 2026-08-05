using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleCommandFlow
{
    public bool IsBusy => _isSelecting;
    private readonly BattleServices _battleServices;

    private Action<List<ActionUnit[]>>? _onComplete;
    private readonly Stack<Entity> _selectedHistory = new();
    private Stack<Entity> _awaitForSelectActors = new();
    private readonly List<ActionUnit[]> _createdActions = new();
    private bool _isSelecting = false;

    public BattleCommandFlow(BattleServices battleServices)
    {
        _battleServices = battleServices;
    }
    public void StartSelect(List<Entity> actors, ConditionContext conditionContext, 
        Action<List<ActionUnit[]>> onComplete)
    {
        _awaitForSelectActors = new(actors);
        _onComplete = onComplete;
        _selectedHistory.Clear();
        _createdActions.Clear();

        _isSelecting = true;

        SelectActor(conditionContext);
    }
    private void SelectActor(ConditionContext conditionContext)
    {
        if (_awaitForSelectActors.TryPop(out var actor))
        {
            if (actor is CharacterBase chara)
                _selectedHistory.Push(chara);
            CreateAction(actor, conditionContext);
        }
        else
        {
            _isSelecting = false;
            _onComplete?.Invoke(_createdActions);
        }
    }
    private void CreateAction(Entity entity, ConditionContext conditionContext)
    {
        CommandSelectionContext selectionContext = new(entity, conditionContext with { User = entity });
        SelectCommand(selectionContext);
    }
    private void SelectCommand(CommandSelectionContext selectionContext)
    {
        if (selectionContext.Actor is CharacterBase)
        {
            selectionContext.SelectFlow.Push(SelectStep.SelectCommand);
            _battleServices.BattleActionQueue.CreatePlayerCommand
                (selectionContext.ConditionContext, 
                (success) => OnCommandSelected(success, selectionContext),
                () => OnReturnPreviousActor(selectionContext.ConditionContext));
        }
        else if (selectionContext.Actor is EnemyCharacter)
        {
            //どうしようかな
        }
    }
    private void OnCommandSelected(ActionType actionType, CommandSelectionContext selectionContext)
    {
        selectionContext.SetActionType(actionType);
        switch (actionType)
        {
            case ActionType.Attack:
                OnDefaultAttack(selectionContext);
                break;

            case ActionType.Skill:
                SelectSkill(selectionContext);
                break;

            case ActionType.Guard:
                OnGuard(selectionContext);
                break;

            case ActionType.Escape:
                OnEscape(selectionContext);
                break;

            case ActionType.Item:
                SelectItem(selectionContext);
                break;
        }
    }
    private void SelectSkill(CommandSelectionContext selectionContext)
    {
        selectionContext.SelectFlow.Push(SelectStep.SelectSkill);
        _battleServices.BattleActionQueue.SelectSkill
            (selectionContext.ConditionContext,
            (select) => OnSelectedSkill(select, selectionContext), 
            () => OnCanceled(selectionContext));
    }
    private void SelectItem(CommandSelectionContext selectionContext)
    {
        selectionContext.SelectFlow.Push(SelectStep.SelectItem);
        _battleServices.BattleActionQueue.SelectItem
            (selectionContext.ConditionContext,(select) => OnSelectedItem(select, selectionContext),
            () => OnCanceled(selectionContext));
    }
    private void SelectTargets(CommandSelectionContext selectionContext)
    {
        selectionContext.SelectFlow.Push(SelectStep.SelectTargets);
        selectionContext.CheckResolveResult();
        _battleServices.BattleActionQueue.SelectTargets
            (selectionContext.TargetResolveResult,
            (targets) => OnSelectedTarget(targets, selectionContext), 
            () => OnCanceled(selectionContext));
    }
    private void OnCanceled(CommandSelectionContext selectionContext)
    {
        if(selectionContext.SelectFlow.TryPop(out _))
        {
            if(selectionContext.SelectFlow.TryPeek(out SelectStep step))
            {
                switch (step)
                {
                    case SelectStep.SelectCommand:
                        SelectCommand(selectionContext); break;
                    case SelectStep.SelectSkill:
                        SelectSkill(selectionContext); break;
                    case SelectStep.SelectItem:
                        SelectItem(selectionContext); break;
                    case SelectStep.SelectTargets:
                        SelectTargets(selectionContext); break;
                }
                return;
            }
        }
        OnReturnPreviousActor(selectionContext.ConditionContext);
    }
    private void OnDefaultAttack(CommandSelectionContext selectionContext)
    {
        Skill? defaultSkill = selectionContext.Actor.DefaultSkill;
        var result = (defaultSkill is null) ? 
            TargetResolver.GetTargetResolve
            (ConditionData.Default, selectionContext.ConditionContext, TargetData.SingleTarget) :
            TargetResolver.GetTargetResolve
            (defaultSkill.ConditionData, selectionContext.ConditionContext, defaultSkill.TargetData);
        selectionContext.SetResolveResult(result);
        selectionContext.SetActionSource(ActionSource.Default(defaultSkill));
        SelectTargets(selectionContext);
    }
    private void OnGuard(CommandSelectionContext selectionContext)
    {
        var result = TargetResolver.GetTargetResolve
            (ConditionData.Empty, selectionContext.ConditionContext, TargetData.Self);
        selectionContext.SetResolveResult(result);
        selectionContext.SetActionSource(ActionSource.Default());
        SelectTargets(selectionContext);
    }
    private void OnEscape(CommandSelectionContext selectionContext)
    {
        var result = TargetResolver.GetTargetResolve
            (ConditionData.Empty, selectionContext.ConditionContext, TargetData.Self);
        selectionContext.SetResolveResult(result);
        selectionContext.SetActionSource(ActionSource.Default());
        SelectTargets(selectionContext);
    }
    private void OnSelectedSkill(Skill skill, CommandSelectionContext selectionContext)
    {
        selectionContext.SetSkill(skill);
        var result =
            TargetResolver.GetTargetResolve(skill.ConditionData, selectionContext.ConditionContext, skill.TargetData);
        selectionContext.SetResolveResult(result);
        selectionContext.SetActionSource(ActionSource.Default(skill: skill));
        SelectTargets(selectionContext);
    }
    private void OnSelectedItem(SelectItemData selectItemData, CommandSelectionContext selectionContext)
    {
        UseItemInfo itemInfo = new() { ItemId = selectItemData.ItemId }; //アイテム関連はあとから要整理
        selectionContext.SetItem(itemInfo);
        selectionContext.SetResolveResult(selectItemData.TargetResolveResult);
        selectionContext.SetActionSource(ActionSource.Default(itemId: selectItemData.ItemId));
        SelectTargets(selectionContext);
    }
    private void OnSelectedTarget(List<Entity> targets, CommandSelectionContext selectionContext)
    {
        selectionContext.CheckActionSource();
        ActionUnit[] actions = ActionUnitCreator.GetActionUnit
            (selectionContext.ActionType, selectionContext.ActionSource,
            selectionContext.Actor, targets, selectionContext.Skill, selectionContext.ItemInfo);

        _createdActions.Add(actions);
        SelectActor(selectionContext.ConditionContext);
    }
    private void OnReturnPreviousActor(ConditionContext conditionContext) //前のプレイヤーキャラの選択に戻りたい
    {
        if (_selectedHistory.TryPop(out var previousEntity))
        {
            if (_selectedHistory.TryPeek(out var entity))
            {
                _awaitForSelectActors.Push(previousEntity);
                CreateAction(entity, conditionContext);
                return;
            }
            _selectedHistory.Push(previousEntity);
            CreateAction(previousEntity, conditionContext);
        }
    }
}

public class CommandSelectionContext(Entity actor, ConditionContext conditionContext)
{
    public readonly Stack<SelectStep> SelectFlow = new();
    public Entity Actor = actor;
    public ConditionContext ConditionContext = conditionContext;
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