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
    public void ExecuteAction(ActionUnit[] actionUnits, BattleManager battleManager, ConditionContext conditionContext)
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
                    AttackAction(actionUnit.Executor, actionUnit.Target, actionUnit, loopCount, battleManager);
                    break;

                case ActionType.Guard:
                    GuardAction(actionUnit, actionUnit.Executor);
                    break;

                case ActionType.Skill:
                    SkillAction(battleManager, conditionContext, actionUnit, actionUnit.Executor, loopCount);
                    break;

                case ActionType.Escape:
                    EscapeAction(actionUnit, actionUnit.Executor, battleManager);
                    break;

                case ActionType.UseItem:
                    UseItemAction(actionUnit, conditionContext, loopCount, battleManager);
                    break;

                case ActionType.Heal:
                    HealAction(actionUnit, conditionContext, loopCount, battleManager);
                    break;

            }

            if (battleManager.ExitRequested)
                return;

            BattleNotification.TriggerPhase(Phase.EndAction, actionUnit, null); //アクション終了
        }
    }

    private void AttackAction(Entity attacker, Entity target, ActionUnit actionUnit, int loopNum, BattleManager battleManager)
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

        if (battleManager.ExitRequested)
            return;

        BattleNotification.TriggerPhase(Phase.AfterAttack, actionUnit, target); //攻撃直後

    }

    private void GuardAction(ActionUnit actionUnit, Entity guarder)
    {
        BattleNotification.TriggerPhase(Phase.BeforeGuard, actionUnit, guarder); //ガード直前

        _logProvider.Log($"--{guarder.Name}はガードした");

        BattleNotification.TriggerPhase(Phase.AfterGuard, actionUnit, guarder); //ガード直後
    }

    private void SkillAction
        (BattleManager battleManager, ConditionContext conditionContext, ActionUnit actionUnit, Entity executor, int loopNum)
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
            EffectContent content = new(actionUnit.Executor, actionUnit.Target, 
                battleManager, _battleCalculator, conditionContext.RandomProvider);

            if (actionUnit.Skill is ActiveSkill activeSkill)
            {
                bool success = activeSkill.TryPayCost(executor);
                if (!success)
                {
                    _logProvider.Log($"--しかしコストが足りなかった");
                }
                else
                {
                    actionUnit.Skill.ExecuteSkill(actionUnit, actionUnit.Target, content);
                    if (loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid))
                    {
                        _logProvider.Log($"--{executor.Name}の{actionUnit.Skill.SkillInfo.SkillName}!");
                        actionUnit.Skill.SetCoolTime();
                    }
                }
            }
            else
            {
                actionUnit.Skill.ExecuteSkill(actionUnit, actionUnit.Target, content);
                if (loopNum == 1 && !_loggedAction.Contains(actionUnit.Guid))
                {
                    _logProvider.Log($"--{executor.Name}の{actionUnit.Skill.SkillInfo.SkillName}!");
                    actionUnit.Skill.SetCoolTime();
                }
            }
        }

        if (battleManager.ExitRequested)
            return;

        BattleNotification.TriggerPhase(Phase.AfterSkill, actionUnit, executor);
    }

    private void EscapeAction(ActionUnit actionUnit, Entity executor, BattleManager battleManager)
    {
        BattleNotification.TriggerPhase(Phase.BeforeEscape, actionUnit, executor);

        _logProvider.Log($"--{executor.Name}は逃げ出した");

        BattleNotification.TriggerPhase(Phase.AfterEscape, actionUnit, executor);

        battleManager.RequestExitDungeon();
    }

    private void UseItemAction
        (ActionUnit actionUnit, ConditionContext conditionContext, int loopNum, BattleManager battleManager)
    {
        BattleNotification.TriggerPhase(Phase.BeforeUseItem, actionUnit, actionUnit.Executor);


        if(actionUnit.UseItemInfo.ItemId.IsEmpty)
        {
            _logProvider.Log($"{actionUnit.Executor.Name}は使うアイテムがなく困っている");
        }

        else
        {
            bool canUse = ItemMasterData.TryUseItem(actionUnit.UseItemInfo.ItemId,
                conditionContext with { User = actionUnit.Executor, Target = actionUnit.Target }, out var itemData);
            if (string.IsNullOrEmpty(actionUnit.OnExecuteContent) && loopNum == 1)
            {
                _logProvider.Log($"{actionUnit.Executor.Name}は{itemData.ItemName}を使用した");
            }
            if (canUse)
            {
                foreach (var effect in itemData.ItemEffectData.ItemEffects)
                {
                    EffectContent content = 
                        new(actionUnit.Executor, actionUnit.Target, 
                        battleManager, _battleCalculator, conditionContext.RandomProvider);
                    var result = effect.ApplyEffect(content, ActionSource.FromItem(actionUnit.UseItemInfo.ItemId));
                    if (!string.IsNullOrEmpty(result.Message))
                        _logProvider.Log(result.Message);
                    if(result.ActionUnit != null)
                    {
                        battleManager.InsertInterruptAction(result.ActionUnit);
                    }
                }
                conditionContext.PartyController.Inventory.RemoveItem(actionUnit.UseItemInfo.ItemId, 1);
            }
            else
            {
                _logProvider.Log($"しかしうまくいかなかった");
            }
        }
        if (battleManager.ExitRequested)
            return;
        BattleNotification.TriggerPhase(Phase.AfterUseItem, actionUnit, actionUnit.Executor);
    }

    private void HealAction(ActionUnit actionUnit, ConditionContext conditionContext, int loopNum, BattleManager battleManager)
    {
        BattleNotification.TriggerPhase(Phase.BeforeHeal, actionUnit, actionUnit.Target);

        (bool doneCorrext, int healAmount) = _battleCalculator.CalculateHeal(actionUnit.Target.Stat, actionUnit.HealInfo);

        string message = "";
        if(doneCorrext)
        {
            actionUnit.Target.Stat.TakeHeal(healAmount);
            message = $"{actionUnit.Target.Name}の{actionUnit.HealInfo.TargetPoint.ToString()}が{healAmount}回復した";

        }
        else
        {
            message =
                $"{actionUnit.Executor.Name}は{actionUnit.Target.Name}を回復しようとしたが" +
                $"既に{actionUnit.Target.Name}は力尽きていた";
        }
        _logProvider.Log(message);
        if (battleManager.ExitRequested)
            return;

        BattleNotification.TriggerPhase(Phase.AfterHeal, actionUnit, actionUnit.Target);
    }
}
