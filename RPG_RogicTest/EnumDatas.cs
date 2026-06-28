using System;

#region Enumたち
public enum BodyParts { Head, Chest, Legs, Feet, Arms, Hands, LeftHand, RightHand, Blank };
public enum EquipmentType { Armor, Weapon, Blank };
public enum ItemRarity { Common, Rare, SuperRare};
public enum EnemyType { Normal, Boss };
public enum ActionType { Attack, Skill, Guard, Escape, Item, UseItem};
public enum DirectionType { Left, Right, Center};
public enum DungeonEventType { Battle, Treasure, None}
public enum DamageType { Physical, Magical, Dot, Heal, None};
public enum SkillType { Active, Passive};
public enum NotifyStackType { Refresh, Independent, Ignore, Replace}　//Notifyの重複処理タイプ
public enum CostType { CurrentMP, CurrentHP, MaxMP, MaxHP};
public enum ReferType { Current, Max};
public enum TargetPoint { HP, MP};
public enum StatType { Hp, Mp, Atk, Def, Agi, Cri, Criper};
public enum FieldValidType {DungeonBattleOnly, DungeonExploreOnly, OutsideBattleOnly, OutsideExploreOnly,
    AnyBattle, AnywhereDungeon, AnywhereOutside, AnyExplore };
public enum FieldType { Dungeon, OutSide};
public enum CompareType { More, MoreOrEqual, Less, LessOrEqual, Equal, NotEqual};
public enum ConditionTarget { User, Target};
public enum LogicalOperator { And, Or };
public enum AilmentType { Poison, Sleep, Paralysis, Burn};
public enum ItemCategory { Consumable, Tool, Unique, Valuable, Material};
public enum TargetType { All, Ally, Enemy, Self, None};
public enum TargetSelectType { Random, Self};
public enum BattleResultType { Victory, Defeat, Escape, ContinueBattle};
public enum Phase
{
    StartBattle,
    StartTurn,
    StartAction,
    BeforeAttack,
    AfterAttack,
    BeforeGuard,
    AfterGuard,
    BeforeSkill,
    AfterSkill,
    BeforeEscape,
    AfterEscape,
    EndAction,
    OnHitDamage,
    OnAvoidAttack,
    OnDeath,
    EndTurn,
    EndBattle,
    None
}
public enum NotifyType { None, Counter, Heal, Poison } //必要か分からない

#endregion
