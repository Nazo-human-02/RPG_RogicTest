using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TargetResolver
{
    public TargetResolveResult TargetResolve
        (ConditionData conditionData, ConditionContext conditionContext, TargetData targetData)
    {
        IReadOnlyList<EnemyCharacter> enemies = conditionContext.BattleSession?.Enemies ?? new List<EnemyCharacter>();
        List<Entity> candidates = new List<Entity>();
        List<Entity> entities =
            GetBaseTargetCandidates(conditionContext.PartyController, enemies,
            targetData.TargetType, conditionContext.User!);
        
        foreach(var entity in entities)
        {
            ConditionContext context = conditionContext with { Target = entity };
            bool canUse = CheckCondition(conditionData, context);
            if (canUse) candidates.Add(entity);
        }
        
        return new TargetResolveResult(candidates, targetData.TargetSelectType, targetData.TargetAmount);
    }
    private List<Entity> GetBaseTargetCandidates(PartyController partyController, 
        IReadOnlyList<EnemyCharacter> enemies, TargetType targetType, Entity user)
    {
        bool isEnemy = user is EnemyCharacter;
        return targetType switch
        {
            TargetType.Enemy => (isEnemy) ? partyController.PartyMember.Cast<Entity>().ToList() 
                : enemies.Cast<Entity>().ToList(),
            TargetType.Ally => (isEnemy) ? enemies.Cast<Entity>().ToList() 
                : partyController.PartyMember.Cast<Entity>().ToList(),
            TargetType.Self => new List<Entity>() { user },
            TargetType.All => partyController.PartyMember.Cast<Entity>().Concat(enemies).ToList(),
            _ => new List<Entity>() { user },
        };
    }
    private bool CheckCondition(ConditionData conditionData, ConditionContext conditionContext)
    {
        if (conditionData.Conditions.Count == 0)
            return true;
        foreach(var condition in conditionData.Conditions)
        {
            bool canUse = condition.CanExecute(conditionContext);
            if(canUse && conditionData.LogicalOperator == LogicalOperator.Or)
                return true;
            if (!canUse && conditionData.LogicalOperator == LogicalOperator.And)
                return false;
        }
        return (conditionData.LogicalOperator == LogicalOperator.And);
    }
}

public record TargetResolveResult
(
    List<Entity> TargetCandidates,
    TargetSelectType TargetSelectType,
    int TargetAmount
);