using NetInject.Cecil;
using NUnit.Framework;
using System;
using System.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using static NUnit.Framework.Assert;

namespace NetInject.Test
{
    [TestFixture]
    public class TypePatchTest
    {
        private AssemblyDefinition _ass;

        #region Load and remove

        [SetUp]
        public void Setup()
        {
            ITypeCollector coll = new TypeCollector();
            coll.Collect<SuperClass>();
            _ass = coll.Types.First().Module.Assembly;
        }

        [TearDown]
        public void Teardown()
        {
            _ass?.Dispose();
        }

        #endregion

        private TypeReference TestSomeType(TypeReference key,
            ITypeSuggestor patcher, ITypeImporter importer)
        {
            Console.WriteLine($" got '{WithoutNamespace(key)}'");
            var newKey = patcher[key, importer];
            Console.WriteLine($" --> '{WithoutNamespace(newKey)}'");
            return newKey;
        }

        private TypeReference TestSomeType(TypeReference key,
            ITypePatcher patcher, ITypeImporter importer)
        {
            var field = new FieldDefinition("test", FieldAttributes.Private, key)
            {
                DeclaringType = _ass.GetAllTypes().First()
            };
            var result = new List<TypeReference>();
            Console.Write($"Input := {field.FieldType}");
            patcher.Patch(field, o => result.Add(o));
            Console.WriteLine($"   Output := {field.FieldType}");
            return field.FieldType ?? result.FirstOrDefault();
        }

        private TypeReference TestSomeType(TypeReference key,
            Tuple<TypeSuggestor, TypePatcher> patcher, ITypeImporter importer)
        {
            var newKey = TestSomeType(key, patcher.Item1, importer);
            var newVal = TestSomeType(key, patcher.Item2, importer);
            if (!(newKey + "").Equals(newVal + ""))
                throw new InvalidOperationException($"{newKey} != {newVal}");
            return newKey ?? newVal;
        }

        private static string WithoutNamespace(TypeReference key)
            => key.ToString().Replace("System.Collections.Generic.", "").Replace("System.", "")
                .Replace("`2", "").Replace("`1", "");

        [Test]
        public void ShouldPatchGenericsAndArrays()
        {
            var testTypes = GetTestTypes().ToArray();
            AreEqual(4, testTypes.Length);
            var mbs = testTypes.SelectMany(GetMemberTypes);
            var types = mbs.Distinct().OrderBy(t => t.FullName.Length).ToArray();
            var dict = new Dictionary<TypeReference, TypeReference>();
            var clr = types[0];
            var my = types[6];
            dict[ToRef(typeof(SuperClass), my)] = ToRef(typeof(decimal), clr);
            dict[ToRef(typeof(SuperDelegate), my)] = ToRef(typeof(double), clr);
            dict[ToRef(typeof(SuperStruct), my)] = ToRef(typeof(ulong), clr);
            dict[ToRef(typeof(SuperWeird), my)] = ToRef(typeof(sbyte), clr);
            var imp = new TypeImporter(clr);
            var patcher = Tuple.Create(new TypeSuggestor(dict), new TypePatcher(dict));
            AreEqual("System.String", TestSomeType(types[0], patcher, imp).FullName);
            AreEqual("System.Int32*", TestSomeType(types[1], patcher, imp).FullName);
            AreEqual("System.UInt32&", TestSomeType(types[2], patcher, imp).FullName);
            AreEqual("System.UInt32&", TestSomeType(types[3], patcher, imp).FullName);
            AreEqual("System.Single**", TestSomeType(types[4], patcher, imp).FullName);
            AreEqual("System.Int16***", TestSomeType(types[5], patcher, imp).FullName);
            AreEqual("System.Decimal", TestSomeType(types[6], patcher, imp).FullName);
            AreEqual("System.UInt64", TestSomeType(types[7], patcher, imp).FullName);
            AreEqual("System.Decimal&", TestSomeType(types[8], patcher, imp).FullName);
            AreEqual("System.Decimal&", TestSomeType(types[9], patcher, imp).FullName);
            AreEqual("System.SByte&", TestSomeType(types[10], patcher, imp).FullName);
            AreEqual("System.SByte&", TestSomeType(types[11], patcher, imp).FullName);
            AreEqual("System.Double", TestSomeType(types[12], patcher, imp).FullName);
            AreEqual("System.Double[]", TestSomeType(types[13], patcher, imp).FullName);
            AreEqual("System.Double[]", TestSomeType(types[14], patcher, imp).FullName);
            AreEqual("System.Double[][]", TestSomeType(types[15], patcher, imp).FullName);
            AreEqual("System.Double[][][]", TestSomeType(types[16], patcher, imp).FullName);
            AreEqual("System.Double[,]", TestSomeType(types[17], patcher, imp).FullName);
            AreEqual("System.Double[,,]", TestSomeType(types[18], patcher, imp).FullName);
            AreEqual("System.Nullable`1<System.UInt64>", TestSomeType(types[19], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.ISet`1<System.UInt64>",
                TestSomeType(types[20], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.IList`1<System.UInt64>",
                TestSomeType(types[21], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.IList`1<System.Decimal>&",
                TestSomeType(types[22], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.ISet`1<System.Decimal[]>[]&",
                TestSomeType(types[23], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.ICollection`1<System.UInt64>",
                TestSomeType(types[24], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.IEnumerable`1<System.UInt64>",
                TestSomeType(types[25], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.IEnumerable`1<System.Double[]>",
                TestSomeType(types[26], patcher, imp).FullName);
            AreEqual("System.Collections.Generic.IDictionary`2<System.Double,System.UInt64>",
                TestSomeType(types[27], patcher, imp).FullName);
            AreEqual(
                "System.Collections.Generic.ICollection`1<System.Collections.Generic.IEnumerable`1<System.Double[]>>",
                TestSomeType(types[28], patcher, imp).FullName);
            AreEqual(
                "System.Collections.Generic.IList`1<System.Collections.Generic.ICollection`1<System.Collections.Generic.IEnumerable`1<System.Double[]>>>",
                TestSomeType(types[29], patcher, imp).FullName);
            AreEqual(
                "System.Collections.Generic.ISet`1<System.Collections.Generic.IList`1<System.Collections.Generic.ICollection`1<System.Collections.Generic.IEnumerable`1<System.Double[]>>>>",
                TestSomeType(types[30], patcher, imp).FullName);
            AreEqual(
                "System.Collections.Generic.ISet`1<System.Collections.Generic.IList`1<System.Collections.Generic.ICollection`1<System.Collections.Generic.IEnumerable`1<System.Double[]>[]>>[]>[]",
                TestSomeType(types[31], patcher, imp).FullName);
            AreEqual(
                "System.Collections.Generic.IDictionary`2<System.Collections.Generic.IEnumerable`1<System.Double[]>,System.Collections.Generic.ISet`1<System.Collections.Generic.IList`1<System.Collections.Generic.ICollection`1<System.Tuple`2<System.Double,System.UInt64>>>>>",
                TestSomeType(types[32], patcher, imp).FullName);
            AreEqual(
                "System.Collections.Generic.IDictionary`2<System.Collections.Generic.IEnumerable`1<System.Double>[],System.Collections.Generic.ISet`1<System.Collections.Generic.IList`1<System.Collections.Generic.ICollection`1<System.Tuple`2<System.Double,System.UInt64>>>>[]>",
                TestSomeType(types[33], patcher, imp).FullName);
            AreEqual(34, types.Length);
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
            => _ass.GetAllTypes().Where(t => t.Name.StartsWith("Super"));
    }
}