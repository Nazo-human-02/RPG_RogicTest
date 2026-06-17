using System;

public abstract class Entity : IEquipable, ITalkable
{
    public Dictionary<BodyParts, Equipment> Equipments { get => equipments; set => equipments = value; }
    private Dictionary<BodyParts, Equipment> equipments = new();

    public List<Notification> Notifications { get; set; } = new();

    public HashSet<Skill> ValidSkills { get; set; } = new();
    public string? Name { get; protected set; }
    public GameId<IBaseStatId> EntityID { get; protected set; }

    public string? Content { get; set; }

    public bool IsPartyMember { get; protected set; } = false;

    public BattleStat Stat { get; protected set; } = new BattleStat();

    protected Entity(string name, BattleStat battleStat, GameId<IBaseStatId> id)
    {
        InitializeEquipment();
        InitializeName(name);
        InitializeId(id);
        InitializeStat(battleStat);
    }

    protected void InitializeEquipment()
    {
        Equipment blankEquipment = new Equipment() { equipmentType = EquipmentType.Blank, bodyParts = BodyParts.Blank };
        foreach (BodyParts part in Enum.GetValues(typeof(BodyParts)))
        {
            if (part == BodyParts.Blank) continue;
            equipments[part] = blankEquipment;
        }
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
        UpdateStat();
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
        StatCalculator.SetUpLevelStat(Stat, EntityID);
    }

    public void SetCurrentStat()
    {
        StatCalculator.UpdateStat(Stat, EntityID);
    }

    public void AddNotify(Notification notification)
    {
        Notifications.Add(notification);
    }
    public void SetSkill(GameId<ISkillId> skillId)
    {
        Skill skill = SkillCreator.Create(skillId);
        ValidSkills.Add(skill);
    }
    public void RemoveSkill(GameId<ISkillId> skillId)
    {
        ValidSkills.RemoveWhere(skill => skill.SkillId == skillId);
    }
    public void OnDeath()
    {
        if(Stat.IsDead)
        {
            Notifications.Clear();
            if (this is EnemyCharacter)
            {
                LogWrite.Log($"{Name}を倒した！");
            }
            else
            {
                LogWrite.Log($"{Name}は倒された...");
            }
        }
    }
    public Entity Clone()
    {
        Entity clone = (Entity)this.MemberwiseClone();

        clone.Stat = new BattleStat()
        {
            CurrentHp = this.Stat.CurrentHp,
            CurrentMp = this.Stat.CurrentMp,
            MaxHp = this.Stat.MaxHp,
            MaxMp = this.Stat.MaxMp,

            expSet = new ExpSet()
            {
                CurrentLevel = this.Stat.expSet.CurrentLevel,
                CurrentExp = this.Stat.expSet.CurrentExp,
                TotalExp = this.Stat.expSet.TotalExp,
                ExpModifier = this.Stat.expSet.ExpModifier
            },

            baseStat = new BaseStat()
            {
                Atk = this.Stat.baseStat.Atk,
                Def = this.Stat.baseStat.Def,
                Agi = this.Stat.baseStat.Agi,
                CriPer = this.Stat.baseStat.CriPer,
                Cri = this.Stat.baseStat.Cri
            }
        };

        clone.Equipments = new Dictionary<BodyParts, Equipment>();
        foreach (var kvp in this.Equipments)
        {
            clone.Equipments[kvp.Key] = new Equipment()
            {
                equipmentType = kvp.Value.equipmentType,
                bodyParts = kvp.Value.bodyParts
            };
        }
        clone.ValidSkills = new HashSet<Skill>(this.ValidSkills);
        clone.Notifications = new List<Notification>();

        return clone;
    }
}

public class EnemyCharacter : Entity, IEnemy
{
    public EnemyType EnemyType { get; set; }
    public DropRewardData DropData { get; set; }

    public EnemyCharacter(string name, EnemyType enemyType, BattleStat battleStat, GameId<IBaseStatId> id, DropRewardData rewardData) 
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
