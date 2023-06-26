using System;
using System.IO;
using MineUtil;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public enum MarineConstType
    {
        Bool,
        Int,
        Float,
        Char,
        String,
        Enum,
        Null = 126,
        Unit = 127
    }

    public static class MarineConstTypeIOExtensions
    {
        public static void WriteConstValue(this MarineBinaryImageWriter writer, object value)
        {
            if (value == null)
            {
                writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Null);
                return;
            }

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
                case char c:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Char);
                    writer.Write(c);
                    break;
                case string s:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.String);
                    writer.Write(s);
                    break;
                case Enum e:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Enum);
                    writer.Write(e.GetType());
                    writer.Write7BitEncodedInt64Polyfill(Convert.ToInt64(e));
                    break;
                case Unit _:
                    writer.Write7BitEncodedIntPolyfill((int)MarineConstType.Unit);
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
                    return reader.Read7BitEncodedIntPolyfill();
                case MarineConstType.Float:
                    return reader.ReadSingle();
                case MarineConstType.Char:
                    return reader.ReadChar();
                case MarineConstType.String:
                    return reader.ReadString();
                case MarineConstType.Enum:
                    var enumType = reader.ReadType();
                    var value = reader.Read7BitEncodedInt64Polyfill();
                    return Enum.ToObject(enumType, value);
                case MarineConstType.Null:
                    return null;
                case MarineConstType.Unit:
                    return Unit.Value;

                default:
                    throw new InvalidDataException();
            }
        }
    }
}