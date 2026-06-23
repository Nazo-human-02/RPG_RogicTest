using System;

public abstract class Skill(GameId<ISkillId> id, string skillName, int coolTime, GameId<INotificationId> notifyId, TargetType targetType, int targetAmount = 1)
{
    public GameId<ISkillId> SkillId { get; init; } = id;
    public string Name { get; init; } = skillName;
    public int MaxCoolTime { get; init; } = coolTime;
    public int CurrentCoolTime { get; private set; } = 0;
    public GameId<INotificationId> NotifyId { get; init; } = notifyId;
    public TargetType TargetType { get; init; } = targetType;
    public int TargetAmount { get;private set; } = targetAmount;

    public void ReduceCoolTime()
    {
        if (CurrentCoolTime > 0)
        {
            CurrentCoolTime--;
        }
    }
    public void SetCoolTime(int time = -1)
    {
        if(time != -1)
        {
            CurrentCoolTime = time;
        }
        else
        {
            CurrentCoolTime = MaxCoolTime;
        }
    }

    abstract public void ExecuteSkill(BattleManager battleManager, ActionUnit actionUnit, Entity target);

    public Skill Clone()
    { 
        return (Skill)this.MemberwiseClone();
    }
}

public abstract class PassiveSkill(GameId<ISkillId> id, string skillName, int coolTime, GameId<INotificationId> notifyId, Phase activePhase, TargetType targetType, int targetAmount = 1)
    : Skill(id, skillName, coolTime, notifyId, targetType, targetAmount)
{
    public Phase ActivePhase { get; set; } = activePhase;
}

public abstract class ActiveSkill(GameId<ISkillId> id, string skillName, int coolTime, GameId<INotificationId> notifyId, CostType costType, bool isFixed, int cost, TargetType targetType, int targetAmount = 1) 
    : Skill(id, skillName, coolTime, notifyId, targetType, targetAmount)
{
    public CostType CostType { get; init; } = costType;
    public bool IsFixed { get; init; } = isFixed;
    public int Cost {  get; init; } = cost;

    public int GetRequiredCost(Entity entity)
    {
        if(IsFixed) return Cost;

        return (CostType) switch
        {
            CostType.CurrentHP => (int)(entity.Stat.CurrentHp * Cost / 100.0f),
            CostType.CurrentMP => (int)(entity.Stat.CurrentMp * Cost / 100.0f),
            CostType.MaxHP => (int)(entity.Stat.TotalHP * Cost / 100.0f),
            CostType.MaxMP => (int)(entity.Stat.TotalMP * Cost / 100.0f),
            _ => Cost,
        };
    }
    public bool CanUseSkill(Entity entity)
    {
        int requiredCost = GetRequiredCost(entity);
        return CostType switch
        {
            CostType.CurrentHP or CostType.MaxHP => entity.Stat.CurrentHp > requiredCost,
            CostType.CurrentMP or CostType.MaxMP => entity.Stat.CurrentMp >= requiredCost,
            _ => false,
        };
    }
    public void PayCost(Entity entity)
    {
        int requiredCost = GetRequiredCost(entity);
        switch (CostType)
        {
            case CostType.CurrentHP or CostType.MaxHP:
                entity.Stat.CurrentHp -= requiredCost;
                break;
            case CostType.CurrentMP or CostType.MaxMP:
                entity.Stat.CurrentMp -= requiredCost;
                break;
        }
    }
    public bool TryPayCost(Entity entity)
    {
        if (CanUseSkill(entity))
        {
            PayCost(entity);
            return true;
        }
        return false;
    }
}

public class NullBrankSkill(GameId<ISkillId> id, string skillName, int coolTime, GameId<INotificationId> notifyId,
    CostType costType, bool isFixed, int cost, TargetType targetType, int targetAmount)
    : ActiveSkill(id, skillName, coolTime, notifyId, costType, isFixed, cost, targetType, targetAmount)
{ 
    public override void ExecuteSkill(BattleManager battleManager, ActionUnit actionUnit, Entity target)
    {
        // Do nothing
    }
}


public class AffordNotifySkill(GameId<ISkillId> id, string skillName, int coolTime, GameId<INotificationId> notifyId, 
    CostType costType, bool isFixed, int cost, TargetType targetType, int targetAmount = 1) 
    : ActiveSkill(id, skillName, coolTime, notifyId, costType, isFixed, cost, targetType, targetAmount)
{
    public override void ExecuteSkill(BattleManager battleManager, ActionUnit actionUnit, Entity target)
    {
        Notification notify = NotifyCreator.Creator(NotifyId, target);
        target.AddNotify(notify);
    }
}

public class AttackSkill(GameId<ISkillId> id, string skillName, int coolTime, GameId<INotificationId> notifyId,
    CostType costType, bool isFixed, int cost, TargetType targetType, int targetAmount,
    float attackRate, int attackTime = 1, bool isDmgFixed = false, int damageValue = 1)
    : ActiveSkill(id, skillName, coolTime, notifyId, costType, isFixed, cost, targetType, targetAmount)
{
    public float AttackRate { get; init; } = attackRate; //ダメージ倍率
    public int AttackTime { get; init; } = attackTime; //攻撃回数
    public bool IsDmgFixed { get; init; } = isDmgFixed; //固定ダメージか
    public int DamageValue { get; init; } = damageValue; //固定ダメージが有効時に使用
    
    public override void ExecuteSkill(BattleManager battleManager, ActionUnit actionUnit, Entity target)
    {
        UnitGuid unitGuid = new();
        for (int hit = 0; hit < AttackTime; hit++)
        {
            DamageInfo info = new DamageInfo() { DamageMultiplier = AttackRate, FixedDamage = (IsDmgFixed) ? DamageValue : 0 };
            ActionUnit unit = new ActionUnit(ActionType.Attack, actionUnit.Executor, target, unitGuid:unitGuid, guid:actionUnit.Guid, damageInfo:info);
            unit.SetContent($"{actionUnit.Executor.Name}の{Name}!");
            battleManager.StackInterruptAction(unit, hit);
        }
    }
}