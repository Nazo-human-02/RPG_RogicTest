using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class TargetResolver
{
    static public TargetResolveResult GetTargetResolve
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
            if( ConditionChecker.Check(conditionData, context))
            {
                candidates.Add(entity);
            }
        }
        
        return new TargetResolveResult(candidates, targetData.TargetSelectType, targetData.TargetAmount);
    }
    static private List<Entity> GetBaseTargetCandidates(PartyController partyController, 
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
}

public record TargetResolveResult
(
    List<Entity> TargetCandidates,
    TargetSelectType TargetSelectType,
    int TargetAmount
)
{
    public static TargetResolveResult NullResult()
        => new(new(), TargetSelectType.Self, 0);
}