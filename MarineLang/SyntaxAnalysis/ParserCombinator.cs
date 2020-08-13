using MarineLang.Models;
using System;
using System.Collections.Generic;

namespace MarineLang.SyntaxAnalysis
{
    public static class ParserCombinator
    {
        public static Parser<IEnumerable<T>> Many<T>(Parser<T> parser)
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
