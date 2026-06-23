using System;


public static class EntityBaseStatMasterData
{
    public static IReadOnlyDictionary<GameId<IBaseStatId>, EntityBaseStatData> EntityBaseStatDatas => _entityBaseStatData;
    private static readonly Dictionary<GameId<IBaseStatId>, EntityBaseStatData> _entityBaseStatData = new Dictionary<GameId<IBaseStatId>, EntityBaseStatData>();
    
    public static void Load()
    {
        _entityBaseStatData.Clear();

        _entityBaseStatData["stat_hero_001"] = new EntityBaseStatData(
            Name:"主人公", 
            BaseHP:100, BaseMP:100, BaseAtk:300, BaseDef:120, BaseAgi:5, BaseCri:2f, BaseCriPer:50f);

        _entityBaseStatData["stat_npc_001"] = new EntityBaseStatData(
            Name: "村人",
            BaseHP: 50, BaseMP: 10, BaseAtk: 50, BaseDef: 50, BaseAgi: 1, BaseCri: 1.1f, BaseCriPer: 10f);

        _entityBaseStatData["stat_slime_001"] = new EntityBaseStatData(
            Name: "スライム",
            BaseHP: 60, BaseMP: 10, BaseAtk: 80, BaseDef: 60, BaseAgi: 1, BaseCri: 1.2f, BaseCriPer: 20f);

        _entityBaseStatData["stat_goblin_001"] = new EntityBaseStatData(
            Name: "ゴブリン",
            BaseHP: 80, BaseMP: 30, BaseAtk: 100, BaseDef: 90, BaseAgi: 3, BaseCri: 1.5f, BaseCriPer: 30f);

        _entityBaseStatData["stat_dragon_001"] = new EntityBaseStatData(
            Name: "ドラゴン",
            BaseHP: 200, BaseMP: 300, BaseAtk: 200, BaseDef: 220, BaseAgi: 8, BaseCri: 2.3f, BaseCriPer: 40f);

    }

    public static EntityBaseStatData GetEntityBaseStat(GameId<IBaseStatId> BaseStatID)
    {
        if(!EntityBaseStatDatas.TryGetValue(BaseStatID, out var baseStatData))
        {
            throw new Exception($"ステータスID:{BaseStatID}のデータが見つかりません");
        }
        return baseStatData;
    }
}

public record EntityBaseStatData
(
    string Name,
    int BaseHP, int BaseMP,
    int BaseAtk, int BaseDef, int BaseAgi,
    float BaseCri, float BaseCriPer
);