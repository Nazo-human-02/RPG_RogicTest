using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public record RequestOpenSelector<T>
(
    ISelector<T> Selector,
    Action SelectorOpen,
    Action<SelectionSuccess<T>> OnSuccess,
    Action<SelectionResult<T>>? OnCanceled = null,
    Action<SelectionOpenMenu<T>>? OnOpenMenu = null
) : ISelectorRequest
{
    public event Action? Closed;
    public void InvokeResult(SelectionResult<T> result)
    {
        if (result is SelectionSuccess<T> success)
        {
            OnSuccess.Invoke(success);
        }
        else if (result is SelectionOpenMenu<T> openMenu)
        {
            OnOpenMenu?.Invoke(openMenu);
            //return;
        }
        else
            OnCanceled?.Invoke(result);
        Closed?.Invoke();
    }
    public void HandleInput(int num)
    {
        Selector.HandleInput(num, out var result);
        if (result is null || result is SelectionContinue<T>)
            return;
        InvokeResult(result);
    }
    public void OpenSelector()
    {
        SelectorOpen.Invoke();
    }
}

public interface ISelectorRequest
{
    event Action? Closed;
    void HandleInput(int num);
    void OpenSelector();
}
