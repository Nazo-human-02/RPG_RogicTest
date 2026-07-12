using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CommandSelect(ILogProvider logProvider, IInputProvider input, IScreenProvider screenProvider)
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly IInputProvider _inputProvider = input;
    private readonly IScreenProvider _screenProvider = screenProvider;
    private readonly Dictionary<int, ActionType> commandOption = new Dictionary<int, ActionType>();

    public void InitializeCommand()
    {
        commandOption.Clear();
        commandOption[0] = ActionType.Attack;
        commandOption[1] = ActionType.Guard;
        commandOption[2] = ActionType.Skill;
        commandOption[3] = ActionType.UseItem;
        commandOption[4] = ActionType.Escape;
    }

    public ActionType WaitCommandSelect(Entity entity)
    {
        _screenProvider.Clear(ScreenLayer.Content);
        while (true)
        {
            _screenProvider.Set(ScreenLayer.InputArea, "[0:攻撃, 1:防御, 2:スキル, 3:アイテム, 4:逃走]");
            //_logProvider.WriteLog();
            _screenProvider.RefreshUntil();
            string? selected = _inputProvider.Input();

            if (string.IsNullOrEmpty(selected) || !int.TryParse(selected, out var n))
            {
                _screenProvider.Set(ScreenLayer.Content, "!入力が正しくありません!");
                //_logProvider.WriteLog();
            }
            else if (!commandOption.TryGetValue(n, out var actionType))
            {
                _screenProvider.Set(ScreenLayer.Content, "!設定されていない番号です!");
                //_logProvider.WriteLog("!設定されていない番号です!");
            }
            else if (actionType == ActionType.Skill && entity.ValidSkills.Count == 0)
            {
                _screenProvider.Set(ScreenLayer.Content, $"!{entity.Name}はスキルを所持していません!");
                //_logProvider.WriteLog($"!{entity.Name}はスキルを所持していません!");
            }
            else
            {
                return actionType;
            }
        }
    }
}

