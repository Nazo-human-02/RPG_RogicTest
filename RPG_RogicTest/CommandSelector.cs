using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CommandSelect(ILogProvider logProvider)
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly Dictionary<int, ActionType> commandOption = new Dictionary<int, ActionType>();

    public void InitializeCommand()
    {
        commandOption.Clear();
        commandOption[0] = ActionType.Attack;
        commandOption[1] = ActionType.Guard;
        commandOption[2] = ActionType.Skill;
        commandOption[3] = ActionType.Escape;
    }

    public ActionType WaitCommandSelect(Entity entity)
    {
        _logProvider.Log("[0:攻撃, 1:防御, 2:スキル, 3:逃走]");

        string? selected = Console.ReadLine();

        if (string.IsNullOrEmpty(selected) || !int.TryParse(selected, out var n))
        {
            _logProvider.Log("!入力が正しくありません!");
            return WaitCommandSelect(entity);

        }
        if (!commandOption.TryGetValue(n, out var actionType))
        {
            _logProvider.Log("!設定されていない番号です!");
            return WaitCommandSelect(entity);
        }
        if (actionType == ActionType.Skill && entity.ValidSkills.Count == 0)
        {
            _logProvider.Log($"!{entity.Name}はスキルを所持していません!");
            return WaitCommandSelect(entity);
        }
        return actionType;
    }
}

