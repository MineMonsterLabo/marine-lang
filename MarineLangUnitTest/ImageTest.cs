using System;
using System.IO;
using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class ImageTest
    {
        [Fact]
        public void CreateImage()
        {
            var path = $"{Environment.CurrentDirectory}/image.mrnc";
            var vm = VmCreateHelper.Create("fun main() ret ret_123() end");
            var image = vm.CreateCompiledBinaryImage();

            var oldRet = vm.Run<int>("main");
            File.WriteAllBytes(path, image);

            var newVm = VmCreateHelper.CreateVM();
            newVm.LoadCompiledBinaryImage(image);

            var ret = newVm.Run<int>("main");
            Assert.Equal(oldRet.Value, ret.Value);
        }
    }
}