using System;
using System.Collections.Generic;
using Antlr4.Runtime.Misc;

namespace MiniLangCompiler
{
    public partial class SemanticVisitor : MiniLangBaseVisitor<MiniType>
    {
        private SymbolTable _symbolTable = new SymbolTable();
        private List<string> _errors = new List<string>();

        private FunctionSymbol _currentFunction = null;

        public List<string> Errors => _errors;
        public SymbolTable SymbolTable => _symbolTable;

        private void AddError(int line, string message)
        {
            _errors.Add($"Eroare Semantica [Linie {line}]: {message}");
        }

        private MiniType ParseType(string typeStr)
        {
            switch (typeStr)
            {
                case "int": return MiniType.Int;
                case "float": return MiniType.Float;
                case "double": return MiniType.Double;
                case "string": return MiniType.String;
                case "void": return MiniType.Void;
                case "bool": return MiniType.Bool;
                default: return MiniType.Unknown;
            }
        }

        private bool AreTypesCompatible(MiniType target, MiniType source)
        {
            if (target == MiniType.Unknown || source == MiniType.Unknown) return true;
            if (target == source) return true;
            if (target == MiniType.Double && (source == MiniType.Float || source == MiniType.Int)) return true;
            if (target == MiniType.Float && source == MiniType.Int) return true;
            return false;
        }

        private MiniType GetCommonType(MiniType t1, MiniType t2)
        {
            if (t1 == MiniType.Unknown || t2 == MiniType.Unknown) return MiniType.Unknown;
            if (t1 == MiniType.String || t2 == MiniType.String) return MiniType.String;
            if (t1 == MiniType.Double || t2 == MiniType.Double) return MiniType.Double;
            if (t1 == MiniType.Float || t2 == MiniType.Float) return MiniType.Float;
            return MiniType.Int;
        }

        public override MiniType VisitProgram([NotNull] MiniLangParser.ProgramContext context)
        {
            return base.VisitProgram(context);
        }

        public override MiniType VisitGlobalVarDeclaration([NotNull] MiniLangParser.GlobalVarDeclarationContext context)
        {
            return Visit(context.varDecl());
        }

        public override MiniType VisitVarDecl([NotNull] MiniLangParser.VarDeclContext context)
        {
            string typeStr = "unknown";
            if (context.type() != null)
            {
                typeStr = context.type().GetText();
            }

            string name = context.ID().GetText();
            bool isConst = context.CONST() != null;
            int line = context.Start.Line;
            MiniType declType = ParseType(typeStr);

            if (_symbolTable.ResolveCurrentScope(name) != null)
            {
                if (_currentFunction == null)
                    AddError(line, $"Variabila globala '{name}' este deja definita.");
                else
                    AddError(line, $"Variabila locala '{name}' este deja definita in functia '{_currentFunction.Name}'.");
            }

            if (_currentFunction != null)
            {
                foreach (var param in _currentFunction.Parameters)
                {
                    if (param.Name == name)
                        AddError(line, $"Variabila locala '{name}' intra in conflict cu numele unui parametru.");
                }
            }

            string initValStr = "null";

            if (context.expression() != null)
            {
                MiniType exprType = Visit(context.expression());
                initValStr = context.expression().GetText();

                if (!AreTypesCompatible(declType, exprType))
                {
                    AddError(line, $"Tip incompatibil la initializarea variabilei '{name}'. Asteptat {declType}, primit {exprType}.");
                }
            }
            else if (isConst)
            {
                AddError(line, $"Variabila constanta '{name}' trebuie initializata.");
            }

            var newVar = new VariableSymbol
            {
                Name = name,
                Type = declType,
                Line = line,
                IsConst = isConst,
                InitValue = initValStr
            };

            _symbolTable.Define(newVar);

            if (_currentFunction != null)
            {
                _currentFunction.LocalVariables.Add(newVar);
            }

            return MiniType.Void;
        }

        public override MiniType VisitGlobalFuncDeclaration([NotNull] MiniLangParser.GlobalFuncDeclarationContext context)
        {
            var funcDecl = context.funcDecl();
            string typeStr = funcDecl.type() != null ? funcDecl.type().GetText() : "void";
            string name = funcDecl.ID().GetText();
            int line = funcDecl.Start.Line;
            MiniType retType = ParseType(typeStr);

            if (_symbolTable.Functions.ContainsKey(name))
            {
                AddError(line, $"Functia '{name}' este deja definita.");
                return MiniType.Error;
            }

            FunctionSymbol funcSym = new FunctionSymbol
            {
                Name = name,
                Type = retType,
                Line = line
            };

            _symbolTable.Define(funcSym);
            _symbolTable.EnterScope();
            _currentFunction = funcSym;

            if (funcDecl.paramList() != null)
            {
                foreach (var paramCtx in funcDecl.paramList().param())
                {
                    string pTypeStr = paramCtx.type().GetText();
                    string pName = paramCtx.ID().GetText();
                    MiniType pType = ParseType(pTypeStr);

                    var pSym = new VariableSymbol { Name = pName, Type = pType, Line = paramCtx.Start.Line };

                    if (!_symbolTable.Define(pSym))
                    {
                        AddError(paramCtx.Start.Line, $"Parametrul '{pName}' este duplicat in declaratia functiei '{name}'.");
                    }
                    funcSym.Parameters.Add(pSym);
                }
            }

            Visit(funcDecl.block());

            if (retType != MiniType.Void && !funcSym.HasReturnStmt)
            {
                AddError(line, $"Functia '{name}' de tip {retType} nu are o instructiune 'return' pe toate ramurile.");
            }

            if (name == "main" && funcSym.IsRecursive)
            {
                AddError(line, "Functia 'main' nu poate fi recursiva.");
            }

            _currentFunction = null;
            _symbolTable.ExitScope();

            return MiniType.Void;
        }

        public override MiniType VisitLocalVarStmt([NotNull] MiniLangParser.LocalVarStmtContext context)
        {
            return Visit(context.varDecl());
        }

        public override MiniType VisitAssignStmt([NotNull] MiniLangParser.AssignStmtContext context)
        {
            string name = context.assignment().ID().GetText();
            Symbol sym = _symbolTable.Resolve(name);
            int line = context.Start.Line;

            if (sym == null)
            {
                AddError(line, $"Variabila '{name}' nu a fost declarata inainte de utilizare.");
                return MiniType.Error;
            }

            if (!(sym is VariableSymbol varSym))
            {
                AddError(line, $"'{name}' nu este o variabila.");
                return MiniType.Error;
            }

            if (varSym.IsConst)
            {
                AddError(line, $"Nu se poate atribui o noua valoare variabilei constante '{name}'.");
            }

            if (context.assignment().expression() != null)
            {
                MiniType exprType = Visit(context.assignment().expression());
                if (!AreTypesCompatible(varSym.Type, exprType))
                {
                    AddError(line, $"Tip incompatibil la atribuire pentru '{name}'. Asteptat {varSym.Type}, primit {exprType}.");
                }
            }

            return MiniType.Void;
        }

        public override MiniType VisitIfStmt([NotNull] MiniLangParser.IfStmtContext context)
        {
            if (_currentFunction != null)
                _currentFunction.ControlStructures.Add($"if...else, Linie {context.Start.Line}");

            Visit(context.expression());
            Visit(context.block(0));
            if (context.block().Length > 1)
                Visit(context.block(1));

            return MiniType.Void;
        }

        public override MiniType VisitForStmt([NotNull] MiniLangParser.ForStmtContext context)
        {
            if (_currentFunction != null)
                _currentFunction.ControlStructures.Add($"for, Linie {context.Start.Line}");

            VisitChildren(context);
            return MiniType.Void;
        }

        public override MiniType VisitWhileStmt([NotNull] MiniLangParser.WhileStmtContext context)
        {
            if (_currentFunction != null)
                _currentFunction.ControlStructures.Add($"while, Linie {context.Start.Line}");

            VisitChildren(context);
            return MiniType.Void;
        }

        public override MiniType VisitReturnStmt([NotNull] MiniLangParser.ReturnStmtContext context)
        {
            if (_currentFunction == null)
            {
                AddError(context.Start.Line, "Return in afara unei functii.");
                return MiniType.Error;
            }

            _currentFunction.HasReturnStmt = true;
            MiniType retType = MiniType.Void;

            if (context.expression() != null)
            {
                retType = Visit(context.expression());
            }

            if (!AreTypesCompatible(_currentFunction.Type, retType))
            {
                AddError(context.Start.Line, $"Tip returnat incompatibil in functia '{_currentFunction.Name}'. Asteptat {_currentFunction.Type}, primit {retType}.");
            }

            return MiniType.Void;
        }

        public override MiniType VisitFuncCallStmt([NotNull] MiniLangParser.FuncCallStmtContext context)
        {
            Visit(context.funcCall());
            return MiniType.Void;
        }

        public override MiniType VisitBlockStmt([NotNull] MiniLangParser.BlockStmtContext context)
        {
            return Visit(context.block());
        }

        public override MiniType VisitExprStmt([NotNull] MiniLangParser.ExprStmtContext context)
        {
            return Visit(context.expression());
        }
    }
}