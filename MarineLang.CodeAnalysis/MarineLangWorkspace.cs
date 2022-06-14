using System;
using System.Collections.Generic;
using System.IO;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using Newtonsoft.Json.Linq;

namespace MarineLang.CodeAnalysis
{
    public class MarineLangWorkspace
    {
        private readonly Dictionary<string, string> _textDocuments = new Dictionary<string, string>();

        private JObject _configuraion;
        private string _dumpFilePath;

        public string RootFolder { get; private set; }

        public Func<SyntaxAnalyzer> OnCreateSyntaxAnalyzer { private get; set; }

        public MarineLangWorkspace()
        {
        }

        public void SetRootFolder(string folder)
        {
            RootFolder = folder;
        }

        public void UpdateTextDocument(string fileName, string document)
        {
            _textDocuments[fileName] = document;
        }

        public void LoadConfiguration(string fileName)
        {
            if (File.Exists(fileName))
            {
                _configuraion = JObject.Parse(File.ReadAllText(fileName));
                if (_configuraion.TryGetValue("dumpFilePath", out var dumpFilePath))
                {
                    _dumpFilePath = dumpFilePath.Value<string>();
                }
            }
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

            return new CompletionContext(result);
        }
    }
}