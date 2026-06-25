using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

public abstract class ItemEffectBase
{
    public abstract void ApplyEffect(EffectContent effectContent);
}

public class HealEffect(int healAmount, bool isFixed, ReferType referType, TargetPoint targetPoint) : ItemEffectBase
{
    public readonly int HealAmount = healAmount;
    private readonly bool IsFixed = isFixed;
    private readonly ReferType ReferType = referType;
    private readonly TargetPoint TargetPoint = targetPoint;

    public override void ApplyEffect(EffectContent effectContent)
    {
        foreach (var target in effectContent.Targets)
        {
            int heal = CalHealAmount(target);
            target.Stat.TakeHeal(heal);
        }
    }

    private int CalHealAmount(Entity target)
    {
        if (IsFixed) return HealAmount;
        (int current, int max) = (TargetPoint)  switch 
        {
            TargetPoint.HP => (target.Stat.CurrentHp, target.Stat.TotalHP),
            TargetPoint.MP => (target.Stat.CurrentMp, target.Stat.TotalMP),
            _ => (1, 1)
        };
        return (ReferType) switch
        {
            ReferType.Current => (int)( current * HealAmount / 100f),
            ReferType.Max => (int)( max * HealAmount / 100f),
            _ => HealAmount
        };
    }

}

public class DamageEffect(int damage, bool isFixed, float multiPlier = 1.0f) : ItemEffectBase
{
    private readonly int _damage = damage;
    private readonly bool _isFixed = isFixed;
    private readonly float _multiPlier = multiPlier;
    public override void ApplyEffect(EffectContent effectContent)
    {
        foreach(var target in effectContent.Targets)
        {
            ActionUnit actionUnit = new ActionUnit(ActionType.Item, effectContent.User, target);
            int dmg = CalDamage(effectContent.User, target, effectContent);
            target.Stat.TakeDamage(actionUnit, target, dmg);
        }
    }
    public int CalDamage(Entity user, Entity target, EffectContent effectContent)
    {
        if(_isFixed) return _damage;
        else
        {
            DamageInfo info = new DamageInfo() { DamageMultiplier = _multiPlier};
            int dmg = effectContent.BattleCalculator.CalculateDamage(user.Stat, target.Stat, info, false).Item2;
            return dmg;
        }
    }
}

public class RemoveNotifyEffect(int successRate, AilmentType ailmentType) : ItemEffectBase
{
    private readonly int _successRate = successRate;
    private readonly AilmentType _ailmentType = ailmentType;

    public override void ApplyEffect(EffectContent effectContent)
    {
        int rdm = effectContent.RandomProvider.GetRandomInt(1, 101);
        if(rdm < _successRate)
        {
            //notifyにAilemntTypeの項目を追加して、見つけたら除去
        }
    }
}

public class AddNotifyEffect(GameId<INotificationId> notifyID) : ItemEffectBase
{
    private readonly GameId<INotificationId> _notifyID = notifyID;

    public override void ApplyEffect(EffectContent effectContent)
    {
        foreach (var target in effectContent.Targets)
        {
            var notify = NotifyCreator.Creator(_notifyID, target);
            target.AddNotify(notify);
        }
    }
}

public class EscapeEffect() : ItemEffectBase
{
    public override void ApplyEffect(EffectContent effectContent)
    {
        //ダンジョンから離脱し、入口の移動orホームへ帰還する処理
    }
}


public record EffectContent
(
    Entity User,
    IReadOnlyList<Entity> Targets,

    BattleManager? BattleManager,
    DungeonManager? DungeonManager,

    BattleCalculator BattleCalculator,
    IRandomProvider RandomProvider
);