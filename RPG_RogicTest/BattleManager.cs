using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;

public class BattleManager(ProvidorContext providorContext, BattleServices battleServices,
	BattleRuntimeContext battleRuntimeContext, PartyController partyController, BattleSession session, FieldContext fieldContext)
{
	private readonly BattleServices _battleServices = battleServices;
	private readonly BattleRuntimeContext _runtimeContext = battleRuntimeContext;
	private readonly PartyController _partyController = partyController;
	private readonly BattleSession _battleSession = session;
    private BattleCommandFlow? _battleCommandFlow = null!;
    private readonly ConditionContext _baseConditioncontext = 
		new(true, 0, null, null, partyController, session, fieldContext, providorContext.RandomProvider);
	private ConditionContext CurrentCondition(Entity? user = null, Entity? target = null)
		=> _baseConditioncontext with {User = user, Target = target, CurrentTurn = _currentTurn };
	public bool ExitRequested => _exitDungeon || _exitBattle;
	private bool _exitDungeon = false;
	private bool _exitBattle = false;


	private Action<ISelectorRequest>? _selectorOpenRequest;
	private Action<BattleResult>? _onBattleFinished;

	private BattleState _currentState;
	private readonly Stack<BattleState> _stateFlow = new();
	private int _currentTurn = 0;
	private BattleResultType _resultType = BattleResultType.ContinueBattle;
	private bool _isActing = false;
	private readonly List<ActionUnit[]> _createdActions = new();
	private BattleResult? _battleResult;
    private void Dispose()
	{
		foreach (Entity party in _battleSession.Party)
		{ 
			party.Notifications.ClearNotify();
			party.ClearSkillCoolTime();
		}
		
	}
	private void UpdateState(BattleState state)
	{
		_stateFlow.Push(_currentState);
		_currentState = state;
	}
	public void Initialize(Action<ISelectorRequest> openRequest, Action<BattleResult> onBattleFinished)
	{
		_selectorOpenRequest = openRequest;
		_onBattleFinished = onBattleFinished;
		Reset();
	}
	private void Reset()
	{
        _stateFlow.Clear();
        _createdActions.Clear();
        _currentState = BattleState.BattleStart;
		_currentTurn = 0;
		_exitBattle = false;
		_exitDungeon = false;
		_isActing = false;
		_battleResult = null;
    }
	public void NextState() //状態遷移用
	{
		if(_battleCommandFlow is not null && _battleCommandFlow.IsBusy)
			{ return; }
		switch (_currentState)
		{
			case BattleState.BattleStart:
				BattleStart();break;

			case BattleState.UpdateBattleCondition:
				UpdateBattleCondition(); break;

			case BattleState.CreateActorAction:
				CreateAction(); break;

			case BattleState.SetActionSchedule:
				SetActionSchedule(); break;

			case BattleState.TurnStart:
				TurnStart(); break;

			case BattleState.Action:
				ExecuteAction(); break;

			case BattleState.TurnEnd:
				TurnEnd(); break;

			case BattleState.BattleEnd:
				BattleEnd(); break;

			case BattleState.RewardProcess:
				RewardProcess(); break;
		}
	}
	private void BattleStart()
	{
		_battleServices.BattleScreenController.BattleStart();
		_battleServices.BattleActionQueue.Initialize(_selectorOpenRequest!);
		_battleCommandFlow = new(_battleServices);
        BattleNotification.Initialize(_battleSession, this);
		BattleNotification.TriggerPhase(Phase.StartBattle, null, null);

		UpdateState(BattleState.Action);
    }
	private void UpdateBattleCondition()
	{
		(bool isEnd, BattleResultType resultType) = _battleSession.IsBattleOver();
		if(isEnd || ExitRequested)
		{
			_resultType = (ExitRequested) ? BattleResultType.Escape : resultType;
			UpdateState(BattleState.BattleEnd);
		}
		else
		{
			if(_isActing)
			{
				UpdateState(BattleState.Action);
			}
			else if(_createdActions.Count == 0)
			{
				UpdateState(BattleState.CreateActorAction);
			}
			else
			{
				UpdateState(BattleState.Action);
			}
		}
	}
	private void CreateAction()
	{
		_battleCommandFlow!.StartSelect(_battleSession.GetAllAliveEntity(), CurrentCondition(), 
			(actions) => OnCompleted(actions));
	}
	private void OnCompleted(List<ActionUnit[]> actionUnits)
	{
		_createdActions.AddRange(actionUnits);
		UpdateState(BattleState.SetActionSchedule);
	}
	private void SetActionSchedule()
	{
        _runtimeContext.Enqueue(SortActionQueue(_createdActions));
		_createdActions.Clear();
        UpdateState(BattleState.Action);
	}
	private void TurnStart()
	{
        _currentTurn++;
        BattleNotification.UpDateEntities();
        _battleServices.BattleScreenController.
			TurnStart(_currentTurn, TextMasterData.GetEncounterEnemyText(_battleSession.GetAliveEnemy()));
		BattleNotification.TriggerPhase(Phase.StartTurn, null, null);
		UpdateState(BattleState.Action);
    }
	private void ExecuteAction()
	{
		if (TryExecuteActionUnit(out var currentAction))
		{
			_isActing = true;
			_battleServices.ActionExecutor.ExecuteAction(currentAction!, this, CurrentCondition());
			_battleServices.BattleScreenController.UpdatePartyText(_partyController);

			UpdateState(BattleState.UpdateBattleCondition);
		}
		else if (_stateFlow.TryPeek(out var state))
		{
			_isActing = false;
			if (state is BattleState.TurnStart)
				UpdateState(BattleState.SetActionSchedule);

			else if (state is BattleState.TurnEnd || state is BattleState.BattleStart)
				UpdateState(BattleState.UpdateBattleCondition);

			else if (state is BattleState.UpdateBattleCondition)
				UpdateState(BattleState.TurnEnd);

			else if (state is BattleState.BattleEnd)
				UpdateState(BattleState.RewardProcess);

			else
				UpdateState(BattleState.TurnEnd);
		}
		else
		{
            _isActing = false;
            UpdateState(BattleState.TurnEnd);
		}
    }
	private void TurnEnd()
	{
		BattleNotification.TriggerPhase(Phase.EndTurn, null, null);
        _battleServices.ActionExecutor.ClearLogCache();
        Tick();

		UpdateState(BattleState.Action);
		_battleServices.BattleScreenController.RefreshAndWait();
    }
	private void BattleEnd()
	{
        _battleServices.BattleScreenController.Clear(ScreenLayer.Label);
        _battleServices.BattleScreenController.Clear(ScreenLayer.SubView);
        _battleResult = CreateBattleResult(_resultType);
        BattleNotification.TriggerPhase(Phase.EndBattle, null, null); //通知配布と実行
		UpdateState(BattleState.Action);
    }

	private void RewardProcess()
	{
        Dispose();
        if (_resultType == BattleResultType.Victory) //報酬処理
        {
            var reward = 
				_battleServices.BattleRewardProcessor.ProcessReward
				(_battleSession.Enemies, _partyController, _baseConditioncontext.FieldContext);
            _partyController.GetReward(reward);
			_battleServices.BattleScreenController.RefreshAndWait();
        }
        _battleServices.BattleScreenController.UpdatePartyText(_partyController);
		_onBattleFinished!.Invoke(_battleResult!);
		Reset();
    }
	private void Tick()
	{
		foreach(Entity entity in _battleSession.GetAllEntity())
		{
			entity.ReduceSkillCoolTime();
			if (!entity.Stat.IsDead)
				entity.Notifications.TickNotify();	
		}
    }
    private Queue<ActionUnit[]> SortActionQueue(List<ActionUnit[]> actionUnits)
	{
		return _battleServices.TurnScheduler.ActionOrder(actionUnits);
	}

	public bool TryExecuteActionUnit(out ActionUnit[]? action)
	{
		action = null;
		if(_runtimeContext.IsActionEmpty() || _battleSession.IsBattleOver().Item1)
		{
			return false;
		}
		if(_runtimeContext.TryGetNextAction(out var currentAction))
		{
			action = currentAction;
			return true;
        }
		return false;
    }

	private BattleResult CreateBattleResult(BattleResultType resultType)
	{
        _battleServices.BattleScreenController.ResultText(resultType);
        switch (resultType)
		{
			case BattleResultType.Victory:
                return new BattleResult(resultType, _exitDungeon);

			case BattleResultType.Defeat:
                return new BattleResult(resultType, true);
				
			case BattleResultType.Escape:
                return new BattleResult(resultType, _exitDungeon);
				
			default:
                return new BattleResult(resultType, true);				
        }
    }

    public void InsertInterruptAction(ActionUnit interruptAction)
    {
        _runtimeContext.EnqueueInterrupt([interruptAction]);
    }

    public void StackInterruptAction(ActionUnit interruptAction, int num)
	{
		_runtimeContext.StackAction((interruptAction, num));
	}

	public void RequestExitDungeon()
	{
		_exitDungeon = true;
	}

	public void RequestExitBattle()
	{
		_exitBattle = true;
	}
}

