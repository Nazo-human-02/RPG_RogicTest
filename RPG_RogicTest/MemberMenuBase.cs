using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class MemberMenuBase(IInputProvider inputProvider, IScreenProvider screenProvider)
{
    protected IInputProvider _input = inputProvider;
    protected IScreenProvider _screen = screenProvider;

    protected List<Entity> _displayMembers = new();
    protected Dictionary<int, MenuCommand> _commands = new();
    protected int _currentPage = 1;
    protected Entity _currentMember 
        => _displayMembers[_currentPage - 1];
    protected virtual void Initialize(PartyController partyController)
    {
        _displayMembers = partyController.PartyMember.Cast<Entity>().ToList();
        if (_displayMembers.Count == 0)
            throw new Exception("パーティーメンバーが0人です");
        _currentPage = 1;
    }
    protected bool ChangePage(int num)
    {
        if (num < 1 || num > _displayMembers.Count)
            return false;
        _currentPage = num;
        return true;
    }
    protected bool TryReadNumber(string? input, out int num)
    {
        if(string.IsNullOrEmpty(input) || !int.TryParse(input, out int inputNum))
        {
            num = -1;
            return false;
        }

        num = inputNum;
        return true;
    }
    public abstract void ValidMenu(PartyController partyController);
    protected abstract void Render(Entity member);
    protected void SelectErrorText(int errorId)
    {
        if (errorId == -1)
        {
            _screen.Set(ScreenLayer.Content, "入力が正しくありません");
        }
        else if (errorId == -2)
        {
            _screen.Set(ScreenLayer.Content, "範囲外の入力です");
        }
        _screen.RefreshUntil();
    }


}

public interface IUpdateCondition
{
    ConditionContext ConditionContext { get;}
    void UpdateCondition(ConditionContext conditionContext);
}
public record MenuCommand
    (
    string Text,
    int Number,
    Action Execute
    );