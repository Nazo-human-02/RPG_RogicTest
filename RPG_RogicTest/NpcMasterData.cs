using System;

static public class NpcMasterData
{
    static public IReadOnlyDictionary<GameId<INpcId>, NpcBaseData> NpcMasterDataBase => _npcMasterDataBase;
    static private readonly Dictionary<GameId<INpcId>, NpcBaseData> _npcMasterDataBase = new Dictionary<GameId<INpcId>, NpcBaseData>();

    static public void Load()
    {
        _npcMasterDataBase.Clear();

        _npcMasterDataBase["npc_001"] = new NpcBaseData("stat_npc_001", "テスト用村人", "こんにちは", IsShop: false);

    }

    static public NpcBaseData GetNpcData(GameId<INpcId> NpcID)
    {
        if (!NpcMasterDataBase.TryGetValue(NpcID, out var data))
        {
            throw new Exception($"NPC_ID:{NpcID}のデータが見つかりません");
        }
        return data;
    }
}

public record NpcBaseData
(
    GameId<IBaseStatId> BaseStatID, string properName, string content, bool IsShop
);