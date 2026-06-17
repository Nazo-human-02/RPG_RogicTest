using System;

public static class TargetSelect
{
    private static HashSet<Entity> _currentSelected = new HashSet<Entity>();

    public static List<Entity> SetSelecting(Entity selecter, BattleSession battleSession, TargetType targetType, int targetAmount)
    {
        _currentSelected.Clear();
        List<Entity> _alltargets = GetTargetsList(selecter, battleSession, targetType);
        Dictionary<int, Entity> targetCandidates = new Dictionary<int, Entity>();
        int n = 1;
        foreach (Entity entity in _alltargets)
        {
            targetCandidates[n] = entity;
            n++;
        }
        if(targetCandidates.Count <= targetAmount)
        {
            return targetCandidates.Values.ToList();
        }
        GetSelectedTargetList(targetCandidates, targetAmount);
        return _currentSelected.ToList();
    }

    private static void GetSelectedTargetList(Dictionary<int, Entity> targetDict, int targetAmount)
    {
        bool isSelectionDone = false;
        while(!isSelectionDone)
        {
            SelectionText(targetDict);

            Entity? target = GetSelectedTarget(targetDict, targetAmount);
            if(target == null)
            {
                int rest = targetAmount - _currentSelected.Count;
                if(rest == 0)
                {
                    isSelectionDone = true;
                }
                else if (_currentSelected.Count == 0)
                {
                    LogWrite.Log("ターゲットの番号を入力してください");
                }
                else if (rest > 0)
                {
                    LogWrite.Log($"選択可能数 残り:{rest}");
                }
                else
                {
                    LogWrite.Log("選択数が多すぎます");
                }
                
            }
            else if(_currentSelected.Contains(target))
            {
                _currentSelected.Remove(target);
            }
            else
            {
                if(_currentSelected.Count < targetAmount)
                {
                    _currentSelected.Add(target);
                    int rest = targetAmount - _currentSelected.Count;
                    LogWrite.Log($"選択可能数 残り:{rest}");
                }
                else
                {
                    LogWrite.Log("選択可能数を超えます");
                }
            }
            
        }
    }

    private static Entity? GetSelectedTarget(Dictionary<int, Entity> targetDict, int targetAmount)
    {
        string? selectNum = Console.ReadLine();
        if(string.IsNullOrEmpty(selectNum))
        {
            return null;
        }
        if(!int.TryParse(selectNum, out int num)||!targetDict.TryGetValue(num, out var entity))
        {
            LogWrite.Log("入力が正しくありません");
            return GetSelectedTarget(targetDict, targetAmount);
            
        }
        return entity;
    }

    private static void SelectionText(Dictionary<int, Entity> targetDict)
    {
        string text = "";
        foreach(var target in targetDict)
        {
            bool isSelected = _currentSelected.Contains(target.Value);
            string t = (isSelected) ? "選択中" : "未選択";
            text += $"[{target.Key.ToString()}:{target.Value.Name}(HP:{target.Value.Stat.CurrentHp}/{target.Value.Stat.MaxHp},{t})]";
        }
        text += "\nEnterキーで確定";
        LogWrite.Log(text);
    }

    private static List<Entity> GetTargetsList(Entity selecter, BattleSession battleSession, TargetType targetType)
    {
        if(selecter is EnemyCharacter)
        {
            return targetType switch
            { 
                TargetType.Enemy => battleSession.GetAliveParty().Cast<Entity>().ToList(),
                TargetType.Ally => battleSession.GetAliveEnemy().Cast<Entity>().ToList(),
                TargetType.Self => new List<Entity>() { selecter },
                TargetType.All => battleSession.GetAliveEnemy().Cast<Entity>().Concat(battleSession.GetAliveParty()).ToList(),
                _ => new List<Entity>() { selecter },
            };
        }
        else
        {
            return targetType switch
            {
                TargetType.Enemy => battleSession.GetAliveEnemy().Cast<Entity>().ToList(),
                TargetType.Ally => battleSession.GetAliveParty().Cast<Entity>().ToList(),
                TargetType.Self => new List<Entity>() { selecter },
                TargetType.All => battleSession.GetAliveEnemy().Cast<Entity>().Concat(battleSession.GetAliveParty()).ToList(),
                _ => new List<Entity>() { selecter },
            };
        }
    }
}
