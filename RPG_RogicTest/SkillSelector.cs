using System;

public class SkillSelection(ILogProvider log, IInputProvider inputProvider)
{
    private readonly ILogProvider _log = log;
    private readonly IInputProvider _input = inputProvider;
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
                    _log.Log("スキルを選択してください");
                }
            }
            else if (!int.TryParse(num, out int n) || n < 0 || n > skillList.Count)
            {
                _log.Log("入力が正しくありません");
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
                    _log.Log($"クールタイム中:残り{skill.CurrentCoolTime}ターン");
                    continue;
                }

                selected = skill;
                _log.Log($"現在選択中:{selected.Name}(Enterキーで確定)");
            }
        }
    }
    private void SkillSelectText(IReadOnlyList<Skill> skillList)
    {
        string text = "[0:もどる]";
        for (int i = 0; i < skillList.Count; i++)
        {
            text += $"[{i + 1}:{skillList[i].Name}]";
        }
        text += "\nEnterキーで確定";
        _log.Log(text);
    }
}