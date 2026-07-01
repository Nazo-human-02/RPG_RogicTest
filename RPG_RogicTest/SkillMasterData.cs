using System;

public static class GameSkillMasterData
{
    public static IReadOnlyDictionary<GameId<ISkillId>, BaseSkillMasterData> SkillDataDict => _skillDataDict;
    private static readonly Dictionary<GameId<ISkillId>, BaseSkillMasterData> _skillDataDict = new Dictionary<GameId<ISkillId>, BaseSkillMasterData>();

    public static void Load()
    {
        _skillDataDict.Clear();

        _skillDataDict["skill_001"] = 
            new EffectSkillData(
                new SkillInfo("skill_001", "カウンターの構え", 2),
                new TargetData(TargetType.Self, TargetSelectType.Self, 1),
                ConditionData.Empty,
                SkillType.Active, "cost_001",
                [new AddNotifyEffect("notify_001"), new HealEffect(999, true, ReferType.Max, TargetPoint.HP)]);
        _skillDataDict["skill_002"] = 
            new EffectSkillData(
                new SkillInfo("skill_002", "どくどく", 1),
                new TargetData(TargetType.Enemy, TargetSelectType.Self, 1),
                ConditionData.And([new LevelCondition(20, ConditionTarget.User, CompareType.MoreOrEqual), 
                    new LifeStateCondition(LifeState.Alive, ConditionTarget.Target)]),
                SkillType.Active, "cost_002",
                [new AddNotifyEffect("notify_002")]);
        _skillDataDict["skill_003"] =
            new EffectSkillData(
                new SkillInfo("skill_003", "スラッシュ", 3),
                new TargetData(TargetType.Enemy, TargetSelectType.Self, 2),
                ConditionData.Default,
                SkillType.Active, "cost_001",
                [new DamageEffect(0, false, 2f, 3)]);

        _skillDataDict["skill_004"] =
            new EffectSkillData(
                new SkillInfo("skill_004", "即滅斬", 0),
                new TargetData(TargetType.Enemy, TargetSelectType.Self, 99),
                ConditionData.Default,
                SkillType.Active, "cost_001",
                [new DamageEffect(0, false, 10f, 5)]);
    }

    public static BaseSkillMasterData GetSkillData(GameId<ISkillId> skillID)
    {
        if(!SkillDataDict.TryGetValue(skillID, out var skillData))
        {
            throw new Exception($"スキルID:{skillID}のデータが見つかりません");
        }
        return skillData;
    }
}

public record struct SkillInfo
(
    GameId<ISkillId> SkillId,
    string SkillName,
    int MaxCoolTime
);

