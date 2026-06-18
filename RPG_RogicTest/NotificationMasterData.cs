using System;

static public class NotificationMasterData
{
    public static IReadOnlyDictionary<GameId<INotificationId>, BaseNotifyData> NotifyDataDict => _notifyDict;
    private static readonly Dictionary<GameId<INotificationId>, BaseNotifyData> _notifyDict = new Dictionary<GameId<INotificationId>, BaseNotifyData>();

    public static void Load()
    {
        _notifyDict.Clear();

        _notifyDict["notify_000"] = new NullNotifyData("notify_000", Phase.None, NotifyStackType.Ignore,"", 0, false);
        _notifyDict["notify_001"] = new CounterNotifyData("notify_001", Phase.OnHitDamage, NotifyStackType.Refresh, "{0}のカウンター！", 5, false, 100, 2.0f, true);
        _notifyDict["notify_002"] = new PoisonNotifyData("notify_002", Phase.EndTurn, NotifyStackType.Refresh, "{0}は毒にやられている", 3, false, 5, false, true);
        _notifyDict["notify_003"] = new CounterNotifyData("notify_003", Phase.OnDeath, NotifyStackType.Refresh, "{0}の悪あがき!", 4, true, 100, 10f, true);
    }

}



abstract public class BaseNotifyData(GameId<INotificationId> id, Phase phase, NotifyStackType stackType, string logMessage, int remainTime, bool isForced)
{
    public GameId<INotificationId> NotifyId = id;
    public string LogMessage = logMessage;
    public Phase Phase = phase;
    public NotifyStackType StackType = stackType;
    public int RemainTime = remainTime;
    public bool IsForcedAction = isForced;
    public abstract NotifyType Type { get; }

    public abstract Notification Create();
}

public class NullNotifyData(GameId<INotificationId> id, Phase phase, NotifyStackType stackType, string logMessage, int remainTime, bool isForced) 
    : BaseNotifyData(id, phase, stackType, logMessage, remainTime, isForced)
{
    public override NotifyType Type => NotifyType.None;
    public override Notification Create()
    {
        return new NullNotify(NotifyId, logMassage:LogMessage);
    }
}

public class CounterNotifyData(GameId<INotificationId> id, Phase phase, NotifyStackType stackType,
    string logMessage, int remainTime, bool isForced, int rate, float multiple, bool onAvoided) 
    : BaseNotifyData(id, phase, stackType, logMessage, remainTime, isForced)
{
    public int Rate = rate;
    public float Multiple = multiple;
    public bool OnAvoided = onAvoided;

    public override NotifyType Type => NotifyType.Counter;

    public override Notification Create()
    {
        return new CounterNotify(NotifyId, LogMessage, Phase, StackType, IsForcedAction, Rate, Multiple, OnAvoided);
    }
}

public class PoisonNotifyData(GameId<INotificationId> id, Phase phase, NotifyStackType stackType,
    string logMessage, int remainTime, bool isForced, int rate, bool isFixed, bool referMax) 
    : BaseNotifyData(id, phase, stackType, logMessage, remainTime, isForced)
{
    public int Rate = rate;
    public bool IsFixed = isFixed;
    public bool ReferMax = referMax;

    public override NotifyType Type => NotifyType.Poison;
    public override Notification Create()
    {
        return new PoisonNotify(NotifyId, LogMessage, Phase, StackType, IsForcedAction, Rate, IsFixed, ReferMax);
    }
}

