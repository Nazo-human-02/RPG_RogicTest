#region 乱数生成の差し込みインターフェース
using System.ComponentModel.DataAnnotations;

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
    void Log(string message);
}

public class ConsoleLogProvider : ILogProvider
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
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