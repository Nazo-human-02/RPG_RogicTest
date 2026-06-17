using System;

public static class AreaEntitySetting
{
    public static IReadOnlyDictionary<GameId<IAreaId>, EnemyCandidateList> AreaEnemyCandidates => _areaEnemyCandidates;
    private static readonly Dictionary<GameId<IAreaId>, EnemyCandidateList> _areaEnemyCandidates = new Dictionary<GameId<IAreaId>, EnemyCandidateList>();

    public static void AreaEnemySet()
    {
        EnemyCandidateList area1_CandidateList = new EnemyCandidateList();

        area1_CandidateList.AddCandidate("enemy_slime_001", EnemyType.Normal, 1, 5, 70);
        area1_CandidateList.AddCandidate("enemy_goblin_001", EnemyType.Normal, 3, 8, 30);
        area1_CandidateList.AddCandidate("enemy_dragon_001", EnemyType.Boss, 6, 8, 100);

        _areaEnemyCandidates["area_001"] = area1_CandidateList;
    }

    public static List<EnemyCharacter> RandomSpawnEnemy(GameId<IAreaId> areaID, int spawnAmount)
    {
        EnemyCandidateList candidateList = GetEnemyCandidateList(areaID);
        List<EnemyCharacter> enemyList = candidateList.RamdomSpawnEnemy(spawnAmount);
        return enemyList;
    }

    public static EnemyCandidateList GetEnemyCandidateList(GameId<IAreaId> areaID)
    {
        if(!AreaEnemyCandidates.TryGetValue(areaID, out var candidateList))
        {
            throw new Exception($"エリアID:{areaID}の出現エネミーデータが見つかりません");
        }
        return candidateList;
    }
}

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