using System;
using System.IO;
using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class DumpPassTest
    {
        internal void CreateDump(string path = null)
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
        }
    }
}