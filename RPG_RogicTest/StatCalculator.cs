using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static public class StatCalculator
{
    static public void SetUpLevelStat(BattleStat battleStat, EntityBaseStatData baseData)
    {
        UpdateStat(battleStat, baseData);
        RecoverAll(battleStat);
    }
    static public void RecoverAll(BattleStat battleStat)
    {
        battleStat.CurrentHp = battleStat.TotalHP;
        battleStat.CurrentMp = battleStat.TotalMP;
    }
    static public void UpdateStat(BattleStat battleStat, EntityBaseStatData baseData)
    {
        int lv = battleStat.expSet.CurrentLevel;

        battleStat.MaxHp = baseData.BaseHP + (lv - 1) * 20;
        battleStat.MaxMp = baseData.BaseMP + (lv - 1) * 15;

        battleStat.baseStat.Atk = baseData.BaseAtk + (lv - 1) * 5;
        battleStat.baseStat.Def = baseData.BaseDef + (lv - 1) * 3;
    }
}
