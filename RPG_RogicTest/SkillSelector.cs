using System;

public class SkillSelection(ILogProvider log, IInputProvider inputProvider, IScreenProvider screenProvider)
{
    private readonly ILogProvider _log = log;
    private readonly IInputProvider _input = inputProvider;
    private readonly IScreenProvider _screen = screenProvider;
    public SelectionResult<Skill> SkillSelect(Entity entity)
    {
        IReadOnlyList<Skill> skills = entity.ValidSkills.ToList();
        SkillSelectText(skills);

        return WaitForSkillSelection(skills);
    }
    private SelectionResult<Skill> WaitForSkillSelection(IReadOnlyList<Skill> skillList)
    {
        Skill? selected = null;
        while (true)
        {
            string? num = _input.Input();

            if (string.IsNullOrEmpty(num))
            {
                if (selected != null)
                {
                    return new SelectionSuccess<Skill>(selected);
                }
                else
                {
                    _screen.Set(ScreenLayer.Content, "スキルを選択してください");
                    //_log.WriteLog("スキルを選択してください");
                }
            }
            else if (!int.TryParse(num, out int n) || n < 0 || n > skillList.Count)
            {
                _screen.Set(ScreenLayer.Content, "入力が正しくありません");
                //_log.WriteLog("入力が正しくありません");
            }
            else if(n == 0)
            {
                return new SelectionCancel<Skill>();
            }
            else
            {
                Skill skill = skillList[n - 1];
                if (skill.CurrentCoolTime > 0)
                {
                    _screen.Set(ScreenLayer.Content, $"クールタイム中:残り{skill.CurrentCoolTime}ターン");
                    //_log.WriteLog($"クールタイム中:残り{skill.CurrentCoolTime}ターン");
                    continue;
                }
                else
                {
                    selected = skill;
                    _screen.Set(ScreenLayer.Content, $"現在選択中:{selected.SkillInfo.SkillName}(Enterキーで確定)");
                }
            }
            _screen.RefreshUntil();
        }
    }
    private void SkillSelectText(IReadOnlyList<Skill> skillList)
    {
        string text = "[0:もどる]";
        for (int i = 0; i < skillList.Count; i++)
        {
            text += $"[{i + 1}:{skillList[i].SkillInfo.SkillName}]";
        }
        text += "\nEnterキーで確定";
        _screen.Set(ScreenLayer.InputArea, text);
        _screen.Clear(ScreenLayer.Content);
        _screen.RefreshUntil();
        //_log.WriteLog(text);
    }
}