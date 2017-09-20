using NetInject.Cecil;
using NUnit.Framework;
using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;

namespace NetInject.Test
{
    [TestFixture]
    public class TypePatchTest
    {
        private AssemblyDefinition ass;

        #region Load and remove
        [SetUp]
        public void Setup()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<SuperClass>();
            ass = coll.Types.First().Module.Assembly;
        }

        [TearDown]
        public void Teardown()
        {
            ass?.Dispose();
        }
        #endregion

        [Test]
        public void ShouldPatchGenericsAndArrays()
        {
            var testTypes = GetTestTypes().ToArray();
            Assert.AreEqual(3, testTypes.Length);
            var types = GetTestTypes().SelectMany(GetMemberTypes).ToArray();
            foreach (var type in types)
            {
                Console.WriteLine(type);
            }
            Assert.AreEqual(23, types.Length);
        }

        private IEnumerable<TypeReference> GetMemberTypes(TypeDefinition t)
            => t.Fields.Where(f => !f.Name.Contains("Backing")).Select(f => f.FieldType)
            .Concat(t.Properties.Select(p => p.PropertyType))
            .Concat(t.Methods.Where(m => m.Name == "Invoke")
                .SelectMany(m => m.Parameters.Select(p => p.ParameterType)));

        private IEnumerable<TypeDefinition> GetTestTypes()
            => ass.GetAllTypes().Where(t => t.Name.StartsWith("Super"));
    }
}