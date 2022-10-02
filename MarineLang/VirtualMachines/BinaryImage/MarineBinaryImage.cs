using System.IO;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImage
    {
        public const int IMAGE_VERSION = 1;
        public const string BUILD_MARINE_VERSION_KEY = "LanguageVersion";
        public const string BUILD_TIME_STAMP_KEY = "TimeStamp"; 
        
        public static readonly byte[] ImageHeader = new byte[] { 0x07, 0x1E, 0x20, 0x2F, 0x25, 0x69, 0x00, 0x00 };

        public static ILGeneratedData ReadImage(byte[] buffer)
        {
            var stream = new MemoryStream(buffer);
            var reader = new MarineBinaryImageReader(stream);
            var data = reader.ReadImage();
            reader.Dispose();
            stream.Dispose();

            return data;
        }

        public static byte[] WriteImage(ILGeneratedData data)
        {
            var stream = new MemoryStream();
            var writer = new MarineBinaryImageWriter(stream);
            writer.WriteImage(data);

            return stream.ToArray();
        }
    }
}