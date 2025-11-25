using System;
using System.Collections.Generic;

namespace Tema1
{
    public abstract class Node
    {
        public string Value { get; set; }
        public Node Left { get; set; }
        public Node Right { get; set; }

        public Node(string value)
        {
            Value = value;
        }
    }

    public class OperandNode : Node
    {
        public OperandNode(char symbol) : base(symbol.ToString()) { }
    }

    public class OperatorNode : Node
    {
        public OperatorNode(char symbol, Node left, Node right) : base(symbol.ToString())
        {
            Left = left;
            Right = right;
        }

        public OperatorNode(char symbol, Node child) : base(symbol.ToString())
        {
            Left = child;
            Right = null;
        }
    }

    public static class SyntaxTreeBuilder
    {
        public static Node BuildTree(string postfix)
        {
            var stack = new Stack<Node>();

            foreach (char c in postfix)
            {
                if (char.IsLetterOrDigit(c))
                {
                    stack.Push(new OperandNode(c));
                }
                else if (c == '*')
                {
                    if (stack.Count > 0)
                    {
                        var child = stack.Pop();
                        stack.Push(new OperatorNode(c, child));
                    }
                }
                else if (c == '.' || c == '|')
                {
                    if (stack.Count >= 2)
                    {
                        var right = stack.Pop();
                        var left = stack.Pop();
                        stack.Push(new OperatorNode(c, left, right));
                    }
                }
            }

            return stack.Count > 0 ? stack.Peek() : null;
        }

        public static void PrintTree(Node node, string indent = "", bool isLast = true)
        {
            if (node == null) return;

            Console.Write(indent);
            if (isLast)
            {
                Console.Write("└── ");
                indent += "    ";
            }
            else
            {
                Console.Write("├── ");
                indent += "│   ";
            }

            Console.WriteLine(node.Value);

            var children = new List<Node>();
            if (node.Left != null) children.Add(node.Left);
            if (node.Right != null) children.Add(node.Right);

            for (int i = 0; i < children.Count; i++)
            {
                PrintTree(children[i], indent, i == children.Count - 1);
            }
        }
    }
}