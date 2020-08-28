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

        public static Parser<object[]> Parsers(params Parser<object>[] parsers)
        {
            var values = new object[parsers.Length];

            return
                stream =>
                {
                    for (var i = 0; i < parsers.Length; i++)
                    {
                        var result = parsers[i](stream);
                        if (result.IsError)
                            return result.CastError<object[]>();
                        if (stream.IsEnd)
                            return ParseResult<object[]>.CreateError(new Error(ErrorKind.InComplete));
                        values[i] = result.Value;
                    }
                    return ParseResult<object[]>.CreateSuccess(values);
                };
        }

        public static Parser<(T1, T2)> Tuple<T1, T2>(Parser<T1> parser1, Parser<T2> parser2)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1]));
        }

        public static Parser<(T1, T2, T3)> Tuple<T1, T2, T3>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2]));
        }

        public static Parser<(T1, T2, T3, T4)> Tuple<T1, T2, T3, T4>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3]));
        }

        public static Parser<(T1, T2, T3, T4, T5)> Tuple<T1, T2, T3, T4, T5>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>, parser5 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4]));
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
