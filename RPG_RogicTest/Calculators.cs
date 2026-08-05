using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;



public class TurnScheduler(IRandomProvider randomProvider)
{
    private readonly IRandomProvider _randomProvider = randomProvider;
    const int AgilityVariance = 3;
    public Queue<ActionUnit[]> ActionOrder(List<ActionUnit[]> units)
    {
        var guidSpeedDict = units.GroupBy(u => u[0].Guid).ToDictionary(group => group.Key,group => 
        {
                var representative = group.First();
                return representative[0].Executor.Stat.baseStat.Agi + _randomProvider.GetRandomInt(1, AgilityVariance + 1);
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
public record DropItem
(
    GameId<IItemId>? ItemId,
    int Amount,
    ItemRarity ItemRarity
);
