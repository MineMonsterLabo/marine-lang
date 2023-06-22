using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class ImageTest
    {
        [Fact]
        public void CreateImage()
        {
            var vm = VmCreateHelper.Create("fun main() ret ret_123() end");
            var image = vm.CreateCompiledBinaryImage();
        }
    }
}