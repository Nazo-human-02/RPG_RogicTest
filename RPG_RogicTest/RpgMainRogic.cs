using System;

public static class RpgMainRogic
{
	static void Main()
	{
        InitializeGame();

        bool isPlaying = true;
        int battleCount = 1;

        while (isPlaying)
        {
            Console.WriteLine($"\n====================================");
            Console.WriteLine($"  第 {battleCount} 戦目の準備開始   ");
            Console.WriteLine($"====================================");

            List<EnemyCharacter> enemies = SpawnEnemies("area_001", 3); // エリア1、3体出現

            ShowPartyStatus();

            BattleManager battleManager = new BattleManager(PartyController.PartyMember, enemies);

            BattleResultType result = battleManager.BattleStart();

            if (result == BattleResultType.Victory)
            {
                Console.WriteLine("\n戦闘に勝利した！次のエンカウントへ進みます。");
                battleCount++;

                PrepareForNextBattle();
            }
            else if (result == BattleResultType.Defeat)
            {
                Console.WriteLine("\nパーティが全滅した... GAME OVER");
                isPlaying = false; // ループを抜けて終了
            }
            else
            {
                // 逃走などの場合
                Console.WriteLine("\n戦闘から逃げ出した。");
                battleCount++;
            }
        }

        Console.WriteLine("\nゲームを終了します。プレイありがとうございました！");
	}

	private static void InitializeGame()
	{
        DropRewardMasterData.Load();
        EnemyMasterData.Load();
        EntityBaseStatMasterData.Load();
        NotificationMasterData.Load();
        GameSkillMasterData.Load();

        AreaEntitySetting.AreaEnemySet();

        if (PartyController.PartyMember.Count == 0)
        {
            MainCharacter main = EntityCreator.CreateMainChara("stat_hero_001");
            Notification notify = NotifyCreator.Creator("notify_001", main);
            Notification poison = NotifyCreator.Creator("notify_002", main);
            main.AddNotify(notify);
            main.AddNotify(poison);
            main.SetSkill("skill_001");
            main.SetSkill("skill_002");
            main.SetSkill("skill_003");
            PartyController.AddMember(main);
        }
		LogWrite.Log("ゲームの初期化完了");
    }

	private static List<EnemyCharacter> SpawnEnemies(GameId<IAreaId> areaID, int enemyAmount)
	{
        Console.WriteLine($"エリア{areaID}:{AreaEntitySetting.AreaEnemyCandidates[areaID]}");
        List<EnemyCharacter> enemies = AreaEntitySetting.RandomSpawnEnemy(areaID, enemyAmount);
        foreach (var enemy in enemies)
        {
            Notification notify = NotifyCreator.Creator("notify_003", enemy);
            enemy.AddNotify(notify);
            Console.WriteLine($"Lv{enemy.Stat.expSet.CurrentLevel}:{enemy.Name}," +
                $"[ステータス]最大HP:{enemy.Stat.MaxHp},最大MP:{enemy.Stat.MaxMp}," +
                $"攻撃力:{enemy.Stat.baseStat.Atk},防御力:{enemy.Stat.baseStat.Def},敏捷:{enemy.Stat.baseStat.Agi},状態個数:{enemy.Notifications.Notifications.Count}");
        }
		return enemies;
    }

	private static void ShowPartyStatus()
	{
        Console.WriteLine("\n---現在のパーティー状況---");
        foreach (var party in PartyController.PartyMember)
        {
            Console.WriteLine($"Lv{party.Stat.expSet.CurrentLevel}:{party.Name}, 最大HP:{party.Stat.MaxHp}");
            Console.WriteLine($"状態個数:{party.Notifications.Notifications.Count}");
            foreach (var not in party.Notifications.Notifications)
            {
                if (not.Owner == null)
                {
                    continue;
                }
                Console.WriteLine($"[{not.Owner.Name}]:残り{not.RemainTime}ターン");
            }
        }
        Console.WriteLine("-----------------------\n");
    }

    private static void PrepareForNextBattle()
    {
        ActionExecutor.ClearLogCache();

        foreach (var member in PartyController.PartyMember)
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
