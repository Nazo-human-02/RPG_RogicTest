using System;

public abstract class Entity : ITalkable
{
    public EquipmentController EquipmentController { get; set; }

    public NotificationContainer Notifications { get; set; } = new();

    public HashSet<Skill> ValidSkills { get; set; } = new();
    public string? Name { get; protected set; }
    public GameId<IBaseStatId> EntityID { get; protected set; }

    public string? Content { get; set; }

    public bool IsPartyMember { get; protected set; } = false;

    public BattleStat Stat { get; protected set; } = new BattleStat();

    protected Entity(string name, BattleStat battleStat, GameId<IBaseStatId> id)
    {
        EquipmentController = new(this);
        EquipmentController.Initialize();
        InitializeName(name);
        InitializeId(id);
        InitializeStat(battleStat);
    }

    protected void InitializeName(string name)
    {
        Name = name;
    }
    protected void InitializeId(GameId<IBaseStatId> id)
    {
        EntityID = id;
    }
    protected void InitializeStat(BattleStat stat)
    {
        Stat = stat;
        //UpdateStat();
    }
    public void AddParty()
    {
        if (!IsPartyMember) IsPartyMember = true;
    }

    public void RemoveParty()
    {
        if (IsPartyMember) IsPartyMember = false;
    }

    public void UpdateStat()
    {
        EntityBaseStatData baseStatData = EntityBaseStatMasterData.GetEntityBaseStat(EntityID);
        StatCalculator.UpdateStat(Stat, baseStatData);
    }
    public void UpdateEquipmentStat()
    {
        var equipMod = EquipmentController.GetTotalModifier();
        Stat.UpDateEquipStat(equipMod);
        UpdateStat();
    }

    public void SetLevelUpStat()
    {
        EntityBaseStatData baseStatData = EntityBaseStatMasterData.GetEntityBaseStat(EntityID);
        StatCalculator.SetUpLevelStat(Stat, baseStatData);
    }

    public void AddNotify(Notification notification)
    {
        Notifications.AddNotify(notification);
    }
    public void SetSkill(GameId<ISkillId> skillId)
    {
        Skill skill = SkillCreator.Create(skillId);
        ValidSkills.Add(skill);
    }
    public void DirectSetSkill(Skill skill)
    {
        ValidSkills.Add(skill);
    }
    public void RemoveSkill(GameId<ISkillId> skillId)
    {
        ValidSkills.RemoveWhere(skill => skill.SkillInfo.SkillId == skillId);
    }
    public void ClearSkillCoolTime()
    {
        foreach (var skill in ValidSkills) skill.SetCoolTime(0);
    }
    public void ReduceSkillCoolTime()
    {
        foreach (var skill in ValidSkills) skill.ReduceCoolTime();
    }
    public void OnDeath()
    {
        if(Stat.IsDead)
        {
            Notifications.ClearNotify();
        }
    }
    public Entity Clone()
    {
        Entity clone = (Entity)this.MemberwiseClone();

        clone.Stat = this.Stat.Clone();

        clone.EquipmentController = this.EquipmentController.Clone(clone);

        clone.ValidSkills = new HashSet<Skill>();
        foreach(Skill skill in this.ValidSkills) 
        {
            clone.ValidSkills.Add(skill.Clone());
        }
        clone.Notifications = this.Notifications.Clone();

        return clone;
    }
}

public class EnemyCharacter : Entity, IEnemy
{
    public EnemyType EnemyType { get; set; }
    public RewardConfig DropData { get; set; }
    public GameId<IEnemyId> EnemyID;

    public EnemyCharacter(string name, EnemyType enemyType, BattleStat battleStat, GameId<IBaseStatId> id, RewardConfig rewardData) 
        : base(name, battleStat, id)
    {
        Name = name;
        EnemyType = enemyType;
        DropData = rewardData;
    }

    public void Rename(string reName)
    {
        Name = reName;
    }
}

public abstract class CharacterBase : Entity, IMovable
{
    public int MoveSpeed { get; set; }

    protected CharacterBase(string name, BattleStat battleStat, GameId<IBaseStatId> id) : base (name, battleStat, id)
    {

    }
}

public class NonPlayerCharacter : CharacterBase, INpc
{
    public bool IsShop { get; set; }

    public NonPlayerCharacter(string name, BattleStat battleStat, GameId<IBaseStatId> id, string content, bool isShop) : base(name, battleStat, id)
    {
        Content = content;
        IsShop = isShop;
    }
}

public class MainCharacter : CharacterBase
{
    public MainCharacter(string name, BattleStat battleStat, GameId<IBaseStatId> id) : base (name, battleStat, id) 
    {
        Name = name;
        IsPartyMember = true;
    }
}
