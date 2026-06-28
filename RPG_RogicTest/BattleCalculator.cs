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
}
