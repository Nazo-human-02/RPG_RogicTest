using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class AreaMasterData
{
    public static IReadOnlyDictionary<GameId<IAreaId>, AreaData> AreaDataBase => _areaDataBase;
    private static readonly Dictionary<GameId<IAreaId>, AreaData> _areaDataBase = new();

    public static void Load()
    {
        _areaDataBase.Clear();

        _areaDataBase["Area_001"] = new AreaData("table_enemy_001",1,4);
        _areaDataBase["Area_002"] = new AreaData("table_enemy_002", 10, 12);
    }

    public static AreaData GetAreaData(GameId<IAreaId> areaID)
    {
        if(AreaDataBase.TryGetValue(areaID, out var areaData))
        {
            return areaData;
        }
        throw new Exception($"エリアID:{areaID}のエリアデータが見つかりません");
    }
}


public record AreaData
(
    GameId<IEnemyTableId> SpawnTableID,
    int SpawnMinAmount,
    int SpawnMaxAmount
    //GameId<IDropItemTableId> AreaDropTableID
    //エリア特有のドロップテーブルとか環境影響とかをいれる予定
);
