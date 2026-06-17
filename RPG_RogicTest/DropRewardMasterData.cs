using System;

static public class DropRewardMasterData
{
    public static IReadOnlyDictionary<GameId<IDropRewardId>, DropRewardData> DropRewardBaseDatas => _dropRewardBaseDatas;
    private static readonly Dictionary<GameId<IDropRewardId>, DropRewardData> _dropRewardBaseDatas = new Dictionary<GameId<IDropRewardId>, DropRewardData>();

    static public void Load()
    {
        _dropRewardBaseDatas.Clear();

        _dropRewardBaseDatas["drop_000"] = new DropRewardData(Gold:0, Exp:0);
        _dropRewardBaseDatas["drop_001"] = new DropRewardData(100, 10);
        _dropRewardBaseDatas["drop_002"] = new DropRewardData(20000, 99999);
    }


    static public DropRewardData GetDropData(GameId<IDropRewardId> dropID)
    {
        if(!DropRewardBaseDatas.TryGetValue(dropID, out var dropRewardData))
        {
            throw new Exception($"ドロップID:{dropID}のデータが見つかりません");
        }
        return dropRewardData;
    }
}

public record DropRewardData
(
    int Gold,
    int Exp
    //List<int> ItemIdList = new();
);