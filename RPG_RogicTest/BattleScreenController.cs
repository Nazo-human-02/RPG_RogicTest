using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleScreenController(IScreenProvider screen)
{
    private readonly IScreenProvider _screen = screen;

    public void BattleStart()
    {
        _screen.Set(ScreenLayer.Content, "戦闘開始");
        RefreshAndWait(ScreenLayer.Content);
    }
    public void TurnStart(int currentTurn, BattleSession battleSession)
    {
        _screen.Set(ScreenLayer.Label, $"-----------{currentTurn}ターン目---------------");
        _screen.Set(ScreenLayer.SubView, TextMasterData.GetEncounterEnemyText(battleSession.GetAliveEnemy()));
        _screen.Clear(ScreenLayer.Content);
        _screen.RefreshUntil();
    }
    public void UpdatePartyText(PartyController partyController)
    {
        _screen.Set(ScreenLayer.MainView, TextMasterData.GetPartyText(partyController));
        _screen.RefreshUntil();
    }
    public void ResultText(BattleResultType battleResult)
    {
        string text = (battleResult) switch
        {
            BattleResultType.Victory => "_戦闘に勝利した!_",
            BattleResultType.Defeat => "_戦闘に敗北した..._",
            BattleResultType.Escape => "_戦闘から逃げ出した",
            _ => "想定外の結果"
        };
        _screen.Set(ScreenLayer.Content, text);
        _screen.RefreshUntil(ScreenLayer.Content);
        _screen.WaitForEnter();
    }
    public void RefreshAndWait(ScreenLayer screenLayer = ScreenLayer.None)
    {
        _screen.RefreshUntil(screenLayer);
        _screen.WaitForEnter();
    }
    public void Clear(ScreenLayer screenLayer)
    {
        _screen.Clear(screenLayer);
    }
}