using MarineLang.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public static class ParserCombinator
    {
        public static Parser<List<T>> Many<T>(Parser<T> parser)
        {
            return
                stream =>
                {
                    var list = new List<T>();
                    while (stream.IsEnd == false)
                    {
                        var parseResult = parser(stream);
                        if (parseResult.IsError && parseResult.Error.ErrorKind != ErrorKind.InComplete)
                            return parseResult.CastError<List<T>>();
                        if (parseResult.IsError)
                            break;
                        list.Add(parseResult.Value);
                    }
                    return ParseResult<List<T>>.CreateSuccess(list);
                };
        }

        public static Parser<List<T>> OneMany<T>(Parser<T> parser)
        {
            return
                stream =>
                {
                    var result = Many(parser)(stream);
                    if (result.IsError == false && result.Value.Count == 0)
                        return ParseResult<List<T>>.CreateError(new Error(ErrorKind.InComplete));
                    return result;
                };
        }

        public static Parser<(T, TT)> Pair<T, TT>(Parser<T> parser1, Parser<TT> parser2)
        {
            return
                stream =>
                {
                    var result1 = parser1(stream);
                    if (result1.IsError)
                        return result1.CastError<(T, TT)>();
                    if (stream.IsEnd)
                        return ParseResult<(T, TT)>.CreateError(new Error(ErrorKind.InComplete));
                    var result2 = parser2(stream);
                    if (result2.IsError)
                        return result2.CastError<(T, TT)>();
                    return ParseResult<(T, TT)>.CreateSuccess((result1.Value, result2.Value));
                };
        }

        public static Parser<T> Try<T>(Parser<T> parser)
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

        public static Parser<T> Or<T>(params Parser<T>[] parsers)
        {
            return
                stream =>
                {
                    foreach (var parser in parsers)
                    {
                        var parseResult = parser(stream);
                        if (parseResult.IsError == false || parseResult.Error.ErrorKind == ErrorKind.ForceError)
                            return parseResult;
                    }
                    return ParseResult<T>.CreateError(new Error(ErrorKind.InComplete)); ;
                };
        }

        public static Parser<T[]> Separated<T, TT>(Parser<T> parser, Parser<TT> separateParser)
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
                        return ParseResult<T[]>.CreateError(new Error(ErrorKind.InComplete));
                    isFirst = false;
                    if (result.IsError)
                        break;
                    list.Add(result.Value);
                }
                return ParseResult<T[]>.CreateSuccess(list.ToArray());
            };
        }

        public static Parser<Token> TestOnce(Func<Token, bool> test)
        {
            return stream =>
            {
                var token = stream.Current;
                if (test(token))
                {
                    stream.MoveNext();
                    return ParseResult<Token>.CreateSuccess(token);
                }
                return ParseResult<Token>.CreateError(new Error(ErrorKind.InComplete));
            };
        }
    }
}
