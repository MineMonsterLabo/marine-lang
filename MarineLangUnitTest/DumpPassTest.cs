using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.Dumps.Members;
using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class DumpPassTest
    {
        internal IReadOnlyDictionary<string, ClassDumper> CreateDump(string path = null)
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

            return DumpHelper.FromJson(json);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("test.json")]
        public void CreateDumpTest(string path)
        {
            var dumps = CreateDump(path);
            Assert.True(dumps.Count > 0);

            Assert.Equal(9, dumps["hoge"].MemberDumpers.Count);
            Assert.Equal(nameof(VmCreateHelper.Hoge), dumps["hoge"].Type.Name);
            Assert.Equal(typeof(VmCreateHelper.Hoge).FullName, dumps["hoge"].Type.FullName);
            Assert.Equal(typeof(VmCreateHelper.Hoge).AssemblyQualifiedName, dumps["hoge"].Type.QualifiedName);


            Assert.Equal(9, dumps["fuga"].MemberDumpers.Count);
            Assert.Equal(nameof(VmCreateHelper.Fuga), dumps["fuga"].Type.Name);
            Assert.Equal(typeof(VmCreateHelper.Fuga).FullName, dumps["fuga"].Type.FullName);
            Assert.Equal(typeof(VmCreateHelper.Fuga).AssemblyQualifiedName, dumps["fuga"].Type.QualifiedName);

            FieldDumper fieldDumper =
                dumps["fuga"].MemberDumpers.First(e => e.Name == nameof(VmCreateHelper.Fuga.member1)) as FieldDumper;
            Assert.Equal(DumpMemberKind.Field, fieldDumper.MemberKind);
            Assert.Equal(typeof(int).FullName, fieldDumper.Type.FullName);

            PropertyDumper propertyDumper =
                dumps["fuga"].MemberDumpers.First(e => e.Name == nameof(VmCreateHelper.Fuga.Member2)) as PropertyDumper;
            Assert.Equal(DumpMemberKind.Property, propertyDumper.MemberKind);
            Assert.Equal(typeof(string).FullName, propertyDumper.Type.FullName);

            MethodDumper methodDumper =
                dumps["fuga"].MemberDumpers.First(e => e.Name == nameof(VmCreateHelper.Fuga.Plus)) as MethodDumper;
            Assert.Equal(DumpMemberKind.Method, methodDumper.MemberKind);
            Assert.Equal(typeof(int).FullName, methodDumper.RetType.FullName);
            Assert.Equal(2, methodDumper.Parameters.Count);
            Assert.True(methodDumper.Parameters.All(e => e.Type.FullName == typeof(int).FullName));

            MethodDumper methodDumper2 =
                dumps["fuga"].MemberDumpers.First(e => e.Name == nameof(VmCreateHelper.Fuga.DefaultAndRef)) as
                    MethodDumper;
            Assert.Equal(DumpMemberKind.Method, methodDumper2.MemberKind);
            Assert.Equal(typeof(int).FullName, methodDumper2.RetType.FullName);
            Assert.Equal(2, methodDumper2.Parameters.Count);
            Assert.True(methodDumper2.Parameters.ElementAt(0).IsRef);
            Assert.False(methodDumper2.Parameters.ElementAt(0).IsOut);
            Assert.Equal(typeof(int).MakeByRefType().FullName, methodDumper2.Parameters.ElementAt(0).Type.FullName);
            Assert.True(methodDumper2.Parameters.ElementAt(1).IsOptional);
            Assert.Equal(typeof(int).FullName, methodDumper2.Parameters.ElementAt(1).Type.FullName);
            Assert.Equal(1234, Convert.ToInt32(methodDumper2.Parameters.ElementAt(1).Value));
        }
    }
}