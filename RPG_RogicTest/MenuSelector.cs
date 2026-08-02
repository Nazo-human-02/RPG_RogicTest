using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MenuSelector(IInputProvider inputProvider, IScreenProvider screenProvider) : ISelector<MenuType>
{
    private readonly IInputProvider _input = inputProvider;
    private readonly IScreenProvider _screen = screenProvider;
    private Dictionary<int, MenuType?> _menuOptions = new();
    private Dictionary<int, SelectionCommand<MenuType>> _selectionCommands = new();
    public void Open()
    {
        InitializeCommands();
        Render();
    }
    public void HandleInput(int num, out SelectionResult<MenuType>? result)
    {
        if(!_selectionCommands.TryGetValue(num, out var option))
        {
            _screen.Set(ScreenLayer.Content, "選択肢の範囲外です");
            _screen.RefreshUntil();
            result = null;
        }
        else
        {
            result = option.Execute.Invoke();
        }
    }
    private void InitializeCommands()
    {
        _selectionCommands.Clear();
        _selectionCommands[0] =
            new SelectionCommand<MenuType>($"[0:もどる]", 0, () => new SelectionCancel<MenuType>());

        int num = 1;
        foreach(MenuType option in Enum.GetValues<MenuType>())
        {
            _selectionCommands[num] = 
                new SelectionCommand<MenuType>($"[{num}:{TextMasterData.GetMenuTypeText((MenuType)option)}]", 
                num, () => new SelectionSuccess<MenuType>(option));
            num++;
        }
    }
    private void Render()
    {
        StringBuilder sb = new();
        sb.AppendLine("表示するメニューを選択、エンターで決定");
        foreach(var command in _selectionCommands.Values)
        {
            sb.AppendLine(command.Text);
        }
        _screen.RefreshInput(sb.ToString());
    }
}

