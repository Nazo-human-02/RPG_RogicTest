using System;

#region Enumたち
public enum BodyParts { Head, Chest, Legs, Feet, Arms, Hands, LeftHand, RightHand, Blank };
public enum EquipmentType { Armor, Weapon, Blank };
public enum ItemRarity { Common, Rare, SuperRare};
public enum EnemyType { Normal, Boss };
public enum ActionType { Attack, Skill, Guard, Escape, Item, UseItem, Heal};
public enum ActionSourceType { Default, Skill, Item, Notification };
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
public enum LifeState {Alive, Dead, Any };
public enum TargetSelectType { Random, Self};
public enum BattleResultType { Victory, Defeat, Escape, ContinueBattle};
public enum Phase
{
    StartBattle, //戦闘開始
    StartTurn, //ターン開始
    StartAction, //アクション開始
    BeforeAttack, //攻撃前
    AfterAttack, //攻撃後
    BeforeGuard, //防御前
    AfterGuard, //防御後
    BeforeSkill, //スキル使用前
    AfterSkill, //スキル使用後
    BeforeEscape, //逃走前
    AfterEscape, //逃走後
    BeforeUseItem, //アイテム使用前
    AfterUseItem, //アイテム使用後
    BeforeHeal, //回復前
    AfterHeal, //回復後
    EndAction, //アクション終了
    OnHitDamage, //ダメージヒット時
    OnAvoidAttack, //攻撃回避時
    OnDeath, //死亡時
    EndTurn, //ターン終了
    EndBattle, //戦闘終了
    None //該当なし
}
public enum NotifyType { None, Counter, Heal, Poison } //必要か分からない

#endregion
