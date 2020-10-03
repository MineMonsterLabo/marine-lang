using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class ExprAst : StatementAst
    {
        public FuncCallAst GetFuncCallAst()
        {
            return this as FuncCallAst;
        }

        public ValueAst GetValueAst()
        {
            return this as ValueAst;
        }

        public VariableAst GetVariableAst()
        {
            return this as VariableAst;
        }

        public BinaryOpAst GetBinaryOpAst()
        {
            return this as BinaryOpAst;
        }

        public IfExprAst GetIfExprAst()
        {
            return this as IfExprAst;
        }

        public InstanceFuncCallAst GetInstanceFuncCallAst()
        {
            return this as InstanceFuncCallAst;
        }

        public InstanceFieldAst GetInstanceFieldAst()
        {
            return this as InstanceFieldAst;
        }

        public GetIndexerAst GetGetIndexerAst()
        {
            return this as GetIndexerAst;
        }

        public ArrayLiteralAst GetArrayLiteralAst()
        {
            return this as ArrayLiteralAst;
        }

        public ActionAst GetActionAst()
        {
            return this as ActionAst;
        }

        public AwaitAst GetAwaitAst()
        {
            return this as AwaitAst;
        }

        public UnaryOpAst GetUnaryOpAst()
        {
            return this as UnaryOpAst;
        }
    }

    public class ValueAst : ExprAst
    {
        public object value;

        public static ValueAst Create(object value)
        {
            return new ValueAst { value = value };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            if (this is T t)
                yield return t;
        }
    }

    public class VariableAst : ExprAst
    {
        public string varName;

        public static VariableAst Create(string varName)
        {
            return new VariableAst { varName = varName };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            if (this is T t)
                yield return t;
        }

        public class Comparer : IEqualityComparer<VariableAst>
        {
            public bool Equals(VariableAst x, VariableAst y)
            {
                return x.varName == y.varName;
            }

            public int GetHashCode(VariableAst obj)
            {
                return obj.varName.GetHashCode();
            }
        }
    }

    public class BinaryOpAst : ExprAst
    {
        public TokenType opKind;
        public ExprAst leftExpr;
        public ExprAst rightExpr;

        public static BinaryOpAst Create(ExprAst leftExpr, ExprAst rightExpr, TokenType opKind)
        {
            return new BinaryOpAst
            {
                leftExpr = leftExpr,
                rightExpr = rightExpr,
                opKind = opKind
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return leftExpr.LookUp<T>().Concat(rightExpr.LookUp<T>());
        }
    }

    public class UnaryOpAst : ExprAst
    {
        public TokenType opKind;
        public ExprAst expr;

        public static UnaryOpAst Create(ExprAst expr, TokenType opKind)
        {
            return new UnaryOpAst
            {
                expr = expr,
                opKind = opKind
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return expr.LookUp<T>();
        }
    }

    public class InstanceFuncCallAst : ExprAst
    {
        public ExprAst instanceExpr;
        public FuncCallAst instancefuncCallAst;

        public static InstanceFuncCallAst Create(ExprAst instanceExpr, FuncCallAst instancefuncCallAst)
        {
            return new InstanceFuncCallAst
            {
                instanceExpr = instanceExpr,
                instancefuncCallAst = instancefuncCallAst
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>().Concat(instancefuncCallAst.LookUp<T>());
        }
    }

    public class InstanceFieldAst : ExprAst
    {
        public ExprAst instanceExpr;
        public string fieldName;

        public static InstanceFieldAst Create(ExprAst instanceExpr, string fieldName)
        {
            return new InstanceFieldAst
            {
                instanceExpr = instanceExpr,
                fieldName = fieldName
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>();
        }
    }

    public class AwaitAst : ExprAst
    {
        public ExprAst instanceExpr;

        public static AwaitAst Create(ExprAst instanceExpr)
        {
            return new AwaitAst
            {
                instanceExpr = instanceExpr,
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>();
        }
    }

    public class IfExprAst : ExprAst
    {
        public ExprAst conditionExpr;
        public StatementAst[] thenStatements;
        public StatementAst[] elseStatements;

        public static IfExprAst Create(ExprAst conditionExpr, StatementAst[] thenStatements, StatementAst[] elseStatements)
        {
            return new IfExprAst
            {
                conditionExpr = conditionExpr,
                thenStatements = thenStatements,
                elseStatements = elseStatements
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                conditionExpr.LookUp<T>()
                .Concat(thenStatements.SelectMany(x => x.LookUp<T>()))
                .Concat(elseStatements.SelectMany(x => x.LookUp<T>()));
        }
    }

    public class GetIndexerAst : ExprAst
    {
        public ExprAst instanceExpr;
        public ExprAst indexExpr;

        public static GetIndexerAst Create(ExprAst expr, ExprAst indexExpr)
        {
            return new GetIndexerAst { instanceExpr = expr, indexExpr = indexExpr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>().Concat(indexExpr.LookUp<T>());
        }
    }

    public class ArrayLiteralAst : ExprAst
    {
        public ExprAst[] exprAsts;
        public int size;

        public static ArrayLiteralAst Create(ExprAst[] exprAsts, int size)
        {
            return new ArrayLiteralAst { exprAsts = exprAsts, size = size };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return exprAsts.SelectMany(x => x.LookUp<T>());
        }
    }

    public class ActionAst : ExprAst
    {
        public VariableAst[] args;
        public StatementAst[] statementAsts;

        public static ActionAst Create(VariableAst[] args, StatementAst[] statementAsts)
        {
            return new ActionAst { args = args, statementAsts = statementAsts };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                args.SelectMany(x => x.LookUp<T>())
                .Concat(statementAsts.SelectMany(x => x.LookUp<T>()));
        }
    }



    public class FuncCallAst : ExprAst
    {
        public string funcName;
        public ExprAst[] args;

        public static FuncCallAst Create(string funcName, ExprAst[] args)
        {
            return new FuncCallAst { funcName = funcName, args = args };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return args.SelectMany(x => x.LookUp<T>());
        }
    }
}
