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

#endregion

#region ゲームタグ用のインターフェース
public interface IAreaId { }
public interface IBaseStatId { }
public interface IEntityId { }
public interface IEnemyId { }
public interface INpcId { }
public interface ICharacterId { }
public interface IDropRewardId { }
public interface ISkillId { }
public interface INotificationId { }
public interface ICostId { }
public interface IItemId { }
public interface IEquipmentId { }
#endregion