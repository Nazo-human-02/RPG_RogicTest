using System;

public static class SkillSelection
{
    public static Skill SkillSelect(Entity entity)
    {
        Skill? currentSelect = null;
        Dictionary<int, Skill> skillDict = new Dictionary<int, Skill>();
        int n = 1;
        foreach(Skill skill in entity.ValidSkills)
        {
            skillDict[n] = skill;
            n++;
        }
        SkillSelectText(skillDict);

        return Selecting(skillDict, currentSelect);
    }
    private static Skill Selecting(Dictionary<int, Skill> skillDict, Skill? currentSelect)
    {
        string? num = Console.ReadLine();
        
        if(string.IsNullOrEmpty(num))
        {
            if(currentSelect != null)
            {
                return currentSelect;
            }
            else
            {
                LogWrite.Log("スキルを選択してください");
            }
        }
        else if(!int.TryParse(num, out int n) || !skillDict.TryGetValue(n, out var skill))
        {
            LogWrite.Log("入力が正しくありません");
        }
        else if(skill.CurrentCoolTime > 0)
        {
            LogWrite.Log($"クールタイム中:残り{skill.CurrentCoolTime}ターン");
        }
        else
        {
            currentSelect = skill;
            LogWrite.Log($"現在選択中:{skill.Name}(Enterキーで確定)");
        }
        return Selecting(skillDict, currentSelect);
    }
    private static void SkillSelectText(Dictionary<int, Skill> skillDict)
    {
        string text = "";
        foreach(var dict in skillDict)
        {
            text += $"[{dict.Key}:{dict.Value.Name}]";
        }
        text += "\nEnterキーで確定";
        LogWrite.Log(text);
    }
}