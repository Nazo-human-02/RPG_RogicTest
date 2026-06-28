using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class ConditionBase
{
    public abstract bool CanExecute(ConditionContext conditionContext);

    protected bool CompareValue(int target, int refer, CompareType compareType)
    {
        return (compareType) switch
        {
            CompareType.More => target > refer,
            CompareType.MoreOrEqual => target >= refer,
            CompareType.Less => target < refer,
            CompareType.LessOrEqual => target <= refer,
            CompareType.Equal => target == refer,
            CompareType.NotEqual => target != refer,
            _ => false
        };

    }

    protected Entity? GetConditionTarget(ConditionContext conditionContext, ConditionTarget conditionTarget)
    {
        return (conditionTarget) switch
        {
            ConditionTarget.User => conditionContext.User,
            ConditionTarget.Target => conditionContext.Target,
            _ => null
        };
    }
}

public class FieldCondition(FieldValidType fieldValidType) : ConditionBase
{
    private readonly FieldValidType _fieldValidType = fieldValidType;
    public override bool CanExecute(ConditionContext conditionContext)
    {
        return (_fieldValidType) switch
        {
            FieldValidType.OutsideBattleOnly //ダンジョン外での戦闘のみ
                => (conditionContext.IsBattle && conditionContext.FieldContext.FieldType != FieldType.Dungeon),
            FieldValidType.DungeonExploreOnly //戦闘外のダンジョン内のみ
                => (!conditionContext.IsBattle && conditionContext.FieldContext.FieldType == FieldType.Dungeon),
            FieldValidType.OutsideExploreOnly //戦闘外のダンジョン外のみ
                => (!conditionContext.IsBattle && conditionContext.FieldContext.FieldType != FieldType.Dungeon),
            FieldValidType.DungeonBattleOnly //ダンジョン内の戦闘のみ
                => (conditionContext.IsBattle && conditionContext.FieldContext.FieldType == FieldType.Dungeon),
            FieldValidType.AnyBattle //戦闘中ならどこでも
                => (conditionContext.IsBattle),
            FieldValidType.AnywhereDungeon //ダンジョン内ならいつでも
                => (conditionContext.FieldContext.FieldType == FieldType.Dungeon),
            FieldValidType.AnywhereOutside //ダンジョン外ならいつでも
                => (conditionContext.FieldContext.FieldType != FieldType.Dungeon),
            FieldValidType.AnyExplore //戦闘外ならどこでも
                => (!conditionContext.IsBattle),
            _ => false
        };
    }
}
public class CurrentPointCondition(TargetPoint targetPoint, int condition, bool isFixed, 
    ConditionTarget conditionTarget, CompareType compareType) : ConditionBase
{
    private readonly TargetPoint _targetPoint = targetPoint;
    private readonly int _condition = condition;
    private readonly bool _isFixed = isFixed;
    private readonly ConditionTarget _conditionTarget = conditionTarget;
    private readonly CompareType _compareType = compareType;

    public override bool CanExecute(ConditionContext conditionContext)
    {
        var target = GetConditionTarget(conditionContext, _conditionTarget);
        if(target == null) return false;
        int point = GetTargetPoint(target.Stat);
        int refer = GetReferTargetPoint(target.Stat);
        return CompareValue(point, refer, _compareType);

    }
    private int GetTargetPoint(BattleStat stat)
    {
        return (_targetPoint) switch
        {
            TargetPoint.HP => stat.CurrentHp,
            TargetPoint.MP => stat.CurrentMp,
            _ => 0
        };
    }
    private int GetReferTargetPoint(BattleStat stat)
    {
        if (_isFixed) return _condition;
        return (_targetPoint) switch
        {
            TargetPoint.HP => (int)(stat.TotalHP * _condition / 100f),
            TargetPoint.MP => (int)(stat.TotalMP * _condition / 100f),
            _ => 1
        };
    }
}
public class StatCondition(StatType statType, int condition, ConditionTarget conditionTarget, CompareType compareType) : ConditionBase
{
    private readonly StatType _statType = statType;
    private readonly int _condition = condition;
    private readonly ConditionTarget _conditionTarget = conditionTarget;
    private readonly CompareType _compareType = compareType;

    public override bool CanExecute(ConditionContext conditionContext)
    {
        int? target = GetStatValue(conditionContext);
        if(target == null) return false;
        return CompareValue((int)target, _condition, _compareType);

    }

    private int? GetStatValue(ConditionContext conditionContext)
    {
        var target = GetConditionTarget(conditionContext, _conditionTarget);
        if(target == null) return null;
        return GetValue(target.Stat);
    }
    private int GetValue(BattleStat battleStat)
    {
        return (_statType) switch
        {
            StatType.Hp => battleStat.MaxHp,
            StatType.Mp => battleStat.MaxMp,
            StatType.Atk => battleStat.baseStat.Atk,
            StatType.Def => battleStat.baseStat.Def,
            StatType.Agi => battleStat.baseStat.Agi,
            StatType.Cri => (int)battleStat.baseStat.Cri, //切り捨てで一旦対応
            StatType.Criper => (int)battleStat.baseStat.CriPer, //ここも同様
            _ => throw new NotImplementedException()
        };
    }
}

public class LevelCondition(int level, ConditionTarget conditionTarget, CompareType compareType) : ConditionBase
{
    private readonly int _level = level;
    private readonly ConditionTarget _conditionTarget = conditionTarget;
    private readonly CompareType _compareType = compareType;
    public override bool CanExecute(ConditionContext conditionContext)
    {
        int? currentLevel = GetCurrentLevel(conditionContext);
        if (currentLevel == null) 
            return false;
        return CompareValue((int)currentLevel, _level, _compareType);

    }

    private int? GetCurrentLevel(ConditionContext conditionContext)
    {
        var target = GetConditionTarget(conditionContext, _conditionTarget);
        return target?.Stat.expSet.CurrentLevel;
    }
}

public class ItemCondition(GameId<IItemId> itemID, int amount, CompareType compareType) : ConditionBase
{
    private readonly GameId<IItemId> _itemID = itemID;
    private readonly int _amount = amount;
    private readonly CompareType _compareType = compareType;

    public override bool CanExecute(ConditionContext conditionContext)
    {
        //使用者のインベントリから該当アイテムが指定個数分あるか確認する処理
        return false;
    }
}

public class EquipmentCondition(GameId<IEquipmentId> equipmentID, bool shouldEquip) : ConditionBase
{
    private readonly GameId<IEquipmentId> _equipmentID = equipmentID;
    private readonly bool _shouldEquip = shouldEquip;

    public override bool CanExecute(ConditionContext conditionContext)
    {
        //使用者の装備欄から該当装備があるか確認する処理
        return false;
    }
}

public class FloorCondition(int floorNum, CompareType compareType) : ConditionBase
{
    private readonly int _floorNum = floorNum;
    private readonly CompareType _compareType = compareType;

    public override bool CanExecute(ConditionContext conditionContext)
    {
        if (conditionContext.FieldContext.FieldType != FieldType.Dungeon) return false;
        int currentFloor = conditionContext.FieldContext.FloorNumber;
        return CompareValue(currentFloor, _floorNum, _compareType);

    }
}

public class PartyCondition(int partyAmount, CompareType compareType) : ConditionBase
{
    private readonly int _partyAmount = partyAmount;
    private readonly CompareType _compareType = compareType;

    public override bool CanExecute(ConditionContext conditionContext)
    {
        int currentMember = conditionContext.PartyController.PartyMember.Count;
        return CompareValue(currentMember, _partyAmount, _compareType);
    }
}
//使用者以外を参照する条件とかも作ってみたい

