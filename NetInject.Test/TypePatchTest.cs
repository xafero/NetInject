using NetInject.Cecil;
using NUnit.Framework;
using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using Mono.Cecil.Rocks;

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

        private Tuple<TypeReference, List<TypeReference>> PatchTestField(
            TypeReference key, ITypePatcher patcher)
        {
            var field = new FieldDefinition("test", FieldAttributes.Private, key)
            {
                DeclaringType = ass.GetAllTypes().First()
            };
            var result = new List<TypeReference>();
            Console.Write($"Input := {field.FieldType}");
            patcher.Patch(field, o => result.Add(o));
            Console.WriteLine($"   Output := {field.FieldType}");
            return Tuple.Create(field.FieldType, result);
        }

        [Test]
        public void ShouldPatchGenericsAndArrays()
        {
            var testTypes = GetTestTypes().ToArray();
            Assert.AreEqual(4, testTypes.Length);
            var mbs = testTypes.SelectMany(GetMemberTypes);
            var types = mbs.Distinct().OrderBy(t => t.FullName.Length).ToArray();
            var dict = new Dictionary<TypeReference, TypeReference>();
            var clr = types[0];
            var my = types[6];
            dict[ToRef(typeof(SuperClass), my)] = ToRef(typeof(decimal), clr);
            dict[ToRef(typeof(SuperDelegate), my)] = ToRef(typeof(double), clr);
            dict[ToRef(typeof(SuperStruct), my)] = ToRef(typeof(ulong), clr);
            dict[ToRef(typeof(SuperWeird), my)] = ToRef(typeof(sbyte), clr);
            var patcher = new TypePatcher(dict);            
            Assert.AreEqual("System.String", PatchTestField(types[0], patcher).Item1.FullName);
            Assert.AreEqual("System.Int32*", PatchTestField(types[1], patcher).Item1.FullName);
            Assert.AreEqual("System.UInt32&", PatchTestField(types[2], patcher).Item1.FullName);
            Assert.AreEqual("System.UInt32&", PatchTestField(types[3], patcher).Item1.FullName);
            Assert.AreEqual("System.Single**", PatchTestField(types[4], patcher).Item1.FullName);
            Assert.AreEqual("System.Int16***", PatchTestField(types[5], patcher).Item1.FullName);
            Assert.AreEqual("System.Decimal", PatchTestField(types[6], patcher).Item1.FullName);
            Assert.AreEqual("System.UInt64", PatchTestField(types[7], patcher).Item1.FullName);
            Assert.AreEqual("System.Decimal&", PatchTestField(types[8], patcher).Item1.FullName);
            Assert.AreEqual("System.Decimal&", PatchTestField(types[9], patcher).Item1.FullName);
            Assert.AreEqual("System.SByte&", PatchTestField(types[10], patcher).Item1.FullName);
            Assert.AreEqual("System.SByte&", PatchTestField(types[11], patcher).Item1.FullName);
            Assert.AreEqual("System.Double", PatchTestField(types[12], patcher).Item1.FullName);
            Assert.AreEqual("System.Double[]", PatchTestField(types[13], patcher).Item1.FullName);
            Assert.AreEqual("System.Double[]", PatchTestField(types[14], patcher).Item1.FullName);
            Assert.AreEqual("System.Double[][]", PatchTestField(types[15], patcher).Item1.FullName);
            Assert.AreEqual("System.Double[][][]", PatchTestField(types[16], patcher).Item1.FullName);
            Assert.AreEqual("System.Double[,]", PatchTestField(types[17], patcher).Item1.FullName);
            Assert.AreEqual("System.Double[,,]", PatchTestField(types[18], patcher).Item1.FullName);
            Assert.AreEqual("System.Nullable`1<System.UInt64>", PatchTestField(types[19], patcher).Item1.FullName);
            Assert.AreEqual("System.Collections.Generic.ISet`1<System.UInt64>", PatchTestField(types[20], patcher).Item1.FullName);
            Assert.AreEqual("System.Collections.Generic.IList`1<System.UInt64>", PatchTestField(types[21], patcher).Item1.FullName);
            Assert.AreEqual("System.Collections.Generic.IList`1<System.Decimal>&", PatchTestField(types[22], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[23], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[24], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[25], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[26], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[27], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[28], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[29], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[30], patcher).Item1.FullName);
            Assert.AreEqual(0, PatchTestField(types[31], patcher).Item1.FullName);
            Assert.AreEqual(30, types.Length);
        }

        private static TypeReference ToRef(Type type, TypeReference typeRef)
            => ToRef(type, typeRef.Module, typeRef.Scope);

        private static TypeReference ToRef(Type type, ModuleDefinition mod, IMetadataScope scope)
            => new TypeReference(type.Namespace, type.Name, mod, scope);

        private static IEnumerable<TypeReference> GetMemberTypes(TypeDefinition t)
            => t.Fields.Where(f => !f.Name.Contains("Backing")).Select(f => f.FieldType)
                .Concat(t.Properties.Select(p => p.PropertyType))
                .Concat(t.Methods.Where(m => m.Name == "Invoke")
                    .SelectMany(m => m.Parameters.Select(p => p.ParameterType)));

        private IEnumerable<TypeDefinition> GetTestTypes()
            => ass.GetAllTypes().Where(t => t.Name.StartsWith("Super"));
    }
}