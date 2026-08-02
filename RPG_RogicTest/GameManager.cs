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
        _dungeonManager.Initialize(OpenMenuWindow);
    }
    public void EnterToDungeon(int floorNum = 1)
    {
        _dungeonManager.EnterDungeon(_partyController, floorNum);
    }

    public void OpenMenuWindow(ConditionContext conditionContext)
    {
        _screenManager.OpenMenu(_partyController, conditionContext);
    }
}
