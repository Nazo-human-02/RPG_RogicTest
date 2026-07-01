using System;

public static class SkillCreator
{
    public static Skill Create(GameId<ISkillId> skillId)
    {
        BaseSkillMasterData skillMasterData = GameSkillMasterData.GetSkillData(skillId);
        CostData costData = CostMasterData.GetCostData(skillMasterData.CostID);
        Skill skill = skillMasterData.Create(costData);
        return skill;
    }
}

public static class NotifyCreator
{
    public static Notification Creator(GameId<INotificationId> notifyId, Entity owner)
    {
        BaseNotifyData notifyData = NotificationMasterData.GetNotifyData(notifyId);
        Notification notification = notifyData.Create();
        notification.Initialize(owner, notifyData.RemainTime);
        return notification;
    }
}

public static class ActionUnitCreator
{
    public static ActionUnit[] GetActionUnit
        (ActionType actionType, ActionSource actionSource, Entity executor,
        List<Entity> targets, Skill? skill = null, UseItemInfo? useItemInfo = null)
    {
        List<ActionUnit> actionUnits = new List<ActionUnit>();
        Guid guid = Guid.NewGuid();
        UnitGuid unitGuid = new UnitGuid();
        foreach (Entity target in targets)
        {
            ActionUnit actionUnit =
                new ActionUnit(actionType, actionSource, executor, target, skill, unitGuid: unitGuid, guid: guid, useItemInfo:useItemInfo);
            actionUnits.Add(actionUnit);
        }
        return actionUnits.ToArray();
    }
}
