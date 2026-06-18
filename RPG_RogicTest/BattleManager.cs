using System;
using System.Data;

public class BattleSession(IReadOnlySet<CharacterBase> party, List<EnemyCharacter> enemies)
{
    public IReadOnlySet<CharacterBase> Party { get; private set; } = party;
    public List<EnemyCharacter> Enemies { get; private set; } = enemies;

    public List<CharacterBase> GetAliveParty() => Party.Where(p => !p.Stat.IsDead).ToList();
	public List<EnemyCharacter> GetAliveEnemy() => Enemies.Where(e => !e.Stat.IsDead).ToList();

	public (bool,int) IsBattleOver() //int=1で勝利、int=2で敗北
	{
		if(GetAliveParty().Count() == 0)
		{
			return (true, 2);
		}
        else if(GetAliveEnemy().Count() == 0)
        {
			return (true, 1);
        }
		else
		{
			return (false, 0);
		}
    }
}

public static class ActionUnitCreator
{
	public static ActionUnit[] GetActionUnit(ActionType actionType, Entity executor, List<Entity> targets, Skill? skill = null)
	{
		List<ActionUnit> actionUnits = new List<ActionUnit>();
		Guid guid = Guid.NewGuid();
		UnitGuid unitGuid = new UnitGuid();
		foreach(Entity target in targets)
		{
			ActionUnit actionUnit = new ActionUnit(actionType, executor, target, skill, unitGuid:unitGuid, guid:guid);
			actionUnits.Add(actionUnit);
		}
		return actionUnits.ToArray();
	}
}

public class BattleManager
{
	public BattleSession Session => _battleSession;
	private BattleSession _battleSession {  get; set; }

	private Queue<ActionUnit[]> _actionQueue = new Queue<ActionUnit[]>();
	private Queue<ActionUnit[]> _interruptAction = new Queue<ActionUnit[]>();
	private List<(ActionUnit, int)> _subStackInterrupt = new List<(ActionUnit, int)>();
	
	public BattleManager(IReadOnlySet<CharacterBase> party, List<EnemyCharacter> enemies)
	{
        _battleSession = new BattleSession(party, enemies);
    }
	public void Dispose()
	{
		foreach (Entity party in _battleSession.Party) party.Notifications.ClearNotify();
	}
    public BattleResultType BattleStart()
	{
		LogWrite.Log("戦闘開始");

		BattleNotification.Initialize(_battleSession, this);

		bool isOver = false;
		int result = 0;
        CommandSelect.InitializeCommand();

		BattleNotification.TriggerPhase(Phase.StartBattle, null, null); //戦闘開始

		ExecuteActionUnit();

		int currentTurn = 1;
        while (!isOver)
		{
			LogWrite.Log($"-----------{currentTurn}ターン目---------------");
			currentTurn++;
			BattleNotification.UpDateEntities();
			List<ActionUnit[]> actionUnits = CommandSelectStep();
			_actionQueue = SortActionQueue(actionUnits);

			BattleNotification.TriggerPhase(Phase.StartTurn, null, null); //ターン開始

			ExecuteActionUnit();

			BattleNotification.TriggerPhase(Phase.EndTurn, null, null); //ターン終了

			ExecuteActionUnit();

            (isOver, result) = _battleSession.IsBattleOver();
			ActionExecutor.ClearLogCache();
			foreach(Entity enemy in _battleSession.GetAliveEnemy()) enemy.Notifications.TickNotify();
			foreach(Entity party in _battleSession.GetAliveParty()) party.Notifications.TickNotify();
        }

        CheckBattleResult(result);

		BattleNotification.TriggerPhase(Phase.EndBattle, null, null); //戦闘終了

		ExecuteActionUnit();
		Dispose();
		if(result == 1)
		{
			int totalGold = _battleSession.Enemies.Sum(e => e.DropData.Gold);
			int totalExp = _battleSession.Enemies.Sum(e => e.DropData.Exp);
			DropRewardData totalReward = new DropRewardData(totalGold, totalExp);
			PartyController.GetReward(totalReward);
			return BattleResultType.Victory;
		}
		else if(result == 2)
		{
			return BattleResultType.Defeat;
		}
		else
		{
			return BattleResultType.Escape;
		}
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
			ActionType actionType = CommandSelect.WaitCommandSelect(party);
			if (actionType == ActionType.Skill)
			{
				Skill skill = SkillSelection.SkillSelect(party);
				List<Entity> targets = TargetSelect.SetSelecting(party, _battleSession, skill.TargetType, skill.TargetAmount);
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
		return TurnScheduler.ActionOrder(actionUnits);
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
			ActionExecutor.ExecuteAction(currentAction, this);
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
			LogWrite.Log("エラーメッセージ:Action");
			return null;
		}
	}

	private void CheckBattleResult(int result)
	{
        if (result == 1)
        {
            LogWrite.Log("_戦闘に勝利した!_");
        }
        else if (result == 2)
        {
            LogWrite.Log("_戦闘に敗北した..._");
        }
        else
        {
            LogWrite.Log("想定外の結果");
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

public static class CommandSelect
{
	private static readonly Dictionary<int, ActionType> commandOption = new Dictionary<int, ActionType>();

	public static void InitializeCommand()
	{
		commandOption.Clear();
		commandOption[0] = ActionType.Attack;
		commandOption[1] = ActionType.Guard;
		commandOption[2] = ActionType.Skill;
		commandOption[3] = ActionType.Escape;
	}

	public static ActionType WaitCommandSelect(Entity entity)
	{
		LogWrite.Log("[0:攻撃, 1:防御, 2:スキル, 3:逃走]");

		string? selected = Console.ReadLine();

		if (string.IsNullOrEmpty(selected) || !int.TryParse(selected, out var n))
		{
			LogWrite.Log("!入力が正しくありません!");
			return WaitCommandSelect(entity);
			
		}
		if (!commandOption.TryGetValue(n, out var actionType))
		{
			LogWrite.Log("!設定されていない番号です!");
			return WaitCommandSelect(entity);
		}
		if(actionType == ActionType.Skill && entity.ValidSkills.Count == 0)
		{
			LogWrite.Log($"!{entity.Name}はスキルを所持していません!");
			return WaitCommandSelect(entity);
		}
		return actionType;
	}
}

public static class BattleNotification
{
	private static BattleSession? _battleSession;
	private static BattleManager? _battleManager;
	private static List<Entity> _allEntities = new();

	public static void Initialize(BattleSession battleSession, BattleManager battleManager)
	{
		_battleSession = battleSession;
		_battleManager = battleManager;
        _allEntities = _battleSession.GetAliveEnemy().Cast<Entity>().Concat(_battleSession.GetAliveParty()).ToList();
    }

	public static void UpDateEntities()
	{
		if (_battleSession == null)
			return;
        _allEntities = _battleSession.GetAliveEnemy().Cast<Entity>().Concat(_battleSession.GetAliveParty()).ToList();
    }
    public static void TriggerPhase(Phase phase, ActionUnit? actionUnit, Entity? currentTarget)
	{
		foreach(Entity entity in _allEntities)
		{
			if(entity.Stat.IsDead && phase != Phase.OnDeath)
			{
				continue;
			}
			if(actionUnit != null &&  actionUnit.ActionGuid.IsProcessed(phase, entity))
			{
				continue;
			}
			if(_battleManager == null)
			{
				continue;
            }
            entity.Notifications.ExecuteNotifies(_battleManager, phase, actionUnit, currentTarget);
		}
	}
}