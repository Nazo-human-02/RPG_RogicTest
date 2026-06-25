using System;

public static class RpgMainRogic
{
	static void Main()
	{
        IRandomProvider random = new RandomProvider();
        ILogProvider log = new ConsoleLogProvider();
        IInputProvider input = new ConsoleInputProvider();

        PartyController partyController = new PartyController(log);

        InitializeGame(log, partyController);

        DungeonManager dungeonManager = new DungeonManager(log, random, input);
        while(true)
        {
            dungeonManager.EnterDungeon(partyController);
            bool isContinue = ContinueGame(input, log);
            if (!isContinue)
                break;
            PrepareForNextBattle(partyController);
        }


        log.Log("\nゲームを終了します。プレイありがとうございました！");
	}

    private static void LoadMasterDatas()
    {
        AreaMasterData.Load();
        BossPartyMasterData.Load();
        CostMasterData.Load();
        DropRewardMasterData.Load();
        DropItemTableMasterData.Load();
        DungeonFloorMasterData.Load();
        EnemyMasterData.Load();
        EnemyTableMasterData.Load();
        EntityBaseStatMasterData.Load();
        ItemMasterData.Load();
        NotificationMasterData.Load();
        NpcMasterData.Load();
        GameSkillMasterData.Load();
    }
	private static void InitializeGame(ILogProvider log, PartyController partyController)
	{
        LoadMasterDatas();

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

    private static bool ContinueGame(IInputProvider inputProvider, ILogProvider logProvider)
    {
        while (true)
        {
            logProvider.Log("0:戦闘継続,1:ゲーム終了");
            string? input = inputProvider.Input();
            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int num) || (num != 0 && num != 1))
            {
                logProvider.Log("正しく入力してください");
            }
            else if (num == 0)
                return true;
            else
                return false;
        }
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

    private static void PrepareForNextBattle(PartyController partyController)
    {
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
