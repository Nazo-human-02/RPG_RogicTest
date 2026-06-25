using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class ItemBase(string name, GameId<IItemId> itemID, ItemCategory itemCategory)
{
    public string Name { get; init; } = name;
    public GameId<IItemId> ItemID { get; init; } = itemID;
    public ItemCategory Category { get; init; } = itemCategory;

}

