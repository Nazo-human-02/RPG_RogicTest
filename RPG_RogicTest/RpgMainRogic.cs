using System;

public static class RpgMainRogic
{
	static void Main()
	{

        bool isPlaying = true;
        int battleCount = 1;

        IRandomProvider random = new RandomProvider();
        ILogProvider log = new ConsoleLogProvider();
        IInputProvider input = new ConsoleInputProvider();
        BattleCalculator battleCalculator = new BattleCalculator(random);
        ActionExecutor actionExecutor = new ActionExecutor(battleCalculator, log);
        TurnScheduler turnScheduler = new TurnScheduler(random);

        PartyController partyController = new PartyController(log);

        InitializeGame(log, partyController);

        while (isPlaying)
        {
            log.Log($"\n====================================");
            log.Log($"  第 {battleCount} 戦目の準備開始   ");
            log.Log($"====================================");

            List<EnemyCharacter> enemies = SpawnEnemies("area_001", 3, log); // エリア1、3体出現

            ShowPartyStatus(log, partyController);

            BattleManager battleManager = new BattleManager(partyController.PartyMember, enemies, log, input, actionExecutor, turnScheduler, partyController);

            BattleResultType result = battleManager.BattleStart();

            if (result == BattleResultType.Victory)
            {
                log.Log("\n戦闘に勝利した！次のエンカウントへ進みます。");
                battleCount++;

                PrepareForNextBattle(actionExecutor, partyController);
            }
            else if (result == BattleResultType.Defeat)
            {
                log.Log("\nパーティが全滅した... GAME OVER");
                isPlaying = false; // ループを抜けて終了
            }
            else
            {
                // 逃走などの場合
                log.Log("\n戦闘から逃げ出した。");
                battleCount++;
            }
        }

        log.Log("\nゲームを終了します。プレイありがとうございました！");
	}

    private static void LoadMasterDatas()
    {
        DropRewardMasterData.Load();
        EnemyMasterData.Load();
        EntityBaseStatMasterData.Load();
        NotificationMasterData.Load();
        CostMasterData.Load();
        GameSkillMasterData.Load();
    }
	private static void InitializeGame(ILogProvider log, PartyController partyController)
	{
        LoadMasterDatas();

        AreaEntitySetting.AreaEnemySet();

        if (partyController.PartyMember.Count == 0)
        {
            MainCharacter main = EntityCreator.CreateMainChara("stat_hero_001");
            Notification notify = NotifyCreator.Creator("notify_001", main);
            Notification poison = NotifyCreator.Creator("notify_002", main);
            main.AddNotify(notify);
            main.AddNotify(poison);
            main.SetSkill("skill_001");
            main.SetSkill("skill_002");
            main.SetSkill("skill_003");
            partyController.AddMember(main);
        }
		log.Log("ゲームの初期化完了");
    }

	private static List<EnemyCharacter> SpawnEnemies(GameId<IAreaId> areaID, int enemyAmount, ILogProvider log)
	{
        log.Log($"エリア{areaID}:{AreaEntitySetting.AreaEnemyCandidates[areaID]}");
        List<EnemyCharacter> enemies = AreaEntitySetting.RandomSpawnEnemy(areaID, enemyAmount);
        foreach (var enemy in enemies)
        {
            Notification notify = NotifyCreator.Creator("notify_003", enemy);
            enemy.AddNotify(notify);
            log.Log($"Lv{enemy.Stat.expSet.CurrentLevel}:{enemy.Name}," +
                $"[ステータス]最大HP:{enemy.Stat.MaxHp},最大MP:{enemy.Stat.MaxMp}," +
                $"攻撃力:{enemy.Stat.baseStat.Atk},防御力:{enemy.Stat.baseStat.Def},敏捷:{enemy.Stat.baseStat.Agi},状態個数:{enemy.Notifications.Notifications.Count}");
        }
		return enemies;
    }

	private static void ShowPartyStatus(ILogProvider log, PartyController partyController)
	{
        log.Log("\n---現在のパーティー状況---");
        foreach (var party in partyController.PartyMember)
        {
            log.Log($"Lv{party.Stat.expSet.CurrentLevel}:{party.Name}, 最大HP:{party.Stat.MaxHp}");
            log.Log($"状態個数:{party.Notifications.Notifications.Count}");
            foreach (var not in party.Notifications.Notifications)
            {
                if (not.Owner == null)
                {
                    continue;
                }
                log.Log($"[{not.Owner.Name}]:残り{not.RemainTime}ターン");
            }
        }
        log.Log("-----------------------\n");
    }

    private static void PrepareForNextBattle(ActionExecutor actionExecutor, PartyController partyController)
    {
        actionExecutor.ClearLogCache();

        foreach (var member in partyController.PartyMember)
        {
            foreach (var skill in member.ValidSkills)
            {
                if (skill != null)
                {
                    skill.SetCoolTime(0);
                }
            }
            member.Stat.CurrentHp = member.Stat.MaxHp;
        }
    }
}
