using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

abstract public class BaseSkillMasterData
    (SkillInfo skillInfo, SkillType skillType, GameId<ICostId> costId, 
    TargetData targetData, ConditionData conditionData)
{
    public SkillInfo SkillInfo = skillInfo;
    public SkillType SkillType = skillType;
    public GameId<ICostId> CostID = costId;
    public TargetData TargetData = targetData;
    public ConditionData ConditionData = conditionData;
    abstract public Skill Create(CostData costData);
}
public class EffectSkillData(SkillInfo skillInfo, TargetData targetData, ConditionData conditionData,
    SkillType skillType, GameId<ICostId> costId, List<EffectBase> effects)
    : BaseSkillMasterData(skillInfo, skillType, costId, targetData, conditionData)
{
    private readonly List<EffectBase> effectBases = effects;
    public override Skill Create(CostData costData)
    {
        return new EffectSkill(SkillInfo, costData, TargetData, ConditionData, effectBases);
    }
}
