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
