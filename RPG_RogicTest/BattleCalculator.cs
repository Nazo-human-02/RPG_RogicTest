using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleCalculator(IRandomProvider randomProvider)
{
    private readonly IRandomProvider _randomProvider = randomProvider;
    public (bool, int) CalculateDamage(BattleStat attacker, BattleStat target, DamageInfo damageInfo, bool validCritical = true) //使用スキルも追加予定
    {
        if (IsFixedDamage(damageInfo))
            return (false, damageInfo.FixedDamage);

        int damage = CalculateBaseDamage(attacker.TotalAtk, target.TotalDef);
        damage = ApplyDamageMultiplier(damage, damageInfo.DamageMultiplier);
        float criticalRate = CalculateCriticalRate(attacker, target);
        bool cri = IsCritical(criticalRate);
        int result = (cri && validCritical) ? ApplyCriticalMultiplier(damage, attacker.TotalCri) : damage;
        return (cri, result);
    }
    #region ダメージ計算機補助関数
    private bool IsFixedDamage(DamageInfo damageInfo)
    {
        return damageInfo.FixedDamage > 0;
    }
    private int CalculateBaseDamage(int atk, int def)
    {
        int dmg = Math.Max(0, atk - def);
        int variance = Math.Max(1, (int)(dmg * 0.1f));
        dmg += _randomProvider.GetRandomInt(-variance, variance + 1);
        return Math.Max(1, dmg);
    }

    private int ApplyDamageMultiplier(int dmg, float multiplier)
    {
        return (int)(dmg * multiplier);
    }
    public float CalculateCriticalRate(BattleStat attacker, BattleStat target)
    {
        float modifier = (attacker.expSet.CurrentLevel - target.expSet.CurrentLevel) * 1.5f;
        return MathF.Max(1f, attacker.TotalCriPer + modifier);
    }
    private bool IsCritical(float criticalRate) //使用スキルなどでの補正も将来入れたい
    {
        float rdm = (float)_randomProvider.GetRandomFloat() * 100f;
        return criticalRate > rdm;
    }
    private int ApplyCriticalMultiplier(int dmg, float criticalMultiplier)
    {
        return (int)(dmg * criticalMultiplier);
    }
    #endregion

    public (bool, int) CalculateHeal(BattleStat target, HealInfo healInfo)
    {
        bool doneCorrect = !target.IsDead;

        if (healInfo.IsFixed)
            return (doneCorrect, healInfo.HealValue);
        int healAmount = CalculateBaseHeal(target, healInfo);
        healAmount = ApplyHealMultiplier(healAmount, healInfo.HealMultiplier);
        return (doneCorrect, healAmount);
    }
    #region 回復量計算機補助関数
    private int CalculateBaseHeal(BattleStat targetStat, HealInfo healInfo)
    {
        (int current, int max) = (healInfo.TargetPoint) switch
        {
            TargetPoint.HP => (targetStat.CurrentHp, targetStat.TotalHP),
            TargetPoint.MP => (targetStat.CurrentMp, targetStat.TotalMP),
            _ => (1, 1)
        };
        return (healInfo.ReferType) switch
        {
            ReferType.Current => (int)(current * healInfo.HealValue / 100f),
            ReferType.Max => (int)(max * healInfo.HealValue / 100f),
            _ => healInfo.HealValue
        };
    }
    private int ApplyHealMultiplier(int healAmount, float multiplier)
    {
        return (int)(healAmount * multiplier);
    }
    #endregion
}
