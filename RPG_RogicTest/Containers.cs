using System;

#region データコンテナ

public class BattleStat
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

    public void TakeDamage(ActionUnit? actionUnit, Entity target, int dmg)
    {
        CurrentHp -= dmg;

        if(CurrentHp <= 0)
        {
            CurrentHp = 0;
            BattleNotification.TriggerPhase(Phase.OnDeath, actionUnit, target);
            if(IsDead)
            {
                target.OnDeath();
                return;
            }
        }
        BattleNotification.TriggerPhase(Phase.OnHitDamage, actionUnit, target);
    }
}
public class ExpSet
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

    public void GetExp(Entity entity,int exp)
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
        LogWrite.Log($"{entity.Name}は[{exp}exp]獲得した");
        if(isLevelUp)
        {
            LogWrite.Log($"{entity.Name}はレベルアップした!:[Lv{currentLevel}]--->[Lv{CurrentLevel}]");
            entity.UpdateStat();
        }

    }

    public void SetLevel(int level)
    {
        CurrentLevel = level;
    }

}
public class BaseStat
{
    public int Atk { get; set; } = 10;
    public int Def { get; set; } = 5;
    public int Agi { get; set; } = 4;
    public float CriPer { get; set; } = 10f; //%表示
    public float Cri { get; set; } = 1.5f; //倍率
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
}
public class Equipment
{
    public EquipmentType equipmentType;
    public BodyParts bodyParts;
}



#endregion

#region 行動単位

public class ActionUnit(ActionType actionType, Entity executor, Entity target, 
    Skill? skill = null, UnitGuid? unitGuid = null, Guid? guid = null, DamageType? damageType = null, DamageInfo? damageInfo = null, bool? isForced = null)
{
    public UnitGuid ActionGuid { get; init; } = unitGuid ?? new UnitGuid();
    public Guid Guid { get; init; } = guid ?? Guid.NewGuid();
    public Entity Executor { get; init; } = executor;
    public Entity Target { get; init; } = target;
    public ActionType ActionType { get; init; } = actionType;
    public DamageType DamageType { get; init; } = damageType ?? DamageType.Physical;
    public DamageInfo DamageInfo { get; init; } = damageInfo ?? new DamageInfo();
    public string OnExecuteContent { get; private set; } = string.Empty;
    public Skill? Skill { get; init; } = skill;
    public bool IsForced { get; init; } = isForced ?? false;

    public void SetContent(string content) => OnExecuteContent = content;
}

public class DamageInfo()
{
    public float DamageMultiplier { get; set; } = 1.0f;
    public int FixedDamage { get; set; } = 0;
    public bool IsCounter { get; set; } = false;

}

public class ApplyNotifyInfo(GameId<INotificationId> notifyID, int applyRate = 0, string? content = null)
{
    public GameId<INotificationId> NotifyID { get; init; } = notifyID;
    public int ApplyRate { get; private set; } = applyRate;
    public string OnApplyContent = content ?? string.Empty;

    public bool TryApplyNotify(ActionUnit actionUnit, Entity target)
    {
        if(target.Stat.IsDead)
        {
            return false;
        }
        int rdm = Random.Shared.Next(1, 101);

        if(rdm <= ApplyRate)
        {
            Notification notification = NotifyCreator.Creator(NotifyID, target);
            target.AddNotify(notification);

            if(!string.IsNullOrEmpty(OnApplyContent))
            {
                LogWrite.Log(string.Format(OnApplyContent, target.Name, actionUnit.Executor.Name));
            }
            return true;
        }
        return false;
    }
}
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
#endregion