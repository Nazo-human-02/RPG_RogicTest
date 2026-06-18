#region 乱数生成の差し込みインターフェース
public interface IRandomProvider
{
    int GetRandomInt(int min, int max);
    float GetRandomFloat();
}

public class FixedRandomProvider(int fixedValue, float fixedFloatValue) : IRandomProvider
{
    private readonly int _fixedValue = fixedValue;
    private readonly float _fixedFloatValue = fixedFloatValue; // 固定の浮動小数点値を設定

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