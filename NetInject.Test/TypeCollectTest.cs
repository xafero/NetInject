using NUnit.Framework;
using NetInject.Cecil;
using NUnit.Framework.Internal;

namespace NetInject.Test
{
    [TestFixture]
    public class TypeCollectTest
    {
        [Test]
        public void ShouldReadSomeAssembly()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<TestFixture>();
        }
    }
}