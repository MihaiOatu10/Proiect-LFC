using System;
using Antlr4.Runtime.Misc;

namespace MiniLangCompiler
{
    public partial class SemanticVisitor
    {
        public override MiniType VisitFuncCall([NotNull] MiniLangParser.FuncCallContext context)
        {
            string funcName = context.ID().GetText();
            int line = context.Start.Line;

            if (!_symbolTable.Functions.TryGetValue(funcName, out FunctionSymbol targetFunc))
            {
                AddError(line, $"Functia '{funcName}' nu este definita.");
                return MiniType.Unknown;
            }

            if (funcName == "main")
            {
                if (_currentFunction != null && _currentFunction.Name == "main")
                {
                    _currentFunction.IsRecursive = true;
                }
                else
                {
                    AddError(line, "Functia 'main' nu poate fi apelata explicit.");
                }
            }

            if (_currentFunction != null && _currentFunction.Name == funcName)
            {
                _currentFunction.IsRecursive = true;
            }

            var args = context.argsList()?.expression() ?? new MiniLangParser.ExpressionContext[0];

            if (args.Length != targetFunc.Parameters.Count)
            {
                AddError(line, $"Numar incorect de argumente pentru '{funcName}'. Asteptat {targetFunc.Parameters.Count}, primit {args.Length}.");
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    MiniType argType = Visit(args[i]);
                    if (!AreTypesCompatible(targetFunc.Parameters[i].Type, argType))
                    {
                        AddError(line, $"Argumentul {i + 1} pentru '{funcName}' este incompatibil. Asteptat {targetFunc.Parameters[i].Type}, primit {argType}.");
                    }
                }
            }

            return targetFunc.Type;
        }

        public override MiniType VisitAtomExpr([NotNull] MiniLangParser.AtomExprContext context)
        {
            var atom = context.atom();
            if (atom.ID() != null)
            {
                string name = atom.ID().GetText();
                Symbol s = _symbolTable.Resolve(name);
                if (s == null)
                {
                    AddError(atom.Start.Line, $"Variabila '{name}' nu este declarata.");
                    return MiniType.Unknown;
                }
                if (s is VariableSymbol vs) return vs.Type;
                return MiniType.Unknown;
            }
            if (atom.INT_LIT() != null) return MiniType.Int;
            if (atom.FLOAT_LIT() != null) return MiniType.Float;
            if (atom.STRING_LIT() != null) return MiniType.String;
            if (atom.BOOL() != null) return MiniType.Bool;
            if (atom.funcCall() != null) return Visit(atom.funcCall());

            if (atom.expression() != null) return Visit(atom.expression());

            return MiniType.Unknown;
        }

        public override MiniType VisitAddSubExpr([NotNull] MiniLangParser.AddSubExprContext context)
        {
            MiniType left = Visit(context.expression(0));
            MiniType right = Visit(context.expression(1));
            return GetCommonType(left, right);
        }

        public override MiniType VisitMulDivExpr([NotNull] MiniLangParser.MulDivExprContext context)
        {
            MiniType left = Visit(context.expression(0));
            MiniType right = Visit(context.expression(1));
            return GetCommonType(left, right);
        }

        public override MiniType VisitRelExpr([NotNull] MiniLangParser.RelExprContext context)
        {
            Visit(context.expression(0));
            Visit(context.expression(1));
            return MiniType.Bool;
        }

        public override MiniType VisitEqExpr([NotNull] MiniLangParser.EqExprContext context)
        {
            Visit(context.expression(0));
            Visit(context.expression(1));
            return MiniType.Bool;
        }

        public override MiniType VisitAndExpr([NotNull] MiniLangParser.AndExprContext context)
        {
            Visit(context.expression(0));
            Visit(context.expression(1));
            return MiniType.Bool;
        }

        public override MiniType VisitOrExpr([NotNull] MiniLangParser.OrExprContext context)
        {
            Visit(context.expression(0));
            Visit(context.expression(1));
            return MiniType.Bool;
        }

        public override MiniType VisitNotExpr([NotNull] MiniLangParser.NotExprContext context)
        {
            Visit(context.expression());
            return MiniType.Bool;
        }
    }
}