using System;

namespace Types.Tests
{
    public struct Fruit : IEquatable<Fruit>
    {
        public int value;

        public Fruit(int value)
        {
            this.value = value;
        }

        public void Eat(int amount)
        {
            value -= amount;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Fruit fruit && Equals(fruit);
        }

        public readonly bool Equals(Fruit other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static bool operator ==(Fruit left, Fruit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fruit left, Fruit right)
        {
            return !(left == right);
        }
    }
}