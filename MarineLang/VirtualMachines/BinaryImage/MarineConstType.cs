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

    public static class MarineConstTypeIOExtensions
    {
        public static void WriteConstValue(this MarineBinaryImageWriter writer, object value)
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

        public static object ReadConstValue(this MarineBinaryImageReader reader)
        {
            var type = (MarineConstType)reader.Read7BitEncodedIntPolyfill();
            switch (type)
            {
                case MarineConstType.Bool:
                    return reader.ReadBoolean();
                case MarineConstType.Int:
                    return reader.ReadInt32();
                case MarineConstType.Float:
                    return reader.ReadSingle();
                case MarineConstType.String:
                    return reader.ReadString();

                default:
                    throw new InvalidDataException();
            }
        }
    }
}