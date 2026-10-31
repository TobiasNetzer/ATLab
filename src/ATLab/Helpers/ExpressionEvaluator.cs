using System;
using System.Collections.Generic;
using System.Globalization;

namespace ATLab.Helpers;

/// <summary>
/// Evaluates arithmetic and bitwise expressions with C# operator precedence. Used for the {expr:...} blocks in
/// commands, limits, delays and the Expression source.
/// </summary>
public static class ExpressionEvaluator
{
    private enum TokenType { NUMBER, OPERATOR, LEFT_PAREN, RIGHT_PAREN }

    private record Token(TokenType Type, string Value);

    // Operator precedence and associativity follow C#:
    // unary + -  >  * / %  >  + -  >  << >>  >  &  >  ^  >  |
    private const string UnaryMinus = "u-";
    private const string UnaryPlus = "u+";

    private static readonly Dictionary<string, (int prec, bool rightAssoc)> Ops = new()
    {
        { "|",  (1, false) },
        { "^",  (2, false) },
        { "&",  (3, false) },

        { "<<", (4, false) },
        { ">>", (4, false) },

        { "+",  (5, false) },
        { "-",  (5, false) },

        { "*",  (6, false) },
        { "/",  (6, false) },
        { "%",  (6, false) },

        { UnaryMinus, (7, true) },
        { UnaryPlus,  (7, true) },
    };

    /// <summary>Tokenizes the expression, converts it to reverse polish notation and evaluates it.</summary>
    public static double Evaluate(string expr)
    {
        var tokens = Tokenize(expr);
        var rpn = ToRpn(tokens);
        return EvalRpn(rpn);
    }

    private static List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < expr.Length)
        {
            char c = expr[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }
            
            if (c == '0' && i + 1 < expr.Length &&
                (expr[i + 1] == 'x' || expr[i + 1] == 'X'))
            {
                var start = i;
                i += 2; // skip 0x

                while (i < expr.Length && Uri.IsHexDigit(expr[i]))
                    i++;

                tokens.Add(new Token(TokenType.NUMBER, expr[start..i]));
                continue;
            }

            // number
            if (char.IsDigit(c) || c == '.')
            {
                var start = i;
                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    i++;

                if (i < expr.Length && (expr[i] == 'e' || expr[i] == 'E'))
                {
                    i++;
                    if (i < expr.Length && (expr[i] == '+' || expr[i] == '-'))
                        i++;

                    while (i < expr.Length && char.IsDigit(expr[i]))
                        i++;
                }

                tokens.Add(new Token(TokenType.NUMBER, expr[start..i]));
                continue;
            }

            switch (c)
            {
                case '(':
                    tokens.Add(new Token(TokenType.LEFT_PAREN, "("));
                    i++;
                    continue;
                case ')':
                    tokens.Add(new Token(TokenType.RIGHT_PAREN, ")"));
                    i++;
                    continue;
            }

            if (i + 1 < expr.Length)
            {
                var two = expr.Substring(i, 2);
                if (Ops.ContainsKey(two))
                {
                    tokens.Add(new Token(TokenType.OPERATOR, two));
                    i += 2;
                    continue;
                }
            }

            var one = c.ToString();

            // A '+' or '-' at the start, after an operator or after '(' is a sign, not a binary operator
            if ((c == '-' || c == '+') && IsUnaryPosition(tokens))
            {
                tokens.Add(new Token(TokenType.OPERATOR, c == '-' ? UnaryMinus : UnaryPlus));
                i++;
                continue;
            }

            if (Ops.ContainsKey(one))
            {
                tokens.Add(new Token(TokenType.OPERATOR, one));
                i++;
                continue;
            }

            throw new Exception($"Unexpected character '{c}'");
        }

        return tokens;
    }

    private static bool IsUnaryPosition(List<Token> tokens)
    {
        if (tokens.Count == 0)
            return true;

        var previous = tokens[^1].Type;
        return previous is TokenType.OPERATOR or TokenType.LEFT_PAREN;
    }

    private static List<Token> ToRpn(List<Token> tokens)
    {
        var output = new List<Token>();
        var stack = new Stack<Token>();

        foreach (var t in tokens)
        {
            switch (t.Type)
            {
                case TokenType.NUMBER:
                    output.Add(t);
                    break;
                case TokenType.OPERATOR:
                {
                    while (stack.Count > 0 && stack.Peek().Type == TokenType.OPERATOR)
                    {
                        var top = stack.Peek();
                        var (p1, r1) = Ops[t.Value];
                        var (p2, _) = Ops[top.Value];

                        if ((r1 == false && p1 <= p2) || (r1 == true && p1 < p2))
                            output.Add(stack.Pop());
                        else
                            break;
                    }
                    stack.Push(t);
                    break;
                }
                case TokenType.LEFT_PAREN:
                    stack.Push(t);
                    break;
                case TokenType.RIGHT_PAREN:
                {
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.LEFT_PAREN)
                        output.Add(stack.Pop());

                    if (stack.Count == 0)
                        throw new Exception("Mismatched parentheses");

                    stack.Pop();
                    break;
                }
            }
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t.Type == TokenType.LEFT_PAREN)
                throw new Exception("Mismatched parentheses");

            output.Add(t);
        }

        return output;
    }

    private static double EvalRpn(List<Token> rpn)
    {
        var stack = new Stack<double>();

        foreach (var t in rpn)
        {
            if (t.Type == TokenType.NUMBER)
            {

                if (t.Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    stack.Push(Convert.ToInt64(t.Value, 16));
                }
                else
                {
                    stack.Push(double.Parse(t.Value, CultureInfo.InvariantCulture));
                }

                continue;
            }

            if (t.Value is UnaryMinus or UnaryPlus)
            {
                if (stack.Count < 1)
                    throw new Exception("Invalid expression: missing operand");

                var operand = stack.Pop();
                stack.Push(t.Value == UnaryMinus ? -operand : operand);
                continue;
            }

            if (stack.Count < 2)
                throw new Exception($"Invalid expression: missing operand for '{t.Value}'");

            var b = stack.Pop();
            var a = stack.Pop();

            stack.Push(t.Value switch
            {
                "+"  => a + b,
                "-"  => a - b,
                "*"  => a * b,
                "/"  => a / b,
                "%"  => a % b,

                "<<" => (long)a << (int)b,
                ">>" => (long)a >> (int)b,
                "&"  => (long)a & (long)b,
                "|"  => (long)a | (long)b,
                "^"  => (long)a ^ (long)b,

                _ => throw new Exception($"Unknown operator {t.Value}")
            });
        }

        if (stack.Count != 1)
            throw new Exception("Invalid expression");

        return stack.Pop();
    }
}