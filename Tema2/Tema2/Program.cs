using System;
using System.IO;
using Antlr4.Runtime;

namespace MiniLangCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = "input.txt";

            string tokensFilePath = "tokens.txt";
            string varsFilePath = "global_vars.txt";
            string funcsFilePath = "functions.txt";
            string errorsFilePath = "errors.txt";

            try
            {
                if (!File.Exists(inputFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"EROARE: Fisierul '{inputFilePath}' nu a fost gasit.");
                    Console.WriteLine($"Calea curenta de executie este: {Directory.GetCurrentDirectory()}");
                    Console.WriteLine("Asigura-te ca ai setat 'Copy to Output Directory' pe 'Copy Always' pentru input.txt in Visual Studio.");
                    Console.ResetColor();
                    return;
                }

                string input = File.ReadAllText(inputFilePath);
                Console.WriteLine("Compilare inceputa...");

                AntlrInputStream inputStream = new AntlrInputStream(input);
                MiniLangLexer lexer = new MiniLangLexer(inputStream);
                CommonTokenStream tokenStream = new CommonTokenStream(lexer);
                MiniLangParser parser = new MiniLangParser(tokenStream);

                tokenStream.Fill();
                using (StreamWriter writer = new StreamWriter(tokensFilePath))
                {
                    foreach (var token in tokenStream.GetTokens())
                    {
                        if (token.Type > 0)
                        {
                            string typeName = lexer.Vocabulary.GetSymbolicName(token.Type);
                            string text = token.Text.Replace("\n", "\\n").Replace("\r", "");
                            writer.WriteLine($"<Token: {typeName}, Lexema: '{text}', Linie: {token.Line}>");
                        }
                    }
                }
                Console.WriteLine($"- Tokeni salvati in {tokensFilePath}");

                var tree = parser.program();
                SemanticVisitor visitor = new SemanticVisitor();
                visitor.Visit(tree);

                using (StreamWriter writer = new StreamWriter(varsFilePath))
                {
                    writer.WriteLine("--- Variabile Globale ---");
                    foreach (var v in visitor.SymbolTable.GlobalVariables)
                    {
                        writer.WriteLine($"Nume: {v.Name}, Tip: {v.Type}, Initializat cu: {v.InitValue}, Const: {v.IsConst}");
                    }
                }
                Console.WriteLine($"- Variabile globale salvate in {varsFilePath}");

                using (StreamWriter writer = new StreamWriter(funcsFilePath))
                {
                    writer.WriteLine("--- Functii ---");
                    foreach (var f in visitor.SymbolTable.Functions.Values)
                    {
                        string isMain = f.Name == "main" ? "DA" : "NU";
                        string isRec = f.IsRecursive ? "DA" : "NU";

                        writer.WriteLine($"Nume: {f.Name}");
                        writer.WriteLine($"  Tip Retur: {f.Type}");
                        writer.WriteLine($"  Main: {isMain}, Recursiva: {isRec}");

                        writer.WriteLine("  Parametri:");
                        if (f.Parameters.Count == 0) writer.WriteLine("    (niciunul)");
                        foreach (var p in f.Parameters)
                            writer.WriteLine($"    {p.Type} {p.Name}");

                        writer.WriteLine("  Variabile Locale:");
                        if (f.LocalVariables.Count == 0) writer.WriteLine("    (niciuna)");
                        foreach (var lv in f.LocalVariables)
                            writer.WriteLine($"    {lv.Type} {lv.Name} (Init: {lv.InitValue})");

                        writer.WriteLine("  Structuri Control:");
                        if (f.ControlStructures.Count == 0) writer.WriteLine("    (niciuna)");
                        foreach (var cs in f.ControlStructures)
                            writer.WriteLine($"    <{cs}>");

                        writer.WriteLine("-----------------------------");
                    }
                }
                Console.WriteLine($"- Detalii functii salvate in {funcsFilePath}");

                bool hasMain = visitor.SymbolTable.Functions.ContainsKey("main");
                if (!hasMain) visitor.Errors.Add("Eroare Semantica: Lipseste functia 'main'.");

                using (StreamWriter writer = new StreamWriter(errorsFilePath))
                {
                    int totalErrors = visitor.Errors.Count + parser.NumberOfSyntaxErrors;
                    if (totalErrors == 0)
                    {
                        writer.WriteLine("Compilare reusita! Nu s-au gasit erori.");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Compilare reusita! Verifica fisierele generate.");
                        Console.ResetColor();
                    }
                    else
                    {
                        writer.WriteLine($"S-au gasit erori ({totalErrors}):");
                        if (parser.NumberOfSyntaxErrors > 0)
                            writer.WriteLine("Erori sintactice detectate (verifica output-ul consolei pentru detalii ANTLR).");

                        foreach (var err in visitor.Errors)
                        {
                            writer.WriteLine(err);
                        }
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Au fost gasite {totalErrors} erori. Detalii in errors.txt.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("A aparut o exceptie neasteptata: " + ex.Message);
            }

            Console.WriteLine("\napasa orice tasta pentru a iesi...");
            Console.ReadKey();
        }
    }
}