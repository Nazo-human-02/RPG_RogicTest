using System;
using System.Text;

public class SkillSelection(ILogProvider log, IInputProvider inputProvider, IScreenProvider screenProvider)
    : ISelector<Skill>
{
    private readonly ILogProvider _log = log;
    private readonly IInputProvider _input = inputProvider;
    private readonly IScreenProvider _screen = screenProvider;
    private Dictionary<int, SelectionCommand<Skill>> _selectionCommands = new();

    public void Open(Entity entity)
    {
        SetCommandsDict(entity);
        Render();
    }
    public void HandleInput(int num, out SelectionResult<Skill>? result)
    {
        result = null;
        if(!_selectionCommands.TryGetValue(num, out var command))
        {
            _screen.Set(ScreenLayer.Content, "選択範囲外です");
            _screen.RefreshUntil();
            return;
        }
        result = command.Execute.Invoke();
    }
    private void SetCommandsDict(Entity entity)
    {
        _selectionCommands.Clear();
        int num = 1;
        foreach(var skill in entity.ValidSkills)
        {
            bool useAble = (skill.CurrentCoolTime <= 0);
            _selectionCommands[num] = new($"[{num}:{skill.SkillInfo.SkillName}]", num, () => OnSelect(skill, useAble));
            num++;
        }
        _selectionCommands[0] = new("[0:もどる]", 0, () => new SelectionCancel<Skill>());
    }
    private void Render()
    {
        StringBuilder sb = new();
        foreach(var command in _selectionCommands.Values)
        {
            sb.Append(command.Text);
        }
        _screen.RefreshInput(sb.ToString());
    }
    private SelectionResult<Skill> OnSelect(Skill skill, bool canUse)
    {
        if (canUse)
            return new SelectionSuccess<Skill>(skill);
        else
        {
            _screen.Set(ScreenLayer.Content, $"クールタイム中:残り{skill.CurrentCoolTime}ターン");
            _screen.RefreshUntil();
            return new SelectionContinue<Skill>();
        }
    }
}