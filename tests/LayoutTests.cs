using System;
using System.Numerics;

namespace Types.Tests
{
    public unsafe class LayoutTests : TypeTests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            Type type = TypeRegistry.Get<Stress>();
            Assert.That(type.SystemType, Is.EqualTo(typeof(Stress)));
            Assert.That(type.Name.ToString(), Is.EqualTo("Stress"));
            Assert.That(type.size, Is.EqualTo((uint)sizeof(Stress)));

            ReadOnlySpan<Field> fields = type.Fields;
            Assert.That(fields.Length, Is.EqualTo(5));
            Assert.That(fields[0].Size, Is.EqualTo(1));
            Assert.That(fields[0].Name.ToString(), Is.EqualTo("first"));
            Assert.That(fields[1].Size, Is.EqualTo(2));
            Assert.That(fields[1].Name.ToString(), Is.EqualTo("second"));
            Assert.That(fields[2].Size, Is.EqualTo(4));
            Assert.That(fields[2].Name.ToString(), Is.EqualTo("third"));
            Assert.That(fields[3].Size, Is.EqualTo(4));
            Assert.That(fields[3].Name.ToString(), Is.EqualTo("fourth"));
            Assert.That(fields[4].Size, Is.EqualTo((uint)sizeof(Cherry)));
            Assert.That(fields[4].Name.ToString(), Is.EqualTo("cherry"));
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

            Assert.That(TypeRegistry.Get<Vector3>().size, Is.EqualTo((uint)sizeof(Vector3)));
            Assert.That(TypeRegistry.Get<Vector3>().Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void CheckLayouts()
        {
            Assert.That(TypeRegistry.IsRegistered<bool>(), Is.True);
            Assert.That(TypeRegistry.IsRegistered<byte>(), Is.True);
            Type boolean = TypeRegistry.Get<bool>();
            Type byteType = TypeRegistry.Get<byte>();
            Assert.That(boolean.size, Is.EqualTo(1));
            Assert.That(byteType.size, Is.EqualTo(1));
            Assert.That(boolean.GetHashCode(), Is.EqualTo(TypeRegistry.Get<bool>().GetHashCode()));
            Assert.That(byteType.GetHashCode(), Is.EqualTo(TypeRegistry.Get<byte>().GetHashCode()));
        }

        [Test]
        public void CheckIfLayoutIs()
        {
            Type layout = TypeRegistry.Get<Stress>();

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
            string c = Type.GetFullName<bool>();
            Assert.That(c.ToString(), Is.EqualTo("System.Boolean"));

            string a = Type.GetFullName<Dictionary<Cherry, Stress>>();
            Assert.That(a.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>"));

            string b = Type.GetFullName<Dictionary<Cherry, Dictionary<Cherry, Stress>>>();
            Assert.That(b.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>>"));
        }

        [Test]
        public void CreateObjectFromTypeLayout()
        {
            Type layout = TypeRegistry.Get<Stress>();
            object instance = layout.CreateInstance();
            Assert.That(instance, Is.InstanceOf<Stress>());
            Assert.That((Stress)instance, Is.EqualTo(default(Stress)));
        }

        [Test]
        public void GetOrRegister()
        {
            Assert.That(TypeRegistry.IsRegistered<DayOfWeek>(), Is.False);
            Type type = TypeRegistry.GetOrRegister<DayOfWeek>();
            Assert.That(TypeRegistry.IsRegistered<DayOfWeek>(), Is.True);
            Assert.That(type.Is<DayOfWeek>(), Is.True);
        }
    }
}