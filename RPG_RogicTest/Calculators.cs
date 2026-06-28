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

public class BattleRewardCalculator(IRandomProvider random)
{
    private readonly IRandomProvider _random = random;

    public BattleResultConfig CalculateReward(IReadOnlyList<EnemyCharacter> enemies)
    {
        int totalGold = enemies.Sum(enemy => enemy.DropData.Gold);
        int totalExp = enemies.Sum(enemy => enemy.DropData.Exp);
        List<DropItem> dropItems = new List<DropItem>();
        foreach(var enemy in enemies)
        {
            var dropItem = RollDropItem(enemy.DropData.DropTableId);
            if(dropItem.ItemId != null)
                dropItems.Add(dropItem);
        }
        return new BattleResultConfig(totalExp, totalGold, dropItems);
    }

    private DropItem RollDropItem(GameId<IDropItemTableId> dropTableId)
    {
        var tableData = DropItemTableMasterData.GetDropItemTable(dropTableId);
        int totalWeight = tableData.Sum(item => item.DropWeight);
        int rdm = _random.GetRandomInt(0, totalWeight);
        foreach(var dropItem in tableData)
        {
            if(rdm < dropItem.DropWeight)
            {
                return new DropItem(dropItem.ItemID, dropItem.Amount, dropItem.Rarity);
            }
            rdm -= dropItem.DropWeight;
        }
        throw new InvalidOperationException("ドロップアイテムの抽選が正常に実行されませんでした");
    }
}

public record DropItem
(
    GameId<IItemId>? ItemId,
    int Amount,
    ItemRarity ItemRarity
);
