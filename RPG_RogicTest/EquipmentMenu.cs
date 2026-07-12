using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EquipmentMenu(EquipmentSelector equipmentSelector)
{
    private readonly EquipmentSelector _equipmentSelector = equipmentSelector;

    private List<Entity> _displayMember = new();
    private (Entity, List<EquipmentSet>) Initialize(PartyController partyController, IScreenProvider screen, int pageNum = 1)
    {
        _displayMember = partyController.PartyMember.Cast<Entity>().ToList();

        if (pageNum > _displayMember.Count)
            return (_displayMember[0], _displayMember[0].EquipmentController.Equipments.Values.ToList());

        StringBuilder sb = new();

        var member = _displayMember[pageNum - 1];

        sb.AppendLine("=装備=");
        sb.AppendLine($"[{member.Name}]");

        int num = 1;
        List<EquipmentSet> equipments = new();
        foreach(var equipment in member.EquipmentController.Equipments)
        {
            sb.AppendLine($"-<{num}>-[{TextMasterData.GetBodyPartsText(equipment.Key)}" +
                $"({TextMasterData.GetEquipmentTypeText(equipment.Value.Equipment.EquipmentInfo.EquipmentType)})|" +
                $"{equipment.Value.Equipment.EquipmentInfo.Name}]");
            equipments.Add(equipment.Value);
            num++;
        }
        sb.AppendLine("<0>でもどる|番号を入力し装備詳細");

        screen.Set(ScreenLayer.InputArea, sb.ToString());
        screen.Clear(ScreenLayer.Content);
        screen.RefreshUntil();

        return (member, equipments);
    }

    private void ChangeDisplayPage() { }

    public void ValidEquipmentMenu(PartyController partyController,
        IScreenProvider screen, IInputProvider input, IRandomProvider random)
    {
        var currentDisplay = Initialize(partyController, screen);
        int currentPage = 1;

        while(true)
        {
            string? inputText = input.Input();

            if (string.IsNullOrEmpty(inputText) || !int.TryParse(inputText, out int inputNum)
                || (inputNum < 0 || inputNum > currentDisplay.Item2.Count))
            {
                screen.Set(ScreenLayer.Content, "入力が正しくありません");
            }
            else if (inputNum == 0)
            {
                break;
            }
            else
            {
                var equipment = currentDisplay.Item2[inputNum - 1];
                string text = "<0>|もどる <1>|装備を変更する";

                screen.Set(ScreenLayer.Content, $"{equipment.Equipment.EquipmentInfo.Name}|装備の説明");
                screen.Set(ScreenLayer.InputArea, text);
                screen.RefreshUntil();

                while(true)
                {
                    string? inputT = input.Input();

                    if (string.IsNullOrEmpty(inputT) || !int.TryParse(inputT, out int inputN)
                        || (inputN != 0 && inputN != 1))
                    {
                        screen.Set(ScreenLayer.Content, "入力が正しくありません");
                    }
                    else if (inputN == 0)
                    {
                        currentDisplay = Initialize(partyController, screen, currentPage);
                        break;
                    }
                    else
                    {
                        var equipmentController = currentDisplay.Item1.EquipmentController;
                        var result = _equipmentSelector.SelectingEquipment(partyController, 
                            equipment.Equipment.EquipmentInfo.BodyParts, screen, input);
                        if(result is not SelectionSuccess<EquipmentSet> success)
                        {
                            currentDisplay = Initialize(partyController, screen, currentPage);
                            break;
                        }
                        if(success.Value.Equiped)
                        {
                            screen.Set
                                (ScreenLayer.Content, $"既に{success.Value.Equipment.EquipmentInfo.Name}は装備されています");
                            currentDisplay = Initialize(partyController, screen);
                            break;
                        }
                        bool equipSuccess = equipmentController.TryEquip(success.Value, out var previousEquipment);
                        if (!equipSuccess || previousEquipment == null)
                        {
                            screen.Set(ScreenLayer.Content, "装備の変更に失敗しました");
                            currentDisplay = Initialize(partyController, screen);
                            break;
                        }

                        screen.Set(ScreenLayer.Content, $"装備を変更しました。" +
                            $"[{previousEquipment.Equipment.EquipmentInfo.Name}-->" +
                            $"{success.Value.Equipment.EquipmentInfo.Name}]");
                        currentDisplay = Initialize(partyController, screen);
                        break;
                    }
                    screen.RefreshUntil();
                }
            }
            screen.RefreshUntil();
        }
    }
}