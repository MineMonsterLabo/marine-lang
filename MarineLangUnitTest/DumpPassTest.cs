using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.Dumps.Models;
using MarineLangUnitTest.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class DumpPassTest
    {
        internal IReadOnlyDictionary<string, ClassDumpModel> CreateDump(string path = null)
        {
            if (path == null)
                VmCreateHelper.Create("").CreateDump();
            else
                VmCreateHelper.Create("").CreateDump(path);

            if (path == null)
                path = $"{Environment.CurrentDirectory}/marine_dump.json";
            Assert.True(File.Exists(path));

            string json = File.ReadAllText(path);
            Assert.True(json.Length > 2);

            DumpDeserializer deserializer = new DumpDeserializer();
            return deserializer.Deserialize(json);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("test.json")]
        public void CreateDumpTest(string path)
        {
            var dumps = CreateDump(path);
            Assert.True(dumps.Count > 0);

            Assert.Equal(11, dumps["hoge"].Members.Length);
            Assert.Equal(nameof(VmCreateHelper.Hoge), dumps["hoge"].Type.Name);
            Assert.Equal(typeof(VmCreateHelper.Hoge).FullName, dumps["hoge"].Type.FullName);
            Assert.Equal(typeof(VmCreateHelper.Hoge).AssemblyQualifiedName, dumps["hoge"].Type.QualifiedName);


            Assert.Equal(9, dumps["fuga"].Members.Length);
            Assert.Equal(nameof(VmCreateHelper.Fuga), dumps["fuga"].Type.Name);
            Assert.Equal(typeof(VmCreateHelper.Fuga).FullName, dumps["fuga"].Type.FullName);
            Assert.Equal(typeof(VmCreateHelper.Fuga).AssemblyQualifiedName, dumps["fuga"].Type.QualifiedName);

            FieldDumpModel fieldDumper =
                dumps["fuga"].Members.First(e => e.Name == nameof(VmCreateHelper.Fuga.member1)) as FieldDumpModel;
            Assert.Equal(MemberDumpKind.Field, fieldDumper.Kind);
            Assert.Equal(typeof(int).FullName, fieldDumper.Type.FullName);

            PropertyDumpModel propertyDumper =
                dumps["fuga"].Members.First(e => e.Name == nameof(VmCreateHelper.Fuga.Member2)) as PropertyDumpModel;
            Assert.Equal(MemberDumpKind.Property, propertyDumper.Kind);
            Assert.Equal(typeof(string).FullName, propertyDumper.Type.FullName);

            MethodDumpModel methodDumper =
                dumps["fuga"].Members.First(e => e.Name == nameof(VmCreateHelper.Fuga.Plus)) as MethodDumpModel;
            Assert.Equal(MemberDumpKind.Method, methodDumper.Kind);
            Assert.Equal(typeof(int).FullName, methodDumper.RetType.FullName);
            Assert.Equal(2, methodDumper.Parameters.Length);
            Assert.True(methodDumper.Parameters.All(e => e.Type.FullName == typeof(int).FullName));

            MethodDumpModel methodDumper2 =
                dumps["fuga"].Members.First(e => e.Name == nameof(VmCreateHelper.Fuga.DefaultAndRef)) as
                    MethodDumpModel;
            Assert.Equal(MemberDumpKind.Method, methodDumper2.Kind);
            Assert.Equal(typeof(int).FullName, methodDumper2.RetType.FullName);
            Assert.Equal(2, methodDumper2.Parameters.Length);
            Assert.True(methodDumper2.Parameters.ElementAt(0).IsRef);
            Assert.False(methodDumper2.Parameters.ElementAt(0).IsOut);
            Assert.Equal(typeof(int).MakeByRefType().FullName, methodDumper2.Parameters.ElementAt(0).Type.FullName);
            Assert.True(methodDumper2.Parameters.ElementAt(1).IsOptional);
            Assert.Equal(typeof(int).FullName, methodDumper2.Parameters.ElementAt(1).Type.FullName);
            Assert.Equal(1234, Convert.ToInt32(methodDumper2.Parameters.ElementAt(1).DefaultValue));
        }
    }
}