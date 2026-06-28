using System;
using System.Net.Http.Headers;

public class TargetSelect(ILogProvider log, IInputProvider input)
{
    private readonly ILogProvider _log = log;
    private readonly IInputProvider _input = input;

    public SelectionResult<List<Entity>> SelectingTargets(Entity selecter, BattleSession battleSession, TargetType targetType, int targetAmount)
    {
        List<Entity> _alltargets = GetTargetsList(selecter, battleSession, targetType);
        if(_alltargets.Count <= targetAmount)
        {
            return new SelectionSuccess<List<Entity>>(_alltargets);
        }
        return GetSelectedTargetList(_alltargets, targetAmount);
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
        while (true)
        {
            _log.Log(SelectionText(targetCandidates, currentSelected));
            string? selectNum = _input.Input();

            if(string.IsNullOrEmpty(selectNum))
            {
                var (isDone, content) = TryFinishSelection(currentSelected, targetAmount);
                if (content != null)
                {
                    _log.Log(content);
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
                        _log.Log("選択可能数を超えます");
                    }
                }
            }
            else if (result == 0)
            {
                return new SelectionCancel<List<Entity>>();
            }
            else
            {
                _log.Log("入力が正しくありません");
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

    private List<Entity> GetTargetsList(Entity selecter, BattleSession battleSession, TargetType targetType)
    {
        bool isEnemy = selecter is EnemyCharacter;
        return targetType switch
        { 
            TargetType.Enemy => (isEnemy) ? battleSession.GetAliveParty().Cast<Entity>().ToList() : battleSession.GetAliveEnemy().Cast<Entity>().ToList(),
            TargetType.Ally => (isEnemy) ? battleSession.GetAliveEnemy().Cast<Entity>().ToList() : battleSession.GetAliveParty().Cast<Entity>().ToList(),
            TargetType.Self => new List<Entity>() { selecter },
            TargetType.All => battleSession.GetAliveEnemy().Cast<Entity>().Concat(battleSession.GetAliveParty()).ToList(),
            _ => new List<Entity>() { selecter },
        };
    }
}
