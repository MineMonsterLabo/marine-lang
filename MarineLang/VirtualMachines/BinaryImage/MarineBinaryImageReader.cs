using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MarineLang.Models;
using MarineLang.VirtualMachines.MarineILs;
using MineUtil;

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
            var count = this.Read7BitEncodedIntPolyfill();
            var dict = new Dictionary<string, string>(count);
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
        
        public IMarineIL ReadMarineIL()
        {
            var ilType = (MarineILType)this.Read7BitEncodedIntPolyfill();
            switch (ilType)
            {
                case MarineILType.NoOpIL:
                    return new NoOpIL();
                case MarineILType.StaticCSharpFieldLoadIL:
                {
                    var type = ReadType();
                    var fieldName = ReadString();
                    return new StaticCSharpFieldLoadIL(type, fieldName);
                }
                case MarineILType.StaticCSharpFieldStoreIL:
                {
                    var type = ReadType();
                    var fieldName = ReadString();
                    return new StaticCSharpFieldStoreIL(type, fieldName);
                }
                case MarineILType.InstanceCSharpFieldLoadIL:
                {
                    var fieldName = ReadString();
                    return new InstanceCSharpFieldLoadIL(fieldName);
                }
                case MarineILType.InstanceCSharpFieldStoreIL:
                {
                    var fieldName = ReadString();
                    return new InstanceCSharpFieldStoreIL(fieldName);
                }
                case MarineILType.CSharpFuncCallIL:
                {
                    var methodInfo = ReadMethodInfo();
                    var argCount = this.Read7BitEncodedIntPolyfill();
                    return new CSharpFuncCallIL(methodInfo, argCount);
                }
                case MarineILType.StaticCSharpFuncCallIL:
                {
                    var type = ReadType();
                    var funcName = ReadString();
                    var methodCount = this.Read7BitEncodedIntPolyfill();
                    var methods = new MethodInfo[methodCount];
                    for (int i = 0; i < methodCount; i++)
                    {
                        methods[i] = ReadMethodInfo();
                    }

                    var typeCount = this.Read7BitEncodedIntPolyfill();
                    var types = new Type[typeCount];
                    for (int i = 0; i < typeCount; i++)
                    {
                        types[i] = ReadType();
                    }

                    var argCount = this.Read7BitEncodedIntPolyfill();

                    return new StaticCSharpFuncCallIL(type, methods, funcName, argCount, types);
                }
                case MarineILType.InstanceCSharpFuncCallIL:
                {
                    var funcName = ReadString();

                    Type[] types = null;
                    var hasGenericTypes = ReadBoolean();
                    if (hasGenericTypes)
                    {
                        var typeCount = this.Read7BitEncodedIntPolyfill();
                        types = new Type[typeCount];
                        for (int i = 0; i < typeCount; i++)
                        {
                            types[i] = ReadType();
                        }
                    }

                    var argCount = this.Read7BitEncodedIntPolyfill();

                    return new InstanceCSharpFuncCallIL(funcName, argCount, types);
                }
                case MarineILType.MarineFuncCallIL:
                {
                    var funcName = ReadString();
                    var ilIndex = new FuncILIndex
                    {
                        Index = this.Read7BitEncodedIntPolyfill()
                    };
                    var argCount = this.Read7BitEncodedIntPolyfill();
                    return new MarineFuncCallIL(funcName, ilIndex, argCount);
                }
                case MarineILType.StaticCSharpConstructorCallIL:
                {
                    var type = ReadType();
                    var funcName = ReadString();

                    var constructorCount = this.Read7BitEncodedIntPolyfill();
                    var constructors = new ConstructorInfo[constructorCount];
                    for (int i = 0; i < constructorCount; i++)
                    {
                        constructors[i] = ReadConstructorInfo();
                    }

                    var argCount = this.Read7BitEncodedIntPolyfill();
                    return new StaticCSharpConstructorCallIL(type, constructors, funcName, argCount);
                }
                case MarineILType.MoveNextIL:
                    return new MoveNextIL();
                case MarineILType.GetIterCurrentIL:
                    return new GetIterCurrentIL();
                case MarineILType.InstanceCSharpIndexerLoadIL:
                    return new InstanceCSharpIndexerLoadIL();
                case MarineILType.InstanceCSharpIndexerStoreIL:
                    return new InstanceCSharpIndexerStoreIL();
                case MarineILType.BinaryOpIL:
                {
                    var opKind = (TokenType)this.Read7BitEncodedIntPolyfill();
                    return new BinaryOpIL(opKind);
                }
                case MarineILType.UnaryOpIL:
                {
                    var opKind = (TokenType)this.Read7BitEncodedIntPolyfill();
                    return new UnaryOpIL(opKind);
                }
                case MarineILType.RetIL:
                {
                    var argCount = this.Read7BitEncodedIntPolyfill();
                    return new RetIL(argCount);
                }
                case MarineILType.JumpFalseIL:
                {
                    var ilIndex = this.Read7BitEncodedIntPolyfill();
                    return new JumpFalseIL(ilIndex);
                }
                case MarineILType.JumpFalseNoPopIL:
                {
                    var ilIndex = this.Read7BitEncodedIntPolyfill();
                    return new JumpFalseNoPopIL(ilIndex);
                }
                case MarineILType.JumpTrueNoPopIL:
                {
                    var ilIndex = this.Read7BitEncodedIntPolyfill();
                    return new JumpTrueNoPopIL(ilIndex);
                }
                case MarineILType.JumpIL:
                {
                    var ilIndex = this.Read7BitEncodedIntPolyfill();
                    return new JumpIL(ilIndex);
                }
                case MarineILType.BreakIL:
                {
                    var breakIndex = new BreakIndex
                    {
                        Index = this.Read7BitEncodedIntPolyfill()
                    };
                    return new BreakIL(breakIndex);
                }
                // case MarineILType.StoreValueIL:
                //     return new StoreValueIL();
                case MarineILType.PushValueIL:
                {
                    var value = this.ReadConstValue();
                    return new PushValueIL(value);
                }
                case MarineILType.StoreIL:
                {
                    var stackIndex = ReadStackIndex();
                    return new StoreIL(stackIndex);
                }
                case MarineILType.LoadIL:
                {
                    var stackIndex = ReadStackIndex();
                    return new LoadIL(stackIndex);
                }
                case MarineILType.PopIL:
                    return new PopIL();
                case MarineILType.CreateArrayIL:
                {
                    var initSize = this.Read7BitEncodedIntPolyfill();
                    var size = this.Read7BitEncodedIntPolyfill();
                    return new CreateArrayIL(initSize, size);
                }
                case MarineILType.StackAllocIL:
                {
                    var size = this.Read7BitEncodedIntPolyfill();
                    return new StackAllocIL(size);
                }
                case MarineILType.YieldIL:
                    return new YieldIL();
                case MarineILType.PushYieldCurrentRegisterIL:
                    return new PushYieldCurrentRegisterIL();
                case MarineILType.PushDebugContextIL:
                {
                    var debugContext = ReadDebugContext();
                    return new PushDebugContextIL(debugContext);
                }
                case MarineILType.PopDebugContextIL:
                    return new PopDebugContextIL();
                default:
                    throw new InvalidDataException();
            }
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

        public object ReadConstValue()
        {
            var type = (MarineConstType)this.Read7BitEncodedIntPolyfill();
            switch (type)
            {
                case MarineConstType.Bool:
                    return ReadBoolean();
                case MarineConstType.Int:
                    return this.Read7BitEncodedIntPolyfill();
                case MarineConstType.Float:
                    return ReadSingle();
                case MarineConstType.Char:
                    return ReadChar();
                case MarineConstType.String:
                    return ReadString();
                case MarineConstType.Enum:
                    var enumType = ReadType();
                    var value = this.Read7BitEncodedInt64Polyfill();
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