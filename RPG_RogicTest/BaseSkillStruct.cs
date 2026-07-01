using System;

public abstract class Skill
    (SkillInfo skillInfo, TargetData targetData, ConditionData conditionData)
{
    public SkillInfo SkillInfo { get; init; } = skillInfo;
    public int CurrentCoolTime { get; private set; } = 0;
    public TargetData TargetData { get; init; } = targetData;
    public ConditionData ConditionData { get; init; } = conditionData;

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
            CurrentCoolTime = SkillInfo.MaxCoolTime;
        }
    }

    abstract public void ExecuteSkill
        (ActionUnit actionUnit, Entity target, EffectContent effectContent);

    public Skill Clone()
    { 
        return (Skill)this.MemberwiseClone();
    }
}

public abstract class PassiveSkill
    (SkillInfo skillInfo, Phase activePhase, TargetData targetData, ConditionData conditionData)
    : Skill(skillInfo, targetData, conditionData)
{
    public Phase ActivePhase { get; set; } = activePhase;
}

public abstract class ActiveSkill
    (SkillInfo skillInfo, CostData costData, TargetData targetData, ConditionData conditionData) 
    : Skill(skillInfo, targetData, conditionData)
{
    public CostData CostData { get; init; } = costData;


    public int GetRequiredCost(Entity entity)
    {
        if(CostData.IsFixed) return CostData.Cost;

        return (CostData.CostType) switch
        {
            CostType.CurrentHP => (int)(entity.Stat.CurrentHp * CostData.Cost / 100.0f),
            CostType.CurrentMP => (int)(entity.Stat.CurrentMp * CostData.Cost / 100.0f),
            CostType.MaxHP => (int)(entity.Stat.TotalHP * CostData.Cost / 100.0f),
            CostType.MaxMP => (int)(entity.Stat.TotalMP * CostData.Cost / 100.0f),
            _ => CostData.Cost,
        };
    }
    public bool CanUseSkill(Entity entity)
    {
        int requiredCost = GetRequiredCost(entity);
        return CostData.CostType switch
        {
            CostType.CurrentHP or CostType.MaxHP => entity.Stat.CurrentHp > requiredCost,
            CostType.CurrentMP or CostType.MaxMP => entity.Stat.CurrentMp >= requiredCost,
            _ => false,
        };
    }
    public void PayCost(Entity entity)
    {
        int requiredCost = GetRequiredCost(entity);
        switch (CostData.CostType)
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

public class NullBrankSkill
    (SkillInfo skillInfo, CostData costData, TargetData targetData, ConditionData conditionData)
    : ActiveSkill(skillInfo, costData, targetData, conditionData)
{ 
    public override void ExecuteSkill
        (ActionUnit actionUnit, Entity target, EffectContent effectContent)
    {
        // Do nothing
    }
}

public class EffectSkill
    (SkillInfo skillInfo, CostData costData, TargetData targetData, ConditionData conditionData,
    List<EffectBase> effects) 
    : ActiveSkill(skillInfo, costData, targetData, conditionData)
{
    List<EffectBase> Effects { get; init; } = new List<EffectBase>(effects);
    public override void ExecuteSkill
        (ActionUnit actionUnit, Entity target, EffectContent effectContent)
    {
        ActionSource actionSource = ActionSource.FromSkill(this);
        foreach (var effect in Effects)
        {
            var result = effect.ApplyEffect(effectContent, actionSource);
            if(result.ActionUnit != null)
            {
                effectContent.BattleManager?.InsertInterruptAction(result.ActionUnit);
            }
        }
    }
}

