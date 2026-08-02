using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GameManager(ProvidorContext providorContext, BattleManagerGenerator battleManagerGenerator,
    MenuManager menuManager, DungeonManager dungeonManager, PartyController partyController, ScreenManager screenManager)
{
    private readonly ProvidorContext _providorContext = providorContext;
    private readonly BattleManagerGenerator _battleManagerGenerator = battleManagerGenerator; //ダンジョン外での戦闘用

    private readonly MenuManager _menuManager = menuManager;
    private readonly ScreenManager _screenManager = screenManager;
    private readonly DungeonManager _dungeonManager = dungeonManager;
    private readonly PartyController _partyController = partyController;

    public void Initialize()
    {
        _dungeonManager.Initialize(OpenMenuWindow, OnRequestBattle,
            (request) => _screenManager.RequestOpenSelector(request));
    }
    public void MainGameLoop()
    {
        Initialize();
        while (true)
        {
            string? input = _providorContext.InputProvider.Input();
            if(String.IsNullOrEmpty(input) || !int.TryParse(input, out int num))
            {
                _providorContext.ScreenProvider.Set(ScreenLayer.Content, "入力が正しくありません");
                _providorContext.ScreenProvider.RefreshUntil();
            }
            else
            {
                HandleInput(num);
            }
        }
    }
    public void HandleInput(int num)
    {
        _screenManager.HandleInput(num);
    }
    public void EnterToDungeon(int floorNum = 1)
    {
        _dungeonManager.EnterDungeon(_partyController, floorNum);
    }

    public void OpenMenuWindow(ConditionContext conditionContext)
    {
        _screenManager.OpenMenu(_partyController, conditionContext);
    }
    public BattleResult OnRequestBattle(List<EnemyCharacter> enemies, FieldType fieldType, int floorNum = 0)
    {
        BattleManager battleManager = 
            _battleManagerGenerator.Create(
                _providorContext.LogProvider, 
                _providorContext.RandomProvider,
                _providorContext.InputProvider, 
                _providorContext.ScreenProvider,
                enemies, _partyController,
                fieldType, floorNum);
        return battleManager.BattleStart();
    }
}
