using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;

namespace MarineLang.ParserCore
{
    public static class ParserCombinator
    {
        public static Parser<List<T>, I> Many<T, I>(Parser<T, I> parser)
        {
            return
                input =>
                {
                    var list = new List<T>();
                    while (input.IsEnd == false)
                    {
                        var parseResult = parser(input);
                        input = parseResult.Remain;
                        if (parseResult.TryGetError(out var parseErrorInfo) && parseErrorInfo.ErrorKind != ErrorKind.InComplete)
                            return ParseResult.Error<List<T>, I>(parseErrorInfo, input);
                        if (parseResult.Result.IsError)
                            break;
                        list.Add(parseResult.Result.Unwrap());
                    }
                    return ParseResult.Ok(list, input);
                };
        }

        public static Parser<List<T>, I> OneMany<T, I>(Parser<T, I> parser)
        {
            return
                input =>
                {
                    var result = Many(parser)(input);
                    if (result.Result.IsError == false && result.Result.RawValue.Count == 0)
                        return ParseResult.Error<List<T>, I>(new ParseErrorInfo(ErrorKind.InComplete), result.Remain);
                    return result;
                };
        }

        public static Parser<object[], I> Parsers<I>(params Parser<object, I>[] parsers)
        {
            var values = new object[parsers.Length];

            return
                input =>
                {
                    for (var i = 0; i < parsers.Length; i++)
                    {
                        var result = parsers[i](input);
                        input = result.Remain;
                        if (result.Result.IsError)
                            return ParseResult.Error<object[], I>(result.Result.RawError, input);
                        if (input.IsEnd && i + 1 != parsers.Length)
                            return ParseResult.Error<object[], I>(new ParseErrorInfo(ErrorKind.InComplete), input);
                        values[i] = result.Result.RawValue;
                    }
                    return ParseResult.Ok(values, input);
                };
        }

        public static Parser<(T1, T2), I> Tuple<T1, T2, I>(Parser<T1, I> parser1, Parser<T2, I> parser2)
        {
            return Parsers(parser1 as Parser<object, I>, parser2 as Parser<object, I>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1]));
        }

        public static Parser<(T1, T2, T3), I> Tuple<T1, T2, T3, I>(Parser<T1, I> parser1, Parser<T2, I> parser2, Parser<T3, I> parser3)
        {
            return Parsers(parser1 as Parser<object, I>, parser2 as Parser<object, I>, parser3 as Parser<object, I>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2]));
        }

        public static Parser<(T1, T2, T3, T4), I> Tuple<T1, T2, T3, T4, I>(Parser<T1, I> parser1, Parser<T2, I> parser2, Parser<T3, I> parser3, Parser<T4, I> parser4)
        {
            return Parsers(parser1 as Parser<object, I>, parser2 as Parser<object, I>, parser3 as Parser<object, I>, parser4 as Parser<object, I>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3]));
        }

        public static Parser<(T1, T2, T3, T4, T5), I> Tuple<T1, T2, T3, T4, T5, I>
            (Parser<T1, I> parser1, Parser<T2, I> parser2, Parser<T3, I> parser3, Parser<T4, I> parser4, Parser<T5, I> parser5)
        {
            return Parsers(parser1 as Parser<object, I>, parser2 as Parser<object, I>, parser3 as Parser<object, I>, parser4 as Parser<object, I>, parser5 as Parser<object, I>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4]));
        }

        public static Parser<(T1, T2, T3, T4, T5, T6), I> Tuple<T1, T2, T3, T4, T5, T6, I>
            (Parser<T1, I> parser1, Parser<T2, I> parser2, Parser<T3, I> parser3, Parser<T4, I> parser4, Parser<T5, I> parser5, Parser<T6, I> parser6)
        {
            return Parsers(parser1 as Parser<object, I>, parser2 as Parser<object, I>, parser3 as Parser<object, I>, parser4 as Parser<object, I>, parser5 as Parser<object, I>, parser6 as Parser<object, I>)
                .MapResult(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4], (T6)objects[5]));
        }

        public static Parser<T, I> Or<T, I>(params Parser<T, I>[] parsers)
        {
            return
                input =>
                {
                    foreach (var parser in parsers)
                    {
                        var parseResult = parser(input);
                        input = parseResult.Remain;
                        if (parseResult.Result.IsError == false || parseResult.Result.RawError.ErrorKind == ErrorKind.ForceError)
                            return parseResult;
                    }
                    return ParseResult.Error<T, I>(new ParseErrorInfo(ErrorKind.InComplete), input);
                };
        }

        public static Parser<T[], I> Separated<T, TT, I>(Parser<T, I> parser, Parser<TT, I> separateParser)
        {
            return input =>
            {
                var isFirst = true;
                var list = new List<T>();

                while (input.IsEnd == false)
                {
                    var separateResult = separateParser(input);
                    input = separateResult.Remain;
                    if (isFirst == false && separateResult.Result.IsError)
                        break;
                    var result = parser(input);
                    input = result.Remain;
                    if (result.Result.IsError && isFirst == false)
                        return ParseResult.Error<T[], I>(new ParseErrorInfo(ErrorKind.InComplete), input);
                    isFirst = false;
                    if (result.Result.IsError)
                        break;
                    list.Add(result.Result.Unwrap());
                }
                return ParseResult.Ok(list.ToArray(), input);
            };
        }

        public static Parser<I, I> TestOnce<I>(Func<I, bool> test)
        {
            return input =>
            {
                if (input.IsEnd)
                {
                    return ParseResult.Error<I, I>(new ParseErrorInfo(ErrorKind.InComplete), input);
                }

                var token = input.Current;
                if (test(token))
                {
                    return ParseResult.Ok(token, input.Advance());
                }
                return ParseResult.Error<I, I>(new ParseErrorInfo(ErrorKind.InComplete), input);
            };
        }
    }
}
