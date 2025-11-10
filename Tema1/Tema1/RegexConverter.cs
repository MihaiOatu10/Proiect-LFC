using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tema1
{
    public static class RegexConverter
    {
        private const char LAMBDA = '\0';
        private static int nfaStateCounter;

        private class NFA
        {
            public HashSet<string> States { get; }
            public HashSet<char> Alphabet { get; }
            public string StartState { get; }
            public string AcceptState { get; }
            public Dictionary<string, List<(char, string)>> Transitions { get; }

            public NFA(string start, string accept)
            {
                States = new HashSet<string> { start, accept };
                Alphabet = new HashSet<char>();
                Transitions = new Dictionary<string, List<(char, string)>>();
                StartState = start;
                AcceptState = accept;
            }
        }

        public static DeterministicFiniteAutomation ConvertToDfa(string regex)
        {
            nfaStateCounter = 0;
            string postfix = GetPostfix(regex);
            NFA nfa = PostfixToNFA(postfix);
            return NFAtoDFA(nfa);
        }

        public static string GetPostfix(string regex)
        {
            string explicitRegex = InsertConcatenationOperators(regex);
            return RegexToPostfix(explicitRegex);
        }

        private static string InsertConcatenationOperators(string regex)
        {
            var output = new StringBuilder();
            for (int i = 0; i < regex.Length; i++)
            {
                output.Append(regex[i]);

                if (i + 1 < regex.Length)
                {
                    char current = regex[i];
                    char next = regex[i + 1];

                    if ((char.IsLetterOrDigit(current) || current == '*' || current == ')') &&
                      (char.IsLetterOrDigit(next) || next == '('))
                    {
                        output.Append('.');
                    }
                }
            }
            return output.ToString();
        }

        private static int GetPrecedence(char op)
        {
            switch (op)
            {
                case '|': return 1;
                case '.': return 2;
                case '*': return 3;
                default: return 0;
            }
        }

        private static string RegexToPostfix(string regex)
        {
            var postfix = new StringBuilder();
            var operatorStack = new Stack<char>();

            foreach (char c in regex)
            {
                if (char.IsLetterOrDigit(c))
                {
                    postfix.Append(c);
                }
                else if (c == '(')
                {
                    operatorStack.Push(c);
                }
                else if (c == ')')
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek() != '(')
                    {
                        postfix.Append(operatorStack.Pop());
                    }
                    operatorStack.Pop();
                }
                else
                {
                    while (operatorStack.Count > 0 &&
                       operatorStack.Peek() != '(' &&
                       GetPrecedence(operatorStack.Peek()) >= GetPrecedence(c))
                    {
                        postfix.Append(operatorStack.Pop());
                    }
                    operatorStack.Push(c);
                }
            }

            while (operatorStack.Count > 0)
            {
                postfix.Append(operatorStack.Pop());
            }

            return postfix.ToString();
        }

        private static string NewState() => "q" + nfaStateCounter++;

        private static void AddTransition(NFA nfa, string from, char symbol, string to)
        {
            if (!nfa.Transitions.ContainsKey(from))
            {
                nfa.Transitions[from] = new List<(char, string)>();
            }
            nfa.Transitions[from].Add((symbol, to));
        }

        private static NFA CreateNfaForSymbol(char symbol)
        {
            string start = NewState();
            string accept = NewState();
            var nfa = new NFA(start, accept);
            nfa.Alphabet.Add(symbol);
            AddTransition(nfa, start, symbol, accept);
            return nfa;
        }

        private static NFA CreateNfaForAlternation(NFA a, NFA b)
        {
            string start = NewState();
            string accept = NewState();
            var nfa = new NFA(start, accept);

            nfa.States.UnionWith(a.States);
            nfa.States.UnionWith(b.States);
            nfa.Alphabet.UnionWith(a.Alphabet);
            nfa.Alphabet.UnionWith(b.Alphabet);

            foreach (var transition in a.Transitions)
                if (transition.Value != null) nfa.Transitions[transition.Key] = new List<(char, string)>(transition.Value);
            foreach (var transition in b.Transitions)
                if (transition.Value != null) nfa.Transitions[transition.Key] = new List<(char, string)>(transition.Value);


            AddTransition(nfa, start, LAMBDA, a.StartState);
            AddTransition(nfa, start, LAMBDA, b.StartState);
            AddTransition(nfa, a.AcceptState, LAMBDA, accept);
            AddTransition(nfa, b.AcceptState, LAMBDA, accept);

            return nfa;
        }

        private static NFA CreateNfaForConcatenation(NFA a, NFA b)
        {
            var nfa = new NFA(a.StartState, b.AcceptState);

            nfa.States.UnionWith(a.States);
            nfa.States.UnionWith(b.States);
            nfa.Alphabet.UnionWith(a.Alphabet);
            nfa.Alphabet.UnionWith(b.Alphabet);

            foreach (var transition in a.Transitions)
                if (transition.Value != null) nfa.Transitions[transition.Key] = new List<(char, string)>(transition.Value);
            foreach (var transition in b.Transitions)
                if (transition.Value != null) nfa.Transitions[transition.Key] = new List<(char, string)>(transition.Value);


            if (nfa.Transitions.ContainsKey(a.AcceptState))
            {
                if (nfa.Transitions.ContainsKey(b.StartState))
                {
                    nfa.Transitions[a.AcceptState].AddRange(nfa.Transitions[b.StartState]);
                }
            }
            else if (nfa.Transitions.ContainsKey(b.StartState))
            {
                nfa.Transitions[a.AcceptState] = new List<(char, string)>(nfa.Transitions[b.StartState]);
            }

            foreach (var state in nfa.States.ToList())
            {
                if (nfa.Transitions.ContainsKey(state))
                {
                    var newTransitions = new List<(char, string)>();
                    foreach (var trans in nfa.Transitions[state])
                    {
                        if (trans.Item2 == b.StartState)
                            newTransitions.Add((trans.Item1, a.AcceptState));
                        else
                            newTransitions.Add(trans);
                    }
                    nfa.Transitions[state] = newTransitions;
                }
            }

            nfa.States.Remove(b.StartState);
            nfa.Transitions.Remove(b.StartState);

            return nfa;
        }

        private static NFA CreateNfaForKleeneStar(NFA a)
        {
            string start = NewState();
            string accept = NewState();
            var nfa = new NFA(start, accept);

            nfa.States.UnionWith(a.States);
            nfa.Alphabet.UnionWith(a.Alphabet);
            foreach (var transition in a.Transitions)
                if (transition.Value != null) nfa.Transitions[transition.Key] = new List<(char, string)>(transition.Value);

            AddTransition(nfa, start, LAMBDA, a.StartState);
            AddTransition(nfa, start, LAMBDA, accept);
            AddTransition(nfa, a.AcceptState, LAMBDA, a.StartState);
            AddTransition(nfa, a.AcceptState, LAMBDA, accept);

            return nfa;
        }

        private static NFA PostfixToNFA(string postfix)
        {
            var stack = new Stack<NFA>();

            foreach (char c in postfix)
            {
                if (char.IsLetterOrDigit(c))
                {
                    stack.Push(CreateNfaForSymbol(c));
                }
                else if (c == '|')
                {
                    var b = stack.Pop();
                    var a = stack.Pop();
                    stack.Push(CreateNfaForAlternation(a, b));
                }
                else if (c == '.')
                {
                    var b = stack.Pop();
                    var a = stack.Pop();
                    stack.Push(CreateNfaForConcatenation(a, b));
                }
                else if (c == '*')
                {
                    var a = stack.Pop();
                    stack.Push(CreateNfaForKleeneStar(a));
                }
            }

            return stack.Pop();
        }

        private static HashSet<string> EpsilonClosure(NFA nfa, HashSet<string> states)
        {
            var closure = new HashSet<string>(states);
            var queue = new Queue<string>(states);

            while (queue.Count > 0)
            {
                string currentState = queue.Dequeue();
                if (nfa.Transitions.ContainsKey(currentState))
                {
                    foreach (var trans in nfa.Transitions[currentState])
                        {
                        if (trans.Item1 == LAMBDA && !closure.Contains(trans.Item2))
                        {
                            closure.Add(trans.Item2);
                            queue.Enqueue(trans.Item2);
                        }
                    }
                }
            }
            return closure;
        }

        private static HashSet<string> Move(NFA nfa, HashSet<string> states, char symbol)
        {
            var reachableStates = new HashSet<string>();
            foreach (string state in states)
            {
                if (nfa.Transitions.ContainsKey(state))
                {
                    foreach (var trans in nfa.Transitions[state])
                    {
                        if (trans.Item1 == symbol)
                        {
                            reachableStates.Add(trans.Item2);
                        }
                    }
                }
            }
            return reachableStates;
        }

        private static DeterministicFiniteAutomation NFAtoDFA(NFA nfa)
        {
            var dfaStates = new HashSet<string>();
            var dfaTransitions = new Dictionary<(string, char), string>();
            var dfaAcceptStates = new HashSet<string>();
            var dfaStartState = "Q0";

            var stateMap = new Dictionary<HashSet<string>, string>(HashSet<string>.CreateSetComparer());
            var workQueue = new Queue<HashSet<string>>();
            int dfaStateCounter = 0;

            HashSet<string> startClosure = EpsilonClosure(nfa, new HashSet<string> { nfa.StartState });

            string startStateName = $"Q{dfaStateCounter++}";
            dfaStates.Add(startStateName);
            stateMap[startClosure] = startStateName;
            workQueue.Enqueue(startClosure);
            if (startClosure.Contains(nfa.AcceptState))
            {
                dfaAcceptStates.Add(startStateName);
            }

            while (workQueue.Count > 0)
            {
                HashSet<string> currentNfaStates = workQueue.Dequeue();
                string currentDfaStateName = stateMap[currentNfaStates];

                foreach (char symbol in nfa.Alphabet)
                {
                    HashSet<string> moveStates = Move(nfa, currentNfaStates, symbol);
                    HashSet<string> targetClosure = EpsilonClosure(nfa, moveStates);

                    if (targetClosure.Count == 0)
                        continue;

                    if (!stateMap.TryGetValue(targetClosure, out string targetDfaStateName))
                    {
                        targetDfaStateName = $"Q{dfaStateCounter++}";
                        dfaStates.Add(targetDfaStateName);
                        stateMap[targetClosure] = targetDfaStateName;
                        workQueue.Enqueue(targetClosure);

                        if (targetClosure.Contains(nfa.AcceptState))
                        {
                            dfaAcceptStates.Add(targetDfaStateName);
                        }
                    }

                    dfaTransitions[(currentDfaStateName, symbol)] = targetDfaStateName;
                }
            }

            return new DeterministicFiniteAutomation(dfaStates, nfa.Alphabet, dfaTransitions, dfaStartState, dfaAcceptStates);
        }
    }
}