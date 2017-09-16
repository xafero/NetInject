using NUnit.Framework;
using NetInject.Cecil;
using NUnit.Framework.Internal;
using NUnit.Framework.Interfaces;
using NUnit;

namespace NetInject.Test
{
    [TestFixture]
    public class TypeCollectTest
    {
        [Test]
        public void ShouldReadAssembly()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect(typeof(TestFixture).Assembly);
            Assert.AreEqual(1, coll.Asses.Count);
            Assert.AreEqual(678, coll.Events.Count);
            Assert.AreEqual(18993, coll.Fields.Count);
            Assert.AreEqual(36931, coll.Methods.Count);
            Assert.AreEqual(1, coll.Modules.Count);
            Assert.AreEqual(5347, coll.Properties.Count);
            Assert.AreEqual(2062, coll.Types.Count);
        }

        [Test]
        public void ShouldReadClass()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect(typeof(FrameworkPackageSettings));
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(0, coll.Events.Count);
            Assert.AreEqual(14, coll.Fields.Count);
            Assert.AreEqual(0, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(0, coll.Properties.Count);
            Assert.AreEqual(1, coll.Types.Count);
            Assert.IsTrue(typeof(FrameworkPackageSettings).IsClass);
        }

        [Test]
        public void ShouldReadInterface()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<ITestListener>();
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(0, coll.Events.Count);
            Assert.AreEqual(3101, coll.Fields.Count);
            Assert.AreEqual(5352, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(78, coll.Properties.Count);
            Assert.AreEqual(20, coll.Types.Count);
            Assert.IsTrue(typeof(ITestListener).IsInterface);
        }

        [Test]
        public void ShouldReadEnum()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<ActionTargets>();
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(0, coll.Events.Count);
            Assert.AreEqual(4, coll.Fields.Count);
            Assert.AreEqual(0, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(0, coll.Properties.Count);
            Assert.AreEqual(1, coll.Types.Count);
            Assert.IsTrue(typeof(ActionTargets).IsEnum);
        }

        [Test]
        public void ShouldReadDelegate()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<TestDelegate>();
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(0, coll.Events.Count);
            Assert.AreEqual(0, coll.Fields.Count);
            Assert.AreEqual(4, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(0, coll.Properties.Count);
            Assert.AreEqual(1, coll.Types.Count);
            Assert.IsTrue(typeof(TestDelegate).IsDelegate());
        }

        [Test]
        public void ShouldReadGenerics()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<SuperClass>();
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(0, coll.Events.Count);
            Assert.AreEqual(12, coll.Fields.Count);
            Assert.AreEqual(18, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(6, coll.Properties.Count);
            Assert.AreEqual(3, coll.Types.Count);
            Assert.IsTrue(typeof(SuperClass).IsClass);
        }
    }
}