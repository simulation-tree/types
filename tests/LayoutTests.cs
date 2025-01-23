using Unmanaged;

namespace Types.Tests
{
    public class LayoutTests : TypeTests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            TypeLayout layout = TypeRegistry.Get<Stress>();
            Assert.That(layout.Name.ToString(), Is.EqualTo("Stress"));
            Assert.That(layout.Size, Is.EqualTo(TypeInfo<Stress>.size));
            Assert.That(layout.Variables.Length, Is.EqualTo(5));
            Assert.That(layout.Variables[0].Size, Is.EqualTo(1));
            Assert.That(layout.Variables[0].Name.ToString(), Is.EqualTo("first"));
            Assert.That(layout.Variables[1].Size, Is.EqualTo(2));
            Assert.That(layout.Variables[1].Name.ToString(), Is.EqualTo("second"));
            Assert.That(layout.Variables[2].Size, Is.EqualTo(4));
            Assert.That(layout.Variables[2].Name.ToString(), Is.EqualTo("third"));
            Assert.That(layout.Variables[3].Size, Is.EqualTo(4));
            Assert.That(layout.Variables[3].Name.ToString(), Is.EqualTo("fourth"));
            Assert.That(layout.Variables[4].Size, Is.EqualTo(TypeInfo<Cherry>.size));
            Assert.That(layout.Variables[4].Name.ToString(), Is.EqualTo("cherry"));
        }

        [Test]
        public void PrimitiveTypesAreAvailable()
        {
            Assert.That(TypeRegistry.IsRegistered<bool>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<byte>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<sbyte>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<short>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<ushort>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<int>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<uint>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<long>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<ulong>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<float>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<double>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<char>(), Is.True);

            Assert.That(TypeRegistry.IsRegistered(typeof(bool).FullName ?? typeof(bool).Name), Is.True);
            Assert.That(TypeRegistry.IsRegistered(typeof(byte).FullName ?? typeof(byte).Name), Is.True);
            Assert.That(TypeRegistry.IsRegistered(typeof(sbyte).FullName ?? typeof(sbyte).Name), Is.True);
            Assert.That(TypeRegistry.IsRegistered(typeof(short).FullName ?? typeof(short).Name), Is.True);
        }

        [Test]
        public void SerializeTypes()
        {
            TypeLayout a = TypeRegistry.Get<Stress>();
            using BinaryWriter writer = new();
            writer.WriteObject(a);

            using BinaryReader reader = new(writer);
            TypeLayout b = reader.ReadObject<TypeLayout>();

            Assert.That(a.Name.ToString(), Is.EqualTo(b.Name.ToString()));
            Assert.That(a.Variables.Length, Is.EqualTo(5));
            Assert.That(a.Variables.Length, Is.EqualTo(b.Variables.Length));
            Assert.That(a.Variables[4].Name.ToString(), Is.EqualTo(b.Variables[4].Name.ToString()));
            Assert.That(a.Variables[4].TypeLayout.Variables[0], Is.EqualTo(b.Variables[4].TypeLayout.Variables[0]));
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void CheckLayouts()
        {
            Assert.That(TypeRegistry.IsRegistered<bool>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<byte>(), Is.True);
            TypeLayout boolean = TypeRegistry.Get<bool>();
            TypeLayout byteType = TypeRegistry.Get<byte>();
            Assert.That(boolean.Size, Is.EqualTo(1));
            Assert.That(byteType.Size, Is.EqualTo(1));
            Assert.That(boolean.GetHashCode(), Is.EqualTo(TypeRegistry.Get<bool>().GetHashCode()));
            Assert.That(byteType.GetHashCode(), Is.EqualTo(TypeRegistry.Get<byte>().GetHashCode()));
        }

        [Test]
        public void CheckIfLayoutIs()
        {
            TypeLayout layout = TypeRegistry.Get<Stress>();

            Assert.That(layout.Is<Stress>(), Is.True);
            Assert.That(layout.Is<Cherry>(), Is.False);
        }

        [Test]
        public void CheckNamesOfTypes()
        {
            Assert.That(TypeRegistry.Get<bool>().Name.ToString(), Is.EqualTo("Boolean"));
            Assert.That(TypeRegistry.Get<bool>().FullName.ToString(), Is.EqualTo("System.Boolean"));
            Assert.That(TypeRegistry.Get<Cherry>().Name.ToString(), Is.EqualTo("Cherry"));
            Assert.That(TypeRegistry.Get<Cherry>().FullName.ToString(), Is.EqualTo("Types.Tests.Cherry"));
        }

        [Test]
        public void GetFullNameOfType()
        {
            FixedString a = TypeLayout.GetFullName<Dictionary<Cherry, Stress>>();
            Assert.That(a.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>"));

            FixedString b = TypeLayout.GetFullName<Dictionary<Cherry, Dictionary<Cherry, Stress>>>();
            Assert.That(b.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>>"));
        }

        [Test]
        public void CreateObjectFromTypeLayout()
        {
            TypeLayout layout = TypeRegistry.Get<Stress>();
            object instance = layout.CreateInstance();
            Assert.That(instance, Is.InstanceOf<Stress>());
            Assert.That((Stress)instance, Is.EqualTo(default(Stress)));
        }

        [Test]
        public void VerifyInheritingType()
        {
            TypeLayout childLayout = TypeRegistry.Get<ChildType>();
            Assert.That(childLayout.Variables.Length, Is.EqualTo(3));
            Assert.That(childLayout.ContainsVariable("a"), Is.True);
            Assert.That(childLayout.ContainsVariable("b"), Is.True);
            Assert.That(childLayout.ContainsVariable("cd"), Is.True);

            TypeLayout grandChildLayout = TypeRegistry.Get<GrandChildType>();
            Assert.That(grandChildLayout.Variables.Length, Is.EqualTo(4));
            Assert.That(grandChildLayout.ContainsVariable("a"), Is.True);
            Assert.That(grandChildLayout.ContainsVariable("b"), Is.True);
            Assert.That(grandChildLayout.ContainsVariable("cd"), Is.True);
            Assert.That(grandChildLayout.ContainsVariable("value"), Is.True);
        }
    }
}