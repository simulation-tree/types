using System;
using System.Numerics;

namespace Types.Tests
{
    public unsafe class LayoutTests : TypeTests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            Type type = TypeRegistry.GetType<Stress>();
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
            Assert.That(TypeRegistry.IsTypeRegistered<bool>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<byte>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<sbyte>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<short>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<ushort>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<int>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<uint>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<long>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<ulong>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<float>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<double>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<char>(), Is.True);

            Assert.That(TypeRegistry.IsRegistered(typeof(bool).FullName ?? typeof(bool).Name), Is.True);
            Assert.That(TypeRegistry.IsRegistered(typeof(byte).FullName ?? typeof(byte).Name), Is.True);
            Assert.That(TypeRegistry.IsRegistered(typeof(sbyte).FullName ?? typeof(sbyte).Name), Is.True);
            Assert.That(TypeRegistry.IsRegistered(typeof(short).FullName ?? typeof(short).Name), Is.True);

            Assert.That(TypeRegistry.GetType<Vector3>().size, Is.EqualTo((uint)sizeof(Vector3)));
            Assert.That(TypeRegistry.GetType<Vector3>().Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void CheckLayouts()
        {
            Assert.That(TypeRegistry.IsTypeRegistered<bool>(), Is.True);
            Assert.That(TypeRegistry.IsTypeRegistered<byte>(), Is.True);
            Type boolean = TypeRegistry.GetType<bool>();
            Type byteType = TypeRegistry.GetType<byte>();
            Assert.That(boolean.size, Is.EqualTo(1));
            Assert.That(byteType.size, Is.EqualTo(1));
            Assert.That(boolean.GetHashCode(), Is.EqualTo(TypeRegistry.GetType<bool>().GetHashCode()));
            Assert.That(byteType.GetHashCode(), Is.EqualTo(TypeRegistry.GetType<byte>().GetHashCode()));
        }

        [Test]
        public void CheckIfLayoutIs()
        {
            Type layout = TypeRegistry.GetType<Stress>();

            Assert.That(layout.Is<Stress>(), Is.True);
            Assert.That(layout.Is<Cherry>(), Is.False);
        }

        [Test]
        public void CheckNamesOfTypes()
        {
            Assert.That(TypeRegistry.GetType<bool>().Name.ToString(), Is.EqualTo("Boolean"));
            Assert.That(TypeRegistry.GetType<bool>().FullName.ToString(), Is.EqualTo("System.Boolean"));
            Assert.That(TypeRegistry.GetType<Cherry>().Name.ToString(), Is.EqualTo("Cherry"));
            Assert.That(TypeRegistry.GetType<Cherry>().FullName.ToString(), Is.EqualTo("Types.Tests.Cherry"));
        }

        [Test]
        public void GetFullNameOfType()
        {
            string c = TypeRegistry.GetFullName<bool>();
            Assert.That(c.ToString(), Is.EqualTo("System.Boolean"));

            string a = TypeRegistry.GetFullName<Dictionary<Cherry, Stress>>();
            Assert.That(a.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>"));

            string b = TypeRegistry.GetFullName<Dictionary<Cherry, Dictionary<Cherry, Stress>>>();
            Assert.That(b.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>>"));
        }

        [Test]
        public void CreateObjectFromTypeLayout()
        {
            Type layout = TypeRegistry.GetType<Stress>();
            object instance = layout.CreateInstance();
            Assert.That(instance, Is.InstanceOf<Stress>());
            Assert.That((Stress)instance, Is.EqualTo(default(Stress)));
        }

        [Test]
        public void GetOrRegister()
        {
            Assert.That(TypeRegistry.IsTypeRegistered<DayOfWeek>(), Is.False);
            Type type = TypeRegistry.GetOrRegisterType<DayOfWeek>();
            Assert.That(TypeRegistry.IsTypeRegistered<DayOfWeek>(), Is.True);
            Assert.That(type.Is<DayOfWeek>(), Is.True);
        }

        [Test]
        public void GetImplementedInterfaces()
        {
            Type type = TypeRegistry.GetType<Stress>();
            ReadOnlySpan<Interface> interfaces = type.Interfaces;
            Assert.That(interfaces.Length, Is.EqualTo(1));
            Assert.That(interfaces[0].Name.ToString(), Is.EqualTo("IDisposable"));
        }
    }
}