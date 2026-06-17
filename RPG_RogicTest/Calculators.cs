using System;

#region 計算機
static public class StatCalculator
{
    static public void SetUpLevelStat(BattleStat battleStat, GameId<IBaseStatId> id)
    {
        UpdateStat(battleStat, id);
        battleStat.CurrentHp = battleStat.MaxHp;
        battleStat.CurrentMp = battleStat.MaxMp;
    }

    static public void UpdateStat(BattleStat battleStat, GameId<IBaseStatId> statID)
    {
        if (!EntityBaseStatMasterData.EntityBaseStatDatas.TryGetValue(statID, out var baseData))
        {
            throw new Exception($"エンティティID:{statID}が見つかりません");
        }

        int lv = battleStat.expSet.CurrentLevel;

        battleStat.MaxHp = baseData.BaseHP + (lv - 1) * 20;
        battleStat.MaxMp = baseData.BaseMP + (lv - 1) * 15;

        battleStat.baseStat.Atk = baseData.BaseAtk + (lv - 1) * 5;
        battleStat.baseStat.Def = baseData.BaseDef + (lv - 1) * 3;
    }
}

static public class BattleCalculator
{
    static public (bool, int) BaseDamageCal(Entity attacker, Entity target, DamageInfo damageInfo) //使用スキルも追加予定
    {
        if(damageInfo.FixedDamage > 0)
            return (false, damageInfo.FixedDamage);
        int baseDmg = Math.Max(0, attacker.Stat.TotalAtk - target.Stat.TotalDef);
        int variance = Math.Max(1, (int)(baseDmg * 0.1f));
        int damage = baseDmg + Random.Shared.Next(-variance, variance + 1);
        damage = Math.Max(1, damage);
        if(damageInfo.DamageMultiplier > 1f)
        {
            damage = (int)(damage * damageInfo.DamageMultiplier);
        }
        bool cri = IsCritical(attacker, target);
        int result = (cri) ? (int)(damage * attacker.Stat.TotalCri) : damage;
        return (cri, result);
    }
    
    static public bool IsCritical(Entity attacker, Entity target) //使用スキルなどでの補正も将来入れたい
    {
        float modifier =(float)(attacker.Stat.expSet.CurrentLevel - target.Stat.expSet.CurrentLevel)*1.5f;
        float rdm = (float)Random.Shared.NextDouble() * 100f;
        float per = MathF.Max(1f, attacker.Stat.TotalCriPer + modifier);
        return per > rdm;
    }
}

static public class TurnScheduler
{
    static public Queue<ActionUnit[]> ActionOrder(List<ActionUnit[]> units)
    {
        var guidSpeedDict = units.GroupBy(u => u[0].Guid).ToDictionary(group => group.Key,group => 
        {
                var representative = group.First();
                return representative[0].Executor.Stat.baseStat.Agi + Random.Shared.Next(1, 4);
        });

        List<ActionUnit[]> sortedList = units.OrderByDescending(unit => guidSpeedDict[unit[0].Guid]).ToList();
        Queue<ActionUnit[]> result = new Queue<ActionUnit[]>();
        foreach (ActionUnit[] unit in sortedList)
        {
            result.Enqueue(unit);
        }
        return result;
    }
}

public static class ActionExecutor
{
    private static HashSet<Guid> _loggedAction = new HashSet<Guid>();
    public static void ClearLogCache()
    {
        _loggedAction.Clear();
    }
    public static void ExecuteAction(ActionUnit[] actionUnits)
    {
        if (!string.IsNullOrEmpty(actionUnits[0].OnExecuteContent) && !_loggedAction.Contains(actionUnits[0].Guid))
        {
            LogWrite.Log(actionUnits[0].OnExecuteContent);
            _loggedAction.Add(actionUnits[0].Guid);
        }

        int loopCount = 0;
        HashSet<Entity> targets = new HashSet<Entity>();
        foreach (ActionUnit actionUnit in actionUnits)
        {
            if ((actionUnit.Target.Stat.IsDead || actionUnit.Executor.Stat.IsDead) && !actionUnit.IsForced)
            {
                continue;
            }

            loopCount++;
            targets.Add(actionUnit.Target);
            BattleNotification.TriggerPhase(Phase.StartAction, actionUnit, null); //アクション開始

            switch (actionUnit.ActionType)
            {
                default:
                    LogWrite.Log("想定外のアクション");
                    break;

                case ActionType.Attack:
                    AttackAction(actionUnit.Executor, actionUnit.Target, actionUnit, loopCount);
                    break;

                case ActionType.Guard:
                    GuardAction(actionUnit, actionUnit.Executor);
                    break;

                case ActionType.Skill:
                    SkillAction(actionUnit, actionUnit.Executor, loopCount);
                    break;

                case ActionType.Escape:
                    EscapeAction(actionUnit, actionUnit.Executor);
                    break;

            }

            BattleNotification.TriggerPhase(Phase.EndAction, actionUnit, null); //アクション終了
        }
    }

    private static void AttackAction(Entity attacker, Entity target, ActionUnit actionUnit, int loopNum)
    {
        if (string.IsNullOrEmpty(actionUnit.OnExecuteContent) && loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid))
        {
            LogWrite.Log($"--{attacker.Name}のこうげき！");
            LogWrite.Log($"[{attacker.Name}]--->[{target.Name}]");
            _loggedAction.Add(actionUnit.Guid);
        }
        BattleNotification.TriggerPhase(Phase.BeforeAttack, actionUnit, target); //攻撃直前

        (bool cri, int dmg) = BattleCalculator.BaseDamageCal(attacker, target, actionUnit.DamageInfo);
        string text = (cri) ? $"クリティカル!{target.Name}に{dmg}のダメージ！" : $"{target.Name}に{dmg}のダメージ";
        LogWrite.Log(text);
        target.Stat.TakeDamage(actionUnit, target, dmg);
        if(!target.Stat.IsDead)
        {
            LogWrite.Log($"{target.Name}残りHP:[{target.Stat.CurrentHp}]");
        }

        BattleNotification.TriggerPhase(Phase.AfterAttack, actionUnit, target); //攻撃直後
        
    }

    private static void GuardAction(ActionUnit actionUnit, Entity guarder)
    {
        BattleNotification.TriggerPhase(Phase.BeforeGuard, actionUnit, guarder); //ガード直前

        LogWrite.Log($"--{guarder.Name}はガードした");

        BattleNotification.TriggerPhase(Phase.AfterGuard, actionUnit, guarder); //ガード直後
    }

    private static void SkillAction(ActionUnit actionUnit, Entity executor, int loopNum)
    {
        BattleNotification.TriggerPhase(Phase.BeforeSkill, actionUnit, executor);

        if (string.IsNullOrEmpty(actionUnit.OnExecuteContent) && loopNum == 1)
        {
            LogWrite.Log($"--{executor.Name}はスキルを使用した");
        }
        if(actionUnit.Skill == null)
        {
            LogWrite.Log($"--しかしうまくいかなかった");
        }
        else
        {
            actionUnit.Skill.ExecuteSkill(actionUnit, actionUnit.Target);
            if (loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid)) 
            { 
                LogWrite.Log($"--{executor.Name}の{actionUnit.Skill.Name}!");
                actionUnit.Skill.SetCoolTime();
            }
        }

        BattleNotification.TriggerPhase(Phase.AfterSkill, actionUnit, executor);
    }

    private static void EscapeAction(ActionUnit actionUnit, Entity executor)
    {
        BattleNotification.TriggerPhase(Phase.BeforeEscape, actionUnit, executor);

        LogWrite.Log($"--{executor.Name}は逃げ出した");

        BattleNotification.TriggerPhase(Phase.AfterEscape, actionUnit, executor);
    }
}
#endregion