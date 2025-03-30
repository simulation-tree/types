using System;
using System.Collections.Generic;
using System.Numerics;

namespace Types.Tests
{
    public unsafe class TypeTests : Tests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            Type type = MetadataRegistry.GetType<Stress>();
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
            Assert.That(MetadataRegistry.IsTypeRegistered<bool>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<byte>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<sbyte>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<short>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<ushort>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<int>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<uint>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<long>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<ulong>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<float>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<double>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<char>(), Is.True);

            Assert.That(MetadataRegistry.IsTypeRegistered(typeof(bool).FullName ?? typeof(bool).Name), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered(typeof(byte).FullName ?? typeof(byte).Name), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered(typeof(sbyte).FullName ?? typeof(sbyte).Name), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered(typeof(short).FullName ?? typeof(short).Name), Is.True);

            Assert.That(MetadataRegistry.GetType<Vector3>().size, Is.EqualTo((uint)sizeof(Vector3)));
            Assert.That(MetadataRegistry.GetType<Vector3>().Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void CheckLayouts()
        {
            Assert.That(MetadataRegistry.IsTypeRegistered<bool>(), Is.True);
            Assert.That(MetadataRegistry.IsTypeRegistered<byte>(), Is.True);
            Type boolean = MetadataRegistry.GetType<bool>();
            Type byteType = MetadataRegistry.GetType<byte>();
            Assert.That(boolean.size, Is.EqualTo(1));
            Assert.That(byteType.size, Is.EqualTo(1));
            Assert.That(boolean.GetHashCode(), Is.EqualTo(MetadataRegistry.GetType<bool>().GetHashCode()));
            Assert.That(byteType.GetHashCode(), Is.EqualTo(MetadataRegistry.GetType<byte>().GetHashCode()));
        }

        [Test]
        public void CheckIfLayoutIs()
        {
            Type layout = MetadataRegistry.GetType<Stress>();

            Assert.That(layout.Is<Stress>(), Is.True);
            Assert.That(layout.Is<Cherry>(), Is.False);
        }

        [Test]
        public void CheckNamesOfTypes()
        {
            Assert.That(MetadataRegistry.GetType<bool>().Name.ToString(), Is.EqualTo("Boolean"));
            Assert.That(MetadataRegistry.GetType<bool>().FullName.ToString(), Is.EqualTo("System.Boolean"));
            Assert.That(MetadataRegistry.GetType<Cherry>().Name.ToString(), Is.EqualTo("Cherry"));
            Assert.That(MetadataRegistry.GetType<Cherry>().FullName.ToString(), Is.EqualTo("Types.Tests.Cherry"));
        }

        [Test]
        public void GetFullNameOfType()
        {
            string c = MetadataRegistry.GetFullName<bool>();
            Assert.That(c.ToString(), Is.EqualTo("System.Boolean"));

            string a = MetadataRegistry.GetFullName<Dictionary<Cherry, Stress>>();
            Assert.That(a.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>"));

            string b = MetadataRegistry.GetFullName<Dictionary<Cherry, Dictionary<Cherry, Stress>>>();
            Assert.That(b.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>>"));
        }

        [Test]
        public void CreateObjectFromTypeLayout()
        {
            Type layout = MetadataRegistry.GetType<Stress>();
            object instance = layout.CreateInstance();
            Assert.That(instance, Is.InstanceOf<Stress>());
            Assert.That((Stress)instance, Is.EqualTo(default(Stress)));
        }

        [Test]
        public void GetOrRegister()
        {
            Assert.That(MetadataRegistry.IsTypeRegistered<DayOfWeek>(), Is.False);
            Type type = MetadataRegistry.GetOrRegisterType<DayOfWeek>();
            Assert.That(MetadataRegistry.IsTypeRegistered<DayOfWeek>(), Is.True);
            Assert.That(type.Is<DayOfWeek>(), Is.True);
        }

        [Test]
        public void GetImplementedInterfaces()
        {
            Type type = MetadataRegistry.GetType<Stress>();
            ReadOnlySpan<Interface> interfaces = type.Interfaces;
            Assert.That(interfaces.Length, Is.EqualTo(1));
            Assert.That(interfaces[0].Name.ToString(), Is.EqualTo("IDisposable"));
            Assert.That(type.Implements<IDisposable>(), Is.True);
            Assert.That(type.Implements<ICloneable>(), Is.False);
            Assert.That(interfaces[0].Is<IDisposable>(), Is.True);
            Assert.That(interfaces[0].Is<ICloneable>(), Is.False);
        }

        [Test]
        public void IterateThroughAllDisposableTypes()
        {
            List<Type> types = new();
            foreach (Type type in Type.GetAllThatImplement<IDisposable>())
            {
                types.Add(type);
            }

            Assert.That(types.Count, Is.EqualTo(1));
            Assert.That(types[0].Is<Stress>(), Is.True);
        }
    }
}