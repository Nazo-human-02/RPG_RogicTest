using System;

//パーティー管理,操作
public class PartyController(ILogProvider logProvider)
{
    public IReadOnlySet<CharacterBase> PartyMember => _partyMember;
    private readonly HashSet<CharacterBase> _partyMember = new();
    private readonly ILogProvider log = logProvider;
    const int MaxPartyMember = 4; //パーティーメンバー,増えるかも
    public int OwnedGold { get; private set; } = 0;
    

    public void AddMember(CharacterBase chara)
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

    public void RemoveMember(CharacterBase chara)
    {
        if(_partyMember.Contains(chara))
        {
            _partyMember.Remove(chara);
            chara.RemoveParty();
        }
    }

    void ChangeMember()
    {
        //選択してキャラをRemoveする処理
    }

    public void GetReward(DropRewardData dropRewardData)
    {
        AddGold(dropRewardData.Gold);
        log.Log($"_____戦利品を獲得した_____");
        log.Log($"～{dropRewardData.Gold}G手に入れた！(所持金{OwnedGold})～");
        foreach(Entity entity in PartyMember)
        {
            if(entity.Stat.IsDead)
            {
                continue;
            }
            ExpResult expResult = entity.Stat.expSet.GetExp(entity, dropRewardData.Exp);
            string text = $"{entity.Name}:{expResult.GetExp}exp獲得!";
            if(expResult.IsLevelUp)
            {
                text += $"レベルアップ！{expResult.BeforeLevel}→{expResult.AfterLevel}";
            }
            log.Log(text);
        }
    }

    public void AddGold(int gold)
    {
        OwnedGold += gold;
    }
    public bool SpendGold(int gold)
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

    public int RandomLevel(IRandomProvider random)
    {
        return random.GetRandomInt(MinLevel, MaxLevel + 1);
    }
}
public class EnemyCandidateList(IRandomProvider random)
{
    private readonly IRandomProvider randomProvider = random;
    private List<EnemyCandidate> _nomalEnemyCandidates = new List<EnemyCandidate>();
    private List<EnemyCandidate> _bossEnemyCandidates = new List<EnemyCandidate>();

    public List<EnemyCharacter> RandomSpawnEnemy(int spawnAmount)
    {
        List<EnemyCharacter> enemies = new();
        int totalWeight = _nomalEnemyCandidates.Sum(enemy => enemy.SpawnRate);
        for(int i = 0; i < spawnAmount; i++)
        {
            int total = totalWeight;
            int rdm = randomProvider.GetRandomInt(1, totalWeight + 1);
            foreach(EnemyCandidate candidate in _nomalEnemyCandidates)
            {
                if(rdm <= candidate.SpawnRate )
                {
                    EnemyCharacter enemy = (EnemyCharacter)candidate.Enemy.Clone();
                    int level = candidate.RandomLevel(randomProvider);
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
        
        return RenameDuplicateenemies(enemies);
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

    private List<EnemyCharacter> RenameDuplicateenemies(List<EnemyCharacter> spawnedList)
    {
        var sortedList = spawnedList.GroupBy(enemy => enemy.EntityID).ToList();

        foreach(var group in sortedList)
        {
            var enemies = group.ToList();
            if (enemies.Count <= 1)
            {
                continue;
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Rename($"{enemies[i].Name}{(char)('A' + i)}");// 'A' + 0 = 'A', 'A' + 1 = 'B' ...
            }
        }
        return spawnedList;
    }
}



