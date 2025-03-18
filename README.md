# Types

[![Test](https://github.com/simulation-tree/types/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/types/actions/workflows/test.yml)

Assists programming with value types in C#.

### Type layouts

These store metadata about what fields are found on a type:
```cs
namespace Example
{
	public struct Apple
	{
		public byte first;
		public ushort second;
		public Item item;
	}

	public struct Item
	{
		public uint id;
	}
}

TypeLayout layout = TypeRegistry.Get<Apple>();
Assert.That(layout.FullName.ToString(), Is.EqualTo("Example.Apple"));
Assert.That(layout.size, Is.EqualTo(7));
Assert.That(layout.variableCount, Is.EqualTo(3));
Assert.That(layout[0].Name.ToString(), Is.EqualTo("first"));
Assert.That(layout[1].Name.ToString(), Is.EqualTo("second"));
Assert.That(layout[2].Name.ToString(), Is.EqualTo("item"));
Assert.That(layout[2].Type, Is.EqualTo(TypeRegistry.Get<Item>()));
```

### Type banks

Type banks are generated for every project that references this library.
They contain code that registers declared types:
```cs
TypeRegistry.Load<CustomTypeBank>();

public readonly struct CustomTypeBank : ITypeBank
{
    void ITypeBank.Load(Register register)
    {
        register.Invoke<DateTime>();
    }
}
```

### Type registry loaders

These are also generated like type banks. But differ in that they're only
generated for projects with an entry point (a `static void Main()` method).
They load all type banks found in the project:
```cs
TypeRegistryLoader.Load();
```
