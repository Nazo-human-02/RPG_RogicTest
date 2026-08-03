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
    private Action<BattleRequest>? _requestBattle;
    private Action<ISelectorRequest>? _requestOpenSelector;

    //状態
    private PartyController? _partyController;
    private DungeonFloor? _currentFloor;
    private RouteData? _currentRoute;
    private int _currentFloorNumber;
    private BattleRequest? _battleRequest;
    private BattleResult? _battleResult;
    private bool _waitForInput = false;
    //
    public void Initialize(Action<ConditionContext> requestOpenMenu, 
        Action<BattleRequest> requestBattle, 
        Action<ISelectorRequest> requestOpenSelector)
    {
        _requestOpenMenu = requestOpenMenu;
        _requestBattle = requestBattle;
        _requestOpenSelector = requestOpenSelector;
        ClearState();
    }
    private void ClearState()
    {
        _currentState = DungeonState.Enter;
        _partyController = null;
        _currentFloor = null;
        _currentRoute = null;
        _currentFloorNumber = 0;
    }
    public void NextState()
    {
        if(!IsEntering || _waitForInput)
            { return; }
        switch (_currentState)
        {
            case DungeonState.RouteSelect: //進行方向選択(=>イベント実行に遷移)
                RouteSelect();
                break;

            case DungeonState.ProceedFloor: //フロアを進む(=>進行方向選択に遷移)
                ProceedFloor();
                break;

            case DungeonState.Event: //ダンジョンイベント実行(=>フロアを進むor脱出に遷移)
                ExecuteDungeonEvent();
                break;

            case DungeonState.Boss: //ボス戦開始(=>次の階層に進むor脱出に遷移)
                EncounterBossEvent();
                break;

            case DungeonState.Battle: //バトル実行(=>アフターバトルに遷移)
                BattleStart();
                break;

            case DungeonState.AfterBattle: //アフターバトル(=>結果、ボス戦か、により分岐)
                AfterBattle();
                break;

            case DungeonState.NextFloor: //次の階層に進む(=>進行方向選択に遷移)
                ProceedToNextFloor();
                break;

            case DungeonState.Exit: //脱出(=>終了、リセット)
                ExitDungeon();
                break;
        };
    }
    public void EnterDungeon(PartyController enterdParty, int floorNum = 1)
    {
        if(_currentState is not DungeonState.Enter)
            { return; }
        IsEntering = true;

        _currentFloorNumber = floorNum;
        _currentFloor = new DungeonFloor(_currentFloorNumber);
        _partyController = enterdParty;

        _screenProvider.RefreshContent($"{enterdParty.PartyMember.First().Name}はダンジョンに侵入した");
        _screenProvider.WaitForEnter();

        _screenProvider.Set(ScreenLayer.Header, TextMasterData.GetDungeonHeaderText(_currentFloorNumber));
        _screenProvider.Set(ScreenLayer.MainView, TextMasterData.GetPartyText(_partyController));
        _screenProvider.RefreshUntil();

        _currentState = DungeonState.RouteSelect;
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

        _currentState = DungeonState.RouteSelect;
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
                OnCanceled: (canceled) => OnCanceled(),
                OnOpenMenu: (openMenu) => _requestOpenMenu?.Invoke(condition)
            );
        _requestOpenSelector?.Invoke(request);
        _waitForInput = true;
    }
    private void OnSuccess(RouteData routeData)
    {
        _currentRoute = routeData;
        _currentState = DungeonState.Event;
        _waitForInput = false;
    }
    private void OnCanceled()
    {
        _currentState = DungeonState.RouteSelect;
        _waitForInput = false;
    }
    public void ProceedFloor()
    {
        CheckFloorState();
        CheckRouteState();

        _currentFloor.Advance(_currentRoute.Progress);

        _screenProvider.Set(ScreenLayer.Content, $"進行度合い;{_currentFloor.CurrentProgress}" +
            $"(ボスまで{_currentFloor.FloorData.BossDistance})");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();

        _currentState = DungeonState.RouteSelect;
    }
    private void ExitDungeon()
    {
        IsEntering = false;
        _screenProvider.Set(ScreenLayer.Content, "ダンジョンから脱出した");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        _screenProvider.ClearAll();
        _screenProvider.ClearLog();
        _currentState = DungeonState.Enter;
    }
    private void OnBattleRequest(IReadOnlyList<EnemyCharacter> enemyParty, bool isBossBattle)
    {
        if (_requestBattle is null)
            throw new InvalidOperationException("戦闘開始のリクエストが設定されていません");
        BattleRequest request =
            new(enemyParty.ToList(), FieldType.Dungeon,
            (result) => OnBattleFinished(result), isBossBattle, _currentFloorNumber);
        _battleRequest = request;
        _currentState = DungeonState.Battle;
    }
    private void BattleStart()
    {
        CheckBattleRequestState();
        CheckRequestState();
        _screenProvider.Set(ScreenLayer.SubView, TextMasterData.GetEncounterEnemyText(_battleRequest.Enemies));
        _screenProvider.RefreshUntil(ScreenLayer.SubView);
        _requestBattle.Invoke(_battleRequest);
        _waitForInput = true;
    }
    private void OnBattleFinished(BattleResult result)
    {
        _waitForInput = false;
        _battleResult = result;
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
        _currentState = DungeonState.AfterBattle;
    }
    private void AfterBattle()
    {
        CheckBattleRequestState();
        CheckResultState();
        CheckFloorState();
        CheckPartyState();
        
        switch (_battleResult.BattleResultType)
        {
            case BattleResultType.Victory:
                if (_battleRequest.IsBossBattle)
                {
                    _screenProvider.Append(ScreenLayer.Content, "\nフロアボスを撃破した！");
                    _screenProvider.RefreshUntil(ScreenLayer.Content);
                    _currentState = DungeonState.NextFloor;
                }
                else
                    _currentState = DungeonState.ProceedFloor;
                break;

            case BattleResultType.Defeat:
                _screenProvider.RefreshUntil(ScreenLayer.Content);
                _screenProvider.WaitForEnter();
                _currentState = DungeonState.Exit;
                break;

            case BattleResultType.Escape:
                _screenProvider.Append(ScreenLayer.Content, $"{_partyController.PartyMember.First().Name}は逃走に成功した");
                if (_battleResult.ExitDungeon)
                    _currentState = DungeonState.Exit;
                else
                    _currentState = DungeonState.ProceedFloor;
                break;
        }
        _battleRequest = null;
        _battleResult = null;
    }
    private void ExecuteDungeonEvent()
    {
        CheckRouteState();
        CheckFloorState();
        switch (_currentRoute.EventType)
        {
            case DungeonEventType.Battle:
                BattleEvent(false);
                break;
            case DungeonEventType.Treasure:
                TreasureEvent();
                break;
            case DungeonEventType.None:
                NoneEvent();
                break;
            default:
                throw new InvalidOperationException("想定外のイベント");
        }
    }
    private IReadOnlyList<EnemyCharacter> GetEncounterBoss() //ボスエネミー生成
    {
        CheckFloorState();
        SpawnEnemyTable spawnTable = _currentFloor.GetSpawnTable();
        BossConfig bossConfig = _enemySpawnSelector.GetRandomBossParty(spawnTable.BossPartyConfigs);
        BossParty bossParty = BossPartyMasterData.GetBossParty(bossConfig.BossPartyID);
        IReadOnlyList<EnemyCharacter> bosses = _enemyGenerator.CreateBossEnemies(bossParty.BossMembers);
        return bosses;
    }
    private void BattleEvent(bool isBoss) //イベント用
    {
        CheckRouteState();
        if (_currentRoute.RouteContentData is BattleEventContent battle)
        {
            OnBattleRequest(battle.EnemyParty, isBoss);
        }
    }
    private void TreasureEvent() //イベント用
    {
        CheckRouteState();
        if (_currentRoute.RouteContentData is TreasureEventContent treasure)
        {
            _screenProvider.Set(ScreenLayer.Content, "宝箱を見つけた(仮)");
            _screenProvider.RefreshUntil(ScreenLayer.Content);
        }
        _currentState = DungeonState.ProceedFloor;
        _screenProvider.WaitForEnter();
    }
    private void NoneEvent() //イベント用
    {
        _currentState = DungeonState.ProceedFloor;
        _screenProvider.Set(ScreenLayer.Content, "何もなかった(仮)");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();
    }
    private void EncounterBossEvent()
    {
        _screenProvider.Append(ScreenLayer.Content, "ボスの気配がする(仮)");
        _screenProvider.RefreshUntil(ScreenLayer.Content);
        _screenProvider.WaitForEnter();

        var bosses = GetEncounterBoss();
        OnBattleRequest(bosses, true);
    }
    #region チェック用
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
    [MemberNotNull(nameof(_battleRequest))]
    private void CheckBattleRequestState()
    {
        if (_battleRequest is null)
            throw new InvalidOperationException("戦闘リクエスト内容が設定されていません");
    }
    [MemberNotNull(nameof(_requestBattle))]
    private void CheckRequestState()
    {
        if (_requestBattle is null)
            throw new InvalidOperationException("戦闘開始のリクエストが設定されていません");
    }
    [MemberNotNull(nameof(_battleResult))]
    private void CheckResultState()
    {
        if (_battleResult is null)
            throw new InvalidOperationException("戦闘結果が設定されていません");
    }
    #endregion
}