using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tema1
{
    internal class Program
    {
        public static DeterministicFiniteAutomation RegexToDFA(string regex)
        {
            return RegexConverter.ConvertToDfa(regex);
        }

        public static string GetPostfix(string regex)
        {
            return RegexConverter.GetPostfix(regex);
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string inputFilePath = "regex.txt";
            string outputFilePath = "automaton_output.txt";

            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine($"Fișierul '{inputFilePath}' nu a fost găsit. Vă rugăm creați fișierul și adăugați o expresie regulată.");
                Console.ReadKey();
                return;
            }

            string r = File.ReadAllText(inputFilePath).Trim();
            if (string.IsNullOrWhiteSpace(r))
            {
                Console.WriteLine("Fișierul 'regex.txt' este gol.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Expresie regulată citită: {r}");

            DeterministicFiniteAutomation dfa = null;
            string postfix = "";

            try
            {
                dfa = RegexToDFA(r);
                postfix = GetPostfix(r);
                Console.WriteLine("[Validare OK] Automatul M a fost generat cu succes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la generarea automatului: {ex.Message}");
                Console.ReadKey();
                return;
            }


            while (true)
            {
                Console.WriteLine("\n--- Meniu ---");
                Console.WriteLine("1. Afișare formă poloneză postfixată");
                Console.WriteLine("2. Afișare arbore sintactic");
                Console.WriteLine("3. Afișare automat M (consolă și fișier)");
                Console.WriteLine("4. Verificare cuvânt");
                Console.WriteLine("0. Ieșire");
                Console.Write("Alegere: ");
                string alegere = Console.ReadLine();

                switch (alegere)
                {
                    case "1":
                        Console.WriteLine($"Forma postfixată: {postfix}");
                        break;
                    case "2":
                        Console.WriteLine("Afișarea arborelui sintactic nu este implementată.");
                        break;
                    case "3":
                        Console.WriteLine("\n--- Afișare Automaton (Consolă) ---");
                        dfa.PrintAutomaton();
                        Console.WriteLine("-------------------------------------");
                        try
                        {
                            WriteAutomatonToFile(dfa, outputFilePath);
                            Console.WriteLine($"Automatul a fost salvat și în '{outputFilePath}'");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Eroare la scrierea în fișier: {ex.Message}");
                        }
                        break;
                    case "4":
                        Console.WriteLine("Introduceți cuvinte de verificat (scrie 'gata' pentru a reveni la meniu):");
                        while (true)
                        {
                            Console.Write("Cuvânt: ");
                            string cuvant = Console.ReadLine();
                            if (cuvant.ToLower() == "gata")
                            {
                                break;
                            }
                            bool acceptat = dfa.CheckWord(cuvant);
                            Console.WriteLine(acceptat ? "=> ACCEPTAT" : "=> RESPINS");
                        }
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Alegere invalidă. Vă rugăm încercați din nou.");
                        break;
                }
            }
        }

        private static void WriteAutomatonToFile(DeterministicFiniteAutomation dfa, string filePath)
        {
            TextWriter originalOut = Console.Out;
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                Console.SetOut(sw);
                dfa.PrintAutomaton();
            }
            Console.SetOut(originalOut);
        }
    }
}