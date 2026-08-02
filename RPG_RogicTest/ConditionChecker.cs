using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ConditionChecker
{
    public static bool Check(ConditionData conditionData, ConditionContext conditionContext)
    {
        if (conditionData.Conditions.Count == 0)
            return true;
        foreach (var condition in conditionData.Conditions)
        {
            bool canUse = condition.CanExecute(conditionContext);
            if (canUse && conditionData.LogicalOperator == LogicalOperator.Or)
                return true;
            if (!canUse && conditionData.LogicalOperator == LogicalOperator.And)
                return false;
        }
        return (conditionData.LogicalOperator == LogicalOperator.And);
    }
}