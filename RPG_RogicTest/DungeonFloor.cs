using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DungeonFloor(int floorNum)
{
    public int FloorNumber { get; } = floorNum;
    public FloorData FloorData { get; } = DungeonFloorMasterData.GetFloorData(floorNum);
    public int CurrentProgress { get; private set; }
    public bool IsBossReached => CurrentProgress >= FloorData.BossDistance;

    public void Advance(int progress)
    {
        CurrentProgress += progress;
    }

    public SpawnEnemyTable GetSpawnTable()
    {
        var areaData = AreaMasterData.GetAreaData(FloorData.AreaID);
        var spawnTable = EnemyTableMasterData.GetEnemyTable(areaData.SpawnTableID);
        return spawnTable;
    }
}

public static class DungeonFloorMasterData
{
    public static IReadOnlyDictionary<int, FloorData> FloorDataBase => _floorDataBase;
    private static readonly Dictionary<int, FloorData> _floorDataBase = new();

    public static void Load()
    {
        _floorDataBase.Clear();

        _floorDataBase[1] = new FloorData("area_001", 30, 
            (DungeonEventType.Battle, 80),(DungeonEventType.None, 20));
        _floorDataBase[2] = new FloorData("area_002", 20, 
            (DungeonEventType.Battle, 100));
    }
    public static FloorData GetFloorData(int floorNum)
    {
        if(FloorDataBase.TryGetValue(floorNum, out var floorData))
        {
            return floorData;
        }
        throw new Exception($"{floorNum}層のフロアデータが見つかりません");
    }
}

public record FloorData
(
    GameId<IAreaId> AreaID,
    int BossDistance,
    params (DungeonEventType, int)[] EventRate
    //宝箱のIDとかも追加予定
);


