using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class EnemyTableMasterData
{
    public static IReadOnlyDictionary<GameId<IEnemyTableId>, SpawnEnemyTable> EnemyTableData => _enemyTableData;
    private static readonly Dictionary<GameId<IEnemyTableId>, SpawnEnemyTable> _enemyTableData = new();

    public static void Load()
    {
        _enemyTableData.Clear();

        _enemyTableData["table_enemy_001"] = new SpawnEnemyTable
            (
                new BossPartyConfigs(new BossConfig("boss_001", 100)),
                new SpawnConfig("enemy_slime_001", 1, 3, 80, EnemyType.Normal),
                new SpawnConfig("enemy_slime_001", 10, 15, 50, EnemyType.Normal),
                new SpawnConfig("enemy_goblin_001", 10, 15, 50, EnemyType.Normal),
                new SpawnConfig("enemy_dragon_001", 20, 20, 10, EnemyType.Normal)
            );
        _enemyTableData["table_enemy_002"] = new SpawnEnemyTable
            (
                new BossPartyConfigs(new BossConfig("boss_001", 80), new BossConfig("boss_002", 20)),
                new SpawnConfig("enemy_goblin", 15, 15, 100, EnemyType.Normal),
                new SpawnConfig("enemy_dragon_001", 21, 21, 10, EnemyType.Normal)
            );
    }

    public static SpawnEnemyTable GetEnemyTable(GameId<IEnemyTableId> tableID)
    {
        if(EnemyTableData.TryGetValue(tableID, out var configList))
        {
            return configList;
        }
        else
        {
            throw new Exception($"スポーンテーブルID:{tableID}のデータが見つかりません");
        }
    }
}

public record SpawnEnemyTable
(
    //GameId<IAreaDropTableId> AreaDropTableId とかを追加したい
    BossPartyConfigs BossPartyConfigs,
    params SpawnConfig[] SpawnConfigs
    
);
public record SpawnConfig
(
    GameId<IEnemyId> EnemyID,
    int MinLevel,
    int MaxLevel,
    int SpawnRate,
    EnemyType EnemyType
    //GameId<IDropItemTableId>? AdditionDropTableId = null
    //params GameId<ISkillId>[]? Skills
);
public record BossPartyConfigs
(
    params BossConfig[] BossConfigs
);
public record BossConfig
(
    GameId<IBossPartyId> BossPartyID,
    int SpawnRate
);
