using MarineLang.Models;
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
                            return parseResult.CastError<IEnumerable<T>>();

                        list.Add(parseResult.Value);
                    }
                    return ParseResult<IEnumerable<T>>.CreateSuccess(list);
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
                    var parseResult = ParseResult<T>.CreateError(new Error());

                    foreach (var parser in parsers)
                    {
                        parseResult = parser(stream);

                        if (parseResult.IsError == false)
                            return parseResult;
                    }
                    return parseResult;
                };
        }

        public static Func<TokenStream, IParseResult<T[]>> Separated<T, TT>
            (Func<TokenStream, IParseResult<T>> parser, Func<TokenStream, IParseResult<TT>> separateParser)
        {
            return stream =>
            {
                var isFirst = true;
                var list = new List<T>();

                while (stream.IsEnd == false)
                {
                    if (isFirst == false && separateParser(stream).IsError)
                        break;
                    var result = parser(stream);
                    if (result.IsError && isFirst == false)
                        return ParseResult<T[]>.CreateError(new Error());
                    isFirst = false;
                    if (result.IsError)
                        break;
                    list.Add(result.Value);
                }
                return ParseResult<T[]>.CreateSuccess(list.ToArray());
            };
        }

        public static Func<TokenStream, IParseResult<Token>> TestOnce(Func<Token, bool> test)
        {
            return stream =>
            {
                if (test(stream.Current))
                {
                    var token = stream.Current;
                    stream.MoveNext();
                    return ParseResult<Token>.CreateSuccess(token);
                }
                return ParseResult<Token>.CreateError(new Error("", ErrorKind.InComplete));
            };
        }
    }
}
