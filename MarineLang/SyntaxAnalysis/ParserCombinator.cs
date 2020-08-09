using MarineLang.Streams;
using System;
using System.Collections.Generic;

namespace MarineLang.SyntaxAnalysis
{
    public static class ParserCombinator
    {
        public static Func<TokenStream, IParseResult<IEnumerable<T>>> Many<T>(Func<TokenStream, IParseResult<T>> parser)
        {
            return
                stream =>
                {
                    var list = new List<T>();
                    while (stream.IsEnd == false)
                    {
                        var parseResult = parser(stream);

                        if (parseResult.IsError)
                            return ParseResult<IEnumerable<T>>.Error("");

                        list.Add(parseResult.Value);
                    }
                    return ParseResult<IEnumerable<T>>.Success(list);
                };
        }

        public static Func<TokenStream, IParseResult<T>> Try<T>(Func<TokenStream, IParseResult<T>> parser)
        {
            return
                stream =>
                {
                    var backUpIndex = stream.Index;
                    var parseResult = parser(stream);

                    if (parseResult.IsError)
                        stream.SetIndex(backUpIndex);

                    return parseResult;
                };
        }

        public static Func<TokenStream, IParseResult<T>> Or<T>
            (params Func<TokenStream, IParseResult<T>>[] parsers)
        {
            return
                stream =>
                {
                    var parseResult = ParseResult<T>.Error("");

                    foreach (var parser in parsers)
                    {
                        parseResult = parser(stream);

                        if (parseResult.IsError == false)
                            return parseResult;
                    }
                    return parseResult;
                };
        }
    }
}
