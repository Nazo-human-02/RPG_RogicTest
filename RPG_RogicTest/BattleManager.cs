using System;
using System.Data;

public class BattleSession(IReadOnlySet<CharacterBase> party, IReadOnlyList<EnemyCharacter> enemies)
{
    public IReadOnlySet<CharacterBase> Party { get; private set; } = party;
    public IReadOnlyList<EnemyCharacter> Enemies { get; private set; } = enemies;

    public List<CharacterBase> GetAliveParty() => Party.Where(p => !p.Stat.IsDead).ToList();
	public List<EnemyCharacter> GetAliveEnemy() => Enemies.Where(e => !e.Stat.IsDead).ToList();

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
    public void Dispose()
	{
		foreach (Entity party in _battleSession.Party) party.Notifications.ClearNotify();
	}
    public BattleResultType BattleStart()
	{
		_providorContext.LogProvider.Log("戦闘開始");

		BattleNotification.Initialize(_battleSession, this);

		bool isOver = false;
		BattleResultType resultType = BattleResultType.ContinueBattle;

		BattleNotification.TriggerPhase(Phase.StartBattle, null, null); //戦闘開始

		ExecuteActionUnit();
		int currentTurn = 1;
        while (!isOver)
		{
			_providorContext.LogProvider.Log($"-----------{currentTurn}ターン目---------------");
			currentTurn++;
			var conditioncontext = _baseConditioncontext with { CurrentTurn = currentTurn };
			BattleNotification.UpDateEntities();
			List<ActionUnit[]> enemyActions = _battleServices.BattleActionQueue.CreateEnemyActions(conditioncontext);
			List<ActionUnit[]> playerActions = _battleServices.BattleActionQueue.CreatePlayerActions(conditioncontext);
			var sortedActions = SortActionQueue(enemyActions.Concat(playerActions).ToList());
			_runtimeContext.Enqueue(sortedActions);

			BattleNotification.TriggerPhase(Phase.StartTurn, null, null); //ターン開始

			ExecuteActionUnit();

			BattleNotification.TriggerPhase(Phase.EndTurn, null, null); //ターン終了

			ExecuteActionUnit();

            (isOver, resultType) = _battleSession.IsBattleOver();
			_battleServices.ActionExecutor.ClearLogCache();
			foreach(Entity enemy in _battleSession.GetAliveEnemy()) enemy.Notifications.TickNotify();
			foreach(Entity party in _battleSession.GetAliveParty()) party.Notifications.TickNotify();
        }

        CheckBattleResult(resultType);

		BattleNotification.TriggerPhase(Phase.EndBattle, null, null); //戦闘終了

		ExecuteActionUnit();
		Dispose();
		if (resultType == BattleResultType.Victory)
		{
            var reward = _battleServices.BattleRewardCalculator.CalculateReward(_battleSession.Enemies);
            _partyController.GetReward(reward);
		}
		return resultType;
	}

	public Queue<ActionUnit[]> SortActionQueue(List<ActionUnit[]> actionUnits)
	{
		return _battleServices.TurnScheduler.ActionOrder(actionUnits);
	}

	public void ExecuteActionUnit()
	{
		while((!_runtimeContext.IsActionEmpty()) && !_battleSession.IsBattleOver().Item1)
		{
			if(!_runtimeContext.TryGetNextAction(out var currentAction))
			{
				break;
			}
			_battleServices.ActionExecutor.ExecuteAction(currentAction, this);
		}
	}

	private void CheckBattleResult(BattleResultType resultType)
	{
		switch (resultType)
		{
			case BattleResultType.Victory:
                _providorContext.LogProvider.Log("_戦闘に勝利した!_");
				break;
			case BattleResultType.Defeat:
                _providorContext.LogProvider.Log("_戦闘に敗北した..._");
				break;
			case BattleResultType.Escape:
				_providorContext.LogProvider.Log("_戦闘から逃げ出した");
				break;
			default:
				_providorContext.LogProvider.Log("想定外の結果");
				break;
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

}

