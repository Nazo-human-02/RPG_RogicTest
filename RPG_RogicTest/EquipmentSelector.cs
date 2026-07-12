using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EquipmentSelector
{
    public SelectionResult<EquipmentSet> SelectingEquipment(PartyController partyController, BodyParts bodyParts,
        IScreenProvider screen, IInputProvider input)
    {
        StringBuilder sb = new();

        sb.AppendLine("[装備インベントリ]");
        int num = 1;
        foreach(var set in partyController.Inventory.EquipmentInventory)
        {
            Console.WriteLine("装備インベントリ通過");
            screen.Append(ScreenLayer.Content, "通過");
            if (set.Equipment.EquipmentInfo.BodyParts != bodyParts)
                continue;

            string equiped = (set.Equiped) ? "(装備中)" : "";

            sb.AppendLine($"-<{num}>- 部位:{TextMasterData.GetBodyPartsText(set.Equipment.EquipmentInfo.BodyParts)}" +
            $"({TextMasterData.GetEquipmentTypeText(set.Equipment.EquipmentInfo.EquipmentType)})" +
            $"[{set.Equipment.EquipmentInfo.Name}{equiped}]");

            num++;
        }

        sb.AppendLine("<0>でもどる|番号を入力し装備を選択");

        screen.Set(ScreenLayer.InputArea, sb.ToString());
        //screen.Clear(ScreenLayer.Content);
        screen.RefreshUntil();


        while (true)
        {
            string? inputText = input.Input();

            if (string.IsNullOrEmpty(inputText) || !int.TryParse(inputText, out int n) ||
                (n < 0 || n > partyController.Inventory.EquipmentInventory.Count))
            {
                screen.Set(ScreenLayer.Content, "入力が正しくありません");
            }
            else if (n == 0)
            {
                return new SelectionCancel<EquipmentSet>();
            }
            else
            {
                return new SelectionSuccess<EquipmentSet>(partyController.Inventory.EquipmentInventory[n - 1]);
            }
            screen.RefreshUntil();
        }
    }
}