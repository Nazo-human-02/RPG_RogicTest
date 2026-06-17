using System;

public static class GameSkillMasterData
{
    public static IReadOnlyDictionary<GameId<ISkillId>, BaseSkillMasterData> SkillDataDict => _skillDataDict;
    private static readonly Dictionary<GameId<ISkillId>, BaseSkillMasterData> _skillDataDict = new Dictionary<GameId<ISkillId>, BaseSkillMasterData>();

    public static IReadOnlyDictionary<GameId<ICostId>, CostData> CostDict => _costDict;
    private static readonly Dictionary<GameId<ICostId>, CostData> _costDict = new Dictionary<GameId<ICostId>, CostData>();

    public static void Load()
    {
        CostLoad();
        SkillLoad();
    }
    public static void CostLoad()
    {
        _costDict.Clear();

        _costDict["cost_000"] = new CostData(CostType.CurrentMP, true, 0);
        _costDict["cost_001"] = new CostData(CostType.MaxMP, true, 10);
        _costDict["cost_002"] = new CostData(CostType.CurrentMP, false, 50);

    }
    public static void SkillLoad()
    {
        _skillDataDict.Clear();

        _skillDataDict["skill_001"] = new AffordSkillData("skill_001", SkillType.Active, "カウンターの構え", 2, "notify_001", TargetType.Self, 1, "cost_001");
        _skillDataDict["skill_002"] = new AffordSkillData("skill_002", SkillType.Active, "どくどく", 1, "notify_002", TargetType.Enemy, 1, "cost_002");
        _skillDataDict["skill_003"] = new AttackSkillData("skill_003", SkillType.Active, "スラッシュ", 3, "notify_000", TargetType.Enemy, 2, "cost_001", 2f, 3, false, 0);
    }

}

abstract public class BaseSkillMasterData(GameId<ISkillId> id, SkillType skillType , string skillName,
    int coolTime, GameId<INotificationId> notificationID, GameId<ICostId> costId, TargetType targetType, int targetAmount = 1)
{
    public GameId<ISkillId> SkillId = id;
    public SkillType SkillType = skillType;
    public string SkillName = skillName;
    public int CoolTime = coolTime;
    public GameId<INotificationId> NotificationID = notificationID;
    public GameId<ICostId> CostID = costId;
    public TargetType TargetType = targetType;
    public int TargetAmount = targetAmount;
    abstract public Skill Create(CostData costData);
}

public class AffordSkillData(GameId<ISkillId> id, SkillType skillType, string skillName, int coolTime, 
    GameId<INotificationId> notificationID, TargetType targetType, int targetAmount, GameId<ICostId> costId)
    : BaseSkillMasterData(id, skillType, skillName, coolTime, notificationID, costId, targetType, targetAmount)
{
    public override Skill Create(CostData costData)
    {
        return 
            new AffordNotifySkill(SkillId, SkillName, CoolTime, NotificationID, costData.CostType, costData.IsFixed, costData.Cost, TargetType, TargetAmount);
    }
}

public class AttackSkillData(GameId<ISkillId> id, SkillType skillType, string skillName, int coolTime, GameId<INotificationId> notificationID,
    TargetType targetType, int targetAmount, GameId<ICostId> costId, float attackRate, int attackTime, bool isFixed, int attackValue)
    : BaseSkillMasterData(id, skillType, skillName, coolTime, notificationID, costId, targetType, targetAmount)
{
    public float AttackRate = attackRate;
    public int AttackTime = attackTime;
    public bool IsFixed = isFixed;
    public int AttackValue = attackValue;

    public override Skill Create(CostData costData)
    {
        return
            new AttackSkill(SkillId, SkillName, CoolTime, NotificationID, costData.CostType, costData.IsFixed, costData.Cost, TargetType, TargetAmount, 
            AttackRate, AttackTime, IsFixed, AttackValue);
    }
}

public class CostData(CostType costType, bool isFixed, int cost)
{
    public CostType CostType = costType;
    public bool IsFixed = isFixed;
    public int Cost = cost;
}