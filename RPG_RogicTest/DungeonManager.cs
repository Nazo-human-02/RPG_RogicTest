using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    public bool IsEntering { get; private set; }

    private DungeonState _currentState = DungeonState.Exit;
    private Action<ConditionContext>? _requestOpenMenu;
    private Func<List<EnemyCharacter>, FieldType, int, BattleResult>? _requestBattle;
    private Action<ISelectorRequest>? _requestOpenSelector;

    //状態
    private PartyController? _partyController;
    private DungeonFloor? _currentFloor;
    private RouteData? _currentRoute;
    private int _currentFloorNumber;
    //
    public void Initialize(Action<ConditionContext> requestOpenMenu, 
        Func<List<EnemyCharacter>, FieldType, int, BattleResult> requestBattle, 
        Action<ISelectorRequest> requestOpenSelector)
    {
        _requestOpenMenu = requestOpenMenu;
        _requestBattle = requestBattle;
        _requestOpenSelector = requestOpenSelector;
        ClearState();
    }
    private void ClearState()
    {
        _currentState = DungeonState.Exit;
        _partyController = null;
        _currentFloor = null;
        _currentRoute = null;
        _currentFloorNumber = 0;
    }
    public void NextState()
    {
        CheckPartyState();
        if (!IsEntering)
        {
            ExitDungeon();
            return;
        }
        switch (_currentState)
        {
            case DungeonState.Enter:
                RouteSelect();
                break;
            case DungeonState.RouteSelect:
                ProceedFloor();
                break;

            case DungeonState.ProceedFloor:
                CheckFloorState();
                if (_currentFloor.IsBossReached)
                    EncounterBossEvent();
                else
                    ExecuteDungeonEvent();
                break;
            case DungeonState.Event:
                RouteSelect();
                break;

            case DungeonState.Boss:
                ProceedToNextFloor();
                break;

            case DungeonState.NextFloor:
                RouteSelect();
                break;
        };
    }
    public void EnterDungeon(PartyController enterdParty, int floorNum = 1)
    {
        IsEntering = true;

        _currentState = DungeonState.Enter;
        _currentFloorNumber = floorNum;
        _currentFloor = new DungeonFloor(_currentFloorNumber);
        _partyController = enterdParty;

        _screenProvider.RefreshContent($"{enterdParty.PartyMember.First().Name}はダンジョンに侵入した");
        _screenProvider.WaitForEnter();

        _screenProvider.Set(ScreenLayer.Header, TextMasterData.GetDungeonHeaderText(_currentFloorNumber));
        _screenProvider.Set(ScreenLayer.MainView, TextMasterData.GetPartyText(_partyController));
        _screenProvider.RefreshUntil();

        NextState();
    }
    private void ProceedToNextFloor()
    {
        CheckPartyState();

        _currentFloorNumber++;
        _currentFloor = new DungeonFloor(_currentFloorNumber);
        _currentState = DungeonState.NextFloor;

        _screenProvider.Set(ScreenLayer.Content, $"フロア{_currentFloorNumber}に進む");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();

        NextState();
    }
    private void RouteSelect()
    {
        CheckPartyState();
        CheckFloorState();

        _currentState = DungeonState.RouteSelect;

        var routes = _routeGenerator.CreateRoutes(_currentFloor.FloorData);
        ConditionContext condition =
            new(
                IsBattle: false, CurrentTurn: 0, User: null, Target: null,
            PartyController: _partyController, BattleSession: null,
            FieldContext: new FieldContext(FieldType.Dungeon, _currentFloorNumber), RandomProvider: _randomProvider);
        RequestOpenSelector<RouteData> request = new
            (
                Selector: _routeSelector,
                SelectorOpen: () => _routeSelector.Open(routes),
                OnSuccess: (success) => OnSuccess(success.Value),
                OnCanceled: (canceled) => OnCanceld(),
                OnOpenMenu: (openMenu) => _requestOpenMenu?.Invoke(condition)
            );
        _requestOpenSelector?.Invoke(request);
    }
    private void OnSuccess(RouteData routeData)
    {
        _currentRoute = routeData;
        NextState();
    }
    private void OnCanceld()
    {
        _currentState = DungeonState.Enter;
        NextState();
    }
    public void ProceedFloor()
    {
        CheckFloorState();
        CheckRouteState();

        _currentState = DungeonState.ProceedFloor;

        _currentFloor.Advance(_currentRoute.Progress);

        _screenProvider.Set(ScreenLayer.Content, $"進行度合い;{_currentFloor.CurrentProgress}" +
            $"(ボスまで{_currentFloor.FloorData.BossDistance})");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        NextState();
    }
    private void ExitDungeon()
    {
        _currentState = DungeonState.Exit;
        IsEntering = false;
        _screenProvider.Set(ScreenLayer.Content, "ダンジョンから脱出した");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        _screenProvider.ClearAll();
        _screenProvider.ClearLog();
    }
    private BattleResult BattleStart(IReadOnlyList<EnemyCharacter> enemyParty)
    {
        _screenProvider.Set(ScreenLayer.SubView, TextMasterData.GetEncounterEnemyText(enemyParty));
        _screenProvider.RefreshUntil(ScreenLayer.SubView);
        if(_requestBattle is null)
            throw new InvalidOperationException("戦闘開始のリクエストが設定されていません");
        return _requestBattle!.Invoke(enemyParty.ToList(), FieldType.Dungeon, _currentFloorNumber);
    }
    private void ExecuteDungeonEvent()
    {
        CheckRouteState();
        CheckFloorState();
        _currentState = DungeonState.Event;
        IsEntering = _currentRoute.EventType switch
        {
            DungeonEventType.Battle => BattleEvent(),
            DungeonEventType.Treasure => TreasureEvent(),
            DungeonEventType.None => NoneEvent(),
            _ => throw new InvalidOperationException("想定外のイベント"),
        };
    }
    private IReadOnlyList<EnemyCharacter> GetEncounterBoss()
    {
        CheckFloorState();
        SpawnEnemyTable spawnTable = _currentFloor.GetSpawnTable();
        BossConfig bossConfig = _enemySpawnSelector.GetRandomBossParty(spawnTable.BossPartyConfigs);
        BossParty bossParty = BossPartyMasterData.GetBossParty(bossConfig.BossPartyID);
        IReadOnlyList<EnemyCharacter> bosses = _enemyGenerator.CreateBossEnemies(bossParty.BossMembers);
        return bosses;
    }
    private bool BattleEvent()
    {
        CheckRouteState();
        CheckPartyState();
        if (_currentRoute.RouteContentData is BattleEventContent battle)
        {
            var battleResult = BattleStart(battle.EnemyParty);
            if (battleResult.BattleResultType == BattleResultType.Defeat)
            {
                _screenProvider.RefreshUntil(ScreenLayer.Content);
                _screenProvider.WaitForEnter();
                return false;
            }
            else if (battleResult.BattleResultType == BattleResultType.Victory)
            {
            }
            else if (battleResult.BattleResultType == BattleResultType.Escape)
            {
                _screenProvider.Append(ScreenLayer.Content, $"{_partyController.PartyMember.First().Name}は逃走に成功した");
            }
            _screenProvider.RefreshUntil(ScreenLayer.Content);
            _screenProvider.WaitForEnter();
        }
        return true;
    }
    private bool TreasureEvent()
    {
        CheckRouteState();
        if (_currentRoute.RouteContentData is TreasureEventContent treasure)
        {
            _screenProvider.Set(ScreenLayer.Content, "宝箱を見つけた(仮)");
            _screenProvider.RefreshUntil(ScreenLayer.Content);
            _screenProvider.WaitForEnter();
        }
        return true;
    }
    private bool NoneEvent()
    {
        _screenProvider.Set(ScreenLayer.Content, "何もなかった(仮)");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        return true;
    }
    private void EncounterBossEvent()
    {
        _currentState = DungeonState.Boss;
        _screenProvider.Append(ScreenLayer.Content, "ボスの気配がする(仮)");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();

        var bosses = GetEncounterBoss();
        var bossBattleResult = BattleStart(bosses);
        if (bossBattleResult.BattleResultType == BattleResultType.Defeat)
            IsEntering = false;
        else if (bossBattleResult.BattleResultType == BattleResultType.Victory)
        {
            _screenProvider.Append(ScreenLayer.Content, "\nフロアボスを撃破した！");
            _screenProvider.RefreshUntil(ScreenLayer.Content);
        }

        NextState();
    }
    [MemberNotNull(nameof(_partyController))]
    private void CheckPartyState()
    {
        if (_partyController is null)
            throw new InvalidOperationException("パーティーが設定されていません");
    }
    [MemberNotNull(nameof(_currentFloor))]
    private void CheckFloorState()
    {
        if (_currentFloor is null)
            throw new InvalidOperationException("フロアが設定されていません");
    }
    [MemberNotNull(nameof(_currentRoute))]
    private void CheckRouteState()
    {
        if (_currentRoute is null)
            throw new InvalidOperationException("ルートが設定されていません");
    }

}