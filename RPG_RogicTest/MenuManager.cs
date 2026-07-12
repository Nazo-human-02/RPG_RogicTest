using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class MenuManager(MenuSelector menuSelector, ProvidorContext providorContext,
    InventoryMenu inventoryViewer, SkillMenu skillMenu, EquipmentMenu equipmentMenu)
{
    private readonly MenuSelector _menuSelector = menuSelector;
    private readonly ProvidorContext _providerContext = providorContext;

    //メニューコマンドオプション
    private readonly InventoryMenu _inventoryViewer = inventoryViewer;
    private readonly SkillMenu _skillMenu = skillMenu;
    private readonly EquipmentMenu _equipmentMenu = equipmentMenu;

    public void OpenMenu(PartyController partyController, ConditionContext conditionContext)
    {
        while (true)
        {
            SelectionResult<MenuType> option = _menuSelector.MenuOptionSelect();
            if (option is not SelectionSuccess<MenuType> success)
            {
                return;
            }
            switch(success.Value)
            {
                case MenuType.Inventory:
                    _inventoryViewer.ValidInventoryView(partyController.Inventory, conditionContext, 
                        _providerContext.ScreenProvider, _providerContext.InputProvider, _providerContext.RandomProvider);
                    break;
                case MenuType.Skill:
                    _skillMenu.ValidSkillMenu(partyController, conditionContext, 
                        _providerContext.ScreenProvider, _providerContext.InputProvider, _providerContext.RandomProvider);
                    break;
                case MenuType.Equipment:
                    _equipmentMenu.ValidEquipmentMenu(partyController,
                        _providerContext.ScreenProvider, _providerContext.InputProvider, _providerContext.RandomProvider);
                    break;
                //残りメンバーのステータスを表示するものとセーブを行うもの
                default:
                    Console.WriteLine("未設定");
                    break;
            }
        }
    }
}