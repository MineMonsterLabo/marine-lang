using System.IO;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public enum MarineConstType
    {
        Bool,
        Int,
        Float,
        String
    }

    public static class MarineConstTypeExtensions
    {
        public static void WriteConstValue(this object value, MarineBinaryImageWriter writer)
        {
            switch (value)
            {
                case bool b:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Bool);
                    writer.Write(b);
                    break;
                case int i:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Int);
                    writer.Write7BitEncodedIntPolyfill(i);
                    break;
                case float f:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Float);
                    writer.Write(f);
                    break;
                case string s:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.String);
                    writer.Write(s);
                    break;

                default:
                    throw new InvalidDataException();
            }
        }
    }
}