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
            var source = $"fun main(i){nl}if i > 0 {{ let a = 5 }}{nl}end";

            MarineLangWorkspace workspace = new MarineLangWorkspace(string.Empty);
            workspace.SetTextDocument("test.mrn", source);
            var context = workspace.GetCompletionContext("test.mrn");
            var list = context.GetCompletions(new Position(2, 9)).ToArray();
        }
    }
}