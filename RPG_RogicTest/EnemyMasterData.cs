using System;

static public class EnemyMasterData
{

    public static IReadOnlyDictionary<GameId<IEnemyId>, EnemyData> EnemyMasterDataBase => _enemyMasterDataBase;
    private static readonly Dictionary<GameId<IEnemyId>, EnemyData> _enemyMasterDataBase = new Dictionary<GameId<IEnemyId>, EnemyData>();


    public static void Load()
    {
        _enemyMasterDataBase.Clear();

        _enemyMasterDataBase["enemy_slime_001"] = new EnemyData("stat_slime_001", "drop_001", "drop_table_000");
        _enemyMasterDataBase["enemy_goblin_001"] = new EnemyData("stat_goblin_001", "drop_002", "drop_table_000");
        _enemyMasterDataBase["enemy_dragon_001"] = new EnemyData("stat_dragon_001", "drop_002", "drop_table_001");

    }

    static public EnemyData GetEnemyData(GameId<IEnemyId> enemyID)
    {
        if (!EnemyMasterDataBase.TryGetValue(enemyID, out var enemyData))
        {
            throw new Exception($"エンティティID：{enemyID}のデータが見つかりません");
        }
        return enemyData;
    }
}


public class EnemyData(GameId<IBaseStatId> statId, GameId<IDropRewardId> dropId, GameId<IDropItemTableId> dropTableId)
{
    public GameId<IBaseStatId> StatId {  get; private set; } = statId;
    public GameId<IDropRewardId> DropId {  get; private set; } = dropId;
    public GameId<IDropItemTableId> DropTableId { get; private set; } = dropTableId;
}
