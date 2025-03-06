using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using System.Numerics;

namespace Types.Tests
{
    public unsafe class LayoutTests : TypeTests
    {
        [Test]
        public void VerifyLayoutOfRegisteredTypes()
        {
            TypeLayout type = TypeRegistry.Get<Stress>();
            Assert.That(type.SystemType, Is.EqualTo(typeof(Stress)));
            Assert.That(type.Name.ToString(), Is.EqualTo("Stress"));
            Assert.That(type.Size, Is.EqualTo((uint)sizeof(Stress)));
            Assert.That(type.Count, Is.EqualTo(5));
            Assert.That(type[0].Size, Is.EqualTo(1));
            Assert.That(type[0].Name.ToString(), Is.EqualTo("first"));
            Assert.That(type[1].Size, Is.EqualTo(2));
            Assert.That(type[1].Name.ToString(), Is.EqualTo("second"));
            Assert.That(type[2].Size, Is.EqualTo(4));
            Assert.That(type[2].Name.ToString(), Is.EqualTo("third"));
            Assert.That(type[3].Size, Is.EqualTo(4));
            Assert.That(type[3].Name.ToString(), Is.EqualTo("fourth"));
            Assert.That(type[4].Size, Is.EqualTo((uint)sizeof(Cherry)));
            Assert.That(type[4].Name.ToString(), Is.EqualTo("cherry"));
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

            Assert.That(TypeRegistry.Get<Vector3>().Size, Is.EqualTo((uint)sizeof(Vector3)));
            Assert.That(TypeRegistry.Get<Vector3>().Count, Is.EqualTo(3));
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
            string c = TypeLayout.GetFullName<bool>();
            Assert.That(c.ToString(), Is.EqualTo("System.Boolean"));

            string a = TypeLayout.GetFullName<Dictionary<Cherry, Stress>>();
            Assert.That(a.ToString(), Is.EqualTo("Types.Tests.Dictionary<Types.Tests.Cherry, Types.Tests.Stress>"));

            string b = TypeLayout.GetFullName<Dictionary<Cherry, Dictionary<Cherry, Stress>>>();
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
        public void GetOrRegister()
        {
            Assert.That(TypeRegistry.IsRegistered<PlatformOperatingSystem>(), Is.False);
            TypeLayout type = TypeRegistry.GetOrRegister<PlatformOperatingSystem>();
            Assert.That(TypeRegistry.IsRegistered<PlatformOperatingSystem>(), Is.True);
            Assert.That(type.Is<PlatformOperatingSystem>(), Is.True);
        }
    }
}