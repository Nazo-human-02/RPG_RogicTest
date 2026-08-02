using System;

#region インターフェース
public interface IEnemy
{
    EnemyType EnemyType { get; set; }
}

public interface IPlayable
{

}

public interface INpc
{
    bool IsShop { get; set; }
}

public interface IObject
{

}

public interface IMovable
{
    int MoveSpeed { get; set; }
}

public interface ITalkable
{
    string? Content { get; set; }
}

public interface IEquipable
{
    Dictionary<BodyParts, Equipment> Equipments { get; set; }
}
public interface IUseable
{
    void Use(Entity user, params Entity[] target);
}

public interface IMenu
{
    MenuState CurrentMenuState { get; }
    Action<ISelectorRequest>? OpenSelector { get; set; }
    void OpenMenu(PartyController partyController);
    void HandleInput(int num);
    void Close();
    bool IsClosed { get; }

}

public interface ISelector<T>
{
    void HandleInput(int num, out SelectionResult<T>? result);
}
public interface ISelectionResult
{ }
#endregion

#region ゲームタグ用のインターフェース
public interface IAreaId { }
public interface IEnemyTableId { }
public interface IBaseStatId { }
public interface IEntityId { }
public interface IEnemyId { }
public interface IBossPartyId { }
public interface INpcId { }
public interface ICharacterId { }
public interface IDropRewardId { }
public interface IDropItemTableId { }
public interface ISkillId { }
public interface INotificationId { }
public interface ICostId { }
public interface IItemId { }
public interface IEquipmentId { }
#endregion