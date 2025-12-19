using System.Collections.Generic;

namespace MiniLangCompiler
{
    public enum MiniType
    {
        Int,
        Float,
        Double,
        String,
        Void,
        Bool,
        Unknown,
        Error
    }

    public abstract class Symbol
    {
        public string Name { get; set; }
        public MiniType Type { get; set; }
        public int Line { get; set; }
    }

    public class VariableSymbol : Symbol
    {
        public bool IsConst { get; set; }
        public string InitValue { get; set; }
        public bool IsGlobal { get; set; }
    }

    public class FunctionSymbol : Symbol
    {
        public List<VariableSymbol> Parameters { get; set; } = new List<VariableSymbol>();
        public List<VariableSymbol> LocalVariables { get; set; } = new List<VariableSymbol>();
        public List<string> ControlStructures { get; set; } = new List<string>();
        public bool HasReturnStmt { get; set; }
        public bool IsRecursive { get; set; }

        public override string ToString()
        {
            string paramsStr = string.Join(", ", Parameters.ConvertAll(p => $"{p.Type} {p.Name}"));
            return $"Func: {Name}, Ret: {Type}, Params: ({paramsStr})";
        }
    }

    public class SymbolTable
    {
        private readonly Stack<Dictionary<string, Symbol>> _scopes = new Stack<Dictionary<string, Symbol>>();
        public List<VariableSymbol> GlobalVariables { get; private set; } = new List<VariableSymbol>();
        public Dictionary<string, FunctionSymbol> Functions { get; private set; } = new Dictionary<string, FunctionSymbol>();

        public SymbolTable()
        {
            _scopes.Push(new Dictionary<string, Symbol>());
        }

        public void EnterScope()
        {
            _scopes.Push(new Dictionary<string, Symbol>());
        }

        public void ExitScope()
        {
            if (_scopes.Count > 1)
                _scopes.Pop();
        }

        public bool Define(Symbol symbol)
        {
            var currentScope = _scopes.Peek();
            if (currentScope.ContainsKey(symbol.Name))
            {
                return false;
            }

            currentScope[symbol.Name] = symbol;

            if (_scopes.Count == 1 && symbol is VariableSymbol v)
            {
                v.IsGlobal = true;
                GlobalVariables.Add(v);
            }

            if (symbol is FunctionSymbol f)
            {
                Functions[f.Name] = f;
            }

            return true;
        }

        public Symbol Resolve(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.TryGetValue(name, out Symbol s))
                    return s;
            }
            return null;
        }

        public Symbol ResolveCurrentScope(string name)
        {
            if (_scopes.Peek().TryGetValue(name, out Symbol s))
                return s;
            return null;
        }
    }
}