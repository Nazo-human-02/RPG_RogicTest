using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SelectorManager
{
    public ISelectorRequest? CurrentSelector => _currentSelector;
    [MemberNotNullWhen(true, nameof(_currentSelector))]
    public bool IsSelecting => _currentSelector is not null;
    ISelectorRequest? _currentSelector = null;
    
    public void OpenSelector(ISelectorRequest requestOpenSelector)
    {
        _currentSelector = requestOpenSelector;
        _currentSelector.Closed += CloseSelector;
        _currentSelector.OpenSelector();
    }

    public void HandleInput(int num)
    {
        if (!IsSelecting)
            return;
        _currentSelector.HandleInput(num);
    }
    public void CloseSelector()
    {
        if (IsSelecting)
        {
            _currentSelector.Closed -= CloseSelector;
        }
        _currentSelector = null;
    }
}