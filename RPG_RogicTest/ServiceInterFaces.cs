#region 乱数生成の差し込みインターフェース
using System.ComponentModel.DataAnnotations;
using System.Text;

public interface IRandomProvider
{
    int GetRandomInt(int min, int max);
    float GetRandomFloat();
}

public class FixedRandomProvider(int fixedValue, float fixedFloatValue) : IRandomProvider
{
    private readonly int _fixedValue = fixedValue;
    [Range(0f, 1f)] private readonly float _fixedFloatValue = fixedFloatValue; // 固定の浮動小数点値を設定

    public int GetRandomInt(int min, int max)
    {
        return _fixedValue;
    }
    public float GetRandomFloat()
    {
        return _fixedFloatValue;
    }
}

public class RandomProvider : IRandomProvider
{
    public int GetRandomInt(int min, int max)
    {
        return Random.Shared.Next(min, max);
    }
    public float GetRandomFloat()
    {
        return (float)Random.Shared.NextDouble();
    }
}
public class ReturnMaxProvider(int? intLimit = null, float? floatLimit = null) : IRandomProvider
{
    int? maxLimit = intLimit; // 最大値の上限を設定
    float? maxFloatLimit = floatLimit; // 浮動小数点の最大値の上限を設定
    public int GetRandomInt(int min, int max)
    {
        if(maxLimit.HasValue && max > maxLimit.Value)
        {
            max = maxLimit.Value;
        }
        return max - 1; // maxはexclusiveなので、max - 1を返す
    }
    public float GetRandomFloat()
    {
        if(maxFloatLimit.HasValue && maxFloatLimit.Value < 1.0f)
        {
            return maxFloatLimit.Value;
        }
        return 1.0f; // 常に最大値を返す
    }
}
public class ReturnMinProvider(int? intLowerLimit = null, float? floatLowerLimit = null) : IRandomProvider
{
    int? minLimit = intLowerLimit;
    float? minFloatLimit = floatLowerLimit;
    public int GetRandomInt(int min, int max)
    {
        if(minLimit.HasValue && min < minLimit.Value)
        {
            return minLimit.Value;
        }
        return min; // 常に最小値を返す
    }
    public float GetRandomFloat()
    {
        if(minFloatLimit.HasValue && minFloatLimit.Value > 0.0f)
        {
            return minFloatLimit.Value;
        }
        return 0.0f; // 常に最小値を返す
    }
}
#endregion

#region ログ生成の差し込みインターフェース
public interface ILogProvider
{
    void WriteLog(string message);
    void ClearLog();

}

public class LogProvider : ILogProvider
{
    public void WriteLog(string message)
    {
        Console.WriteLine(message);
    }
    public void ClearLog() { }
}

public class ConsoleLogProvider(IInputProvider inputProvider) : ILogProvider, IScreenProvider
{
    private readonly IInputProvider inputProvider = inputProvider;
    public Dictionary<ScreenLayer, StringBuilder> _screenLayers { get; } = new() 
    {
        [ScreenLayer.Header] = new StringBuilder(),
        [ScreenLayer.MainView] = new StringBuilder(),
        [ScreenLayer.SubView] = new StringBuilder(),
        [ScreenLayer.Label] = new StringBuilder()
    };
    public Queue<string> Content { get; set; } = new();
    public List<string> InputArea { get; set; } = new();
    public void WriteLog(string message)
    {
        Console.WriteLine(message);
    }

    public void ClearLog()
    {
        Console.Clear();
    }
    public void ClearAll()
    {
        foreach(var sb in _screenLayers.Values)
        {
            sb.Clear();
        }
        Content.Clear();
        InputArea.Clear();
    }
    public void Clear(ScreenLayer screenLayer)
    {
        if (_screenLayers.TryGetValue(screenLayer, out var sb))
        {
            sb.Clear();
        }
        else if(screenLayer == ScreenLayer.Content)
        {
            Content.Clear();
        }
        else if(screenLayer == ScreenLayer.InputArea)
        {
            InputArea.Clear();
        }
    }
    
    public void RefreshUntil(ScreenLayer range = ScreenLayer.None)
    {
        ClearLog();
        foreach (ScreenLayer layer in Enum.GetValues<ScreenLayer>())
        {
            if (_screenLayers.TryGetValue(layer, out var sb))
            {
                if (sb.Length > 0)
                {
                    Console.WriteLine(sb.ToString());
                }
            }
            if (layer == range && range is ScreenLayer.InputArea or ScreenLayer.None )
            {
                foreach (var input in InputArea)
                {
                    Console.WriteLine(input);
                }
            }
            if (layer == range && range is ScreenLayer.Content or ScreenLayer.None)
            {
                foreach (var content in Content)
                {
                    Console.WriteLine(content);
                }
            }

        }
    }
    public void Set(ScreenLayer screenLayer, string content)
    {
        if (_screenLayers.TryGetValue(screenLayer, out var sb))
        {
            sb.Clear();
            sb.Append(content);
        }
        else if (screenLayer == ScreenLayer.Content)
        {
            Content.Clear();
            Content.Enqueue(content);
        }
        else if (screenLayer == ScreenLayer.InputArea)
        {
            InputArea.Clear();
            InputArea.Add(content);
        }
    }
    public void Append(ScreenLayer screenLayer, string content)
    {
        if (_screenLayers.TryGetValue(screenLayer, out var sb))
        {
            sb.Append(content);
        }
        else if (screenLayer == ScreenLayer.Content)
        {
            Content.Enqueue(content);
        }
        else if (screenLayer == ScreenLayer.InputArea)
        {
            InputArea.Add(content);
        }
    }
    public void WaitForEnter()
    {
        Console.WriteLine(">>[Enter]");
        while (Console.ReadKey(true).Key != ConsoleKey.Enter)
        {
        }
    }
}
#endregion
#region スクリーン管理
public interface IScreenProvider
{
    Dictionary<ScreenLayer, StringBuilder> _screenLayers { get; }
    Queue<string> Content { get; set; }
    List<string> InputArea { get; set; }

    void RefreshUntil(ScreenLayer RefleshRange = ScreenLayer.None);

    void Set(ScreenLayer screenLayer, string content);

    void Append(ScreenLayer screenLayer, string content);
    void Clear(ScreenLayer screenLayer);
    void ClearAll();
    void ClearLog();
    void WaitForEnter();
}
#endregion
#region インプット用の差し込みインターフェース
public interface IInputProvider
{
    string? Input();
}

public class ConsoleInputProvider : IInputProvider
{
    public string? Input()
    {
        return Console.ReadLine();
    }
}
#endregion