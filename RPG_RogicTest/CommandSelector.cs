using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CommandSelect(ILogProvider logProvider, IInputProvider input, IScreenProvider screenProvider) 
    : ISelector<ActionType>
{
    private readonly ILogProvider _logProvider = logProvider;
    private readonly IInputProvider _inputProvider = input;
    private readonly IScreenProvider _screenProvider = screenProvider;
    private readonly Dictionary<int, ActionType> commandOption = new() 
    {
        [0] = ActionType.Attack,
        [1] = ActionType.Guard,
        [2] = ActionType.Skill,
        [3] = ActionType.UseItem,
        [4] = ActionType.Escape
    };
    private Dictionary<int, SelectionCommand<ActionType>> _selectionCommands = new();

    public void Open(Entity entity)
    {
        SetCommandsDict(entity);
        Render();
    }
    public void HandleInput(int num, out SelectionResult<ActionType>? result)
    {
        if (!_selectionCommands.TryGetValue(num, out var command))
        {
            _screenProvider.Set(ScreenLayer.Content, "選択肢の範囲外です");
            _screenProvider.RefreshUntil();
            result = null;
            return;
        }
        result = command.Execute();
    }
    private void SetCommandsDict(Entity entity)
    {
        _selectionCommands.Clear();
        foreach (var option in commandOption)
        {
            if (option.Value == ActionType.Skill && entity.ValidSkills.Count == 0)
            {
                _selectionCommands[option.Key] = new SelectionCommand<ActionType>(
                    $"[{option.Key}:{GetActionTypeText(option.Value)}]", option.Key, () => ShortageContent(option.Value));
            }
            else
            {
                _selectionCommands[option.Key] = new SelectionCommand<ActionType>(
                    $"[{option.Key}:{GetActionTypeText(option.Value)}]", option.Key, 
                    () => new SelectionSuccess<ActionType>(option.Value));
            }
        }
    }
    private SelectionContinue<ActionType> InvalidAction(ActionType actionType) //特殊な状態用
    {
        _screenProvider.Set(ScreenLayer.Content, $"!{GetActionTypeText(actionType)}は使用できません!");
        _screenProvider.RefreshUntil();
        return new SelectionContinue<ActionType>();
    }
    private SelectionContinue<ActionType> ShortageContent(ActionType actionType) 
    {
        _screenProvider.Set(ScreenLayer.Content, $"使用できる{GetActionTypeText(actionType)}がありません");
        _screenProvider.RefreshUntil(); 
        return new SelectionContinue<ActionType>();
    }
    private void Render()
    {
        StringBuilder sb = new();
        sb.AppendLine("行動を選択、エンターで決定");
        foreach (var command in _selectionCommands.Values)
        {
            sb.Append(command.Text);
        }
        _screenProvider.RefreshInput(sb.ToString());
    }
    public static string GetActionTypeText(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Attack => "攻撃",
            ActionType.Guard => "防御",
            ActionType.Skill => "スキル",
            ActionType.UseItem => "アイテム",
            ActionType.Escape => "逃走",
            _ => throw new ArgumentException($"Unknown action type: {actionType}"),
        };
    }
}

