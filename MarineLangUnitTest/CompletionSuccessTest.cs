using System;
using System.Linq;
using MarineLang.CodeAnalysis;
using MarineLang.Models;
using Xunit;

namespace MarineLangUnitTest
{
    public class CompletionSuccessTest
    {
        private const int KeywordCount = 6;

        [Theory]
        [InlineData("fun main(i★) if i > 0 { let a = 5 } end", 0)]
        [InlineData("fun main(i) if i > 0★ { let a = 5 } end", KeywordCount + 2)]
        [InlineData("fun main(i, j) if i > 0★ { let a = 5 } end fun main2(i) ret 0 end", KeywordCount + 4)]
        [InlineData("fun main(i, j) if i > 0 { let a = 5★ } end fun main2(i) ret 0 end", KeywordCount + 4)]
        // [InlineData("fun main(i, j) if i > 0 { let a = ★ } end fun main2(i) ret 0 end", KeywordCount + 4)]
        public void Test(string source, int listCount)
        {
            var nl = Environment.NewLine;

            var position = GetMarkerPosition(source, out source);
            Assert.NotNull(position);

            MarineLangWorkspace workspace = new MarineLangWorkspace(string.Empty);
            workspace.SetTextDocument("test.mrn", source);
            var context = workspace.GetCompletionContext("test.mrn");
            var list = context.GetCompletions(position.Value).ToArray();
            Assert.NotNull(list);
            Assert.Equal(listCount, list.Length);
        }

        private Position? GetMarkerPosition(string source, out string replaceSource, string marker = "★")
        {
            Position? position = null;
            var lines = source.Split(Environment.NewLine);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var index = line.IndexOf(marker, StringComparison.Ordinal);
                if (index != -1)
                {
                    position = new Position(0, i + 1, index + 1);
                    break;
                }
            }

            replaceSource = source.Replace(marker, string.Empty);

            return position;
        }
    }
}