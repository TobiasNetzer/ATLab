using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ATLab.Pcb.Kicad;

/// <summary>
/// Node of a KiCad S-expression file: either an atom (symbol, number or quoted string) or a list like
/// <c>(at 1.27 2.54 90)</c>, whose first item is its name.
/// </summary>
public sealed class SNode
{
    private static readonly IReadOnlyList<SNode> NoItems = [];

    private SNode(string? atom, IReadOnlyList<SNode> items)
    {
        Atom = atom;
        Items = items;
    }

    /// <summary>Text of an atom; null for a list.</summary>
    public string? Atom { get; }

    /// <summary>Items of a list, including the name; empty for an atom.</summary>
    public IReadOnlyList<SNode> Items { get; }

    public bool IsList => Atom is null;

    /// <summary>Name of a list (its first atom), e.g. "pad"; empty for an atom.</summary>
    public string Name => IsList && Items.Count > 0 && Items[0].Atom is { } name ? name : string.Empty;

    internal static SNode CreateAtom(string text) => new(text, NoItems);
    internal static SNode CreateList(List<SNode> items) => new(null, items);

    /// <summary>Atom after the name, e.g. index 0 of <c>(layer "F.Cu")</c> is F.Cu.</summary>
    public string? Arg(int index) => index + 1 < Items.Count ? Items[index + 1].Atom : null;

    public double ArgDouble(int index, double fallback = 0) =>
        Arg(index) is { } text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    /// <summary>All atoms after the name, e.g. the layers of <c>(layers "F.Cu" "F.Mask")</c>.</summary>
    public IEnumerable<string> Args() => Items.Skip(1).Select(i => i.Atom).OfType<string>();

    public SNode? Child(string name)
    {
        foreach (var item in Items)
            if (item.IsList && item.Name == name)
                return item;
        return null;
    }

    public IEnumerable<SNode> Children(string name) => Items.Where(i => i.IsList && i.Name == name);

    public override string ToString() => Atom ?? $"({Name} …)";
}

/// <summary>Reads the S-expression format of KiCad's board, footprint and schematic files.</summary>
public static class SExpressionParser
{
    public static SNode Parse(string text)
    {
        var position = 0;
        SkipWhitespace(text, ref position);
        if (position >= text.Length || text[position] != '(')
            throw new FormatException("The file does not start with an S-expression.");

        var root = ReadList(text, ref position);
        return root;
    }

    // Iterative, so deeply nested files cannot overflow the stack
    private static SNode ReadList(string text, ref int position)
    {
        var stack = new Stack<List<SNode>>();
        var current = new List<SNode>();
        position++; // (

        while (true)
        {
            SkipWhitespace(text, ref position);
            if (position >= text.Length)
                throw new FormatException("Unexpected end of file: a list is not closed.");

            var c = text[position];
            if (c == '(')
            {
                position++;
                stack.Push(current);
                current = [];
            }
            else if (c == ')')
            {
                position++;
                var list = SNode.CreateList(current);
                if (stack.Count == 0)
                    return list;
                current = stack.Pop();
                current.Add(list);
            }
            else if (c == '"')
            {
                current.Add(SNode.CreateAtom(ReadQuoted(text, ref position)));
            }
            else
            {
                var start = position;
                while (position < text.Length && !char.IsWhiteSpace(text[position]) && text[position] is not ('(' or ')'))
                    position++;
                current.Add(SNode.CreateAtom(text[start..position]));
            }
        }
    }

    private static string ReadQuoted(string text, ref int position)
    {
        position++; // opening quote
        var builder = new StringBuilder();
        while (position < text.Length)
        {
            var c = text[position++];
            if (c == '"')
                return builder.ToString();

            if (c == '\\' && position < text.Length)
            {
                var escaped = text[position++];
                builder.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => escaped
                });
            }
            else
            {
                builder.Append(c);
            }
        }

        throw new FormatException("Unexpected end of file: a text is not closed.");
    }

    private static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
            position++;
    }
}