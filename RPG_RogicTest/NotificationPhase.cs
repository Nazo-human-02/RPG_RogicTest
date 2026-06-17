using System;

abstract public class Notification(GameId<INotificationId> notifyID, Phase phase, string messageLog, bool isForced)
{
	public GameId<INotificationId> NotifyID { get; init; } = notifyID;
	public string MessageLog { get; set; } = messageLog;
	public Entity? Owner { get;private set; }
	public int RemainTime { get;private set; }
	public bool IsForcedAction { get; init; } = isForced;
	public Phase Phase { get; init; } = phase;

	virtual public void Initialize(Entity owner, int remainTime)
	{
		Owner = owner;
		RemainTime = remainTime;
	}
	abstract public bool CheckMet(Phase currentPhase, ActionUnit? actionUnit, Entity? currentTarget);

	public void OnNotify(BattleManager battleManager, Phase currentPhase, ActionUnit? actionUnit, Entity? currentTarget)
	{
		if(RemainTime == 0)
		{
			return;
		}
		if(CheckMet(currentPhase, actionUnit, currentTarget))
		{
			ActionUnit? action = CreateUnit(actionUnit, currentTarget);
            if (action == null)
            {
				return;
            }
            battleManager.InsertInterruptAction(action);
			if (actionUnit != null && Owner != null)
			{
                action.SetContent(string.Format(MessageLog, Owner.Name, action.Target.Name));
                actionUnit.ActionGuid.MarkAsProcessed(currentPhase, Owner);
			}
		}
	}

	protected bool NullSafty(ActionUnit? actionUnit, Entity? currentTarget)
	{
		return (actionUnit == null || currentTarget == null);
	}
	protected abstract ActionUnit? CreateUnit(ActionUnit? actionUnit, Entity? currenttarget);

	public void ReduceRemainTime()
	{
		if (RemainTime > 0)
		{
			RemainTime--;
		}
	}
}

public class NullNotify(GameId<INotificationId> notifyID, string logMassage, Phase phase = Phase.None, bool isForced = false) 
	: Notification(notifyID, phase, logMassage, isForced)
{
    public override bool CheckMet(Phase currentPhase, ActionUnit? actionUnit, Entity? currentTarget)
    {
		return false;
    }

    protected override ActionUnit? CreateUnit(ActionUnit? actionUnit, Entity? currenttarget)
    {
		return null;
    }
}
public class CounterNotify(GameId<INotificationId> notifyID, string logMassage, Phase phase, bool isForced, int rate, float multiple, bool onAvoid)
	: Notification(notifyID, phase, logMassage, isForced)
{
    int counterRate { get; init; } = rate;
    float counterMultiple { get; init; } = multiple;
    bool onAvoided { get; init; } = onAvoid;

    public override bool CheckMet(Phase currentPhase, ActionUnit? actionUnit, Entity? currentTarget)
    {
		if(actionUnit == null || currentTarget == null)
		{
			return false;
		}
		return (currentPhase == Phase && Owner == currentTarget && !actionUnit.DamageInfo.IsCounter && actionUnit.DamageType == DamageType.Physical);
    }

    protected override ActionUnit? CreateUnit(ActionUnit? actionUnit, Entity? currentTarget)
    {
		int rdm = Random.Shared.Next(1, 101);
		if(Owner == null || currentTarget == null || actionUnit == null)
		{
			return null;
		}
		if(counterRate >= rdm)
		{
			return null;
		}
		DamageInfo damageInfo = new DamageInfo() { DamageMultiplier = counterMultiple, IsCounter = true};
        ActionUnit action = 
			new ActionUnit(ActionType.Attack, Owner, actionUnit.Executor, guid:Guid.NewGuid(), damageInfo:damageInfo, isForced:IsForcedAction);
        action.SetContent($"--{Owner.Name}のカウンター！");

        return action;
    }
}

public class PoisonNotify(GameId<INotificationId> notifyID, string logMassage, Phase phase, bool isForced, int rate, bool isFixed, bool referMax) 
	: Notification(notifyID, phase, logMassage, isForced)
{
    int poisonRate { get; init; } = rate;
    bool IsFixed { get; init; } = isFixed;
    bool ReferMax { get; init; } = referMax;

    public override bool CheckMet(Phase currentPhase, ActionUnit? actionUnit, Entity? currentTarget)
    {
		return (Phase == currentPhase);
    }
    protected override ActionUnit? CreateUnit(ActionUnit? actionUnit, Entity? currenttarget)
    {
		if(Owner == null)
		{
			return null;
		}
		int poisonDmg = (IsFixed) ? poisonRate : (ReferMax) ? (int)(Owner.Stat.MaxHp * poisonRate / 100f) : (int)(Owner.Stat.CurrentHp * poisonRate / 100f);
		DamageInfo damageInfo = new DamageInfo() {FixedDamage = poisonDmg };
        ActionUnit action =
			new ActionUnit(ActionType.Attack, Owner, Owner, damageInfo:damageInfo, damageType:DamageType.Dot, guid:Guid.NewGuid(), isForced:IsForcedAction);
        action.SetContent($"--{Owner.Name}は毒にやられている");
        return action;
    }
}

