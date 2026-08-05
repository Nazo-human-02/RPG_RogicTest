using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EquipmentMenu(EquipmentSelector equipmentSelector, IInputProvider inputProvider, IScreenProvider screenProvider) 
    : MemberMenuBase(inputProvider, screenProvider), IMenu
{
    private readonly EquipmentSelector _equipmentSelector = equipmentSelector;
    public MenuState CurrentMenuState => _currentMenuState;
    private MenuState _currentMenuState;
    public bool IsClosed => _isClosed;
    private bool _isClosed = true;
    private Action? _subCommand;
    private BodyParts _selectedPart = BodyParts.Head;
    public Action<ISelectorRequest>? OpenSelector { get; set; } = null;
    public Action? OnClosed { get; set; } = null;
    public void Close()
    { 
        _isClosed = true;
        OnClosed?.Invoke();
    }
    public void HandleInput(int num) 
    {
        if(IsClosed)
        {
            return;
        }
        switch (_currentMenuState)
        {
            case MenuState.MainMenu:
                MainMenuCommands(num);
                break;
            case MenuState.Detail:
                SubMenuCommands(num);
                break;
        }
    }
    private void MainMenuCommands(int num)
    {
        if (num < 0 || num > _commands.Count)
        {
            SelectErrorText(-2);
            return;
        }
        _commands[num].Execute.Invoke();
        //RenderDetail(_currentMember.EquipmentController.Equipments[_selectedPart].Equipment);
        //Render(_currentMember);
    }
    public void OpenMenu(PartyController partyController)
    {
        _isClosed = false;
        ValidMenu(partyController);
    }
    public override void ValidMenu(PartyController partyController)
    {
        _currentMenuState = MenuState.MainMenu;
        Initialize(partyController);
        CommandsInitialize(partyController);
        Render(_currentMember);
    }
    private void CommandsInitialize(PartyController partyController)
    {
        _commands.Clear();
        int n = 1;
        foreach(var equipment in _currentMember.EquipmentController.Equipments)
        {
            string text = $"-<{n}>-[{TextMasterData.GetBodyPartsText(equipment.Key)}" +
            $"({TextMasterData.GetEquipmentTypeText(equipment.Value.Equipment.EquipmentInfo.EquipmentType)})|" +
            $"{equipment.Value.Equipment.EquipmentInfo.Name}]";
            _commands[n] = new(text, n, () => ValidEquipmentDetail(equipment.Key, partyController));
            n++;
        }
        int i = 1;
        foreach(Entity member in _displayMembers)
        {
            string text = $"<{n}>|{member.Name}";
            _commands[n] = new(text, n, () => ChangeDisplayPage(i));
            n++;
            i++;
        }
        _commands[0] = new("<0>|もどる", 0, () => Close());
    }
    private void ValidEquipmentDetail(BodyParts bodyParts, PartyController partyController)
    {
        _currentMenuState = MenuState.Detail;
        _selectedPart = bodyParts;
        var equipment = _currentMember.EquipmentController.Equipments[bodyParts];
        _subCommand = () => OpenEquipSelector(partyController, bodyParts);
        RenderDetail(equipment.Equipment);
    }
    
    private void SubMenuCommands(int num)
    {
        if (num == 0)
        {
            _currentMenuState = MenuState.MainMenu;
            Render(_currentMember);
        }
        else if (num == 1)
        {
            _subCommand?.Invoke();
            //RenderDetail(_currentMember.EquipmentController.Equipments[_selectedPart].Equipment);
        }
        else
            SelectErrorText(-2);
    }
    private void RenderDetail(Equipment equipment)
    {
        _screen.Set(ScreenLayer.Content, $"{equipment.EquipmentInfo.Name}|装備の説明");
        _screen.Set(ScreenLayer.InputArea, "<0>|もどる <1>|装備を変更する");
        _screen.RefreshUntil();
    }
    private void ChangeDisplayPage(int num)
    {
        if(ChangePage(num))
        {
            Render(_currentMember);
        }
    }
    protected override void Render(Entity member)
    {
        StringBuilder sb = new();

        sb.AppendLine("==装備==");
        sb.AppendLine($"表示中:[{_currentMember.Name}]");
        foreach(var command in _commands)
        {
            sb.AppendLine(command.Value.Text);
        }

        sb.AppendLine("番号入力で装備詳細");
        sb.AppendLine(TextMasterData.GetWindowSmallLine());

        _screen.RefreshInput(sb.ToString());
    }
    private void OpenEquipSelector(PartyController partyController, BodyParts bodyParts)
    {
        _screen.Clear(ScreenLayer.Content);
        //_isClosed = true;
        RequestOpenSelector<EquipmentSet> openSelector =
            new(_equipmentSelector,
            () => _equipmentSelector.OpenSelector(partyController, bodyParts), 
            OnSuccess:(success) => OnTryEquip(success, partyController, bodyParts),
            OnCanceled:(cancel) => OnCanceled(cancel, partyController, bodyParts));
        OpenSelector?.Invoke(openSelector);
        //_currentMenuState = MenuState.Selection;
        //_equipmentSelector.OpenSelector(partyController, bodyParts);
    }
    private void OnCanceled(SelectionResult<EquipmentSet> cancel, PartyController party, BodyParts bodyParts)
    {
        //_isClosed = false;
        //Console.WriteLine("_isClosed = false");
        ValidEquipmentDetail(bodyParts, party);
        //_currentMenuState = MenuState.Detail;
    }
    private void OnTryEquip(SelectionSuccess<EquipmentSet> success, PartyController partyController, BodyParts bodyParts)
    {
        //_isClosed = false;
        ValidEquipmentDetail(bodyParts, partyController);
        if (success.Value.Equiped && success.Value.Equipper != null)
        {
            _screen.Append(ScreenLayer.Content, $"既に{success.Value.Equipment.EquipmentInfo.Name}" +
                $"は{success.Value.Equipper.Name}に装備されています");
            _screen.RefreshUntil();
            return;
        }
        bool equipSuccess = _currentMember.EquipmentController.TryEquip(success.Value, out EquipmentSet previousEquipment);
        if (!equipSuccess)
        {
            _screen.Set(ScreenLayer.ErrorArea, "装備の変更に失敗しました");
        }
        else
        {
            _screen.Append(ScreenLayer.Content, $"装備を変更しました。" +
            $"[{previousEquipment?.Equipment.EquipmentInfo.Name}-->" +
            $"{success.Value.Equipment.EquipmentInfo.Name}]");
        }
        CommandsInitialize(partyController);
        _screen.RefreshUntil();
    }
}
