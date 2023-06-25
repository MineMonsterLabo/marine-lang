using System.IO;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImage
    {
        public const ushort IMAGE_VERSION = 1;
        public const string BUILD_MARINE_VERSION_KEY = "LanguageVersion";
        public const string BUILD_TIME_STAMP_KEY = "TimeStamp";

        public static readonly byte[] ImageHeader = new byte[] { 0x07, 0x1E, 0x20, 0x2F, 0x25, 0x69, 0x00, 0x00 };

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

            return stream.ToArray();
        }
    }
}