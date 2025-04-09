namespace Types.Tests
{
    public struct VictimOfABug : IHahaInterfaceWithStaticMethodsHaha
    {
        static void IHahaInterfaceWithStaticMethodsHaha.DoTheThing()
        {
            //there isnt actually anything to do
        }
    }

    public interface IHahaInterfaceWithStaticMethodsHaha
    {
        static abstract void DoTheThing();
    }
}