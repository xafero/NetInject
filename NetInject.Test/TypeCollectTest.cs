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
            Assert.IsTrue(coll.Events.Count >= 678);
            Assert.IsTrue(coll.Fields.Count >= 14761);
            Assert.IsTrue(coll.Methods.Count >= 29653);
            Assert.AreEqual(1, coll.Modules.Count);
            Assert.IsTrue(coll.Properties.Count >= 5255);
            Assert.IsTrue(coll.Types.Count >= 1732);
            Assert.IsTrue(coll.GenericMethods.Count >= 172);
            Assert.IsTrue(coll.GenericTypes.Count >= 109);
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
            Assert.AreEqual(0, coll.GenericMethods.Count);
            Assert.AreEqual(0, coll.GenericTypes.Count);
            Assert.IsTrue(typeof(FrameworkPackageSettings).IsClass);
        }

        [Test]
        public void ShouldReadInterface()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<ITestListener>();
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(0, coll.Events.Count);
            Assert.IsTrue(coll.Fields.Count >= 1240);
            Assert.IsTrue(coll.Methods.Count >= 1798);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(78, coll.Properties.Count);
            Assert.AreEqual(20, coll.Types.Count);
            Assert.IsTrue(coll.GenericMethods.Count >= 10);
            Assert.AreEqual(6, coll.GenericTypes.Count);
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
            Assert.AreEqual(0, coll.GenericMethods.Count);
            Assert.AreEqual(0, coll.GenericTypes.Count);
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
            Assert.AreEqual(0, coll.GenericMethods.Count);
            Assert.AreEqual(0, coll.GenericTypes.Count);
            Assert.IsTrue(typeof(TestDelegate).IsDelegate());
        }

        [Test]
        public void ShouldReadGenerics()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<SuperClass>();
            Assert.AreEqual(0, coll.Asses.Count);
            Assert.AreEqual(1, coll.Events.Count);
            Assert.IsTrue(coll.Fields.Count >= 20);
            Assert.IsTrue(coll.Methods.Count >= 28);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.IsTrue(coll.Properties.Count >= 9);
            Assert.AreEqual(4, coll.Types.Count);
            Assert.IsTrue(coll.GenericMethods.Count >= 1);
            Assert.IsTrue(coll.GenericTypes.Count >= 11);
            Assert.IsTrue(typeof(SuperClass).IsClass);
        }
    }
}