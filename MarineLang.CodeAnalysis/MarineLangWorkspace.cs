using System;
using System.Collections.Generic;
using System.IO;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines.Dumps;
using MarineLang.VirtualMachines.Dumps.Models;

namespace MarineLang.CodeAnalysis
{
    public class MarineLangWorkspace
    {
        private readonly Dictionary<string, string> _textDocuments = new Dictionary<string, string>();

        private readonly List<MarineDumpModel> _dumpModels = new List<MarineDumpModel>();
        private readonly Dictionary<string, int> _dumpIndexes = new Dictionary<string, int>();

        public string RootFolder { get; }

        public Func<SyntaxAnalyzer> OnCreateSyntaxAnalyzer { private get; set; }

        public MarineLangWorkspace(string rootFolder)
        {
            RootFolder = rootFolder;
        }

        public string GetTextDocument(string fileName)
        {
            if (!_textDocuments.ContainsKey(fileName))
                return null;

            return _textDocuments[fileName];
        }

        public void SetTextDocument(string fileName, string document, int attachDumpId = -1)
        {
            _textDocuments[fileName] = document;

            if (attachDumpId != -1)
            {
                _dumpIndexes[fileName] = attachDumpId;
            }
        }

        public int LoadDumpFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return -1;
            }

            var deserializer = new DumpDeserializer();
            var model = deserializer.Deserialize(File.ReadAllText(fileName));
            _dumpModels.Add(model);

            return _dumpModels.Count;
        }

        public CompletionContext GetCompletionContext(string fileName)
        {
            if (!_textDocuments.ContainsKey(fileName))
            {
                return null;
            }

            var text = _textDocuments[fileName];
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var analyzer = OnCreateSyntaxAnalyzer?.Invoke() ?? new SyntaxAnalyzer();
            var result = analyzer.Parse(tokens);

            MarineDumpModel model = null;
            if (_dumpIndexes.ContainsKey(fileName))
            {
                var index = _dumpIndexes[fileName];
                if (index != -1)
                {
                    model = _dumpModels[index];
                }
            }

            return new CompletionContext(result, model);
        }

        public CompletionContext GetCompletionContext(string fileName, CompletionContext oldContext)
        {
            if (!_textDocuments.ContainsKey(fileName))
            {
                return null;
            }

            var text = _textDocuments[fileName];
            var tokens = new LexicalAnalyzer().GetTokens(text);
            var analyzer = OnCreateSyntaxAnalyzer?.Invoke() ?? new SyntaxAnalyzer();
            var result = analyzer.Parse(tokens);

            return new CompletionContext(result, oldContext.CodeAnalyzer);
        }
    }
}