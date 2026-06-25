using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class CostMasterData
{
    public static IReadOnlyDictionary<GameId<ICostId>, CostData> CostMasterDataBase => _costMasterDataBase;
    private static readonly Dictionary<GameId<ICostId>, CostData> _costMasterDataBase = new Dictionary<GameId<ICostId>, CostData>();

    public static void Load()
    {
        _costMasterDataBase.Clear();

        _costMasterDataBase["cost_000"] = new CostData(CostType.CurrentMP, true, 0);
        _costMasterDataBase["cost_001"] = new CostData(CostType.MaxMP, true, 10);
        _costMasterDataBase["cost_002"] = new CostData(CostType.CurrentMP, false, 50);
    }

    public static CostData GetCostData(GameId<ICostId> costID)
    {
        if (!CostMasterDataBase.TryGetValue(costID, out var costData))
        {
            throw new Exception($"コストID:{costID}のデータが見つかりません");
        }
        return costData;
    }
}

public class CostData(CostType CostType, bool isFixed, int cost)
{
    public CostType CostType = CostType;
    public bool IsFixed = isFixed;
    public int Cost = cost;
}