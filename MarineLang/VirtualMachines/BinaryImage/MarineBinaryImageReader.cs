using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImageReader : BinaryReader
    {
        public MarineBinaryImageReader(Stream input) : base(input)
        {
        }

        public MarineBinaryImageReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public MarineBinaryImageReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding,
            leaveOpen)
        {
        }

        public ILGeneratedData ReadImage()
        {
            if (!ReadHeader())
                throw new InvalidDataException("Image header not match.");

            var version = ReadUInt16();
            ValidateVersion(version);

            var metadata = ReadMetadata();
            ValidateMetadata(metadata);

            var namespaceTable = ReadNamespaceTable();
            var ilData = ReadMarineILs();

            return new ILGeneratedData(namespaceTable, ilData);
        }

        private bool ReadHeader()
        {
            var constantHeader = MarineBinaryImage.ImageHeader;
            var header = ReadBytes(constantHeader.Length);
            return header.SequenceEqual(constantHeader);
        }

        protected virtual void ValidateVersion(ushort version)
        {
            // Compatibility code here.
        }

        private IReadOnlyDictionary<string, string> ReadMetadata()
        {
            var dict = new Dictionary<string, string>();
            var count = this.Read7BitEncodedIntPolyfill();
            for (int i = 0; i < count; i++)
            {
                var key = ReadString();
                var value = ReadString();
                dict[key] = value;
            }

            return dict;
        }

        protected virtual void ValidateMetadata(IReadOnlyDictionary<string, string> metadata)
        {
            // Validate code here.
        }

        private NamespaceTable ReadNamespaceTable()
        {
            var namespaceTable = new NamespaceTable();
            namespaceTable.ReadBinaryFormat(this);

            return namespaceTable;
        }

        protected virtual IReadOnlyList<IMarineIL> ReadMarineILs()
        {
            List<IMarineIL> ILs = new List<IMarineIL>();

            return ILs;
        }
    }
}