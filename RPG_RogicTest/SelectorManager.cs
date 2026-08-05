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
    public bool IsValid => IsSelecting && _isOpennig;
    public bool IsSelecting => _currentSelector is not null;
    ISelectorRequest? _currentSelector = null;
    private ISelectorRequest? _previousSelector = null;
    private bool _isOpennig = false;
    public void OpenSelector(ISelectorRequest requestOpenSelector)
    {
        _isOpennig = true;
        Console.WriteLine("isOpenning = true");
        Console.WriteLine("セレクターOpenリクエスト");
        _currentSelector = requestOpenSelector;
        _currentSelector.Closed += CloseSelector;
        _currentSelector.OpenSelector();
    }
    public void ReturnPreviousSelector()
    {
        Console.WriteLine("実行ーーーーーーーーーーーーーー");
        _currentSelector = _previousSelector;
        _previousSelector = null;
        Console.WriteLine("保存したセレクターを現在セレクターに(保存場所をnullに)");
        if (_currentSelector is not null)
        {
            OpenSelector(_currentSelector);
        }
    }
    public void HandleInput(int num)
    {
        if (!_isOpennig && IsSelecting)
        {
            Console.WriteLine("現在セレクターをnullに(handle)");
            _currentSelector = null;
            return;
        }
        else if (!IsSelecting)
        {
            return;
        }
        else if(IsSelecting)
            _currentSelector!.HandleInput(num);
    }
    public void CloseSelector()
    {
        Console.WriteLine("セレクターClose実行");
        if (IsSelecting)
        {
            _currentSelector!.Closed -= CloseSelector;
        }

        if (_previousSelector is null)
        {
            _previousSelector = _currentSelector;
            Console.WriteLine("現在セレクターを保存");
        }
        if (!_isOpennig)
        {
            _currentSelector = null;
            Console.WriteLine("現在セレクターをnullに");
        }
        _isOpennig = false;
        Console.WriteLine("isOpenning = false");
    }
}