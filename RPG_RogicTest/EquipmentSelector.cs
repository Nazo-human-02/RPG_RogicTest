using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EquipmentSelector(IScreenProvider screenProvider) : ISelector<EquipmentSet>
{
    private IScreenProvider _screen = screenProvider;

    private bool _isClosed = true;

    private Dictionary<int, SelectionCommand<EquipmentSet>> _commands = new();
    public void HandleInput(int num, out SelectionResult<EquipmentSet>? result)
    {
        result = null;
        if (num < 0 || num > _commands.Count)
        {
            _screen.Set(ScreenLayer.Content, "選択肢の範囲外です");
            _screen.RefreshUntil();
            return;
        }
        result = _commands[num].Execute.Invoke();
        return;
    }

    private void Inititalize(PartyController partyController, BodyParts bodyParts)
    {
        _commands.Clear();
        int num = 1;
        foreach (var set in partyController.Inventory.EquipmentInventory)
        {
            if (set.Equipment.EquipmentInfo.BodyParts != bodyParts)
                continue;
            string equiped = (set.Equiped) ? "(装備中)" : "";
            string text = $"-<{num}>- 部位:{TextMasterData.GetBodyPartsText(set.Equipment.EquipmentInfo.BodyParts)}" +
            $"({TextMasterData.GetEquipmentTypeText(set.Equipment.EquipmentInfo.EquipmentType)})" +
            $"[{set.Equipment.EquipmentInfo.Name}{equiped}]";
            _commands[num] = new(text, num, () => MainCommand(set)); 
            num++;
        }
        _commands[0] = new("<0>でもどる|番号を入力し装備を選択", 0, () => Close());
    }
    private SelectionResult<EquipmentSet> MainCommand(EquipmentSet equipmentSet)
    {
        return new SelectionSuccess<EquipmentSet>(equipmentSet);
    }
    public void OpenSelector(PartyController partyController, BodyParts bodyParts)
    {
        Inititalize(partyController, bodyParts);
        Render();
        _isClosed = false;
    }
    public SelectionCancel<EquipmentSet> Close()
    {
        _isClosed = true;
        return new SelectionCancel<EquipmentSet>();
    }
    private void Render()
    {
        StringBuilder sb = new();

        sb.AppendLine("[装備インベントリ]");
        foreach (var command in _commands)
        {
            sb.AppendLine(command.Value.Text.ToString());
        }

        _screen.Set(ScreenLayer.InputArea, sb.ToString());
        _screen.RefreshUntil();
    }
}

public record SelectionCommand<T>
(
    string Text,
    int Number,
    Func<SelectionResult<T>> Execute
);