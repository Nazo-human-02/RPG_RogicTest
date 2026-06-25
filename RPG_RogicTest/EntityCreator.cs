public static class EntityCreator
{
    public static EnemyCharacter CreateEnemy(GameId<IEnemyId> enemyID, EnemyType enemyType)
    {
        EnemyData enemyData = EnemyMasterData.GetEnemyData(enemyID);
        EntityBaseStatData baseStat = EntityBaseStatMasterData.GetEntityBaseStat(enemyData.StatId);
        BattleStat stat = GetBattleStat(baseStat);
        DropRewardData rewardData = DropRewardMasterData.GetDropData(enemyData.DropId);

        RewardConfig rewardConfig = new RewardConfig(rewardData.Gold, rewardData.Exp, enemyData.DropTableId);
        return new EnemyCharacter(baseStat.Name, enemyType, stat, enemyData.StatId, rewardConfig);
    }

    public static MainCharacter CreateMainChara(GameId<IBaseStatId> mainID) //将来的にはキャラクターIDから生成
    {
        EntityBaseStatData mainBaseStatData = EntityBaseStatMasterData.GetEntityBaseStat(mainID);
        BattleStat stat = GetBattleStat(mainBaseStatData);

        return new MainCharacter(mainBaseStatData.Name, stat, mainID);

    }
    public static NonPlayerCharacter CreateNpc(GameId<INpcId> NpcID)
    {
        NpcBaseData npcData = NpcMasterData.GetNpcData(NpcID);
        EntityBaseStatData baseStatData = EntityBaseStatMasterData.GetEntityBaseStat(npcData.BaseStatID);
        BattleStat stat = GetBattleStat(baseStatData);

        return new NonPlayerCharacter(npcData.properName, stat, npcData.BaseStatID, npcData.content, npcData.IsShop);
    }

    private static BattleStat GetBattleStat(EntityBaseStatData masterData)
    {
        BattleStat stat = new BattleStat()
        {
            MaxHp = masterData.BaseHP,
            MaxMp = masterData.BaseMP,
            CurrentHp = masterData.BaseHP,
            CurrentMp = masterData.BaseMP,
            baseStat = new BaseStat()
            {
                Atk = masterData.BaseAtk,
                Def = masterData.BaseDef,
                Agi = masterData.BaseAgi,
                Cri = masterData.BaseCri,
                CriPer = masterData.BaseCriPer
            }
        };

        return stat;
    }
}