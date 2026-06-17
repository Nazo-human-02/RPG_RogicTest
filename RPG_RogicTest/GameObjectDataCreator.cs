using System;

public static class SkillCreator
{
    public static Skill Create(GameId<ISkillId> skillId)
    {
        if (!GameSkillMasterData.SkillDataDict.TryGetValue(skillId, out var data))
        {
            throw new Exception($"スキルID:{skillId}のデータが見つかりません");
        }

        Skill skillMasterData = GetSkillData(data);
        return skillMasterData;
    }

    private static Skill GetSkillData(BaseSkillMasterData skillMasterData)
    {
        CostData costData = GetCostData(skillMasterData.CostID);

        return skillMasterData.Create(costData);
    }
    private static CostData GetCostData(GameId<ICostId> costId)
    {
        if (!GameSkillMasterData.CostDict.TryGetValue(costId, out var costData))
        {
            return GameSkillMasterData.CostDict["cost_000"];
        }
        return costData;
    }
}

public static class NotifyCreator
{
    public static Notification Creator(GameId<INotificationId> notifyId, Entity owner)
    {
        if (!NotificationMasterData.NotifyDataDict.TryGetValue(notifyId, out var data))
        {
            throw new Exception($"効果ID:{notifyId}のデータが見つかりません");
        }

        Notification notification = GetNotify(data);
        notification.Initialize(owner, data.RemainTime);
        return notification;
    }

    private static Notification GetNotify(BaseNotifyData baseNotifyData)
    {
        return baseNotifyData.Create();
    }
}
