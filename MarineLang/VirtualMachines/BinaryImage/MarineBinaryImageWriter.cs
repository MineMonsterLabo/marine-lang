using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MarineLang.Models;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImageWriter : BinaryWriter
    {
        public ImageOptimization Optimization { get; set; }

        public MarineBinaryImageWriter(Stream input) : base(input)
        {
        }

        public MarineBinaryImageWriter(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public MarineBinaryImageWriter(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding,
            leaveOpen)
        {
        }

        public void WriteImage(ILGeneratedData data)
        {
            if (!Optimization.HasFlag(ImageOptimization.NoHeaderAndMeta))
            {
                WriteHeader();
                WriteVersion();

                WriteMetadata();
            }

            WriteNamespaceTable(data.namespaceTable);
            WriteMarineILs(data.marineILs);
        }

        private void WriteHeader()
        {
            Write(MarineBinaryImage.ImageHeader);
        }

        private void WriteVersion()
        {
            Write(MarineBinaryImage.IMAGE_VERSION);
        }

        private void WriteMetadata()
        {
            var metadata = new Dictionary<string, string>();

            var version =
                $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}";
            metadata[MarineBinaryImage.BUILD_MARINE_VERSION_KEY] = version;
            metadata[MarineBinaryImage.BUILD_TIME_STAMP_KEY] = DateTime.Now.Ticks.ToString();

            OverrideMetadata(metadata);

            this.Write7BitEncodedIntPolyfill(metadata.Count);
            foreach (var pair in metadata)
            {
                Write(pair.Key);
                Write(pair.Value);
            }
        }

        protected virtual void OverrideMetadata(Dictionary<string, string> metadata)
        {
            // Override metadata code here.
        }

        private void WriteNamespaceTable(NamespaceTable namespaceTable)
        {
            namespaceTable.WriteBinaryFormat(this);
        }

        protected virtual void WriteMarineILs(IReadOnlyList<IMarineIL> ilData)
        {
            this.Write7BitEncodedIntPolyfill(ilData.Count);
            foreach (var il in ilData)
            {
                WriteMarineIL(il);
            }
        }

        protected virtual void WriteMarineIL(IMarineIL il)
        {
            if (Optimization.HasFlag(ImageOptimization.NoDebug))
                if (il is PushDebugContextIL || il is PopDebugContextIL)
                    return;

            il.WriteMarineIL(this);
        }

        public virtual void Write(Type type)
        {
            Write(type.AssemblyQualifiedName);
        }

        public virtual void Write(MethodInfo methodInfo)
        {
            Write(methodInfo.DeclaringType);

            var parameters = methodInfo.GetParameters();
            Write7BitEncodedInt(parameters.Length);
            foreach (var parameter in parameters)
            {
                Write(parameter.ParameterType);
            }
        }

        public void Write(StackIndex stackIndex)
        {
            Write(stackIndex.isAbsolute);
            Write7BitEncodedInt(stackIndex.index);
        }

        public void Write(DebugContext debugContext)
        {
            Write(debugContext.ProgramUnitName);
            Write(debugContext.FuncName);
            Write(debugContext.RangePosition.Start);
            Write(debugContext.RangePosition.End);
        }

        public void Write(Position position)
        {
            Write7BitEncodedInt(position.column);
            Write7BitEncodedInt(position.line);
            Write7BitEncodedInt(position.index);
        }
    }
}