using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MarineLang.Models;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImageReader : BinaryReader
    {
        public ImageOptimization Optimization { get; set; }

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
            if (!Optimization.HasFlag(ImageOptimization.NoHeaderAndMeta))
            {
                if (!ReadHeader())
                    throw new InvalidDataException("Image header not match.");

                var version = ReadUInt16();
                ValidateVersion(version);

                var metadata = ReadMetadata();
                ValidateMetadata(metadata);
            }

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
            List<IMarineIL> iLs = new List<IMarineIL>();

            var count = this.Read7BitEncodedIntPolyfill();
            for (int i = 0; i < count; i++)
            {
                var il = this.ReadMarineIL();
                iLs.Add(il);
            }

            return iLs;
        }

        public virtual Type ReadType()
        {
            return Type.GetType(ReadString());
        }

        public virtual MethodInfo ReadMethodInfo()
        {
            var type = ReadType();
            var name = ReadString();

            var count = this.Read7BitEncodedIntPolyfill();
            var parameters = new Type[count];
            for (int i = 0; i < count; i++)
            {
                var isGenericParam = ReadBoolean();
                if (!isGenericParam)
                    parameters[i] = ReadType();
            }

            return type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.InvokeMethod, null, parameters, null);
        }

        public virtual ConstructorInfo ReadConstructorInfo()
        {
            var type = ReadType();

            var count = this.Read7BitEncodedIntPolyfill();
            var parameters = new Type[count];
            for (int i = 0; i < count; i++)
            {
                var isGenericParam = ReadBoolean();
                if (!isGenericParam)
                    parameters[i] = ReadType();
            }

            return type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.CreateInstance, null, parameters, null);
        }

        public StackIndex ReadStackIndex()
        {
            var isAbsolute = ReadBoolean();
            var index = this.Read7BitEncodedIntPolyfill();
            return new StackIndex(index, isAbsolute);
        }

        public DebugContext ReadDebugContext()
        {
            var programUnitName = ReadString();
            var funcName = ReadString();
            var range = new RangePosition(ReadPosition(), ReadPosition());
            return new DebugContext(programUnitName, funcName, range);
        }

        public Position ReadPosition()
        {
            var column = this.Read7BitEncodedIntPolyfill();
            var line = this.Read7BitEncodedIntPolyfill();
            var index = this.Read7BitEncodedIntPolyfill();
            return new Position(index, line, column);
        }
    }
}