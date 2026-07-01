using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

public abstract class EffectBase
{
    public abstract EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource);
}

public class HealEffect(int healAmount, bool isFixed, ReferType referType, TargetPoint targetPoint) : EffectBase
{
    public readonly int HealAmount = healAmount;
    private readonly bool IsFixed = isFixed;
    private readonly ReferType ReferType = referType;
    private readonly TargetPoint TargetPoint = targetPoint;

    public override EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource)
    {
        HealInfo healInfo =
            new() { HealValue = HealAmount, IsFixed = IsFixed, ReferType = ReferType, TargetPoint = TargetPoint};
        ActionUnit actionUnit
            = new(ActionType.Heal, actionSource, effectContent.User, effectContent.Target, healInfo:healInfo);
        if (effectContent.IsBattle)
        {
            //effectContent.BattleManager?.InsertInterruptAction(actionUnit);
            Console.WriteLine($"戦闘中のため、{effectContent.Target.Name}の{TargetPoint.ToString()}を回復する処理を割り込みアクションとして登録しました。");
            return new("", actionUnit);
        }
        else
        {
            (bool correctDone, int heal) = effectContent.BattleCalculator.CalculateHeal(effectContent.Target.Stat, healInfo);

            if(correctDone)
                effectContent.Target.Stat.TakeHeal(heal);
            string text = (correctDone) ? $"{effectContent.Target.Name}の{TargetPoint.ToString()}が{heal}回復した!"
                : $"{effectContent.Target.Name}はすでに力尽きている";
            return new($"{text}");
        }
        
    }
}

public class DamageEffect(int damage, bool isFixed, float multiPlier = 1.0f, int hitCount= 1) : EffectBase
{
    private readonly int _damage = damage;
    private readonly bool _isFixed = isFixed;
    private readonly float _multiPlier = multiPlier;
    public override EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource)
    {
        DamageInfo info = new DamageInfo() { DamageMultiplier = _multiPlier, FixedDamage = _damage };
        ActionUnit actionUnit =
            new ActionUnit(ActionType.Attack, actionSource, effectContent.User, effectContent.Target, damageInfo: info);
        if (effectContent.IsBattle)
        {
            //effectContent.BattleManager?.InsertInterruptAction(actionUnit);
        }
        else
        {
            int dmg = effectContent.BattleCalculator.CalculateDamage
                (effectContent.User.Stat, effectContent.Target.Stat, info, false).Item2;
            effectContent.Target.Stat.TakeDamage(actionUnit, effectContent.Target, dmg);
        }
        return new("", actionUnit);
    }
}

public class RemoveNotifyEffect(int successRate, AilmentType ailmentType) : EffectBase
{
    private readonly int _successRate = successRate;
    private readonly AilmentType _ailmentType = ailmentType;

    public override EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource)
    {
        int rdm = effectContent.RandomProvider.GetRandomInt(1, 101);
        if(rdm < _successRate)
        {
            //notifyにAilemntTypeの項目を追加して、見つけたら除去
        }
        return new("");
    }
}

public class AddNotifyEffect(GameId<INotificationId> notifyID) : EffectBase
{
    private readonly GameId<INotificationId> _notifyID = notifyID;

    public override EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource)
    {
        var notify = NotifyCreator.Creator(_notifyID, effectContent.Target);
        effectContent.Target.AddNotify(notify); //notify追加はActionにしなくていいかな
        return new("");
    }
}

public class EscapeEffect() : EffectBase
{
    public override EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource)
    {
        effectContent.BattleManager?.RequestExitDungeon();
        return new ("");
    }
}

public class  RepeatEffect(int repeatCount, List<EffectBase> effects) : EffectBase
{
    public override EffectResult ApplyEffect(EffectContent effectContent, ActionSource actionSource)
    {
        List<(ActionUnit, int)> actionUnits = new List<(ActionUnit, int)>();
        for(int n = 0; n < repeatCount; n++)
        {
            foreach(var effect in effects)
            {
                var result = effect.ApplyEffect(effectContent, actionSource);
                if(result.ActionUnit != null)
                {
                    effectContent.BattleManager?.StackInterruptAction(result.ActionUnit, n);
                }
            }
        }
        return new ("");
    }
}

public record EffectResult
(
    string Message,
    ActionUnit? ActionUnit = null
);