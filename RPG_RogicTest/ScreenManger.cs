using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ScreenManager(MenuManager menuManager, SelectorManager selectorManager)
{
    private readonly MenuManager _menuManager = menuManager;
    private readonly SelectorManager _selectorManager = selectorManager;
    private bool _isOpenSelector => _selectorManager.IsSelecting;
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
        if(_isOpenSelector)
        {
            _selectorManager.HandleInput(num);
        }
        else
        {
            _menuManager.HandleInput(num);
        }
    }
}