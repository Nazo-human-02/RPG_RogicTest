using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScreenManager(MenuManager menuManager, SelectorManager selectorManager)
{
    private readonly MenuManager _menuManager = menuManager;
    private readonly SelectorManager _selectorManager = selectorManager;
    private bool IsOpenSelector => _selectorManager.IsValid;
    private bool IsOpenMenu => _menuManager.IsOpenMenu;
    public bool ValidHandleInput => IsOpenSelector || IsOpenMenu;
    public void Initialize()
    {
        _menuManager.OnReturnSelector = _selectorManager.ReturnPreviousSelector;
    }
    public void OpenMenu(PartyController partyController, ConditionContext conditionContext)
    {
        _menuManager.OpenMenuSelector(partyController, conditionContext, RequestOpenSelector);
    }
    public void RequestOpenSelector(ISelectorRequest selectorRequest)
    {
        _selectorManager.OpenSelector(selectorRequest);
    }

    public void HandleInput(int num)
    {
        if(IsOpenSelector)
        {
            _selectorManager.HandleInput(num);
        }
        else if(IsOpenMenu)
        {
            _menuManager.HandleInput(num);
        }
    }
}