using System;
using System.Data;

public class BattleSession(IReadOnlySet<CharacterBase> party, IReadOnlyList<EnemyCharacter> enemies)
{
    public IReadOnlySet<CharacterBase> Party { get; private set; } = party;
    public IReadOnlyList<EnemyCharacter> Enemies { get; private set; } = enemies;

    public List<CharacterBase> GetAliveParty() => Party.Where(p => !p.Stat.IsDead).ToList();
	public List<EnemyCharacter> GetAliveEnemy() => Enemies.Where(e => !e.Stat.IsDead).ToList();
	public List<Entity> GetAllEntity() => Party.Cast<Entity>().Concat(Enemies).ToList();

	public (bool,BattleResultType) IsBattleOver() //int=1で勝利、int=2で敗北
	{
		if(GetAliveParty().Count == 0)
		{
			return (true, BattleResultType.Defeat);
		}
        else if(GetAliveEnemy().Count == 0)
        {
			return (true, BattleResultType.Victory);
        }
//		else if(GetAliveEnemy().Count > 0 && GetAliveParty().Count > 0)
//		{
//			return (true, BattleResultType.Escape);
//		}
		else
		{
			return (false, BattleResultType.ContinueBattle);
		}
    }
}



public class BattleManager(ProvidorContext providorContext, BattleServices battleServices,
	BattleRuntimeContext battleRuntimeContext, PartyController partyController, BattleSession session, FieldContext fieldContext)
{
	private readonly ProvidorContext _providorContext = providorContext;
	private readonly BattleServices _battleServices = battleServices;
	private readonly BattleRuntimeContext _runtimeContext = battleRuntimeContext;
	private readonly PartyController _partyController = partyController;
	private readonly BattleSession _battleSession = session;
	private readonly ConditionContext _baseConditioncontext = 
		new(true, 0, null, null, partyController, session, fieldContext, providorContext.RandomProvider);

	public bool ExitRequested => _exitDungeon || _exitBattle;
	private bool _exitDungeon = false;
	private bool _exitBattle = false;

	private Action<ISelectorRequest>? _selectorOpenRequest;
	private BattleState _currentState;
	private int _currentTurn = 0;
    private void Dispose()
	{
		foreach (Entity party in _battleSession.Party)
		{ 
			party.Notifications.ClearNotify();
			party.ClearSkillCoolTime();
		}
		
	}
    public BattleResult BattleStart()
	{
		_providorContext.ScreenProvider.Set(ScreenLayer.Content, "戦闘開始");
		_providorContext.ScreenProvider.RefreshUntil(ScreenLayer.Content);
		_providorContext.ScreenProvider.WaitForEnter();

		BattleNotification.Initialize(_battleSession, this);

		bool isOver = false;
		BattleResultType resultType = BattleResultType.ContinueBattle;

        ExecuteNotify(Phase.StartBattle, null, null); //通知配布と実行

        _currentTurn = 1;
		var conditionContext = _baseConditioncontext with { CurrentTurn = _currentTurn };
        while (!isOver)
		{
			_providorContext.ScreenProvider.Set(ScreenLayer.Label, $"-----------{_currentTurn}ターン目---------------");
			_providorContext.ScreenProvider.Set
				(ScreenLayer.SubView, TextMasterData.GetEncounterEnemyText(_battleSession.GetAliveEnemy()));
			_providorContext.ScreenProvider.Clear(ScreenLayer.Content);
            _providorContext.ScreenProvider.RefreshUntil();

            conditionContext = _baseConditioncontext with { CurrentTurn = _currentTurn };
			BattleNotification.UpDateEntities();
			List<ActionUnit[]> enemyActions = _battleServices.BattleActionQueue.CreateEnemyActions(conditionContext);
			List<ActionUnit[]> playerActions = _battleServices.BattleActionQueue.CreatePlayerActions(conditionContext);
			var sortedActions = SortActionQueue(enemyActions.Concat(playerActions).ToList());
			_runtimeContext.Enqueue(sortedActions);

            ExecuteNotify(Phase.StartTurn, null, null); //通知配布と実行


            if (ExitRequested) //強制終了フラグ確認
				break;

            ExecuteNotify(Phase.EndTurn, null, null); //通知配布と実行


            (isOver, resultType) = _battleSession.IsBattleOver();
			_battleServices.ActionExecutor.ClearLogCache();

			//持続効果,スキルのターンを減少
			Tick();
			//

            _currentTurn++;

			_providorContext.ScreenProvider.WaitForEnter();
        }
        if (ExitRequested)
			resultType = BattleResultType.Escape;

		_providorContext.ScreenProvider.Clear(ScreenLayer.Label);
        _providorContext.ScreenProvider.Clear(ScreenLayer.SubView);

        var result = CheckBattleResult(resultType);

		ExecuteNotify(Phase.EndBattle, null, null); //通知配布と実行

		Dispose();
		if (resultType == BattleResultType.Victory) //報酬処理
		{
            var reward = _battleServices.BattleRewardCalculator.CalculateReward(_battleSession.Enemies);
            _partyController.GetReward(reward);
            _providorContext.ScreenProvider.RefreshUntil();
            _providorContext.ScreenProvider.WaitForEnter();
			_providorContext.ScreenProvider.Set(ScreenLayer.MainView, TextMasterData.GetPartyText(_partyController));

		}
		_providorContext.ScreenProvider.RefreshUntil();
		return result;
	}
	private void Tick()
	{
        foreach (Entity enemy in _battleSession.GetAliveEnemy()) enemy.Notifications.TickNotify();
        foreach (Entity party in _battleSession.GetAliveParty()) party.Notifications.TickNotify();
        foreach (Entity entity in _battleSession.GetAllEntity()) entity.ReduceSkillCoolTime();
    }
	private void ExecuteNotify(Phase phase, ActionUnit? actionUnit = null, Entity? target = null)
	{
        BattleNotification.TriggerPhase(Phase.StartBattle, actionUnit, target); //戦闘開始
        ExecuteActionUnit(_baseConditioncontext with 
		{CurrentTurn = _currentTurn, User = actionUnit?.Executor, Target = target}); //通知とセット
    }
    public Queue<ActionUnit[]> SortActionQueue(List<ActionUnit[]> actionUnits)
	{
		return _battleServices.TurnScheduler.ActionOrder(actionUnits);
	}

	public void ExecuteActionUnit(ConditionContext conditionContext)
	{
		while((!_runtimeContext.IsActionEmpty()) && !_battleSession.IsBattleOver().Item1)
		{
			if(!_runtimeContext.TryGetNextAction(out var currentAction))
			{
				break;
			}
			_battleServices.ActionExecutor.ExecuteAction(currentAction, this, conditionContext);

            _providorContext.ScreenProvider.Set(ScreenLayer.MainView, TextMasterData.GetPartyText(_partyController));
			_providorContext.ScreenProvider.RefreshUntil();
        }
    }

	private BattleResult CheckBattleResult(BattleResultType resultType)
	{
		switch (resultType)
		{
			case BattleResultType.Victory:
                //_providorContext.LogProvider.WriteLog("_戦闘に勝利した!_");
				_providorContext.ScreenProvider.Set(ScreenLayer.Content, "_戦闘に勝利した!_");
				_providorContext.ScreenProvider.RefreshUntil(ScreenLayer.Content);
				_providorContext.ScreenProvider.WaitForEnter();
                return new BattleResult(resultType, _exitDungeon);

			case BattleResultType.Defeat:
                //_providorContext.LogProvider.WriteLog("_戦闘に敗北した..._");
				_providorContext.ScreenProvider.Set(ScreenLayer.Content, "_戦闘に敗北した..._");
				_providorContext.ScreenProvider.RefreshUntil(ScreenLayer.Content);
				_providorContext.ScreenProvider.WaitForEnter();
                return new BattleResult(resultType, true);
				
			case BattleResultType.Escape:
				//_providorContext.LogProvider.WriteLog("_戦闘から逃げ出した");
				_providorContext.ScreenProvider.Set(ScreenLayer.Content, "_戦闘から逃げ出した");
				_providorContext.ScreenProvider.RefreshUntil(ScreenLayer.Content);
				_providorContext.ScreenProvider.WaitForEnter();
                return new BattleResult(resultType, _exitDungeon);
				
			default:
				//_providorContext.LogProvider.WriteLog("想定外の結果");
				_providorContext.ScreenProvider.Set(ScreenLayer.Content, "想定外の結果");
				_providorContext.ScreenProvider.RefreshUntil(ScreenLayer.Content);
				_providorContext.ScreenProvider.WaitForEnter();
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

