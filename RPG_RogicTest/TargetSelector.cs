using System;
using System.Text;


public class TargetSelector(ILogProvider log, IInputProvider input, IScreenProvider screenProvider) : ISelector<List<Entity>>
{
    private readonly ILogProvider _log = log;
    private readonly IInputProvider _input = input;
    private readonly IScreenProvider _screen = screenProvider;

    private Dictionary<int, SelectionCommand<List<Entity>>> _selectionCommands = new();
    private List<Entity> _currentSelected = new();
    public void Open(TargetResolveResult resolveResult)
    {
        _currentSelected.Clear();
        SetCommands(resolveResult);
        Render();
    }
    private void SetCommands(TargetResolveResult resolveResult)
    {
        _selectionCommands.Clear();
        int num = 1;
        bool isShortage = resolveResult.TargetCandidates.Count <= resolveResult.TargetAmount;
        _selectionCommands[0] = new("[もどる:<0>]", 0, OnClosed);
        foreach(var target in resolveResult.TargetCandidates)
        {
            bool isSelected = _currentSelected.Contains(target);
            string t = (isSelected) ? "選択中" : "未選択";
            t = (isShortage) ? "自動選択" : t;
            string text = $"[{num}:{target.Name}(HP:{target.Stat.CurrentHp}/{target.Stat.TotalHP},{t})]";
            _selectionCommands[num] = new(text, num, () => OnSelected(target, isShortage, resolveResult));
            num++;
        }
        if(isShortage) _currentSelected = resolveResult.TargetCandidates;
        int rest = resolveResult.TargetAmount - _currentSelected.Count;
        _selectionCommands[num] = 
            new($"[確定:<{num}>](選択可能数 残り:{rest})", num, () => OnDecision(resolveResult.TargetAmount));
    }

    private SelectionResult<List<Entity>> OnClosed()
    {
        _currentSelected.Clear();
        return new SelectionCancel<List<Entity>>();
    }
    private SelectionResult<List<Entity>> OnSelected(Entity entity, bool isShortage, TargetResolveResult resolveResult)
    {
        if (!isShortage)
        {
            if(!_currentSelected.Remove(entity))
                _currentSelected.Add(entity);
        }
        SetCommands(resolveResult);
        Render();
        return new SelectionContinue<List<Entity>>();
    }
    private SelectionSuccess<List<Entity>> OnDecision(int targetAmount)
    {
        if (_currentSelected.Count == targetAmount)
            return new SelectionSuccess<List<Entity>>(_currentSelected);
        else if (_currentSelected.Count < targetAmount)
            _screen.Set(ScreenLayer.Content, "選択可能数を満たしていません");
        else if(_currentSelected.Count > targetAmount)
            _screen.Set(ScreenLayer.Content, "選択可能数を超えています");
        return new SelectionSuccess<List<Entity>>([]);
    }

    public void HandleInput(int num, out SelectionResult<List<Entity>>? result)
    {
        result = null;
        if (num < 0 || num > _selectionCommands.Count)
        {
            _screen.Set(ScreenLayer.Content, "選択肢の範囲外です");
            _screen.RefreshUntil();
            return;
        }
        result = _selectionCommands[num].Execute.Invoke();
        if(result is SelectionContinue<List<Entity>>)
        {
            result = null;
        }
        else if(result is SelectionSuccess<List<Entity>> success && success.Value.Count == 0)
        {
            result = null;
        }
    }
    private void Render()
    {
        StringBuilder sb = new();
        foreach (var command in _selectionCommands)
        {
            sb.AppendLine(command.Value.Text.ToString());
        }
        _screen.Set(ScreenLayer.InputArea, sb.ToString());
        _screen.RefreshUntil();
    }
    public SelectionResult<List<Entity>> SelectingTargets(TargetResolveResult targetResolveResult)
    {
        if(targetResolveResult.TargetCandidates.Count <= targetResolveResult.TargetAmount)
        {
            return new SelectionSuccess<List<Entity>>(targetResolveResult.TargetCandidates);
        }
        return GetSelectedTargetList(targetResolveResult.TargetCandidates, targetResolveResult.TargetAmount);
    }
    private SelectionResult<List<Entity>> GetSelectedTargetList(List<Entity> targetCandidates, int targetAmount)
    {
        List<Entity> currentSelected = new List<Entity>();
        _screen.Clear(ScreenLayer.Content);
        while (true)
        {
            //_log.WriteLog(SelectionText(targetCandidates, currentSelected));
            _screen.Set(ScreenLayer.InputArea, SelectionText(targetCandidates, currentSelected));
            _screen.RefreshUntil();
            string? selectNum = _input.Input();

            if(string.IsNullOrEmpty(selectNum))
            {
                var (isDone, content) = TryFinishSelection(currentSelected, targetAmount);
                if (content != null)
                {
                    _screen.Set(ScreenLayer.Content, content);
                    //_log.WriteLog(content);
                }
                if (isDone)
                {
                    return new SelectionSuccess<List<Entity>>(currentSelected);
                }
            }
            else if(int.TryParse(selectNum, out var result) && result >= 1 && result <= targetCandidates.Count)
            {
                var target = targetCandidates[result - 1];
                if(currentSelected.Contains(target))
                {
                    currentSelected.Remove(target);
                }
                else
                {
                    if(currentSelected.Count < targetAmount)
                    {
                        currentSelected.Add(target);
                    }
                    else
                    {
                        _screen.Set(ScreenLayer.Content, "選択可能数を超えます");
                        //_log.WriteLog("選択可能数を超えます");
                    }
                }
            }
            else if (result == 0)
            {
                return new SelectionCancel<List<Entity>>();
            }
            else
            {
                _screen.Set(ScreenLayer.Content, "入力が正しくありません");
                //_log.WriteLog("入力が正しくありません");
            }            
        }
    }
    private (bool, string?) TryFinishSelection(List<Entity> currentSelected, int targetAmount)
    {
        if(currentSelected.Count == 0)
        {
            return (false, "ターゲットの番号を入力してください");
        }
        else if(currentSelected.Count < targetAmount)
        {
            int rest = targetAmount - currentSelected.Count;
            return (false, $"選択可能数 残り:{rest}");
        }
        else if(currentSelected.Count == targetAmount)
        {
            return (true, null);
        }
        else
        {
            return (false, "選択数が多すぎます");
        }
    }
    private string SelectionText(List<Entity> targetCandidates, List<Entity> currentSelecting)
    {
        string text = "[もどる:<0>]\n";
        for(int i = 0; i < targetCandidates.Count; i++)
        {
            var target = targetCandidates[i];
            bool isSelected = currentSelecting.Contains(target);
            string t = (isSelected) ? "選択中" : "未選択";
            text += $"[{i+1}:{target.Name}(HP:{target.Stat.CurrentHp}/{target.Stat.TotalHP},{t})]";
        }
        text += "\nEnterキーで確定";
        return text;
    }  
}
