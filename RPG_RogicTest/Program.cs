using System;

//パーティー管理,操作
public static class PartyController
{
    public static IReadOnlySet<CharacterBase> PartyMember => _partyMember;
    private static readonly HashSet<CharacterBase> _partyMember = new();
    static int MaxPartyMember = 4;
    public static int OwnedGold { get; private set; } = 0;
    

    public static void AddMember(CharacterBase chara)
    {
        if(!_partyMember.Contains(chara))
        {
            if(_partyMember.Count >= MaxPartyMember)
            {
                ChangeMember();
            }
            _partyMember.Add(chara);
            chara.AddParty();
        }
    }

    public static void RemoveMember(CharacterBase chara)
    {
        if(_partyMember.Contains(chara))
        {
            _partyMember.Remove(chara);
            chara.RemoveParty();
        }
    }

    static void ChangeMember()
    {
        //選択してキャラをRemoveする処理
    }

    public static void GetReward(DropRewardData dropRewardData)
    {
        AddGold(dropRewardData.Gold);
        LogWrite.Log($"_____戦利品を獲得した_____");
        LogWrite.Log($"～{dropRewardData.Gold}G手に入れた！(所持金{OwnedGold})～");
        foreach(Entity entity in PartyMember)
        {
            if(entity.Stat.IsDead)
            {
                continue;
            }
            entity.Stat.expSet.GetExp(entity, dropRewardData.Exp);
        }
    }

    public static void AddGold(int gold)
    {
        OwnedGold += gold;
    }
    public static bool SpendGold(int gold)
    {
        if(OwnedGold < gold)
        {
            return false;
        }
        OwnedGold -= gold;
        return true;
    }
}

//敵エンティティリスト
public class EnemyCandidate
{
    public EnemyCharacter Enemy { get; set; }
    int MaxLevel { get; set; } = 5;
    int MinLevel { get; set; } = 1;
    public int SpawnRate { get; set; } = 100;
    public EnemyCandidate(EnemyCharacter enemy, int minLevel, int maxLevel, int spawnRate)
    {
        Enemy = enemy;
        MinLevel = minLevel;
        MaxLevel = maxLevel;
        SpawnRate = spawnRate;
    }

    public int RamdomLevel()
    {
        return Random.Shared.Next(MinLevel, MaxLevel + 1);
    }
}
public class EnemyCandidateList
{
    private HashSet<EnemyCandidate> _nomalEnemyCandidates = new HashSet<EnemyCandidate>();
    private HashSet<EnemyCandidate> _bossEnemyCandidates = new();

    public List<EnemyCharacter> RamdomSpawnEnemy(int spawnAmount)
    {
        List<EnemyCharacter> enemies = new();
        int totalWeight = _nomalEnemyCandidates.Sum(enemy => enemy.SpawnRate);
        for(int i = 0; i < spawnAmount; i++)
        {
            int total = totalWeight;
            int rdm = Random.Shared.Next(1, totalWeight + 1);
            foreach(EnemyCandidate candidate in _nomalEnemyCandidates)
            {
                if(rdm <= candidate.SpawnRate )
                {
                    EnemyCharacter enemy = (EnemyCharacter)candidate.Enemy.Clone();
                    int level = candidate.RamdomLevel();
                    enemy.Stat.expSet.SetLevel(level);
                    enemy.UpdateStat();
                    enemies.Add(enemy);
                    break;
                }
                else
                {
                    rdm -= candidate.SpawnRate;
                }
            }
        }
        
        return AddAlphabet(enemies);
    }

    public void AddCandidate(GameId<IEnemyId> enemyID, EnemyType enemyType, int minLevel, int maxLevel, int spawnRate)
    {
        EnemyCharacter enemy = EntityCreator.CreateEnemy(enemyID, enemyType);
        EnemyCandidate candidate = new EnemyCandidate(enemy, minLevel, maxLevel, spawnRate);

        if(enemy.EnemyType == EnemyType.Boss)
        {
            _bossEnemyCandidates.Add(candidate);
        }
        else
        {
            _nomalEnemyCandidates.Add(candidate);
        }
    }

    private List<EnemyCharacter> AddAlphabet(List<EnemyCharacter> spawnedList)
    {
        Dictionary<GameId<IBaseStatId>, int> nameCounts = new Dictionary<GameId<IBaseStatId>, int>();
        foreach (var enemy in spawnedList)
        {

            if (nameCounts.TryGetValue(enemy.EntityID, out int value))
            {
                nameCounts[enemy.EntityID] = ++value;
            }
            else
            {
                nameCounts[enemy.EntityID] = 1;
            }
        }

        Dictionary<GameId<IBaseStatId>, int> currentNamingIndices = new Dictionary<GameId<IBaseStatId>, int>();

        foreach (var enemy in spawnedList)
        {
            // 1匹しか選ばれなかったモンスターは「A」をつけずそのまま（スライム 等）
            if (nameCounts[enemy.EntityID] <= 1) continue;

            // 2匹以上いる場合は連番を振る
            if (!currentNamingIndices.ContainsKey(enemy.EntityID))
            {
                currentNamingIndices[enemy.EntityID] = 0; // 0番目＝'A'
            }

            int index = currentNamingIndices[enemy.EntityID];
            char alphabet = (char)('A' + index); // 'A' + 0 = 'A', 'A' + 1 = 'B' ...

            enemy.Rename($"{enemy.Name}{alphabet}");

            currentNamingIndices[enemy.EntityID]++;
        }

        return spawnedList;
    }
}



