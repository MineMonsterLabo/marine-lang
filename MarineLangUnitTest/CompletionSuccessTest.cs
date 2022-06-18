using System;
using System.Linq;
using MarineLang.CodeAnalysis;
using MarineLang.Models;
using Xunit;

namespace MarineLangUnitTest
{
    public class CompletionSuccessTest
    {
        [Fact]
        public void Test()
        {
            var nl = Environment.NewLine;
            var source = $"fun main(i){nl}if i > 0★ {{ let a = 5 }}{nl}end";

            var position = GetMarkerPosition(source);
            Assert.NotNull(position);

            source = source.Replace("★", string.Empty);

            const int keywordCount = 6;
            MarineLangWorkspace workspace = new MarineLangWorkspace(string.Empty);
            workspace.SetTextDocument("test.mrn", source);
            var context = workspace.GetCompletionContext("test.mrn");
            var list = context.GetCompletions(position.Value).ToArray();
            Assert.NotNull(list);
            Assert.Equal(keywordCount + 2, list.Length);
        }

        private Position? GetMarkerPosition(string source, string marker = "★")
        {
            Position? position = null;
            var lines = source.Split(Environment.NewLine);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var index = line.IndexOf(marker, StringComparison.Ordinal);
                if (index != -1)
                {
                    position = new Position(i + 1, index + 1);
                }
            }

            return position;
        }
    }
}