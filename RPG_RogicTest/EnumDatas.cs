using System;

#region Enumたち
public enum BodyParts { Head, Chest, Legs, Feet, Arms, Hands, LeftHand, RightHand, Blank };
public enum EquipmentType { Armor, Weapon, Blank };
public enum EnemyType { Normal, Boss };
public enum ActionType { Attack, Skill, Guard, Escape};
public enum DamageType { Physical, Magical, Dot, Heal, None};
public enum SkillType { Active, Passive};
public enum NotifyStackType { Refresh, Independent, Ignore, Replace}
public enum CostType { CurrentMP, CurrentHP, MaxMP, MaxHP};
public enum TargetType { All, Ally, Enemy, Self};
public enum EntityID {Hero, Npc, Slime, Goblin, Dragon }; //IDに置換
public enum EnemyID { Slime, Goblin, Dragon }; //IDに置換
public enum NpcID { Villager, Guard, Knight, King}; //IDに置換
public enum BattleResultType { Victory, Defeat, Escape};
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
