# Types

Provides assistance for meta-programming with value types in C#.

### Type layouts

These store metadata about what fields are in a type with the `[Type]` attribute:
```cs
namespace Example
{
	[Type]
	public struct Apple
	{
		public byte first;
		public ushort second;
		public Item item;
	}

	[Type]
	public struct Item
	{
		public uint id;
	}
}

TypeLayout layout = TypeLayout.Get<Apple>();
Assert.That(layout.FullName, Is.EqualTo("Example.Apple"));
Assert.That(layout.Size, Is.EqualTo(7));
Assert.That(layout.Variables.Length, Is.EqualTo(3));
Assert.That(layout.Variables[0].Name, Is.EqualTo("first"));
Assert.That(layout.Variables[1].Name, Is.EqualTo("second"));
Assert.That(layout.Variables[2].Name, Is.EqualTo("item"));
```

### Inheriting

Value types that are partial, and use the `IInherit<>` interface can inherit fields
and methods from the mentioned type:
```cs
public struct ParentType
{
	public ushort cd;
}

public partial struct ChildType : IInherit<ParentType>
{
	public byte a;
	public byte b;
}

ChildType child = new();
child.a = 0;
child.b = 1;
child.cd = 2;
```