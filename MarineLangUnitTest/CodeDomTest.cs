using MarineLang.CodeDoms;
using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;
using Xunit;

namespace MarineLangUnitTest
{
    public class CodeDomTest
    {
        public SyntaxParseResult ParseHelper(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            return parser.Parse(lexer.GetTokens(str));
        }

        [Fact]
        public void CreateProgram()
        {
            var programAst = ProgramAst.Create(new FuncDefinitionAst[] {
                FuncDefinitionAst.Create(
                    "hoge",
                    new VariableAst[]{
                        VariableAst.Create("a"),
                        VariableAst.Create("b")
                    },
                    new StatementAst[]{
                        AssignmentVariableAst.Create(VariableAst.Create("foo"),ValueAst.Create(5)),
                        AssignmentVariableAst.Create(VariableAst.Create("ggg"),ValueAst.Create(3.5)),
                        AssignmentVariableAst.Create(VariableAst.Create("c"),ValueAst.Create('あ')),
                        AssignmentVariableAst.Create(VariableAst.Create("flag"),ValueAst.Create(false)),
                        AssignmentVariableAst.Create(VariableAst.Create("aaa"),ValueAst.Create("aaa")),
                        ExprStatementAst.Create(BinaryOpAst.Create(
                            BinaryOpAst.Create(
                                ValueAst.Create(2),
                                ValueAst.Create(3),
                                TokenType.MulOp
                            ),
                            BinaryOpAst.Create(
                                ValueAst.Create(10),
                                ValueAst.Create(2),
                                TokenType.DivOp
                            ),
                            TokenType.PlusOp
                        )),
                        ExprStatementAst.Create(BinaryOpAst.Create(
                            BinaryOpAst.Create(
                                ValueAst.Create(5),
                                ValueAst.Create(1),
                                TokenType.MinusOp
                            ),
                            BinaryOpAst.Create(
                                ValueAst.Create(4),
                                ValueAst.Create(7),
                                TokenType.PlusOp
                            ),
                            TokenType.MulOp
                        )),
                        AssignmentVariableAst.Create(
                            VariableAst.Create("a"),
                            UnaryOpAst.Create(
                                ValueAst.Create(5),
                                new Token(TokenType.MinusOp,"-")
                            )
                        ),
                        ExprStatementAst.Create(InstanceFuncCallAst.Create(
                             UnaryOpAst.Create(
                                VariableAst.Create("hoge"),
                                new Token(TokenType.MinusOp,"-")
                            ),
                            FuncCallAst.Create(
                                 "to_string",
                                 new ExprAst[]{ ValueAst.Create(7) , ValueAst.Create(8) }
                            )
                        )),
                        ExprStatementAst.Create(UnaryOpAst.Create(
                            BinaryOpAst.Create(
                                ValueAst.Create(4),
                                ValueAst.Create(7),
                                TokenType.PlusOp
                            ),
                            new Token(TokenType.MinusOp, "-")
                        )),
                        ExprStatementAst.Create(InstanceFieldAst.Create(
                            ValueAst.Create(777),
                            VariableAst.Create("hoge")
                        )),
                        ExprStatementAst.Create(ArrayLiteralAst.Create(
                            ArrayLiteralAst.ArrayLiteralExprs.Create(
                               new ExprAst[]{ ValueAst.Create(1), ValueAst.Create(2) }
                            )
                        )),
                        ExprStatementAst.Create(ArrayLiteralAst.Create(
                            ArrayLiteralAst.ArrayLiteralExprs.Create(
                                new ExprAst[]{ ValueAst.Create(1)},
                                5
                            )
                        )),
                        ExprStatementAst.Create(GetIndexerAst.Create(
                            BinaryOpAst.Create(
                                VariableAst.Create("fuga"),
                                ValueAst.Create(1),
                                TokenType.PlusOp
                            ),
                            BinaryOpAst.Create(
                                ValueAst.Create(4),
                                ValueAst.Create(7),
                                TokenType.PlusOp
                            )
                        )),
                        ExprStatementAst.Create(AwaitAst.Create(ValueAst.Create(123))),
                        ExprStatementAst.Create(IfExprAst.Create(
                            IfExprAst.Create(
                                ValueAst.Create(true),
                                new StatementAst[]{ ExprStatementAst.Create(ValueAst.Create(true))},
                                new StatementAst[]{ }
                            ),
                            new StatementAst[]{ ExprStatementAst.Create(ValueAst.Create(false)) },
                            new StatementAst[]{ }
                        )),
                        ExprStatementAst.Create(IfExprAst.Create(
                            ValueAst.Create(false),
                            new StatementAst[]{ ExprStatementAst.Create(ValueAst.Create(1))},
                            new StatementAst[]{ ExprStatementAst.Create(ValueAst.Create(2))}
                        )),
                        YieldAst.Create(ValueAst.Create(null)),
                        ReturnAst.Create(VariableAst.Create("hhh")),
                        InstanceFieldAssignmentAst.Create(
                            InstanceFieldAst.Create(
                                VariableAst.Create("foo"),
                                VariableAst.Create("bar")
                            ),
                            ValueAst.Create(555)
                        ),
                        ReAssignmentIndexerAst.Create(
                            GetIndexerAst.Create(
                                BinaryOpAst.Create(
                                    VariableAst.Create("abab"),
                                    ValueAst.Create(1),
                                    TokenType.PlusOp
                                ),
                                ValueAst.Create(6)
                            ),
                            ValueAst.Create(8)
                        ),
                        ReAssignmentVariableAst.Create(
                            VariableAst.Create("yyy"),
                            ValueAst.Create(789)
                        ),
                        AssignmentVariableAst.Create(
                            VariableAst.Create("action"),
                            ActionAst.Create(
                                new VariableAst[]{VariableAst.Create("x"), VariableAst.Create("y")},
                                new StatementAst[]{
                                    ExprStatementAst.Create(BinaryOpAst.Create(
                                        VariableAst.Create("x"),
                                        VariableAst.Create("y"),
                                        TokenType.PlusOp
                                    ))
                                }
                            )
                        ),
                        WhileAst.Create(
                            BinaryOpAst.Create(
                                ValueAst.Create(4),
                                ValueAst.Create(5),
                                TokenType.GreaterEqualOp
                            ),
                            new StatementAst[]{
                               ExprStatementAst.Create( FuncCallAst.Create("print",new ExprAst[]{ ValueAst.Create(123)}))
                            }
                        ),
                        ForAst.Create(
                            VariableAst.Create("i"),
                            ValueAst.Create(0),
                            ValueAst.Create(100),
                            ValueAst.Create(1),
                            new StatementAst[]
                            {
                                ReAssignmentVariableAst.Create(
                                    VariableAst.Create("total"),
                                    BinaryOpAst.Create(
                                        VariableAst.Create("total"),
                                        VariableAst.Create("i"),
                                        TokenType.PlusOp
                                    )
                                )
                            }

                        )
                    }
                )
            });
            var expected =
@"fun hoge(a, b)
    let foo = 5
    let ggg = 3.5
    let c = 'あ'
    let flag = false
    " + "let aaa = \"aaa\"" + @"
    2 * 3 + 10 / 2
    (5 - 1) * (4 + 7)
    let a = -5
    (-hoge).to_string(7, 8)
    -(4 + 7)
    777.hoge
    [1, 2]
    [1; 5]
    (fuga + 1)[4 + 7]
    123.await
    if(if(true)
        {
            true
        })
    {
        false
    }
    if(false)
    {
        1
    }
    else
    {
        2
    }
    yield
    ret hhh
    foo.bar = 555
    (abab + 1)[6] = 8
    yyy = 789
    let action = {|x, y|
            x + y
        }
    while(4 >= 5)
    {
        print(123)
    }
    for i = 0, 100, 1
    {
        total = total + i
    }
end";
            var actual = MarineCodeDom.CreateProgram(programAst);
            Assert.Equal(expected, actual);
        }
    }
}