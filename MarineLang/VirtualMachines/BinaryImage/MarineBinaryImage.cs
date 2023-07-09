using System.IO;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public static class MarineBinaryImage
    {
        public static ILGeneratedData ReadImage(byte[] buffer, ImageOptimization optimization)
        {
            var stream = new MemoryStream(buffer);
            var reader = new MarineBinaryImageReader(stream)
            {
                Optimization = optimization
            };
            var data = reader.ReadImage();
            reader.Dispose();
            stream.Dispose();

            return data;
        }

        public static byte[] WriteImage(ILGeneratedData data, ImageOptimization optimization)
        {
            var stream = new MemoryStream();
            var writer = new MarineBinaryImageWriter(stream)
            {
                Optimization = optimization
            };
            writer.WriteImage(data);

            var image = stream.ToArray();
            writer.Dispose();
            stream.Dispose();

            return image;
        }
    }
}