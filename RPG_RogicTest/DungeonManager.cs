using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

public class DungeonManager(ILogProvider logProvider, IScreenProvider screenProvider,
    IRandomProvider randomProvider, IInputProvider inputProvider)
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly IScreenProvider _screenProvider = screenProvider; //試し、出来そうならlogからscreenに移行
    private readonly IRandomProvider _randomProvider = randomProvider;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly RouteGenerator _routeGenerator = new RouteGenerator(randomProvider);
    private readonly RouteSelector _routeSelector = new RouteSelector(logProvider, inputProvider, screenProvider);
    private readonly EnemySpawnSelector _enemySpawnSelector = new(randomProvider);
    private readonly EnemyGenerator _enemyGenerator = new(randomProvider);
    private readonly BattleManagerGenerator _battleManagerGenerator = new();
    public bool IsEntering { get; private set; }

    private Action<ConditionContext>? _requestOpenMenu;
    public void Initialize(Action<ConditionContext> requestOpenMenu)
    {
        _requestOpenMenu = requestOpenMenu;
    }
    public void EnterDungeon(PartyController enterdParty, int floorNum = 1)
    {
        IsEntering = true;
        int currentFloor = floorNum;
        _screenProvider.Set(ScreenLayer.Content, $"{enterdParty.PartyMember.First().Name}はダンジョンに侵入した");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        _screenProvider.Clear(ScreenLayer.Content);

        _screenProvider.Set(ScreenLayer.Header, TextMasterData.GetDungeonHeaderText(currentFloor));
        _screenProvider.Set(ScreenLayer.MainView, TextMasterData.GetPartyText(enterdParty));
        _screenProvider.RefreshUntil();

        while (IsEntering)
        {
            var dungeonFloor = new DungeonFloor(currentFloor);
            bool isContinue = ProceedFloor(dungeonFloor, enterdParty, currentFloor);
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
        _screenProvider.Set(ScreenLayer.Content, "ダンジョンから脱出した");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        _screenProvider.ClearAll();
        _screenProvider.ClearLog();
    }

    public bool ProceedFloor(DungeonFloor dungeonFloor, PartyController party, int floorNum) //false=脱出、true=進行
    {
        while(!dungeonFloor.IsBossReached)
        {
            _screenProvider.Clear(ScreenLayer.Content);
            var routes = _routeGenerator.CreateRoutes(dungeonFloor.FloorData);
            RouteData selectedRoute;
            while(true)
            {
                var selectedResult = _routeSelector.SelectingRoute(routes);
                if (selectedResult is SelectionSuccess<RouteData> success)
                {
                    selectedRoute = success.Value;
                    break;
                }
                else if (selectedResult is SelectionOpenMenu<RouteData> requestOpenMenu)
                {
                    if (_requestOpenMenu == null)
                        continue;
                    ConditionContext conditionContext = new(IsBattle:false, CurrentTurn:0, User:null, Target:null, 
                        PartyController:party, BattleSession:null,
                        FieldContext: new FieldContext(FieldType.Dungeon, floorNum), RandomProvider:_randomProvider);
                    _requestOpenMenu.Invoke(conditionContext);
                }
            }
            switch (selectedRoute.EventType)
            {
                case DungeonEventType.Battle:
                    if(selectedRoute.RouteContentData is BattleEventContent battle)
                    {
                        var battleResult = BattleStart(party, battle.EnemyParty, floorNum);
                        if (battleResult.BattleResultType == BattleResultType.Defeat)
                        {
                            //_screenProvider.Set(ScreenLayer.Content, "全滅してしまった...");
                            _screenProvider.RefreshUntil(ScreenLayer.Content);
                            _screenProvider.WaitForEnter();
                            return false;
                        }
                        else if (battleResult.BattleResultType == BattleResultType.Victory)
                        {
                            //_screenProvider.Set(ScreenLayer.Content, "\n戦闘に勝利した！");
                            _screenProvider.RefreshUntil(ScreenLayer.Content);
                            _screenProvider.WaitForEnter();
                        }
                        else if (battleResult.BattleResultType == BattleResultType.Escape)
                        {
                            _screenProvider.Append(ScreenLayer.Content, $"{party.PartyMember.First().Name}は逃走に成功した");
                            _screenProvider.RefreshUntil(ScreenLayer.Content);
                            _screenProvider.WaitForEnter();
                        }
                    }
                    break;
                case DungeonEventType.Treasure:
                    _screenProvider.Set(ScreenLayer.Content, "宝箱を見つけた(仮)");
                    _screenProvider.RefreshUntil(ScreenLayer.Content);
                    _screenProvider.WaitForEnter();
                    break;
                case DungeonEventType.None:
                    _screenProvider.Set(ScreenLayer.Content, "何もなかった(仮)");
                    _screenProvider.RefreshUntil(ScreenLayer.Content);
                    _screenProvider.WaitForEnter();
                    break;
                default:
                    throw new InvalidOperationException("想定外のイベント");
            }
            dungeonFloor.Advance(selectedRoute.Progress);
            _screenProvider.Set(ScreenLayer.Content, $"進行度合い;{dungeonFloor.CurrentProgress}" +
                $"(ボスまで{dungeonFloor.FloorData.BossDistance})");
            _screenProvider.RefreshUntil(ScreenLayer.Content);
            _screenProvider.WaitForEnter();
        }
        _screenProvider.Append(ScreenLayer.Content, "ボスの気配がする(仮)");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        var bosses = GetEncounterBoss(dungeonFloor);
        var bossBattleResult = BattleStart(party, bosses, floorNum);
        if(bossBattleResult.BattleResultType == BattleResultType.Defeat)
            return false;
        else if(bossBattleResult.BattleResultType == BattleResultType.Victory)
            _screenProvider.Append(ScreenLayer.Content, "\nフロアボスを撃破した！");
            _screenProvider.RefreshUntil(ScreenLayer.Content);
            _screenProvider.WaitForEnter();

        return true;
    }

    private BattleResult BattleStart(PartyController party, IReadOnlyList<EnemyCharacter> enemyParty, int floorNum)
    {
        _screenProvider.Set(ScreenLayer.SubView, TextMasterData.GetEncounterEnemyText(enemyParty));
        _screenProvider.RefreshUntil(ScreenLayer.SubView);

        BattleManager battleManager = 
            _battleManagerGenerator.Create(_logProvider, _randomProvider, _inputProvider, _screenProvider,
            enemyParty, party, FieldType.Dungeon, floorNum);
        return battleManager.BattleStart();
    }
    private IReadOnlyList<EnemyCharacter> GetEncounterBoss(DungeonFloor dungeonFloor)
    {
        SpawnEnemyTable spawnTable = dungeonFloor.GetSpawnTable();
        BossConfig bossConfig = _enemySpawnSelector.GetRandomBossParty(spawnTable.BossPartyConfigs);
        BossParty bossParty = BossPartyMasterData.GetBossParty(bossConfig.BossPartyID);
        IReadOnlyList<EnemyCharacter> bosses = _enemyGenerator.CreateBossEnemies(bossParty.BossMembers);
        return bosses;
    }
}