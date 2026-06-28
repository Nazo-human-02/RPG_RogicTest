using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ActionExecutor(BattleCalculator battleCalculator, ILogProvider logProvider)
{
    private readonly BattleCalculator _battleCalculator = battleCalculator;
    private readonly ILogProvider _logProvider = logProvider;
    private HashSet<Guid> _loggedAction { get; set; } = new HashSet<Guid>();
    public void ClearLogCache()
    {
        _loggedAction.Clear();
    }
    public void ExecuteAction(ActionUnit[] actionUnits, BattleManager battleManager)
    {
        if (!string.IsNullOrEmpty(actionUnits[0].OnExecuteContent) && !_loggedAction.Contains(actionUnits[0].Guid))
        {
            _logProvider.Log(actionUnits[0].OnExecuteContent);
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
                    _logProvider.Log("想定外のアクション");
                    break;

                case ActionType.Attack:
                    AttackAction(actionUnit.Executor, actionUnit.Target, actionUnit, loopCount);
                    break;

                case ActionType.Guard:
                    GuardAction(actionUnit, actionUnit.Executor);
                    break;

                case ActionType.Skill:
                    SkillAction(battleManager, actionUnit, actionUnit.Executor, loopCount);
                    break;

                case ActionType.Escape:
                    EscapeAction(actionUnit, actionUnit.Executor);
                    break;

            }

            BattleNotification.TriggerPhase(Phase.EndAction, actionUnit, null); //アクション終了
        }
    }

    private void AttackAction(Entity attacker, Entity target, ActionUnit actionUnit, int loopNum)
    {
        if (string.IsNullOrEmpty(actionUnit.OnExecuteContent) && loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid))
        {
            _logProvider.Log($"--{attacker.Name}のこうげき！");
            _logProvider.Log($"[{attacker.Name}]--->[{target.Name}]");
            _loggedAction.Add(actionUnit.Guid);
        }
        BattleNotification.TriggerPhase(Phase.BeforeAttack, actionUnit, target); //攻撃直前

        (bool cri, int dmg) = _battleCalculator.CalculateDamage(attacker.Stat, target.Stat, actionUnit.DamageInfo);
        string text = (cri) ? $"クリティカル!{target.Name}に{dmg}のダメージ！" : $"{target.Name}に{dmg}のダメージ";
        _logProvider.Log(text);
        bool isDead = target.Stat.TakeDamage(actionUnit, target, dmg);
        if (!isDead)
        {
            _logProvider.Log($"{target.Name}残りHP:[{target.Stat.CurrentHp}]");
        }
        else
        {
            if (target is EnemyCharacter)
            {
                _logProvider.Log($"{target.Name}を倒した！");
            }
            else
            {
                _logProvider.Log($"{target.Name}は倒された...");
            }

        }

        BattleNotification.TriggerPhase(Phase.AfterAttack, actionUnit, target); //攻撃直後

    }

    private void GuardAction(ActionUnit actionUnit, Entity guarder)
    {
        BattleNotification.TriggerPhase(Phase.BeforeGuard, actionUnit, guarder); //ガード直前

        _logProvider.Log($"--{guarder.Name}はガードした");

        BattleNotification.TriggerPhase(Phase.AfterGuard, actionUnit, guarder); //ガード直後
    }

    private void SkillAction(BattleManager battleManager, ActionUnit actionUnit, Entity executor, int loopNum)
    {
        BattleNotification.TriggerPhase(Phase.BeforeSkill, actionUnit, executor);

        if (string.IsNullOrEmpty(actionUnit.OnExecuteContent) && loopNum == 1)
        {
            _logProvider.Log($"--{executor.Name}はスキルを使用した");
        }
        if (actionUnit.Skill == null)
        {
            _logProvider.Log($"--しかしうまくいかなかった");
        }
        else
        {
            if (actionUnit.Skill is ActiveSkill activeSkill)
            {
                bool success = activeSkill.TryPayCost(executor);
                if (!success)
                {
                    _logProvider.Log($"--しかしコストが足りなかった");
                }
                else
                {
                    actionUnit.Skill.ExecuteSkill(battleManager, actionUnit, actionUnit.Target);
                    if (loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid))
                    {
                        _logProvider.Log($"--{executor.Name}の{actionUnit.Skill.Name}!");
                        actionUnit.Skill.SetCoolTime();
                    }
                }
            }
            else
            {
                actionUnit.Skill.ExecuteSkill(battleManager, actionUnit, actionUnit.Target);
                if (loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid))
                {
                    _logProvider.Log($"--{executor.Name}の{actionUnit.Skill.Name}!");
                    actionUnit.Skill.SetCoolTime();
                }
            }
        }

        BattleNotification.TriggerPhase(Phase.AfterSkill, actionUnit, executor);
    }

    private void EscapeAction(ActionUnit actionUnit, Entity executor)
    {
        BattleNotification.TriggerPhase(Phase.BeforeEscape, actionUnit, executor);

        _logProvider.Log($"--{executor.Name}は逃げ出した");

        BattleNotification.TriggerPhase(Phase.AfterEscape, actionUnit, executor);
    }
}
