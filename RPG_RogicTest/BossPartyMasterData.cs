using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class BossPartyMasterData
{
    public static IReadOnlyDictionary<GameId<IBossPartyId>, BossParty> BossPartyDatabase => _bossPartyDatabase;
    private static readonly Dictionary<GameId<IBossPartyId>, BossParty> _bossPartyDatabase = new();

    public static void Load()
    {
        _bossPartyDatabase.Clear();

        _bossPartyDatabase["boss_001"] = new BossParty(
            new BossMember("enemy_Dragon_001", true, 20, 24, [], null) );

        _bossPartyDatabase["boss_002"] = new BossParty(
            new BossMember("enemy_Dragon_001", true, 30, 35, [], null),
            new BossMember("enemy_Goblin_001", false, 20, 25, [], null),
            new BossMember("enemy_Goblin_001", false, 20, 25, [], null));
    }

    public static BossParty GetBossParty(GameId<IBossPartyId> bossPartyID)
    {
        if(BossPartyDatabase.TryGetValue(bossPartyID, out var data))
        {
            return data;
        }
        throw new InvalidOperationException($"ボスパーティーID:{bossPartyID}のデータが見つかりません");
    }
}

public record BossParty
(
    params BossMember[] BossMembers
);

public record BossMember
(
    GameId<IEnemyId> EnemyID,
    bool IsBoss,
    int MinLevel,
    int MaxLevel,
    GameId<ISkillId>[] ExtraSkills,
    GameId<IDropItemTableId>? ExtraDropTable
);
