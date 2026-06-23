using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public class RouteSelector(ILogProvider logProvider, IInputProvider inputProvider)
{
    private readonly ILogProvider _log = logProvider;
    private readonly IInputProvider _input = inputProvider;

    public RouteData SelectingRoute(IReadOnlyList<RouteData> routeDatas)
    {
        while(true)
        {
            SelectionText(routeDatas);
            string? select = _input.Input();
            if(string.IsNullOrEmpty(select) || !int.TryParse(select, out int num))
            {
                _log.Log("進行方向に対応する数字を入力してください");
            }
            else
            {
                RouteData? routeData = GetInputNum(routeDatas, num);
                if(routeData != null)
                {
                    OnMoveText(routeData.DirectionType);
                    return routeData;
                }
                else
                {
                    _log.Log("範囲外の数値です");
                }
            }
        }
    }
    private void SelectionText(IReadOnlyList<RouteData> routeDatas)
    {
        DirectionType[] directionTypes = {DirectionType.Left, DirectionType.Center, DirectionType.Right };
        StringBuilder selectionText = new();
        foreach (DirectionType directionType in directionTypes)
        {
            RouteData? direction = GetDirectionRoute(routeDatas, directionType);
            if(direction == null)
            {
                selectionText.Append("-----");
            }
            else
            {
                (string text, int num) = GetDirectionText(direction.DirectionType);
                selectionText.Append($"{text}に進む<{num}>");
            }
        }
        _log.Log(selectionText.ToString());
    }
    private void OnMoveText(DirectionType directionType)
    {
        string direction = GetDirectionText(directionType).Item1;
        _log.Log($"{direction}に進んだ");
    }


    private static RouteData? GetDirectionRoute(IReadOnlyList<RouteData> routeDatas, DirectionType directionType)
    {
        return routeDatas.FirstOrDefault(data => data.DirectionType == directionType);
    }

    private static (string, int) GetDirectionText(DirectionType directionType)
    {
        return (directionType) switch
        {
            DirectionType.Left => ("左", 1),
            DirectionType.Center => ("正面", 2),
            DirectionType.Right => ("右", 3),
            _ => throw new InvalidOperationException("想定外の方向です")
        };
    }
    private RouteData? GetInputNum(IReadOnlyList<RouteData> routeDatas, int input)
    {
        DirectionType? type = (input) switch
        {
            1 => DirectionType.Left,
            2 => DirectionType.Center,
            3 => DirectionType.Right,
            _ => null
        };
        RouteData? routeData = (type != null) ? GetDirectionRoute(routeDatas, (DirectionType)type) : null;
        return routeData;
    }
}
