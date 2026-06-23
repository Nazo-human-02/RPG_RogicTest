using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RouteContentGenerator(IRandomProvider random)
{
    private IRandomProvider _random = random;
    private EnemySpawnSelector _enemySpawnSelector = new(random);
    private EnemyGenerator _enemygenerator = new(random);
     public RouteContentData CreateEventContent(DungeonEventType eventType, FloorData floorData)
    {
        return (eventType) switch
        {
            DungeonEventType.None => new NoneEventContent(),
            DungeonEventType.Treasure => new TreasureEventContent(),
            DungeonEventType.Battle => CreateBattleEventContent(floorData),
            _ => new NoneEventContent()
        };
    }

    private BattleEventContent CreateBattleEventContent(FloorData floorData)
    {
        AreaData areaData = AreaMasterData.GetAreaData(floorData.AreaID);
        SpawnEnemyTable spawnTable = EnemyTableMasterData.GetEnemyTable(areaData.SpawnTableID);
        int spawnAmount = _random.GetRandomInt(areaData.SpawnMinAmount, areaData.SpawnMaxAmount + 1);
        List<SpawnConfig> configs = _enemySpawnSelector.RandomSpawn(spawnTable, spawnAmount);
        List<EnemyCharacter> enemies = _enemygenerator.CreateNomalEnemies(configs);
        return new BattleEventContent(enemies);
    }
}


public abstract class RouteContentData
{

}

public class NoneEventContent : RouteContentData
{

}
public class BattleEventContent(List<EnemyCharacter> enemies) : RouteContentData
{
    public IReadOnlyList<EnemyCharacter> EnemyParty = enemies;
}

public class TreasureEventContent : RouteContentData
{

}