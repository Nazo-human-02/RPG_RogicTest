using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GameManager(ProvidorContext providorContext, BattleManagerGenerator battleManagerGenerator,
    DungeonManager dungeonManager, PartyController partyController, ScreenManager screenManager)
{
    private readonly ProvidorContext _providorContext = providorContext;
    private readonly BattleManagerGenerator _battleManagerGenerator = battleManagerGenerator; //ダンジョン外での戦闘用

    private readonly ScreenManager _screenManager = screenManager;
    private readonly DungeonManager _dungeonManager = dungeonManager;
    private readonly PartyController _partyController = partyController;

    private BattleManager? _battleManager;

    private void InitializeDungeonManager()
    {
        _dungeonManager.Initialize(OpenMenuWindow, OnRequestBattle,
            (request) => _screenManager.RequestOpenSelector(request));
    }
    private void InitializeScreenManager()
    {
        _screenManager.Initialize();
    }
    public void MainGameLoop()
    {
        InitializeScreenManager();
        InitializeDungeonManager();
        EnterToDungeon();
        while (true)
        {
            if(_screenManager.ValidHandleInput)
                HandleInput();
            else if (_battleManager is not null)
                _battleManager.NextState();
            else if (_dungeonManager.IsEntering)
                _dungeonManager.NextState();
            //Console.WriteLine("ループ");
        }
    }
    public void HandleInput()
    {
        Console.WriteLine("ゲームマネージャー入力待機");
        string? input = _providorContext.InputProvider.Input();
        if (String.IsNullOrEmpty(input) || !int.TryParse(input, out int num))
        {
            _providorContext.ScreenProvider.Set(ScreenLayer.ErrorArea, "入力が正しくありません");
            _providorContext.ScreenProvider.RefreshUntil();
        }
        else
        {
            _screenManager.HandleInput(num);
        }
    }
    private void EnterToDungeon(int floorNum = 1)
    {
        _dungeonManager.EnterDungeon(_partyController, floorNum);
    }

    private void OpenMenuWindow(ConditionContext conditionContext)
    {
        _screenManager.OpenMenu(_partyController, conditionContext);
    }
    private void OnRequestBattle(BattleRequest battleRequest)
    {
        BattleManager battleManager = 
            _battleManagerGenerator.Create(
                _providorContext.LogProvider, 
                _providorContext.RandomProvider,
                _providorContext.InputProvider, 
                _providorContext.ScreenProvider,
                battleRequest.Enemies, _partyController,
                battleRequest.FieldType, battleRequest.floorNum);
        battleManager.Initialize(
            request => _screenManager.RequestOpenSelector(request),
            (result) => 
            {
                battleRequest.OnFinished(result); 
                _battleManager = null; 
            });

        _battleManager = battleManager;
    }
}
public record BattleRequest
(
    List<EnemyCharacter> Enemies,
    FieldType FieldType,
    Action<BattleResult> OnFinished,
    bool IsBossBattle,
    int floorNum = 0
);