using System;
using NUnit.Framework;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Text;
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
            // coll.Collect<TestFixture>();
            Assert.AreEqual(0, null);
        }
    }
}