using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EquipmentController(Entity owner)
{
    public IReadOnlyDictionary<BodyParts, EquipmentSet> Equipments => _equipments;
    private Dictionary<BodyParts, EquipmentSet> _equipments = new();

    private readonly Entity _owner = owner;

    public void Initialize()
    {
        _equipments.Clear();
        foreach (BodyParts part in Enum.GetValues(typeof(BodyParts)))
        {
            if (part == BodyParts.Blank) continue;
            _equipments[part] = EquipmentSet.Blank(part);
        }
    }

    public bool TryEquip(EquipmentSet equipmentSet, out EquipmentSet previousEquipment)
    {
        if (equipmentSet.Equipment == null) throw new ArgumentNullException(nameof(equipmentSet.Equipment));

        var part = equipmentSet.Equipment.EquipmentInfo.BodyParts;
        
        if (part == BodyParts.Blank) 
            throw new ArgumentException("装備の部位が不正です。", nameof(equipmentSet));
        if (!_equipments.TryGetValue(part, out var equip) || equip.Equipment.IsCursed)
        {
            previousEquipment = null;
            return false;
        }
        _equipments[part] = equipmentSet;

        equipmentSet.Equip(_owner);
        equip.UnEquip();
        previousEquipment = equip;
        _owner.UpdateEquipmentStat();
        return true;
    }
    public string GetEquipmentsText()
        => TextMasterData.GetEquipmentsText(Equipments);

    public ModifierStat GetTotalModifier()
    {
        var totalModifier = new ModifierStat();
        foreach (var equipment in Equipments.Values)
        {
            totalModifier = ModifiableStat.GetTotalModifier(totalModifier, equipment.Equipment.ModifierStat);            
        }
        return totalModifier;
    }
    public EquipmentController Clone(Entity entity)
    {
        var clone = new EquipmentController(entity);
        clone.Initialize();
        foreach (var kvp in this.Equipments)
        {
            clone._equipments[kvp.Key] = kvp.Value.Clone(entity);
        }
        return clone;
    }
}