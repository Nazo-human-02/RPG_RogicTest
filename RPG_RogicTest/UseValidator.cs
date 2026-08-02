using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class UseValidator
{
    public static UseLessType TryUseSkill
        (ConditionContext conditionContext, Skill skill, out TargetResolveResult targetResolveResult)
    {
        targetResolveResult = TargetResolver.GetTargetResolve(skill.ConditionData, conditionContext, skill.TargetData);

        if (conditionContext.User == null)
            return UseLessType.Error;

        if (skill.CurrentCoolTime > 0)
            return UseLessType.CoolTime;

        if (skill is ActiveSkill active && !active.CanPaySkillCost(conditionContext.User))
            return UseLessType.NotEnoughCost;

        if (!ConditionChecker.Check(skill.ConditionData, conditionContext))
            return UseLessType.ConditionDisMatch;

        if (targetResolveResult.TargetCandidates.Count <= 0)
            return UseLessType.NoneTargets;

        return UseLessType.UseAble;
    }

    public static UseLessType TryUseItem
        (ConditionContext conditionContext, ItemData itemData, out TargetResolveResult resolveResult)
    {
        resolveResult = TargetResolver.GetTargetResolve(itemData.ConditionData, conditionContext, itemData.TargetData);

        if (conditionContext.User == null)
            return UseLessType.Error;

        if (itemData.ItemCategory != ItemCategory.Consumable && itemData.ItemCategory != ItemCategory.Tool)
            return UseLessType.Invalid;


        if (!ConditionChecker.Check(itemData.ConditionData, conditionContext))
            return UseLessType.ConditionDisMatch;

        if (resolveResult.TargetCandidates.Count <= 0)
            return UseLessType.NoneTargets;

        return UseLessType.UseAble;
    }

    public static UseLessType TryEquip() //将来的に作る予定、既装備が着脱できるかは判定せず、純粋に装備できるかを判定
    {
        return UseLessType.Invalid;
    }

    public static bool CanUse(UseLessType useLessType)
    {
        return useLessType switch
        {
            UseLessType.UseAble => true,

            _ => false
        };
    }
}