using System;

#region データコンテナ

public class BattleStat()
{
    public ExpSet expSet { get; set; } = new ExpSet();
    public int CurrentHp { get; set; } = 100;
    public int CurrentMp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public int MaxMp { get; set; } = 100;
    public BaseStat baseStat { get; set; } = new BaseStat();
    public ModifierStat EquipmentModStat { get; set; } = new ModifierStat();
    public ModifierStat NotifyModStat { get; set; } = new ModifierStat();
    public ModifierStat TotalModSet => ModifiableStat.GetTotalModifier(EquipmentModStat, NotifyModStat);
    public int TotalHP => TotalModSet.HpMod.TotalValue(MaxHp);
    public int TotalMP => TotalModSet.MpMod.TotalValue(MaxMp);
    public int TotalAtk => TotalModSet.AtkMod.TotalValue(baseStat.Atk);
    public int TotalDef => TotalModSet.DefMod.TotalValue(baseStat.Def);
    public int TotalAgi => TotalModSet.AgiMod.TotalValue(baseStat.Agi);
    public float TotalCriPer => TotalModSet.CriPerMod.TotalFloat(baseStat.CriPer);
    public float TotalCri => TotalModSet.CriMod.TotalFloat(baseStat.Cri);
    public bool IsDead => CurrentHp <= 0;

    public bool TakeDamage(ActionUnit? actionUnit, Entity target, int dmg) //ダメージを受ける。死亡した場合はtrueを返す
    {
        CurrentHp -= dmg;

        if(CurrentHp <= 0)
        {
            CurrentHp = 0;
            BattleNotification.TriggerPhase(Phase.OnDeath, actionUnit, target);
            if(IsDead)
            {
                target.OnDeath();
                return true;
            }
        }
        BattleNotification.TriggerPhase(Phase.OnHitDamage, actionUnit, target);
        return false;
    }
    public bool TakeHeal(int heal)
    {
        if (IsDead) return false;
        CurrentHp += heal;
        if (CurrentHp > TotalHP) CurrentHp = TotalHP;
        return true;
    }
    public BattleStat Clone()
    {
        BattleStat clone = (BattleStat)this.MemberwiseClone();
        clone.baseStat = this.baseStat.Clone();
        clone.expSet = this.expSet.Clone();
        clone.EquipmentModStat = this.EquipmentModStat.Clone();
        clone.NotifyModStat = this.NotifyModStat.Clone();
        return clone;
    }
}
public class ExpSet()
{
    public int CurrentLevel { get; set; } = 1;
    public int CurrentExp { get; set; } = 0;
    public int TotalExp { get; set; } = 0;
    public int NextRequiredExp => CalculateNextExp();
    public float ExpModifier { get; set; } = 1f;
    int CalculateNextExp()
    {
        return (int)(CurrentLevel * 100 * ExpModifier);
    }

    public ExpResult GetExp(Entity entity,int exp)
    {
        TotalExp += exp;
        CurrentExp += exp;
        int currentLevel = CurrentLevel;
        bool isLevelUp = false;
        while (CurrentExp >= NextRequiredExp)
        {
            CurrentExp -= NextRequiredExp;
            CurrentLevel++;
            isLevelUp = true;
        }
        if(isLevelUp) entity.SetLevelUpStat();
        return new ExpResult(exp, isLevelUp, currentLevel, CurrentLevel);
    }

    public void SetLevel(int level)
    {
        CurrentLevel = level;
    }

    public ExpSet Clone()
    {
        ExpSet clone = (ExpSet)this.MemberwiseClone();
        return clone;
    }
}
public record ExpResult(int GetExp, bool IsLevelUp, int BeforeLevel, int AfterLevel);
public class BaseStat(int Atk = 1, int Def = 1, int Agi = 1, float CriPer = 0f, float Cri = 0f)
{
    public int Atk { get; set; } = Atk;
    public int Def { get; set; } = Def;
    public int Agi { get; set; } = Agi;
    public float CriPer { get; set; } = CriPer;
    public float Cri { get; set; } = Cri;
    public BaseStat Clone()
    {
        return new BaseStat(Atk, Def, Agi, CriPer, Cri);
    }
}



public class ModifierStat
{
    public ModifiableStat HpMod = new ModifiableStat();
    public ModifiableStat MpMod = new ModifiableStat();
    public ModifiableStat AtkMod = new ModifiableStat();
    public ModifiableStat DefMod = new ModifiableStat();
    public ModifiableStat AgiMod = new ModifiableStat();
    public ModifiableStat CriPerMod = new ModifiableStat();
    public ModifiableStat CriMod = new ModifiableStat();

    public ModifierStat Clone()
    {
        ModifierStat clone = new ModifierStat();
        clone.HpMod = HpMod.Clone();
        clone.MpMod = MpMod.Clone();
        clone.AtkMod = AtkMod.Clone();
        clone.DefMod = DefMod.Clone();
        clone.AgiMod = AgiMod.Clone();
        clone.CriPerMod = CriPerMod.Clone();
        clone.CriMod = CriMod.Clone();
        return clone;
    }
}
public class ModifiableStat
{
    public float BaseFlat { get; set; } = 0f;
    public float FlatOffset { get; set; } = 0f;
    public float RatePercent { get; set; } = 1.0f;
    public float FinalRate { get; set; } = 1.0f;

    public int TotalValue(int stat)
    {
        return (int)(((stat + BaseFlat) * RatePercent + FlatOffset) * FinalRate);
    }
    public float TotalFloat(float stat)
    {
        return ((stat + BaseFlat) * RatePercent + FlatOffset) * FinalRate;
    }

    public static ModifierStat GetTotalModifier(ModifierStat mod1, ModifierStat mod2)
    {
        ModifierStat result = new ModifierStat();
        result.HpMod = GetTotalModifiable(mod1.HpMod, mod2.HpMod);
        result.MpMod = GetTotalModifiable(mod1.MpMod, mod2.MpMod);
        result.AtkMod = GetTotalModifiable(mod1.AtkMod, mod2.AtkMod);
        result.DefMod = GetTotalModifiable(mod1.DefMod, mod2.DefMod);
        result.AgiMod = GetTotalModifiable(mod1.AgiMod, mod2.AgiMod);
        result.CriPerMod = GetTotalModifiable(mod1.CriPerMod, mod2.CriPerMod);
        result.CriMod = GetTotalModifiable(mod1.CriMod, mod2.CriMod);
        return result;
    }

    public static ModifiableStat GetTotalModifiable(ModifiableStat mod1, ModifiableStat mod2)
    {
        ModifiableStat result = new ModifiableStat();
        result.BaseFlat = mod1.BaseFlat + mod2.BaseFlat;
        result.FlatOffset = mod1.FlatOffset + mod2.FlatOffset;
        result.RatePercent = 1.0f + (mod1.RatePercent - 1.0f) + (mod2.RatePercent - 1.0f);
        result.FinalRate = mod1.FinalRate * mod2.FinalRate;
        return result;
    }

    public ModifiableStat Clone()
    {
        return (ModifiableStat)this.MemberwiseClone();
    }
}
public class Equipment
{
    public EquipmentType equipmentType;
    public BodyParts bodyParts;

    public Equipment Clone()
    {
        return (Equipment)this.MemberwiseClone();
    }
}

public class NotificationContainer
{
    public IReadOnlyList<Notification> Notifications => _notifications;
    private List<Notification> _notifications = new List<Notification>();

    public void AddNotify(Notification notification)
    {
        bool isExisting = IsExistNotify(notification.NotifyID);
        if(!isExisting)
        {
            _notifications.Add(notification);
            return;
        }
        switch (notification.StackType)
        {
            case NotifyStackType.Independent:
                _notifications.Add(notification);
                break;
            case NotifyStackType.Refresh:
                var notify = GetNotify(notification.NotifyID);
                notify?.SetRemainTime(Math.Max(notification.RemainTime, notify.RemainTime));
                break;
            case NotifyStackType.Ignore:
                break;

            default:
                break;
        }
    }
    public void RemoveNotify(GameId<INotificationId> notifyID)
    {
        var notification = Notifications.FirstOrDefault(n => n.NotifyID.Equals(notifyID));
        if (notification != null)
        {
            _notifications.Remove(notification);
        }
    }

    public void ExecuteNotifies(BattleManager battleManager, Phase currentPhase, ActionUnit? actionUnit, Entity? currentTarget)
    {
        foreach(var notify in Notifications.ToList())
        {
            notify.OnNotify(battleManager, currentPhase, actionUnit, currentTarget);
            //LogWrite.Log($"通知[{notify.NotifyID}]が発動試行した");
        }
        _notifications.RemoveAll(n => n.RemainTime == 0);
    }
    public void TickNotify()
    {
        foreach(var notify in Notifications.ToList())
        {
            if(notify.RemainTime > 0)
            {
                notify.ReduceRemainTime();
            }
            if(notify.RemainTime == 0)
            {
                RemoveNotify(notify.NotifyID);
            }
        }
    }

    public bool IsExistNotify(GameId<INotificationId> notifyID)
    {
        return Notifications.Any(n => n.NotifyID.Equals(notifyID));
    }
    public Notification? GetNotify(GameId<INotificationId> notifyID)
    {
        return Notifications.FirstOrDefault(n => n.NotifyID.Equals(notifyID));
    }

    public void ClearNotify()
    {
        _notifications.Clear();
    }

    public NotificationContainer Clone()
    {
        var clone = (NotificationContainer)this.MemberwiseClone();
        clone._notifications = new List<Notification>(_notifications.Select(n => n.Clone()));
        return clone;
    }
}


#endregion

#region 行動単位

public class ActionUnit(ActionType actionType,ActionSource actionSource, Entity executor, Entity target, 
    Skill? skill = null, UnitGuid? unitGuid = null, Guid? guid = null, DamageType? damageType = null,
    DamageInfo? damageInfo = null, HealInfo? healInfo = null, ApplyNotifyInfo? notifyInfo = null, UseItemInfo? useItemInfo = null, 
    bool? isForced = null)
{
    public UnitGuid ActionGuid { get; init; } = unitGuid ?? new UnitGuid();
    public Guid Guid { get; init; } = guid ?? Guid.NewGuid();
    public Entity Executor { get; init; } = executor;
    public Entity Target { get; init; } = target;
    public ActionType ActionType { get; init; } = actionType;
    public ActionSource ActionSource { get; init; } = actionSource;
    public DamageType DamageType { get; init; } = damageType ?? DamageType.Physical;
    public DamageInfo DamageInfo { get; init; } = damageInfo ?? new DamageInfo();
    public HealInfo HealInfo { get; init; } = healInfo ?? new();
    public ApplyNotifyInfo ApplyNotifyInfo { get; init; } = notifyInfo ?? new ApplyNotifyInfo();
    public UseItemInfo UseItemInfo { get; init; } = useItemInfo ?? new UseItemInfo();
    public string OnExecuteContent { get; private set; } = string.Empty;
    public Skill? Skill { get; init; } = skill;
    public bool IsForced { get; init; } = isForced ?? false;

    public void SetContent(string content) => OnExecuteContent = content;
}

public record DamageInfo()
{
    public float DamageMultiplier { get; set; } = 1.0f;
    public int FixedDamage { get; set; } = 0;
    public bool IsCounter { get; set; } = false;

}
public record HealInfo()
{
    public int HealValue { get; set; } = 0;
    public float HealMultiplier { get; set; } = 1.0f;
    public TargetPoint TargetPoint { get; set; } = TargetPoint.HP;
    public ReferType ReferType { get; set; } = ReferType.Max;
    public bool IsFixed { get; set; } = false;
}
public class UseItemInfo()
{
    public GameId<IItemId> ItemId { get; set; }
}

public class ApplyNotifyInfo()
{
    public GameId<INotificationId> NotifyID;
    public int ApplyRate;
    public string? OnApplyContent;

    public ApplyNotifyResult TryApplyNotify(IRandomProvider random, ActionUnit actionUnit, Entity target)
    {
        if(NotifyID.IsEmpty || target.Stat.IsDead)
        {
            return new ApplyNotifyResult(false, null);
        }
        int rdm = random.GetRandomInt(1, 101);

        if(rdm <= ApplyRate)
        {
            Notification notification = NotifyCreator.Creator(NotifyID, target);
            target.AddNotify(notification);

            string? context = (string.IsNullOrEmpty(OnApplyContent)) ? null : string.Format(OnApplyContent, target.Name, actionUnit.Executor.Name);
            return new ApplyNotifyResult(true, context);
        }
        return new ApplyNotifyResult(false, null);
    }
}
public record ApplyNotifyResult(bool Success, string? Context);
public class UnitGuid
{
    public Dictionary<Phase, HashSet<Entity>> ProcessedEntities { get; set; } = new Dictionary<Phase, HashSet<Entity>>();

    public bool IsProcessed(Phase currentPhase, Entity owner)
    {
        if (!ProcessedEntities.ContainsKey(currentPhase)) return false;
        return ProcessedEntities[currentPhase].Contains(owner);
    }

    public void MarkAsProcessed(Phase currentPhase, Entity entity)
    {
        if (!ProcessedEntities.ContainsKey(currentPhase))
        {
            ProcessedEntities[currentPhase] = new HashSet<Entity>();
        }
        ProcessedEntities[currentPhase].Add(entity);
    }
}

public record ActionSource
(
    ActionSourceType SourceType,
    Skill? Skill = null,
    GameId<IItemId> ItemId = default,
    Notification? Notification = null
)
{
    public static ActionSource Default => new (ActionSourceType.Default);
    public static ActionSource FromSkill(Skill skill) => new (ActionSourceType.Skill, skill);
    public static ActionSource FromItem(GameId<IItemId> itemId) => new (ActionSourceType.Item, null, itemId);
    public static ActionSource FromNotification(Notification notification) => 
        new (ActionSourceType.Notification, null, default, notification);
}
#endregion

#region 戦闘結果config
public record BattleResultConfig
(
    int TotalExp,
    int TotalGold,
    List<DropItem>? DropItems
);
#endregion

public record RewardConfig
(
    int Gold,
    int Exp,
    GameId<IDropItemTableId> DropTableId
);

public record BattleResult
(
    BattleResultType BattleResultType,
    bool ExitDungeon
);