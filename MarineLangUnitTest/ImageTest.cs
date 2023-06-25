using System;
using System.IO;
using MarineLang.VirtualMachines.BinaryImage;
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

            File.WriteAllBytes(path, image);

            Assert.True(image.Length > 0);
        }

        [Fact]
        public void CreateAndLoadImage()
        {
            var path = $"{Environment.CurrentDirectory}/image.mrnc";
            var vm = VmCreateHelper.Create("fun main() ret ret_123() end");
            var image = vm.CreateCompiledBinaryImage();

            Assert.True(image.Length > 0);

            var oldRet = vm.Run<int>("main");
            File.WriteAllBytes(path, image);

            var newVm = VmCreateHelper.CreateVM();
            newVm.LoadCompiledBinaryImage(image);

            var ret = newVm.Run<int>("main");
            Assert.Equal(oldRet.Value, ret.Value);
        }

        [Fact]
        public void CreateAndLoadOptimizeNoDebug()
        {
            var option = ImageOptimization.NoDebug;
            var path = $"{Environment.CurrentDirectory}/image_opt_no_debug.mrnc";
            var vm = VmCreateHelper.Create("fun main() ret ret_123() end");
            var image = vm.CreateCompiledBinaryImage(option);

            Assert.True(image.Length > 0);

            var oldRet = vm.Run<int>("main");
            File.WriteAllBytes(path, image);

            var newVm = VmCreateHelper.CreateVM();
            newVm.LoadCompiledBinaryImage(image, option);

            var ret = newVm.Run<int>("main");
            Assert.Equal(oldRet.Value, ret.Value);
        }

        [Fact]
        public void CreateAndLoadOptimizeNoHeader()
        {
            var option = ImageOptimization.NoHeaderAndMeta;
            var path = $"{Environment.CurrentDirectory}/image_opt_no_header.mrnc";
            var vm = VmCreateHelper.Create("fun main() ret ret_123() end");
            var image = vm.CreateCompiledBinaryImage(option);

            Assert.True(image.Length > 0);

            var oldRet = vm.Run<int>("main");
            File.WriteAllBytes(path, image);

            var newVm = VmCreateHelper.CreateVM();
            newVm.LoadCompiledBinaryImage(image, option);

            var ret = newVm.Run<int>("main");
            Assert.Equal(oldRet.Value, ret.Value);
        }

        [Fact]
        public void CreateAndLoadOptimizeNoDebugAndNoHeader()
        {
            var option = ImageOptimization.NoDebug | ImageOptimization.NoHeaderAndMeta;
            var path = $"{Environment.CurrentDirectory}/image_opt_no_debug_and_header.mrnc";
            var vm = VmCreateHelper.Create("fun main() ret ret_123() end");
            var image = vm.CreateCompiledBinaryImage(option);

            Assert.True(image.Length > 0);

            var oldRet = vm.Run<int>("main");
            File.WriteAllBytes(path, image);

            var newVm = VmCreateHelper.CreateVM();
            newVm.LoadCompiledBinaryImage(image, option);

            var ret = newVm.Run<int>("main");
            Assert.Equal(oldRet.Value, ret.Value);
        }
    }
}