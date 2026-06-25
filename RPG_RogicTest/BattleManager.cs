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



public class BattleManager(IReadOnlySet<CharacterBase> party, IReadOnlyList<EnemyCharacter> enemies, 
	ILogProvider logProvider, IInputProvider inputProvider, IRandomProvider randomProvider, PartyController partyController)
{
    private readonly BattleSession _battleSession = new BattleSession(party, enemies);
	private readonly CommandSelect _commandSelect = new CommandSelect(logProvider);
    private readonly ILogProvider _logProvider = logProvider;
    private readonly ActionExecutor _actionExecutor =
		new ActionExecutor(new BattleCalculator(randomProvider), logProvider);
    private readonly TurnScheduler _turnScheduler = new TurnScheduler(randomProvider);

	private readonly SkillSelection skillSelection = new SkillSelection(logProvider, inputProvider);
	private readonly TargetSelect targetSelect = new TargetSelect(logProvider, inputProvider);

	private readonly PartyController _partyController = partyController;
	private readonly BattleRewardCalculator _rewardCalculator = new BattleRewardCalculator(randomProvider);
    private Queue<ActionUnit[]> _actionQueue = new Queue<ActionUnit[]>();
	private Queue<ActionUnit[]> _interruptAction = new Queue<ActionUnit[]>();
	private List<(ActionUnit, int)> _subStackInterrupt = new List<(ActionUnit, int)>();

    public void Dispose()
	{
		foreach (Entity party in _battleSession.Party) party.Notifications.ClearNotify();
	}
    public BattleResultType BattleStart()
	{
		_logProvider.Log("戦闘開始");

		BattleNotification.Initialize(_battleSession, this);

		bool isOver = false;
		BattleResultType resultType = BattleResultType.ContinueBattle;
        _commandSelect.InitializeCommand();

		BattleNotification.TriggerPhase(Phase.StartBattle, null, null); //戦闘開始

		ExecuteActionUnit();

		int currentTurn = 1;
        while (!isOver)
		{
			_logProvider.Log($"-----------{currentTurn}ターン目---------------");
			currentTurn++;
			BattleNotification.UpDateEntities();
			List<ActionUnit[]> actionUnits = CommandSelectStep();
			_actionQueue = SortActionQueue(actionUnits);

			BattleNotification.TriggerPhase(Phase.StartTurn, null, null); //ターン開始

			ExecuteActionUnit();

			BattleNotification.TriggerPhase(Phase.EndTurn, null, null); //ターン終了

			ExecuteActionUnit();

            (isOver, resultType) = _battleSession.IsBattleOver();
			_actionExecutor.ClearLogCache();
			foreach(Entity enemy in _battleSession.GetAliveEnemy()) enemy.Notifications.TickNotify();
			foreach(Entity party in _battleSession.GetAliveParty()) party.Notifications.TickNotify();
        }

        CheckBattleResult(resultType);

		BattleNotification.TriggerPhase(Phase.EndBattle, null, null); //戦闘終了

		ExecuteActionUnit();
		Dispose();
		if (resultType == BattleResultType.Victory)
		{
            var reward = _rewardCalculator.CalculateReward(enemies);
            _partyController.GetReward(reward);
		}
		return resultType;
	}

	public List<ActionUnit[]> CommandSelectStep()
	{
		List<ActionUnit[]> actionUnits = new List<ActionUnit[]>();
		foreach (var enemy in _battleSession.GetAliveEnemy())
		{
			var first = _battleSession.GetAliveParty().FirstOrDefault();
			if(first == null)
			{
				continue;
			}
			Guid guid = Guid.NewGuid(); 
			ActionUnit action = new ActionUnit(ActionType.Attack, enemy, first, guid:guid);
			actionUnits.Add([action]);
		}
		foreach(var party in _battleSession.GetAliveParty())
		{
			var first = _battleSession.GetAliveEnemy().FirstOrDefault();
            if (first == null)
			{
				continue;
			}
			ActionType actionType = _commandSelect.WaitCommandSelect(party);
			if (actionType == ActionType.Skill)
			{
				Skill skill = skillSelection.SkillSelect(party);
				List<Entity> targets = targetSelect.SelectingTargets(party, _battleSession, skill.TargetType, skill.TargetAmount);
				ActionUnit[] actions = ActionUnitCreator.GetActionUnit(actionType, party, targets, skill);
				actionUnits.Add(actions);
			}
			else
			{
				Guid guid = Guid.NewGuid();
				ActionUnit action = new ActionUnit(actionType, party, first, guid:guid);
                actionUnits.Add([action]);
            }
        }

		return actionUnits;
	}
	public Queue<ActionUnit[]> SortActionQueue(List<ActionUnit[]> actionUnits)
	{
		return _turnScheduler.ActionOrder(actionUnits);
	}

	public void ExecuteActionUnit()
	{
		while((_actionQueue.Count > 0 || _interruptAction.Count > 0 ) && !_battleSession.IsBattleOver().Item1)
		{

			ActionUnit[]? currentAction = NextActionUnit();
			if(currentAction == null || (currentAction[0].Executor.Stat.IsDead && !currentAction[0].IsForced))
			{
				continue;
			}
			_actionExecutor.ExecuteAction(currentAction, this);
		}
	}

	private ActionUnit[]? NextActionUnit()
	{
		ReleaseStackAction();
		if(_interruptAction.Count > 0)
		{
			return _interruptAction.Dequeue();
		}
		else if (_actionQueue.Count > 0)
		{
			return _actionQueue.Dequeue();
		}
		else
		{
			_logProvider.Log("エラーメッセージ:Action");
			return null;
		}
	}

	private void CheckBattleResult(BattleResultType resultType)
	{
		switch (resultType)
		{
			case BattleResultType.Victory:
                _logProvider.Log("_戦闘に勝利した!_");
				break;
			case BattleResultType.Defeat:
                _logProvider.Log("_戦闘に敗北した..._");
				break;
			case BattleResultType.Escape:
				_logProvider.Log("_戦闘から逃げ出した");
				break;
			default:
				_logProvider.Log("想定外の結果");
				break;
        }
    }

    public void InsertInterruptAction(ActionUnit interruptAction)
    {
        _interruptAction.Enqueue([interruptAction]);
    }
	public void InsertInterruptAction(ActionUnit[] interruptAction)
	{
        _interruptAction.Enqueue(interruptAction);
    }

    public void StackInterruptAction(ActionUnit interruptAction, int num)
	{
		_subStackInterrupt.Add((interruptAction, num));
	}
	public void ReleaseStackAction()
	{
		if(_subStackInterrupt.Count == 0)
		{
			return;
		}
		var stacks = _subStackInterrupt.GroupBy(x => x.Item2).ToList();
		foreach (var stack in stacks)
		{
			List<ActionUnit> actions = new List<ActionUnit>();
			foreach (var item in stack)
			{
				actions.Add(item.Item1);
			}
			InsertInterruptAction(actions.ToArray());
		}
		_subStackInterrupt.Clear();
 	}
}

