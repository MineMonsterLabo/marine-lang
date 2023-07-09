namespace MarineLang.VirtualMachines.BinaryImage
{
    public static class MarineBinaryImageConstants
    {
        public const ushort IMAGE_VERSION = 1;
        public const string BUILD_MARINE_VERSION_KEY = "LanguageVersion";
        public const string BUILD_TIME_STAMP_KEY = "TimeStamp";
        
        public static readonly byte[] ImageHeader = new byte[] { 0x07, 0x1E, 0x20, 0x2F, 0x25, 0x69, 0x00, 0x00 };
    }
}