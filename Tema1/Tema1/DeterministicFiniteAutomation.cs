using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Tema1
{
    internal class DeterministicFiniteAutomation
    {
        private HashSet<string> states;
        private HashSet<char> alphabet;
        private Dictionary<(string, char), string> transitionFunction;
        private string startState;
        private HashSet<string> acceptStates;
        public DeterministicFiniteAutomation(HashSet<string> states, HashSet<char> alphabet,
            Dictionary<(string, char), string> transitionFunction, string startState,
            HashSet<string> acceptStates)
        {
            this.states = states;
            this.alphabet = alphabet;
            this.transitionFunction = transitionFunction;
            this.startState = startState;
            this.acceptStates = acceptStates;
        }

        public bool VerifyAutomaton()
        {
            if (!this.states.Contains(this.startState))
            {
                Console.WriteLine($"[Eroare Validare] Starea inițială '{this.startState}' nu se află în mulțimea stărilor Q.");
                return false;
            }

            foreach (string finalState in this.acceptStates)
            {
                if (!this.states.Contains(finalState))
                {
                    Console.WriteLine($"[Eroare Validare] Starea finală '{finalState}' nu se află în mulțimea stărilor Q.");
                    return false;
                }
            }

            foreach (var transition in this.transitionFunction)
            {
                string sourceState = transition.Key.Item1;
                char symbol = transition.Key.Item2;

                string destinationState = transition.Value;

                if (!this.states.Contains(sourceState))
                {
                    Console.WriteLine($"[Eroare Validare] Starea sursă '{sourceState}' din tranziție nu se află în Q.");
                    return false;
                }

                if (!this.alphabet.Contains(symbol))
                {
                    Console.WriteLine($"[Eroare Validare] Simbolul '{symbol}' din tranziție nu se află în alfabetul Σ.");
                    return false;
                }

                if (!this.states.Contains(destinationState))
                {
                    Console.WriteLine($"[Eroare Validare] Starea destinație '{destinationState}' din tranziție nu se află în Q.");
                    return false;
                }
            }

            Console.WriteLine("[Validare OK] Automatul Finit Deterministic este valid.");
            return true;
        }

    public void PrintAutomaton()
    {
        var sortedAlphabet = this.alphabet.OrderBy(c => c).ToList();
        var sortedStates = this.states.OrderBy(s => s).ToList();

            Console.Write("δ\t");
            foreach (char symbol in sortedAlphabet)
            {
                Console.Write($"{symbol}\t");
            }
            Console.WriteLine();

            foreach (string state in sortedStates)
            {
                string prefix = "";
                if (state == this.startState)
                {
                    prefix += "->";
                }
                if (this.acceptStates.Contains(state))
                {
                    prefix += "*";
                }

                Console.Write($"{prefix}{state}\t");

                foreach (char symbol in sortedAlphabet)
                {
                    var key = (state, symbol);
                    if (this.transitionFunction.TryGetValue(key, out string destState))
                    {
                        Console.Write($"{destState}\t");
                    }
                    else
                    {
                        Console.Write("-\t");
                    }
                }
                Console.WriteLine();
            }
        }
        public bool CheckWord(string word)
        {
            string currentState = this.startState;

            foreach (char symbol in word)
            {
                if (!this.alphabet.Contains(symbol))
                {
                    return false;
                }

                var key = (currentState, symbol);
                if (!this.transitionFunction.TryGetValue(key, out string nextState))
                {
                    return false;
                }
                currentState = nextState;
            }

            return this.acceptStates.Contains(currentState);
        }

    }
}
