using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleSession(IReadOnlySet<CharacterBase> party, IReadOnlyList<EnemyCharacter> enemies)
{
    public IReadOnlySet<CharacterBase> Party { get; private set; } = party;
    public IReadOnlyList<EnemyCharacter> Enemies { get; private set; } = enemies;

    public List<CharacterBase> GetAliveParty() => Party.Where(p => !p.Stat.IsDead).ToList();
    public List<EnemyCharacter> GetAliveEnemy() => Enemies.Where(e => !e.Stat.IsDead).ToList();
    public List<Entity> GetAllAliveEntity() => GetAllEntity().Where(e => !e.Stat.IsDead).ToList();
    public List<Entity> GetAllEntity() => Party.Cast<Entity>().Concat(Enemies).ToList();

    public (bool, BattleResultType) IsBattleOver() //int=1で勝利、int=2で敗北
    {
        if (GetAliveParty().Count == 0)
        {
            return (true, BattleResultType.Defeat);
        }
        else if (GetAliveEnemy().Count == 0)
        {
            return (true, BattleResultType.Victory);
        }
        //		else if(GetAliveEnemy().Count > 0 && GetAliveParty().Count > 0)
        //		{
        //			return (true, BattleResultType.Escape);
        //		}
        else
        {
            return (false, BattleResultType.ContinueBattle);
        }
    }
}