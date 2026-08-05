using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public class RouteSelector(ILogProvider logProvider, IInputProvider inputProvider, IScreenProvider screenProvider)
    : ISelector<RouteData>
{
    //private readonly ILogProvider _log = logProvider;
    //private readonly IInputProvider _input = inputProvider; //外部入力にできたら消す
    private readonly IScreenProvider _screen = screenProvider;

    private readonly Dictionary<DirectionType, int> _directions = new()
    {[DirectionType.Left] = 1, [DirectionType.Center] = 2, [DirectionType.Right] = 3 };
    private Dictionary<int, SelectionCommand<RouteData>> _selectionCommands = new();
    public void Open(IReadOnlyList<RouteData> routeDatas)
    {
        SetCommandsDict(routeDatas);
        Render();
    }
    public void HandleInput(int num, out SelectionResult<RouteData>? result)
    {
        if(!_selectionCommands.TryGetValue(num, out var command))
        {
            _screen.Set(ScreenLayer.ErrorArea, "選択肢の範囲外です");
            _screen.RefreshUntil();
            result = null;
            return;
        }
        result = command.Execute();
        if (result is not SelectionContinue<RouteData>)
            return;
        else
        {
            _screen.Set(ScreenLayer.ErrorArea, "選択肢の範囲外です");
            _screen.RefreshUntil();
            result = null;
        }
    }
    private void SetCommandsDict(IReadOnlyList<RouteData> routeDatas)
    {
        _selectionCommands.Clear();
        foreach(var direction in _directions)
        {
            var routeData = GetDirectionRoute(routeDatas, direction.Key);
            string direct = GetDirectionText(direction.Key);
            string text = (routeData == null) ? "[-----]" : $"[{direct}に進む<{direction.Value}>]";
            Func<SelectionResult<RouteData>> action = (routeData == null) ? 
                (() => OnContinue()) : (() => OnSelect(routeData));
            _selectionCommands[direction.Value] = new(text, direction.Value, action);
        }
        _selectionCommands[0] = new("[メニュー<0>]", 0, () => new SelectionOpenMenu<RouteData>(MenuContext.Dungeon));
    }
    private void Render()
    {
        StringBuilder sb = new();
        foreach(var command in _selectionCommands.Values)
        {
            sb.Append(command.Text);
        }
        _screen.RefreshInput(sb.ToString());
    }
    private SelectionSuccess<RouteData> OnSelect(RouteData routeData)
    {
        _screen.Set(ScreenLayer.Content, $"{GetDirectionText(routeData.DirectionType)}に進んだ");
        _screen.RefreshUntil();
        return new SelectionSuccess<RouteData>(routeData);
    }
    private SelectionContinue<RouteData> OnContinue()
    {
        return new SelectionContinue<RouteData>();
    }
    private static RouteData? GetDirectionRoute(IReadOnlyList<RouteData> routeDatas, DirectionType directionType)
    {
        return routeDatas.FirstOrDefault(data => data.DirectionType == directionType);
    }
    private static string GetDirectionText(DirectionType directionType)
    {
        return (directionType) switch
        {
            DirectionType.Left => "左",
            DirectionType.Center => "正面",
            DirectionType.Right => "右",
            _ => throw new InvalidOperationException("想定外の方向です")
        };
    }
}
