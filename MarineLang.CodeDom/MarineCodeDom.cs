﻿using System;
using System.Linq;
using MarineLang.Models;
using MarineLang.Models.Asts;

namespace MarineLang.CodeDom
{
    public class MarineCodeDom
    {
        public string Eof{get;}
        public MarineCodeDom(string eof)
        {
            Eof = eof;
        }

        public MarineCodeDom()
        {
            Eof = Environment.NewLine;
        }

        public string CreateProgram(ProgramAst programAst)
        {
            return string.Join(Eof, programAst.funcDefinitionAsts.Select(CreateFuncDefinition));
        }

        public string CreateFuncDefinition(FuncDefinitionAst funcDefinitionAst)
        {
            var funcName = funcDefinitionAst.funcName;
            var args = string.Join(", ", funcDefinitionAst.args.Select(arg => arg.VarName));
            var statements =
                string.Join(
                    Eof,
                    funcDefinitionAst.statementAsts.Select(statement => CreateStatement(statement, 1))
                );
            var statementLastNewLine = statements.Length > 0 ? Eof : "";

            return
                $"fun {funcName}({args}){Eof}{statements}{statementLastNewLine}end";
        }

        public string CreateStatement(StatementAst statementAst, int indent)
        {
            var tabs = GetTabs(indent);
            var text = "";
            if (statementAst.GetAssignmentVariableAst() != null)
                text = CreateAssignmentVariable(statementAst.GetAssignmentVariableAst(), indent);
            else if (statementAst.GetExprStatementAst() != null)
                text = CreateExpr(statementAst.GetExprStatementAst().expr, indent);
            else if (statementAst.GetYieldAst() != null)
                text = CreateYield(statementAst.GetYieldAst());
            else if (statementAst.GetReturnAst() != null)
                text = CreateReturn(statementAst.GetReturnAst(), indent);
            else if (statementAst.GetInstanceFieldAssignmentAst() != null)
                text = CreateFieldAssignment(statementAst.GetInstanceFieldAssignmentAst(), indent);
            else if (statementAst.GetReAssignmentIndexerAst() != null)
                text = CreateReAssignmentIndexer(statementAst.GetReAssignmentIndexerAst(), indent);
            else if (statementAst.GetReAssignmentVariableAst() != null)
                text = CreateReAssignmentVariable(statementAst.GetReAssignmentVariableAst(), indent);
            else if (statementAst.GetWhileAst() != null)
                text = CreateWhile(statementAst.GetWhileAst(),indent);
            else if (statementAst.GetForAst() != null)
                text = CreateFor(statementAst.GetForAst(),indent);

            return tabs + text;
        }

        public string CreateAssignmentVariable(AssignmentVariableAst assignmentVariableAst, int indent)
        {
            var varName = assignmentVariableAst.variableAst.VarName;
            var expr = CreateExpr(assignmentVariableAst.expr, indent + 1);
            return $"let {varName} = {expr}";
        }

        public string CreateYield(YieldAst yieldAst)
        {
            return $"yield";
        }

        public string CreateReturn(ReturnAst returnAst, int indent)
        {
            var expr = CreateExpr(returnAst.expr, indent + 1);
            return $"ret {expr}";
        }

        public string CreateFieldAssignment(InstanceFieldAssignmentAst fieldAssignmentAst,int indent)
        {
            var instanceExpr = CreateExpr(fieldAssignmentAst.instanceFieldAst,indent);
            var expr = CreateExpr(fieldAssignmentAst.expr, indent + 1);
            return $"{instanceExpr} = {expr}";
        }

        public string CreateReAssignmentVariable(ReAssignmentVariableAst reAssignmentVariableAst, int indent)
        {
            var varName = reAssignmentVariableAst.variableAst.VarName;
            var expr = CreateExpr(reAssignmentVariableAst.expr, indent + 1);
            return $"{varName} = {expr}";
        }

        public string CreateWhile(WhileAst whileAst,int indent)
        {
            var conditionExpr = CreateExpr(whileAst.conditionExpr, indent + 1);
            var statements = string.Join(
                Eof,
                whileAst.statements.Select(statement => CreateStatement(statement, indent + 1))
            );
            var tabs = GetTabs(indent);
            return $"while({conditionExpr}){Eof}{tabs}{{{Eof}{statements}{Eof}{tabs}}}";
        }

        public string CreateFor(ForAst forAst, int indent)
        {
            var initVariable= CreateVariable(forAst.initVariable);
            var initExpr = CreateExpr(forAst.initExpr, indent + 1);
            var maxValueExpr = CreateExpr(forAst.maxValueExpr, indent + 1);
            var addValueExpr = CreateExpr(forAst.addValueExpr, indent + 1);
            var statements = string.Join(
                Eof,
                forAst.statements.Select(statement => CreateStatement(statement, indent + 1))
            );
            var tabs = GetTabs(indent);
            return $"for {initVariable} = {initExpr}, {maxValueExpr}, {addValueExpr}{Eof}{tabs}{{{Eof}{statements}{Eof}{tabs}}}";
        }

        public string CreateReAssignmentIndexer(ReAssignmentIndexerAst reAssignmentIndexerAst, int indent)
        {
            var indexerExpr = CreateExpr(reAssignmentIndexerAst.getIndexerAst, indent);
            var expr = CreateExpr(reAssignmentIndexerAst.assignmentExpr, indent + 1);
            return $"{indexerExpr} = {expr}";
        }

        public string CreateExpr(ExprAst exprAst, int indent)
        {
            if (exprAst.GetValueAst() != null)
                return CreateValue(exprAst.GetValueAst());
            if (exprAst.GetVariableAst() != null)
                return CreateVariable(exprAst.GetVariableAst());
            if (exprAst.GetBinaryOpAst() != null)
                return CreateBinaryOp(exprAst.GetBinaryOpAst(), indent);
            if (exprAst.GetUnaryOpAst() != null)
                return CreateUnaryOp(exprAst.GetUnaryOpAst(), indent);
            if (exprAst.GetFuncCallAst() != null)
                return CreateFuncCall(exprAst.GetFuncCallAst(), indent);
            if (exprAst.GetInstanceFuncCallAst() != null)
                return CreateInstanceFuncCall(exprAst.GetInstanceFuncCallAst(), indent);
            if (exprAst.GetInstanceFieldAst() != null)
                return CreateInstanceField(exprAst.GetInstanceFieldAst(), indent);
            if (exprAst.GetArrayLiteralAst() != null)
                return CreateArrayLiteral(exprAst.GetArrayLiteralAst(), indent);
            if (exprAst.GetGetIndexerAst() != null)
                return CreateGetIndexer(exprAst.GetGetIndexerAst(), indent);
            if (exprAst.GetAwaitAst() != null)
                return CreateAwait(exprAst.GetAwaitAst(), indent);
            if (exprAst.GetIfExprAst() != null)
                return CreateIfExpr(exprAst.GetIfExprAst(), indent);
            if (exprAst.GetActionAst() != null)
                return CreateAction(exprAst.GetActionAst(),indent);
            return "";
        }

        public string CreateValue(ValueAst valueAst)
        {
            if (valueAst.value is string str)
                return $"\"{str}\"";

            if (valueAst.value is char c)
                return $"'{c}'";

            if (valueAst.value is bool flag)
                return flag ? "true" : "false";

            return valueAst.value.ToString();
        }

        public string CreateVariable(VariableAst variableAst)
        {
            return variableAst.VarName;
        }

        public string CreateBinaryOp(BinaryOpAst binaryOpAst, int indent)
        {
            var op = binaryOpAst.opKind.GetText();
            var leftExpr = CreateExpr(binaryOpAst.leftExpr, indent);
            leftExpr = SetParenthesis(leftExpr, binaryOpAst.leftExpr.ExprPriority, binaryOpAst.ExprPriority);
            var rightExpr = CreateExpr(binaryOpAst.rightExpr, indent);
            rightExpr = SetParenthesis(rightExpr, binaryOpAst.rightExpr.ExprPriority, binaryOpAst.ExprPriority);
            return $"{leftExpr} {op} {rightExpr}";
        }

        public string CreateUnaryOp(UnaryOpAst unaryOpAst, int indent)
        {
            var op = unaryOpAst.opToken.text;
            var expr = SetParenthesis(
                CreateExpr(unaryOpAst.expr, indent),
                unaryOpAst.expr.ExprPriority,
                unaryOpAst.ExprPriority
            );
            return $"{op}{expr}";
        }

        public string CreateFuncCall(FuncCallAst funcCallAst, int indent)
        {
            var funcName = funcCallAst.FuncName;
            var args = string.Join(", ", funcCallAst.args.Select(arg => CreateExpr(arg, indent + 1)));
            return $"{funcName}({args})";
        }

        public string CreateInstanceFuncCall(InstanceFuncCallAst instanceFuncCallAst, int indent)
        {
            var expr = CreateExpr(instanceFuncCallAst.instanceExpr, indent);
            expr = SetParenthesis(expr, instanceFuncCallAst.instanceExpr.ExprPriority, instanceFuncCallAst.ExprPriority);
            var funcCall = CreateFuncCall(instanceFuncCallAst.instancefuncCallAst, indent);
            return $"{expr}.{funcCall}";
        }

        public string CreateInstanceField(InstanceFieldAst instanceFieldAst, int indent)
        {
            var expr = CreateExpr(instanceFieldAst.instanceExpr, indent);
            expr = SetParenthesis(expr, instanceFieldAst.instanceExpr.ExprPriority, instanceFieldAst.ExprPriority);
            var variable = CreateVariable(instanceFieldAst.variableAst);
            return $"{expr}.{variable}";
        }

        public string CreateArrayLiteral(ArrayLiteralAst arrayLiteralAst, int indent)
        {
            var args =
                string.Join(
                    ", ",
                    arrayLiteralAst.arrayLiteralExprs.exprAsts.Select(arg => CreateExpr(arg, indent + 1))
                );
            var size = arrayLiteralAst.arrayLiteralExprs.size;
            if (size == null)
                return $"[{args}]";
            return $"[{args}; {size}]";
        }

        public string CreateGetIndexer(GetIndexerAst getIndexerAst, int indent)
        {
            var expr = CreateExpr(getIndexerAst.instanceExpr, indent);
            expr = SetParenthesis(expr, getIndexerAst.instanceExpr.ExprPriority, getIndexerAst.ExprPriority);
            var index = CreateExpr(getIndexerAst.indexExpr, indent + 1);
            return $"{expr}[{index}]";
        }

        public string CreateAwait(AwaitAst awaitAst, int indent)
        {
            var expr = CreateExpr(awaitAst.instanceExpr, indent);
            expr = SetParenthesis(expr, awaitAst.instanceExpr.ExprPriority, awaitAst.ExprPriority);
            return $"{expr}.await";
        }

        public string CreateIfExpr(IfExprAst ifExprAst, int indent)
        {
            var tabs = GetTabs(indent);
            var conditionExpr = CreateExpr(ifExprAst.conditionExpr, indent + 1);
            var thens = string.Join(
                Eof,
                ifExprAst.thenStatements.Select(statement => CreateStatement(statement, indent + 1))
            );
            var elses = string.Join(
                Eof,
                ifExprAst.elseStatements.Select(statement => CreateStatement(statement, indent + 1))
            );
            if (elses.Length == 0)
                return $"if({conditionExpr}){Eof}{tabs}{{{Eof}{thens}{Eof}{tabs}}}";
            return $"if({conditionExpr}){Eof}{tabs}{{{Eof}{thens}{Eof}{tabs}}}{Eof}{tabs}else{Eof}{tabs}{{{Eof}{elses}{Eof}{tabs}}}";
        }

        public string CreateAction(ActionAst actionAst, int indent)
        {
            var args = string.Join(", ", actionAst.args.Select(arg => CreateExpr(arg, indent + 1)));
            var statements = string.Join(
              Eof,
              actionAst.statementAsts.Select(statement => CreateStatement(statement, indent + 1))
            );
            var tabs = GetTabs(indent);

            if (args.Length > 0)
                return $"{{|{args}|{Eof}{statements}{Eof}{tabs}}}";
            return $"{{{Eof}{statements}{Eof}{tabs}}}";
        }

        static string GetTabs(int indent)
        {
          return new string(' ', indent * 4);
        }

        static string SetParenthesis(
            string childText,
            ExprPriority childExprPriority,
            ExprPriority parentExprPriority
        )
        {
            return childExprPriority < parentExprPriority ? $"({childText})" : childText;
        }
    }
}
