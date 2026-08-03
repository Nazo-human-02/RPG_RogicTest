using System;

public static class RpgMainRogic
{
	static void Main()
	{
        IRandomProvider random = new RandomProvider();
        IInputProvider input = new ConsoleInputProvider();
        ConsoleLogProvider logProvider = new ConsoleLogProvider(input); //log兼screen
        //ILogProvider log = new LogProvider();

        PartyController partyController = new PartyController(logProvider, logProvider);
        ProvidorContext providorContext = new(logProvider, random, input, logProvider);
        InitializeGame(logProvider, partyController);

        DungeonManager dungeonManager = new DungeonManager(logProvider, logProvider, random, input);


        //仮置き
        FieldContext fieldContext = new(FieldType.OutSide, 0);
        ConditionContext conditionContext = new(false, 0, null, null, partyController, null, fieldContext, random);
        TargetSelector targetSelect = new(logProvider, input, logProvider);
        BattleCalculator battleCalculator = new(random);
        BattleManagerGenerator battleManagerGenerator = new();
        MenuSelector menuSelector = new(input, logProvider);
        InventoryMenu inventoryMenu = new(targetSelect, battleCalculator, conditionContext, logProvider, input);
        StatusMenu statusMenu = new(input, logProvider);
        SkillMenu skillMenu = new(targetSelect, battleCalculator, input, logProvider, conditionContext);
        EquipmentSelector equipmentSelector = new();
        EquipmentMenu equipmentMenu = new(equipmentSelector, input, logProvider);
        MenuManager menuManager = new(menuSelector, providorContext, statusMenu, inventoryMenu, skillMenu, equipmentMenu);

        GameManager gameManager = new(providorContext, battleManagerGenerator, menuManager, dungeonManager, partyController);
        gameManager.InitializeDungeonManager();
        //
        
        while(true)
        {
            gameManager.EnterToDungeon();
            //dungeonManager.EnterDungeon(partyController);
            bool isContinue = ContinueGame(input, logProvider);
            if (!isContinue)
                break;
            PrepareForNextBattle(partyController);
        }


        logProvider.WriteLog("\nゲームを終了します。プレイありがとうございました！");
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
        EquipmentMasterData.Load();
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
            main.SetSkill("skill_004");
            partyController.Inventory.AddItem("item_test_000", 100);
            var equipment_1 = EquipmentCreator.Create("equip_head_001");
            partyController.Inventory.AddEquipment(equipment_1);
            partyController.AddMember(main);
        }
		log.WriteLog("ゲームの初期化完了");
    }

    private static bool ContinueGame(IInputProvider inputProvider, ILogProvider logProvider)
    {
        while (true)
        {
            logProvider.WriteLog("0:戦闘継続,1:ゲーム終了");
            string? input = inputProvider.Input();
            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int num) || (num != 0 && num != 1))
            {
                logProvider.WriteLog("正しく入力してください");
            }
            else if (num == 0)
                return true;
            else
                return false;
        }
    }
	private static void ShowPartyStatus(ILogProvider log, PartyController partyController)
	{
        log.WriteLog("\n---現在のパーティー状況---");
        foreach (var party in partyController.PartyMember)
        {
            log.WriteLog($"Lv{party.Stat.expSet.CurrentLevel}:{party.Name}, 最大HP:{party.Stat.MaxHp}");
            log.WriteLog($"状態個数:{party.Notifications.Notifications.Count}");
            foreach (var not in party.Notifications.Notifications)
            {
                if (not.Owner == null)
                {
                    continue;
                }
                log.WriteLog($"[{not.Owner.Name}]:残り{not.RemainTime}ターン");
            }
        }
        log.WriteLog("-----------------------\n");
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
