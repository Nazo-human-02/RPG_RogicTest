using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class MenuManager(MenuSelector menuSelector, ProvidorContext providorContext,
    StatusMenu statusMenu, InventoryMenu inventoryViewer, SkillMenu skillMenu, EquipmentMenu equipmentMenu)
{
    //メニューコマンドオプション
    private readonly Dictionary<MenuType, IMenu> _menuOption = new()
    {
        [MenuType.Status] = statusMenu,
        [MenuType.Inventory] = inventoryViewer,
        [MenuType.Equipment] = equipmentMenu,
        [MenuType.Skill] = skillMenu
    };
    private readonly List<IUpdateCondition> _conditionUpdate = new() { inventoryViewer, skillMenu };

    private readonly MenuSelector _menuSelector = menuSelector;
    private IMenu? _currentMenu = null;
    public void HandleInput(int num)
    {
        _currentMenu?.HandleInput(num);
        if(_currentMenu?.IsClosed == true)
        {
            _currentMenu = null;
        }
    }
    public void OpenMenuSelector(PartyController partyController, ConditionContext conditionContext, 
        Action<ISelectorRequest> selectorRequest)
    {
        Action<SelectionSuccess<MenuType>> onSuccess = (menuType) =>
        OpenMenu(partyController, conditionContext, selectorRequest, menuType.Value);
        RequestOpenSelector<MenuType> request = new(_menuSelector, () => _menuSelector.Open(), onSuccess);
        selectorRequest.Invoke(request);
    }

    private void OpenMenu(PartyController partyController, ConditionContext conditionContext,
        Action<ISelectorRequest> selectorRequest, MenuType menuType)
    {
        foreach (var updateMenu in _conditionUpdate)
        {
            updateMenu.UpdateCondition(conditionContext);
        }

        if (_menuOption.TryGetValue(menuType, out var menu))
        {
            _currentMenu = menu;
            menu.OpenSelector = selectorRequest;
            menu.OpenMenu(partyController);
        }
        else
        {
            Console.WriteLine("未設定");
        }
    }
}


