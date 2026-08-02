using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StatusMenu(IInputProvider inputProvider, IScreenProvider screenProvider) 
    : MemberMenuBase(inputProvider, screenProvider), IMenu
{
    public MenuState CurrentMenuState => _currentMenuState;
    private MenuState _currentMenuState = MenuState.MainMenu;
    public bool IsClosed => _isClosed;
    private bool _isClosed = true;
    public Action<ISelectorRequest>? OpenSelector { get; set; } = null;
    public void OpenMenu(PartyController partyController)
    {
        ValidMenu(partyController);
    }
    public void HandleInput(int num)
    {
        if (num < 0 || num > _commands.Count)
        {
            SelectErrorText(-2);
            return;
        }
        _commands[num].Execute.Invoke();
    }
    public override void ValidMenu(PartyController partyController)
    {
        Initialize(partyController);
        SetCommands();
        Render(_currentMember);
        _isClosed = false;
    }
    private void SetCommands()
    {
        _commands.Clear();
        _commands[0] = new("<0>|もどる", 0, () => Close());
        int num = 1;
        foreach(var member in _displayMembers)
        {
            string showing = (member == _currentMember) ? "(表示中)" : "";
            _commands[num] = new($"<{num}>|{member.Name}{showing}", num, () => TryChangePage(num));
            num++;
        }
    }
    private void TryChangePage(int num)
    {
        if (!ChangePage(num))
            _screen.Set(ScreenLayer.Content, "ページの切り替えに失敗しました");
        else
        {
            SetCommands();
            Render(_currentMember);
        }
    }
    protected override void Render(Entity member)
    {
        StringBuilder sb = new();
        sb.AppendLine("==ステータス==");
        sb.AppendLine(TextMasterData.GetCharacterStatusText(member));
        sb.AppendLine(TextMasterData.GetCharacterSubWindowText(member));
        sb.AppendLine(SelectOptionText());
        sb.AppendLine(TextMasterData.GetWindowLine());

        _screen.RefreshInput(sb.ToString());
    }
    private string SelectOptionText()
    {
        StringBuilder sb = new();
        foreach (var command in _commands)
        {
            sb.Append(command.Value.Text);
        }
        return sb.ToString();
    }

    public void Close()
    {
        _isClosed = true;
    }
}