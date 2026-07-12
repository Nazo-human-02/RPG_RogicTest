using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SkillMenu(TargetResolver targetResolver, TargetSelect targetSelector, BattleCalculator battleCalculator)
{
    private readonly TargetResolver _targetResolver = targetResolver;
    private readonly TargetSelect _targetSelector = targetSelector;
    private readonly BattleCalculator _battleCalculator = battleCalculator;

    private List<Entity> _skillMembersDisplay = new();

    private (List<Skill>, Entity) Initialize(PartyController partyController, IScreenProvider screen, int pageNum = 1)
    {
        StringBuilder sb = new();
        sb.Append("=スキル一覧=");

        _skillMembersDisplay = partyController.PartyMember.Cast<Entity>().ToList();
        if(pageNum > _skillMembersDisplay.Count)
        {
            return (_skillMembersDisplay[0].ValidSkills.ToList(), _skillMembersDisplay[0]);
        }
        var member = _skillMembersDisplay[pageNum - 1];

        sb.AppendLine($"[{member.Name}]");
        int num = 1;
        foreach(var skill in member.ValidSkills)
        {
            sb.AppendLine($"-<{num}>- [{skill.SkillInfo.SkillName}]");
            num++;
        }
        sb.AppendLine("<0>でもどる|番号を入力しアイテム詳細");

        screen.Set(ScreenLayer.InputArea, sb.ToString());
        screen.Clear(ScreenLayer.Content);
        screen.RefreshUntil();

        return (member.ValidSkills.ToList(), member);
    }
    private void ChangedisplayPage() { } //スキルを表示するメンバーを変更する処理
    public void ValidSkillMenu(PartyController partyController, ConditionContext conditionContext,
        IScreenProvider screen, IInputProvider input, IRandomProvider random)
    {
        var currentDisplay = Initialize(partyController, screen);
        int currentPage = 1;
        while(true)
        {
            string? inputText = input.Input();

            if(string.IsNullOrEmpty(inputText) || !int.TryParse(inputText, out int inputNum) 
                || (inputNum < 0 || inputNum > currentDisplay.Item1.Count))
            {
                screen.Set(ScreenLayer.Content, "入力が正しくありません");
            }
            else if (inputNum == 0)
            {
                break;
            }
            else
            {
                var skill = currentDisplay.Item1[inputNum -1];
                var result = _targetResolver.TargetResolve(skill.ConditionData, conditionContext, skill.TargetData);
                string text = "<0>|もどる";
                bool canPay = (skill is ActiveSkill activeSkill) ? activeSkill.TryPayCost(currentDisplay.Item2) : true;
                text += (result.TargetCandidates.Count > 0 && skill.CurrentCoolTime == 0)
                    ? "<1>使用する" : "\033[9m<1>使用する\033[0m";

                screen.Set(ScreenLayer.Content, $"{skill.SkillInfo.SkillName}|スキルの説明");
                screen.Set(ScreenLayer.InputArea, text);
                screen.RefreshUntil();

                while(true)
                {
                    string? inputT = input.Input();

                    if (string.IsNullOrEmpty(inputT) || !int.TryParse(inputT, out int inputN)
                        || (inputN != 0 && inputN != 1))
                    {
                        screen.Set(ScreenLayer.Content, "入力が正しくありません");
                    }
                    else if (inputN == 0)
                    {
                        currentDisplay = Initialize(partyController, screen, currentPage);
                        break;
                    }
                    else
                    {
                        var targets = _targetSelector.SelectingTargets(result);
                        if (targets is not SelectionSuccess<List<Entity>> success)
                        {
                            currentDisplay = Initialize(partyController, screen, currentPage);
                        }
                        else
                        {
                            UseSkill(currentDisplay.Item2, skill, success.Value, random);
                            currentDisplay = Initialize(partyController, screen, currentPage);
                        }
                    }
                }
            }

            screen.RefreshUntil();
        }
    }

    private void UseSkill(Entity skillUser, Skill skill, List<Entity> targets, IRandomProvider random)
    {
        foreach(var target in targets)
        {
            EffectContent effectContent = new(skillUser, target, null, _battleCalculator, random);
            ActionUnit actionUnit = new(ActionType.Skill, ActionSource.FromSkill(skill), skillUser, target, skill:skill);
            skill.ExecuteSkill(actionUnit, target, effectContent);
        }
    }
}