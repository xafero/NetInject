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
            Assert.AreEqual(855, coll.Events.Count);
            Assert.AreEqual(14761, coll.Fields.Count);
            Assert.AreEqual(29653, coll.Methods.Count);
            Assert.AreEqual(1, coll.Modules.Count);
            Assert.AreEqual(5255, coll.Properties.Count);
            Assert.AreEqual(1732, coll.Types.Count);
            Assert.AreEqual(172, coll.GenericMethods.Count);
            Assert.AreEqual(109, coll.GenericTypes.Count);
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
            Assert.AreEqual(1240, coll.Fields.Count);
            Assert.AreEqual(1798, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(78, coll.Properties.Count);
            Assert.AreEqual(20, coll.Types.Count);
            Assert.AreEqual(10, coll.GenericMethods.Count);
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
            Assert.AreEqual(0, coll.Events.Count);
            Assert.AreEqual(20, coll.Fields.Count);
            Assert.AreEqual(24, coll.Methods.Count);
            Assert.AreEqual(0, coll.Modules.Count);
            Assert.AreEqual(9, coll.Properties.Count);
            Assert.AreEqual(3, coll.Types.Count);
            Assert.AreEqual(0, coll.GenericMethods.Count);
            Assert.AreEqual(10, coll.GenericTypes.Count);
            Assert.IsTrue(typeof(SuperClass).IsClass);
        }
    }
}