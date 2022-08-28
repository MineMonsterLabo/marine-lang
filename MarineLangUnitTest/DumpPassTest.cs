using System;
using System.IO;
using System.Linq;
using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.Dumps.Models;
using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class DumpPassTest
    {
        private MarineDumpModel CreateFileDump(string path = null)
        {
            if (path == null)
                VmCreateHelper.Create("").CreateDumpWithFile();
            else
                VmCreateHelper.Create("").CreateDumpWithFile(path);

            if (path == null)
                path = $"{Environment.CurrentDirectory}/marine_dump.json";
            Assert.True(File.Exists(path));

            string json = File.ReadAllText(path);
            Assert.True(json.Length > 2);

            DumpDeserializer deserializer = new DumpDeserializer();
            return deserializer.Deserialize(json);
        }

        private void DumpTest(MarineDumpModel dumpModel)
        {
            var hogeTypeRef = dumpModel.GlobalVariables["hoge"];
            var hoge = hogeTypeRef.GetTypeDumpModel(dumpModel);
            Assert.Equal(15, hoge.Members.Count);
            Assert.Equal(nameof(VmCreateHelper.Hoge), hogeTypeRef.Name);
            Assert.Equal(typeof(VmCreateHelper.Hoge).FullName, hogeTypeRef.FullName);
            Assert.Equal(typeof(VmCreateHelper.Hoge).AssemblyQualifiedName, hogeTypeRef.QualifiedName);

            var fugaTypeRef = dumpModel.GlobalVariables["fuga"];
            var fuga = fugaTypeRef.GetTypeDumpModel(dumpModel);
            Assert.Equal(9, fuga.Members.Count);
            Assert.Equal(nameof(VmCreateHelper.Fuga), fugaTypeRef.Name);
            Assert.Equal(typeof(VmCreateHelper.Fuga).FullName, fugaTypeRef.FullName);
            Assert.Equal(typeof(VmCreateHelper.Fuga).AssemblyQualifiedName, fugaTypeRef.QualifiedName);

            var fieldDumper =
                fuga.Members.First(e => e.Key == nameof(VmCreateHelper.Fuga.member1)).Value.First() as FieldDumpModel;
            Assert.Equal(MemberDumpKind.Field, fieldDumper.Kind);
            Assert.Equal(typeof(int).FullName, fieldDumper.TypeName.FullName);

            PropertyDumpModel propertyDumper =
                fuga.Members.First(e => e.Key == nameof(VmCreateHelper.Fuga.Member2)).Value
                    .First() as PropertyDumpModel;
            Assert.Equal(MemberDumpKind.Property, propertyDumper.Kind);
            Assert.Equal(typeof(string).FullName, propertyDumper.TypeName.FullName);

            MethodDumpModel methodDumper =
                fuga.Members.First(e => e.Key == nameof(VmCreateHelper.Fuga.Plus)).Value.First() as MethodDumpModel;
            Assert.Equal(MemberDumpKind.Method, methodDumper.Kind);
            Assert.Equal(typeof(int).FullName, methodDumper.TypeName.FullName);
            Assert.Equal(2, methodDumper.Parameters.Count);
            Assert.True(methodDumper.Parameters.All(e => e.Value.TypeName.FullName == typeof(int).FullName));

            MethodDumpModel methodDumper2 =
                fuga.Members.First(e => e.Key == nameof(VmCreateHelper.Fuga.DefaultAndRef)).Value.First() as
                    MethodDumpModel;
            Assert.Equal(MemberDumpKind.Method, methodDumper2.Kind);
            Assert.Equal(typeof(int).FullName, methodDumper2.TypeName.FullName);
            Assert.Equal(2, methodDumper2.Parameters.Count);
            Assert.True(methodDumper2.Parameters.ElementAt(0).Value.IsRef);
            Assert.False(methodDumper2.Parameters.ElementAt(0).Value.IsOut);
            Assert.Equal(typeof(int).MakeByRefType().FullName,
                methodDumper2.Parameters.ElementAt(0).Value.TypeName.FullName);
            Assert.True(methodDumper2.Parameters.ElementAt(1).Value.IsOptional);
            Assert.Equal(typeof(int).FullName, methodDumper2.Parameters.ElementAt(1).Value.TypeName.FullName);
            Assert.Equal(1234, Convert.ToInt32(methodDumper2.Parameters.ElementAt(1).Value.DefaultValue));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("test.json")]
        public void CreateFileDumpTest(string path)
        {
            var dumps = CreateFileDump(path);
            Assert.True(dumps.Types.Count > 0);

            DumpTest(dumps);
        }

        [Fact]
        public void CreateStringDumpTest()
        {
            var json = VmCreateHelper.Create("").CreateDumpWithString();
            DumpDeserializer deserializer = new DumpDeserializer();
            var dumps = deserializer.Deserialize(json);
            Assert.True(dumps.Types.Count > 0);

            DumpTest(dumps);
        }
    }
}