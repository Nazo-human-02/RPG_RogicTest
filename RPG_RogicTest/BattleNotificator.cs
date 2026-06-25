using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class BattleNotification
{
    private static BattleSession? _battleSession;
    private static BattleManager? _battleManager;
    private static List<Entity> _allEntities = new();

    public static void Initialize(BattleSession battleSession, BattleManager battleManager)
    {
        _battleSession = battleSession;
        _battleManager = battleManager;
        _allEntities = _battleSession.GetAliveEnemy().Cast<Entity>().Concat(_battleSession.GetAliveParty()).ToList();
    }

    public static void UpDateEntities()
    {
        if (_battleSession == null)
            return;
        _allEntities = _battleSession.GetAliveEnemy().Cast<Entity>().Concat(_battleSession.GetAliveParty()).ToList();
    }
    public static void TriggerPhase(Phase phase, ActionUnit? actionUnit, Entity? currentTarget)
    {
        foreach (Entity entity in _allEntities)
        {
            if (entity.Stat.IsDead && phase != Phase.OnDeath)
            {
                continue;
            }
            if (actionUnit != null && actionUnit.ActionGuid.IsProcessed(phase, entity))
            {
                continue;
            }
            if (_battleManager == null)
            {
                continue;
            }
            entity.Notifications.ExecuteNotifies(_battleManager, phase, actionUnit, currentTarget);
        }
    }
}
