using System;

public struct GameId<T> : IEquatable<GameId<T>>
{
    private readonly string _value;
    public GameId(string value)
    {
        _value = value?.Trim().ToLower() ?? string.Empty;
    }

    public static implicit operator GameId<T>(string value) => new GameId<T>(value);

    public static implicit operator string(GameId<T> id) => id._value;

    public bool Equals(GameId<T> other) => _value == other._value;
    public override bool Equals(object obj) => obj is GameId<T> other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public static bool operator ==(GameId<T> left, GameId<T> right) => left.Equals(right);
    public static bool operator !=(GameId<T> left, GameId<T> right) => !left.Equals(right);
    public override string ToString() => _value;
    public bool IsEmpty => string.IsNullOrEmpty(_value);
}