using MarineLang.Models;
using MarineLang.Models.Errors;
using System;
using System.Collections.Generic;

namespace MarineLang.ParserCore
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
                        if (parseResult.IsError && parseResult.RawError.ErrorKind != ErrorKind.InComplete)
                            return ParseResult.Error<List<T>>(parseResult.RawError);
                        if (parseResult.IsError)
                            break;
                        list.Add(parseResult.RawValue);
                    }
                    return ParseResult.Ok(list);
                };
        }

        public static Parser<List<T>> OneMany<T>(Parser<T> parser)
        {
            return
                stream =>
                {
                    var result = Many(parser)(stream);
                    if (result.IsError == false && result.RawValue.Count == 0)
                        return ParseResult.Error<List<T>>(new ParseErrorInfo(ErrorKind.InComplete));
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
                            return ParseResult.Error<object[]>(result.RawError);
                        if (stream.IsEnd && i + 1 != parsers.Length)
                            return ParseResult.Error<object[]>(new ParseErrorInfo(ErrorKind.InComplete));
                        values[i] = result.RawValue;
                    }
                    return ParseResult.Ok(values);
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

        public static Parser<(T1, T2, T3, T4, T5, T6)> Tuple<T1, T2, T3, T4, T5, T6>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5, Parser<T6> parser6)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>, parser5 as Parser<object>, parser6 as Parser<object>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4], (T6)objects[5]));
        }

        public static Parser<T> Or<T>(params Parser<T>[] parsers)
        {
            return
                stream =>
                {
                    foreach (var parser in parsers)
                    {
                        var parseResult = parser(stream);
                        if (parseResult.IsError == false || parseResult.RawError.ErrorKind == ErrorKind.ForceError)
                            return parseResult;
                    }
                    return ParseResult.Error<T>(new ParseErrorInfo(ErrorKind.InComplete)); ;
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
                        return ParseResult.Error<T[]>(new ParseErrorInfo(ErrorKind.InComplete));
                    isFirst = false;
                    if (result.IsError)
                        break;
                    list.Add(result.RawValue);
                }
                return ParseResult.Ok(list.ToArray());
            };
        }

        public static Parser<Token> TestOnce(Func<Token, bool> test)
        {
            return stream =>
            {
                if (stream.IsEnd)
                {
                    return ParseResult.Error<Token>(new ParseErrorInfo(ErrorKind.InComplete));
                }

                var token = stream.Current;
                if (test(token))
                {
                    stream.MoveNext();
                    return ParseResult.Ok(token);
                }
                return ParseResult.Error<Token>(new ParseErrorInfo(ErrorKind.InComplete));
            };
        }
    }
}
