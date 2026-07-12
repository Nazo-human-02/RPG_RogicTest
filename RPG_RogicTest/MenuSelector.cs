using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MenuSelector(IInputProvider inputProvider, IScreenProvider screenProvider)
{
    private readonly IInputProvider _input = inputProvider;
    private readonly IScreenProvider _screen = screenProvider;
    private Dictionary<int, MenuType?> _menuOptions = new();

    public SelectionResult<MenuType> MenuOptionSelect()
    {
        InitializeSelecting();

        return GetSelectResult();
    }

    private void InitializeSelecting()
    {
        StringBuilder sb = new();
        _menuOptions.Clear();

        sb.AppendLine("表示するメニューを選択、エンターで決定");
        int n = 0;
        _menuOptions[0] = null;
        sb.Append("[0:もどる]");
        foreach(MenuType menuType in Enum.GetValues<MenuType>())
        {
            n++;
            sb.Append($"[{n}:{TextMasterData.GetMenuTypeText(menuType)}]");
            _menuOptions[n] = menuType;
        }

        _screen.Set(ScreenLayer.InputArea, sb.ToString());
        _screen.Clear(ScreenLayer.Content);
        _screen.RefreshUntil();
    }
    
    private SelectionResult<MenuType> GetSelectResult()
    {
        while(true)
        {
            string? input = _input.Input();

            if(String.IsNullOrEmpty(input) || !int.TryParse(input, out int num))
            {
                _screen.Set(ScreenLayer.Content, "対応する番号を入力してください");
            }
            else if(!_menuOptions.TryGetValue(num, out MenuType? option))
            {
                _screen.Set(ScreenLayer.Content, "その番号は対応していません");
            }
            else
            {
                return (option != null) ? new SelectionSuccess<MenuType>((MenuType)option) : new SelectionCancel<MenuType>();
            }
            _screen.RefreshUntil();
        }
    }
}

public interface IMenuDetailSelector
{

}

