using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines.BinaryImage
{
    public class MarineBinaryImageWriter : BinaryWriter
    {
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
            WriteHeader();
            WriteVersion();

            WriteMetadata();

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
            
        }
    }
}