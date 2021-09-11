using MarineLang.Models;
using MarineLang.Models.Errors;
using MineUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.ParserCore
{
    public static class Parse<I>
    {
        public delegate IParseResult<T, I> Parser<out T>(IInput<I> input);

        public static Parser<List<T>> Many<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var list = new List<T>();
                    var parseResult = ParseResult.NewOk(default(T), input);

                    while (parseResult.Remain.IsEnd == false)
                    {
                        var parseResult2 = parseResult.ChainRight(parser(parseResult.Remain));

                        if (parseResult2.Result.IsError)
                        {
                            parseResult = parseResult.SetRemain(parseResult2.Remain);
                            break;
                        }
                        parseResult = parseResult2;
                        list.Add(parseResult.Result.Unwrap());
                    }
                    return parseResult.Ok(list);
                };
        }

        public static Parser<List<T>> ManyUntilEnd<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var list = new List<T>();
                    var parseResult = ParseResult.NewOk(default(T), input);

                    while (parseResult.Remain.IsEnd == false)
                    {
                        parseResult = parseResult.ChainRight(parser(parseResult.Remain));
                        if (parseResult.Result.IsError)
                            return parseResult.CastError<List<T>>();
                        list.Add(parseResult.Result.Unwrap());
                    }
                    return parseResult.Ok(list);
                };
        }

        public static Parser<List<T>> ManyUntilEndStackConsumeError<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var list = new List<T>();
                    var parseResult = ParseResult.NewOk(default(T), input);

                    while (parseResult.Remain.IsEnd == false)
                    {
                        var parseResult2 = parseResult.ChainRight(parser(parseResult.Remain));

                        if (parseResult2.Result.IsError && parseResult.Remain.Index == parseResult2.Remain.Index)
                            return parseResult2.CastError<List<T>>();

                        if (parseResult2.Result.IsError)
                        {
                            parseResult = parseResult2.Ok(parseResult.Result.RawValue);
                            continue;
                        }

                        parseResult = parseResult2;
                        list.Add(parseResult.Result.Unwrap());
                    }
                    return parseResult.Ok(list);
                };
        }

        public static Parser<List<T>> OneMany<T>(Parser<T> parser)
        {
            return
                input =>
                {
                    var result = Many(parser)(input);
                    if (result.Result.IsError == false && result.Result.RawValue.Count == 0)
                        return result.Error<List<T>>(new ParseErrorInfo());
                    return result;
                };
        }

        public static Parser<object[]> Parsers(params Parser<object>[] parsers)
        {
            var values = new object[parsers.Length];

            return
                input =>
                {
                    var parseResult = ParseResult.NewOk(default(object), input);

                    for (var i = 0; i < parsers.Length; i++)
                    {
                        parseResult = parseResult.ChainRight( parsers[i](parseResult.Remain));
                        if (parseResult.Result.IsError)
                            return parseResult.CastError<object[]>();
                        if (parseResult.Remain.IsEnd && i + 1 != parsers.Length)
                            return parseResult.Error<object[]>(new ParseErrorInfo());
                        values[i] = parseResult.Result.Unwrap();
                    }
                    return parseResult.Ok(values);
                };
        }

        public static Parser<(T1, T2)> Tuple<T1, T2>(Parser<T1> parser1, Parser<T2> parser2)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>)
                .Map(objects => ((T1)objects[0], (T2)objects[1]));
        }

        public static Parser<(T1, T2, T3)> Tuple<T1, T2, T3>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>)
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2]));
        }

        public static Parser<(T1, T2, T3, T4)> Tuple<T1, T2, T3, T4>(Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>)
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3]));
        }

        public static Parser<(T1, T2, T3, T4, T5)> Tuple<T1, T2, T3, T4, T5>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>, parser5 as Parser<object>)
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4]));
        }

        public static Parser<(T1, T2, T3, T4, T5, T6)> Tuple<T1, T2, T3, T4, T5, T6>
            (Parser<T1> parser1, Parser<T2> parser2, Parser<T3> parser3, Parser<T4> parser4, Parser<T5> parser5, Parser<T6> parser6)
        {
            return Parsers(parser1 as Parser<object>, parser2 as Parser<object>, parser3 as Parser<object>, parser4 as Parser<object>, parser5 as Parser<object>, parser6 as Parser<object>)
                .Map(objects => ((T1)objects[0], (T2)objects[1], (T3)objects[2], (T4)objects[3], (T5)objects[4], (T6)objects[5]));
        }

        public static Parser<T> Or<T>(params Parser<T>[] parsers)
        {
            return
                input =>
                {
                    IParseResult<T, I> lastParseResult = null;
                    foreach (var parser in parsers)
                    {
                        lastParseResult = parser(input);
                        input = lastParseResult.Remain;
                        if (lastParseResult.Result.IsError == false)
                            return lastParseResult;
                    }
                    return lastParseResult;
                };
        }

        //入力を消費するエラーが発生したら、即座に終了するOr
        public static Parser<T> OrConsumedError<T>(params Parser<T>[] parsers)
        {
            return
                input =>
                {
                    IParseResult<T, I> lastParseResult = null;
                    foreach (var parser in parsers)
                    {
                        lastParseResult = parser(input);
                        if (lastParseResult.Remain.Index != input.Index && lastParseResult.Result.IsError)
                            return lastParseResult;
                        input = lastParseResult.Remain;
                        if (lastParseResult.Result.IsOk)
                            return lastParseResult;
                    }
                    return lastParseResult;
                };
        }

        public static Parser<T[]> Separated<T, TT>(Parser<T> parser, Parser<TT> separateParser)
        {
            return input =>
            {
                var isFirst = true;
                var list = new List<T>();

                var parseResult = ParseResult.NewOk(default(T), input);

                while (parseResult.Remain.IsEnd == false)
                {
                    var parseResult2 = parseResult.ChainLeft(separateParser(parseResult.Remain));

                    if (isFirst == false && parseResult2.Result.IsError)
                    {
                        parseResult = parseResult.SetRemain(parseResult2.Remain);
                        break;
                    }

                    parseResult2 = parseResult.ChainRight(parser(parseResult2.Remain));

                    if (parseResult2.Result.IsError && isFirst == false)
                        return parseResult2.CastError<T[]>();

                    isFirst = false;
                    
                    if (parseResult2.Result.IsError)
                    {
                        parseResult = parseResult.SetRemain(parseResult2.Remain);
                        break;
                    }
                    
                    parseResult = parseResult2;

                    list.Add(parseResult.Result.Unwrap());
                }
                return parseResult.Ok(list.ToArray());
            };
        }

        public static Parser<I> Verify(Func<I, bool> test)
        {
            return input =>
            {
                if (input.IsEnd)
                {
                    return ParseResult.NewError<I, I>(new ParseErrorInfo(), input);
                }

                var token = input.Current;
                if (test(token))
                {
                    return ParseResult.NewOk(token, input.Advance());
                }
                return ParseResult.NewError<I, I>(new ParseErrorInfo(), input);
            };
        }

        public static Parser<List<T>> Until<T, TT>(Parser<T> parser, Parser<TT> until)
        {
            return input =>
            {
                var list = new List<T>();
                var parseResult = ParseResult.NewOk(default(T), input);

                while (parseResult.Remain.IsEnd == false)
                {
                    var untilParseResult = parseResult.ChainLeft(until(parseResult.Remain));
                    if (untilParseResult.Result.IsOk)
                    {
                        parseResult = untilParseResult;
                        break;
                    }

                    parseResult = parseResult.SetRemain(untilParseResult.Remain);

                    parseResult = parseResult.ChainRight(parser(parseResult.Remain));

                    if (parseResult.Result.IsError)
                        return parseResult.CastError<List<T>>();

                    list.Add(parseResult.Result.Unwrap());
                }
                return parseResult.Ok(list);
            };
        }

        public static Parser<List<T>> UntilStackConsumeError<T, TT>(Parser<T> parser, Parser<TT> until)
        {
            return input =>
            {
                var list = new List<T>();
                var parseResult = ParseResult.NewOk(default(T), input);

                while (parseResult.Remain.IsEnd == false)
                {
                    var untilParseResult = parseResult.ChainLeft(until(parseResult.Remain));
                    if (untilParseResult.Result.IsOk)
                    {
                        parseResult = untilParseResult;
                        break;
                    }

                    parseResult = parseResult.SetRemain(untilParseResult.Remain);

                    var parseResult2 = parseResult.ChainRight(parser(parseResult.Remain));

                    if (parseResult2.Result.IsError && parseResult.Remain.Index == parseResult2.Remain.Index)
                        return parseResult.CastError<List<T>>();

                    if (parseResult2.Result.IsError)
                    {
                        parseResult = parseResult2.Ok(parseResult.Result.RawValue);
                        continue;
                    }

                    parseResult = parseResult2;

                    list.Add(parseResult.Result.Unwrap());
                }
                return parseResult.Ok(list);
            };
        }

        public static readonly Parser<Unit> End =
            input =>
                input.IsEnd ?
                    UnitReturn(input) :
                    ParseResult.NewError<Unit, I>(new ParseErrorInfo(), input);

        public static Parser<Unit> Except<T>(Parser<T> except)
        {
            return input =>
            {
                var result = except(input);
                if (result.Result.IsOk)
                    return result.Error<Unit>(new ParseErrorInfo());
                return ParseResult.NewOk(Unit.Value,result.Remain);
            };
        }

        public static Parser<T> Return<T>(T t)
        {
            return input => ParseResult.NewOk(t, input);
        }

        public static Parser<T> ErrorReturn<T>(ParseErrorInfo parseErrorInfo)
        {
            return input => ParseResult.NewError<T, I>(parseErrorInfo, input);
        }

        public static readonly Parser<I> Any =
            input =>
                input.IsEnd ?
                    ParseResult.NewError<I, I>(new ParseErrorInfo(), input) :
                    ParseResult.NewOk(input.Current, input.Advance());

        public static Parse<char>.Parser<char> Char(char c)
        {
            return Parse<char>.Verify(inputChar => inputChar == c);
        }

        public static Parse<char>.Parser<string> String(string s)
        {
            return s.Select(Char).Concat().Map(string.Concat).Try();
        }

        public static readonly Parser<Unit> UnitReturn = Return(Unit.Value);

        public static readonly Parser<RangePosition> Positioned = input => ParseResult.NewOk(input.RangePosition, input);

        public static readonly Parser<I> Current = input => ParseResult.NewOk(input.Current, input.Advance());
        public static readonly Parser<I> LastCurrent = input => ParseResult.NewOk(input.LastCurrent, input.Advance());
    }
}