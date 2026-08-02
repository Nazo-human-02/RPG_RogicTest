using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SkillMenu(TargetSelector targetSelector, BattleCalculator battleCalculator,
    IInputProvider inputProvider, IScreenProvider screenProvider, 
    ConditionContext conditionContext)
    : MemberMenuBase(inputProvider, screenProvider) , IUpdateCondition, IMenu
{
    private readonly TargetSelector _targetSelector = targetSelector;
    private readonly BattleCalculator _battleCalculator = battleCalculator;
    public MenuState CurrentMenuState => _currentMenuState;
    private MenuState _currentMenuState = MenuState.MainMenu;
    public bool IsClosed => _isClosed;
    private bool _isClosed = true;
    public Action<ISelectorRequest>? OpenSelector { get; set; } = null;
    public ConditionContext ConditionContext => _conditionContext with {User = _currentMember };
    private ConditionContext _conditionContext = conditionContext;

    private Action? _onUseSkill = null;
    public void HandleInput(int num)
    {
        switch (_currentMenuState)
        {
            case MenuState.MainMenu:
                MainCommands(num);
                break;
            case MenuState.Detail:
                DetailCommands(num);
                break;
        }
    }
    private void MainCommands(int num)
    {
        if (num < 0 || num > _commands.Count)
        {
            SelectErrorText(-2);
            return;
        }
        _commands[num].Execute.Invoke();
    }
    private void DetailCommands(int num)
    {
        if (num != 0 && num != 1)
        {
            SelectErrorText(-2);
            return;
        }
        else if (num == 1 && _onUseSkill == null)
        {
            _screen.Append(ScreenLayer.InputArea, "そのスキルは使用できません");
            _screen.RefreshUntil();
        }
        else if (num == 0)
        {
            Render(_currentMember);
            _currentMenuState = MenuState.MainMenu;
        }
        else if (num == 1 && _onUseSkill != null)
        {
            _onUseSkill.Invoke();
        }
    }
    public void OpenMenu(PartyController partyController)
    {
        _isClosed = false;
        ValidMenu(partyController);
    }
    public void Close()
    {
        _isClosed = true;
    }
    public override void ValidMenu(PartyController partyController)
    {
        Initialize(partyController);
        _currentMenuState = MenuState.MainMenu;
        Render(_currentMember);
    }
    private void SetCommandDict(Entity showMember)
    {
        _commands.Clear();
        int n = 1;
        foreach(var skill in showMember.ValidSkills)
        {
            string text  = $"-<{n}>-[{skill.SkillInfo.SkillName}]";
            _commands[n] = new (text, n, () => ValidSkillDetail(skill));
            n++;
        }
        int i = 1;
        foreach(var member in _displayMembers)
        {
            string showing = (i == _currentPage) ? "(表示中)" : "";
            string text = $"-<{n}>-[{member.Name?? "nameless"}{showing}]";
            _commands[n] = new (text, n, () => ChangedisplayPage(i));
            i++;
            n++;
        }
        _commands[0] = new ("-<0>-[もどる]|番号を入力して詳細,ページ切り替え", 0, () => Close());
    }
    protected override void Render(Entity member)
    {
        SetCommandDict(member);
        StringBuilder sb = new();
        sb.AppendLine("==スキルメニュー==");
        foreach (var command in _commands)
        {
            sb.AppendLine(command.Value.Text);
        }
        _screen.RefreshInput(sb.ToString());
    }
    private void ChangedisplayPage(int num)
    {
        if (ChangePage(num))
        {
            Render(_currentMember);
        }
    } //スキルを表示するメンバーを変更する処理
    private void RenderSkillDetail(bool canUse, Skill skill)
    {
        string text = "<0>|もどる";
        text += (canUse) ? "<1>使用する" : "使用不可";

        _screen.Set(ScreenLayer.Content, $"{skill.SkillInfo.SkillName}|スキルの説明");
        _screen.Set(ScreenLayer.InputArea, text);
        _screen.RefreshUntil();
    }
    private void ValidSkillDetail(Skill skill)
    {
        UseLessType useLessType = UseValidator.TryUseSkill(ConditionContext, skill, out var result);
        bool canUse = UseValidator.CanUse(useLessType);
        RenderSkillDetail(canUse, skill);
        _currentMenuState = MenuState.Detail;
        _onUseSkill = (canUse) ? 
          () =>RequestTargetSelector(_currentMember, skill, ConditionContext.RandomProvider, result) : null;
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
    private void RequestTargetSelector(Entity user, Skill skill, IRandomProvider random, TargetResolveResult result)
    {
        RequestOpenSelector<List<Entity>> request = 
            new(_targetSelector, () => _targetSelector.Open(result), (targets) => UseSkill(user, skill, targets.Value, random),
            (cancel) => OnCanceled(cancel, skill));
        OpenSelector?.Invoke(request);
    }
    private void OnCanceled(SelectionResult<List<Entity>> cancel, Skill skill)
    {
        RenderSkillDetail(true, skill); //使用できる前提なのでtrueを渡す
    }
    public void UpdateCondition(ConditionContext conditionContext)
    {
        _conditionContext = conditionContext;
    }
}