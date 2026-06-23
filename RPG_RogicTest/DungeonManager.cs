using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

public class DungeonManager(ILogProvider logProvider, IRandomProvider randomProvider, IInputProvider inputProvider)
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly IRandomProvider _randomProvider = randomProvider;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly RouteGenerator _routeGenerator = new RouteGenerator(randomProvider);
    private readonly RouteSelector _routeSelector = new RouteSelector(logProvider, inputProvider);
    private readonly EnemySpawnSelector _enemySpawnSelector = new(randomProvider);
    private readonly EnemyGenerator _enemyGenerator = new(randomProvider);
    public bool IsEntering { get; private set; }

    public void EnterDungeon(PartyController enterdParty, int floorNum = 1)
    {
        IsEntering = true;
        int currentFloor = floorNum;
        _logProvider.Log($"{enterdParty.PartyMember.First().Name}はダンジョンに侵入した");
        while (IsEntering)
        {
            var dungeonFloor = new DungeonFloor(currentFloor);
            bool isContinue = ProceedFloor(dungeonFloor, enterdParty);
            if(isContinue)
            {
                currentFloor++;
            }
            else
            {
                IsEntering = false;
                break;
            }

        }
        _logProvider.Log("ダンジョンから脱出した");
    }

    public bool ProceedFloor(DungeonFloor dungeonFloor, PartyController party) //false=脱出、true=進行
    {
        while(!dungeonFloor.IsBossReached)
        {
            var routes = _routeGenerator.CreateRoutes(dungeonFloor.FloorData);
            var selectedRoute = _routeSelector.SelectingRoute(routes);
            switch (selectedRoute.EventType)
            {
                case DungeonEventType.Battle:
                    if(selectedRoute.RouteContentData is BattleEventContent battle)
                    {
                        BattleResultType battleResult = BattleStart(party, battle.EnemyParty);
                        if (battleResult == BattleResultType.Defeat)
                        {
                            _logProvider.Log("全滅してしまった...");
                            return false;
                        }
                        else if (battleResult == BattleResultType.Victory)
                            _logProvider.Log("\n戦闘に勝利した！");
                        else if (battleResult == BattleResultType.Escape)
                            _logProvider.Log($"{party.PartyMember.First().Name}は逃走に成功した");
                    }
                    break;
                case DungeonEventType.Treasure:
                    _logProvider.Log("宝箱を見つけた(仮)");
                    break;
                case DungeonEventType.None:
                    _logProvider.Log("何もなかった(仮)");
                    break;
                default:
                    throw new InvalidOperationException("想定外のイベント");
            }
            dungeonFloor.Advance(selectedRoute.Progress);
            _logProvider.Log($"進行度合い;{dungeonFloor.CurrentProgress}(ボスまで{dungeonFloor.FloorData.BossDistance})");
        }
        _logProvider.Log("ボスの気配がする(仮)");
        MoveToBoss();
        BattleResultType bossBattleResult = EncounterBoss(party, dungeonFloor);
        if(bossBattleResult == BattleResultType.Defeat)
            return false;
        else if(bossBattleResult == BattleResultType.Victory)
            _logProvider.Log("\nフロアボスを撃破した！");

        return true;
    }

    private BattleResultType BattleStart(PartyController party, IReadOnlyList<EnemyCharacter> enemyParty)
    {
        StringBuilder text = new StringBuilder();
        foreach (var enemy in enemyParty) text.Append($"[Lv{enemy.Stat.expSet.CurrentLevel}:{enemy.Name}]");
        _logProvider.Log(text.ToString());
        BattleManager battleManager = 
            new BattleManager(party.PartyMember, enemyParty, _logProvider, _inputProvider, _randomProvider, party);
        return battleManager.BattleStart();
    }
    private BattleResultType EncounterBoss(PartyController party, DungeonFloor dungeonFloor)
    {
        SpawnEnemyTable spawnTable = dungeonFloor.GetSpawnTable();
        BossConfig bossConfig = _enemySpawnSelector.GetRandomBossParty(spawnTable.BossPartyConfigs);
        BossParty bossParty = BossPartyMasterData.GetBossParty(bossConfig.BossPartyID);
        IReadOnlyList<EnemyCharacter> enemies = _enemyGenerator.CreateBossEnemies(bossParty.BossMembers);
        return BattleStart(party, enemies);
    }
    private void MoveToBoss() //仮置き
    {
        while (true)
        {
            _logProvider.Log("Enterを押してボス戦を開始");
            string? input = _inputProvider.Input();
            if (string.IsNullOrEmpty(input))
                return;
            else
                _logProvider.Log("入力せずEnterを押してください");
        }

    }
}