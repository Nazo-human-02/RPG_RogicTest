using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleRewardProcessor(IRandomProvider randomProvider)
{
    private readonly IRandomProvider _random = randomProvider;
    public BattleResultConfig ProcessReward(IReadOnlyList<EnemyCharacter> enemies, PartyController party, FieldContext fieldContext)
    {
        int totalGold = 0;
        int totalExp = 0;
        List<DropItemData> droptables = new();
        RewardModifier totalMod = TotalRewardModifier(party);
        foreach(var enemy in enemies)
        {
            totalGold += enemy.DropData.Gold;
            totalExp += enemy.DropData.Exp;
            var dropTable = DropItemTableMasterData.GetDropItemTable(enemy.DropData.DropTableId);
            droptables.AddRange(dropTable);
        }
        int finalGold = DropGoldGenerator(totalGold, totalMod);
        int finalExp = DropExpGenerator(totalExp, totalMod);
        List<DropItem> finalDropItems = DropItemGenerator(droptables, totalMod);
        BattleResultConfig resultReward = new(finalExp, finalGold, finalDropItems);
        return resultReward;
    }
    private RewardModifier TotalRewardModifier(PartyController party)
    {
        RewardModifier totalMod = new();
        foreach (RewardModifytype modifytype in Enum.GetValues<RewardModifytype>())
        {
            float total = 1.0f;
            foreach (var member in party.PartyMember)
            {
                total += member.Stat.RewardModifier.GetRewardMod(modifytype) - 1f;
            }
            totalMod.SetRewardModifier(modifytype, total);
        }
        return totalMod;
    }
    private List<DropItem> DropItemGenerator(List<DropItemData> dropItemDatas, RewardModifier rewardModifier)
    {
        List<DropItem> dropItems = new();
        foreach(var dropItemData in dropItemDatas)
        {
            float random = _random.GetRandomFloat();
            float totalRate =  //基礎ドロ率 * 全体ドロ率補正 * 特定レアリティドロ率補正
                dropItemData.DropRate * rewardModifier.AllDropRateMod * GetItemMod(dropItemData.Rarity, rewardModifier);
            totalRate = Math.Min(1.0f, totalRate);
            if(random < totalRate)
            {
                DropItem dropItem = new(dropItemData.ItemID, dropItemData.Amount, dropItemData.Rarity);
                dropItems.Add(dropItem);
            }
        }
        return dropItems;
    }
    private int DropGoldGenerator(int baseGold, RewardModifier rewardModifier) 
    {
        return (int)(baseGold * rewardModifier.GoldMod);
    }
    private int DropExpGenerator(int baseExp, RewardModifier rewardModifier)
    {
        return (int)(baseExp * rewardModifier.ExpMod);
    }
    private AreaData? GetAreaData(FieldContext fieldContext)
    {
        FloorData? floorData = (fieldContext.FieldType is FieldType.Dungeon) ?
            DungeonFloorMasterData.GetFloorData(fieldContext.FloorNumber) : null;
        AreaData? areaData = (floorData is not null) ? AreaMasterData.GetAreaData(floorData.AreaID) : null;
        return areaData;
        //AreaDataはエリア専用のドロップテーブルを取り出すため(未実装)
    }
    private float GetItemMod(ItemRarity itemRarity, RewardModifier rewardModifier)
    {
        return itemRarity switch
        {
            ItemRarity.Common => rewardModifier.CommonDropRateMod,
            ItemRarity.Rare => rewardModifier.RareDropRateMod,
            ItemRarity.SuperRare => rewardModifier.SuperRareDropRateMod,
            _ => 1.0f
        };
    }
}

public class RewardModifier
{
    private readonly Dictionary<RewardModifytype, float> _modifiers = new(); 
    //倍率は基本小数表記
    public float GoldMod => GetRewardMod(RewardModifytype.Gold);
    public float ExpMod => GetRewardMod(RewardModifytype.Exp);
    public float AllDropRateMod => GetRewardMod(RewardModifytype.AllDrop);
    public float CommonDropRateMod => GetRewardMod(RewardModifytype.CommonDrop);
    public float RareDropRateMod => GetRewardMod(RewardModifytype.RareDrop);
    public float SuperRareDropRateMod => GetRewardMod(RewardModifytype.SuperRareDrop);
    public float GetRewardMod(RewardModifytype type)
    {
        return _modifiers.GetValueOrDefault(type, 1.0f);
    }

    public void SetRewardModifier(RewardModifytype type, float modifyRate)
    {
        _modifiers[type] = modifyRate;
    }

    public void ResetRewardModifier(RewardModifytype type = default)
    {
        if (type == default)
        {
            _modifiers.Clear(); // 全部まとめて初期化（デフォルトの1.0fに戻る）
        }
        else
        {
            _modifiers.Remove(type); // 指定された種類だけ初期化
        }
    }
}

public enum RewardModifytype { Gold, Exp, AllDrop, CommonDrop, RareDrop, SuperRareDrop};