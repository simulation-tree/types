# Types

[![Test](https://github.com/simulation-tree/types/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/types/actions/workflows/test.yml)

Assists programming with value types in C#.

### Initializing

Before types are interacted/accessed, the `MetadataRegistry` must be initialized
by registering the types and interfaces in use:

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

TypeMetadata type = TypeMetadata.Get<Apple>();
Assert.That(type.FullName.ToString(), Is.EqualTo("Example.Apple"));
Assert.That(type.Size, Is.EqualTo(7));

ReadOnlySpan<Field> fields = type.Fields;
Assert.That(fields.Length, Is.EqualTo(3));
Assert.That(fields[0].Name.ToString(), Is.EqualTo("first"));
Assert.That(fields[1].Name.ToString(), Is.EqualTo("second"));
Assert.That(fields[2].Name.ToString(), Is.EqualTo("item"));
Assert.That(fields[2].type, Is.EqualTo(TypeMetadata.Get<Item>()));
```

### Type banks

Type banks are generated for every project that references this library.
They contain code that registers declared types:
```cs
MetadataRegistry.Load<CustomTypeBank>();

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
MetadataRegistryLoader.Load();
```
